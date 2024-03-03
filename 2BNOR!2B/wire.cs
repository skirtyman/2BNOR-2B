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
        private bool repeated; 
        private Point inputPoint;
        private Point outputPoint; 
        private List<Point> points = new List<Point>();
        private List<Line> lines = new List<Line>();
        private Ellipse e; 
        private logicGate inputGate; 
        private Brush colour = Brushes.Red; 
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

        public void setRepeated(bool repeated)
        {
            this.repeated = repeated;
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


        //Returns list of lines depending on orientation. Or all lines if not specificed.
        public List<Line> getLines(bool? isHorizontal)
        {
            if (isHorizontal != null)
            {
                return lines.Where(l => determineOrientation(l) == isHorizontal).ToList();
            }
            else
            {
                return lines; 
            }

        }

        private bool determineOrientation(Line l)
        {
            if (l.Y1 == l.Y2)
            {
                //Horizontal line
                return true;
            }
            else
            {
                //Vertical line
                return false;
            }
        }

        public void setColour(Brush colour)
        {
            this.colour = colour;
            foreach (Line line in lines)
            {
                line.Stroke = colour;
            }

            if (repeated)
            {
                e.Fill = colour;
                e.Stroke = colour;
            }
        }

        private List<Point> calculatePoints(int shift = 0)
        {
            points.Add(inputPoint);
            //creating the first horizontal line. 
            double midpointX = ((inputPoint.X + outputPoint.X) / 2) - (shift * 10); 
            Point midpoint = new Point(midpointX, inputPoint.Y);
            points.Add(midpoint);
            midpoint.Y = outputPoint.Y;
            points.Add(midpoint);
            points.Add(outputPoint);
            return points;
        }

        private void addCircle()
        {
            e = new Ellipse();
            e.Width = 10;
            e.Height = 10;
            e.Fill = colour; 
            e.Stroke = colour;
            Canvas.SetTop(e, lines[2].Y2-5);
            Canvas.SetLeft(e, lines[2].X1-5);
            c.Children.Add(e);
        }

        //Splits the line segment, adds a curved bridge for the wire intersection. 
        public void addBridge(Line segment, Point bridgeLocation)
        {
            //segment.Stroke = Brushes.Blue; 
            e = new Ellipse();
            e.Width = 10;
            e.Height = 10;
            e.Fill = colour;
            e.Stroke = colour;
            Canvas.SetTop(e, bridgeLocation.Y);
            Canvas.SetLeft(e, bridgeLocation.X);
            c.Children.Add(e);

            //for (int i = 0; i < lines.Count; i++)
            //{
            //    if (lines[i] == segment)
            //    {
            //        //lines.RemoveAt(i);
            //        lines[i].Stroke = Brushes.Blue; 
            //    }
            //}

            //Line line = new Line();
            //lines.Add(line);
            //line.X1 = segment.X1;
            //line.Y1 = segment.Y1;
            //line.X2 = bridgeLocation.X;
            //line.Y2 = bridgeLocation.Y + 10;
            //line.Stroke = Brushes.Blue;
            //line.StrokeThickness = 2; 
            //c.Children.Add(line);






        }

        public void draw(int shift = 0, bool isRepeatWire = false)
        {
            points.Clear(); 
            points = calculatePoints(shift);
            Line l;
            for (int i = 0; i < points.Count - 1; i++)
            {
                l = new Line();
                if (i <= 2)
                {
                    lines.Add(l);  
                }
                l.StrokeThickness = 2;
                l.Stroke = colour;
                l.X1 = points[i].X;
                l.Y1 = points[i].Y;
                l.X2 = points[i + 1].X;
                l.Y2 = points[i + 1].Y;
                c.Children.Add(l);
            }
            if (isRepeatWire)
            {
                addCircle(); 
            }
        }

    }
}
