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
    /// Interaction logic for GateInformation.xaml
    /// </summary>
    public partial class GateInformation : Window
    {
        private readonly string[] names = { "AND gate", "OR gate", "NOT gate", "XOR gate", "NAND gate", "NOR gate" };
        private readonly string[] descriptions = {"Outputs true only if both inputs are true. ", 
                                         "Outputs true if either one or both inputs are true.", 
                                         "Outputs true only if the input is false. A NOT gate inverts the input it is given.",
                                         "Outputs true only if one of the inputs are true but not both. It is known as exclusive OR. ",
                                         "Only true when one or none of the inputs are true. This is the same as applying a NOT gate to an AND gate.",
                                         "Only true when both of the inputs are false. This is the same as applying a NOT gate to an OR gate. "};
        private readonly string[] exampleExpressions = { "A.B", "A+B", "!A", "A^B", "!(A.B)", "!(A+B)" }; 

        public GateInformation(Diagram d, string gate)
        {
            InitializeComponent();
            gateName.Text = $"Name: {gate}";
            int i = Array.IndexOf(names, gate);
            gateDescription.Text = $"Description: {descriptions[i]}";
            var bc = new BooleanConverter();
            string converted = bc.ConvertString(exampleExpressions[i]);
            renderedExpressionBox.Formula = converted;
            d.DrawTruthTable(TruthTableCanvas, exampleExpressions[i]);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
