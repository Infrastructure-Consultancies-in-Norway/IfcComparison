using System;
using System.IO;
using System.Windows;
using IfcComparison.Logging;
using Microsoft.Extensions.Logging;

namespace IfcComparison
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize logging
            LoggerInitializer.EnsureInitialized();
            
            // Log application startup
            var logger = LoggingService.CreateLogger<App>();
            logger.LogDebug("IfcComparison standalone application started at {Time}", DateTime.Now);
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var logger = LoggingService.CreateLogger<App>();
                logger.LogDebug("IfcComparison standalone application exiting at {Time}", DateTime.Now);
            }
            catch
            {
                // Ignore errors during shutdown
            }
            
            base.OnExit(e);
        }
    }
}
