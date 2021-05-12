using System;
using System.IO;
using System.Threading.Tasks;

namespace Common
{
    public sealed class Logger : IAsyncDisposable
    {
        private readonly FileStream _buffer;
        private readonly StreamWriter _writer;
        private readonly string _name;

        public Logger(string name)
        {
            _name = name;
            var path = Path.Combine(Writer.OUTPUT_DIR, name.ToLower() + ".log");
            _buffer = File.OpenWrite(path);
            _writer = new StreamWriter(_buffer) { AutoFlush = true };
        }

        public void Write(string message)
        {
            var line = string.Format("{0:yyyy-MM-dd HH:mm:sss.fff}\t{1}", DateTime.Now, message);
            _writer.WriteLine(line);
            Console.WriteLine("{0}\t{1}", line, _name);
        }

        public async ValueTask DisposeAsync()
        {
            await _buffer.FlushAsync();
            await _writer.DisposeAsync();
            await _buffer.DisposeAsync();
        }
    }
}
