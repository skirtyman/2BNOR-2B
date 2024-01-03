using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.RightsManagement;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace _2BNOR_2B
{
    class diagram
    {
        //the base operators within boolean logic. NAND and NOR not included as they are compound gates.
        private char[] booleanOperators = { '.', '^', '+', '!' };
        private string infixExpression=""; 
        //The root of the tree. Do not need array as the children are stored within the class itself. 
        private element rootNode;
        private wire[] wires;
        //The following attributes are the constants for the diagram drawing. 
        int elementWidth = 2;
        int xOffset = 12;
        int pixelsPerSquare = 15;
        double canvasWidth, canvasHeight;



        public diagram()
        {

        }

        private void generateBinaryTreeFromExpression(string inputExpression)
        {
            string postfixExpression = ConvertInfixtoPostfix(inputExpression);
            string[] names = { "and_gate", "xor_gate", "or_gate" };
            Stack<element> nodeStack = new Stack<element>();
            element nodeToAdd;
            element leftChild;
            element rightChild;
            int elementID = 0;
            int elementType;
            string elementName = "";
            //add an output pin to the tree as this the root node of the tree.
            foreach (char c in postfixExpression)
            {
                if (char.IsLetter(c) && char.IsUpper(c))
                {
                    //create an input pin
                    //fix the label generation 
                    nodeToAdd = new element(elementID, c.ToString());
                    nodeStack.Push(nodeToAdd);
                }
                else if (c == '!')
                {
                    rightChild = nodeStack.Pop();
                    //create a logic gate
                    elementType = Array.IndexOf(booleanOperators, c);
                    nodeToAdd = new element("not_gate", elementID, elementType, null, rightChild);
                    rightChild.parent = nodeToAdd;
                    nodeStack.Push(nodeToAdd);
                }
                else
                {
                    rightChild = nodeStack.Pop();
                    leftChild = nodeStack.Pop();
                    //create a logic gate
                    elementType = Array.IndexOf(booleanOperators, c);
                    elementName = names[elementType];
                    nodeToAdd = new element(elementName, elementID, elementType, leftChild, rightChild);
                    leftChild.parent = nodeToAdd;
                    rightChild.parent = nodeToAdd;
                    nodeStack.Push(nodeToAdd);
                }
                elementID++;
            }
            rootNode = nodeStack.Pop();
        }

        private int calculateHeightOfTree(element root)
        {
            if (root == null)
            {
                return 0;
            }

            int leftChildHeight = calculateHeightOfTree(root.leftChild);
            int rightChildHeight = calculateHeightOfTree(root.rightChild);

            if (leftChildHeight > rightChildHeight)
            {
                return leftChildHeight + 1;
            }
            else
            {
                return rightChildHeight + 1;
            }
        }

        private int findNumberOfNodes(element root)
        {
            if (root == null)
            {
                return 0;
            }
            return 1 + findNumberOfNodes(root.leftChild) + findNumberOfNodes(root.rightChild);
        }

        private double calculateStartYposition(int heightOfTree, int depthWithinTree)
        {
            return (Math.Pow(2, heightOfTree) / Math.Pow(2, depthWithinTree)) * pixelsPerSquare;
        }

        private double calculateNodeYposition(int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            double startY = calculateStartYposition(heightOfTree, depthWithinTree);
            return startY + (startY * positionWithinLayer * 2);
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

        private void drawWire(Canvas c, logicGate logicGate, logicGate child, bool isLeftChild)
        {
            wire w = new wire();
            if (isLeftChild)
            {
                w.setStart(logicGate.getInputPoint1());
                w.setEnd(child.getOutputPoint());
                w.draw(c, Brushes.Green); 
            }
            else
            {
                w.setStart(logicGate.getInputPoint2());
                w.setEnd(child.getOutputPoint());
                w.draw(c, Brushes.Red); 
            }
        }

        private void drawWires(Canvas c, element root)
        {
            Queue<element> q = new Queue<element>();
            q.Enqueue(root);
            while (q.Count != 0)
            {
                element tmp = q.Dequeue();
                if (tmp.leftChild != null)
                {
                    drawWire(c, tmp.getLogicGate(), tmp.leftChild.getLogicGate(), true);
                    q.Enqueue(tmp.leftChild);
                }
                if (tmp.rightChild != null)
                {
                    drawWire(c, tmp.getLogicGate(), tmp.rightChild.getLogicGate(), false); 
                    q.Enqueue(tmp.rightChild);
                }
            }
        }

        //function that carries out a breadth first traversal on the binary tree. Calculates the position of the nodes and draws them 
        //on the canvas. 
        /*
          Current cases that do not work: 
          
            Expressions with repeated inputs (A.B)^A
            Expressions with NOT gate (not offset properly, need to also offset all children)

            --nodes
            if the node is an input: 
                find label
                if label has been drawn 
                    do not draw the gate
                else 
                    draw the gate 

            --wires
            

        */
        

        private void drawNode(Canvas c, element currentNode, int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            string inputsDrawn = "";
            double x;
            double y; 
            if (currentNode.getElementType() == 6)
            {
                if (!inputsDrawn.Contains(currentNode.getLabel()))
                {
                    logicGate gate = new logicGate(currentNode); 
                    currentNode.setLogicGate(gate);
                    x = calculateNodeXposition(currentNode, heightOfTree, depthWithinTree);
                    y = calculateNodeYposition(heightOfTree, depthWithinTree, positionWithinLayer);
                    Canvas.SetTop(gate, y); 
                    Canvas.SetLeft(gate, x);
                    c.Children.Add(gate); 
                    inputsDrawn += currentNode.getLabel();
                }   
            }
            else
            {
                logicGate gate = new logicGate(currentNode);
                currentNode.setLogicGate(gate);
                x = calculateNodeXposition(currentNode, heightOfTree, depthWithinTree);
                y = calculateNodeYposition(heightOfTree, depthWithinTree, positionWithinLayer);
                Canvas.SetTop(gate, y);
                Canvas.SetLeft(gate, x);
                c.Children.Add(gate);
            }
        }

        private void drawNodes(Canvas c, element root, int heightOfTree)
        {
            Queue<element> q = new Queue<element>();
            q.Enqueue(root);
            int depthWithinTree = 0;
            int positionWithinLayer = 0;
            int sizeOfQ = 0;
            double x;
            double y;
            element currentNode;
            logicGate logicGate; 
            while (q.Count != 0)
            {
                sizeOfQ = q.Count;
                while (sizeOfQ != 0)
                {
                    currentNode = q.Peek();
                    drawNode(c, currentNode, heightOfTree, depthWithinTree, positionWithinLayer);
                    q.Dequeue(); 
                    positionWithinLayer++;
                    ////The coords of the node that the traversal is currently on. 
                    //y = calculateNodeYposition(heightOfTree, depthWithinTree, positionWithinLayer);
                    //x = calculateNodeXposition(currentNode, heightOfTree, depthWithinTree);

                    //logicGate = new logicGate(currentNode);
                    //currentNode.setLogicGate(logicGate);    
                    //Canvas.SetTop(logicGate, y);
                    //Canvas.SetLeft(logicGate, x);
                    //c.Children.Add(logicGate);
                    //q.Dequeue();
                    //positionWithinLayer++;
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

        public void drawDiagram(Canvas c, string inputExpression)
        {
            canvasHeight = c.ActualHeight;
            canvasWidth = c.ActualWidth;
            generateBinaryTreeFromExpression(inputExpression);
            int heightOfTree = calculateHeightOfTree(rootNode);
            inOrder(rootNode);
            MessageBox.Show(infixExpression); 
            drawNodes(c, rootNode, heightOfTree);
            drawWires(c, rootNode); 
        }

        private void inOrder(element root)
        {
            if (root.leftChild != null)
            {
                inOrder(root.leftChild);
            }

            infixExpression += root.getLabel(); 


            if (root.rightChild != null)
            {
                inOrder(root.rightChild);
            }
        }

        public void drawTableFromExpression(Canvas c, bool isSteps)
        {
            inOrder(rootNode);
            drawTruthTable(c, infixExpression, isSteps);
        }


        //implementation of the 'Shunting Yard' algorithm for boolean expressions. 
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

        private int evaluateBooleanExpression(int[] binaryCombination, string inputExpression)
        {
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
                    //xor with 1 flips the bit provided. This is the same as using a not gate. Adding 48 to get correct char
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
            return evaluatedStack.Pop();
        }

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
                    binaryDigit = binaryCombination[c - 65].ToString();
                    inputExpression = inputExpression.Replace(c.ToString(), binaryDigit);
                }
            }
            return inputExpression;
        }

        //counts the number of inputs given a boolean expression
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

        //gets the number of operators within a boolean expression
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

        private string ConvertIntintoBinaryString(int n, string booleanExpression)
        {
            return Convert.ToString(n, 2).PadLeft(getNumberOfInputs(booleanExpression), '0');
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

        private string[] generateTTHeadersWithSteps(string inputExpression)
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
            foreach (char c in inputExpression)
            {
                if (!tmp.Contains(c.ToString()) && char.IsLetter(c))
                {
                    tmp[i] = c.ToString();
                    i++;
                }
            }
            //sorting into alphabetical and adding 
            Array.Sort(tmp);
            Array.Copy(tmp, headers, tmp.Length);

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
                        subExpressionStack.Push(subexpression);
                        headers[i] = subexpression;
                    }
                    else
                    {
                        operand1 = subExpressionStack.Pop();
                        operand2 = subExpressionStack.Pop();
                        subexpression = "(" + operand2 + c + operand1 + ")";
                        subExpressionStack.Push(subexpression);
                        headers[i] = subexpression;
                    }
                    i++;
                }
            }
            return headers;
        }

        //function that takes a 2d array and returns the first dimension
        private int[] findRowOfTable(int[,] map, int row)
        {
            int[] rowArray = new int[map.GetLength(0)];
            for (int i = 0; i < map.GetLength(1); i++)
            {
                rowArray[i] = map[row, i];
            }
            return rowArray;
        }

        private int[,] generateOutputMap(string inputExpression)
        {
            string[] headers = generateTTHeadersWithSteps(inputExpression);
            int[,] inputMap = generateInputMap(inputExpression);
            int numberOfRows = (int)Math.Pow(2, getNumberOfInputs(inputExpression));
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
                    inputCombination = findRowOfTable(inputMap, i);
                    //substitute input combination into header and evaluate. Fill the corresponding cell with result
                    outputMap[i, j] = evaluateBooleanExpression(inputCombination, header) - 48;
                }
            }
            return outputMap;
        }

        private int[] findColumnOfTable(int[,] outputMap, int column)
        {
            int[] columnArray = new int[outputMap.GetLength(1)];
            for (int j = 0; j< outputMap.GetLength(0); j++)
            {
                columnArray[j] = outputMap[j, column]; 
            }
            return columnArray; 
        }

        private void drawTableWithSteps(Canvas c, string[] headers, int[,] outputMap)
        {
            Label cell;
            Thickness border = new Thickness(2); 
            double x = 20;
            double y = 20;
            double cellWidth=0;
            int index = 0; 
            foreach (string h in headers)
            {
                cell = new Label();
                cellWidth = 28;
                if (h.Length > 1)
                {
                    //cellWidth = headers[headers.Length - 1].Length * 10;
                    cellWidth = (headers[index].Length * 10) + 5;
                }
                cell.Width = cellWidth; 
                cell.BorderBrush = Brushes.LightGray;
                cell.BorderThickness = border;
                cell.Background = Brushes.White;
                cell.FontFamily = new FontFamily("Consolas");
                cell.FontSize = 14;
                cell.Content = h;
                Canvas.SetTop(cell, y);
                Canvas.SetLeft(cell, x);
                c.Children.Add(cell);
                x += cell.Width;
                index++;
            }
            x = 20;
            y = 50; 
            for (int i = 0; i < headers.Length; i++)
            {
                for (int j = 0; j < outputMap.GetLength(0); j++)
                {
                    cell = new Label();
                    cellWidth = 28;
                    if (headers[i].Length != 1)
                    {
                        cellWidth = (headers[i].Length * 10) + 5;
                    }
                    cell.Width = cellWidth; 
                    cell.BorderBrush = Brushes.LightGray;
                    cell.BorderThickness = border;
                    cell.Background = Brushes.White;
                    cell.FontFamily = new FontFamily("Consolas");
                    cell.FontSize = 14;
                    cell.Content = outputMap[j, i]; 
                    Canvas.SetTop(cell, y);
                    Canvas.SetLeft(cell, x);
                    c.Children.Add(cell); 
                    y+= 30;
                }
                x += cellWidth;
                y = 50; 
            }

        }

        public void drawTruthTable(Canvas c, string inputExpression, bool isSteps)
        {
            string[] headers = generateTTHeadersWithSteps(inputExpression);
            int[,] outputMap = generateOutputMap(inputExpression);
            int numberOfInputs = getNumberOfInputs(inputExpression);
            if (isSteps)
            {
                drawTableWithSteps(c, headers, outputMap);
            }
            else
            {
                MessageBox.Show("Not yet buster!"); 
                //drawTableNoSteps(c, headers, outputMap, numberOfInputs);
            }
        }
    }
}