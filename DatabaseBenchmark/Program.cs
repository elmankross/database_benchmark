using Common;
using Common.Databases;
using Common.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DatabaseBenchmark
{
    class Program
    {
        private static readonly CancellationTokenSource _token = new CancellationTokenSource();

        static async Task Main(string[] _)
        {
            Console.Title = "Databases query time benchmark";
            Console.CancelKeyPress += new ConsoleCancelEventHandler((_, args) =>
            {
                // to prevent the process from terminating.
                args.Cancel = true;
                _token.Cancel();
            });
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets("8bb2ab70-b359-4ba4-8585-138908b69ee6")
                .Build();
            var databases = GetDatabases(config.GetSection("Databases"));
            var contract = GetContract(config.GetSection("Contract"));
            var sampleSize = config.GetValue<int>("SampleSize");
            var tableSize = config.GetValue<int>("TableSize");

            await Prologue(databases, contract, tableSize);
            await Middle(databases, contract, sampleSize);

            var datas = databases.Select(x => x.Writer.FullPath).ToArray();

            await Epilogue(databases);

            foreach (var data in datas)
            {
                GnuPlot.GnuPlot.With(data).Open();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static async Task Prologue(IReadOnlyList<IDatabase> databases, Contract contract, int tableSize)
        {
            var sampler = new Sampler(tableSize);
            sampler.FillUpWith(contract);

            foreach (var database in databases)
            {
                try
                {
                    sampler.FillUpWith(contract);
                    await database.SetupAsync(sampler.Buffer, contract.Names);
                }
                catch (Exception _)
                {
                    await database.TeardownAsync();
                    throw;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static async Task Middle(IReadOnlyList<IDatabase> databases, Contract contract, int sampleSize)
        {
            var tasks = new Task[databases.Count];
            for (var i = 0; i < databases.Count; i++)
            {
                tasks[i] = ExecuteBenchmarkBodyAsync(databases[i], contract, sampleSize);
            }
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static async Task ExecuteBenchmarkBodyAsync(IDatabase database, Contract contract, int sampleSize)
        {
            var random = new Random();
            var index = 0;
            Dictionary<string, object>[] cache = null;

            while (!_token.IsCancellationRequested)
            {
                var tinySampler = new Sampler(1);
                tinySampler.FillUpWith(contract);
                var singleModel = TranslateDataToContract(tinySampler.Buffer, contract, 1)[0];

                var massiveSampler = new Sampler(sampleSize);
                massiveSampler.FillUpWith(contract);

                if (index % 3 == 0 || index == 0)
                {
                    cache = TranslateDataToContract(massiveSampler.Buffer, contract);
                }

                await database.InsertManyAsync(massiveSampler.Buffer, contract.Names);
                await database.SelectAsync(cache[random.Next(cache.Length)]);
                await database.InsertOneAsync(singleModel);

                await Task.Delay(10);
                index++;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static async Task Epilogue(IReadOnlyList<IDatabase> databases)
        {
            for (var i = 0; i < databases.Count; i++)
            {
                await databases[i].TeardownAsync();
                await databases[i].DisposeAsync();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static IReadOnlyList<IDatabase> GetDatabases(IConfigurationSection config)
        {
            var databases = new List<IDatabase>(2);
            foreach (var db in config.GetChildren())
            {
                var dbConfig = db.Get<Configuration>();

                switch (db.Key)
                {
                    case ClickHouseDatabase.NAME:
                        databases.Add(new ClickHouseDatabase(dbConfig));
                        break;

                    case PostgresDatabase.NAME:
                        databases.Add(new PostgresDatabase(dbConfig));
                        break;
                }
            }
            return databases;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private static Contract GetContract(IConfigurationSection config)
        {
            var contract = new Contract();
            var contractPath = config.Value;
            var index = 0;

            foreach (var line in File.ReadLines(contractPath))
            {
                var parts = line.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var name = parts[0].Trim();
                var type = parts[1].Trim();
                var length = -1;

                var lengthIndex = type.IndexOf('(');
                if (lengthIndex != -1)
                {
                    var lengthStr = type.Substring(
                        lengthIndex + 1,
                        type.IndexOf(')') - lengthIndex - 1);
                    length = int.Parse(lengthStr);
                    type = type.Substring(0, lengthIndex);
                }

                contract.Add(index++, new ContractProperty
                {
                    Name = name,
                    Type = Enum.Parse<ContractProperty.PropertyType>(type, true),
                    MaxLength = length
                });
            }

            return contract;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="contract"></param>
        /// <param name="limit"></param>
        /// <returns></returns>
        private static Dictionary<string, object>[] TranslateDataToContract(object[][] data, Contract contract, int limit = 100)
            => data.Take(100)
                   .Select((row) => row
                       .Select((value, index) => new KeyValuePair<string, object>(contract[index].Name, value))
                       .ToDictionary(x => x.Key, x => x.Value))
                   .ToArray();
    }
}
