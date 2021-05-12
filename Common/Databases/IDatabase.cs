using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common.Databases
{
    public interface IDatabase : IAsyncDisposable
    {
        string Name { get; }
        Writer Writer { get; }

        Task SetupAsync(object[][] rowColumns, string[] columnNames);
        Task TeardownAsync();
        Task SelectAsync(IReadOnlyDictionary<string, object> columns);
        Task InsertOneAsync(IReadOnlyDictionary<string, object> columns);
        Task InsertManyAsync(object[][] rowColumns, string[] columnNames);
    }
}
