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
    /// Interaction logic for StepsForTablesDialog.xaml
    /// </summary>
    public partial class StepsForTablesDialog : Window
    {
        public bool result
        {
            get; set;
        }

        public StepsForTablesDialog()
        {
            InitializeComponent();
        }

        private void btnDialogYes_Click(object sender, RoutedEventArgs e)
        {
            result = true;
            this.DialogResult = true;
           
        }

        private void btnDialogNo_Click(object sender, RoutedEventArgs e)
        {
            result = false;
            this.DialogResult = true;
        }
    }
}
