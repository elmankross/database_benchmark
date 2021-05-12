using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Common.Databases
{
    public sealed class PostgresDatabase : IDatabase
    {
        public const string NAME = "PostgreSql";
        public string Name => NAME;
        public Writer Writer { get; }

        private readonly Logger _logger;
        private readonly TimeWatcher _watcher;
        private readonly NpgsqlConnection _connection;
        private readonly Configuration _configuration;

        public PostgresDatabase(Configuration config)
        {
            _watcher = new TimeWatcher();
            Writer = new Writer(NAME.ToLower() + ".data", 3);
            _watcher.ReceivedRange += async (_, e) => await Writer.WriteAsync((int)e.Key, e.Value);

            _logger = new Logger(NAME);
            _configuration = config;
            _connection = new NpgsqlConnection(_configuration.ConnectionString);

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
            _logger.Write("[Dispose] Executing.");
            await _connection.CloseAsync();
            _watcher.Dispose();
            _logger.Write("[Dispose] Executed.");
            await _logger.DisposeAsync();
            await Writer.DisposeAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>
        public async Task InsertOneAsync(IReadOnlyDictionary<string, object> columns)
        {
            _logger.Write("[InsertOne] Executing.");
            using var command = new NpgsqlCommand(_configuration.InsertOneScript, _connection);
            foreach (var column in columns)
            {
                command.Parameters.AddWithValue(column.Key, column.Value);
            }

            using var w = _watcher.Watch(TimeWatcher.Operation.InsertOne);
            await command.ExecuteNonQueryAsync();
            _logger.Write("[InsertOne] Executed.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowColumns"></param>
        /// <param name="columnNames"></param>
        /// <returns></returns>
        public async Task InsertManyAsync(object[][] rowColumns, string[] columnNames)
        {
            _logger.Write("[InsertMany] Executing.");
            using var writer = _connection.BeginBinaryImport(_configuration.InsertManyScript);
            foreach (var row in rowColumns)
            {
                await writer.StartRowAsync();
                foreach (var column in row)
                {
                    await writer.WriteAsync(column);
                }
            }

            using var w = _watcher.Watch(TimeWatcher.Operation.InsertMany);
            await writer.CompleteAsync();
            _logger.Write("[InsertMany] Executed.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task SelectAsync(IReadOnlyDictionary<string, object> columns)
        {
            _logger.Write("[Select] Executing.");
            using var command = new NpgsqlCommand(_configuration.SelectScript, _connection);
            foreach (var column in columns)
            {
                command.Parameters.AddWithValue(column.Key, column.Value);
            }

            using var w = _watcher.Watch(TimeWatcher.Operation.Select);
            var affectedRows = await command.ExecuteNonQueryAsync();
            _logger.Write("[Select] Executed with " + affectedRows + " rows.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task SetupAsync(object[][] rowColumns, string[] columnNames)
        {
            _logger.Write("[Setup] Executing.");
            using var command = new NpgsqlCommand(_configuration.SetupScript, _connection);
            await command.ExecuteNonQueryAsync();

            _logger.Write("[Setup] Preparing table.");
            await InsertManyAsync(rowColumns, columnNames);

            _logger.Write("[Setup] Executed.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task TeardownAsync()
        {
            _logger.Write("[Teardown] Executing.");
            using var command = new NpgsqlCommand(_configuration.TeardownScript, _connection);
            await command.ExecuteNonQueryAsync();
            _logger.Write("[Teardown] Executed.");
        }
    }
}
