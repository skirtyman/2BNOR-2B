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

namespace _2BNOR_2B.Code
{
    public class Wire
    {
        private bool repeated;
        private Point inputPoint;
        private Point outputPoint;
        private readonly List<Point> points = new();
        private Ellipse e;
        private LogicGate inputGate;
        private Brush colour = Brushes.Red;
        private readonly Canvas c;
        private string wireString = "lll";
        private int shift = 0;
        private readonly int radiusOfArc = 15;
        private readonly int radiusOfJunction = 10;
        private readonly Path p = new();
        private readonly PathGeometry pg = new();
        private readonly PathFigureCollection pfc = new();


        public Wire(Canvas c)
        {
            this.c = c;
            p.Stroke = colour;
            p.StrokeThickness = 2;
        }

        public Wire(Point inputPoint, Point outputPoint, Canvas c)
        {
            this.inputPoint = inputPoint;
            this.outputPoint = outputPoint;
            this.c = c;
            p.Stroke = colour;
            p.StrokeThickness = 2;
        }

        public void SetRepeated(bool repeated)
        {
            this.repeated = repeated;
        }

        public void SetStart(Point inputPoint)
        {
            this.inputPoint = inputPoint;
        }

        public void SetEnd(Point outputPoint)
        {
            this.outputPoint = outputPoint;
        }

        public void SetGate(LogicGate logicGate)
        {
            inputGate = logicGate;

        }

        public void SetShift(int shift)
        {
            this.shift = shift;
        }


        public LogicGate GetGate()
        {
            return inputGate;
        }
        public List<Point> GetPoints(bool? isHorizontal)
        {
            if (isHorizontal == true)
            {
                return GetHorizontalPoints();
            }
            else if (isHorizontal == false)
            {
                return GetVerticalPoints();
            }
            else
            {
                return points;
            }
        }

        private List<Point> GetHorizontalPoints()
        {
            var p = new List<Point>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                if (points[i].Y == points[i + 1].Y)
                {
                    p.Add(points[i]);
                    p.Add(points[i + 1]);
                }
            }
            return p;
        }

        private List<Point> GetVerticalPoints()
        {
            var p = new List<Point>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                if (points[i].X == points[i + 1].X)
                {
                    p.Add(points[i]);
                    p.Add(points[i + 1]);
                }
            }
            return p;
        }

        public void SetColour(Brush colour)
        {
            this.colour = colour;
            p.Stroke = colour;

            if (repeated)
            {
                e.Fill = colour;
                e.Stroke = colour;
            }
        }

        public void SetPoints()
        {
            points.Add(inputPoint);
            double midpointX = (inputPoint.X + outputPoint.X) / 2 - shift * 10;
            var midpoint = new Point(midpointX, inputPoint.Y);
            points.Add(midpoint);
            midpoint.Y = outputPoint.Y;
            points.Add(midpoint);
            points.Add(outputPoint);
        }
        public void AddBridge(Point? bridgeLocation)
        {
            points.Insert(2, new Point(bridgeLocation.Value.X, bridgeLocation.Value.Y - radiusOfArc / 2));
            points.Insert(2, new Point(bridgeLocation.Value.X, bridgeLocation.Value.Y + radiusOfArc / 2));
            wireString = wireString.Insert(wireString.Length - 1, "bl");
        }
        public void RenderLine()
        {
            PathFigure pf;
            ArcSegment arc;
            LineSegment l;
            Size s;
            c.Children.Remove(p);
            pg.Clear();
            pfc.Clear();
            for (var i = 0; i < wireString.Length; i++)
            {
                pf = new PathFigure
                {
                    StartPoint = points[i]
                };
                if (wireString[i] == 'l')
                {
                    l = new LineSegment
                    {
                        Point = points[i + 1],
                        IsStroked = true
                    };
                    pf.Segments.Add(l);
                }
                else
                { 
                    s = new Size(radiusOfArc / 4, radiusOfArc / 4);
                    arc = new ArcSegment(points[i + 1], s, 180, true, SweepDirection.Clockwise, true);
                    pf.Segments.Add(arc);
                }
                pfc.Add(pf);
            }
            pg.Figures = pfc;
            p.Data = pg;
            p.Stroke = Brushes.Black;
            Panel.SetZIndex(p, 1);
            c.Children.Add(p);

            if (repeated)
            {
                e = new Ellipse
                {
                    Width = radiusOfJunction,
                    Height = radiusOfJunction,
                    Fill = Brushes.Black,
                    Stroke = Brushes.Black
                };
                Canvas.SetTop(e, points[points.Count - 2].Y - radiusOfJunction / 2);
                Canvas.SetLeft(e, points[points.Count - 2].X - radiusOfJunction / 2);
                Panel.SetZIndex(e, 1);
                c.Children.Add(e);
            }
        }
    }
}
