using System;
using System.Collections.Generic;
using System.Linq;
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
        //This is the element that provides the signal to the wire 
        private Point inputPoint;
        private Point outputPoint; 
        private List<Point> points = new List<Point>();
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

        private List<Point> calculatePoints()
        {
            points.Add(inputPoint);
            //creating the first horizontal line. 
            double midpointX = (inputPoint.X + outputPoint.X) / 2;
            Point midpoint = new Point(midpointX, inputPoint.Y);
            points.Add(midpoint);
            midpoint.Y = outputPoint.Y;
            points.Add(midpoint);
            points.Add(outputPoint);
            return points;
        }

        public void draw()
        {
            points.Clear(); 
            points = calculatePoints();
            Line l;
            for (int i = 0; i < points.Count - 1; i++)
            {
                l = new Line();
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
