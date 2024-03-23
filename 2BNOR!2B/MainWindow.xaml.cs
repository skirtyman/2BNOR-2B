using _2BNOR_2B.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
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
        private Diagram d;
        private string saveString = "";

        public MainWindow()
        {
            InitializeComponent();
            d = new Diagram(MainWindowCanvas);
        }

        private void MenuItem_GenerateTableFromDiagram(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            StepsForTablesDialog stepsDialog = new StepsForTablesDialog();
            bool isSteps = false;
            if (d.getExpression() != "")
            {
                if (stepsDialog.ShowDialog() == true)
                {
                    isSteps = stepsDialog.result;
                    d.DrawTruthTable(TruthTableCanvas, d.getExpression(), isSteps);
                    statusBar_Text.Text = $"Generated truth table for the expression {d.getExpression()}."; 
                }
            }
            else
            {
                MessageBox.Show("Error generating truth table: Diagram does not exist. ", "Truth table error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //Debug button to remove items from the canvas. 
        private void Button_Click_Diagram(object sender, RoutedEventArgs e)
        {
            if (MainWindowCanvas.Children.Count == 0)
            {
                statusBar_Text.Text = "Please draw a diagram first. ";
            }
            else
            {
                d.ClearDiagram();
                MainWindowCanvas.Children.Clear();
                statusBar_Text.Text = "Cleared the current diagram. ";
            }

        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        #region menustrip commands 

        private void MenuItem_OpenHelpWindow(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Show();
            statusBar_Text.Text = "Opened help window.";
        }

        private void MenuItem_GenerateTableFromExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            StepsForTablesDialog stepsDialog = new StepsForTablesDialog();
            string expression = "";
            bool isSteps = false;
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.result;
                if (stepsDialog.ShowDialog() == true)
                {
                    isSteps = stepsDialog.result;
                    d.DrawTruthTable(TruthTableCanvas, expression, isSteps);
                    statusBar_Text.Text = "Generated Truth table from expression: " + expression; 
                }
            }
        }

        private void MenuItem_GenerateExpressionFromDiagram(object sender, RoutedEventArgs e)
        {
            
        }


        private void MenuItem_GenerateDiagramFromExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            string expression = "";
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.result;
                if (d.isExpressionValid(expression))
                {
                    MainWindowCanvas.Children.Clear();
                    d.ClearDiagram(); 
                    d.setExpression(expression);
                    statusBar_Text.Text = "Generated diagram from expression: " + expression;
                    d.DrawDiagram();
                    saveString = expression;
                }
                else
                {
                    MessageBox.Show("This expression is invalid. Please try again. ");
                }
            }
        }

        private void MenuItem_MinimiseDiagram(object sender, RoutedEventArgs e)
        {
            string expression = d.getExpression();
            string minimisedExpression;
            if (expression != "")
            {
                MainWindowCanvas.Children.Clear();
                d.ClearDiagram();
                d.MinimiseExpression(expression);
                minimisedExpression = d.getMinimisedExpression();  
                d.setExpression(minimisedExpression);
                d.DrawDiagram(); 
            }
            else
            {
                MessageBox.Show("Error generating truth table: Diagram does not exist. ", "Truth table error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void MenuItem_MinimiseExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new BooleanExpressionInputDialog();
            string expression = "";
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.result;
                d.setExpression(expression);
                d.MinimiseExpression(expression);
                MessageBox.Show(d.getMinimisedExpression());
                statusBar_Text.Text = "Minimised expression: " + expression; 
            }
        }

        private void MenuItem_SaveDiagram(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B";
            saveFileDialog.DefaultExt = "Expression file (*.2B)|*.2B";
            saveFileDialog.ShowDialog();
            File.WriteAllText(saveFileDialog.FileName, saveString);
            statusBar_Text.Text = "Saved diagram at the path " + saveFileDialog.FileName;
        }

        private void MenuItem_LoadDiagram(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B";
            openFileDialog.DefaultExt = "Expression file (*.2B)|*.2B";
            openFileDialog.ShowDialog();
            saveString = File.ReadAllText(openFileDialog.FileName);
            if (d.isExpressionValid(saveString))
            {
                d.setExpression(saveString);
                d.DrawDiagram();
                statusBar_Text.Text = "Loaded diagram from " + openFileDialog.FileName;
            }
            else
            {
                MessageBox.Show("This expression is invalid. Please try again. ");
            }
           
        }

        private void MenuItem_ExportDiagram(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG (*.png)|*.png";
            sfd.DefaultExt = ".png";
            if (sfd.ShowDialog() != true)
            {
                return;
            }

            Rect bounds = d.GetBoundsOfDiagram();

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)MainWindowCanvas.RenderSize.Width, (int)MainWindowCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Default);
            rtb.Render(MainWindowCanvas);

            CroppedBitmap crop = new CroppedBitmap(rtb, new Int32Rect(0,0, (int)bounds.Width, (int)bounds.Height));

            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(crop));


            using (Stream fs = File.OpenWrite(sfd.FileName))
            {
                pngEncoder.Save(fs);
            }
            statusBar_Text.Text = "Exported diagram to " + sfd.FileName;
        }

        private void MenuItem_CloseApp(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        private void MainWindowCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //Refreshes the wire states whenever the canvas is clicked. 
            if (d.GetTree() != null)
            {
                d.UpdateWires();
                statusBar_Text.Text = "Updated the state of the diagram.";
            }
            else
            {
                statusBar_Text.Text = "Please draw a diagram first. ";
            }
        }

        private void Button_Click_TT(object sender, RoutedEventArgs e)
        {
            if (TruthTableCanvas.Children.Count == 0)
            {
                statusBar_Text.Text = "Please draw a truth table first. "; 
            }
            else
            {
                TruthTableCanvas.Children.Clear();
                statusBar_Text.Text = "Cleared the current truth table. ";
            }
        }
    }
}
