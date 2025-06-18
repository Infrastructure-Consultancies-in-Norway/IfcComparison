using IfcComparison.Logging;
using IfcComparison.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;
using System.Windows.Controls;

namespace IfcComparison
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : UserControl
    {
        private ILogger _logger;

        public MainWindow()
        {
            var vm = new MainViewModel();
            this.DataContext = vm;
            InitializeComponent();

            // Initialize logging when control is created
            LoggerInitializer.EnsureInitialized();

            _logger = LoggingService.CreateLogger<MainWindow>();
            _logger.LogInformation("IfcComparison UserControl created at {Time}", DateTime.Now);

            // Subscribe to loaded event for logging
            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Detect if running standalone or embedded
            bool isStandalone = Application.Current.MainWindow?.GetType() == typeof(MainWindow);
            _logger.LogInformation("IfcComparison UserControl loaded at {Time}. Running mode: {Mode}",
                DateTime.Now, isStandalone ? "Standalone" : "Embedded");
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _logger.LogInformation("IfcComparison UserControl unloaded at {Time}", DateTime.Now);
        }
    }
}
