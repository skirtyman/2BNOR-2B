using _2BNOR_2B.Code;
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
        private readonly Diagram d;
        private string saveString = "";

        public MainWindow()
        {
            InitializeComponent();
            d = new Diagram(MainWindowCanvas);
        }

        private void MenuItem_GenerateTableFromDiagram(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new();
            if (d.GetExpression() != "")
            {          
                d.DrawTruthTable(TruthTableCanvas, d.GetExpression());
                statusBar_Text.Text = $"Generated truth table for the expression {d.GetExpression()}.";             
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

        private void MenuItem_GenerateTableFromExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new();
            string expression = "";
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.Result;
                if (d.IsExpressionValid(expression, true))
                {
                    d.DrawTruthTable(TruthTableCanvas, expression);
                    statusBar_Text.Text = "Generated Truth table from expression: " + expression; 
                }
                else
                {
                    MessageBox.Show("The diagram is invalid", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                
            }
        }

        private void MenuItem_GenerateExpressionFromDiagram(object sender, RoutedEventArgs e)
        {
            renderedExpression r; 
            if (d.GetExpression() != "")
            {
                r = new renderedExpression(d.GetExpression(), "Rendered Expression: ");
                r.Show(); 
            }
            else
            {
                MessageBox.Show("The diagram does not exist. ", "Expression Error", MessageBoxButton.OK, MessageBoxImage.Error); 
            }
        }


        private void MenuItem_GenerateDiagramFromExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new();
            string expression = "";
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.Result;
                if (d.IsExpressionValid(expression))
                {
                    MainWindowCanvas.Children.Clear();
                    d.ClearDiagram(); 
                    d.SetExpression(expression);
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
            string expression = d.GetExpression();
            string minimisedExpression;
            if (expression != "" && d.GetTree() != null)
            {
                MainWindowCanvas.Children.Clear();
                d.ClearDiagram();
                d.MinimiseExpression(expression);
                minimisedExpression = d.GetMinimisedExpression();  
                d.SetExpression(minimisedExpression);
                d.DrawDiagram(); 
            }
            else
            {
                MessageBox.Show("Error generating truth table: Diagram does not exist. ", "Truth table error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void MenuItem_MinimiseExpression(object sender, RoutedEventArgs e)
        {
            BooleanExpressionInputDialog expressionInputDialog = new();
            renderedExpression r; 
            string expression = "";
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.Result;
                d.SetExpression(expression);
                d.MinimiseExpression(expression);
                r = new renderedExpression(d.GetExpression(), "Minimised Expression: ");
                r.Show(); 
                statusBar_Text.Text = "Minimised expression: " + expression; 
            }
        }

        private void MenuItem_SaveDiagram(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B",
                DefaultExt = "Expression file (*.2B)|*.2B"
            };
            saveFileDialog.ShowDialog();
            try
            {
                File.WriteAllText(saveFileDialog.FileName, saveString);
                statusBar_Text.Text = "Saved diagram at the path " + saveFileDialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving diagram:\n{ex.Message}", "Load File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
        }

        private void MenuItem_LoadDiagram(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B",
                DefaultExt = "Expression file (*.2B)|*.2B"
            };
            openFileDialog.ShowDialog();
            try
            {
                saveString = File.ReadAllText(openFileDialog.FileName);
                //validate expression and if valid draw. 
                if (d.IsExpressionValid(saveString))
                {
                    d.SetExpression(saveString);
                    d.DrawDiagram();
                    statusBar_Text.Text = "Loaded diagram from " + openFileDialog.FileName;
                }
                else
                {
                    MessageBox.Show("This expression is invalid. Please try again. ");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file:\n{ex.Message}", "Open File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }    
        }

        private void MenuItem_ExportDiagram(object sender, RoutedEventArgs e)
        {
            Rect bounds;
            CroppedBitmap crop = new();
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            SaveFileDialog sfd = new()
            {
                Filter = "PNG (*.png)|*.png",
                DefaultExt = ".png"
            };
            if (sfd.ShowDialog() != true)
            {
                return;
            }

            if (d.GetExpression() != "")
            {
                bounds = d.GetBoundsOfDiagram();
                //If valid bounds can be calcuated then the window should be captured. 
                RenderTargetBitmap rtb = new((int)MainWindowCanvas.RenderSize.Width, (int)MainWindowCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Default);
                rtb.Render(MainWindowCanvas);
                try
                {
                    crop = new CroppedBitmap(rtb, new Int32Rect(0, 0, (int)bounds.Width, (int)bounds.Height));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid bounds for exporting. Could not crop. \n{ex.Message}", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error); 
                    e.Handled = true;
                }

                try
                { 
                    pngEncoder.Frames.Add(BitmapFrame.Create(crop));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not encode the diagram. \n{ex.Message}", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error); 
                    e.Handled = true;
                }

                try
                {
                    using (Stream fs = File.OpenWrite(sfd.FileName))
                    {
                        pngEncoder.Save(fs); 
                    }
                    statusBar_Text.Text = "Exported diagram to " + sfd.FileName; 
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Could not write Image to the file. \n{ex.Message}", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                }
            }
            else
            {
                MessageBox.Show("Could not calculate bounds: Diagram does not exist. \n", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;

            }
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

        private void ANDInformation(object sender, EventArgs e)
        {
            GateInformation g = new(d, "AND gate");
            g.Show(); 
        }

        private void ORInformation(object sender, EventArgs e)
        {
            GateInformation g = new(d, "OR gate");
            g.Show();
        }

        private void NOTInformation(object sender, EventArgs e)
        {
            GateInformation g = new(d, "NOT gate");
            g.Show();
        }
        
        private void XORInformation(object sender, EventArgs e)
        {
            GateInformation g = new(d, "XOR gate");
            g.Show();
        }

        private void NANDInformation(object sender, EventArgs e)
        {
            GateInformation g = new(d, "NAND gate");
            g.Show();
        }

        private void NORInformation(object sender, EventArgs e)
        {
            GateInformation g = new(d, "NOR gate");
            g.Show();
        }

    }
}
