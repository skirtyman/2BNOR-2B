using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2BNOR_2B.Code
{
    /// <summary>
    /// Represents two product brackets within the product of sums. This simplifies the
    /// process of finding the sum of products by keeping the terms clear and in order.
    /// </summary>
    struct Bracket
    {
        public Bracket(char term1, char term2)
        {
            // Sorting into alphabetical order. This can be done as OR is commutative. 
            if (term1 < term2)
            {
                this.term1 = term1.ToString();
                this.term2 = term2.ToString();
            }
            else
            {
                this.term1 = term2.ToString();
                this.term2 = term1.ToString();
            }
        }

        public string term1;
        public string term2;
    }
}
