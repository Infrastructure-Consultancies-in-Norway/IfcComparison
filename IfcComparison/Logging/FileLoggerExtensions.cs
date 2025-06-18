using Microsoft.Extensions.Logging;
using System;

namespace IfcComparison.Logging
{
    public static class FileLoggerExtensions
    {
        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path must be specified.", nameof(filePath));
            }

            builder.AddProvider(new FileLoggerProvider(filePath));
            return builder;
        }

        public static ILoggingBuilder AddFile(this ILoggingBuilder builder, string filePath, 
            Func<string, LogLevel, bool> filter)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path must be specified.", nameof(filePath));
            }

            builder.AddProvider(new FileLoggerProvider(filePath, filter));
            return builder;
        }
    }
}