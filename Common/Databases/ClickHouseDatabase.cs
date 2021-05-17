using Octonica.ClickHouseClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Databases
{
    public sealed class ClickHouseDatabase : IDatabase
    {
        public const string NAME = "ClickHouse";
        public string Name => NAME;
        public Writer Writer { get; }

        private readonly Logger _logger;
        private readonly TimeWatcher _watcher;
        private readonly Configuration _configuration;
        private readonly ClickHouseConnection _connection;

        public ClickHouseDatabase(Configuration config)
        {
            _watcher = new TimeWatcher();
            Writer = new Writer(NAME.ToLower() + ".data", 3);
            _watcher.ReceivedRange += async (_, e) => await Writer.WriteAsync((int)e.Key, e.Value);

            _logger = new Logger(NAME);
            _configuration = config;
            _connection = new ClickHouseConnection(_configuration.ConnectionString);

            _connection.Open();
            _connection.StateChange += (_, state) =>
            {
                if (state.CurrentState == ConnectionState.Broken)
                {
                    _connection.Close();
                    _connection.Open();
                }
            };
        }

        public async ValueTask DisposeAsync()
        {
            _logger.Write("[Dispose] Executing");
            await _connection.CloseAsync();
            _watcher.Dispose();
            _logger.Write("[Dispose] Executed");
            await _logger.DisposeAsync();
            await Writer.DisposeAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowColumns"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task InsertManyAsync(object[][] rowColumns, string[] columnNames, CancellationToken token = default)
        {
            _logger.Write("[InsertMany] Executing");
            await using (var writer = await _connection.CreateColumnWriterAsync(_configuration.InsertManyScript, token))
            {
                var columnIndex = 0;
                var columns = rowColumns.Aggregate(new object[columnNames.Length][], (current, column) =>
                {
                    /*
                     * Swap struct like:
                     *      [{ column1, column2, column3 }, { column1, column2, column3 }] 
                     * into:
                     *      [{ column1, column1, column1 }, { column2, column2, column2 }]
                     */
                    for (var row = 0; row < column.Length; row++)
                    {
                        current[row] = current[row] ?? new object[rowColumns.Length];
                        current[row][columnIndex] = column[row];
                    }
                    columnIndex++;
                    return current;
                });

                using (_watcher.Watch(TimeWatcher.Operation.InsertMany))
                {
                    await writer.WriteTableAsync(columns, columns.Length, token);
                }
            };
            _logger.Write("[InsertMany] Executed");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public async Task InsertOneAsync(IReadOnlyDictionary<string, object> columns, CancellationToken token = default)
        {
            _logger.Write("[InsertOne] Executing");
            using var command = _connection.CreateCommand(_configuration.InsertOneScript);
            foreach (var column in columns)
            {
                command.Parameters.AddWithValue(column.Key, column.Value);
            }

            using (_watcher.Watch(TimeWatcher.Operation.InsertOne))
            {
                await command.ExecuteNonQueryAsync(token);
            }
            _logger.Write("[InsertOne] Executed");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task SelectAsync(IReadOnlyDictionary<string, object> columns, CancellationToken token = default)
        {
            _logger.Write("[Select] Executing");
            using var command = _connection.CreateCommand(_configuration.SelectScript);
            foreach (var column in columns)
            {
                command.Parameters.AddWithValue(column.Key, column.Value);
            }

            using (_watcher.Watch(TimeWatcher.Operation.Select))
            {
                await command.ExecuteNonQueryAsync(token);
            }
            _logger.Write("[Select] Executed");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task SetupAsync(CancellationToken token = default)
        {
            _logger.Write("[Setup] Executing");
            using var command = _connection.CreateCommand(_configuration.SetupScript);
            await command.ExecuteNonQueryAsync(token);
            _logger.Write("[Setup] Executed");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task TeardownAsync(CancellationToken token = default)
        {
            _logger.Write("[Teardown] Executing");
            using var command = _connection.CreateCommand(_configuration.TeardownScript);

            // sudenly it closes internally 
            await _connection.CloseAsync();
            await _connection.OpenAsync();
            await command.ExecuteNonQueryAsync(token);

            _logger.Write("[Teardown] Executed");
        }
    }
}
