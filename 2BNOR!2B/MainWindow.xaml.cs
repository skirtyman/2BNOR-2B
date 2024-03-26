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
        // Stores the main diagram of the application. This creates truth tables, diagrams 
        // and handles of the processing that is done in the application. 
        private readonly Diagram d;
        //  String used to save/load diagrams into and out of the application. 
        private string saveString = "";

        public MainWindow()
        {
            InitializeComponent();
            d = new Diagram(MainWindowCanvas);
        }

        /// <summary>
        /// Generates a truth table from the diagram that is currently drawn in the diagram 
        /// creation window. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_GenerateTableFromDiagram(object sender, RoutedEventArgs e)
        {
            // If the expression stored within the diagram doesn't exist then the diagram
            // must not exist and so error is flagged. 
            if (d.GetExpression() != "")
            {          
                d.DrawTruthTable(TruthTableCanvas, d.GetExpression());
                // Updatign status bar to reflect changes. 
                statusBar_Text.Text = $"Generated truth table for the expression {d.GetExpression()}.";             
            }
            else
            {
                MessageBox.Show("Error generating truth table: Diagram does not exist. ", "Truth table error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Clears the logic diagram currently drawn within the diagram creation window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_Diagram(object sender, RoutedEventArgs e)
        {
            // If the canvas has no elements (children) in it, then the diagram must not
            // exist. 
            if (MainWindowCanvas.Children.Count == 0)
            {
                statusBar_Text.Text = "Please draw a diagram first. ";
            }
            else
            {
                // Clearing the diagram, canvas and notifying the user through the status 
                // bar. 
                d.ClearDiagram();
                MainWindowCanvas.Children.Clear();
                statusBar_Text.Text = "Cleared the current diagram. ";
            }

        }

        /// <summary>
        /// Generates and displays a truth table from a user-entered boolean expression. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_GenerateTableFromExpression(object sender, RoutedEventArgs e)
        {
            var expressionInputDialog = new BooleanExpressionInputDialog();
            string expression;
            // Asking the user to enter a boolean expression and storing the result. 
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.Result;
                // If the entered expression is valid then the truth table can be drawn and 
                // reflecting that change within the status bar. 
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

        /// <summary>
        /// Displaying a rendered boolean expression from the user-drawn diagram. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_GenerateExpressionFromDiagram(object sender, RoutedEventArgs e)
        {
            renderedExpression r; 
            // The expression must exist otherwise an error has occurred. 
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

        /// <summary>
        /// Draws a logic gate diagram from a user entered boolean expression. Also allows
        /// users to constantly enter expressions without having to clear the canvas every
        /// time the would like a new diagram. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_GenerateDiagramFromExpression(object sender, RoutedEventArgs e)
        {
            var expressionInputDialog = new BooleanExpressionInputDialog();
            string expression;
            // Asking the user for a boolean expression and storing the result. 
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.Result;
                // Validating the entered expression and drawing it if it is. 
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

        /// <summary>
        /// Takes the user-drawn diagram and replaces it with the minimised form of it. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_MinimiseDiagram(object sender, RoutedEventArgs e)
        {
            string expression = d.GetExpression();
            string minimisedExpression;
            // Ensuring that the expression exists and the tree is not null to make sure
            // that minimisation can take place. Otherwise an error has occurred. 
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

        /// <summary>
        /// Accepts a user entered boolean expression and returns the rendered expression
        /// of the minimised result. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_MinimiseExpression(object sender, RoutedEventArgs e)
        {
            var expressionInputDialog = new BooleanExpressionInputDialog();
            renderedExpression r; 
            string expression = "";
            // Asking the user to enter a boolean expression. 
            if (expressionInputDialog.ShowDialog() == true)
            {
                expression = expressionInputDialog.Result;
                // Validating the expression and minimising if it is valid. 
                if (d.IsExpressionValid(expression))
                {
                    d.SetExpression(expression);
                    d.MinimiseExpression(expression);
                    r = new renderedExpression(d.GetExpression(), "Minimised Expression: ");
                    r.Show(); 
                    statusBar_Text.Text = "Minimised expression: " + expression; 
                }
                else
                {
                    MessageBox.Show("You have entered an invalid expression.", "Expression input error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
        }

        /// <summary>
        /// Saving a user-drawn diagram on the main window canvas to a text file, so 
        /// the user can save their work and continue at a later point. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_SaveDiagram(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog()
            {
                Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B",
                DefaultExt = "Expression file (*.2B)|*.2B"
            };
            // Showing the savefile dialog to find the desired file path. 
            saveFileDialog.ShowDialog();
            try
            {
                // Simply try writing all of the text to a file or an error has occurred and 
                // the user will be given an error message. 
                File.WriteAllText(saveFileDialog.FileName, saveString);
                statusBar_Text.Text = "Saved diagram at the path " + saveFileDialog.FileName;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving diagram:\n{ex.Message}", "Load File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Allows the user to load saved diagrams from text files so that they can 
        /// continue working on the same diagram/expression. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_LoadDiagram(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B",
                DefaultExt = "Expression file (*.2B)|*.2B"
            };
            // Showing the open file dialog to get the open path. 
            openFileDialog.ShowDialog();
            try
            {
                // Reading all text from from the file and this the expression to be drawn
                // to the main window. Only if it is valid otherwise an error has happened.
                saveString = File.ReadAllText(openFileDialog.FileName);
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

        /// <summary>
        /// Allows the user to export a png of their drawn diagram. This can be put into
        /// schoolwork or in a worksheet of some kind. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_ExportDiagram(object sender, RoutedEventArgs e)
        {
            Rect bounds;
            var crop = new CroppedBitmap();
            var pngEncoder = new PngBitmapEncoder();
            var sfd = new SaveFileDialog()
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
                // May not need bounds on the diagram. Use if things go pear shaped. 
                // No need to crop, directly export to png (canvas size as boudns for bitmap)
                bounds = d.GetBoundsOfDiagram();
                //If valid bounds can be calcuated then the window should be captured. 
                RenderTargetBitmap rtb = new((int)MainWindowCanvas.RenderSize.Width, (int)MainWindowCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Default);
                rtb.Render(MainWindowCanvas);
                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rtb));
                using (Stream stm = File.Create(sfd.FileName))
                {
                    png.Save(stm);
                }
                //MainWindowCanvas.Background = Brushes.WhiteSmoke;
                ////try
                ////{
                ////    crop = new CroppedBitmap(rtb, new Int32Rect(0, 0, (int)bounds.Width, (int)bounds.Height));
                ////}
                ////catch (Exception ex)
                ////{
                ////    MessageBox.Show($"Invalid bounds for exporting. Could not crop. \n{ex.Message}", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ////    e.Handled = true;
                ////}

                //try
                //{
                //    pngEncoder.Frames.Add(BitmapFrame.Create(rtb));
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show($"Could not encode the diagram. \n{ex.Message}", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //    e.Handled = true;
                //}

                //try
                //{
                //    using (Stream fs = File.OpenWrite(sfd.FileName))
                //    {
                //        pngEncoder.Save(fs);
                //    }
                //    statusBar_Text.Text = "Exported diagram to " + sfd.FileName;
                //}
                //catch (Exception ex)
                //{
                //    MessageBox.Show($"Could not write Image to the file. \n{ex.Message}", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                //    e.Handled = true;
                //}
            }
            else
            {
                MessageBox.Show("Could not calculate bounds: Diagram does not exist. \n", "Diagram Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;

            }
        }

        /// <summary>
        /// Closes the application. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_CloseApp(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Updates the state of the logic gate diagram when a click has occurred on the 
        /// main window canvas. This allows the diagrams to be interactive.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindowCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // If the tree does not exist then the user should be prompted to draw a
            // diagram first. Otherwise update the state of the diagram. 
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

        /// <summary>
        /// Clears the truth table canvas of any previously generated truth tables.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_TT(object sender, RoutedEventArgs e)
        {
            //Checking if there is a truth table currently on the canvas. 
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

        /// <summary>
        /// Displays information on the AND logic gate. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ANDInformation(object sender, EventArgs e)
        {
            var g = new GateInformation(d, "AND gate");
            g.Show(); 
        }

        /// <summary>
        /// Displays information on the OR logic gate. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ORInformation(object sender, EventArgs e)
        {
            var g = new GateInformation(d, "OR gate");
            g.Show();
        }

        /// <summary>
        /// Displays information on the NOT logic gate. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NOTInformation(object sender, EventArgs e)
        {
            var g = new GateInformation(d, "NOT gate");
            g.Show();
        }
        
        /// <summary>
        /// Displays information on the XOR logic gate.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XORInformation(object sender, EventArgs e)
        {
            var g = new GateInformation(d, "XOR gate");
            g.Show();
        }

        /// <summary>
        /// Displays information on the NAND logic gate.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NANDInformation(object sender, EventArgs e)
        {
            var g = new GateInformation(d, "NAND gate");
            g.Show();
        }

        /// <summary>
        /// Displays information on the NOR logic gate. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NORInformation(object sender, EventArgs e)
        {
            var g = new GateInformation(d, "NOR gate");
            g.Show();
        }
    }
}
