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
using _2BNOR_2B.Code;

namespace _2BNOR_2B
{
    /// <summary>
    /// Interaction logic for renderedExpression.xaml
    /// </summary>
    public partial class renderedExpressionDisplay : Window
    {
        public renderedExpressionDisplay(string expression, string HeaderText)
        {
            InitializeComponent();
            // Rendering the boolean expression with latex. 
            var bc = new BooleanConverter();
            string converted = bc.ConvertString(expression); 
            renderedExpressionBox.Formula = converted;
            HeadingText.Text = HeaderText; 
        }

        /// <summary>
        /// A simple close button, so the user has a clear way of closing down the window. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close(); 
        }
    }
}
