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
        private readonly char[] booleanOperators = { '+', '^', '.', '!' };
        private static readonly Regex r = new(@"\s+");

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
                        //Debug.Assert(operatorStack.Count > 0, "The stack is empty.");
                        postfixExpression += operatorStack.Pop();
                    }
                    //Debug.Assert(operatorStack.Peek() == '(', "The top item is a (");
                    operatorStack.Pop();
                }
            }
            while (operatorStack.Count > 0)
            {
                //Debug.Assert(operatorStack.Peek() != '(', "The top item is a (");
                postfixExpression += operatorStack.Pop();
            }
            return postfixExpression;
        }

        private static string GetLATEXFromChar(char c)
        {
            switch (c)
            {
                case '!':
                    return @"\overline{";
                case '.':
                    return @"\;.\;";
                case '^':
                    return @"\oplus ";
                default:
                    return c.ToString();
            }
        }

        public string ConvertString(string inputString)
        {
            var subExpressionStack = new Stack<string>();
            string subexpression;
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
                        operand1 = subExpressionStack.Pop();
                        subexpression = $"{GetLATEXFromChar(c)}{operand1}" + "}";
                    }
                    else
                    {
                        operand1 = subExpressionStack.Pop();
                        operand2 = subExpressionStack.Pop();
                        subexpression = $@"\left({operand2}{GetLATEXFromChar(c)}{operand1}\right)"; 
                    }
                    subExpressionStack.Push(subexpression);
                }
            }
            return subExpressionStack.Pop();
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string v = value as string; 
                return ConvertString(value as string);
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
