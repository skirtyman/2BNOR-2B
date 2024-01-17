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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _2BNOR_2B
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private diagram d; 

        public MainWindow()
        {
            InitializeComponent();
            d = new diagram();
        }

        private void MenuItem_OpenDiagramMinimisationWindow(object sender, RoutedEventArgs e)
        {
            //creates an instance of the diagram minimisation window. (click event)
            DiagramMinimisationWindow diagramMinimisationWindow = new DiagramMinimisationWindow(); 
            diagramMinimisationWindow.Show();
        }

        private void MenuItem_OpenHelpWindow(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Show();
        }

        private void MenuItem_GenerateTableFromExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            StepsForTablesDialog stepsDialog = new StepsForTablesDialog();
            displayWindow displayWindow = new displayWindow();
            string expression = "";
            bool isSteps = false; 
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.result; 
            }
            if (stepsDialog.ShowDialog() == true)
            {
                isSteps = stepsDialog.result;
            }
            displayWindow.setStatusForTables(expression); 
            displayWindow.Show();
            Canvas c = displayWindow.getCanvas(); 
            d.drawTruthTable(c, expression, isSteps);
        }

        private void MenuItem_GenerateDiagramFromExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            string expression = ""; 
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.result;
            }
            d.drawDiagram(MainWindowCanvas, expression);
        }

        private void MenuItem_GenerateTableFromDiagram(object sender, RoutedEventArgs e)
        {
        }

        private void componentSidePanel_AddBuiltinCircuit(object sender, RoutedEventArgs e)
        {
            //show the built in circuit dialog
            BuiltInCircuitDialog builtIntCircuitDialog = new BuiltInCircuitDialog();
            builtIntCircuitDialog.Show();
        }

        //Debug button to remove items from the canvas. 
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MainWindowCanvas.Children.Clear(); 
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
