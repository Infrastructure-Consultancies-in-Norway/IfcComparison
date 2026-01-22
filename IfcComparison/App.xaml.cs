using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using IfcComparison.CLI;
using IfcComparison.Logging;
using Microsoft.Extensions.Logging;

namespace IfcComparison
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            // Initialize logging
            LoggerInitializer.EnsureInitialized();
            var logger = LoggingService.CreateLogger<App>();
            
            // Check for CLI mode
            if (e.Args.Length > 0)
            {
                // CLI mode - check for --settings argument
                string settingsFile = null;
                
                if (e.Args.Length == 1)
                {
                    // Single argument - assume it's the settings file path
                    settingsFile = e.Args[0];
                }
                else if (e.Args.Length >= 2)
                {
                    // Check for --settings flag
                    for (int i = 0; i < e.Args.Length - 1; i++)
                    {
                        if (e.Args[i].Equals("--settings", StringComparison.OrdinalIgnoreCase) ||
                            e.Args[i].Equals("-s", StringComparison.OrdinalIgnoreCase))
                        {
                            settingsFile = e.Args[i + 1];
                            break;
                        }
                    }
                    
                    // If no flag found, use first argument
                    if (settingsFile == null)
                    {
                        settingsFile = e.Args[0];
                    }
                }
                
                // Check for help flag
                if (e.Args.Any(arg => arg.Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                                     arg.Equals("-h", StringComparison.OrdinalIgnoreCase) ||
                                     arg.Equals("/?", StringComparison.OrdinalIgnoreCase)))
                {
                    ShowCliHelp();
                    Environment.Exit(0);
                    return;
                }
                
                if (!string.IsNullOrWhiteSpace(settingsFile))
                {
                    logger.LogInformation("Starting CLI mode with settings file: {SettingsFile}", settingsFile);
                    
                    // Run in CLI mode
                    var cliRunner = new CliRunner();
                    var exitCode = await cliRunner.RunAsync(settingsFile);
                    
                    // Exit application with the result code
                    Environment.Exit(exitCode);
                    return;
                }
                else
                {
                    Console.Error.WriteLine("ERROR: No settings file specified.");
                    Console.Error.WriteLine("Use --help for usage information.");
                    Environment.Exit(1);
                    return;
                }
            }
            
            // GUI mode
            base.OnStartup(e);
            logger.LogDebug("IfcComparison standalone application started at {Time}", DateTime.Now);
        }
        
        private void ShowCliHelp()
        {
            Console.WriteLine("IfcComparison CLI - IFC File Comparison Tool");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  IfcComparison.exe <settings-file>");
            Console.WriteLine("  IfcComparison.exe --settings <settings-file>");
            Console.WriteLine("  IfcComparison.exe -s <settings-file>");
            Console.WriteLine();
            Console.WriteLine("OPTIONS:");
            Console.WriteLine("  --settings, -s    Path to the JSON settings file");
            Console.WriteLine("  --help, -h, /?    Show this help message");
            Console.WriteLine();
            Console.WriteLine("EXAMPLE:");
            Console.WriteLine("  IfcComparison.exe \"C:\\path\\to\\settings.json\"");
            Console.WriteLine();
            Console.WriteLine("For GUI mode, run without arguments:");
            Console.WriteLine("  IfcComparison.exe");
            Console.WriteLine();
            Console.WriteLine("See readme.md for JSON settings file format and examples.");
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
