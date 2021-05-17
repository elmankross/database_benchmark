using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Databases
{
    public interface IDatabase : IAsyncDisposable
    {
        string Name { get; }
        Writer Writer { get; }

        Task SetupAsync(CancellationToken token);
        Task TeardownAsync(CancellationToken token);
        Task SelectAsync(IReadOnlyDictionary<string, object> columns, CancellationToken token);
        Task InsertOneAsync(IReadOnlyDictionary<string, object> columns, CancellationToken token);
        Task InsertManyAsync(object[][] rowColumns, string[] columnNames, CancellationToken token);
    }
}
