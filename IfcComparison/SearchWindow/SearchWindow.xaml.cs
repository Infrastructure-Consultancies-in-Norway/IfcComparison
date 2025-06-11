using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IfcComparison
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
        private SearchWindowViewModel vm { get; set; }
        public SearchWindow(object currentCell)
        {
            vm = new SearchWindowViewModel(currentCell);
            DataContext = vm;
            InitializeComponent();

            //PreviewKeyDown += (s, e) => { if (e.Key == Key.Return) Close(); };

            var ifcEntitylistView = (ListView)this.FindName("IfcEntityListView");

            ifcEntitylistView.PreviewKeyDown += new KeyEventHandler(HandleReturn);
            this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
        }

        private void HandleReturn(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                vm.ReturnSelectedItemText();
                Close();
            }
        }

        private void HandleEsc(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }



    }
}
