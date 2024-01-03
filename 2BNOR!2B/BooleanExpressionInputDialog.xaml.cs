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
    /// Interaction logic for BooleanExpressionInputDialog.xaml
    /// </summary>
    public partial class BooleanExpressionInputDialog : Window
    {
        public BooleanExpressionInputDialog()
        {
            InitializeComponent();
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        private void btnDialogCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            inputBox.SelectAll();
            inputBox.Focus();
        }

        public string result
        {
            get { return inputBox.Text; }
        }
    }
}
