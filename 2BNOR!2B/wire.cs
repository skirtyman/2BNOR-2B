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
        private Ellipse e; 
        private logicGate inputGate; 
        private Brush colour = Brushes.Red; 
        private Canvas c;
        private string wireString = "lll";
        private int shift = 0; 


        Path p = new Path();
        PathGeometry pg = new PathGeometry();
        PathFigureCollection pfc = new PathFigureCollection();


        public wire(Canvas c)
        {
            this.c = c;
            p.Stroke = this.colour;
            p.StrokeThickness = 2;
        }

        public wire(Point inputPoint, Point outputPoint, Canvas c)
        {
            this.inputPoint = inputPoint;
            this.outputPoint = outputPoint;
            this.c = c;
            p.Stroke = this.colour;
            p.StrokeThickness = 2;
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

        public void setShift(int shift)
        {
            this.shift = shift;
        }


        public logicGate getGate()
        {
            return inputGate;
        }

        //Returns list of lines depending on orientation. Or all lines if not specificed.
        public List<Point> getPoints(bool? isHorizontal)
        {
            if (isHorizontal == true)
            {
                return getHorizontalPoints();
            }
            else if (isHorizontal == false)
            {
                return getVerticalPoints();
            }
            else
            {
                return points;
            }
        }

        private List<Point> getHorizontalPoints()
        {
            List<Point> p = new List<Point>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (points[i].Y == points[i + 1].Y)
                {
                    p.Add(points[i]);
                    p.Add(points[i + 1]);
                }
            }
            return p;
        }

        private List<Point> getVerticalPoints()
        {
            List<Point> p = new List<Point>();
            for (int i = 0; i < points.Count - 1; i++)
            {
                if (points[i].X == points[i + 1].X)
                {
                    p.Add(points[i]);
                    p.Add(points[i + 1]);
                }
            }
            return p;
        }

        public void setColour(Brush colour)
        {
            this.colour = colour;
            p.Stroke = colour;

            if (repeated)
            {
                e.Fill = colour;
                e.Stroke = colour;
            }
        }

        public void setPoints()
        {
            points.Add(inputPoint);
            //creating the first horizontal line. 
            double midpointX = ((inputPoint.X + outputPoint.X) / 2) - (shift * 10); 
            Point midpoint = new Point(midpointX, inputPoint.Y);
            points.Add(midpoint);
            midpoint.Y = outputPoint.Y;
            points.Add(midpoint);
            points.Add(outputPoint);
            //if (points.Contains(inputPoint))
            //{
            //    MessageBox.Show("Shtuff");
            //}
            //else
            //{
            //    MessageBox.Show("What the fuck is actually happening :("); 
            //}
            //return points;
        }


        //Splits the line segment, adds a curved bridge for the wire intersection. 
        public void addBridge(Point? bridgeLocation)
        {
            points.Insert(wireString.Length - 1, new Point(bridgeLocation.Value.X, bridgeLocation.Value.Y - 10));
            points.Insert(wireString.Length - 1, new Point(bridgeLocation.Value.X, bridgeLocation.Value.Y + 10));
            wireString = wireString.Insert(wireString.Length - 1, "bl");
        }

        //renders the points list so that the wire is displayed to the canvas.
        //Takes the list of points and adds the relative shapes to the geometry group
        public void renderLine()
        {
            c.Children.Remove(p);
            pg.Clear();
            pfc.Clear();
            PathFigure pf;
            //int index = (repeated) ? wireString.Length + 1 : wireString.Length;
            for (int i = 0; i < wireString.Length; i++)
            {
                pf = new PathFigure();
                pf.StartPoint = points[i];
                if (wireString[i] == 'l')
                {
                    //draw a line
                    LineSegment line = new LineSegment();
                    line.Point = points[i + 1];
                    line.IsStroked = true;
                    pf.Segments.Add(line);
                }
                //else if (wireString[i] == 'b')
                else
                {
                    ArcSegment arc = new ArcSegment(points[i + 1], new Size(5, 5), 180, true, SweepDirection.Clockwise, true);
                    pf.Segments.Add(arc);
                }
                pfc.Add(pf);
            }
            pg.Figures = pfc;
            p.Data = pg;
            p.Stroke = Brushes.Green;
            c.Children.Add(p);

            if (repeated)
            {
                e = new Ellipse();
                e.Width = 10;
                e.Height = 10;
                e.Fill = Brushes.Black;
                e.Stroke = Brushes.Black;
                Canvas.SetTop(e, points[points.Count-2].Y - 5);
                Canvas.SetLeft(e, points[points.Count-2].X - 5);
                c.Children.Add(e);
            }
        }
    }
}
