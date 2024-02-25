using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace _2BNOR_2B
{
    public class booleanConverter : IValueConverter
    {
        private char[] booleanOperators = { '.', '^', '+', '!' };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string convertedString = "";
            //The current expression stored within the input text box. 
            string inputString = value as string;
            int numberOfOpenBrackets = 0;
            //Stores the current position within the converted string where the new tokens are being inserted. 
            int pos = 0;
            for (int i = 0; i < inputString.Length; i++)
            {
                if (inputString[i] == '!')
                {
                    convertedString = convertedString.Insert(pos, @"\overline{}");
                    pos += 10;
                }
                else if (inputString[i] == '(')
                {
                    convertedString = convertedString.Insert(pos, @"\left(");
                    pos += 6;
                    numberOfOpenBrackets++;
                }
                else if (inputString[i] == ')')
                {
                    convertedString = convertedString.Insert(pos+1, @"\right)");
                    pos += 7;
                    numberOfOpenBrackets--;
                }
                else if (inputString[i] == '.')
                {
                    convertedString = convertedString.Insert(pos, @"\;.\;");
                    pos += 5;
                }
                else if (inputString[i] == '^')
                {
                    convertedString = convertedString.Insert(pos, @"\oplus ");
                    pos += 7;
                }
                else
                {
                    convertedString = convertedString.Insert(pos, inputString[i].ToString());
                    pos++;
                }

                if (i != inputString.Length - 1 && numberOfOpenBrackets == 0)
                {
                    if (booleanOperators.Contains(inputString[i + 1]))
                    {
                        pos = convertedString.Length;
                    }
                }
            }
            return convertedString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
