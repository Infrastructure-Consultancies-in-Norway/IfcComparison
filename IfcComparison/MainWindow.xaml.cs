using IfcComparison.Converters;
using IfcComparison.Logging;
using IfcComparison.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

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
            _logger.LogDebug("IfcComparison UserControl created at {Time}", DateTime.Now);

            // Subscribe to loaded event for logging
            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
            
            // Add handlers for DataGrid
            DataGridIfcEntities.BeginningEdit += DataGrid_BeginningEdit;
            DataGridIfcEntities.CellEditEnding += DataGrid_CellEditEnding;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Detect if running standalone or embedded
            bool isStandalone = Application.Current.MainWindow?.GetType() == typeof(MainWindow);
            _logger.LogDebug("IfcComparison UserControl loaded at {Time}. Running mode: {Mode}",
                DateTime.Now, isStandalone ? "Standalone" : "Embedded");
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _logger.LogDebug("IfcComparison UserControl unloaded at {Time}", DateTime.Now);
        }

        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (e.Column.Header.ToString() == "IFC Property Set Name")
            {
                CollectionToStringConverter.IsEditing = true;
                CollectionToStringConverter.CurrentlyEditedObject = e.Row.DataContext;
                
                // Store the initial value
                if (e.Row.DataContext is IfcEntity entity)
                {
                    // Preserve the existing text value
                    string initialText = string.Join(", ", entity.IfcPropertySets ?? new List<string>());
                    CollectionToStringConverter.EditingCache[e.Row.DataContext] = initialText;
                }
            }
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "IFC Property Set Name")
            {
                // Get the edited text
                if (e.EditingElement is TextBox textBox && e.Row.DataContext is IfcEntity entity)
                {
                    string text = textBox.Text;
                    
                    // Parse comma-separated values and update the model
                    entity.IfcPropertySets = text.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();
                }
                
                // Reset editing state
                CollectionToStringConverter.IsEditing = false;
                CollectionToStringConverter.CurrentlyEditedObject = null;
                CollectionToStringConverter.EditingCache.Clear();
            }
        }
    }
}
