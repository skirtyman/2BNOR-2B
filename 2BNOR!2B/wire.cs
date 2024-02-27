using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace _2BNOR_2B
{
    public class wire
    {
        private int state;
        private Point inputPoint;
        private Point outputPoint; 
        private List<Point> points = new List<Point>();
        private Line[] lines = new Line[3];
        private logicGate inputGate; 
        private Brush colour = Brushes.Black; 
        private Canvas c; 
        
        public wire(Canvas c)
        {
            this.c = c; 
        }
        
        public wire(Point inputPoint, Point outputPoint, Canvas c)
        {
            this.inputPoint = inputPoint;
            this.outputPoint = outputPoint;
            this.c = c;
        }

        public void setStart(Point inputPoint)
        {
            this.inputPoint = inputPoint;
        }

        public void setEnd(Point outputPoint)
        {
            this.outputPoint = outputPoint;
        }

        public void setGate(logicGate logicGate)
        {
            inputGate = logicGate; 
            
        }

        public logicGate getGate()
        {
            return inputGate;
        }

        public void setColour(Brush colour)
        {
            this.colour = colour;
            foreach (Line line in lines)
            {
                line.Stroke = colour;
            }
        }

        private List<Point> calculatePoints(int shift = 0)
        {
            points.Add(inputPoint);
            //creating the first horizontal line. 
            double midpointX = ((inputPoint.X + outputPoint.X) / 2) + (shift * 10); 
            Point midpoint = new Point(midpointX, inputPoint.Y);
            points.Add(midpoint);
            midpoint.Y = outputPoint.Y;
            points.Add(midpoint);
            points.Add(outputPoint);
            return points;
        }

        public void draw(int shift = 0)
        {
            //Adjust for two shift params, xshift and yshift.
            //xshift adjusts the position of the vertical line within the wire. 
            //  dependent on the number of parents nodes. (how many nodes the input connects too) 
            //yshift adjusts the position of the line connecting the child to the vertical line. 
            //  dependent on the number of parents nodes.
            // use expression to calculate spacings based off of number of times input appears? 

            points.Clear(); 
            points = calculatePoints(shift);
            Line l;
            for (int i = 0; i < points.Count - 1; i++)
            {
                l = new Line();
                if (i <= 2)
                {
                    lines[i] = l; 
                }
                l.StrokeThickness = 2;
                l.Stroke = colour;
                l.X1 = points[i].X;
                l.Y1 = points[i].Y;
                l.X2 = points[i + 1].X;
                l.Y2 = points[i + 1].Y;
                c.Children.Add(l);
            }
        }

    }
}
