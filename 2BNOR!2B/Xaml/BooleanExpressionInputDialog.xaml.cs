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

        /// <summary>
        /// The user has written an expression and they have clicked ok. This means 
        /// that the expression is ready to be validated and processed if valid. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        
        /// <summary>
        /// The user does not want to enter an expression. Respond with no entered expression. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDialogCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        /// <summary>
        /// When this window is shown to the screen select the text box so the user can 
        /// immediately start typing and also draw attention to the window, by focusing 
        /// on it. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ContentRendered(object sender, EventArgs e)
        {
            inputBox.SelectAll();
            inputBox.Focus();
        }

        /// <summary>
        /// Gets the result of the dialog. This is the users' inputted boolean expression. 
        /// </summary>
        public string Result
        {
            get { return inputBox.Text; }
        }
    }
}
