using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;

namespace IfcComparison.Logging
{
    public class FileLoggerProvider : ILoggerProvider
    {
        private readonly string _path;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly object _lock = new object();

        public FileLoggerProvider(string path, Func<string, LogLevel, bool> filter = null)
        {
            _path = path;
            _filter = filter;
            
            // Ensure directory exists
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name => new FileLogger(name, _path, _filter));
        }

        public void Dispose()
        {
            _loggers.Clear();
        }
    }

    public class FileLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly string _path;
        private readonly Func<string, LogLevel, bool> _filter;
        private static readonly object _lock = new object();

        public FileLogger(string categoryName, string path, Func<string, LogLevel, bool> filter)
        {
            _categoryName = categoryName;
            _path = path;
            _filter = filter;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (_filter == null || _filter(_categoryName, logLevel)) && logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            var logEntry = new StringBuilder();
            logEntry.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ");
            logEntry.Append($"[{logLevel}] ");
            logEntry.Append($"[{_categoryName}] ");
            logEntry.Append(message);
            
            if (exception != null)
            {
                logEntry.AppendLine();
                logEntry.Append($"Exception: {exception.Message}");
                logEntry.AppendLine();
                logEntry.Append(exception.StackTrace);
            }

            lock (_lock)
            {
                try
                {
                    File.AppendAllText(_path, logEntry.ToString() + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Cannot log to file, fallback to console
                    Console.WriteLine($"Failed to write to log file: {ex.Message}");
                    Console.WriteLine(logEntry.ToString());
                }
            }
        }
    }
}