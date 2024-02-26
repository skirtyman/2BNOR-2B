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

namespace _2BNOR_2B
{
    public class booleanConverter : IValueConverter
    {
        private char[] booleanOperators = { '.', '^', '+', '!' };
        private static Regex r = new Regex(@"\s+");

        private static string removeWhitespace(string input, string replacement)
        {
            return r.Replace(input, replacement);
        }

        //implementation of the 'Shunting Yard' algorithm for boolean expressions. This produces the postfix boolean expression of an infix expression. 
        private string ConvertInfixtoPostfix(string infixExpression)
        {
            infixExpression = removeWhitespace(infixExpression, "");
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

        private string getLATEXFromChar(char c)
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

        private string convertString(string inputString)
        {
            Stack<string> subExpressionStack = new Stack<string>();
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
                        subexpression = getLATEXFromChar(c) + operand1 + "}";
                    }
                    else
                    {
                        operand1 = subExpressionStack.Pop();
                        operand2 = subExpressionStack.Pop();
                        subexpression = @"\left(" + operand2 + getLATEXFromChar(c) + operand1 + @"\right)";
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
                return convertString(value as string); 
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
