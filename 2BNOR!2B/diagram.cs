using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.RightsManagement;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;

namespace _2BNOR_2B
{
    class diagram
    {
        //the base operators within boolean logic. NAND and NOR not included as they are compound gates.
        private char[] booleanOperators = { '.', '^', '+', '!' };
        private string[] gateNames = { "and_gate", "xor_gate", "or_gate" };
        private string infixExpression = ""; 
        //The root of the tree. Do not need array as the children are stored within the class itself. 
        private element rootNode;
        private element outputNode; 
        //Array to store the input elements within the tree. This is set when the wires are being drawn within the diagram. 
        private element[] elements; 
        private wire[] wires;
        private MouseButtonEventHandler dragHandler;
        //The following attributes are the constants for the diagram drawing. These can be edited to change the look of diagrams. 
        //These values are ones that I have found to produce the nicest diagrams from testing. 
        int elementWidth = 2;
        int xOffset = 12;
        int pixelsPerSquare = 15;
        double canvasWidth, canvasHeight;

        public diagram()
        {

        }

        //(A.((!C.D)+(B.C)))+!(D.A)
        public void setHandler(MouseButtonEventHandler handler)
        {
            dragHandler = handler;
        }

        //implementation of the 'Shunting Yard' algorithm for boolean expressions. This produces the postfix boolean expression of an infix expression. 
        private string ConvertInfixtoPostfix(string infixExpression)
        {
            //removing the compound gates from the expression (NAND and NOR) because they do not have operator precedence.  
            infixExpression = removeCompoundGates(infixExpression);
            Stack<char> operatorStack = new Stack<char>();
            string postfixExpression = "";
            int operatorPrecedence;
            //tokenising infix ready for the conversion
            foreach (char token in infixExpression)
            {
                //spaces within the expression no longer matter as they are simply skipped when found. 
                if (token == ' ')
                {
                    continue;
                }
                //uses the ascii value to filter brackets as ascii of brackets >> letters (operands)
                //also checking that the token is an operand, operators ascii value > brackets ascii value
                if ((token >= 65) && (Array.IndexOf(booleanOperators, token) == -1))
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
        private string removeCompoundGates(string booleanExpression)
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
                    subExpression = "!(" + operand1 + "." + operand2 + ")";
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
                    subExpression = "!(" + operand1 + "+" + operand2 + ")";
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

        #region diagram drawing
        private int getHeightOfTree(element root)
        {
            //If the root doesn't exist, height must be 0. 
            if (root == null)
            {
                return 0;
            }

            int leftChildHeight = getHeightOfTree(root.leftChild);
            int rightChildHeight = getHeightOfTree(root.rightChild);

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
        private element getInputWithSameLabel(char label)
        {
            foreach (element e in elements)
            {
                if (e.getLabel() == label)
                {
                    return e;
                }
            }
            return null;
        }

        private int getNumberOfNodes(element root)
        {
            if (root == null)
            {
                return 0;
            }
            return 1 + getNumberOfNodes(root.leftChild) + getNumberOfNodes(root.rightChild);
        }

        private string inorderTraversal(element root)
        {
            if (root.leftChild != null)
            {
                inorderTraversal(root.leftChild);
            }
            //write method to convert names into symbols 
            //add to infix expression

            if (root.rightChild != null)
            {
                inorderTraversal(root.rightChild); 
            }
        }

        public string getInfixExpression()
        {
            //Get the last item of the headers list and this will give a bracketed version of the expression.  
            string[] headers = generateTruthTableHeadersWithSteps("Expression");
            inorderTraversal(rootNode);
            return infixExpression; 
        }

        private void generateBinaryTreeFromExpression(string inputExpression)
        {
            string postfixExpression = ConvertInfixtoPostfix(inputExpression);   
            Stack<element> nodeStack = new Stack<element>();
            elements = new element[inputExpression.Length+1];
            element nodeToAdd;
            element leftChild;
            element rightChild;
            //The ID of the node within the tree. This is also the same as the position within the input string. 
            int elementID = 0;
            string elementName = "";
            string inputsAdded = "";
            //Tokenising the postfix expression, each token is a node within the tree. The order of postfix is the same as
            //the arrangement of the nodes within the tree. 
            foreach (char c in postfixExpression)
            {
                //If the token is a letter then it must be an input. 
                if (char.IsLetter(c) && char.IsUpper(c))
                {
                    //creating an input pin
                    nodeToAdd = new element(elementID, c);
                    //marking the node if it is not unique. This will be used when drawing diagrams with repeated inputs.  
                    if (inputsAdded.Contains(c) == false)
                    {
                        nodeToAdd.setUniqueness(true);
                        inputsAdded += c;
                    }
                }
                //One operand must be popped for a NOT gate, this means it must be considered separately to the other gates. 
                else if (c == '!')
                {
                    rightChild = nodeStack.Pop();
                    //create a logic gate
                    nodeToAdd = new element("not_gate", elementID, null, rightChild);
                    nodeToAdd.setUniqueness(true);
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
                    nodeToAdd = new element(elementName, elementID, leftChild, rightChild);
                    nodeToAdd.setUniqueness(true);
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
        private double calculateNodeYposition(int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            double initialY = (Math.Pow(2, heightOfTree) / Math.Pow(2, depthWithinTree)) * pixelsPerSquare;
            return initialY + (initialY * positionWithinLayer * 2);
        }

        private double calculateXposition(int depthWithinTree)
        {
            return canvasWidth - ((((pixelsPerSquare - 7) * elementWidth) + ((pixelsPerSquare - 7) * xOffset)) * depthWithinTree);
        }

        private double translateNode(double startX, int heightOfTree)
        {
            double maxX = calculateXposition(heightOfTree);
            //initial offset of 50 pixels for a better placed diagram. 
            return startX - maxX + 50;
        }

        private double calculateNodeXposition(element node, int heightOfTree, int depthWithinTree)
        {
            double x;
            if ((node.leftChild == null) && (node.rightChild == null))
            {
                x = calculateXposition(heightOfTree);
            }
            else
            {
                x = calculateXposition(depthWithinTree);
            }
            return translateNode(x, heightOfTree);
        }

        private void drawWiresForLeftChildren(Canvas c, element root)
        {
            wire w = new wire();
            logicGate rootLogicGate = root.getLogicGate();
            logicGate leftchildLogicGate = root.leftChild.getLogicGate();
            element input; 
            w.setStart(rootLogicGate.getInputPoint1());
            rootLogicGate.setLeftChildWire(w);
            leftchildLogicGate.setLeftChildWire(w); 
            //If the left child gate exists then draw normally. 
            if (leftchildLogicGate != null)
            {
                w.setEnd(leftchildLogicGate.getOutputPoint());
            }
            else
            {
                //Searching for the input with the same label as the left child node does exist but doesnt have a logic gate, therefore it is not a unique input. 
                input = getInputWithSameLabel(root.leftChild.getLabel());
                w.setEnd(input.getLogicGate().getOutputPoint());
            }
            w.draw(c, Brushes.Black);
        }

        private void drawWiresForRightChildren(Canvas c, element root)
        {
            wire w = new wire();
            logicGate rootLogicGate = root.getLogicGate();
            logicGate rightchildLogicGate = root.rightChild.getLogicGate();
            element input; 
            w.setStart(rootLogicGate.getInputPoint2());
            rootLogicGate.setRightChildWire(w); 
            rightchildLogicGate.setRightChildWire(w);
            if (rightchildLogicGate != null)
            {
                w.setEnd(rightchildLogicGate.getOutputPoint());
            }
            else
            {
                input = getInputWithSameLabel(root.rightChild.getLabel());
                w.setEnd(input.getLogicGate().getOutputPoint());
            }
            w.draw(c, Brushes.Black);
        }

        private void drawWires(Canvas c, element root, string inputExpression)
        {
            Queue<element> q = new Queue<element>();
            element tmp; 
            //Using a breadth first traversal to reach all nodes within the tree. Includes nodes without gates because the child wires must also be drawn to an input. 
            q.Enqueue(root);
            while (q.Count != 0)
            {
                tmp = q.Dequeue();  
                if (tmp.leftChild != null)
                {
                    drawWiresForLeftChildren(c, tmp); 
                    q.Enqueue(tmp.leftChild);
                }
                if (tmp.rightChild != null)
                {
                    drawWiresForRightChildren(c, tmp); 
                    q.Enqueue(tmp.rightChild);
                }
            }
        }

        //function that carries out a breadth first traversal on the binary tree. Calculates the position of the nodes and draws them 
        //on the canvas. 
        /*
          Current cases that do not work: 
            Expressions with NOT gate (not offset properly, need to also offset all children)
        */

        //Adds nodes to the canvas by finding their position and then placing them. This only adds gates to nodes that are either unique inputs or boolean operators. 
        private void drawNode(Canvas c, element currentNode, int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            logicGate logicGate;
            //Position of the node within the canvas. 
            double x, y; 
            //checking if a node is not a repeated input.
            if (currentNode.getUniqueness())
            {
                x = calculateNodeXposition(currentNode, heightOfTree, depthWithinTree);
                y = calculateNodeYposition(heightOfTree, depthWithinTree, positionWithinLayer);
                logicGate = new logicGate(currentNode); 
                //Adding the link between the tree and the visual diagram. 
                currentNode.setLogicGate(logicGate);
                Canvas.SetLeft(logicGate, x);
                Canvas.SetTop(logicGate, y);
                logicGate.PreviewMouseDown += dragHandler; 
                c.Children.Add(logicGate);
            }
        }

        //Breadth first traversal that is used to place all of the nodes within the tree onto the canvas. 
        private void drawNodes(Canvas c, element root, int heightOfTree)
        {
            Queue<element> q = new Queue<element>();
            q.Enqueue(root);
            int depthWithinTree = 0;
            int positionWithinLayer = 0;
            int sizeOfQ = 0;
            element currentNode;
            while (q.Count != 0)
            {
                sizeOfQ = q.Count;
                while (sizeOfQ != 0)
                {
                    currentNode = q.Peek();
                    drawNode(c, currentNode, heightOfTree, depthWithinTree, positionWithinLayer);
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

        //Small function for drawing the wire that connects the input to the root node of the binary tree. 
        private wire drawOutputWire(Canvas c)
        {
            wire w = new wire();
            w.setStart(outputNode.getLogicGate().getInputForOutput());
            w.setEnd(rootNode.getLogicGate().getOutputPoint());
            w.draw(c, Brushes.Black);
            rootNode.getLogicGate().setParentWire(w);
            return w;
        }

        //Method that draws the output pin for the diagram. 
        private void drawOutput(Canvas c, int heightOfTree)
        {
            //Creating an element to represent the output. Unique elementID of -1. 
            //Limitation of program can only draw single input gates. 
            outputNode = new element(-1);
            logicGate logicGate = new logicGate(outputNode);
            outputNode.setLogicGate(logicGate);
            //The position of the output node is the same y-position as the root node and a small shift from the root node. 
            double x = translateNode(calculateXposition(0), heightOfTree) + (pixelsPerSquare * 10);
            double y = calculateNodeYposition(heightOfTree, 0, 0);
            Canvas.SetTop(logicGate, y);
            Canvas.SetLeft(logicGate, x);
            logicGate.PreviewMouseDown += dragHandler; 
            c.Children.Add(logicGate);
            logicGate.setLeftChildWire(drawOutputWire(c));
        }

        //public method that links UI to class, 'stitches' all of the methods together to give the drawn diagram. 
        public void drawDiagram(Canvas c, string inputExpression)
        {
            canvasHeight = c.ActualHeight;
            canvasWidth = c.ActualWidth;
            generateBinaryTreeFromExpression(inputExpression);
            int heightOfTree = getHeightOfTree(rootNode); 
            drawNodes(c, rootNode, heightOfTree);
            drawWires(c, rootNode, inputExpression);
            drawOutput(c, heightOfTree); 
        }

        #endregion 
        
        #region Truth table generation

        //Gives the result of the fully evaluated expression. 
        private int evaluateBooleanExpression(string binaryCombination, string inputExpression)
        {
            //Removing compound gates so that the expression can be evaluated properly.
            string flattenedExpression = removeCompoundGates(inputExpression);
            string postfix = ConvertInfixtoPostfix(flattenedExpression);
            int operand1;
            int operand2;
            int tmp;
            postfix = subsituteIntoExpression(binaryCombination, postfix);
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
                    tmp = evaluateSingleOperator(operand1, operand2, c);
                    evaluatedStack.Push(tmp + 48);
                }
                else
                {
                    operand1 = evaluatedStack.Pop();
                    operand2 = evaluatedStack.Pop();
                    tmp = evaluateSingleOperator(operand1, operand2, c);
                    evaluatedStack.Push(tmp);
                }
            }
            //Final item on the stack is the result of the evaluation. 
            return evaluatedStack.Pop();
        }

        //Returns the result of the operation of a boolean operation with two operands. 
        private int evaluateSingleOperator(int o1, int o2, char operation)
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

        private string subsituteIntoExpression(string binaryCombination, string inputExpression)
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
        private string ConvertIntintoBinaryString(int n, string booleanExpression)
        {
            return Convert.ToString(n, 2).PadLeft(getNumberOfInputs(booleanExpression), '0');
        }

        //counts the number of unique inputs within a boolean expression
        private int getNumberOfInputs(string booleanExpression)
        {
            int numberOfInputs = 0;
            string alreadyCounted = "";
            foreach (char token in booleanExpression)
            {
                //any letter is an input within the expression. Therfore add it to the counter. 
                if (char.IsLetter(token) && (!alreadyCounted.Contains(token)))
                {
                    numberOfInputs++;
                    alreadyCounted += token;
                }
            }
            return numberOfInputs;
        }

        //counts the number of operators within a boolean expression
        private int getNumberOfOperators(string booleanExpression)
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

        private string[] generateInputMap(string inputExpression)
        {
            int numberOfInputs = getNumberOfInputs(inputExpression);
            // 2^n is the number of possible binary combinations hence number of rows within table. 
            int numberOfRows = (int)Math.Pow(2, numberOfInputs);
            //the input map of the truth table. Stores the input columns of the truth table. 
            string[] inputMap = new string[numberOfRows];
            for (int i = 0; i < numberOfRows; i++)
            {
                inputMap[i] = ConvertIntintoBinaryString(i, inputExpression);
            }
            return inputMap;
        }

        private string[] generateOutputMap(string inputExpression)
        {
            string[] headers = generateTruthTableHeadersWithSteps(inputExpression);
            string[] inputMap = generateInputMap(inputExpression);
            int numberOfRows = (int)Math.Pow(2, getNumberOfInputs(inputExpression));
            //Number of columns within the table is always the number of inputs and number of operators. 
            int numberOfColumns = getNumberOfInputs(inputExpression) + getNumberOfOperators(inputExpression);
            string inputCombination;
            //array that handles only the output portion of the truth, this can be put together with the input map to form a complete table
            string[] outputMap = new string[inputMap.Length];
            //inputmap looks like ["000", "001"]
            //so subsitute each row into each column header and evaluate and that is the cell commpleted for the output map7
            for (int i = 0; i < numberOfRows; i++)
            {
                inputCombination = inputMap[i];
                outputMap[i] += getOutputRow(headers, inputCombination);
            }
            return outputMap;
        }

        private string getOutputRow(string[] headers, string inputCombination)
        {
            string outputRow = "";
            foreach (string header in headers)
            {
                outputRow += evaluateBooleanExpression(inputCombination, header) - 48;
            }
            return outputRow;
        }

        private string[] generateTruthTableHeadersWithSteps(string inputExpression)
        {
            int numberOfInputs = getNumberOfInputs(inputExpression);
            int numberOfOperators = getNumberOfOperators(inputExpression);
            string[] headers = new string[numberOfInputs + numberOfOperators];
            Stack<string> subExpressionStack = new Stack<string>();
            string postfix = ConvertInfixtoPostfix(inputExpression);
            string subexpression;
            string operand1;
            string operand2;
            string[] tmp = new string[numberOfInputs];
            int i = 0;
            //The inputs for the table. It only contains unique inputs. 
            foreach (char c in inputExpression)
            {
                if (!tmp.Contains(c.ToString()) && char.IsLetter(c))
                {
                    tmp[i] = c.ToString();
                    i++;
                }
            }
            //sorting into alphabetical order and adding to the headers.
            Array.Sort(tmp);
            Array.Copy(tmp, headers, tmp.Length);
            //Producing the headers for evaluation. 
            foreach (char c in postfix)
            {
                if (char.IsLetter(c))
                {
                    subExpressionStack.Push(c.ToString());
                }
                else
                {
                    if (c == '!')
                    {
                        operand1 = subExpressionStack.Pop();
                        subexpression = "(" + c + operand1 + ")";
                    }
                    else
                    {
                        operand1 = subExpressionStack.Pop();
                        operand2 = subExpressionStack.Pop();
                        subexpression = "(" + operand2 + c + operand1 + ")";
                    }
                    subExpressionStack.Push(subexpression);
                    headers[i] = subexpression;
                    i++;
                }
            }
            return headers;
        }

        private void drawTruthTableHeaders(Canvas c, string[] headers)
        {
            Label cell;
            Thickness border = new Thickness(2);
            FontFamily font = new FontFamily("Consolas");
            double cellWidth = 30; 
            double x = 20;
            foreach(string header in headers)
            {
                //Checking for inputs of the table
                if (header.Length != 1)
                {
                    cellWidth = (header.Length * 10) + 5;
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

        private void drawTruthTable(Canvas c, string[] headers, string[] outputMap)
        {
            Label cell;
            Thickness border = new Thickness(2);
            FontFamily font = new FontFamily("Consolas"); 
            double cellWidth = 30;
            double x = 20;
            double y = 50;
            foreach(string row in outputMap)
            {
                for(int i = 0; i < headers.Length; i++)
                {
                    //Checking for inputs of the table
                    if (headers[i].Length != 1)
                    {
                        cellWidth = (headers[i].Length * 10) + 5;
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
        public void drawTruthTable(Canvas c, string inputExpression, bool isSteps)
        {
            string[] headers = generateTruthTableHeadersWithSteps(inputExpression);
            string[] outputMap = generateOutputMap(inputExpression);
            int numberOfInputs = getNumberOfInputs(inputExpression);
            if (isSteps)
            {
                //drawTruthTable(c, headers, outputMap);
                drawTruthTableHeaders(c, headers);
                drawTruthTable(c, headers, outputMap); 
            }
            else
            {
                throw new NotImplementedException("Not yet buster!");
            }
        }
        #endregion

        #region Minimisation

        //Produces the final minimised expression from the essential prime implicants.
        private string convertEPIsToExpression(List<string> essentialPrimeImplicants)
        {
            //Converting each implicant into input form. Ie (-100 becomes B!C!D)
            essentialPrimeImplicants = essentialPrimeImplicants.ConvertAll(new Converter<string, string>(convertImplicantToExpression));
            //Each implicant is separated by OR gates. 
            string expression = string.Join(" + ", essentialPrimeImplicants);
            return expression;
        }

        //Replaces the binary form into the input form so that a complete boolean expression can be created. 
        private string convertImplicantToExpression(string epi)
        {
            //Removing regex characters to make conversion easier (due to "\d" being two characters long). 
            epi = epi.Replace(@"\d", "-");
            string tmp = "";
            char input;
            for (int i = 0; i < epi.Length; i++)
            {
                if (epi[i] == '1')
                {
                    //Each prime implicant in final expression is always sequential.
                    input = (char)(i + 65);
                    tmp += input;
                }
                //If 0 in implicant then the input is the complement (Ie, NOT gate). 
                else if (epi[i] == '0')
                {
                    input = (char)(i + 65);
                    tmp += "!" + input;
                }
            }
            return tmp;
        }

        //Evaluates the minterms to the prime implicants. This forms the basis of the prime implicant chart.
        private void setRegexPatterns(Dictionary<string, string> regex, List<string> minterms)
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
        private void convertImplicantsIntoRegex(Dictionary<string, string> regex, List<string> primeImplicants)
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
        private string mergeMinterms(string m1, string m2)
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
        private bool checkDashesAlign(string m1, string m2)
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
        private bool checkMintermDifference(string m1, string m2)
        {
            int minterm1 = removeDashes(m1);
            int minterm2 = removeDashes(m2);
            int res = minterm1 ^ minterm2;
            return (res != 0 && ((res & (res - 1)) == 0));
        }

        //Utilty function that temporarily removes the dashes from a binary pattern so that it can be compared. 
        private int removeDashes(string minterm)
        {
            return Convert.ToInt32(minterm.Replace('-', '0'), 2);
        }

        private List<string> getMinterms(string expression)
        {
            List<string> minterms = new List<string>();
            string[] inputMap = generateInputMap(expression);
            foreach (string input in inputMap)
            {
                //A minterm has been found if input results in the expresion evaluating to true. 
                if (evaluateBooleanExpression(input, expression) - 48 == 1)
                {
                    minterms.Add(input);
                }
            }
            return minterms;
        }

        //Counts the number of 1's within each place value of the values in the dictionary.
        //Frequency table is used to find the essential prime implicants. 
        private int[] getFrequencyTable(Dictionary<string, string> regex, List<string> minterms)
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
        private string getEssentialPrimeImplicant(Dictionary<string, string> regex, int pos)
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

        private List<string> getEssentialPrimeImplicants(Dictionary<string, string> regex, List<string> minterms)
        {
            //Calculating the number of 1's within each column of the values in the dictionary. 
            int[] bitFrequencyTable = getFrequencyTable(regex, minterms);
            List<string> essentialPrimeImplicants = new List<string>();
            string epi = ""; 
            for (int i = 0; i < bitFrequencyTable.Length; i++)
            {
                //If the total number of bits in one column of the values is 1, then only one prime implicant covers that minterm and hence it is an essential prime implicant. 
                if (bitFrequencyTable[i] == 1)
                {
                    epi = getEssentialPrimeImplicant(regex, i);
                    //Removing repeats to avoid cases such as "A+A" which can be further simplified. 
                    if (!essentialPrimeImplicants.Contains(epi))
                    {
                        essentialPrimeImplicants.Add(epi);
                    }
                }
            }
            return essentialPrimeImplicants;
        }

        //The following function carries out a recursive merging process where a merge can take place if dashes align and 1 bit differs between the two minterms. 
        //Any term that cannot be merged is a prime implicant and can be added to the return list. 
        private List<string> getPrimeImplicants(List<string> mintermList)
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
                    if (checkDashesAlign(m1, m2) && checkMintermDifference(m1, m2))
                    {
                        //merge minterms
                        mergedMinterm = mergeMinterms(m1, m2);
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
                return getPrimeImplicants(primeImplicants);
            }
        }

        //Implementation of the Quine-McCluskey algorithm for diagram/expression minimisation. Returns the minised expression by finding prime and essential prime implicants from merged minterms. 
        public string minimiseExpression(string expression)
        {
            //Finding prime implicants to get essential prime implicants. 
            List<string> minterms = getMinterms(expression);
            List<string> primeImplicants = getPrimeImplicants(minterms);
            //Creating the prime-implicant chart which is used to find the essential prime implicants. 
            Dictionary<string, string> PIchart = new Dictionary<string, string>();
            convertImplicantsIntoRegex(PIchart, primeImplicants);
            setRegexPatterns(PIchart, minterms);
            List<string> PIs = getEssentialPrimeImplicants(PIchart, minterms);
            string minimisedExpression = convertEPIsToExpression(PIs);
            return minimisedExpression;
        }
        #endregion
    }
}