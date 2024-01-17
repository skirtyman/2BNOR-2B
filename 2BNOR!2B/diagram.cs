using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.RightsManagement;
using System.Windows;
using System.Windows.Controls;
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
        private string infixExpression = ""; 
        //The root of the tree. Do not need array as the children are stored within the class itself. 
        private element rootNode;
        //Array to store the input elements within the tree. This is set when the wires are being drawn within the diagram. 
        private element[] elements; 
        private wire[] wires;
        //The following attributes are the constants for the diagram drawing. These can be edited to change the look of diagrams. 
        //These values are ones that I have found to produce the nicest diagrams from testing. 
        int elementWidth = 2;
        int xOffset = 12;
        int pixelsPerSquare = 15;
        double canvasWidth, canvasHeight;

        public diagram()
        {

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

        private void generateBinaryTreeFromExpression(string inputExpression)
        {
            string postfixExpression = ConvertInfixtoPostfix(inputExpression);
            string[] names = { "and_gate", "xor_gate", "or_gate" };
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
                    elementName = names[Array.IndexOf(booleanOperators, c)];
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
            return startX-maxX+50; 
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

        private void drawWiresForRightChildren(Canvas c,  element root)
        {
            wire w = new wire();
            logicGate rootLogicGate = root.getLogicGate();
            logicGate rightchildLogicGate = root.rightChild.getLogicGate();
            element input; 
            w.setStart(rootLogicGate.getInputPoint2());
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

        //public method that links UI to class, 'stitches' all of the methods together to give the drawn diagram. 
        public void drawDiagram(Canvas c, string inputExpression)
        {
            canvasHeight = c.ActualHeight;
            canvasWidth = c.ActualWidth;
            generateBinaryTreeFromExpression(inputExpression);
            int heightOfTree = getHeightOfTree(rootNode); 
            drawNodes(c, rootNode, heightOfTree);
            drawWires(c, rootNode, inputExpression); 
        }

        #endregion 

        #region Truth table generation

        //Gives the result of the fully evaluated expression. 
        private int evaluateBooleanExpression(int[] binaryCombination, string inputExpression)
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

        private string subsituteIntoExpression(int[] binaryCombination, string inputExpression)
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

        //function that takes a 2d array and returns the first dimension of a defined row.
        private int[] getRowOfTable(int[,] map, int row)
        {
            int[] rowArray = new int[map.GetLength(0)];
            for (int i = 0; i < map.GetLength(1); i++)
            {
                rowArray[i] = map[row, i];
            }
            return rowArray;
        }

        //function that takes a 2d array and returns the first dimension of a defined column. 
        private int[] getColumnOfTable(int[,] outputMap, int column)
        {
            int[] columnArray = new int[outputMap.GetLength(1)];
            for (int j = 0; j < outputMap.GetLength(0); j++)
            {
                columnArray[j] = outputMap[j, column];
            }
            return columnArray;
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

        private int[,] generateInputMap(string inputExpression)
        {
            int numberOfInputs = getNumberOfInputs(inputExpression);
            // 2^n is the number of possible binary combinations hence number of rows within table. 
            int numberOfRows = (int)Math.Pow(2, numberOfInputs);
            //binary string representation of the integer. Each character forms a cell in the truth table. 
            string inputBinaryCombination;
            //the input map of the truth table. Stores the input columns of the truth table. 
            int[,] inputMap = new int[numberOfRows, numberOfInputs];
            for (int i = 0; i < numberOfRows; i++)
            {
                inputBinaryCombination = ConvertIntintoBinaryString(i, inputExpression);
                //adding the correct binary digit into the respective column. 
                for (int j = 0; j < inputBinaryCombination.Length; j++)
                {
                    int binaryDigit = inputBinaryCombination[j] - 48;
                    inputMap[i, j] = binaryDigit;
                }
            }
            return inputMap;
        }

        private int[,] generateOutputMap(string inputExpression)
        {
            string[] headers = generateTruthTableHeadersWithSteps(inputExpression);
            int[,] inputMap = generateInputMap(inputExpression);
            int numberOfRows = (int)Math.Pow(2, getNumberOfInputs(inputExpression));
            //Number of columns within the table is always the number of inputs and number of operators. 
            int numberOfColumns = getNumberOfInputs(inputExpression) + getNumberOfOperators(inputExpression);
            int[] inputCombination;
            string header;
            //array that handles only the output portion of the truth, this can be put together with the input map to form a complete table
            int[,] outputMap = new int[numberOfRows, numberOfColumns];
            //inputmap looks like [[0,0,0],[0,0,1]] etc
            //so subsitute each row into each column header and evaluate and that is the cell commpleted for the output map7
            for (int j = 0; j < numberOfColumns; j++)
            {
                header = headers[j];
                for (int i = 0; i < numberOfRows; i++)
                {
                    //find the corresponding input combination. 
                    inputCombination = getRowOfTable(inputMap, i);
                    //substitute input combination into header and evaluate. Fill the corresponding cell with result
                    outputMap[i, j] = evaluateBooleanExpression(inputCombination, header) - 48;
                }
            }
            return outputMap;
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

        private void drawTruthTable(Canvas c, string[] headers, int[,] outputMap)
        {
            Label cell;
            Thickness border = new Thickness(2);
            FontFamily font = new FontFamily("Consolas"); 
            double x = 20;
            double y = 20;
            double cellWidth = 30;
            for (int i = 0; i < headers.Length; i++)
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
                cell.Content = headers[i];
                Canvas.SetTop(cell, y);
                Canvas.SetLeft(cell, x);
                c.Children.Add(cell);
                y += 30;
                for (int j = 0; j < outputMap.GetLength(0); j++)
                {
                    cell = new Label();
                    cell.HorizontalContentAlignment = HorizontalAlignment.Center;
                    cell.Width = cellWidth;
                    cell.BorderBrush = Brushes.LightGray;
                    cell.BorderThickness = border; 
                    cell.Background = Brushes.White;
                    cell.FontFamily = font;
                    cell.FontSize = 14;
                    cell.Content = outputMap[j, i];
                    Canvas.SetTop(cell, y);
                    Canvas.SetLeft(cell, x);
                    c.Children.Add(cell);
                    y += 30;
                }
                x += cell.Width;
                y = 20;
            }
        }

        //Links class to UI, used to draw the truth tables to the canvas. 
        public void drawTruthTable(Canvas c, string inputExpression, bool isSteps)
        {
            string[] headers = generateTruthTableHeadersWithSteps(inputExpression);
            int[,] outputMap = generateOutputMap(inputExpression);
            int numberOfInputs = getNumberOfInputs(inputExpression);
            if (isSteps)
            {
                drawTruthTable(c, headers, outputMap);
            }
            else
            {
                MessageBox.Show("Not yet buster!"); 
            }
        }
        #endregion
    }
}