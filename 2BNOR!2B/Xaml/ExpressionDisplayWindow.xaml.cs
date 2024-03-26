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
    public partial class renderedExpression : Window
    {
        public renderedExpression(string expression, string HeaderText)
        {
            InitializeComponent();
            var bc = new BooleanConverter();
            string converted = bc.ConvertString(expression); 
            renderedExpressionBox.Formula = converted;
            HeadingText.Text = HeaderText; 
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close(); 
        }
    }
}
