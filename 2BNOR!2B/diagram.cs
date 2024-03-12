using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace _2BNOR_2B
{
    class Diagram
    {
        private static Regex r = new Regex(@"\s+");
        //the base operators within boolean logic. NAND and NOR not included as they are compound gates.
        private char[] booleanOperators = { '.', '^', '+', '!' };
        private string[] gateNames = { "and_gate", "xor_gate", "or_gate", "not_gate" };
        private string[] inputMap;
        private string[] outputMap;
        private string[] headers;
        private string infixExpression = "";
        private string inputStates = "";
        //The root of the tree. Do not need array as the children are stored within the class itself. 
        private Element rootNode;
        private Element outputNode;
        //Array to store the input elements within the tree. This is set when the wires are being drawn within the diagram. 
        private Element[] elements;
        private Element[] inputs;
        private Wire[] wires;
        private Canvas c;
        //The following attributes are the constants for the diagram drawing. These can be edited to change the look of diagrams. 
        //These values are ones that I have found to produce the nicest diagrams from testing. 
        int elementWidth = 2;
        int xOffset = 12;
        int pixelsPerSquare = 15;
        double canvasWidth, canvasHeight;

        public Diagram(Canvas c)
        {
            this.c = c;
        }

        //returns the binary tree representing the logic diagram 
        public Element GetTree()
        {
            return rootNode;
        }

        private string RemoveWhitespace(string input, string replacement)
        {
            return r.Replace(input, replacement);
        }

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
        }

        //implementation of the 'Shunting Yard' algorithm for boolean expressions. This produces the postfix boolean expression of an infix expression. 
        private string ConvertInfixtoPostfix(string infixExpression)
        {
            //removing the compound gates from the expression (NAND and NOR) because they do not have operator precedence.  Also removing whitespace for ease. 
            infixExpression = RemoveCompoundGates(RemoveWhitespace(infixExpression, ""));
            Stack<char> operatorStack = new Stack<char>();
            string postfixExpression = "";
            int operatorPrecedence;
            //tokenising infix ready for the conversion
            foreach (char token in infixExpression)
            {
                if (char.IsLetter(token) || char.IsNumber(token))
                {
                    postfixExpression += token;
                }
                //if the token is an operator.
                else if (booleanOperators.Contains(token))
                {
                    //precedence value of the token
                    operatorPrecedence = Array.IndexOf(booleanOperators, token);
                    while ((operatorStack.Count > 0 && operatorStack.Peek() != '(') && (Array.IndexOf(booleanOperators, operatorStack.Peek()) > operatorPrecedence))
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

        //Removes the compound gates from an expression. This is so precedence can be observed within the shunting yard. This is because NAND and NOR are
        //not in the order of precedence for boolean operators. 
        private static string RemoveCompoundGates(string booleanExpression)
        {
            char operand1;
            char operand2;
            string subExpression;
            //the expression with no compound gates (NAND and NOR) 
            string flattenedExpression = "";
            for (int i = 0; i < booleanExpression.Length; i++)
            {
                //add anything straight to the flattened expression that isn't a compound operator. 
                if (!(booleanExpression[i] == '@' || booleanExpression[i] == '#'))
                {
                    flattenedExpression += booleanExpression[i];
                }
                //if the token is a NAND gate
                else if (booleanExpression[i] == '@')
                {
                    //getting the operands that are being operated on by the compound operator.
                    operand1 = flattenedExpression[i - 1];
                    operand2 = booleanExpression[i + 1];
                    //removing the first operand from the flattened expression to remove duplication.
                    flattenedExpression = flattenedExpression.Substring(0, i - 1);
                    subExpression = $"!({operand1}.{operand2})"; 
                    flattenedExpression += subExpression;
                    //incrementing the counter (token in the expression) to 'skip' the second operand -> remove duplicate operands
                    i++;
                }
                //if the token is a NOR gate. This is same logic as above with just with a slightly different subexpression.
                else if (booleanExpression[i] == '#')
                {
                    operand1 = flattenedExpression[i - 1];
                    operand2 = booleanExpression[i + 1];
                    flattenedExpression = flattenedExpression.Substring(0, i - 1);
                    subExpression = $"!({operand1}+{operand2})";
                    flattenedExpression += subExpression;
                    i++;
                }
            }
            if (flattenedExpression == "")
            {
                return booleanExpression;
            }
            else
            {
                return flattenedExpression;
            }
        }

        //Whenever the canvas is clicked. Rerun the colouring to ensure the diagram reflects the new state of the diagram. 
        public void UpdateWires()
        {
            inputStates = "";
            GetInputStates(rootNode);
            AssignGateStates(rootNode);
            ColourWires();
        }

        #region diagram drawing

        //Recursive inorder traversal to get the states of the inputs. A state is added only when the element has the correct name. 
        private void GetInputStates(Element root)
        {
            if (root.leftChild != null)
            {
                GetInputStates(root.leftChild);
            }

            if (root.GetElementName() == "input_pin")
            {
                inputStates += root.GetState();
            }

            if (root.rightChild != null)
            {
                GetInputStates(root.rightChild);
            }
        }

        private int GetHeightOfTree(Element root)
        {
            //If the root doesn't exist, height must be 0. 
            if (root == null)
            {
                return 0;
            }

            int leftChildHeight = GetHeightOfTree(root.leftChild);
            int rightChildHeight = GetHeightOfTree(root.rightChild);

            if (leftChildHeight > rightChildHeight)
            {
                return leftChildHeight + 1;
            }
            else
            {
                return rightChildHeight + 1;
            }
        }

        //Linear search that returns the first input that contains the same label as the one being searched before. 
        //This always works as the unique input is always deeper within the tree than the duplicate.
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

        private int GetNumberOfNodes(Element root)
        {
            if (root == null)
            {
                return 0;
            }
            return 1 + GetNumberOfNodes(root.leftChild) + GetNumberOfNodes(root.rightChild);
        }

        private void GenerateBinaryTreeFromExpression(string inputExpression)
        {
            string postfixExpression = ConvertInfixtoPostfix(inputExpression);
            inputs = new Element[GetNumberOfInputs(inputExpression, false)];
            Stack<Element> nodeStack = new Stack<Element>();
            elements = new Element[inputExpression.Length + 1];
            Element nodeToAdd;
            Element leftChild;
            Element rightChild;
            Element tmp;
            //The ID of the node within the tree. This is also the same as the position within the input string. 
            int elementID = 0;
            int i = 0;
            string elementName = "";
            string inputsAdded = "";
            //Tokenising the postfix expression, each token is a node within the tree. The order of postfix is the same as
            //the arrangement of the nodes within the tree. 
            foreach (char c in postfixExpression)
            {
                //If the token is a letter then it must be an input. 
                if ((char.IsLetter(c) && char.IsUpper(c)) || char.IsNumber(c))
                {
                    //creating an input pin
                    nodeToAdd = new Element(elementID, c);
                    inputs[i] = nodeToAdd;
                    i++;
                    //If the input is a number then the state can already by set. 
                    if (char.IsNumber(c))
                    {
                        nodeToAdd.SetState(c - 48);
                    }

                    //marking the node if it is not unique. This will be used when drawing diagrams with repeated inputs.  
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
                //One operand must be popped for a NOT gate, this means it must be considered separately to the other gates. 
                else if (c == '!')
                {
                    rightChild = nodeStack.Pop();
                    //create a logic gate
                    nodeToAdd = new Element("not_gate", elementID, null, rightChild);
                    nodeToAdd.SetInstances(1);
                    rightChild.parent = nodeToAdd;
                }
                //Gate has been found, form the higher level within the tree. 
                else
                {
                    //Popping two nodes from stack, these form the child nodes to the current token within the tree. 
                    rightChild = nodeStack.Pop();
                    leftChild = nodeStack.Pop();
                    //create a logic gate
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
            //Final item on the stack is the root node with the completed tree. 
            rootNode = nodeStack.Pop();
        }

        //Method that calculates the spacing of the nodes within a binary tree based off of the height and depth within the tree. 
        //This is also the y-position of the left-most node within the logic diagram. The position within layer parameter can act as a multiplier to find the positions of the other 
        //nodes within the same column. 
        private double CalculateNodeYposition(int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            double initialY = (Math.Pow(2, heightOfTree) / Math.Pow(2, depthWithinTree)) * pixelsPerSquare;
            return initialY + (initialY * positionWithinLayer * 2);
        }

        private double CalculateXposition(int depthWithinTree)
        {
            return canvasWidth - ((((pixelsPerSquare - 7) * elementWidth) + ((pixelsPerSquare - 7) * xOffset)) * depthWithinTree);
        }

        private double TranslateNode(double startX, int heightOfTree)
        {
            double maxX = CalculateXposition(heightOfTree);
            //initial offset of 50 pixels for a better placed diagram. 
            return startX - maxX + 50;
        }

        private double CalculateNodeXposition(Element node, int heightOfTree, int depthWithinTree)
        {
            double x;
            //Inputs (leaf nodes) should be drawn at minimum x position within the canvas. 
            if ((node.leftChild == null) && (node.rightChild == null))
            {
                x = CalculateXposition(heightOfTree);
            }
            else
            {
                x = CalculateXposition(depthWithinTree);
            }
            return TranslateNode(x, heightOfTree);
        }

        //Used for exporting diagram. This is used to find the y dimensions of the exported image. 
        private LogicGate GetWidestGate(Element root)
        {

            Element tmp = new Element();
            Queue<Element> q = new Queue<Element>();
            q.Enqueue(root);
            //Carrying out a traversal on only the right children of the root node. This is because the rightmost node will be the furthese down the screen
            //and so the whole the diagram will be in the exported image if this node is within the export. 
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
            int heightOfTree = GetHeightOfTree(rootNode);
            Element current = rootNode;
            LogicGate l = GetWidestGate(rootNode);
            double maxX = Canvas.GetRight(outputNode.GetLogicGate()) + 75;
            double maxY = Canvas.GetBottom(l) + 25;
            return new Rect(new Size(maxX, maxY));
        }

        private Wire DrawWiresForLeftChildren(Element root)
        {
            Wire w = new Wire(c);
            LogicGate rootLogicGate = root.GetLogicGate();
            LogicGate leftchildLogicGate = root.leftChild.GetLogicGate();
            Element input;
            w.SetStart(rootLogicGate.GetInputPoint1());
            //If the left child gate exists then draw normally. 
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
                //Searching for the input with the same label as the left child node does exist but doesnt have a logic gate, therefore it is not a unique input. 
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
            Wire w = new Wire(c);
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

        //Finds the intersection between 4 points representing 2 lines if one exists, otherwise return null. 
        private Point? FindIntersection(Point p0, Point p1, Point p2, Point p3)
        {
            double s_x = p1.X - p0.X;
            double s_y = p1.Y - p0.Y;
            double s2_x = p3.X - p2.X;
            double s2_y = p3.Y - p2.Y;
            double denom = s_x * s2_y - s2_x * s_y;

            if (denom == 0)
            {
                //Lines are collinear. 
                return null;
            }
            bool isDenomPositive = denom > 0;

            double s3_x = p0.X - p2.X;
            double s3_y = p0.Y - p2.Y;
            double s_numer = s_x * s3_y - s_y * s3_x;


            if ((s_numer < 0) == isDenomPositive)
            {
                return null;
            }

            double t_numer = s2_x * s3_y - s_x * s3_y;

            if ((t_numer < 0) == isDenomPositive)
            {
                return null;
            }

            if (((s_numer > denom) == isDenomPositive) || ((t_numer > denom) == isDenomPositive))
            {
                return null;
            }

            double t = t_numer / denom;
            Point? result = new Point(p0.X + (t * s_x), p0.Y + (t * s_y));
            return result;
        }

        //Searches through all points to find the intersections. 
        private void DrawIntersections()
        {
            List<Point> horizontalLines = new List<Point>();
            List<Point> verticalLines = new List<Point>();
            Point? intersection;
            Wire tmp;
            for (int j = 0; j < wires.Length - 1; j++)
            {
                horizontalLines.AddRange(wires[j].GetPoints(true));
                verticalLines.AddRange(wires[j].GetPoints(false));
            }


            for (int i = 0; i < verticalLines.Count - 1; i = i + 2)
            {
                //first point of line = i, second point = i + 1. These form the vertical line being checked. 
                for (int c = 0; c < horizontalLines.Count - 1; c = c + 2)
                {
                    //again c and c + 1 form the horizontal line being checked. 
                    intersection = FindIntersection(verticalLines[i], verticalLines[i + 1], horizontalLines[c], horizontalLines[c + 1]);
                    if (intersection != null && (FindWire(verticalLines[i]) != FindWire(horizontalLines[c])))
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
            Queue<Element> q = new Queue<Element>();
            Queue<Wire> intersectionq = new Queue<Wire>();
            wires = new Wire[GetNumberOfNodes(root)];
            Element tmp;
            int i = 0;
            //Using a breadth first traversal to reach all nodes within the tree. Includes nodes without gates because the child wires must also be drawn to an input. 
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
            for (int j = 0; j < wires.Length - 1; j++)
            {
                wires[j].RenderLine();
            }
        }

        //Sets the colours of the wires within the diagram. 
        private void ColourWires()
        {
            //As the wires have gates assigned to them, the wire can simply assume the state of the deepest node it connects in the tree. 
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

        //Adds nodes to the canvas by finding their position and then placing them. This only adds gates to nodes that are either unique inputs or boolean operators. 
        private void DrawNode(Element currentNode, int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            LogicGate logicGate;
            //Position of the node within the canvas. 
            double x, y;
            //checking if a node is not a repeated input. If it is then a logic gate doesn't have to be drawn. 
            if (currentNode.GetInstances() != 0)
            {
                x = CalculateNodeXposition(currentNode, heightOfTree, depthWithinTree);
                //Gates are not being drawn in parallel to preserve the look of the tree. 
                if (currentNode.parent != null && currentNode.parent.GetElementName() == "not_gate" && currentNode.GetElementName() == "input_pin")
                {
                    //If the nodes parent is a not gate then the child should be drawn in parallel. So the Y-coord of the NOT gate can be used 
                    //and so does not have to be calculated. 
                    y = Canvas.GetTop(currentNode.parent.GetLogicGate());
                }
                else
                {
                    //Otherwise, calculate the Y-coord of the node according to the location within the tree. 
                    y = CalculateNodeYposition(heightOfTree, depthWithinTree, positionWithinLayer);
                }
                logicGate = new LogicGate(currentNode);
                //Adding the link between the tree and the visual diagram. 
                currentNode.SetLogicGate(logicGate);
                Canvas.SetLeft(logicGate, x);
                Canvas.SetTop(logicGate, y);
                //set the other position for saving and exporting. 
                double p = logicGate.GetInputPoint2().Y + 50;
                Canvas.SetBottom(logicGate, p);
                Canvas.SetRight(logicGate, logicGate.GetInputPoint2().X);
                c.Children.Add(logicGate);
            }
        }

        //Breadth first traversal that is used to place all of the nodes within the tree onto the canvas. 
        private void DrawNodes(Element root, int heightOfTree)
        {
            Queue<Element> q = new Queue<Element>();
            q.Enqueue(root);
            int depthWithinTree = 0;
            int positionWithinLayer = 0;
            int sizeOfQ = 0;
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

        //Iterative postorder traversal. Traverses the tree and assigns the gates a state corresponding to the row  the row in the truth table
        //which is also in postorder. 
        private void AssignGateStates(Element root)
        {
            //A singular row of the truth table. Using the input states to find the correct row. 
            string tableRow = GetTruthTableRow();
            Stack<Element> s = new Stack<Element>();
            int i = 0;
            while (true)
            {
                while (root != null)
                {
                    s.Push(root);
                    s.Push(root);
                    //Traversing the left subtree. 
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
                    //set the state of the gate to the corresponding position within table row.
                    int state = tableRow[i] - 48;
                    root.SetState(state);
                    i++;
                    root = null;
                }
            }
        }

        //Small function for drawing the wire that connects the input to the root node of the binary tree. 
        private void DrawOutputWire()
        {
            Wire w = new Wire(c);
            w.SetStart(outputNode.GetLogicGate().GetInputForOutput());
            w.SetEnd(rootNode.GetLogicGate().GetOutputPoint());
            w.SetGate(rootNode.GetLogicGate());
            w.SetPoints();
            w.RenderLine();
            wires[wires.Length - 1] = w;
        }

        //Method that draws the output pin for the diagram. 
        private void DrawOutput(int heightOfTree)
        {
            //Creating an element to represent the output. Unique elementID of -1. 
            //Limitation of program can only draw single input gates. 
            outputNode = new Element(-1);
            LogicGate logicGate = new LogicGate(outputNode);
            outputNode.SetLogicGate(logicGate);
            //The position of the output node is the same y-position as the root node and a small shift from the root node. 
            double x = TranslateNode(CalculateXposition(0), heightOfTree) + (pixelsPerSquare * 10);
            double y = CalculateNodeYposition(heightOfTree, 0, 0);
            Canvas.SetTop(logicGate, y);
            Canvas.SetLeft(logicGate, x);
            Canvas.SetRight(logicGate, x + logicGate.ActualWidth);
            c.Children.Add(logicGate);
        }

        //public method that links UI to class, 'stitches' all of the methods together to give the drawn diagram. 
        public void DrawDiagram(string inputExpression)
        {
            canvasHeight = c.ActualHeight;
            canvasWidth = c.ActualWidth;
            GenerateBinaryTreeFromExpression(inputExpression);
            inputMap = GenerateInputMap(inputExpression, false);
            headers = GetHeaders(inputExpression, false);
            outputMap = GenerateOutputMap(inputExpression, headers, false);
            int heightOfTree = GetHeightOfTree(rootNode);
            DrawNodes(rootNode, heightOfTree);
            DrawWires(rootNode);
            DrawOutput(heightOfTree);
            DrawOutputWire();
            UpdateWires();
        }

        #endregion

        #region Truth table generation

        //Gives the result of the fully evaluated expression. 
        private int EvaluateBooleanExpression(string binaryCombination, string inputExpression)
        {
            //Removing compound gates so that the expression can be evaluated properly.
            string flattenedExpression = RemoveCompoundGates(inputExpression);
            string postfix = ConvertInfixtoPostfix(flattenedExpression);
            int operand1;
            int operand2;
            int tmp;
            postfix = SubsituteIntoExpression(binaryCombination, postfix);
            Stack<int> evaluatedStack = new Stack<int>();
            foreach (char c in postfix)
            {
                if (char.IsNumber(c))
                {
                    evaluatedStack.Push(c);
                }
                else if (c == '!')
                {
                    operand1 = evaluatedStack.Pop();
                    //xor with 1 flips the bit provided. This is the same as using a not gate. 
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
            //Final item on the stack is the result of the evaluation. 
            return evaluatedStack.Pop();
        }

        //Returns the result of the operation of a boolean operation with two operands. 
        private int EvaluateSingleOperator(int o1, int o2, char operation)
        {
            //do not need to consider NAND and NOR as they can be flattened and removed from the expression. 
            //does not include NOT as it only takes one operand.
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

        private string SubsituteIntoExpression(string binaryCombination, string inputExpression)
        {
            string binaryDigit;
            foreach (char c in inputExpression)
            {
                //if the character is a letter it must be replaced with corresponding binary digit. 
                if (char.IsLetter(c))
                {
                    //The index within the binary combination can be calculated using the ascii value of the input label as truth table inputs are always in alphabetical order.
                    binaryDigit = binaryCombination[c - 65].ToString();
                    inputExpression = inputExpression.Replace(c.ToString(), binaryDigit);
                }
            }
            //the final substituted expression that can be now evaluated. 
            return inputExpression;
        }

        //Converts an int into binary string. This can be used to generate the input combinations for truth tables. 
        private string ConvertIntintoBinaryString(int n, string booleanExpression, bool isUnique)
        {
            return Convert.ToString(n, 2).PadLeft(GetNumberOfInputs(booleanExpression, isUnique), '0');
        }

        //Gets the respective string value of the truth table based off of its input. 
        private string GetTruthTableRow()
        {
            //return the same row as the input map for a given set of inputs
            return outputMap[Array.IndexOf(inputMap, inputStates)];
        }

        //counts the number of unique inputs within a boolean expression
        private int GetNumberOfInputs(string booleanExpression, bool isUnique)
        {
            int numberOfInputs = 0;
            string alreadyCounted = "";
            foreach (char token in booleanExpression)
            {
                //any letter is an input within the expression. Therfore add it to the counter. 
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

        //counts the number of operators within a boolean expression
        private int GetNumberOfOperators(string booleanExpression)
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

        private string[] GenerateInputMap(string inputExpression, bool isUnique)
        {
            int numberOfInputs = GetNumberOfInputs(inputExpression, isUnique);
            // 2^n is the number of possible binary combinations hence number of rows within table. 
            int numberOfRows = (int)Math.Pow(2, numberOfInputs);
            //the input map of the truth table. Stores the input columns of the truth table. 
            string[] inputMap = new string[numberOfRows];
            for (int i = 0; i < numberOfRows; i++)
            {
                inputMap[i] = ConvertIntintoBinaryString(i, inputExpression, isUnique);
            }
            return inputMap;
        }

        private string[] GenerateOutputMap(string inputExpression, string[] headers, bool isUnique)
        {
            string[] inputMap = GenerateInputMap(inputExpression, isUnique);
            int numberOfRows = (int)Math.Pow(2, GetNumberOfInputs(inputExpression, isUnique));
            //Number of columns within the table is always the number of inputs and number of operators. 
            int numberOfColumns = GetNumberOfInputs(inputExpression, isUnique) + GetNumberOfOperators(inputExpression);
            string inputCombination;
            //array that handles only the output portion of the truth, this can be put together with the input map to form a complete table
            string[] outputMap = new string[inputMap.Length];
            //inputmap looks like ["000", "001"]
            //so subsitute each row into each column header and evaluate and that is the cell commpleted for the output map7
            for (int i = 0; i < numberOfRows; i++)
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

        //function that takes an inputted expression and produces a series of headers either for display (inputs sorted to beginning)
        //or for colouring wires (postorder). 
        private string[] GetHeaders(string inputExpression, bool isDisplay)
        {
            string postfix = ConvertInfixtoPostfix(inputExpression);
            string[] headers;
            int numberOfInputs = GetNumberOfInputs(postfix, false);
            int numberOfOperators = GetNumberOfOperators(postfix);
            if (isDisplay)
            {
                //numberOfInputs = getNumberOfInputs(postfix, true);
                headers = GenerateDisplayOperatorHeaders(postfix, numberOfInputs, numberOfOperators);
            }
            else
            {
                //numberOfInputs = getNumberOfInputs(postfix, false);
                headers = GeneratePostOrderHeaders(postfix, numberOfInputs, numberOfOperators);
            }
            return headers;
        }

        //function that takes a postfix expression and produces truth table headers that being displayed to the user. (original function). 
        private string[] GenerateDisplayOperatorHeaders(string inputExpression, int numberOfInputs, int numberOfOperators)
        {
            string[] postorderHeaders = GeneratePostOrderHeaders(inputExpression, numberOfInputs, numberOfOperators);
            string[] displayHeaders = new string[GetNumberOfInputs(inputExpression, true) + numberOfOperators];
            Array.Sort(postorderHeaders, (x, y) => x.Length.CompareTo(y.Length));
            postorderHeaders = postorderHeaders.Distinct().ToArray();
            Array.Copy(postorderHeaders, displayHeaders, displayHeaders.Length);
            return displayHeaders;
        }

        private string[] GeneratePostOrderHeaders(string postfix, int numberOfInputs, int numberOfOperators)
        {
            Stack<string> subExpressionStack = new Stack<string>();
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

        private void DrawTruthTableHeaders(Canvas c, string[] headers)
        {
            Label cell;
            Thickness border = new Thickness(2);
            FontFamily font = new FontFamily("Consolas");
            double cellWidth = 30;
            double x = 20;
            foreach (string header in headers)
            {
                //Checking for inputs of the table
                if (header.Length != 1)
                {
                    cellWidth = (header.Length * 10) + 10;
                }
                cell = new Label();
                cell.HorizontalContentAlignment = HorizontalAlignment.Center;
                cell.Width = cellWidth;
                cell.BorderBrush = Brushes.LightGray;
                cell.BorderThickness = border;
                cell.Background = Brushes.White;
                cell.FontFamily = font;
                cell.FontSize = 14;
                cell.Content = header;
                Canvas.SetTop(cell, 20);
                Canvas.SetLeft(cell, x);
                c.Children.Add(cell);
                x += cellWidth;
            }
        }

        private void DrawTruthTableBody(Canvas c, string[] headers, string[] outputMap)
        {
            Label cell;
            Thickness border = new Thickness(2);
            FontFamily font = new FontFamily("Consolas");
            double cellWidth = 30;
            double x = 20;
            double y = 50;
            foreach (string row in outputMap)
            {
                for (int i = 0; i < headers.Length; i++)
                {
                    //Checking for inputs of the table
                    if (headers[i].Length != 1)
                    {
                        cellWidth = (headers[i].Length * 10) + 10;
                    }
                    cell = new Label();
                    cell.HorizontalContentAlignment = HorizontalAlignment.Center;
                    cell.Width = cellWidth;
                    cell.BorderBrush = Brushes.LightGray;
                    cell.BorderThickness = border;
                    cell.Background = Brushes.White;
                    cell.FontFamily = font;
                    cell.FontSize = 14;
                    cell.Content = row[i];
                    Canvas.SetTop(cell, y);
                    Canvas.SetLeft(cell, x);
                    c.Children.Add(cell);
                    x += cellWidth;
                }
                cellWidth = 30;
                x = 20;
                y += 30;
            }


        }

        //Links class to UI, used to draw the truth tables to the canvas. 
        public void DrawTruthTable(Canvas c, string inputExpression, bool isSteps)
        {
            //Removing any previously drawn tables from the canvas. 
            c.Children.Clear();
            //headers = generateTruthTableHeadersWithSteps(inputExpression);
            headers = GetHeaders(inputExpression, true);
            outputMap = GenerateOutputMap(inputExpression, headers, true);
            if (isSteps)
            {
                //drawTruthTable(c, headers, outputMap);
                DrawTruthTableHeaders(c, headers);
                DrawTruthTableBody(c, headers, outputMap);
            }
            else
            {
                throw new NotImplementedException("Not yet buster!");
            }
        }
        #endregion

        #region Minimisation
        //Adds and gates to consecutive inputs (ie A!B -> (A.!B))
        private string AddANDGates(string epi)
        {
            string tmp = epi;
            char last = epi[0];
            for (int i = 1; i < epi.Length; i++)
            {
                if (char.IsLetter(epi[i]) && char.IsLetter(last))
                {
                    tmp = tmp.Insert(i, ".");
                }
            }
            return $"({tmp})";
        }


        //Produces the final minimised expression from the essential prime implicants.
        private string ConvertEPIsToExpression(List<string> essentialPrimeImplicants)
        {
            //Converting each implicant into input form. Ie (-100 becomes B!C!D)
            essentialPrimeImplicants = essentialPrimeImplicants.ConvertAll(new Converter<string, string>(ConvertImplicantToExpression));
            //Each implicant is separated by OR gates. 
            string expression = string.Join("+", essentialPrimeImplicants);
            return expression;
        }

        //Replaces the binary form into the input form so that a complete boolean expression can be created. 
        private string ConvertImplicantToExpression(string epi)
        {
            //Removing regex characters to make conversion easier (due to "\d" being two characters long). 
            epi = epi.Replace(@"\d", "-");
            string tmp = "";
            char input;
            for (int i = 0; i < epi.Length; i++)
            {
                input = (char)(i + 65);
                if (epi[i] == '1')
                {
                    //Each prime implicant in final expression is always sequential.
                    tmp += input;
                }
                //If 0 in implicant then the input is the complement (Ie, NOT gate). 
                else if (epi[i] == '0')
                {
                    tmp += $"!{input}";
                }
            }
            tmp = AddANDGates(tmp);
            return tmp;
        }

        //Evaluates the minterms to the prime implicants. This forms the basis of the prime implicant chart.
        private void SetRegexPatterns(Dictionary<string, string> regex, List<string> minterms)
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

        //Replaces the dashes within the prime implicants with "\d", this allows the prime implicants to form regex pattern. 
        //Replacing "\d" indicating it doesnt matter which digit is present in this position. 
        private void ConvertImplicantsIntoRegex(Dictionary<string, string> regex, List<string> primeImplicants)
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
                //Adding to the dictionary, value is empty because the keys are not evaluated yet. 
                regex.Add(tmp, value);
                tmp = "";
            }
        }

        //Carries out the merge between minterms, replacing bits that do not matter with a dash. 
        private string MergeMinterms(string m1, string m2)
        {
            string mergedMinterm = "";
            //If the minterms are not the same length then a merge cannot occur. 
            if (m1.Length != m2.Length)
            {
                throw new Exception("Incorrect length");
            }
            else
            {
                for (int i = 0; i < m1.Length; i++)
                {
                    //If a bit differs between the two minterms then it is replaced with a dash. Otherwise digit remains the same indicating it doesn't matter. 
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

        //For a merge to happen, the dashes within the minterms must align within both minterms. 
        private bool CheckDashesAlign(string m1, string m2)
        {
            //If the lengths of the minterms are not the same then the comparison cannot be done. 
            if (m1.Length != m2.Length)
            {
                throw new Exception("Incorrect length");
            }
            else
            {
                for (int i = 0; i < m1.Length; i++)
                {
                    if (m1[i] != '-' && m2[i] == '-')
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        //Returns true if the minterms only differ by 1 bit. 
        private bool CheckMintermDifference(string m1, string m2)
        {
            int minterm1 = RemoveDashes(m1);
            int minterm2 = RemoveDashes(m2);
            int res = minterm1 ^ minterm2;
            return (res != 0 && ((res & (res - 1)) == 0));
        }

        //Utilty function that temporarily removes the dashes from a binary pattern so that it can be compared. 
        private int RemoveDashes(string minterm)
        {
            return Convert.ToInt32(minterm.Replace('-', '0'), 2);
        }

        private List<string> GetMinterms(string expression)
        {
            List<string> minterms = new List<string>();
            string[] inputMap = GenerateInputMap(expression, true);
            foreach (string input in inputMap)
            {
                //A minterm has been found if input results in the expresion evaluating to true. 
                if (EvaluateBooleanExpression(input, expression) - 48 == 1)
                {
                    minterms.Add(input);
                }
            }
            return minterms;
        }

        //Counts the number of 1's within each place value of the values in the dictionary.
        //Frequency table is used to find the essential prime implicants. 
        private int[] GetFrequencyTable(Dictionary<string, string> regex, List<string> minterms)
        {
            int[] sums = new int[minterms.Count];
            foreach (string s in regex.Values.ToList())
            {
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == '1')
                    {
                        sums[i]++;
                    }
                }
            }
            return sums;
        }

        //Finds the prime implicant that is essential within the key list. Ie The bit set that results in a frequency of 1.
        private string GetEssentialPrimeImplicant(Dictionary<string, string> regex, int pos)
        {
            string[] essentialPrimes = regex.Values.ToArray();
            string[] keys = regex.Keys.ToArray();
            string prime;
            for (int i = 0; i < essentialPrimes.Length; i++)
            {
                prime = essentialPrimes[i];
                //When the bit has been found that results in a frequency of 1 then the essential prime implicant has been found. 
                if (prime[pos] == '1')
                {
                    return keys[i];
                }
            }
            throw new Exception("Item could be found");
        }

        private List<string> GetEssentialPrimeImplicants(Dictionary<string, string> regex, List<string> minterms, List<string> pis)
        {
            //Calculating the number of 1's within each column of the values in the dictionary. 
            int[] bitFrequencyTable = GetFrequencyTable(regex, minterms);
            List<string> essentialPrimeImplicants = new List<string>();
            string epi = "";
            for (int i = 0; i < bitFrequencyTable.Length; i++)
            {
                //If the total number of bits in one column of the values is 1, then only one prime implicant covers that minterm and hence it is an essential prime implicant. 
                if (bitFrequencyTable[i] == 1)
                {
                    epi = GetEssentialPrimeImplicant(regex, i);
                    //Removing repeats to avoid cases such as "A+A" which can be further simplified. 
                    if (!essentialPrimeImplicants.Contains(epi))
                    {
                        essentialPrimeImplicants.Add(epi);
                    }
                }
            }

            string coveredString = GetCoveredString(essentialPrimeImplicants);
            //If the covered string contains a zero, the essential prime implicants do not cover all minterms, this means more prime implicants must be 
            //added to complete the minimised expression. 
            if (coveredString.Contains('0'))
            {
                //AddPIsToFinalExpression(essentialPrimeImplicants, pis);
                //call petriks method 
            }
            else
            {
                //All minterms are covered so all implicants in final expression have been found. 
                return essentialPrimeImplicants;
            }

            return essentialPrimeImplicants;
        }

        //The following function carries out a recursive merging process where a merge can take place if dashes align and 1 bit differs between the two minterms. 
        //Any term that cannot be merged is a prime implicant and can be added to the return list. 
        private List<string> GetPrimeImplicants(List<string> mintermList)
        {
            //Stores the prime implicants that are within the list of minterms.
            List<string> primeImplicants = new List<string>();
            //Array used to track the merged terms and hence find the prime implicants. 
            bool[] merges = new bool[mintermList.Count];
            //If this is zero then the prime implicants have been found. 
            int numberOfMerges = 0;
            string mergedMinterm;
            string m1;
            string m2;
            for (int i = 0; i < mintermList.Count; i++)
            {
                for (int c = i + 1; c < mintermList.Count; c++)
                {
                    m1 = mintermList[i];
                    m2 = mintermList[c];
                    //Merge can only be done if the dashes align and one bit differs between the minterms. 
                    if (CheckDashesAlign(m1, m2) && CheckMintermDifference(m1, m2))
                    {
                        //merge minterms
                        mergedMinterm = MergeMinterms(m1, m2);
                        //add result to list. 
                        primeImplicants.Add(mergedMinterm);
                        numberOfMerges++;
                        //The terms that have been merged at least once are set to true so that they are filtered. 
                        merges[i] = true;
                        merges[c] = true;
                    }
                }
            }
            //Filtering the merged terms for the prime implicants and removing any repeats. 
            for (int j = 0; j < mintermList.Count; j++)
            {
                if (!merges[j] && !primeImplicants.Contains(mintermList[j]))
                {
                    primeImplicants.Add(mintermList[j]);
                }
            }
            //No more implicants can be merged from the current list so all of the prime implicants have been found. 
            if (numberOfMerges == 0)
            {
                return primeImplicants;
            }
            else
            {
                //If merges have been made then recursive because more prime implicants could be found. 
                return GetPrimeImplicants(primeImplicants);
            }
        }

        //Implementation of the Quine-McCluskey algorithm for diagram/expression minimisation. Returns the minised expression by finding prime and essential prime implicants from merged minterms. 
        public string MinimiseExpression(string expression)
        {
            string minimisedExpression = ""; 
            //Finding prime implicants to get essential prime implicants. 
            List<string> minterms = GetMinterms(expression);
            List<string> primeImplicants = GetPrimeImplicants(minterms);
            //Creating the prime-implicant chart which is used to find the essential prime implicants. 
            Dictionary<string, string> PIchart = new Dictionary<string, string>();
            ConvertImplicantsIntoRegex(PIchart, primeImplicants);
            SetRegexPatterns(PIchart, minterms);
            List<string> PIs = GetEssentialPrimeImplicants(PIchart, minterms, primeImplicants);

            //check that this result forms a complete string if not then call petriks method. 
            if (GetCoveredString(PIs).Contains('0'))
            {
                //call petriks method. 
                //minimisedExpression = doPetriksMethod(PIchart, primeImplicants)
            }
            else
            {
                minimisedExpression = ConvertEPIsToExpression(PIs); 
            }
            return minimisedExpression;
        }
        #endregion

        private string GetCoveredString(List<string> epis)
        {
            int result = 0;
            foreach (string s in epis)
            {

                string tmp = s.Replace(@"\d", "0");
                result = result | Convert.ToInt32(tmp, 2);
            }
            return result.ToString();
        }

        private List<string> AddPIsToFinalExpression(List<string> epis, List<string> pis)
        {
            List<string> filtered = new List<string>();
            foreach (string s in pis)
            {
                if (epis.Contains(s) == false)
                {
                    filtered.Add(s);
                }
            }

            string coveredString = GetCoveredString(epis);
            string fullyCovered = new string('1', coveredString.Length);
            while (coveredString != fullyCovered)
            {
                int[] bitfrequencies = new int[filtered.Count];
                for (int i = 0; i < filtered.Count; i++)
                {
                    //set equal to number of 1s within string 
                    bitfrequencies[i] = GetNumberOf1s(filtered[i]);
                }
                string chosenImplicant = filtered[Array.IndexOf(bitfrequencies, bitfrequencies.Max())];
                epis.Add(chosenImplicant);
                int result = Convert.ToInt32(coveredString, 2) | Convert.ToInt32(coveredString, 2);
                coveredString = result.ToString();
            }
            return epis;
        }

        private int GetNumberOf1s(string binaryValue)
        {
            int total = 0;
            foreach (char bit in binaryValue)
            {
                if (bit == '1')
                {
                    total++;
                }
            }
            return total;
        }

        private string DoPetriksMethod(Dictionary<string, string> PIchart, List<string> primeImplicants)
        {
            string minimisedExpression = ""; 
            //create mapping between prime implicants. 
            Dictionary<char, string> termsImplicantMapping = MapTermsToImplicants(primeImplicants);
            List<string> productOfSums = GetProductOfSums(termsImplicantMapping, PIchart); 



            return minimisedExpression; 
        }

        private Dictionary<char, string> MapTermsToImplicants(List<string> primeImplicants)
        {
            Dictionary<char, string> mapping = new Dictionary<char, string>();
            char minChar = (char)(primeImplicants[0].Length + 65); 
            for(int i = 0; i < primeImplicants.Count; i++)
            {
                mapping.Add(minChar, primeImplicants[i]);
                minChar++; 
            }
            return mapping;

        }

        private List<string> GetProductOfSums(Dictionary<char, string> termToImplicantMap, Dictionary<string, string> primeImplcantChart)
        {
            List<string> productOfSums = new List<string>();
            //int[] frequencyTable = GetFrequencyTable();

            return productOfSums;  
        }

    }
}