using Microsoft.Extensions.Logging;
using System;

namespace IfcComparison.Logging
{
    public class ConsoleLoggerProvider : ILoggerProvider
    {
        private readonly Action<string> _outputAction;
        private readonly Func<string, LogLevel, bool> _filter;

        public ConsoleLoggerProvider(Action<string> outputAction, Func<string, LogLevel, bool> filter = null)
        {
            _outputAction = outputAction ?? throw new ArgumentNullException(nameof(outputAction));
            _filter = filter ?? ((_, _) => true);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new OutputConsoleLogger(categoryName, _outputAction, _filter);
        }

        public void Dispose()
        {
            // Nothing to dispose
        }

        private class OutputConsoleLogger : ILogger
        {
            private readonly string _categoryName;
            private readonly Action<string> _outputAction;
            private readonly Func<string, LogLevel, bool> _filter;

            public OutputConsoleLogger(string categoryName, Action<string> outputAction, Func<string, LogLevel, bool> filter)
            {
                _categoryName = categoryName;
                _outputAction = outputAction;
                _filter = filter;
            }

            public IDisposable BeginScope<TState>(TState state) => default;

            public bool IsEnabled(LogLevel logLevel) => _filter(_categoryName, logLevel);

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                // Only send the message text without category or level info
                var message = formatter(state, exception);
                _outputAction(message);
            }
        }
    }
}