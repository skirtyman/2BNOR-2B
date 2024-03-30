using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Data;

namespace _2BNOR_2B.Code
{
    public class BooleanConverter : IValueConverter
    {
        // All of the boolean operators that a user could enter into a boolean expression.
        private readonly char[] booleanOperators = { '+', '^', '.', '!' };
        // Regular expression used to remove the whitespace in a string.
        private static readonly Regex r = new(@"\s+");

        /// <summary>
        /// Utility function for removing the whitespace from user-entered boolean expressions.
        /// This makes them easier to deal with and removes an element to validate, overall 
        /// simplifying the algorithm. 
        /// </summary>
        /// <param name="input">The string being modified by the method.</param>
        /// <param name="replacement">The character that will replace the white space in the 
        /// input, value = "", to remove the whitespace completely.</param>
        /// <returns></returns>
        private static string RemoveWhitespace(string input, string replacement)
        {
            return r.Replace(input, replacement);
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
            // Removing whitespace to simplify processing.
            infixExpression = RemoveWhitespace(infixExpression, "");
            var operatorStack = new Stack<char>();
            string postfixExpression = "";
            int operatorPrecedence;
            // Tokenising the expression.
            foreach (char token in infixExpression)
            {
                // If the token is an input/constant then it can be added straight into the output.
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
                        postfixExpression += operatorStack.Pop();
                    }
                    operatorStack.Pop();
                }
            }
            while (operatorStack.Count > 0)
            {
                postfixExpression += operatorStack.Pop();
            }
            return postfixExpression;
        }

        /// <summary>
        /// Utility function to convert a given character into its latex representation. 
        /// </summary>
        /// <param name="c">The character being converted.</param>
        /// <returns>The latex representation of the suplied character.</returns>
        private static string GetLATEXFromChar(char c)
        {
            switch (c)
            {
                case '!':
                    return @"\overline{";
                case '.':
                    // Added spaces in the latex as a stylistic choice. 
                    return @"\;.\;";
                case '^':
                    return @"\oplus ";
                default:
                    return c.ToString();
            }
        }

        /// <summary>
        /// Converts the postfix string by evaluating it and building the string, that way. 
        /// However, this differs by instead of popping the program's notation onto the stack, 
        /// the LATEX representation of the string is pushed back onto the stack. 
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public string ConvertString(string inputString)
        {
            var subExpressionStack = new Stack<string>();
            string subexpression;
            // Creating the postfix expression. 
            string postfix = ConvertInfixtoPostfix(inputString);
            string operand1;
            string operand2;
            foreach (char c in postfix)
            {
                if (char.IsLetter(c) || char.IsNumber(c))
                {
                    subExpressionStack.Push(c.ToString());
                }
                else
                {
                    if (c == '!')
                    {
                        // As NOT is an unary operator, then only pop one item from the stack.
                        operand1 = subExpressionStack.Pop();
                        // Push the result pack onto the stack in LATEX form. 
                        subexpression = $"{GetLATEXFromChar(c)}{operand1}" + "}";
                    }
                    else
                    {
                        // Any other operator is a binary operator so pop two items from the 
                        // stack. 
                        operand1 = subExpressionStack.Pop();
                        operand2 = subExpressionStack.Pop();
                        // Adding brackets to show precedence and make the resulting expression 
                        // clearer.
                        subexpression = $@"\left({operand2}{GetLATEXFromChar(c)}{operand1}\right)"; 
                    }
                    // Push the result back onto the stack. 
                    subExpressionStack.Push(subexpression);
                }
            }
            // The LATEX expression is the final result on the stack. 
            return subExpressionStack.Pop();
        }

        /// <summary>
        /// Method to convert the expression the user is entering at runtime into its 
        /// LATEX representation. This is done by converting the expression into postfix
        /// and then building the string by "evaluating" it. 
        /// </summary>
        /// <param name="value">The string value from the textbox that is being converted.</param>
        /// <returns>LATEX representation of the user-inputted boolean expression.</returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Try converting the string into its LATEX representation otherwise display 
            // nothing as the user has not entered a decent expression. 
            try
            {
                return ConvertString(value as string);
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Function to convert back into the string. However this function is N/A for the 
        /// program, hence the notImplmentedException(). It should be noted that it is required
        /// for a IValueConverter to have a ConvertBack method. 
        /// </summary>
        /// <returns>A boolean expression given its LATEX representation.</returns>
        /// <exception cref="NotImplementedException">This method is not needed within the 
        /// program so it does not need to be implemented.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
