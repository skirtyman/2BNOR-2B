using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
        private logicGate dragObject = null;
        private Point offset; 

        public MainWindow()
        {
            InitializeComponent();
            d = new diagram();
            d.setHandler(c_PreviewMouseDown); 
        }

        private void MenuItem_GenerateTableFromDiagram(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            StepsForTablesDialog stepsDialog = new StepsForTablesDialog();
            displayWindow displayWindow = new displayWindow();
            string expression = d.getInfixExpression();
            bool isSteps = false;
            if (stepsDialog.ShowDialog() == true)
            {
                isSteps = stepsDialog.result;
                displayWindow.setStatusForTables(expression);
                displayWindow.Show();
                Canvas c = displayWindow.getCanvas();
                d.drawTruthTable(c, expression, isSteps);
            }

        }

        private void componentSidePanel_AddBuiltinCircuit(object sender, RoutedEventArgs e)
        {
            //show the built in circuit dialog
            BuiltInCircuitDialog builtIntCircuitDialog = new BuiltInCircuitDialog();
            if (builtIntCircuitDialog.ShowDialog() == true)
            {

            }
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

        #region menustrip commands 
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
                if (stepsDialog.ShowDialog() == true)
                {
                    isSteps = stepsDialog.result;
                    displayWindow.setStatusForTables(expression);
                    displayWindow.Show();
                    Canvas c = displayWindow.getCanvas();
                    d.drawTruthTable(c, expression, isSteps);
                }
            }
        }

        private void MenuItem_GenerateExpressionFromDiagram(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(d.getInfixExpression()); 
        }


        private void MenuItem_GenerateDiagramFromExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            string expression = "";
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.result;
                statusBox_mainWindow.Text = "Status: Drew expression " + expression;
                d.drawDiagram(MainWindowCanvas, expression); 
            }
        }

        private void MenuItem_MinimiseExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            string expression = "";
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.result;
                MessageBox.Show(d.minimiseExpression(expression)); 
            }
        }

        private void MenuItem_CloseApp(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        private void MainWindowCanvas_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (dragObject == null)
            {
                return;
            }
            // in order to prevent the object from leaving the window, set the bounds with this condition
            // uses the canvas actual/current height and width to determine the bounds, even if the window will be resized
            IInputElement s = (IInputElement)sender;
            if ((e.GetPosition(s).X < MainWindowCanvas.ActualWidth - 50) && (e.GetPosition(s).Y < MainWindowCanvas.ActualHeight - 50) && (e.GetPosition(s).X > 50 && e.GetPosition(s).Y > 50))
            {
                Point position = e.GetPosition(s);
                Canvas.SetTop(dragObject, position.Y - offset.Y);
                Canvas.SetLeft(dragObject, position.X - offset.X);
                dragObject.updateWires(MainWindowCanvas); 
            }
        }

        public void c_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dragObject = (logicGate)sender;
            offset = e.GetPosition(MainWindowCanvas);
            offset.Y -= Canvas.GetTop(dragObject);
            offset.X -= Canvas.GetLeft(dragObject);
            MainWindowCanvas.CaptureMouse();
        }

        private void MainWindowCanvas_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            dragObject = null;
            MainWindowCanvas.ReleaseMouseCapture();
        }
    }
}
