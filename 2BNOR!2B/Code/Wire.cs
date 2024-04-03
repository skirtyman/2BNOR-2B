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
        // Defines whether or not the wire travels to a repeated input. This is to determine
        // whether or not a bridge should be drawn onto the canvas. 
        private bool repeated;
        // The input point of the wire (node lower down the tree). Given by the respective 
        // logicGate object. 
        private Point inputPoint;
        // The output point of the wire (node higher up the tree). Given by the logicGate
        // object. 
        private Point outputPoint;
        // The list of points that create the line segments of the wire. 
        private readonly List<Point> points = new();
        // The junction on the wire to show that it connects the two wires together. 
        private Ellipse junction;
        // The logic gate that gives the wire its state. 
        private LogicGate inputGate;
        // The default colour of the wire. Red => a state of 0. 
        private Brush colour = Brushes.Red;
        // The canvas in which the wire is drawn onto. 
        private readonly Canvas c;
        // asd
        private string wireString = "lll";
        // The shift value applied to a repeated input wire to separate the two wires and 
        // make them clearer. The default value is 0 as a wire is assumed to not be repeating.
        private int shift = 0;
        // Properties of the bridges and junctions to make them easily modifiable. 
        private readonly int diameterOfArc = 15;
        private readonly int radiusOfJunction = 10;
        // Path which is added to the canvas. This is what stores the created line segments on
        // the canvas. It acts like a container which makes changing the wire (such as the state)
        // simpler. 
        private readonly Path path = new();
        private readonly PathGeometry pathGeometry = new();
        private readonly PathFigureCollection pathFigureCollection = new();


        public Wire(Canvas c)
        {
            this.c = c;
            path.Stroke = colour;
            path.StrokeThickness = 2;
        }

        public Wire(Point inputPoint, Point outputPoint, Canvas c)
        {
            this.inputPoint = inputPoint;
            this.outputPoint = outputPoint;
            this.c = c;
            path.Stroke = colour;
            path.StrokeThickness = 2;
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
            // returns the gate that is the input of the wire. 
            return inputGate;
        }

        /// <summary>
        /// Utility method to get all of the points that would form a defined line
        /// segment of the wire. ie Point 1 and Point 2 would form vertical line is 
        /// isHorizontal is set to FALSE.
        /// </summary>
        /// <param name="isHorizontal">Decides which type of points to return, it is 
        /// nullable to allow all of the points to be get with one method call.</param>
        /// <returns>A list of points specified by the isHorizontal parameter which could 
        /// cause only a subset of those points to be returned. </returns>
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
                // isHorizontal is null => return all of the wires' points. 
                return points;
            }
        }

        /// <summary>
        /// Filters out points so the sequence of points only forms horizontal lines. 
        /// </summary>
        /// <returns>A list of points which represent horizontal lines.</returns>
        private List<Point> GetHorizontalPoints()
        {
            var p = new List<Point>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                // A horizontal line => the y-axis is the same for both points. 
                if (points[i].Y == points[i + 1].Y)
                {
                    p.Add(points[i]);
                    p.Add(points[i + 1]);
                }
            }
            return p;
        }

        /// <summary>
        /// Filters out points so the sequence of points only forms verical lines. 
        /// </summary>
        /// <returns>A list of points which represent vertical lines. </returns>
        private List<Point> GetVerticalPoints()
        {
            var p = new List<Point>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                // A vertical line => the x-axis is the same for both points. 
                if (points[i].X == points[i + 1].X)
                {
                    p.Add(points[i]);
                    p.Add(points[i + 1]);
                }
            }
            return p;
        }

        /// <summary>
        /// Sets the colour of the wire based off of the state of the input gate.
        /// This is to show how the states transmit around the diagram. 
        /// </summary>
        /// <param name="colour"></param>
        public void SetColour(Brush colour)
        {
            this.colour = colour;
            path.Stroke = colour;
            // Changing the colour of the junction if it exists. 
            // It only exists if the wire is a repeated one. 
            if (repeated)
            {
                junction.Fill = colour;
                junction.Stroke = colour;
            }
        }

        /// <summary>
        /// Simple method that calculates the basic shape of the wire given its input and 
        /// output points. This method does not take into account any intersections or 
        /// junctions that may need to be added. However, the a shift value is taken into
        /// account for any repeated input wires. 
        /// </summary>
        public void SetPoints()
        {
            // Adding the first horizontal line. 
            points.Add(inputPoint);
            // A basic wire assumes that the position of the vertical line is simply the 
            // midpoint between the input and output points. 
            double midpointX = (inputPoint.X + outputPoint.X) / 2 - shift * 10;
            var midpoint = new Point(midpointX, inputPoint.Y);
            // Adding the vertical line to the list of points. 
            points.Add(midpoint);
            midpoint.Y = outputPoint.Y;
            points.Add(midpoint);
            // Adding the second horizontal line. 
            points.Add(outputPoint);
        }

        /// <summary>
        /// Adds two new points into the array to take into account the split sections
        /// of the vertical line and to also mark the diameter of the bridge that will 
        /// be drawn. 
        /// </summary>
        /// <param name="bridgeLocation">The location on the canvas where a wire intersection
        /// has been found. </param>
        public void AddBridge(Point? bridgeLocation)
        {
            // Inserting the points of the bridge that connect to the straight
            // vertical sections of the wire. 
            points.Insert(2, new Point(bridgeLocation.Value.X, bridgeLocation.Value.Y - diameterOfArc / 2));
            points.Insert(2, new Point(bridgeLocation.Value.X, bridgeLocation.Value.Y + diameterOfArc / 2));
            // Inserting the bridge into the string so that it will be rendered.
            wireString = wireString.Insert(wireString.Length - 1, "bl");
        }

        public void AddJunction(Point? intersection)
        {
            // Create the junction. 
            junction = new Ellipse
            {
                Width = radiusOfJunction,
                Height = radiusOfJunction,
                Fill = Brushes.Black,
                Stroke = Brushes.Black
            };
            // Setting its position, which is always the joint between 
            // the horizontal input wire and the vertical wire. 
            Canvas.SetTop(junction, intersection.Value.Y - radiusOfJunction / 2);
            Canvas.SetLeft(junction, intersection.Value.X - radiusOfJunction / 2);
            // Setting the low ZIndex to stop overlapping and adding 
            // the junction to the canvas. 
            Panel.SetZIndex(junction, 1);
            c.Children.Add(junction);
        }

        /// <summary>
        /// Draws the wire onto the canvas, taking into account any bridges or junctions
        /// where necessary. This is from the list of points which have been calculated 
        /// previously. 
        /// </summary>
        public void RenderLine()
        {
            PathFigure pf;
            ArcSegment arc;
            LineSegment line;
            Size s;
            c.Children.Remove(path);
            pathGeometry.Clear();
            pathFigureCollection.Clear();
            // Iterating through the wire string to know what to draw. 
            for (var i = 0; i < wireString.Length; i++)
            {
                // Every pathfigure is a segment of the path and so a new object must 
                // be instantiated every time.
                pf = new PathFigure
                {
                    StartPoint = points[i]
                };
                // Draw a line segment if the token indicates a line .
                if (wireString[i] == 'l')
                {
                    line = new LineSegment
                    {
                        Point = points[i + 1],
                        IsStroked = true
                    };
                    // Adding the line to the path figure. 
                    pf.Segments.Add(line);
                }
                // Otherwise draw a bridge. 
                else
                { 
                    // Creating the arc of the wire bridge. 
                    s = new Size(diameterOfArc / 4, diameterOfArc / 4);
                    arc = new ArcSegment(points[i + 1], s, 180, true, SweepDirection.Clockwise, true);
                    // Adding it to the path figure. 
                    pf.Segments.Add(arc);
                }
                // Adding to the path figure collection to make a complete
                // section of the wire. 
                pathFigureCollection.Add(pf);
            }
            pathGeometry.Figures = pathFigureCollection;
            // setting to the path to the path figure collection
            // containing the completed wire. 
            path.Data = pathGeometry;
            path.Stroke = Brushes.Black;
            // Setting a low ZIndex to ensure that the wires are not
            // drawn over the logic gates on the canvas. 
            Panel.SetZIndex(path, 1);
            // Adding the wire to the canvas. 
            c.Children.Add(path);

            // If a wire is a repeated one, then a junction must be added 
            // to show this. 
            if (repeated && junction == null)
            {            
                AddJunction(points[points.Count - 2]);
            }
        }
    }
}
