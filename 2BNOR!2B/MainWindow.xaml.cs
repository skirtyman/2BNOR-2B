using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
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
        private string saveString = ""; 

        public MainWindow()
        {
            InitializeComponent();
            d = new diagram(MainWindowCanvas);
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
                d.drawDiagram(expression);
               
                saveString = expression; 
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

        private void MenuItem_SaveDiagram(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B";
            saveFileDialog.DefaultExt = "Expression file (*.2B)|*.2B";
            saveFileDialog.ShowDialog();
            File.WriteAllText(saveFileDialog.FileName, saveString);
        }

        private void MenuItem_LoadDiagram(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text file (*.txt)|*.txt|XML (*.xml)|*.xml|Expression file (*.2B)|*.2B";
            openFileDialog.DefaultExt = "Expression file (*.2B)|*.2B";
            openFileDialog.ShowDialog();
            saveString = File.ReadAllText(openFileDialog.FileName);
            d.drawDiagram(saveString);
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


            //Rect bounds = VisualTreeHelper.GetDescendantBounds(MainWindowCanvas);
            Rect bounds = d.getBoundsOfDiagram(); 
            double dpi = 96d;

            RenderTargetBitmap rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, dpi, dpi, PixelFormats.Default);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(MainWindowCanvas);
                dc.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
            }

            rtb.Render(dv);

            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

            try
            {
                MemoryStream ms = new MemoryStream();

                pngEncoder.Save(ms);
                ms.Close();
                File.WriteAllBytes(sfd.FileName, ms.ToArray());
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_CloseApp(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        private void MainWindowCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //MessageBox.Show("Clicked");
            d.updateWires();
        }
    }
}
