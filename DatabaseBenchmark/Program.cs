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
        private static readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.Title = "Databases query time benchmark";
            Console.CancelKeyPress += new ConsoleCancelEventHandler((_, args) =>
            {
                // to prevent the process from terminating.
                args.Cancel = true;
                _cancellationSource.Cancel();
            });

            var withPrologue = false;
            var withMiddle = false;
            var withEpilogue = false;
            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-all":
                        withPrologue = withMiddle = withEpilogue = true;
                        break;
                    case "--withPrologue":
                    case "-wp":
                        withPrologue = true;
                        break;
                    case "--withMiddle":
                    case "-wm":
                        withMiddle = true;
                        break;
                    case "--withEpilogue":
                    case "-we":
                        withEpilogue = true;
                        break;
                }
            }

            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets("8bb2ab70-b359-4ba4-8585-138908b69ee6")
                .Build();
            var databases = GetDatabases(config.GetSection("Databases"));
            var contract = GetContract(config.GetSection("Contract"));
            var sampleSize = config.GetValue<int>("SampleSize");
            var tableSize = config.GetValue<int>("TableSize");

            if (withPrologue)
            {
                try
                {
                    await Prologue(databases, contract, tableSize);
                }
                catch (Exception)
                {
                    await Epilogue(databases);
                    throw;
                }
            }

            if (withMiddle)
            {
                await Middle(databases, contract, sampleSize);
            }

            var datas = databases.ToDictionary(x => x.Name, x => x.Writer.FullPath);

            if (withEpilogue)
            {
                await Epilogue(databases);
            }

            foreach (var (name, path) in datas)
            {
                GnuPlot.GnuPlot.With(path, name).Open();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static async Task Prologue(IReadOnlyList<IDatabase> databases, Contract contract, int tableSize)
        {
            foreach (var database in databases)
            {
                await database.SetupAsync(_cancellationSource.Token);
            }

            var batchSize = 50_000;
            var processed = 0;
            var tasks = new Task[databases.Count];

            while (processed != tableSize && !_cancellationSource.IsCancellationRequested)
            {
                var size = Math.Min(tableSize - processed, batchSize);
                var sampler = new Sampler(size);
                sampler.FillUpWith(contract);

                for (var i = 0; i < databases.Count; i++)
                {
                    tasks[i] = databases[i].InsertManyAsync(sampler.Buffer, contract.Names, _cancellationSource.Token);
                }

                try
                {
                    Task.WaitAll(tasks);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                processed += size;
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

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                return;
            }
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

            while (!_cancellationSource.IsCancellationRequested)
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

                await database.InsertManyAsync(massiveSampler.Buffer, contract.Names, _cancellationSource.Token);
                await database.SelectAsync(cache[random.Next(cache.Length)], _cancellationSource.Token);
                await database.InsertOneAsync(singleModel, _cancellationSource.Token);

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
            try
            {
                for (var i = 0; i < databases.Count; i++)
                {
                    await databases[i].TeardownAsync(_cancellationSource.Token);
                    await databases[i].DisposeAsync();
                }
            }
            catch (OperationCanceledException)
            {
                return;
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
            var index = 0;

            foreach (var line in File.ReadLines(config.Value))
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
            => data.Take(limit)
                   .Select((row) => row
                       .Select((value, index) => new KeyValuePair<string, object>(contract[index].Name, value))
                       .ToDictionary(x => x.Key, x => x.Value))
                   .ToArray();
    }
}
