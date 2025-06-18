using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace IfcComparison.Logging
{
    public static class LoggingService
    {
        private static ILoggerFactory _loggerFactory;
        
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