using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Common
{
    public sealed class TimeWatcher : IDisposable
    {
        private int _index;
        private IReadOnlyDictionary<Operation, ushort[]> _buffer;

        public event EventHandler<KeyValuePair<Operation, ushort[]>> ReceivedRange;

        public TimeWatcher(int bufferSize = 10)
        {
            _index = 0;
            _buffer = new Dictionary<Operation, ushort[]>
            {
                [Operation.Select] = new ushort[bufferSize],
                [Operation.InsertOne] = new ushort[bufferSize],
                [Operation.InsertMany] = new ushort[bufferSize]
            };
        }

        internal void AddToRange(Operation operation, ushort value)
        {
            if (_index == _buffer[operation].Length)
            {
                ReceivedRange?.Invoke(this, new KeyValuePair<Operation, ushort[]>(operation, _buffer[operation]));
                _index = 0;
            }

            _buffer[operation][_index++] = value;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TempWatcher Watch(Operation operation) => new TempWatcher(this, operation);

        public void Dispose()
        {
            foreach (var b in _buffer)
            {
                ReceivedRange?.Invoke(this, b);
            }
            _buffer = null;
        }

        /// <summary>
        /// 
        /// </summary>
        public struct TempWatcher : IDisposable
        {
            private readonly TimeWatcher _parent;
            private readonly Stopwatch _watcher;
            private readonly Operation _operation;

            internal TempWatcher(TimeWatcher parent, Operation operation)
            {
                _parent = parent;
                _operation = operation;
                _watcher = new Stopwatch();
                _watcher.Start();
            }

            public void Dispose()
            {
                _watcher.Stop();
                var elapsed = _watcher.ElapsedMilliseconds > ushort.MaxValue
                    ? ushort.MaxValue
                    : (ushort)_watcher.ElapsedMilliseconds;
                _parent.AddToRange(_operation, elapsed);
            }
        }

        public enum Operation : int
        {
            Select = 0,
            InsertOne = 1,
            InsertMany = 2
        }
    }
}
