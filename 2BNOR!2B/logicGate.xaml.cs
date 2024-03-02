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
    public partial class logicGate : UserControl
    {
        private element gate;
        private double labelWidth;
        private int connectedWires = 0; 

        public logicGate(element gate)
        {
            InitializeComponent();
            this.gate = gate;
            //connectedWires = gate.getInstances(); 
            setImage();
            this.PreviewMouseDown += LogicGate_PreviewMouseDown;
            this.MouseMove += LogicGate_MouseMove;
        }

        private void LogicGate_MouseMove(object sender, MouseEventArgs e)
        {
            Mouse.SetCursor(Cursors.Hand);
        }


        public void addWire()
        {
            connectedWires++;
        }

        public int getConnectedWires()
        {
            return connectedWires;
        }

        private void LogicGate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (gate.getElementName() == "input_pin")
            {
                if (gate.getState() == 1)
                {
                    gate.setState(0);

                }
                else
                {
                    gate.setState(1);
                }
            }

        }

        public Point getInputPoint1()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this) + 10);
        }

        public Point getInputPoint2()
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
        public Point getInputForOutput()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this) + elementImage.Height / 2 - 1);
        }

        public Point getOutputPoint()
        {
            if (gate.getElementName() == "input_pin")
            {
                return new Point(Canvas.GetLeft(this) + elementImage.Width + labelWidth - 43, Canvas.GetTop(this) + elementImage.Height / 2);
            }
            else
            {
                return new Point(Canvas.GetLeft(this) + elementImage.Width + labelWidth - 5, Canvas.GetTop(this) + elementImage.Height / 2);
            }
        }

        public element getGate()
        {
            return gate; 
        }

        private void setImage()
        {
            BitmapImage bitmap = new BitmapImage(); 
            string imageName = gate.getElementName();
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", imageName+".png");
            using (FileStream stream = new FileStream(@$"C:\Users\andreas\source\repos\2BNOR!2B - Copy\2BNOR!2B\images\{imageName}.png", FileMode.Open))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            elementImage.Stretch = Stretch.Uniform;
            elementImage.Height = bitmap.Height / 5;
            elementImage.Width = bitmap.Width / 5;
            elementImage.Source = bitmap;
            elementLabel.Content = gate.getLabel(); 
        }
    }
}
