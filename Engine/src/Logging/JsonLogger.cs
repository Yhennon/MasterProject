// Engine/src/Logging/JsonlLogger.cs
using System;
using System.Collections.Concurrent;   // BlockingCollection<T>
using System.IO;
using System.Text;                     // UTF8Encoding
using System.Threading;                // CancellationTokenSource
using System.Threading.Tasks;          // Task
using Newtonsoft.Json;                 // JsonConvert

namespace ScriptsOfTribute.Engine.Logging
{
    public sealed class JsonlLogger : IDisposable
    {
        private readonly StreamWriter _writer;
        // NOTE: older frameworks use ctor(boundedCapacity). Avoid "capacity:" named arg.
        private readonly BlockingCollection<string> _queue = new BlockingCollection<string>(10000);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly Task _pumpTask;

        public JsonlLogger(string path, bool append = true)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            _writer = new StreamWriter(
                new FileStream(
                    path,
                    append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true
            };

            _pumpTask = Task.Run(async () =>
            {
                try
                {
                    foreach (var line in _queue.GetConsumingEnumerable(_cts.Token))
                        await _writer.WriteLineAsync(line).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* normal on dispose */ }
            }, _cts.Token);
        }

        public void Write(object obj)
        {
            var json = JsonConvert.SerializeObject(
                obj,
                Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            // Non-blocking drop if queue full? Up to you. Here we block to keep logs complete.
            _queue.Add(json);
        }

        public void Dispose()
        {
            _queue.CompleteAdding();
            _cts.Cancel();
            try { _pumpTask.Wait(500); } catch { /* ignore */ }
            _writer.Flush();
            _writer.Dispose();
            _cts.Dispose();
            _queue.Dispose();
        }
    }
}
