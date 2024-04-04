using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using System.Security;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;

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
        // The main window canvas. This is the one where logic diagrams are drawn. 
        private Canvas c;
        // Constants used for the diagram drawing formulae. 
        // Square allocation for elements. 
        readonly int elementWidth = 2;
        readonly int xOffset = 12;
        // Number of pixels on the canvas each square takes up. 
        readonly int pixelsPerSquare = 15;
        readonly int maxNumberOfInputs = 12;
        readonly int minCellWidth = 30;
        readonly double initialXofTable = 20;
        readonly double initialYofTable = 20; 
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
        /// Method to check the position of brackets within boolean expression.
        /// </summary>
        /// <param name="expression">The boolean expression being checked.</param>
        /// <returns>Whether or not there are erroneous brackets within the expression</returns>
        private static bool CheckBracketPosition(string expression)
        {
            // Regular expression to check for empty brackets within the expression.
            var bracketCheck = new Regex(@"\(\)");
            var match = bracketCheck.Match(expression);
            if (match.Success)
            {
                return true;
            }
            else
            {
                var gateCheck = new Regex(@"\([!]?[A-Z0-1]*[.+^][!]?[A-Z0-1]\)");
                match = gateCheck.Match(expression);
                if (match.Success)
                {
                    var operandCheck = new Regex(@"\([!]?[A-Z0-1][.+^][!]?[A-Z0-1]\)");
                    match = operandCheck.Match(match.Value);
                    if (match.Success)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
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
            var input = new List<char>();
            foreach (char c in expression)
            {
                // Ensuring only unique inputs are added to the array. 
                if (char.IsLetter(c))
                {
                    input.Add(c);
                }
            }
            // Returning the unique inputs of expression. 
            return input.Distinct().ToArray();
        }


        /// <param name="expression">The user-entered boolean expression being validated.</param>
        /// <returns>The number of constants (0 or 1) in the entered boolean expression.</returns>
        private static int GetNumberOfConstants(string expression)
        {
            int numberOfConstants = 0;
            foreach (char c in expression)
            {
                if (c == '0' || c == '1')
                {
                    numberOfConstants++;
                }
            }
            return numberOfConstants;
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
        public bool IsExpressionValid(string expression)
        {
            string removedWhitespace = RemoveWhitespace(expression, ""); 
            if (removedWhitespace == "")
            {
                return false; 
            }
            if (IsSequential(removedWhitespace) && InvalidCharacters(removedWhitespace) && ValidateBrackets(removedWhitespace))
            {
                // Imposing an input limit for Petrick's method. This is a choice because 
                // it means that the term to implicant map has a unique character as the
                // key. 
                int numberOfInputs = GetNumberOfInputs(removedWhitespace, false) - GetNumberOfConstants(removedWhitespace);
                if (numberOfInputs > maxNumberOfInputs)
                {
                    return false;
                }
                else
                {
                    string postfix = ConvertInfixtoPostfix(removedWhitespace);
                    // Ensuring that the postfix is valid postfix. Covers expressions 
                    // such as "(A.)" which pass the other checks. 
                    bool postfixCheck = PostfixCheck(postfix);
                    return postfixCheck;
                    //if (CheckBracketPosition(removedWhitespace))
                    //{
                    //    return false;
                    //}
                    //else
                    //{
                    //    return postfixCheck;
                    //}
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
                    // Get the state if the node the current traversal is on, is an input. 
                    if (root.GetElementName() == "input_pin")
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
            throw new Exception("Could not find element. ");
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
                        tmp = GetInputWithSameLabel(c); 
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

        /// <summary>
        /// Calculates the y-position of the logic gate on the canvas given the logic 
        /// gate diagram being drawn. 
        /// </summary>
        /// <param name="heightOfTree">The height of the tree. This is the deepest node in
        /// the tree. </param>
        /// <param name="depthWithinTree">Integer which stores the current layer that the 
        /// node is on. This is also the layer that the BST is on. </param>
        /// <param name="positionWithinLayer">The x-position within the tree at the 
        /// depth given by depthWithinTree</param>
        /// <returns>The Y-position of the node on the canvas given its position 
        /// within the tree. </returns>
        private double CalculateNodeYposition(int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            // Formula to calculate the spacing for a node in this position within tree
            // in relation to the size of the tree. 
            double initialY = Math.Pow(2, heightOfTree) / Math.Pow(2, depthWithinTree) * pixelsPerSquare;
            // Multiply by the position within the layer to get the Y position of the node.
            // This is the same idea as the X position calculation. 
            initialY += initialY * positionWithinLayer * 2;
            // Apply a squish to the Y position so the size of the diagram is reduced.
            // This makes odd shaped diagrams more easily viewable. 
            if (heightOfTree > 5 && depthWithinTree < 4)
            {
                return initialY / 2;
            }
            else
            {
                return initialY;
            }
        }

        /// <summary>
        /// Calculates the X position of a logic gate on the canvas. This is simply 
        /// applying a shift factor across the canvas based off of the depth within the tree. 
        /// </summary>
        /// <param name="depthWithinTree">The depth within the tree, the current node is. </param>
        /// <returns>The X position on the canvas which is the location of logic gate.</returns>
        private double CalculateXposition(int depthWithinTree)
        {
            return canvasWidth - ((pixelsPerSquare - 7) * elementWidth + (pixelsPerSquare - 7) * xOffset) * depthWithinTree;
        }

        /// <summary>
        /// Translates the node across the canvas that ensures the logic gates do not
        /// spill over the left side of the canvas. 
        /// </summary>
        /// <param name="startX">The start position of the node on the canvas.</param>
        /// <param name="heightOfTree">The height of the binary tree being drawn.</param>
        /// <returns>The translated position of the node on the canvas. This is the
        /// final position of the node on the canvas. </returns>
        private double TranslateNode(double startX, int heightOfTree)
        {
            double maxX = CalculateXposition(heightOfTree);
            return startX - maxX + 50;
        }

        /// <summary>
        /// Linking method which calculates the final position of the node on the canvas. 
        /// </summary>
        /// <param name="node">The node being drawn to the canvas. </param>
        /// <param name="heightOfTree">The height of the binary tree being drawn.</param>
        /// <param name="depthWithinTree">The current depth of the node within the tree.</param>
        /// <returns></returns>
        private double CalculateNodeXposition(Element node, int heightOfTree, int depthWithinTree)
        {
            double xPosition;
            // If the node is an input(leaf node) then it should be drawn at the 
            // left-most position of the tree. This is the height of the binary tree.
            if (node.leftChild == null && node.rightChild == null)
            {
                xPosition = CalculateXposition(heightOfTree);
            }
            else
            {
                // Calculating at that particular depth. 
                // Subtracting 50 pixels to remain within the bounds of the canvas. 
                xPosition = CalculateXposition(depthWithinTree) - 50;
            }
            // Translating the node to stay within the bounds of the canvas. 
            return TranslateNode(xPosition, heightOfTree);
        }

        /// <summary>
        /// Utility method which is responsible for drawing the left child of the node. 
        /// The method simply sets the points for the wires. It is important to note that
        /// intersections between the wires are not considered. 
        /// </summary>
        /// <param name="root">The wire connects to the input of this node.</param>
        /// <returns>A wire with its points set. Ready to be drawn to the canvas. </returns>
        private Wire DrawWiresForLeftChildren(Element root)
        {
            var wire = new Wire(c);
            LogicGate rootLogicGate = root.GetLogicGate();
            LogicGate leftchildLogicGate = root.leftChild.GetLogicGate();
            Element input;
            wire.SetStart(rootLogicGate.GetInputPoint1());
            // If the left child does not have a logic gate then it must be a repeated 
            // node and so the input with the same label needs to be found. 
            if (leftchildLogicGate != null)
            {
                wire.SetRepeated(false); 
                wire.SetEnd(leftchildLogicGate.GetOutputPoint());
                wire.SetGate(leftchildLogicGate);
                wire.SetShift(leftchildLogicGate.GetConnectedWires());
                wire.SetPoints();
            }
            else
            {
                wire.SetRepeated(true); 
                // Searching for the input with the same label as this is the visual 
                // node that the wire needs to be drawn to. 
                input = GetInputWithSameLabel(root.leftChild.GetLabel());
                wire.SetEnd(input.GetLogicGate().GetOutputPoint());
                wire.SetGate(input.GetLogicGate());
                input.GetLogicGate().AddWire();
                // Applying a shift value to separate the wires going to the same 
                // gate. This makes the diagram clearer.
                wire.SetShift(input.GetLogicGate().GetConnectedWires());
                // Setting the points but not rendering so that the intersections 
                // can be calculated and included. 
                wire.SetPoints();
            }
            // Returning the wire so that it can be added to the array of wires. 
            return wire;
        }

        /// <summary>
        /// Utility method which is resonsible draw drawing the right child of the node. 
        /// The method simply sets the points for the wires. It is important to note that 
        /// intersections between the wires are not considered.
        /// </summary>
        /// <param name="root">The wire connects to the input of this node.</param>
        /// <returns>A wure with its points set. Ready to be drawn to the canvas. </returns>
        private Wire DrawWiresForRightChildren(Element root)
        {
            var wire = new Wire(c);
            LogicGate rootLogicGate = root.GetLogicGate();
            LogicGate rightchildLogicGate = root.rightChild.GetLogicGate();
            Element input;
            wire.SetStart(rootLogicGate.GetInputPoint2());
            // If the left child does not have a logic gate then it must be a repeated 
            // node and so the input with the same label needs to be found. 
            if (rightchildLogicGate != null)
            {
                wire.SetRepeated(false); 
                wire.SetEnd(rightchildLogicGate.GetOutputPoint());
                wire.SetGate(rightchildLogicGate);
                wire.SetShift(rightchildLogicGate.GetConnectedWires());
                wire.SetPoints();
            }
            else
            {
                wire.SetRepeated(true); 
                // Searching for the input with tha same label as this is the visual 
                // node that the wire needs to be drawn to. 
                input = GetInputWithSameLabel(root.rightChild.GetLabel());
                wire.SetEnd(input.GetLogicGate().GetOutputPoint());
                wire.SetGate(input.GetLogicGate());
                input.GetLogicGate().AddWire();
                // Applying a shift value to separate the wires going to the same 
                // gate. This makes the diagram clearer.
                wire.SetShift(input.GetLogicGate().GetConnectedWires());
                // Setting the points but not rendering so that the intersections 
                // can be calculated and included.
                wire.SetPoints();
            }
            // Returning the wire so that it can be added to the array of wires. 
            return wire;
        }

        /// <summary>
        /// Calculates the point of intersection, if any, given 4 points which 
        /// corresponds to 2 lines that are on the canvas. These are always a
        /// vertical and horizontal line as this is the only case where an 
        /// intersection would have to be drawn. 
        /// </summary>
        /// <param name="p0">The start point of the vertical line.</param>
        /// <param name="p1">The end point of the vertical line.</param>
        /// <param name="p2">The start point of the horizontal line.</param>
        /// <param name="p3">The end point of the horizontal line.</param>
        /// <returns>The point on the canvas where the intersection is located.</returns>
        private static Point? FindIntersection(Point p0, Point p1, Point p2, Point p3)
        {
            double s_x = p1.X - p0.X;
            double s_y = p1.Y - p0.Y;
            double s2_x = p3.X - p2.X;
            double s2_y = p3.Y - p2.Y;
            double denom = s_x * s2_y - s2_x * s_y;

            if (denom == 0)
            {
                // The line supplied are collinear. 
                return null;
            }
            bool isDenomPositive = denom > 0;

            double s3_x = p0.X - p2.X;
            double s3_y = p0.Y - p2.Y;
            double s_numer = s_x * s3_y - s_y * s3_x;

            if (s_numer < 0 == isDenomPositive)
            {
                // There is no intersection between the two lines. 
                return null;
            }

            double t_numer = s2_x * s3_y - s_x * s3_y;

            if (t_numer < 0 == isDenomPositive)
            {
                // There is no intersection between the two lines. 
                return null;
            }

            if (s_numer > denom == isDenomPositive || t_numer > denom == isDenomPositive)
            { 
                // There is no intersection between the lines. 
                return null;
            }

            // There must be an intersection between the two lines. 
            double t = t_numer / denom;
            // Finding the position and returning the point of intersection that has been
            // found between the two lines. 
            Point? result = new Point(p0.X + t * s_x, p0.Y + t * s_y);
            return result;
        }

        /// <summary>
        /// Computes all of the intersections within the logic gate diagram being drawn. 
        /// This is done by iterating through all of the line segments which make up a 
        /// wire. It should be noted that the intersections are not drawn, they are only
        /// added to the necessary wires' list of points. 
        /// </summary>
        private void DrawIntersections()
        {
            // List that stores sets of points which create horizontal lines. 
            // This is all of the horizontal segments of the wires. 
            var horizontalLines = new List<Point>();
            // List that stores all of the vertical line segments of the wires. 
            var verticalLines = new List<Point>();
            Point? intersection;
            Wire verticalWire;
            Wire horizontalWire;
            LogicGate gate;
            LogicGate gate1;
            // Add all of the horizontal and vertical points to the respective lists.
            for (var j = 0; j < wires.Length - 1; j++)
            {
                horizontalLines.AddRange(wires[j].GetPoints(true));
                verticalLines.AddRange(wires[j].GetPoints(false));
            }

            for (var i = 0; i < verticalLines.Count - 1; i += 2)
            {
                for (var c = 0; c < horizontalLines.Count - 1; c += 2)
                {
                    // Calculating the intersection, null => an intersection does not exist
                    // between these 2 lines. 
                    intersection = FindIntersection(verticalLines[i], verticalLines[i + 1], horizontalLines[c], horizontalLines[c + 1]);
                    // Ensuring the intersection exists and the intersection is not formed
                    // a horizontal and vertical line of the same wire before the intersection
                    // is added. 
                    verticalWire = FindWire(verticalLines[i]);
                    horizontalWire = FindWire(horizontalLines[c]);
                    gate = verticalWire.GetGate();
                    gate1 = horizontalWire.GetGate();
                    if (intersection != null && verticalWire != horizontalWire)
                    {
                        if (gate.GetGate().GetLabel() != gate1.GetGate().GetLabel())
                        {
                            verticalWire.AddBridge(intersection);
                        }
                        else
                        {
                            verticalWire.AddJunction(intersection); 
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Carries out a linear search on the wires drawn, given a point. 
        /// </summary>
        /// <param name="p">The point p which identifies the wire being searched for. </param>
        /// <returns>The wire which contains the provided point. </returns>
        /// <exception cref="Exception">A wire containing the supplied point could not be found. 
        /// </exception>
        private Wire FindWire(Point p)
        {
            List<Point> points;
            foreach (Wire wire in wires)
            {
                // Get all points of the wire, both horizontal and vertical. 
                points = wire.GetPoints(null);
                if (points.Contains(p))
                {
                    return wire;
                }
            }
            throw new Exception("Could not find wire.");
        }

        /// <summary>
        /// Draws the wires to create the logic gate diagram. The states of the gates has 
        /// not been set so the colour of the wires are at their default state. 
        /// </summary>
        /// <param name="root">The root node of the binary tree. </param>
        private void DrawWires(Element root)
        {
            var q = new Queue<Element>();
            // The number of wires is always the number of nodes as every node has 
            // a wire that is drawn from the nodes output. 
            wires = new Wire[GetNumberOfNodes(root)];
            Element tmp;
            int i = 0;
            // Traversing the nodes using a breadth first traveral to add the wires 
            // in that order. 
            q.Enqueue(root);
            while (q.Count != 0)
            {
                tmp = q.Dequeue();
                // Traversing the left child if it exists. 
                if (tmp.leftChild != null)
                {
                    // Adding the wire created into the array. 
                    wires[i] = DrawWiresForLeftChildren(tmp);
                    i++;
                    // Traversing the left child becuase it exists.
                    q.Enqueue(tmp.leftChild);
                }
                // Traversing the right child if it exists. 
                if (tmp.rightChild != null)
                {
                    // Adding the created wire into the array.
                    wires[i] = DrawWiresForRightChildren(tmp);
                    i++;
                    // Traversing the right child becuase it exists.
                    q.Enqueue(tmp.rightChild);
                }
            }
            // Intersections have not been considered up until this point. 
            // Compute all of the intersections of the currently drawn wires.
            DrawIntersections();
            // Adding the wires to the canvas as all of the points have been calculated.
            for (var j = 0; j < wires.Length - 1; j++)
            {
                wires[j].RenderLine();
            }
        }

        /// <summary>
        /// Small function that colours all of the wires when a change of state has 
        /// occurred. This is to show the user the states transmit throughout the 
        /// diagram. 
        /// </summary>
        private void ColourWires()
        {
            foreach (Wire wire in wires)
            {
                // Every wire is connected to a logic gate and so we can get the 
                // element behind it to colour the state of the wire appropriately. 
                LogicGate logicGate = wire.GetGate();
                Element node = logicGate.GetGate();
                // Setting the colour of the wire. 
                if (node.GetState() == 1)
                {
                    wire.SetColour(Brushes.Green);
                }
                else
                {
                    wire.SetColour(Brushes.Red);
                }
            }
        }

        /// <summary>
        /// Adds a node to the diagram canvas based off of the position of the node 
        /// within the tree. It also readjusts the canvas to make the scrollviewers
        /// visible when the diagram being drawn is too large. 
        /// </summary>
        /// <param name="currentNode">The node behind the visual node being added to 
        /// the canvas. </param>
        /// <param name="heightOfTree">The height of the binary tree being drawn.</param>
        /// <param name="depthWithinTree">The layer within the tree that the node being 
        /// drawn is on. </param>
        /// <param name="positionWithinLayer">The horizonntal position within the layer 
        /// of the tree that the node is on. </param>
        private void DrawNode(Element currentNode, int heightOfTree, int depthWithinTree, int positionWithinLayer)
        {
            LogicGate logicGate;
            double xPosition; 
            double yPosition;
            // Ensuring that the node exists. 
            if (currentNode.GetInstances() != 0)
            {
                // Calculating the x-position of the node on the canvas. This is the 
                // same value in most cases. Unless the node is an input, but this 
                // does not make a huge difference to the methods usage here. 
                xPosition = CalculateNodeXposition(currentNode, heightOfTree, depthWithinTree);
                // Check to show that the child of the NOT gate is drawn in parallel 
                if (currentNode.parent != null && currentNode.parent.GetElementName() == "not_gate" && currentNode.GetElementName() == "input_pin")
                {
                    // The y-position is simply the same as the parent. 
                    yPosition = Canvas.GetTop(currentNode.parent.GetLogicGate());
                }
                else
                {
                    // Otherwise calculate the y-position based off of the position of 
                    // the node within the binary tree. 
                    yPosition = CalculateNodeYposition(heightOfTree, depthWithinTree, positionWithinLayer);
                }
                // Creating a visual gate because the node is not a repeated input. 
                logicGate = new LogicGate(currentNode);
                // Linking the visual and non-visual node together. 
                currentNode.SetLogicGate(logicGate);
                // Setting the position of the logic gate onto the canvas, readjusting the
                // canvas size for the scroll viewer and then adding the visual element to 
                // the canvas. 
                Canvas.SetLeft(logicGate, xPosition);
                Canvas.SetTop(logicGate, yPosition);
                c.Height = Math.Max(yPosition, c.Height) + 30;
                c.Width = Math.Max(xPosition, c.Width) + 30;
                double position = logicGate.GetInputPoint2().Y + 50;
                Canvas.SetBottom(logicGate, position);
                Canvas.SetRight(logicGate, logicGate.GetInputPoint2().X);
                // Setting a large Zindex value so that the wires are not drawn over the 
                // logic gates on the canvas as the gates are drawn first. 
                Panel.SetZIndex(logicGate, 3);
                c.Children.Add(logicGate);
            }
        }

        /// <summary>
        /// A modified breadth first traversal that is responsible for placing all of the
        /// logic gates onto the diagram canvas. The modification is that the traversal 
        /// also tracks the horizontal position within a given depth of the binary tree. 
        /// This allows the y-position to be calculated.
        /// </summary>
        /// <param name="root">The root node of the binary tree. This is the last logic 
        /// gate.</param>
        /// <param name="heightOfTree">The height of the tree given. This is the maximum
        /// depth of the tree. </param>
        private void DrawNodes(Element root, int heightOfTree)
        {
            var q = new Queue<Element>();
            q.Enqueue(root);
            int depthWithinTree = 0;
            // The horizontal position within a given depth of the tree. 
            int positionWithinLayer = 0;
            int sizeOfQ;
            Element currentNode;
            while (q.Count != 0)
            {
                sizeOfQ = q.Count;
                // Travelling along a particular layer of the binary tree. 
                while (sizeOfQ != 0)
                {
                    currentNode = q.Peek();
                    // Draw node at the current position of the breadth first traversal. 
                    DrawNode(currentNode, heightOfTree, depthWithinTree, positionWithinLayer);
                    q.Dequeue();
                    // Travelling horizontally along the tree. 
                    positionWithinLayer++;
                    // If the left child exists then add it to the queue to be reached. 
                    if (currentNode.leftChild != null)
                    {
                        q.Enqueue(currentNode.leftChild);
                    }
                    // If the right child exists then add it to the queue to be reached.
                    if (currentNode.rightChild != null)
                    {
                        q.Enqueue(currentNode.rightChild);
                    }
                    sizeOfQ--;
                }
                // Next level of the tree so increase the depth and reset the position 
                // within the layer to the left-most node => 0. 
                depthWithinTree++;
                positionWithinLayer = 0;
            }
        }

        /// <summary>
        /// Carries out an iterative postorder traversal on the binary tree. This 
        /// is to set the states of the elements in the tree to the corresponding 
        /// cell in the truth table, given the states of the inputs in the diagram.
        /// </summary>
        /// <param name="root">The root node of the binary tree. </param>
        private void AssignGateStates(Element root)
        {
            // Get the correct row of the truth table based off of the state of the 
            // inputs in the diagram. 
            string tableRow = GetTruthTableRow();
            var s = new Stack<Element>();
            int i = 0;
            int state; 
            // Iterative postorder traversal. 
            while (true)
            {
                while (root != null)
                {
                    s.Push(root);
                    s.Push(root);
                    // Traverse down the left sub-tree until no longer. 
                    root = root.leftChild;
                }
                if (s.Count == 0)
                {
                    // The traversal is complete as no nodes remain on the stack. 
                    return;
                }
                root = s.Pop();
                if (s.Count != 0 && s.Peek() == root)
                {
                    // Traverse to the right sub-tree. 
                    root = root.rightChild;
                }
                else
                {
                    // Assign the state to the element. 
                    state = tableRow[i] - 48;
                    root.SetState(state);
                    i++;
                    root = null;
                }
            }
        }

        /// <summary>
        /// Draws the wire from the output node to the root node of the binay tree. 
        /// This shows the visual connection to the tree and the output node to the 
        /// user, which makes the diagram more complete. 
        /// </summary>
        private void DrawOutputWire()
        {
            var wire = new Wire(c);
            // Setting the start position of the wire as the input of the output pin 
            // and the end of the wire as the output point of the last gate in the
            // binary tree. 
            wire.SetStart(outputNode.GetLogicGate().GetInputForOutput());
            wire.SetEnd(rootNode.GetLogicGate().GetOutputPoint());
            wire.SetGate(rootNode.GetLogicGate());
            // Setting the coordinates of the wire and drawing it onto the canvas. 
            wire.SetPoints();
            wire.RenderLine();
            // Adding the wire to the wire list so that it can be coloured where
            // necessary. 
            wires[^1] = wire;
        }

        /// <summary>
        /// Method for drawing the output node onto the diagram canvas. The node is 
        /// not drawn within the main BFT as its state is only the state of the 
        /// last gate in the diagram and so it can simply be added retroactively.
        /// </summary>
        /// <param name="heightOfTree">The height of the binary tree being drawn. 
        /// This is used when calculating the position of the node.</param>
        private void DrawOutput(int heightOfTree)
        {
            // Assigning the only negative element ID. This means that it can be 
            // Identified uniquely. 
            outputNode = new Element(-1);
            // Assigning a logic gate as the output is always a visual gate. 
            var logicGate = new LogicGate(outputNode);
            outputNode.SetLogicGate(logicGate);
            // Can use inital values to find the position of the root node. An x-offset 
            // can simply be added as the y-position is the same. 
            double xPosition = TranslateNode(CalculateXposition(0), heightOfTree) + pixelsPerSquare * 8;
            double yPosition = CalculateNodeYposition(heightOfTree, 0, 0);
            // Setting the position of the logic gate and adding the gate to the canvas. 
            Canvas.SetTop(logicGate, yPosition);
            Canvas.SetLeft(logicGate, xPosition);
            Canvas.SetRight(logicGate, xPosition + logicGate.ActualWidth);
            c.Children.Add(logicGate);
        }

        /// <summary>
        /// Simple public method that ties together all of the private method for 
        /// diagram drawing. This method generates the tree from the validated
        /// user-entered expression. It also updates the wires into the default 
        /// state of 0. This shows the user clearly the diagram is interactive. 
        /// </summary>
        public void DrawDiagram()
        {
            canvasWidth = c.ActualWidth;
            GenerateBinaryTreeFromExpression(infixExpression);
            inputMap = GenerateInputMap(infixExpression, false);
            headers = GetHeaders(infixExpression, false);
            outputMap = GenerateOutputMap(infixExpression, headers, false);
            int heightOfTree = GetHeightOfTree(rootNode);
            DrawNodes(rootNode, heightOfTree);
            DrawWires(rootNode);
            DrawOutput(heightOfTree);
            DrawOutputWire();
            UpdateWires();
        }

        #endregion

        #region Truth table generation
        /// <summary>
        /// Evaluates a user-inputted infix boolean expression. This is done by first 
        /// converting the expression into postfix and then using a stack.
        /// </summary>
        /// <param name="binaryCombination">A string storing the input combination to be
        /// substituted into the postfix boolean expression. </param>
        /// <param name="inputExpression">The infix expression being evaluated. </param>
        /// <returns>The result of evaluating an infix boolean expression with a particular
        /// string of inputs. </returns>
        private int EvaluateBooleanExpression(string binaryCombination, string inputExpression)
        {
            string postfix = ConvertInfixtoPostfix(inputExpression);
            int operand1;
            int operand2;
            int tmp;
            string substitutedExpression = SubsituteIntoExpression(binaryCombination, postfix);
            var evaluatedStack = new Stack<int>();
            foreach (char c in substitutedExpression)
            {
                // An operand has been found. 
                if (char.IsNumber(c))
                {
                    evaluatedStack.Push(c);
                }
                else if (c == '!')
                {
                    // NOT is an unary operator so only pop one item off of the stack
                    operand1 = evaluatedStack.Pop();
                    // Applying logical is the same as applying an XOR of 1 as the 
                    // operand is only every one bit in length. 
                    evaluatedStack.Push(operand1 ^ 1);
                }
                else if (c == '^')
                {
                    // Pop two items for the XOR as it is a binary operator. 
                    operand1 = evaluatedStack.Pop();
                    operand2 = evaluatedStack.Pop();
                    // Carry out the operation and push the result to the stack. 
                    tmp = EvaluateSingleOperator(operand1, operand2, c);
                    evaluatedStack.Push(tmp + 48);
                }
                else
                {
                    // Pop two items for the gate as it is a binary operator. 
                    operand1 = evaluatedStack.Pop();
                    operand2 = evaluatedStack.Pop();
                    // Carry out the operation and push the result to the stack. 
                    tmp = EvaluateSingleOperator(operand1, operand2, c);
                    evaluatedStack.Push(tmp);
                }
            }
            // The final item on the stack is the result of the evaluation. 
            return evaluatedStack.Pop();
        }

        /// <summary>
        /// Simple function that calculates the supplied operation with the suppplied
        /// operands. This exists because of the typing issue between operations and 
        /// the char representing the operation being applied. This function is only
        /// used for the binary operators. 
        /// </summary>
        /// <param name="o1">The state of the first operand (input).</param>
        /// <param name="o2">That state of the second operand (input). </param>
        /// <param name="operation">The boolean operation being applied to the two 
        /// operands. </param>
        /// <returns>The result of the operation with the given operands.</returns>
        private static int EvaluateSingleOperator(int o1, int o2, char operation)
        {
            int result = 0;
            // AND gate.
            if (operation == '.')
            {
                result = o1 & o2;
            }
            // OR gate.
            else if (operation == '+')
            {
                result = o1 | o2;
            }
            // XOR gate
            else if (operation == '^')
            {
                result = o1 ^ o2;
            }
            return result;
        }

        /// <summary>
        /// Substitutes binary input into a boolean expression. This is for evaluation of a 
        /// postfix boolean expression. It should be noted that the position of the bit corresponds 
        /// to the letter it is substituted for. Ie, A => leftmost bit, B=> second bit along. 
        /// This means that the inputs within the expressions must be sequential within the 
        /// expression. 
        /// </summary>
        /// <param name="binaryCombination">The binary combination being substituted into
        /// the boolean expression. </param>
        /// <param name="inputExpression">The boolean expresion that the values are being
        /// substituted into. </param>
        /// <returns>The boolean expression with the inputs replaced with the corresponding 
        /// bits in the binary combination.</returns>
        private static string SubsituteIntoExpression(string binaryCombination, string inputExpression)
        {
            string binaryDigit;
            // Tokenising the expression. 
            foreach (char c in inputExpression)
            {
                // Token must be an input but not a constant in order for it to be substituted.
                if (char.IsLetter(c))
                {
                    // Finding the correct bit from the index of the letter. This is the reason 
                    // for sequential inputs in the expression. 
                    binaryDigit = binaryCombination[c - 65].ToString();
                    // Substituting for all of the same inputs. 
                    inputExpression = inputExpression.Replace(c.ToString(), binaryDigit);
                }
            }
            // The completely substituted expression. 
            return inputExpression;
        }

        /// <summary>
        /// Converts an integer n, into its binay string representation. This is padded to 
        /// ensure that it fits into a truth table. 
        /// </summary>
        /// <param name="n">The integer being converted into its binary representation.</param>
        /// <param name="booleanExpression">The boolean expression that the binary number 
        /// will be used in its truth table. </param>
        /// <param name="isUnique">Boolean value for whether the repeated inputs are being 
        /// considered within the count of the number of inputs. </param>
        /// <returns>The binary string representation of an integer n for a truth table. 
        /// This is padded to ensure that it fits into the columns of the table.</returns>
        private static string ConvertIntintoBinaryString(int n, string booleanExpression, bool isUnique)
        {
            // Finding the number of inputs to ensure the correct level of padding is used.
            int numOfInputs = GetNumberOfInputs(booleanExpression, isUnique);
            int remainder;
            string binaryString = ""; 
            // Repeat until n can no longer be divided 2 anymore. 
            while (n > 0)
            {
                // Finding the remainder when dividing by two. This is the next most 
                // significant bit. 
                remainder = n % 2;
                // Divide n by 2 to get next bit. 
                n /= 2;
                // Prepending as the remainder represents the most significant bit.
                binaryString = remainder.ToString() + binaryString; 
            }
            // Padding with zero to ensure the correct length. 
            return binaryString.PadLeft(numOfInputs, '0');
        }

        /// <summary>
        /// Used when colouring the wires when the diagram is interacted with. This returns
        /// the row of the truth table where the inputs are the same as the state of the 
        /// inputs within the logic gate diagram. 
        /// </summary>
        /// <returns>The entire row of the truth table based off of the states of the inputs
        /// within the drawn logic gate diagram. </returns>
        private string GetTruthTableRow()
        {
            // The row of the truth table to return is the same as the index within the 
            // input map of the same truth table. 
            return outputMap[Array.IndexOf(inputMap, inputStates)];
        }

        /// <summary>
        /// Gets the number of inputs within the supplied boolean expression. This count 
        /// can either be only the unique inputs counted or any input within the expression.
        /// It is important to note that constants are included in the count.
        /// </summary>
        /// <param name="booleanExpression">A boolean expression entered by the user.</param>
        /// <param name="isUnique">Boolean value, whether or not non-unique are counted.</param>
        /// <returns>The total number of unique or non-unique inputs within the boolean expression.
        /// </returns>
        private static int GetNumberOfInputs(string booleanExpression, bool isUnique)
        {
            int numberOfInputs = 0;
            // String to track the inputs that have already been counted in the unique
            // input count. 
            string alreadyCounted = "";
            // Tokenising the expression. 
            foreach (char token in booleanExpression)
            {
                // Ensuring the token is an input. This either be a letter or a number.
                if (char.IsLetter(token) || char.IsNumber(token))
                {
                    if (isUnique)
                    {
                        // Extra check to see if the input has already been 
                        // counted towards the total. 
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

        /// <summary>
        /// Finds the number of operators within a boolean expression. 
        /// </summary>
        /// <param name="booleanExpression">The boolean expression being computed.</param>
        /// <returns>The number of boolean operators within a supplied boolean expression.</returns>
        private int GetNumberOfOperators(string booleanExpression)
        {
            int numberOfOperators = 0;
            // Tokenising the expression. 
            foreach (char token in booleanExpression)
            {
                if (booleanOperators.Contains(token))
                {
                    numberOfOperators++;
                }
            }
            return numberOfOperators;
        }

        /// <summary>
        /// Computes all of the input combinations for a truth table. These can be used to
        /// computer the rest of the binary values in the truth table. 
        /// </summary>
        /// <param name="inputExpression">The boolean expression being tabulated. </param>
        /// <param name="isUnique">Whether or not the map is for a table with unique input
        /// headers. </param>
        /// <returns>All possible binary input combinations given a boolean expression.</returns>
        private static string[] GenerateInputMap(string inputExpression, bool isUnique)
        {
            int numberOfInputs = GetNumberOfInputs(inputExpression, isUnique);
            // There is always 2^n different input combinations where n is the number of
            // unique inputs. 
            int numberOfRows = (int)Math.Pow(2, numberOfInputs);
            string[] inputMap = new string[numberOfRows];
            for (var i = 0; i < numberOfRows; i++)
            {
                // The input combination is simply the row of the table converted into 
                // its respective binary string. 
                inputMap[i] = ConvertIntintoBinaryString(i, inputExpression, isUnique);
            }
            return inputMap;
        }

        /// <summary>
        /// Computes the complete set of values for a truth table. This is done by 
        /// </summary>
        /// <param name="inputExpression">The boolean expression being tabulated. </param>
        /// <param name="headers">The headers of the truth table. </param>
        /// <param name="isUnique">Whether the table has unique inputs or not. This 
        /// decides whether or not the table is being drawn or used for diagram interactivity.
        /// </param>
        /// <returns>The complete table of values for the truth table. </returns>
        private string[] GenerateOutputMap(string inputExpression, string[] headers, bool isUnique)
        {
            // Generating all of the different input combinations for the truth table. 
            // These will be substituted into the boolean expression. 
            string[] inputMap = GenerateInputMap(inputExpression, isUnique);
            int numberOfRows = inputMap.Length;
            string inputCombination;
            string[] outputMap = new string[inputMap.Length];
            for (var i = 0; i < numberOfRows; i++)
            {
                // Getting the input combination for the truth table. 
                inputCombination = inputMap[i];
                // Computing the row of the truth table. 
                outputMap[i] += GetOutputRow(headers, inputCombination);
            }
            return outputMap;
        }

        /// <summary>
        /// Computes a single row of the truth table based off of the headers of the 
        /// truth table and a supplied binary input combination such as "0100" for a 
        /// four input truth table. 
        /// </summary>
        /// <param name="headers">The headers of the truth table. This is used to 
        /// determine a particular value in the column of the table. </param>
        /// <param name="inputCombination">The binary representation of the row 
        /// number in the truth table. </param>
        /// <returns>A single row of the truth table. </returns>
        private string GetOutputRow(string[] headers, string inputCombination)
        {
            string outputRow = "";
            // Evaluating each header to get the complete row. Each character in the 
            // string is a column of the truth table. 
            foreach (string header in headers)
            {
                // Subtracting 48 to convert the character into an integer value. 
                outputRow += EvaluateBooleanExpression(inputCombination, header) - 48;
            }
            return outputRow;
        }

        /// <summary>
        /// Utility function to generate headers for boolean expressions. 
        /// </summary>
        /// <param name="inputExpression">The boolean expression that the headers 
        /// are generated from. </param>
        /// <param name="isDisplay">Whether or not the headers will be displayed to the user. </param>
        /// <returns>The headers from the supplied boolean expression. </returns>
        private string[] GetHeaders(string inputExpression, bool isDisplay)
        {
            // Headers are always generated from the postfix expression. 
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

        /// <summary>
        /// Creates the headers for the truth table that is drawn onto the truth table canvas.
        /// </summary>
        /// <param name="inputExpression">The postfix expression that the headers represent.</param>
        /// <param name="numberOfInputs">The number of inputs within the postfix expression.</param>
        /// <param name="numberOfOperators">The number of operators within the postfix expression.</param>
        /// <returns>The truth table headers that are displayed to the user. </returns>
        private static string[] GenerateDisplayOperatorHeaders(string inputExpression, int numberOfInputs, int numberOfOperators)
        {
            // Display headers are ultimately a subset of the postorder headers. 
            string[] postorderHeaders = GeneratePostOrderHeaders(inputExpression, numberOfInputs, numberOfOperators);
            // Sorting into alphabetical order. 
            Array.Sort(postorderHeaders);
            // Sorting the headers by length. This makes the simplest sub-expressions first. 
            // These are always the shorter sub-expressions as they are lower down the tree. 
            Array.Sort(postorderHeaders, (x, y) => x.Length.CompareTo(y.Length));
            postorderHeaders = postorderHeaders.Distinct().ToArray();
            // Filtering out the duplicate subexpressions and inputs as they are always the 
            // same within the table. 
            return postorderHeaders;
        }

        /// <summary>
        /// Generates headers for colouring the wires when the user clicks on the inputs. 
        /// These represent the subexpression at a certain point within the tree. This is 
        /// also the postorder traversal of the tree. This is done by evaluating the expression.
        /// </summary>
        /// <param name="postfix">The postfix expression that the header will represent.</param>
        /// <param name="numberOfInputs">The number of non-unique inputs within the given boolean expression. </param>
        /// <param name="numberOfOperators">The number of operators within the given boolean expression. </param>
        /// <returns>An array representing the postorder headers of a postfix boolean expression. </returns>
        private static string[] GeneratePostOrderHeaders(string postfix, int numberOfInputs, int numberOfOperators)
        {
            var subExpressionStack = new Stack<string>();
            // The number of headers is always the number of non-unique inputs + the number 
            // of operators as this is the number of nodes in the tree. 
            string[] headers = new string[numberOfOperators + numberOfInputs];
            string subexpression;
            string operand1;
            string operand2;
            int i = 0;
            // Tokenising the expression. 
            foreach (char c in postfix)
            {
                // When an operand is found then pop it and add it to the header array as
                // An input is a header in itself. 
                if (char.IsLetter(c) || char.IsNumber(c))
                {
                    subExpressionStack.Push(c.ToString());
                    headers[i] = c.ToString();
                    i++;
                }
                // An operator has been found. 
                else
                {
                    if (c == '!')
                    {
                        // As a NOT gate is a unary operator, pop one item off of the stack. 
                        operand1 = subExpressionStack.Pop();
                        subexpression = $"({c}{operand1})";
                    }
                    else
                    {
                        // All other operators are binary operators so pop two items off of 
                        // the stack and push the result pack onto the stack. 
                        operand1 = subExpressionStack.Pop();
                        operand2 = subExpressionStack.Pop();
                        // Add brackets for the sub-expression and for the next headers. 
                        subexpression = $"({operand2}{c}{operand1})";
                    }
                    // Pushing the result pack onto the stack and add to the headers array
                    // as a subexpression is a header in itself. 
                    subExpressionStack.Push(subexpression);
                    headers[i] = subexpression;
                    i++;
                }
            }
            return headers;
        }

        /// <summary>
        /// Small method that simplfy calculates the width of cell in the truth table 
        /// based of the header. 
        /// </summary>
        /// <param name="header">The header the cell is in the column of. </param>
        /// <returns>The width of the truth table based off of the column th cell it in. 
        /// The is based off of the header. </returns>
        private double CalculateCellWidth(string header)
        {
            // Default size of the truth table cell. 
            double cellWidth = minCellWidth; 
            // If the header is not an input. 
            if (header.Length != 1)
            {
                // If the header is an expression larger than (A.B).
                if (header.Length > 5)
                {
                    cellWidth = header.Length * 14 + 15;
                }
                else
                {
                    cellWidth = header.Length * 13 + 15;
                }
            }
            return cellWidth;
        }

        /// <summary>
        /// Draws the headers for the truth table. 
        /// </summary>
        /// <param name="c">The canvas being drawn to. </param>
        /// <param name="headers">The headers of the truth table. These represent the stages 
        /// of the post order traversal of the logic gate diagram. </param>
        private void DrawTruthTableHeaders(Canvas c, string[] headers)
        {
            Label cell;
            var border = new Thickness(2);
            var font = new FontFamily("Consolas");
            double cellWidth;
            // Setting the initial postion of the table on the canvas. 
            double xPosition = initialXofTable;
            foreach (string header in headers)
            {
                // Calculating the width of the cell based off of the header. 
                cellWidth = CalculateCellWidth(header);
                cell = new Label
                {
                    // Defining the style of the cell. 
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Width = cellWidth,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = border,
                    Background = Brushes.White,
                    FontFamily = font,
                    FontSize = 24,
                    Content = header
                };
                // Adding the cell to the canvas and incrementing to get the position
                // of the next cell. 
                Canvas.SetTop(cell, initialYofTable);
                Canvas.SetLeft(cell, xPosition);
                c.Children.Add(cell);
                xPosition += cellWidth;
            }
            // Readjusting the x size of the canvas so that the scroll viewer works. 
            c.Width = Math.Max(xPosition, c.Width) + 30;
        }

        /// <summary>
        /// Draws the body of the truth table. This is the inputs and outputs at each stage 
        /// of the table. 
        /// </summary>
        /// <param name="c">The canvas being drawn to. </param>
        /// <param name="headers">The headers of the table. Used to calculate the cell widths. </param>
        /// <param name="outputMap">The data used to fill the cells of the table. </param>
        private void DrawTruthTableBody(Canvas c, string[] headers, string[] outputMap)
        {
            Label cell;
            var border = new Thickness(2);
            var font = new FontFamily("Consolas");
            double cellWidth;
            // Initial position of the body of the truth table. 
            double xPosition = initialXofTable;
            double yPosition = 60;
            foreach (string row in outputMap)
            {
                for (var i = 0; i < headers.Length; i++)
                {
                    // Calculating the width of the cell based off of the size of the header.
                    cellWidth = CalculateCellWidth(headers[i]);
                    // Defining the style of the table cell. 
                    cell = new Label
                    {
                        // Defining the style of the cell in the truth table. 
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        Width = cellWidth,
                        BorderBrush = Brushes.LightGray,
                        BorderThickness = border,
                        Background = Brushes.White,
                        FontFamily = font,
                        FontSize = 24,
                        Content = row[i]
                    };
                    // Adding the cell to the canvas and incrementing the position of the x
                    // to get to the next position. 
                    Canvas.SetTop(cell, yPosition);
                    Canvas.SetLeft(cell, xPosition);
                    c.Children.Add(cell);
                    xPosition += cellWidth;
                }
                // Resetting the x-position on the canvas, so that the table is aligned to
                // the left side of the canvas. 
                xPosition = 20;
                // Incrementing the y-position on the canvas and so, go to the next row of 
                // the table. 
                yPosition += 35;
            }
            // Readjusting the size of the canvas so that the scrollviewer works with 
            // large tables. This makes extremely large tables very easy to view.
            c.Height = Math.Max(yPosition, c.Height) + 30;
        }

        /// <summary>
        /// Removes the outer most set of brackets from the headers. This reduces the total 
        /// width of each header making the table more manageable to look through. It also 
        /// makes the headings cleaer as there is less going on. 
        /// </summary>
        /// <param name="headers"></param>
        /// <returns></returns>
        private static string[] TrimBrackets(string[] headers)
        {
            for (var i = 0; i < headers.Length; i++)
            {
                // Ensuring that the header does not represent an input which does not have a
                // set of brackets. 
                if (headers[i].Length != 1)
                {
                    // Slicing off the first and last characters which are the brackets. 
                    headers[i] = headers[i][1..^1];
                }
            }
            return headers;
        }

        /// <summary>
        /// Simple public method that allows the diagram class to draw to a canvas on the
        /// user interface. 
        /// </summary>
        /// <param name="c">The canvas that the truth table will be drawn on. </param>
        /// <param name="inputExpression">The boolean expression for which the truth table 
        /// represents. </param>
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
        /// <summary>
        /// Produces the final minimised expression is Petrick's method is not used. 
        /// </summary>
        /// <param name="essentialPrimeImplicants">The essential prime implicants found by the program.</param>
        /// <returns>The final minimised expression. </returns>
        private string ConvertEPIsToExpression(List<string> essentialPrimeImplicants)
        {
            // Connverting all of the essential prime implicants that have been found and 
            // producing a final expression. Each implicant is separated by an OR gate in 
            // the final expression. 
            essentialPrimeImplicants = essentialPrimeImplicants.ConvertAll(new Converter<string, string>(ConvertImplicantToExpression));
            string expression = string.Join("+", essentialPrimeImplicants);
            return expression;
        }

        /// <summary>
        /// Converts the binary form (such as -100) into terms of an expression such as 
        /// (B!C!D) in this example. This is used for outputting minimised expressions when 
        /// they need to be converted into their final form. 
        /// </summary>
        /// <param name="epi">The prime implicant being converted. This is usually and essential 
        /// one. </param>
        /// <returns>The expression form of the prime implicant. </returns>
        private string ConvertImplicantToExpression(string epi)
        {
            string tmp;
            // Removing the dashes as they are not apart of the output. 
            var removeDashes = new Regex("-"); 
            epi = removeDashes.Replace(epi, "");
            List<char> terms = epi.ToList();
            // Each term must be separated by an AND gate as the epi is a product. 
            tmp = string.Join(".", terms); 
            char input;
            for(var i = 0; i < tmp.Length; i += 2)
            {
                input = (char)(i + 65);
                // If the bit is a one then the output must be in the expression. 
                if (tmp[i] == '1')
                {
                    tmp = tmp.Remove(i, 1);
                    tmp = tmp.Insert(i, input.ToString());
                }
                // If the bit is a zero then the complement is added to the expression => !A. 
                else if (tmp[i] == '0')
                {
                    tmp = tmp.Remove(i, 1);
                    tmp = tmp.Insert(i, $"!{input}"); 
                }
            }
            // Add brackets to preserve pretty formatting. 
            return $"({tmp})";
        }

        /// <summary>
        /// Creates the minterm coverage strings for the prime implicants. These string show
        /// which prime implicants cover which minterms. This is used in finding the essential 
        /// prime implicants and also they are used in petricks method for finding the product of sums. 
        /// </summary>
        /// <param name="regex">The prime implicant chart being filled.</param>
        /// <param name="minterms">The minterms for the user entered boolean expression.</param>
        private static void SetRegexPatterns(Dictionary<string, string> regex, List<string> minterms)
        {
            Match res;
            foreach (string regexPattern in regex.Keys.ToList())
            {
                foreach (string minterm in minterms)
                {
                    // If minterm matches the form of the prime implicants, shown by Regex, 
                    // then a one can be written to the coverage string showing that the
                    // implicant covers this minterm. 
                    res = Regex.Match(minterm, regexPattern);
                    if (res.Success)
                    {
                        regex[regexPattern] += "1";
                    }
                    //The implicant does not cover this minterm. 
                    else
                    {
                        regex[regexPattern] += "0";
                    }
                }
            }
        }

        /// <summary>
        /// Prepares the prime implicant so that the minterm coverage strings can be created
        /// from the regex. This is also the point in which the keys are added to the dictionary. 
        /// </summary>
        /// <param name="regex">The empty prime implicant chart. </param>
        /// <param name="primeImplicants">The prime implicants that have been found from merging 
        /// minterms. </param>
        private static void ConvertImplicantsIntoRegex(Dictionary<string, string> regex, List<string> primeImplicants)
        {
            string tmp = "";
            string value = "";
            foreach (string primeImplicant in primeImplicants)
            {
                // Replacing any dashes with \d as to indicate that the character in that 
                // position does not matter. Otherwise the bit must remain.  
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
                // Adding the key and empty value to the dictionary. The value will be 
                // populated when the regex is carried out on the minterms. 
                regex.Add(tmp, value);
                tmp = "";
            }
        }

        /// <summary>
        /// Merges two minterms when finding the prime implicants. This is because a dash is 
        /// added in place of the differing bit when the merge has been made. 
        /// </summary>
        /// <param name="minterm1">One of the minterms being merged.</param>
        /// <param name="minterm2">One of the minterms being merged.</param>
        /// <returns>The merged minterm with the dashes present.</returns>
        /// <exception cref="Exception">The minterms are not of the same length and so cannot 
        /// be merged to produce a valid result. </exception>
        private static string MergeMinterms(string minterm1, string minterm2)
        {
            string mergedMinterm = "";
            if (minterm1.Length != minterm2.Length)
            {
                throw new Exception("Incorrect length");
            }
            else
            {
                for (var i = 0; i < minterm1.Length; i++)
                {
                    // If the bit differs then replace it with a dash.
                    // Otherwise just add the bit from one of the mitnerms as it is the same.. 
                    if (minterm1[i] != minterm2[i])
                    {
                        mergedMinterm += '-';
                    }
                    else
                    {
                        mergedMinterm += minterm1[i];
                    }
                }
                return mergedMinterm;
            }
        }

        /// <summary>
        /// For a merge to take place when finding the prime implicants, the dashes in both 
        /// of the minterms must align.
        /// </summary>
        /// <param name="minterm1">A minterm. </param>
        /// <param name="minterm2">The other minterm being checked with. </param>
        /// <returns>Whether or not the dashes within two mintemrs are in the same position.</returns>
        /// <exception cref="Exception">If the minterms are of different lengths then the dashes
        /// cannot be checked as you cannot iterate through one of strings and check both minterms. </exception>
        private static bool CheckDashesAlign(string minterm1, string minterm2)
        {
            if (minterm1.Length != minterm2.Length)
            {
                throw new Exception("Incorrect length");
            }
            else
            {
                for (var i = 0; i < minterm1.Length; i++)
                {
                    // If one of the minterms is a dash and the other is then the dashes
                    // do not align and so the minterms cannot be merged together. 
                    if (minterm1[i] != '-' && minterm2[i] == '-')
                    {
                        return false;
                    }
                }
                // Dashes must align and so this condition has been met. 
                return true;
            }
        }

        /// <summary>
        /// A merge can only take place if only one bit differs between the two minterms. 
        /// </summary>
        private static bool CheckMintermDifference(string m1, string m2)
        {
            // Removing the dashes so that bitwise operators can be used. 
            int minterm1 = RemoveDashes(m1);
            int minterm2 = RemoveDashes(m2);
            // XOR identifies any bits that differ between the two minterms. 
            int res = minterm1 ^ minterm2;
            // If res != 0, then one bit could differ. This checked with the AND.
            return res != 0 && (res & res - 1) == 0;
        }

        /// <summary>
        /// Converts the minterms' dashes into zeros so that the bit difference can 
        /// be easily checked. 
        /// </summary>
        private static int RemoveDashes(string minterm)
        {
            return Convert.ToInt32(minterm.Replace('-', '0'), 2);
        }

        /// <summary>
        /// Finds all of the minterms of the user-entered boolean expression. A minterm is 
        /// any binary input combination that results in the expression evalutating to 1. 
        /// </summary>
        /// <param name="expression">The user-entered boolean expression being minimised.</param>
        /// <returns>The list of minterms of the boolean expression.</returns>
        private List<string> GetMinterms(string expression)
        {
            var minterms = new List<string>();
            int result; 
            inputMap = GenerateInputMap(expression, true);
            foreach (string input in inputMap)
            {
                // Trying the evaluation and seeing if the result is a minterm. 
                result = EvaluateBooleanExpression(input, expression) - 48;
                // All minterms evaluate to 1. 
                if (result == 1)
                {
                    minterms.Add(input);
                }
            }
            return minterms;
        }

        /// <summary>
        /// The number prime implicants that cover a particular minterm within the prime 
        /// implicant chart. This can be used to find the essential prime implicants. 
        /// </summary>
        /// <param name="regex">The prime implicant chart of the boolean expression</param>
        /// <param name="minterms">The binary combinations that result in the expression
        /// evaluating to true. </param>
        /// <returns>The number of prime implicants that cover each minterm. </returns>
        private static int[] GetFrequencyTable(Dictionary<string, string> regex, List<string> minterms)
        {
            int[] mintermCoverage = new int[minterms.Count];
            foreach (string s in regex.Values.ToList())
            {
                for (var i = 0; i < s.Length; i++)
                {
                    if (s[i] == '1')
                    {
                        mintermCoverage[i]++;
                    }
                }
            }
            return mintermCoverage;
        }

        /// <summary>
        /// Performs the column search within the prime implicant chart, this is to 
        /// search for an essential prime implicant. 
        /// </summary>
        /// <param name="regex">The prime implicant chart. </param>
        /// <param name="pos">The column (minterm) that we are checking within the chart.</param>
        /// <returns>The only prime implicant that covers that minterm within the chart.</returns>
        /// <exception cref="Exception">No prime implicant covers that minterm and so the frequency 
        /// table has been incorrectly calculated. </exception>
        private static string GetEssentialPrimeImplicant(Dictionary<string, string> regex, int pos)
        {
            string[] implicantCoverage = regex.Values.ToArray();
            string[] implicants = regex.Keys.ToArray();
            string prime;
            // Iterating through each of the prime implicants. 
            for (var i = 0; i < implicantCoverage.Length; i++)
            {
                // Getting the minterm coverage for the prime implicant. 
                prime = implicantCoverage[i];
                // If the specified column is a 1 then the essential prime implicant has been found.
                if (prime[pos] == '1')
                {
                    return implicants[i];
                }
            }
            throw new Exception("Item could be found");
        }

        /// <summary>
        /// Filters through the coverages of each of the prime implicants searching for the 
        /// essential prime implicants. These are the prime implicants that are the only 
        /// implicant to cover a minterm. 
        /// </summary>
        /// <param name="regex">The prime implicant chart produced from the regex and 
        /// the prime implicants. </param>
        /// <param name="minterms">The binary combinations that result in the expression 
        /// evaluating to 1. </param>
        /// <returns>Every prime implicant within the prime implicant chart that is essential/ </returns>
        private static List<string> GetEssentialPrimeImplicants(Dictionary<string, string> regex, List<string> minterms)
        {
            // Find the number of ones within each column of the prime implicant chart. 
            int[] bitFrequencyTable = GetFrequencyTable(regex, minterms);
            var essentialPrimeImplicants = new List<string>();
            string epi;
            for (var i = 0; i < bitFrequencyTable.Length; i++)
            {
                // This means that there is only one implicant in this column that covers it 
                // and so an essential prime implicant has been found. Iterate through the 
                // column to find the implicant with the 1 within that column. 
                if (bitFrequencyTable[i] == 1)
                { 
                    // Do the column search to find the prime implicant, which must 
                    // be essential. 
                    epi = GetEssentialPrimeImplicant(regex, i);
                    if (!essentialPrimeImplicants.Contains(epi))
                    {
                        essentialPrimeImplicants.Add(epi);
                    }
                }
            }
            return essentialPrimeImplicants;
        }

        /// <summary>
        /// Finds the prime implicants of the boolean expression. This comes from the minterms
        /// of the boolean expression. This is done using a recursive merging process where 
        /// merges take place. Once no merges have taken place on the prim implicants then they 
        /// all have been found and the method ends. 
        /// </summary>
        /// <param name="mintermList">The binary input combinations that result in the 
        /// boolean expression evaluating to one. </param>
        /// <returns>The list of prime implicants of the expression. </returns>
        private static List<string> GetPrimeImplicants(List<string> mintermList)
        {
            var primeImplicants = new List<string>();
            bool[] merges = new bool[mintermList.Count];
            int numberOfMerges = 0;
            string mergedMinterm;
            string minterm1;
            string minterm2;
            for (var i = 0; i < mintermList.Count; i++)
            {
                for (var c = i + 1; c < mintermList.Count; c++)
                {
                    minterm1 = mintermList[i];
                    minterm2 = mintermList[c];
                    // A merge can be made only if the dashes of the minterms align and 
                    // there is only one bit of difference between the two minterms. 
                    if (CheckDashesAlign(minterm1, minterm2) && CheckMintermDifference(minterm1, minterm2))
                    {
                        mergedMinterm = MergeMinterms(minterm1, minterm2);
                        primeImplicants.Add(mergedMinterm);
                        numberOfMerges++;
                        // Mark the terms that have been merged so that they do not persist
                        // to the next stage of the merging process. 
                        merges[i] = true;
                        merges[c] = true;
                    }
                }
            }
            // Filtering out the terms that have been merged so that the minterms do not
            // remain when the next stage occurs. 
            for (var j = 0; j < mintermList.Count; j++)
            {
                if (!merges[j] && !primeImplicants.Contains(mintermList[j]))
                {
                    primeImplicants.Add(mintermList[j]);
                }
            }
            // If no more merges can be made on the list of implicants then all of the prime
            // implicants for the expression must have been found. Otherwise, recurse and try
            // merging again. 
            if (numberOfMerges == 0)
            {
                return primeImplicants;
            }
            else
            {
                return GetPrimeImplicants(primeImplicants);
            }
        }

        /// <summary>
        /// Minimises a user entered boolean expression using the Quine-Mcluskey algorithm and 
        /// Petrick's method. Details of the algorithms can be found on Wikipedia. 
        /// </summary>
        /// <param name="expression">A user entered boolean expression.</param>
        public void MinimiseExpression(string expression)
        {
            // The input combinations that result in the expression evaluating to one. 
            List<string> minterms = GetMinterms(expression);
            List<string> primeImplicants = GetPrimeImplicants(minterms);
            var PIchart = new Dictionary<string, string>();
            ConvertImplicantsIntoRegex(PIchart, primeImplicants);
            // Creating the coverages for each of the prime implicants using regex. 
            SetRegexPatterns(PIchart, minterms);
            PIchart = ReplaceDashesFromRegex(PIchart);
            List<string> PIs = GetEssentialPrimeImplicants(PIchart, minterms);
            string coveredString = GetCoveredString(PIs, PIchart);
            // Filtering out any expression that evaluate to 0 or 1. 
            if (PIs.Count == 0)
            {
                minimisedExpression = "0";
            }
            else if (PIs[0] == "--" || PIs[0] == "0")
            {
                if (PIs[0] == "--")
                {
                    minimisedExpression = "1";
                }
                else
                {
                    minimisedExpression = "0";
                }
            }
            else
            {
                // If a 0 remains in the covered string then the essential prime implicants 
                // do not cover all of the minterms and so petrick's method must be used. 
                if (coveredString.Contains('0'))
                {
                    minimisedExpression = DoPetriksMethod(PIchart, PIs, primeImplicants, minterms);
                }
                else
                {
                    minimisedExpression = ConvertEPIsToExpression(PIs);
                }
            }   
        }

        /// <summary>
        /// Replaces the regex characters in the prime implicants with dashes. This will
        /// make processing, and outputting the prime implicants easier. 
        /// </summary>
        /// <param name="PIChart">The prime implicant chart produced by QM.</param>
        /// <returns>The PIChart with \d replaced for each of the keys. </returns>
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
        /// Checks to make sure that the essential prime implicants cover the original boolean 
        /// expression. Otherwise, Petrick's method should be used to make sure that all of the 
        /// minterms are covered by the implicants. 
        /// </summary>
        /// <param name="epis">The essential prime implicants currently found. </param>
        /// <param name="PIchart">The prime implicant produced from the prime implicants.</param>
        /// <returns></returns>
        private static string GetCoveredString(List<string> epis, Dictionary<string, string> PIchart)
        {
            int coveredString = 0;
            foreach (string s in epis)
            {
                // Apply logical OR to each of the coverages of the essential prime implicants. 
                // This is to ensure at least one 1 covers a minterm. 
                coveredString |= Convert.ToInt32(PIchart[s], 2);
            }
            return coveredString.ToString();
        }

        /// <summary>
        /// Carries out the process to prepare the PI chart for Petricks' method. The first
        /// step is to remove any rows of prime implicants. The minterm that makes the 
        /// implicant essential should also be removed. 
        /// </summary>
        /// <param name="PIchart">The prime implicant chart being reduced.</param>
        /// <param name="epis">The essential prime implicants of the expression. </param>
        /// <param name="minterms">The minterms covered by the expression. </param>
        /// <returns>The updated prime implicant chart with essential prime implicants removed.</returns>
        private static Dictionary<string, string> RemoveEPIs(Dictionary<string, string> PIchart, List<string> epis, List<string> minterms)
        {
            string value;
            int bit;
            // The number of ones that cover each minterm. 
            int[] freq = GetFrequencyTable(PIchart, minterms);
            foreach (string implicant in PIchart.Keys)
            {
                // A prime implicant has been found. 
                if (epis.Contains(implicant))
                {
                    value = PIchart[implicant];
                    // Finding the position of the bit that makes the implicant essential. 
                    // This is the column that being removed. 
                    bit = GetSignificantBit(implicant, freq);
                    // Removing the column of the prime implicant chart. 
                    TrimMinterm(PIchart, bit);
                    PIchart.Remove(implicant);
                }
            }
            return PIchart;
        }

        /// <summary>
        /// Removes the bit of a specified column when an essential prime implicant has been
        /// found. This is for chart reduction in Petrick's method. 
        /// </summary>
        /// <param name="PIchart">The prime implicant chart produced by QM.</param>
        /// <param name="pos">The column of the prime implicant chart being removed.</param>
        /// <returns>The updated dictionary with the column removed. </returns>
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

        /// <summary>
        /// Returns the position of the bit that makes the prime implicant essential. 
        /// </summary>
        /// <param name="epi">The prime implicant being check for. </param>
        /// <param name="freq">The number of times a minterm is covered by the implicant.</param>
        /// <returns>The position within the coverage that makes the implicant essential. </returns>
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

        /// <summary>
        /// Method that carries out the stepwise process of Petricks's method. The process 
        /// the algorithm is defined on the methods' Wikipedia page.
        /// </summary>
        /// <param name="PIchart">The prime implicant chart found during QM. </param>
        /// <param name="epis">The essential prime implicants found in the PIchart.</param>
        /// <param name="primeImplicants">All of the prime implicants found during 
        /// the inital merging process of QM.</param>
        /// <param name="minterms">The minterm of </param>
        /// <returns>The minimised expression produced by Petrick's method.</returns>
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

        /// <summary>
        /// Creates the relationship between terms and prime implicants. This makes 
        /// the creation of product of sums and sum of products much clearer as the 
        /// prime implicants are abstracted to a single letter. It also allows for 
        /// multiple string of prime implicants to represented in a concise manner.
        /// </summary>
        /// <param name="primeImplicants">The prime implicants found by the intial
        /// merge of the minterms of the boolean expression.</param>
        /// <returns>The relationship between terms and prime implicants as a
        /// dictionary. </returns>
        private static Dictionary<char, string> MapTermsToImplicants(List<string> primeImplicants)
        {
            var mapping = new Dictionary<char, string>();
            // To ensure that unique characters are used as the terms. Take the largest
            // ascii value (highest inputs) and convert it to the next character and
            // this is the first character in the map. 
            char minChar = (char)(primeImplicants[0].Length + 65);
            for (var i = 0; i < primeImplicants.Count; i++)
            {
                mapping.Add(minChar, primeImplicants[i]);
                // Incrementing so that the inputs within the map are sequential. 
                minChar++;
            }
            return mapping;

        }

        /// <summary>
        /// Produces the product of sums of the prime implicant chart. This stage prepares
        /// the data so that the boolean algebra can be done on the expression. 
        /// </summary>
        /// <param name="termToImplicantMap">The relationship between terms and 
        /// prime implicants. </param>
        /// <param name="primeImplicantChart">The relationship between minterm coverage
        /// and the prime implicants found in the intial process of QM. </param>
        /// <returns> The product of sums of the PI chart, in the form [('K', 'L'), etc.]</returns>
        private static List<Bracket> GetProductOfSums(Dictionary<char, string> termToImplicantMap, Dictionary<string, string> primeImplicantChart)
        {
            var productOfSums = new List<Bracket>();
            List<Bracket> sumsToAdd;
            string primeImplicant;
            // A sum can be made if the minterm is covered by two implicants. 
            // So loop through each key to find its coverage of the minterms
            foreach (string key in primeImplicantChart.Keys)
            {
                primeImplicant = primeImplicantChart[key];
                // Iterate through each minterm.
                for (var i = 0; i < primeImplicant.Length; i++)
                {
                    // If the prime implicant covers the minterm then a possible sum 
                    // could be found so search through the chart vertically.
                    if (primeImplicant[i] == '1')
                    {
                        // Get all of the possible sums within the column of the covered
                        // minterm and add them to the found sums. 
                        sumsToAdd = GetSumsToAdd(primeImplicantChart, termToImplicantMap, key, i);
                        AddSumsToList(productOfSums, sumsToAdd);
                    }
                }
            }
            return productOfSums;
        }

        /// <summary>
        /// Ensures that duplicate sums are not added the list as X.X = X and so they cancel
        /// off.
        /// </summary>
        /// <param name="productOfSums">The sums that have currently been found during
        /// the iteration through the prime implicant chart. </param>
        /// <param name="sumsToAdd">The sums that have found by searching through the 
        /// column of the covered minterm. </param>
        private static void AddSumsToList(List<Bracket> productOfSums, List<Bracket> sumsToAdd)
        {
            foreach (Bracket s in sumsToAdd)
            {
                if (productOfSums.Contains(s) == false)
                {
                    productOfSums.Add(s);
                }
            }
        }

        /// <summary>
        /// Gets all of the possible sums that are within a column of the prime implicant
        /// chart. This is done by searching through and if another implicant covers the 
        /// same minterm then a sum can be made. 
        /// </summary>
        /// <param name="PIchart">The relationship between the prime implicants and 
        /// the minterms that they cover. </param>
        /// <param name="termToImplicantMap">The relationship between terms in the boolean
        /// algebra and the prime implicants that they represent.</param>
        /// <param name="key">The prime implicant that caused the column search</param>
        /// <param name="positionWithinKey">The minterm that the prime implicant must
        /// cover in order for a sum to be created. It must not be the same implicant. </param>
        /// <returns>All of the possible sums for a prime implicant that covers a minterm. </returns>
        private static List<Bracket> GetSumsToAdd(Dictionary<string, string> PIchart, Dictionary<char, string> termToImplicantMap, string key, int positionWithinKey)
        {
            var sumsToAdd = new List<Bracket>();
            Bracket sum;
            string implicant;
            char term1;
            char term2;
            // Iterate through each prime implicant to try and find as many sums as possible.
            for (var i = 0; i < PIchart.Keys.Count; i++)
            {
                // The prime implicant that could make a sum. 
                implicant = PIchart.Keys.ToArray()[i];
                // If the implicant covers the same minterm then a sum has been found and a
                // bracket can be created. 
                if (PIchart[implicant][positionWithinKey] == '1')
                {
                    // Getting the letters that represent a certain prime implicant. 
                    term1 = GetTermFromImplicant(termToImplicantMap, key);
                    term2 = GetTermFromImplicant(termToImplicantMap, implicant);
                    // Ensuring brackets are not made with duplicate term. 
                    if (term1 != term2)
                    {
                        // A new sum is created and added to the list. 
                        sum = new Bracket(term1, term2);
                        sumsToAdd.Add(sum);
                    }
                }
            }
            return sumsToAdd;
        }

        /// <summary>
        /// Searches and return the given term for a prime implicant, based off of the map
        /// created by the program. 
        /// </summary>
        /// <param name="termToImplicantMap">Assignment of terms(letters) that represent
        /// prime implicants. This simplification easier to deal with. </param>
        /// <param name="implicant">The prime implicant being searched for. </param>
        /// <returns>The term that is the key to the respective prime implicant.</returns>
        /// <exception cref="Exception">The implicant that is being searched for could not
        /// be found within the termToImplicantMap </exception>
        private static char GetTermFromImplicant(Dictionary<char, string> termToImplicantMap, string implicant)
        {
            // All of the prime implicants that are within the prime implicant. 
            string[] implicants = termToImplicantMap.Values.ToArray();
            // The letter that represents the prime implicant within the simplififcation. 
            char[] keys = termToImplicantMap.Keys.ToArray();
            for (var i = 0; i < termToImplicantMap.Values.Count; i++)
            {
                if (implicants[i] == implicant)
                {
                    // The term based off of the implicant. 
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
            List<List<string>> stringProducts = ConvertBracketsToString(productOfSums);
            // Recursively apply the distributive law which does the rest of the work. 
            List<List<string>> sumOfProducts = RecursiveDistributiveLaw(stringProducts);
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

        /// <summary>
        /// Carries out the main distribution to convert the product of sums into the sum
        /// of products. This recursively applies the distributive law until only one 
        /// bracket remains which means the law can no longer be applied. 
        /// </summary>
        /// <param name="brackets">The string representation of the product of sums.</param>
        /// <returns>A singular string representing the sum of products. </returns>
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
            string tempResult = a + b;
            string finalResult = "";
            foreach (char c in tempResult)
            {
                // Simply find the unique inputs to gain the result of the law. 
                if (!finalResult.Contains(c))
                {
                   finalResult += c;
                }
            }
            return finalResult;
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
            string minProduct = sumOfProducts[0];
            foreach (string p in sumOfProducts)
            {
                if (p.Length < minProduct.Length)
                {
                    minProduct = p;
                }
            }
            return minProduct;
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
        /// <returns>The fully minimised expression from Petrick's method. </returns>
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
        #endregion
    }
}
