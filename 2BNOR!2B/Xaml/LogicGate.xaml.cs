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
using _2BNOR_2B.Code;

namespace _2BNOR_2B
{
    /// <summary>
    /// Interaction logic for logicGate.xaml
    /// </summary>
    public partial class LogicGate : UserControl
    {
        // The element (non-visual node in the tree) that links the logic gate to a node.
        // This is the link between the visual interactions and the diagram class itself.
        private readonly Element gate;
        private readonly double labelWidth;
        private int connectedWires = 0; 

        public LogicGate(Element gate)
        {
            InitializeComponent();
            this.gate = gate;
            SetImage();
            this.PreviewMouseDown += LogicGate_PreviewMouseDown;
            this.MouseMove += LogicGate_MouseMove;
        }

        /// <summary>
        /// Changes the cursor when hovering over an input pin, this is to show that they
        /// are clickable and this creates a subtle hint to the user about diagram interactivity. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogicGate_MouseMove(object sender, MouseEventArgs e)
        {
            // Whenever the cursor is over a logic gate and that gate is an input pin
            // then change the cursor to show that the input is clickable. 
            if (gate.GetElementName() == "input_pin")
            {
                Mouse.SetCursor(Cursors.Hand);
            }
        }

        /// <summary>
        /// Used for when a logic gate will have multiple wires connected to it. 
        /// This is used when shifting the repeated input. This makes the diagrams 
        /// produced clearer as the wires do not overlap over each other. 
        /// </summary>
        public void AddWire()
        {
            connectedWires++;
        }

        public int GetConnectedWires()
        {
            return connectedWires;
        }

        /// <summary>
        /// Changes the state of an input pin when it is clicked by the user. This is so
        /// the diagram is updated when it is clicked. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LogicGate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only the state of an input can be changed by the user. 
            if (gate.GetElementName() == "input_pin")
            {
                if (gate.GetState() == 1)
                {
                    gate.SetState(0);
                }
                else
                {
                    gate.SetState(1);
                }
            }

        }

        /// <summary>
        /// Used for connecting wires to gates. 
        /// </summary>
        /// <returns>The point on the canvas where the left input is. This is for any 
        /// logic gate that is the left child of another. </returns>
        public Point GetInputPoint1()
        {
            double xPosition = Canvas.GetLeft(this);
            // Shift down as the input is not in the top left corner of the image. 
            double yPosition = Canvas.GetTop(this) + 10;
            var tmp = new Point(xPosition, yPosition);
            return tmp; 
        }

        /// <summary>
        /// Used for connecting wires to gates. 
        /// </summary>
        /// <returns>The point on the canvas where the left input is. This for any 
        /// logic gate that is the right child of another. </returns>
        public Point GetInputPoint2()
        {
            // The top left position of the logic gate.
            double xPosition = Canvas.GetLeft(this);
            double yPosition = Canvas.GetTop(this); 
            // If the gate is a NOT gate then a different shift needs to be applied
            // as the input point is in the centre of the gate.
            if (gate.leftChild == null && gate.rightChild != null)
            {
                var tmp = new Point(xPosition, yPosition + 20);
                return tmp;
            }
            else
            {
                var tmp = new Point(xPosition, yPosition + 30);
                return tmp; 
            }
        }

        /// <summary>
        /// Used for when drawing a wire from the rootnode of the tree to the output
        /// node of the diagram. 
        /// </summary>
        /// <returns>The point on the canvas where the wire from the output of the rootnode 
        /// connects the the output pin on the canvas. </returns>
        public Point GetInputForOutput()
        {
            // The top left postion of the output pin. 
            double xPosition = Canvas.GetLeft(this);
            double yPosition = Canvas.GetTop(this);
            // The output point is in the centre of the logic gate.
            yPosition += elementImage.Height / 2 - 1;
            var tmp = new Point(xPosition, yPosition);
            return tmp; 
        }

        /// <summary>
        /// Used to connect a gate to a wire. 
        /// </summary>
        /// <returns>The point on the canvas where the wire comes out of a logic gate to 
        /// connect two gates together.
        /// </returns>
        public Point GetOutputPoint()
        {
            // The intial position of the output point. 
            double xPosition = Canvas.GetLeft(this) + elementImage.Width + labelWidth;
            double yPosition = Canvas.GetTop(this) + elementImage.Height / 2;
            // If the gate is an input pin then apply a shift to x-axis to deal with the 
            // differently sized image. 
            if (gate.GetElementName() == "input_pin")
            {
                xPosition -= 43; 
                var tmp = new Point(xPosition, yPosition);
                return tmp; 
            }
            else
            {
                // Apply a different shift for the other gates. 
                xPosition -= 5;
                var tmp = new Point(xPosition, yPosition);
                return tmp; 
            }
        }

        public Element GetGate()
        {
            return gate; 
        }

        /// <summary>
        /// Sets the image for the logic gate that is displayed on the canvas. This 
        /// makes the logic gate visisble. 
        /// </summary>
        /// <exception cref="Exception">A relevant image could not be found for the 
        /// gate being searched for. </exception>
        private void SetImage()
        {
            BitmapImage bitmap;
            Uri path; 
            string imageName = gate.GetElementName();
            try
            {
                // Finding the relevant image based off of the name of the element being 
                // created. 
                path = new Uri($"pack://application:,,,/Resources/{imageName}.png");
                bitmap = new BitmapImage(path);
                // Applying a transformation to the image so that it fits nicely onto the 
                // canvas.
                elementImage.Stretch = Stretch.Uniform;
                elementImage.Height = bitmap.Height / 5;
                elementImage.Width = bitmap.Width / 5;
                elementImage.Source = bitmap;
                // Setting the label of the gate. This is simply empty for a logic gate. 
                elementLabel.Content = gate.GetLabel();
            }
            catch (Exception e)
            {
                throw new Exception($"Could not load the image for: {imageName}", e);
            }
        }
    }
}
