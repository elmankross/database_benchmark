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
        private readonly int _triangular;

        public Writer(string path, int linesCount, int bufferSize = 1 << 6)
        {
            _bufferSize = bufferSize;
            // https://en.wikipedia.org/wiki/Triangular_number
            _triangular = linesCount * (linesCount + 1) / 2;

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
            _watcher.Stop();
            _watcher.Dispose();
            await FlushAsync(onlyPart: false);
            await _stream.DisposeAsync();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task FlushAsync(bool onlyPart)
        {
            var readers = new StreamReader[_buffers.Length];

            foreach (var b in _buffers)
            {
                b.Seek(0, SeekOrigin.Begin);
            }

            using (var writer = new StreamWriter(_stream, leaveOpen: true))
            {
                var empty = false;
                for (var row = 0; ; row += 0x03) // ???? 0x05 ??? 0x04 int + LT (0x01)
                {
                    var lineBuffer = new string[_buffers.Length];
                    for (var column = 0; column < _buffers.Length; column++)
                    {
                        var reader = readers[column] ??= new StreamReader(_buffers[column], bufferSize: _bufferSize);
                        var columnValue = await reader.ReadLineAsync();

                        // save information about full processed buffer position
                        if (columnValue == null)
                        {
                            // it's empty value in line. Needs to revert prev buffers to return used values to the line
                            for (var j = column - 1; j >= 0; j--)
                            {
                                _buffers[j].Seek(-1, SeekOrigin.Current);
                            }

                            empty = true;
                            break;
                        }

                        lineBuffer[column] = columnValue;
                    }

                    if (empty)
                    {
                        break;
                    }

                    for (var i = 0; i < lineBuffer.Length; i++)
                    {
                        if (i > 0)
                        {
                            await writer.WriteAsync(' ');
                        }
                        await writer.WriteAsync(lineBuffer[i]);
                    }

                    await writer.WriteLineAsync();

                    // flush only part of buffer
                    if (onlyPart && row == _bufferSize)
                    {
                        break;
                    }
                }
            }

            await _stream.FlushAsync();

            // all flushed, nothing to shift
            if (!onlyPart)
            {
                return;
            }

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
                FlushAsync(onlyPart: true).Wait();
            }
            _locker.Release();
        }
    }
}
