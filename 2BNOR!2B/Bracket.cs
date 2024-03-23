﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2BNOR_2B
{
    struct Bracket
    {
        public Bracket(char term1, char term2)
        {
            //Sorting so the terms are in alphabetical order. 
            //This is for the distributive law. 
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
