using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace IfcComparison.Logging
{
    public static class LoggingService
    {
        private static ILoggerFactory _loggerFactory;
        private static Action<string> _outputConsoleAction;
        
        public static void Initialize(string logFilePath, LogLevel minimumLogLevel = LogLevel.Information)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                // Add console logging for development
                builder.AddConsole();
                
                // Add file logging
                builder.AddProvider(new FileLoggerProvider(logFilePath, 
                    (category, logLevel) => logLevel >= minimumLogLevel));
                
                // Set minimum log level
                builder.SetMinimumLevel(minimumLogLevel);
            });
        }
        
        public static void SetOutputConsoleAction(Action<string> outputAction)
        {
            _outputConsoleAction = outputAction;

            if (_loggerFactory == null)
            {
                throw new InvalidOperationException(
                    "Logger factory has not been initialized. Call Initialize method first.");
            }

            // If logger factory already exists, add the UI console provider
            if (_loggerFactory != null && _outputConsoleAction != null)
            {
                // We'll filter to only show Information level or higher in the UI
                _loggerFactory.AddProvider(new ConsoleLoggerProvider(
                    _outputConsoleAction, 
                    (category, logLevel) => logLevel >= LogLevel.Information));
            }
        }
        
        public static ILogger<T> CreateLogger<T>()
        {
            if (_loggerFactory == null)
            {
                throw new InvalidOperationException(
                    "Logger factory has not been initialized. Call Initialize method first.");
            }
            
            return _loggerFactory.CreateLogger<T>();
        }
        
        public static ILogger CreateLogger(string categoryName)
        {
            if (_loggerFactory == null)
            {
                throw new InvalidOperationException(
                    "Logger factory has not been initialized. Call Initialize method first.");
            }
            
            return _loggerFactory.CreateLogger(categoryName);
        }
    }
}