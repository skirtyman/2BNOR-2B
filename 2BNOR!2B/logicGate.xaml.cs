using System;
using System.Collections.Generic;
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
    /// Interaction logic for logicGate.xaml
    /// </summary>
    public partial class LogicGate : UserControl
    {
        private Element gate;
        private double labelWidth;
        private int connectedWires = 0; 

        public LogicGate(Element gate)
        {
            InitializeComponent();
            this.gate = gate;
            SetImage();
            this.PreviewMouseDown += LogicGate_PreviewMouseDown;
            this.MouseMove += LogicGate_MouseMove;
        }

        private void LogicGate_MouseMove(object sender, MouseEventArgs e)
        {
            Mouse.SetCursor(Cursors.Hand);
        }


        public void AddWire()
        {
            connectedWires++;
        }

        public int GetConnectedWires()
        {
            return connectedWires;
        }

        private void LogicGate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (gate.GetElementName() == "input_pin")
            {
                //MessageBox.Show(gate.GetState().ToString());
                if (gate.GetState() == 1)
                {
                    gate.SetState(0);
                    //MessageBox.Show(gate.GetState().ToString());
                }
                else
                {
                    gate.SetState(1);
                    //MessageBox.Show(gate.GetState().ToString());
                }
            }

        }

        public Point GetInputPoint1()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this) + 10);
        }

        public Point GetInputPoint2()
        {
            if (gate.leftChild == null && gate.rightChild != null)
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)+20);
            }
            else
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)+30); 
            }
        }

        //Returns the point that is drawn too, for connecting the input and root node together. 
        public Point GetInputForOutput()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this) + elementImage.Height / 2 - 1);
        }

        public Point GetOutputPoint()
        {
            if (gate.GetElementName() == "input_pin")
            {
                return new Point(Canvas.GetLeft(this) + elementImage.Width + labelWidth - 43, Canvas.GetTop(this) + elementImage.Height / 2);
            }
            else
            {
                return new Point(Canvas.GetLeft(this) + elementImage.Width + labelWidth - 5, Canvas.GetTop(this) + elementImage.Height / 2);
            }
        }

        public Element GetGate()
        {
            return gate; 
        }

        private void SetImage()
        {
            BitmapImage bitmap;
            Uri path; 
            string imageName = gate.GetElementName();
            try
            {
                path = new Uri($"pack://application:,,,/Resources/{imageName}.png");
                bitmap = new BitmapImage(path);
                elementImage.Stretch = Stretch.Uniform;
                elementImage.Height = bitmap.Height / 5;
                elementImage.Width = bitmap.Width / 5;
                elementImage.Source = bitmap;
                elementLabel.Content = gate.GetLabel();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not load the image for: {imageName}", e);
            }
        }
    }
}
