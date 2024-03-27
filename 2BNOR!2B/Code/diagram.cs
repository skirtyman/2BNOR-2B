using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace _2BNOR_2B.Code
{
    /// <summary>
    /// Serves as the main class of the application. It handles most of the expression
    /// processing and drawing of diagrams/tables to their respective canvases.
    /// </summary>
    public class Diagram
    {
        // Simply used to remove whitespace from entered boolean expressions.
        private static readonly Regex r = new(@"\s+");
        private readonly char[] booleanOperators = { '+', '^', '.', '!' };
        private readonly string[] gateNames = { "or_gate", "xor_gate", "and_gate", "not_gate" };
        // Stores the input portion of a truth table. For a 2 input table => ["00", "01", etc.]
        private string[] inputMap;
        // Stores the complete truth table. For an AND gate => ["000", "010", "100", "111"]
        private string[] outputMap;
        private string[] headers;
        // The only characters allowed to be in the user entered expression. 
        private static readonly string validCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ().!^+10";
        private string infixExpression = "";
        private string minimisedExpression = "";
        // A binary string of the inputs of the diagram. Position corresponds to input. 
        // e.g. position 0 => A, position 1 => B, etc. 
        private string inputStates = "";
        // Stores the binary tree that is being drawn, traversed, etc. 
        private Element rootNode;
        // Stores the visual output of the logic gate diagram however, it does not need to
        // be considered when traversing the diagram. 
        private Element outputNode;
        private Element[] elements;
        private Element[] inputs;
        private Wire[] wires;
        private readonly Canvas c;
        // Constants used for the diagram drawing formulae. 
        // Square allocation for elements. 
        readonly int elementWidth = 2;
        readonly int xOffset = 12;
        // Number of pixels on the canvas each square takes up. 
        readonly int pixelsPerSquare = 15;
        readonly int maxNumberOfInputs = 13; 
        double canvasWidth;

        public Diagram(Canvas c)
        {
            this.c = c;
        }

        /// <returns>The binary tree representing the logic gate diagram. </returns>
        public Element GetTree()
        {
            return rootNode;
        }

        /// <summary>
        /// Removes all whitespace from an entered string. 
        /// </summary>
        /// <param name="input">The string where whitespace is being removed from. </param>
        /// <param name="replacement">The character that is replacing the whitespace.</param>
        /// <returns>The input with all whitespace replaced with a defind string. </returns>
        private static string RemoveWhitespace(string input, string replacement)
        {
            return r.Replace(input, replacement);
        }

        /// <summary>
        /// Sets the infix (user-entered) expression of the logic gate diagram. 
        /// </summary>
        /// <param name="expression">The new expression that will be stored. </param>
        public void SetExpression(string expression)
        {
            infixExpression = expression;
        }

        public string GetExpression()
        {
            return infixExpression;
        }

        public string GetMinimisedExpression()
        {
            return minimisedExpression;
        }

        /// <summary>
        /// Clears all relevant values stored in the diagram. This ensures a clean slate
        /// for when new expressions are entered and manipulated by the user. 
        /// </summary>
        public void ClearDiagram()
        {
            rootNode = null;
            outputNode = null;
            wires = null;
            outputMap = null;
            inputMap = null;
            headers = null;
            elements = null;
            inputStates = null;
            infixExpression = "";
        }

        /// <summary>
        /// Converts a user entered infix boolean expression to the postfix representation
        /// of the boolean expression. This is a modified version of the 'Shunting yard' as 
        /// given by the pseudo-code on Wikipedia. 
        /// </summary>
        /// <param name="infixExpression">An infix boolean expression. </param>
        /// <returns>The postfix boolean expression of the supplied infix expression.</returns>
        private string ConvertInfixtoPostfix(string infixExpression)
        {
            infixExpression = RemoveWhitespace(infixExpression, "");
            var operatorStack = new Stack<char>();
            string postfixExpression = "";
            int operatorPrecedence;
            foreach (char token in infixExpression)
            {
                if (char.IsLetter(token) || char.IsNumber(token))
                {
                    postfixExpression += token;
                }
                else if (booleanOperators.Contains(token))
                {
                    operatorPrecedence = Array.IndexOf(booleanOperators, token);
                    while (operatorStack.Count > 0 && operatorStack.Peek() != '(' && Array.IndexOf(booleanOperators, operatorStack.Peek()) > operatorPrecedence)
                    {
                        postfixExpression += operatorStack.Pop();
                    }
                    operatorStack.Push(token);
                }
                else if (token == '(')
                {
                    operatorStack.Push(token);
                }
                else if (token == ')')
                {
                    while (operatorStack.Peek() != '(')
                    {
                        Debug.Assert(operatorStack.Count > 0, "The stack is empty.");
                        postfixExpression += operatorStack.Pop();
                    }
                    Debug.Assert(operatorStack.Peek() == '(', "The top item is a (");
                    operatorStack.Pop();
                }
            }
            while (operatorStack.Count > 0)
            {
                Debug.Assert(operatorStack.Peek() != '(', "The top item is a (");
                postfixExpression += operatorStack.Pop();
            }
            return postfixExpression;
        }

        /// <summary>
        /// When the diagram is clicked, get the states of the inputs and map the states
        /// of the truth table to the gates within the logic gate diagram. Colour the wires
        /// to be respective of the states of the gates. 
        /// </summary>
        public void UpdateWires()
        {
            inputStates = "";
            Getinputstates(rootNode);
            AssignGateStates(rootNode);
            ColourWires();
        }

        #region Validation routines
        /// <summary>
        /// Used to validate the postfix produced by the Shunting yard. If this conversion
        /// causes an error then the original postfix must be invalid and so the expression
        /// should not be accepted by the program. 
        /// </summary>
        /// <param name="postfixExpression">A postfix boolean expression produced by the
        /// program, which is going to be validated by this method.</param>
        private static void ConvertPostfixtoInfix(string postfixExpression)
        {
            var s = new Stack<string>();
            string operand1;
            string operand2;
            // Tokenising the expression. 
            foreach (char c in postfixExpression)
            {
                // Inputs can be pushed straight to the stack. 
                if (char.IsLetter(c) || char.IsNumber(c))
                {
                    s.Push(c.ToString());
                }
                // If an operator has been found then pop the desired number of operators
                // and push the result to the stack. 
                else if (c == '!')
                {
                    operand1 = s.Pop();
                    s.Push($"{c}{operand1}");
                }
                else
                {
                    operand1 = s.Pop();
                    operand2 = s.Pop();
                    s.Push($"{operand1}{c}{operand2}");
                }
            }
        }

        /// <summary>
        /// Checks whether the user entered boolean expression contains any invalid 
        /// characters. 
        /// </summary>
        /// <param name="expression">The expression being checked.</param>
        /// <returns>Whether or not the expression contains invalid characters.</returns>
        private static bool InvalidCharacters(string expression)
        {
            // Tokenising the expression so that each character can be checked. 
            foreach (char c in expression)
            {
                if (validCharacters.Contains(c) == false)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Produces a string of only brackets from a user entered boolean expression. 
        /// This will be used within the validate brackets method to ensure that the
        /// brackets of the expression have been entered properly. 
        /// </summary>
        /// <param name="expression">A user entered boolean expression. </param>
        /// <returns>A string containing only brackets "(A.B)" => "()"</returns>
        private static string RemoveComponents(string expression)
        {
            string result = "";
            foreach (char c in expression)
            {
                if (c == '(' || c == ')')
                {
                    result += c;
                }
            }
            return result;
        }

        /// <summary>
        /// Ensures that the brackets in the user-entered boolean expression has a valid
        /// sequence of brackets. ie () => valid and (() => invalid, etc. 
        /// </summary>
        /// <param name="expression">The user entered expression that is being checked.</param>
        /// <returns>Returns a boolean showing whether or not the brackets are valid.</returns>
        private static bool ValidateBrackets(string expression)
        {
            // Getting a string of just the brackets to make the validation easier. 
            // The order of operands and operators is checked during postfix/infix check. 
            string brackets = RemoveComponents(expression);
            var s = new Stack<char>();
            // Tokenising the expression. 
            foreach (char c in brackets)
            {
                // If the token is an open bracket then the corresponding closed bracket must
                // exist and so push it onto the stack. 
                if (c == '(')
                {
                    s.Push(')');
                }
                // If no items remain on the stack and the token exists => bracket imbalance =>
                // the expression is invalid. Or pop an item and see if the close bracket
                // matches with the current token. 
                else if (s.Count == 0 || s.Pop() != c)
                {
                    return false;
                }
            }

            // If no tokens remain on the stack then all brackets have been matched and so the 
            // string is valid, in terms of its brackets. 
            if (s.Count == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Produces a boolean value showing whether or not the ASCII codes of the entered 
        /// boolean expression is sequential. ie, "A+B" => valid and "A+C" => invalid. 
        /// </summary>
        /// <param name="expression">The user entered expression being validated. </param>
        /// <returns>Whether or not the expression contains sequential inputs.</returns>
        private static bool IsSequential(string expression)
        {
            // Getting all of the inputs of the expression in array form so that they can
            // be sorted into alphabetical order. 
            char[] inputs = GetOnlyInputs(expression);
            Array.Sort(inputs);
            for (var i = 0; i < inputs.Length; i++)
            {
                // validCharacters string is written in sequential order therefore if the 
                // sorted inputs do not match this then the string is invalid otherwise, 
                // string is valid. 
                if (inputs[i] != validCharacters[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets all of the unique inputs of a supplied boolean expression. 
        /// </summary>
        /// <param name="expression">The expression being filtered.</param>
        /// <returns>An array representing the sequence of unique inputs within
        /// the user entered boolean expression. </returns>
        private static char[] GetOnlyInputs(string expression)
        {
            // Ensures that constants (0 or 1) are not considered so subtract from the 
            // number of unique inputs. 
            char[] inputs = new char[GetNumberOfInputs(expression, true) - GetNumberOfConstants(expression)];
            int i = 0;
            foreach (char c in expression)
            {
                // Ensuring only unique inputs are added to the array. 
                if (char.IsLetter(c) && inputs.Contains(c) == false)
                {
                    inputs[i] = c;
                    i++;
                }
            }
            return inputs;
        }


        /// <param name="expression">The user-entered boolean expression being validated.</param>
        /// <returns>The number of constants (0 or 1) in the entered boolean expression.</returns>
        private static int GetNumberOfConstants(string expression)
        {
            int total = 0;
            foreach (char c in expression)
            {
                if (c == '0' || c == '1')
                {
                    total++;
                }
            }
            return total;
        }


        /// <param name="expression">Postfix boolean expression being validated.</param>
        /// <returns>Whether or not that postfix expression is valid postfix.</returns>
        private static bool PostfixCheck(string expression)
        {
            // If the postfix cannot be converted to infix, the supplied postfix must be 
            // invalid so it fails validation. 
            try
            {
                ConvertPostfixtoInfix(expression);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Main Validation method, this ties together all of the checks to produce a 
        /// comprehensive validation algorithm. 
        /// </summary>
        /// <param name="expression">The user entered boolean expression from the expression
        /// dilog.</param>
        /// <param name="isTable">Whether or not the expression being validated is 
        /// for producing a truth table or not.</param>
        /// <returns>A boolean value representing whether or not the 
        /// boolean expression is a valid one. (true => valid) </returns>
        public bool IsExpressionValid(string expression, bool isTable = false)
        {
            string postfix = ConvertInfixtoPostfix(expression);
            if (IsSequential(expression) && InvalidCharacters(expression) && ValidateBrackets(expression))
            {
                if (isTable)
                {
                    if (GetNumberOfInputs(expression, true) > maxNumberOfInputs)
                    {
                        return false;
                    }
                    else
                    {
                        // Ensuring that the postfix is valid postfix. Covers expressions 
                        // such as "(A.)" which pass the other checks. 
                        return PostfixCheck(postfix);
                    }
                }
                else
                {
                    return PostfixCheck(postfix);
                }
            }
            // If the expression is of the incorrect form then discard immediately as valid
            // postfix cannot be produced from the expression. 
            else
            {
                return false;
            }
        }

        #endregion

        #region diagram drawing
        /// <summary>
        /// Iterative postorder traverasl of the binary tree representing the logic diagram.
        /// The traversal does not consider whether or not an input is visible or not. 
        /// It simply produces a string of the input states. 
        /// </summary>
        /// <param name="root"> The root of the tree. The last gate/input and also 
        /// the point where the traversal starts. </param>
        private void Getinputstates(Element root)
        {
            var s = new Stack<Element>();
            string visited = "";
            while (true)
            {
                while (root != null)
                {
                    // Push the root onto the stack and traverse to the left child. 
                    // Repeat until this cannot be done anymore. Same as traversing to the
                    // left-most gate in the tree. 
                    s.Push(root);
                    s.Push(root);
                    root = root.leftChild;
                }
                if (s.Count == 0)
                {
                    return;
                }
                root = s.Pop();
                if (s.Count != 0 && s.Peek() == root)
                {
                    root = root.rightChild;
                }
                else
                {
                    // Pick up an input state only if the input of the same label has not 
                    // been visited. This is because they will have the same input state.
                    if (root.GetElementName() == "input_pin" && visited.Contains(root.GetLabel()) == false)
                    {
                        inputStates += root.GetState();
                        visited += root.GetLabel();
                        root = null; 
                    }
                    else
                    {
                        //Used to travese to the right child. 
                        root = null;
                    }

                }
            }
        }


        /// <param name="root">The root node (startpoint) of the tree.</param>
        /// <returns>The maximum depth (height) of the supplied binary tree. </returns>
        private static int GetHeightOfTree(Element root)
        {
            // If the root does not exist then the tree does not exist and therefore the 
            // height of the tree is 0. 
            if (root == null)
            {
                return 0;
            }
            // Recurse to the next level of the tree. 
            // Results in two sums of the deepest left/right child. 
            int leftChildHeight = GetHeightOfTree(root.leftChild);
            int rightChildHeight = GetHeightOfTree(root.rightChild);
            // Need to add one so that the root is considered within the height. Otherwise 
            // return the largest value. 
            if (leftChildHeight > rightChildHeight)
            {
                return leftChildHeight + 1;
            }
            else
            {
                return rightChildHeight + 1;
            }
        }

        private Element GetInputWithSameLabel(char label)
        {
            foreach (Element e in elements)
            {
                if (e.GetLabel() == label)
                {
                    return e;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds the number of non-visual nodes that are within the binary tree. 
        /// </summary>
        /// <param name="root">The root of the tree storing the entire tree itself.</param>
        /// <returns>The number of non-visual nodes within the supplied tree. </returns>
        private static int GetNumberOfNodes(Element root)
        {
            // If the root does not exist then the tree must not exist and so the number of 
            // nodes must be zero. 
            if (root == null)
            {
                return 0;
            }
            // Adding 1 because the root node exists and then traversing the entire tree. 
            return 1 + GetNumberOfNodes(root.leftChild) + GetNumberOfNodes(root.rightChild);
        }

        /// <summary>
        /// Converts the user-entered and validated infix boolean expression and represents
        /// it as a binary tree. This is the same logic as the evaluated where in postfix
        /// the first operands are the deepest within the tree. 
        /// </summary>
        /// <param name="inputExpression">A user-entered infix boolean expression that will
        /// be represented as the binary tree. This is for drawing the logic diagram.</param>
        private void GenerateBinaryTreeFromExpression(string inputExpression)
        {
            string postfixExpression = ConvertInfixtoPostfix(inputExpression);
            inputs = new Element[GetNumberOfInputs(inputExpression, false)];
            var nodeStack = new Stack<Element>();
            elements = new Element[inputExpression.Length + 1];
            Element nodeToAdd;
            Element leftChild;
            Element rightChild;
            Element tmp;
            int elementID = 0;
            int i = 0;
            string elementName;
            string inputsAdded = "";
            foreach (char c in postfixExpression)
            {
                if (char.IsLetter(c) && char.IsUpper(c) || char.IsNumber(c))
                {
                    nodeToAdd = new Element(elementID, c);
                    inputs[i] = nodeToAdd;
                    i++;
                    if (char.IsNumber(c))
                    { 
                        nodeToAdd.SetState(c - 48);
                    }
                    if (inputsAdded.Contains(c) == false)
                    {
                        nodeToAdd.SetInstances(1);
                        inputsAdded += c;
                    }
                    else
                    {
                        tmp = inputs[c - 65];
                        tmp.AddInstance();
                    }
                }
                else if (c == '!')
                {
                    rightChild = nodeStack.Pop();
                    nodeToAdd = new Element("not_gate", elementID, null, rightChild);
                    nodeToAdd.SetInstances(1);
                    rightChild.parent = nodeToAdd;
                }
                else
                {
                    // Any logic gate is a binary operator, so pop two items and these are 
                    // the operands for the current boolean operation. 
                    rightChild = nodeStack.Pop();
                    leftChild = nodeStack.Pop();
                    elementName = gateNames[Array.IndexOf(booleanOperators, c)];
                    nodeToAdd = new Element(elementName, elementID, leftChild, rightChild);
                    nodeToAdd.SetInstances(1);
                    leftChild.parent = nodeToAdd;
                    rightChild.parent = nodeToAdd;
                }
                nodeStack.Push(nodeToAdd);
                elements[elementID] = nodeToAdd;
                elementID++;
            }
            rootNode = nodeStack.Pop();
        }
        private double CalculateNodeYposition(int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            double initialY = Math.Pow(2, heightOfTree) / Math.Pow(2, depthWithinTree) * pixelsPerSquare;
            initialY += initialY * positionWithinLayer * 2;
            if (heightOfTree > 5 && depthWithinTree < 4)
            {
                return initialY / 2;
            }
            else
            {
                return initialY;
            }
        }

        private double CalculateXposition(int depthWithinTree)
        {
            return canvasWidth - ((pixelsPerSquare - 7) * elementWidth + (pixelsPerSquare - 7) * xOffset) * depthWithinTree;
        }

        private double TranslateNode(double startX, int heightOfTree)
        {
            double maxX = CalculateXposition(heightOfTree);
            return startX - maxX + 50;
        }

        private double CalculateNodeXposition(Element node, int heightOfTree, int depthWithinTree)
        {
            double x;
            if (node.leftChild == null && node.rightChild == null)
            {
                x = CalculateXposition(heightOfTree);
            }
            else
            {
                x = CalculateXposition(depthWithinTree) - 50;
            }
            return TranslateNode(x, heightOfTree);
        }
        private static LogicGate GetWidestGate(Element root)
        {

            var tmp = new Element();
            var q = new Queue<Element>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                tmp = q.Dequeue();
                if (tmp.rightChild != null && tmp.rightChild.GetLogicGate() != null)
                {
                    q.Enqueue(tmp.rightChild);
                }
            }
            return tmp.GetLogicGate();
        }

        public Rect GetBoundsOfDiagram()
        {
            try
            {
                LogicGate l = GetWidestGate(rootNode);
                double maxX = Canvas.GetRight(outputNode.GetLogicGate()) + 75;
                double maxY = Canvas.GetBottom(l) + 25;
                return new Rect(new Size(maxX, maxY));
            }
            catch (Exception ex)
            {
                throw new Exception("Could not get bounds for diagram as it doesnt exist. ", ex);
            }
        }

        private Wire DrawWiresForLeftChildren(Element root)
        {
            var w = new Wire(c);
            LogicGate rootLogicGate = root.GetLogicGate();
            LogicGate leftchildLogicGate = root.leftChild.GetLogicGate();
            Element input;
            w.SetStart(rootLogicGate.GetInputPoint1());
            if (leftchildLogicGate != null)
            {
                w.SetRepeated(false);
                w.SetEnd(leftchildLogicGate.GetOutputPoint());
                w.SetGate(leftchildLogicGate);
                w.SetShift(leftchildLogicGate.GetConnectedWires());
                w.SetPoints();
            }
            else
            {
                w.SetRepeated(true);
                input = GetInputWithSameLabel(root.leftChild.GetLabel());
                w.SetEnd(input.GetLogicGate().GetOutputPoint());
                w.SetGate(input.GetLogicGate());
                input.GetLogicGate().AddWire();
                w.SetShift(input.GetLogicGate().GetConnectedWires());
                w.SetPoints();
            }
            return w;
        }

        private Wire DrawWiresForRightChildren(Element root)
        {
            var w = new Wire(c);
            LogicGate rootLogicGate = root.GetLogicGate();
            LogicGate rightchildLogicGate = root.rightChild.GetLogicGate();
            Element input;
            w.SetStart(rootLogicGate.GetInputPoint2());
            if (rightchildLogicGate != null)
            {
                w.SetRepeated(false);
                w.SetEnd(rightchildLogicGate.GetOutputPoint());
                w.SetGate(rightchildLogicGate);
                w.SetShift(rightchildLogicGate.GetConnectedWires());
                w.SetPoints();
            }
            else
            {
                w.SetRepeated(true);
                input = GetInputWithSameLabel(root.rightChild.GetLabel());
                w.SetEnd(input.GetLogicGate().GetOutputPoint());
                w.SetGate(input.GetLogicGate());
                input.GetLogicGate().AddWire();
                w.SetShift(input.GetLogicGate().GetConnectedWires());
                w.SetPoints();
            }
            return w;
        }
        private static Point? FindIntersection(Point p0, Point p1, Point p2, Point p3)
        {
            double s_x = p1.X - p0.X;
            double s_y = p1.Y - p0.Y;
            double s2_x = p3.X - p2.X;
            double s2_y = p3.Y - p2.Y;
            double denom = s_x * s2_y - s2_x * s_y;

            if (denom == 0)
            {
                return null;
            }
            bool isDenomPositive = denom > 0;

            double s3_x = p0.X - p2.X;
            double s3_y = p0.Y - p2.Y;
            double s_numer = s_x * s3_y - s_y * s3_x;


            if (s_numer < 0 == isDenomPositive)
            {
                return null;
            }

            double t_numer = s2_x * s3_y - s_x * s3_y;

            if (t_numer < 0 == isDenomPositive)
            {
                return null;
            }

            if (s_numer > denom == isDenomPositive || t_numer > denom == isDenomPositive)
            {
                return null;
            }

            double t = t_numer / denom;
            Point? result = new Point(p0.X + t * s_x, p0.Y + t * s_y);
            return result;
        }
        private void DrawIntersections()
        {
            var horizontalLines = new List<Point>();
            var verticalLines = new List<Point>();
            Point? intersection;
            Wire tmp;
            for (var j = 0; j < wires.Length - 1; j++)
            {
                horizontalLines.AddRange(wires[j].GetPoints(true));
                verticalLines.AddRange(wires[j].GetPoints(false));
            }


            for (var i = 0; i < verticalLines.Count - 1; i += 2)
            {
                for (var c = 0; c < horizontalLines.Count - 1; c += 2)
                {
                    intersection = FindIntersection(verticalLines[i], verticalLines[i + 1], horizontalLines[c], horizontalLines[c + 1]);
                    if (intersection != null && FindWire(verticalLines[i]) != FindWire(horizontalLines[c]))
                    {
                        tmp = FindWire(verticalLines[i]);
                        tmp.AddBridge(intersection);
                    }
                }
            }
        }

        private Wire FindWire(Point p)
        {
            List<Point> points;
            foreach (Wire w in wires)
            {
                points = w.GetPoints(null);
                if (points.Contains(p))
                {
                    return w;
                }
            }
            throw new Exception("Could not find wire.");
        }

        private void DrawWires(Element root)
        {
            var q = new Queue<Element>();
            _ = new Queue<Wire>();
            wires = new Wire[GetNumberOfNodes(root)];
            Element tmp;
            int i = 0;
            q.Enqueue(root);
            while (q.Count != 0)
            {
                tmp = q.Dequeue();
                if (tmp.leftChild != null)
                {
                    wires[i] = DrawWiresForLeftChildren(tmp);
                    i++;
                    q.Enqueue(tmp.leftChild);
                }

                if (tmp.rightChild != null)
                {
                    wires[i] = DrawWiresForRightChildren(tmp);
                    i++;
                    q.Enqueue(tmp.rightChild);
                }
            }
            DrawIntersections();
            for (var j = 0; j < wires.Length - 1; j++)
            {
                wires[j].RenderLine();
            }
        }
        private void ColourWires()
        {
            foreach (Wire w in wires)
            {
                LogicGate l = w.GetGate();
                Element node = l.GetGate();
                if (node.GetState() == 1)
                {
                    w.SetColour(Brushes.Green);
                }
                else
                {
                    w.SetColour(Brushes.Red);
                }
            }
        }
        private void DrawNode(Element currentNode, int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            LogicGate logicGate;
            double x, y;
            if (currentNode.GetInstances() != 0)
            {
                x = CalculateNodeXposition(currentNode, heightOfTree, depthWithinTree);
                if (currentNode.parent != null && currentNode.parent.GetElementName() == "not_gate" && currentNode.GetElementName() == "input_pin")
                {
                    y = Canvas.GetTop(currentNode.parent.GetLogicGate());
                }
                else
                {
                    y = CalculateNodeYposition(heightOfTree, depthWithinTree, positionWithinLayer);
                }
                logicGate = new LogicGate(currentNode);
                currentNode.SetLogicGate(logicGate);
                Canvas.SetLeft(logicGate, x);
                Canvas.SetTop(logicGate, y);
                c.Height = Math.Max(y, c.Height) + 30;
                c.Width = Math.Max(x, c.Width) + 30;
                double p = logicGate.GetInputPoint2().Y + 50;
                Canvas.SetBottom(logicGate, p);
                Canvas.SetRight(logicGate, logicGate.GetInputPoint2().X);
                Panel.SetZIndex(logicGate, 3);
                c.Children.Add(logicGate);
            }
        }
        private void DrawNodes(Element root, int heightOfTree)
        {
            var q = new Queue<Element>();
            q.Enqueue(root);
            int depthWithinTree = 0;
            int positionWithinLayer = 0;
            int sizeOfQ;
            Element currentNode;
            while (q.Count != 0)
            {
                sizeOfQ = q.Count;
                while (sizeOfQ != 0)
                {
                    currentNode = q.Peek();
                    DrawNode(currentNode, heightOfTree, depthWithinTree, positionWithinLayer);
                    q.Dequeue();
                    positionWithinLayer++;
                    if (currentNode.leftChild != null)
                    {
                        q.Enqueue(currentNode.leftChild);
                    }

                    if (currentNode.rightChild != null)
                    {
                        q.Enqueue(currentNode.rightChild);
                    }
                    sizeOfQ--;
                }
                depthWithinTree++;
                positionWithinLayer = 0;
            }
        }
        private void AssignGateStates(Element root)
        {
            string tableRow = GetTruthTableRow();
            var s = new Stack<Element>();
            int i = 0;
            while (true)
            {
                while (root != null)
                {
                    s.Push(root);
                    s.Push(root);
                    root = root.leftChild;
                }
                if (s.Count == 0)
                {
                    return;
                }
                root = s.Pop();
                if (s.Count != 0 && s.Peek() == root)
                {
                    root = root.rightChild;
                }
                else
                {
                    int state = tableRow[i] - 48;
                    root.SetState(state);
                    i++;
                    root = null;
                }
            }
        }
        private void DrawOutputWire()
        {
            var w = new Wire(c);
            w.SetStart(outputNode.GetLogicGate().GetInputForOutput());
            w.SetEnd(rootNode.GetLogicGate().GetOutputPoint());
            w.SetGate(rootNode.GetLogicGate());
            w.SetPoints();
            w.RenderLine();
            wires[^1] = w;
        }
        private void DrawOutput(int heightOfTree)
        {
            outputNode = new Element(-1);
            var logicGate = new LogicGate(outputNode);
            outputNode.SetLogicGate(logicGate);
            double x = TranslateNode(CalculateXposition(0), heightOfTree) + pixelsPerSquare * 8;
            double y = CalculateNodeYposition(heightOfTree, 0, 0);
            Canvas.SetTop(logicGate, y);
            Canvas.SetLeft(logicGate, x);
            Canvas.SetRight(logicGate, x + logicGate.ActualWidth);
            c.Children.Add(logicGate);
        }
        public void DrawDiagram()
        {
            canvasWidth = c.ActualWidth;
            GenerateBinaryTreeFromExpression(infixExpression);
            inputMap = GenerateInputMap(infixExpression, true);
            headers = GetHeaders(infixExpression, true);
            outputMap = GenerateOutputMap(infixExpression, headers, true);
            //outputMap = outputMap.Distinct().ToArray();
            int heightOfTree = GetHeightOfTree(rootNode);
            DrawNodes(rootNode, heightOfTree);
            DrawWires(rootNode);
            DrawOutput(heightOfTree);
            DrawOutputWire();
            UpdateWires();
        }

        #endregion

        #region Truth table generation
        private int EvaluateBooleanExpression(string binaryCombination, string inputExpression)
        {
            string postfix = ConvertInfixtoPostfix(inputExpression);
            int operand1;
            int operand2;
            int tmp;
            string sub = SubsituteIntoExpression(binaryCombination, postfix);
            var evaluatedStack = new Stack<int>();
            foreach (char c in sub)
            {
                if (char.IsNumber(c))
                {
                    evaluatedStack.Push(c);
                }
                else if (c == '!')
                {
                    operand1 = evaluatedStack.Pop();
                    evaluatedStack.Push(operand1 ^ 1);
                }
                else if (c == '^')
                {
                    operand1 = evaluatedStack.Pop();
                    operand2 = evaluatedStack.Pop();
                    tmp = EvaluateSingleOperator(operand1, operand2, c);
                    evaluatedStack.Push(tmp + 48);
                }
                else
                {
                    operand1 = evaluatedStack.Pop();
                    operand2 = evaluatedStack.Pop();
                    tmp = EvaluateSingleOperator(operand1, operand2, c);
                    evaluatedStack.Push(tmp);
                }
            }
            return evaluatedStack.Pop();
        }
        private static int EvaluateSingleOperator(int o1, int o2, char operation)
        {
            int result = 0;
            if (operation == '.')
            {
                result = o1 & o2;
            }
            else if (operation == '+')
            {
                result = o1 | o2;
            }
            else if (operation == '^')
            {
                result = o1 ^ o2;
            }
            return result;
        }

        private static string SubsituteIntoExpression(string binaryCombination, string inputExpression)
        {
            string binaryDigit;
            foreach (char c in inputExpression)
            {
                if (char.IsLetter(c))
                {
                    binaryDigit = binaryCombination[c - 65].ToString();
                    inputExpression = inputExpression.Replace(c.ToString(), binaryDigit);
                }
            }
            return inputExpression;
        }
        private static string ConvertIntintoBinaryString(int n, string booleanExpression, bool isUnique)
        {
            int numInp = GetNumberOfInputs(booleanExpression, isUnique);
            int rem;
            string bin = ""; 
            while (n > 0)
            {
                rem = n % 2;
                n /= 2;
                bin = rem.ToString() + bin; 
            }
            return bin.PadLeft(numInp, '0');
        }
        private string GetTruthTableRow()
        {
            return outputMap[Array.IndexOf(inputMap, inputStates)];
        }
        private static int GetNumberOfInputs(string booleanExpression, bool isUnique)
        {
            int numberOfInputs = 0;
            string alreadyCounted = "";
            foreach (char token in booleanExpression)
            {
                if (char.IsLetter(token) || char.IsNumber(token))
                {
                    if (isUnique)
                    {
                        if (!alreadyCounted.Contains(token))
                        {
                            alreadyCounted += token;
                            numberOfInputs++;
                        }
                    }
                    else
                    {
                        numberOfInputs++;
                    }
                }
            }
            return numberOfInputs;
        }
        private static int GetNumberOfOperators(string booleanExpression)
        {
            char[] booleanOperators = { '.', '^', '+', '!' };
            int numberOfOperators = 0;
            foreach (char token in booleanExpression)
            {
                if (booleanOperators.Contains(token) || token == '@' || token == '#')
                {
                    numberOfOperators++;
                }
            }
            return numberOfOperators;
        }

        private static string[] GenerateInputMap(string inputExpression, bool isUnique)
        {
            int numberOfInputs = GetNumberOfInputs(inputExpression, isUnique);
            int numberOfRows = (int)Math.Pow(2, numberOfInputs);
            string[] inputMap = new string[numberOfRows];
            for (var i = 0; i < numberOfRows; i++)
            {
                inputMap[i] = ConvertIntintoBinaryString(i, inputExpression, isUnique);
            }
            return inputMap;
        }

        private string[] GenerateOutputMap(string inputExpression, string[] headers, bool isUnique)
        {
            string[] inputMap = GenerateInputMap(inputExpression, isUnique);
            int numberOfRows = (int)Math.Pow(2, GetNumberOfInputs(inputExpression, isUnique));
            string inputCombination;
            string[] outputMap = new string[inputMap.Length];
            for (var i = 0; i < numberOfRows; i++)
            {
                inputCombination = inputMap[i];
                outputMap[i] += GetOutputRow(headers, inputCombination);
            }
            return outputMap;
        }

        private string GetOutputRow(string[] headers, string inputCombination)
        {
            string outputRow = "";
            foreach (string header in headers)
            {
                outputRow += EvaluateBooleanExpression(inputCombination, header) - 48;
            }
            return outputRow;
        }
        private string[] GetHeaders(string inputExpression, bool isDisplay)
        {
            string postfix = ConvertInfixtoPostfix(inputExpression);
            string[] headers;
            int numberOfInputs = GetNumberOfInputs(postfix, false);
            int numberOfOperators = GetNumberOfOperators(postfix);
            if (isDisplay)
            {
                headers = GenerateDisplayOperatorHeaders(postfix, numberOfInputs, numberOfOperators);
            }
            else
            {
                headers = GeneratePostOrderHeaders(postfix, numberOfInputs, numberOfOperators);
            }
            return headers;
        }
        private static string[] GenerateDisplayOperatorHeaders(string inputExpression, int numberOfInputs, int numberOfOperators)
        {
            string[] postorderHeaders = GeneratePostOrderHeaders(inputExpression, numberOfInputs, numberOfOperators);
            Array.Sort(postorderHeaders);
            Array.Sort(postorderHeaders, (x, y) => x.Length.CompareTo(y.Length));
            postorderHeaders = postorderHeaders.Distinct().ToArray();
            return postorderHeaders;
        }

        private static string[] GeneratePostOrderHeaders(string postfix, int numberOfInputs, int numberOfOperators)
        {
            var subExpressionStack = new Stack<string>();
            string[] headers = new string[numberOfOperators + numberOfInputs];
            string subexpression;
            string operand1;
            string operand2;
            int i = 0;
            foreach (char c in postfix)
            {
                if (char.IsLetter(c) || char.IsNumber(c))
                {
                    subExpressionStack.Push(c.ToString());
                    headers[i] = c.ToString();
                    i++;
                }
                else
                {
                    if (c == '!')
                    {
                        operand1 = subExpressionStack.Pop();
                        subexpression = $"({c}{operand1})";
                    }
                    else
                    {
                        operand1 = subExpressionStack.Pop();
                        operand2 = subExpressionStack.Pop();
                        subexpression = $"({operand2}{c}{operand1})";
                    }
                    subExpressionStack.Push(subexpression);
                    headers[i] = subexpression;
                    i++;
                }
            }
            return headers;
        }

        private static double CalculateCellWidth(string header)
        {
            double cellWidth = 30;
            if (header.Length != 1)
            {
                if (header.Length > 5)
                {
                    cellWidth = header.Length * 11 + 15;
                }
                else
                {
                    cellWidth = header.Length * 10 + 15;
                }
            }
            return cellWidth;
        }

        private static void DrawTruthTableHeaders(Canvas c, string[] headers)
        {
            Label cell;
            var border = new Thickness(2);
            var font = new FontFamily("Consolas");
            double cellWidth;
            double x = 20;
            foreach (string header in headers)
            {
                cellWidth = CalculateCellWidth(header);
                cell = new Label
                {
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Width = cellWidth,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = border,
                    Background = Brushes.White,
                    FontFamily = font,
                    FontSize = 18,
                    Content = header
                };
                Canvas.SetTop(cell, 20);
                Canvas.SetLeft(cell, x);
                c.Children.Add(cell);
                x += cellWidth;
            }
            c.Width = Math.Max(x, c.Width) + 30;
        }

        private static void DrawTruthTableBody(Canvas c, string[] headers, string[] outputMap)
        {
            Label cell;
            var border = new Thickness(2);
            var font = new FontFamily("Consolas");
            double cellWidth;
            double x = 20;
            double y = 50;
            foreach (string row in outputMap)
            {
                for (var i = 0; i < headers.Length; i++)
                {
                    cellWidth = CalculateCellWidth(headers[i]);
                    cell = new Label
                    {
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Width = cellWidth,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = border,
                        Background = Brushes.White,
                        FontFamily = font,
                        FontSize = 18,
                        Content = row[i]
                    };
                    Canvas.SetTop(cell, y);
                    Canvas.SetLeft(cell, x);
                    c.Children.Add(cell);
                    x += cellWidth;
                }
                x = 20;
                y += 30;
            }
            c.Height = Math.Max(y, c.Height) + 30;
        }

        private static string[] TrimBrackets(string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                if (headers[i].Length != 1)
                {
                    headers[i] = headers[i][1..^1];
                }
            }
            return headers;
        }
        public void DrawTruthTable(Canvas c, string inputExpression)
        {
            c.Children.Clear();
            headers = GetHeaders(inputExpression, true);
            outputMap = GenerateOutputMap(inputExpression, headers, true);
            headers = TrimBrackets(headers);
            DrawTruthTableHeaders(c, headers);
            DrawTruthTableBody(c, headers, outputMap);
        }
        #endregion

        #region Minimisation
        private string ConvertEPIsToExpression(List<string> essentialPrimeImplicants)
        {
            essentialPrimeImplicants = essentialPrimeImplicants.ConvertAll(new Converter<string, string>(ConvertImplicantToExpression));
            string expression = string.Join("+", essentialPrimeImplicants);
            return expression;
        }
        private string ConvertImplicantToExpression(string epi)
        {
            string tmp = "";
            char input;
            for (var i = 0; i < epi.Length; i++)
            {
                input = (char)(i + 65);
                if (epi[i] == '1')
                {
                    tmp += input;
                }
                else if (epi[i] == '0')
                {
                    tmp += $"!{input}";
                }

                if (epi.Length == 2)
                {
                    if (i == 0 && epi[i] != '-')
                    {
                        tmp += ".";
                    }
                }
                else
                {
                    if (i != 0 && i < epi.Length - 1 && epi[i] != '-')
                    {
                        tmp += ".";
                    }
                }
            }
            return $"({tmp})";
        }
        private static void SetRegexPatterns(Dictionary<string, string> regex, List<string> minterms)
        {
            Match res;
            foreach (string regexPattern in regex.Keys.ToList())
            {
                foreach (string minterm in minterms)
                {
                    res = Regex.Match(minterm, regexPattern);
                    if (res.Success)
                    {
                        regex[regexPattern] += "1";
                    }
                    else
                    {
                        regex[regexPattern] += "0";
                    }
                }
            }
        }
        private static void ConvertImplicantsIntoRegex(Dictionary<string, string> regex, List<string> primeImplicants)
        {
            string tmp = "";
            string value = "";
            foreach (string primeImplicant in primeImplicants)
            {
                foreach (char c in primeImplicant)
                {
                    if (c == '-')
                    {
                        tmp += @"\d";
                    }
                    else
                    {
                        tmp += c;
                    }
                }
                regex.Add(tmp, value);
                tmp = "";
            }
        }
        private static string MergeMinterms(string m1, string m2)
        {
            string mergedMinterm = "";
            if (m1.Length != m2.Length)
            {
                throw new Exception("Incorrect length");
            }
            else
            {
                for (var i = 0; i < m1.Length; i++)
                {
                    if (m1[i] != m2[i])
                    {
                        mergedMinterm += '-';
                    }
                    else
                    {
                        mergedMinterm += m1[i];
                    }
                }
                return mergedMinterm;
            }
        }
        private static bool CheckDashesAlign(string m1, string m2)
        {
            if (m1.Length != m2.Length)
            {
                throw new Exception("Incorrect length");
            }
            else
            {
                for (var i = 0; i < m1.Length; i++)
                {
                    if (m1[i] != '-' && m2[i] == '-')
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        private static bool CheckMintermDifference(string m1, string m2)
        {
            int minterm1 = RemoveDashes(m1);
            int minterm2 = RemoveDashes(m2);
            int res = minterm1 ^ minterm2;
            return res != 0 && (res & res - 1) == 0;
        }
        private static int RemoveDashes(string minterm)
        {
            return Convert.ToInt32(minterm.Replace('-', '0'), 2);
        }

        private List<string> GetMinterms(string expression)
        {
            var minterms = new List<string>();
            inputMap = GenerateInputMap(expression, true);
            foreach (string input in inputMap)
            {
                int result = EvaluateBooleanExpression(input, expression) - 48;
                if (result == 1)
                {
                    minterms.Add(input);
                }
            }
            return minterms;
        }
        private static int[] GetFrequencyTable(Dictionary<string, string> regex, List<string> minterms)
        {
            int[] sums = new int[minterms.Count];
            foreach (string s in regex.Values.ToList())
            {
                for (var i = 0; i < s.Length; i++)
                {
                    if (s[i] == '1')
                    {
                        sums[i]++;
                    }
                }
            }
            return sums;
        }
        private static string GetEssentialPrimeImplicant(Dictionary<string, string> regex, int pos)
        {
            string[] essentialPrimes = regex.Values.ToArray();
            string[] keys = regex.Keys.ToArray();
            string prime;
            for (var i = 0; i < essentialPrimes.Length; i++)
            {
                prime = essentialPrimes[i];
                if (prime[pos] == '1')
                {
                    return keys[i];
                }
            }
            throw new Exception("Item could be found");
        }

        private static List<string> GetEssentialPrimeImplicants(Dictionary<string, string> regex, List<string> minterms)
        {
            int[] bitFrequencyTable = GetFrequencyTable(regex, minterms);
            var essentialPrimeImplicants = new List<string>();
            string epi;
            for (var i = 0; i < bitFrequencyTable.Length; i++)
            {
                if (bitFrequencyTable[i] == 1)
                {
                    epi = GetEssentialPrimeImplicant(regex, i);
                    if (!essentialPrimeImplicants.Contains(epi))
                    {
                        essentialPrimeImplicants.Add(epi);
                    }
                }
            }
            return essentialPrimeImplicants;
        }
        private static List<string> GetPrimeImplicants(List<string> mintermList)
        {
            var primeImplicants = new List<string>();
            bool[] merges = new bool[mintermList.Count];
            int numberOfMerges = 0;
            string mergedMinterm;
            string m1;
            string m2;
            for (var i = 0; i < mintermList.Count; i++)
            {
                for (var c = i + 1; c < mintermList.Count; c++)
                {
                    m1 = mintermList[i];
                    m2 = mintermList[c];
                    if (CheckDashesAlign(m1, m2) && CheckMintermDifference(m1, m2))
                    {
                        mergedMinterm = MergeMinterms(m1, m2);
                        primeImplicants.Add(mergedMinterm);
                        numberOfMerges++;
                        merges[i] = true;
                        merges[c] = true;
                    }
                }
            }
            for (var j = 0; j < mintermList.Count; j++)
            {
                if (!merges[j] && !primeImplicants.Contains(mintermList[j]))
                {
                    primeImplicants.Add(mintermList[j]);
                }
            }
            if (numberOfMerges == 0)
            {
                return primeImplicants;
            }
            else
            {
                return GetPrimeImplicants(primeImplicants);
            }
        }
        public void MinimiseExpression(string expression)
        {
            List<string> minterms = GetMinterms(expression);
            List<string> primeImplicants = GetPrimeImplicants(minterms);
            var PIchart = new Dictionary<string, string>();
            ConvertImplicantsIntoRegex(PIchart, primeImplicants);
            SetRegexPatterns(PIchart, minterms);
            PIchart = ReplaceDashesFromRegex(PIchart);
            List<string> PIs = GetEssentialPrimeImplicants(PIchart, minterms);
            string covered = GetCoveredString(PIs, PIchart);
            if (covered.Contains('0'))
            {
                minimisedExpression = DoPetriksMethod(PIchart, PIs, primeImplicants, minterms);
            }
            else
            {
                minimisedExpression = ConvertEPIsToExpression(PIs);
            }
        }
        #endregion

        private static Dictionary<string, string> ReplaceDashesFromRegex(Dictionary<string, string> PIChart)
        {
            var newKeys = new Dictionary<string, string>();
            string tmp;
            foreach (string k in PIChart.Keys)
            {
                tmp = k;
                tmp = tmp.Replace(@"\d", "-");
                newKeys.Add(tmp, PIChart[k]);
            }
            return newKeys;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="epis"></param>
        /// <param name="PIchart"></param>
        /// <returns></returns>
        private static string GetCoveredString(List<string> epis, Dictionary<string, string> PIchart)
        {
            int result = 0;
            foreach (string s in epis)
            {
                result |= Convert.ToInt32(PIchart[s], 2);
            }
            return result.ToString();
        }

        private static Dictionary<string, string> RemoveEPIs(Dictionary<string, string> PIchart, List<string> epis, List<string> minterms)
        {
            string value;
            int pos;
            int[] freq = GetFrequencyTable(PIchart, minterms);
            foreach (string k in PIchart.Keys)
            {
                if (epis.Contains(k))
                {
                    value = PIchart[k];
                    pos = GetSignificantBit(k, freq);
                    TrimMinterm(PIchart, pos);
                    PIchart.Remove(k);
                }
            }
            return PIchart;
        }

        private static Dictionary<string, string> TrimMinterm(Dictionary<string, string> PIchart, int pos)
        {
            string value;
            foreach (string s in PIchart.Keys)
            {
                value = PIchart[s];
                value = value.Remove(pos, 1);
                PIchart[s] = value;
            }
            return PIchart;
        }

        private static int GetSignificantBit(string epi, int[] freq)
        {
            for (var i = 0; i < freq.Length; i++)
            {
                if (freq[i] == 1 && epi[i] == '1')
                {
                    return i;
                }
            }
            return -1;
        }
        private string DoPetriksMethod(Dictionary<string, string> PIchart, List<string> epis, List<string> primeImplicants, List<string> minterms)
        {
            PIchart = RemoveEPIs(PIchart, epis, minterms);
            string minimisedExpression;
            Dictionary<char, string> termsImplicantMapping = MapTermsToImplicants(primeImplicants);
            List<Bracket> productOfSums = GetProductOfSums(termsImplicantMapping, PIchart);
            string[] sumOfproducts = GetSumOfProducts(productOfSums);
            string minProduct = GetMinProduct(sumOfproducts);
            minimisedExpression = GetFinalExpression(termsImplicantMapping, minProduct);
            return minimisedExpression;
        }

        private static Dictionary<char, string> MapTermsToImplicants(List<string> primeImplicants)
        {
            var mapping = new Dictionary<char, string>();
            char minChar = (char)(primeImplicants[0].Length + 65);
            for (var i = 0; i < primeImplicants.Count; i++)
            {
                mapping.Add(minChar, primeImplicants[i]);
                minChar++;
            }
            return mapping;

        }

        private static List<Bracket> GetProductOfSums(Dictionary<char, string> termToImplicantMap, Dictionary<string, string> primeImplicantChart)
        {
            var productOfSums = new List<Bracket>();
            List<Bracket> sumsToAdd;
            string primeImplicant;
            foreach (string key in primeImplicantChart.Keys)
            {
                primeImplicant = primeImplicantChart[key];
                for (var i = 0; i < primeImplicant.Length; i++)
                {
                    if (primeImplicant[i] == '1')
                    {
                        sumsToAdd = GetSumsToAdd(primeImplicantChart, termToImplicantMap, key, i);
                        AddSumsToList(productOfSums, sumsToAdd);
                    }
                }
            }
            return productOfSums;
        }

        private static void AddSumsToList(List<Bracket> productOfSums, List<Bracket> sumsToAdd)
        {
            Bracket reverse;
            foreach (Bracket s in sumsToAdd)
            {
                reverse = s;
                if (productOfSums.Contains(s) == false)
                {
                    productOfSums.Add(s);
                }
            }
        }

        private static List<Bracket> GetSumsToAdd(Dictionary<string, string> PIchart, Dictionary<char, string> termToImplicantMap, string key, int positionWithinKey)
        {
            var sumsToAdd = new List<Bracket>();
            Bracket sum;
            string k;
            char term1;
            char term2;
            for (var i = 0; i < PIchart.Keys.Count; i++)
            {
                k = PIchart.Keys.ToArray()[i];
                if (PIchart[k][positionWithinKey] == '1')
                {
                    term1 = GetTermFromImplicant(termToImplicantMap, key);
                    term2 = GetTermFromImplicant(termToImplicantMap, k);
                    if (term1 != term2)
                    {
                        sum = new Bracket(term1, term2);
                        sumsToAdd.Add(sum);
                    }
                }
            }
            return sumsToAdd;
        }

        private static char GetTermFromImplicant(Dictionary<char, string> termToImplicantMap, string implicant)
        {
            string[] values = termToImplicantMap.Values.ToArray();
            char[] keys = termToImplicantMap.Keys.ToArray();
            for (var i = 0; i < termToImplicantMap.Values.Count; i++)
            {
                if (values[i] == implicant)
                {
                    return keys[i];
                }
            }
            throw new Exception("Could not map implicant to key");
        }

        /// <summary>
        /// Converts the product of sums (A+B)(C+D)... into the sum of products of the boolean
        /// expression. sum of products = AB + CD + etc. This allows the program to find the
        /// minimal term(solution) which can ultimately give the minimised expression. 
        /// </summary>
        /// <param name="productOfSums">The product of sums found in the prime implicant 
        /// chart. </param>
        /// <returns> The sum of products of the expression which is used to minimise the input</returns>
        private static string[] GetSumOfProducts(List<Bracket> productOfSums)
        {
            _ = new List<Bracket>();
            Bracket b1;
            Bracket b2;
            Bracket mergedTerm;
            bool merged = true;
            // Keep trying to merge the brackets together until no more merges can be made. 
            // This means that the distributive law can be applied to the brackets at that point.
            while (merged)
            {
                merged = false;
                // Checking each sum to ensure the most merges take place. 
                for (var i = 0; i < productOfSums.Count - 1; i++)
                {
                    // Start c at i + 1 because the order of brackets doe not matter, so
                    // simply search through the remaining brackets. 
                    for (var c = i + 1; c < productOfSums.Count; c++)
                    {
                        b1 = productOfSums[i];
                        b2 = productOfSums[c];
                        // Ensuring that the brackets are not the same. Must also consider if
                        // the terms are the same but in the opposite order.
                        if (b1.term1 == b2.term1 != (b1.term2 == b2.term2) != (b1.term1 == b2.term2) != (b1.term2 == b2.term1))
                        {
                            // A make the merge as it is a valid one and remove the brackets
                            // that result in the merge. This is to stop the same merges 
                            // occurring again and again. 
                            mergedTerm = MergeBrackets(b1, b2);
                            productOfSums.Add(mergedTerm);
                            productOfSums.Remove(b1);
                            productOfSums.Remove(b2);
                            merged = true;
                            i = c + 1;
                            c = i + 1;
                        }
                    }
                }
            }
            // Convert the merged product of sums into a string, this allows each bracket
            // to have more than two products within it. 
            List<List<string>> param = ConvertBracketsToString(productOfSums);
            // Recursively apply the distributive law which does the rest of the work. 
            List<List<string>> sumOfProducts = RecursiveDistributiveLaw(param);
            // The first term within the 2D list is the only bracket that remains and hence
            // can no longer be merged. It also contains all of the solutions to the minimsation.
            return sumOfProducts[0].ToArray();
        }

        /// <summary>
        /// Carries out the first merge when creating the sum of products. This converts
        /// the product of sums found previously into the correct form for the 
        /// recursive application of the distributive law to find the sum of products.
        /// </summary>
        /// <param name="b1">One set of terms being merged</param>
        /// <param name="b2">The other set of terms being merged</param>
        /// <returns></returns>
        private static Bracket MergeBrackets(Bracket b1, Bracket b2)
        {
            // Create new bracket which stores the merged terms. 
            var b = new Bracket();
            // When using the distributive law the term that is the same always remains
            // the first term of the result. The second is the AND of the remaining terms.
            if (b1.term1 == b2.term1)
            {
                b.term1 = b1.term1;
                b.term2 = b1.term2 + b2.term2;
                return b;
            }
            // The second term of the first bracket must match b2.term1 as they are in 
            // alphabetical order. 
            else
            {
                b.term1 = b1.term2;
                b.term2 = b1.term1 + b2.term2;
                return b;
            }
        }

        /// <summary>
        /// Converts the bracket representation into a string representation. This is because
        /// at this stage of the working, it is possible to have a bracket with more than two
        /// terms such as (AB + CD + EF). The first dimension is the bracket and the second
        /// dimension are the terms themselves. 
        /// </summary>
        /// <param name="brackets"></param>
        /// <returns></returns>
        private static List<List<string>> ConvertBracketsToString(List<Bracket> brackets)
        {
            var result = new List<List<string>>();
            List<string> tmp;
            foreach (Bracket b in brackets)
            {
                // For now the each bracket(list) will only store the two terms in the 
                // struct as the distributive has not been applied yet. 
                tmp = new List<string>
                {
                    b.term1,
                    b.term2
                };
                result.Add(tmp);
            }
            return result;
        }

        private static List<List<string>> RecursiveDistributiveLaw(List<List<string>> brackets)
        {
            var lls = new List<List<string>>();
            // The distributive law can be applied as long as there are at least 2 brackets
            // remaining in the expression. 
            if (brackets.Count > 1)
            {
                // Applies the distributive law on two brackets that contain n and m terms
                // each and adds the result to the list of remaining brackets. 
                lls.Add(SingleDistributiveLaw(brackets[0], brackets[1]));
                // The brackets used within the distributive law do not persist.
                // This means that they should be removed.
                brackets.RemoveAt(0);
                brackets.RemoveAt(0);
                lls.AddRange(brackets);
                // More than 2 brackets remain and so repeat the process. 
                return RecursiveDistributiveLaw(lls);
            }
            else
            {
                // Distributive law can no longer be applied and so return the complete
                // sum of products. 
                return brackets;
            }
        }

        /// <summary>
        /// Applies the distributive law on two brackets that store m and n terms
        /// respectively. This is because (KN+KLQ+LMN+LMQ)(P+MQ) can be further
        /// distributed. 
        /// </summary>
        /// <param name="b1">A bracket storing m number of terms. </param>
        /// <param name="b2">A bracket storign n number of temrs that merges into b1.</param>
        /// <returns></returns>
        private static List<string> SingleDistributiveLaw(List<string> b1, List<string> b2)
        {
            var lls = new List<string>();
            // Apply the law to every term within both of the brackets to find the complete
            // expansion of the two brackets. 
            for (var i = 0; i < b1.Count; i++)
            {
                for (var j = 0; j < b2.Count; j++)
                {
                    // Applying the distributive law to two products. e.g. KLQ and P
                    lls.Add(ApplyDistributiveLaw(b1[i], b2[j]));
                }
            }
            return lls;
        }

        /// <summary>
        /// Apply the distributive on two individual terms in the supplied brackets. 
        /// This simply filters out the duplicate inputs as all unique inputs must remain
        /// when the law is applied. 
        /// </summary>
        /// <param name="a">A product with a number of terms. </param>
        /// <param name="b">A product that is being distributed with A</param>
        /// <returns></returns>
        private static string ApplyDistributiveLaw(string a, string b)
        {
            string tempresult = a + b;
            string finalresult = "";
            foreach (char c in tempresult)
            {
                // Simply find the unique inputs to gain the result of the law. 
                if (!finalresult.Contains(c))
                {
                    finalresult += c;
                }
            }
            return finalresult;
        }

        /// <summary>
        /// Finds the product within the sum of products from the distributive law that has
        /// the fewest terms within it. This allows the most minimal expression to be the 
        /// result. 
        /// </summary>
        /// <returns>The shortest product within the array representing the sum of 
        /// products. </returns>
        private static string GetMinProduct(string[] sumOfProducts)
        {
            string min = sumOfProducts[0];
            foreach (string p in sumOfProducts)
            {
                if (p.Length < min.Length)
                {
                    min = p;
                }
            }
            return min;
        }

        /// <summary>
        /// Iterates through the smallest product from the sum of products. Each term within
        /// the product is then replaced with its respective prime implicant which can then 
        /// be converted into the completely minimised expression. 
        /// </summary>
        /// <param name="termToImplicantMap">The mapping between prime implicants and letters.
        /// This simplifies boolean algebra with the prime implicants.</param>
        /// <param name="minProduct">The smallest product found in the sum of products. 
        /// This product will produced the most minimal result. </param>
        /// <returns></returns>
        private string GetFinalExpression(Dictionary<char, string> termToImplicantMap, string minProduct)
        {
            string subExpression;
            string implicant;
            string result = "";
            for (var i = 0; i < minProduct.Length; i++)
            {
                // Convert the letter into an implicant which then produces the minimal 
                // expression. ie. K => -011 => !B.C.D
                implicant = termToImplicantMap[minProduct[i]];
                subExpression = ConvertImplicantToExpression(implicant);
                result += subExpression;
                // If we are not converting the last term then the implicants must be 
                // separated by OR gates. 
                if (i < minProduct.Length - 1)
                {
                    result += " + ";
                }
            }
            return result;
        }

    }
}
