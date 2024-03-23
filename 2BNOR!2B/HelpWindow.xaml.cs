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

namespace _2BNOR_2B
{
    /// <summary>
    /// Interaction logic for HelpWindow.xaml
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void Button_ToggleSideWindow(object sender, RoutedEventArgs e)
        {
            if (listItems.Visibility == Visibility.Collapsed)
            {
                listItems.Visibility = Visibility.Visible;
            }
            else
            {
                listItems.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            currentDocument.Document = Application.Current.FindResource("doctext2") as FlowDocument; 
        }
    }
}
