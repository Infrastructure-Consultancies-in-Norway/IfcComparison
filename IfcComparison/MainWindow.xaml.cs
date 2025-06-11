using IfcComparison.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace IfcComparison
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : UserControl
    {
        public MainWindow()
        {
            var vm = new MainViewModel();
            this.DataContext = vm;
            InitializeComponent();
        }
    }
}
