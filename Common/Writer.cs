using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common
{
    public sealed class Writer : IAsyncDisposable
    {
        public const string OUTPUT_DIR = "_output";

        public string FullPath { get; }

        private readonly int _bufferSize;
        private readonly FileStream _stream;
        private readonly MemoryStream[] _buffers;
        private readonly System.Timers.Timer _watcher;
        private readonly SemaphoreSlim _locker;

        public Writer(string path, int linesCount, int bufferSize = 2 << 3)
        {
            _bufferSize = bufferSize;
            _locker = new SemaphoreSlim(1, 1);

            _watcher = new System.Timers.Timer(200);
            _watcher.Elapsed += Watcher_Elapsed;
            _watcher.Start();

            if (!Directory.Exists(OUTPUT_DIR))
            {
                Directory.CreateDirectory(OUTPUT_DIR);
            }

            FullPath = Path.Combine(OUTPUT_DIR, path);
            _stream = File.Open(FullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _buffers = new MemoryStream[linesCount];
            for (var i = 0; i < _buffers.Length; i++)
            {
                _buffers[i] = new MemoryStream(_bufferSize);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="data"></param>
        public async Task WriteAsync(int line, ushort[] data)
        {
            if (line > _buffers.Length)
            {
                throw new Exception("too many lines. It's not declared.");
            }

            var stream = _buffers[line];
            using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };
            foreach (var d in data)
            {
                writer.WriteLine(d);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await FlushAsync();
            await _stream.DisposeAsync();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task FlushAsync()
        {
            var readers = new StreamReader[_buffers.Length];

            foreach (var b in _buffers)
            {
                b.Seek(0, SeekOrigin.Begin);
            }

            using (var writer = new StreamWriter(_stream, leaveOpen: true))
            {
                for (var row = 0; row < _bufferSize; row += 0x03)
                {
                    for (var column = 0; column < _buffers.Length; column++)
                    {
                        var reader = readers[column] ??= new StreamReader(_buffers[column], bufferSize: _bufferSize);
                        var columnValue = await reader.ReadLineAsync();

                        await writer.WriteAsync(columnValue);
                        await writer.WriteAsync(' ');
                    }
                    await writer.WriteLineAsync();
                }
            }

            await _stream.FlushAsync();

            // shift values from over buffer size, like |buffer_size|.... to the begining of the streams
            foreach (var b in _buffers)
            {
                var count = (int)(b.Length - b.Position);
                var buffer = new byte[count];
                await b.ReadAsync(buffer, 0, count);
                b.Seek(0, SeekOrigin.Begin);
                await b.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Periodical flush buffr data to disk
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Watcher_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // already works
            if (_locker.CurrentCount == 0)
            {
                return;
            }

            _locker.Wait();
            if (_buffers.All(x => x.Position >= _bufferSize))
            {
                FlushAsync().Wait();
            }
            _locker.Release();
        }
    }
}
