using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;

namespace IfcComparison.Logging
{
    public static class LoggerInitializer
    {
        private static bool _isInitialized = false;
        private static readonly object _initLock = new object();
        
        /// <summary>
        /// Initializes the logging system if it hasn't been initialized already.
        /// This method is thread-safe and can be called multiple times.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_isInitialized) return;
            
            lock (_initLock)
            {
                if (_isInitialized) return;
                
                try
                {
                    // Create logs directory in AppData
                    var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var logDir = Path.Combine(appDataPath, "IfcComparison", "Logs");
                    
                    // Ensure directory exists
                    if (!Directory.Exists(logDir))
                    {
                        Directory.CreateDirectory(logDir);
                    }
                    
                    // Create a log file for each day
                    var logFile = Path.Combine(logDir, $"IfcComparison-{DateTime.Now:yyyy-MM-dd}.log");
                    
                    // Initialize logging service
                    LoggingService.Initialize(logFile, LogLevel.Information);
                    
                    var logger = LoggingService.CreateLogger(typeof(LoggerInitializer).FullName);
                    logger.LogDebug("Logging system initialized at {Time}", DateTime.Now);
                    
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    // Only show message box in UI contexts, not when called from non-UI threads
                    if (Application.Current != null)
                    {
                        MessageBox.Show($"Failed to initialize logging: {ex.Message}", "Logging Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}