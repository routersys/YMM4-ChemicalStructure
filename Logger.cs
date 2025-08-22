using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YMM4ChemicalStructurePlugin.Shape
{
    public enum LogType
    {
        Info,
        Warning,
        Error,
        Debug
    }

    public sealed class AsyncLogger : IDisposable
    {
        private static readonly Lazy<AsyncLogger> _instance = new Lazy<AsyncLogger>(() => new AsyncLogger());
        public static AsyncLogger Instance => _instance.Value;

        private readonly BlockingCollection<string> _logQueue = new BlockingCollection<string>();
        private readonly Task _processingTask;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private string _logFilePath = "";
        private const long MaxLogSize = 30720; // 30 KB

        private AsyncLogger()
        {
            _processingTask = Task.Factory.StartNew(
                ProcessLogQueue,
                _cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        public void Initialize(string logDirectory)
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            _logFilePath = Path.Combine(logDirectory, "plugin_log.txt");
            Log(LogType.Info, "Logger initialized.");
        }

        public void Log(LogType type, string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (_cancellationTokenSource.IsCancellationRequested) return;

            try
            {
                string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}][{type,-7}] [{Path.GetFileName(sourceFilePath)}:{memberName}:{sourceLineNumber}] {message}";
                _logQueue.Add(formattedMessage);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ProcessLogQueue()
        {
            try
            {
                foreach (var message in _logQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
                {
                    WriteToFile(message);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in logger processing task: {ex.Message}");
            }
        }

        private void WriteToFile(string message)
        {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    if (fileInfo.Length > MaxLogSize)
                    {
                        var lines = File.ReadAllLines(_logFilePath, Encoding.UTF8).ToList();
                        var linesToKeep = new List<string>();
                        long currentSize = 0;

                        for (int i = lines.Count - 1; i >= 0; i--)
                        {
                            var line = lines[i];
                            var lineSize = Encoding.UTF8.GetByteCount(line + Environment.NewLine);
                            if (currentSize + lineSize > MaxLogSize)
                            {
                                break;
                            }
                            linesToKeep.Insert(0, line);
                            currentSize += lineSize;
                        }

                        File.WriteAllLines(_logFilePath, linesToKeep, Encoding.UTF8);
                    }
                }

                using (var sw = new StreamWriter(_logFilePath, true, Encoding.UTF8))
                {
                    sw.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _logQueue.CompleteAdding();
            _cancellationTokenSource.Cancel();
            _processingTask.Wait();
            _logQueue.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}