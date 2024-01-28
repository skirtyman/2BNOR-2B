using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2BNOR_2B
{
    public class element
    {
        private int elementID;
        public element leftChild;
        public element rightChild;
        public element parent; 
        private logicGate logicGate;
        private int state;
        private char label;
        private string elementName;
        private bool unique = false; 

        public element()
        {

        }

        //constructor for creating output pins 
        public element(int elementID)
        {
            this.elementID = elementID;
            state = 0;
            label = 'Q';
            elementName = "output_pin";
        }

        //constructor for creating input pins 
        public element(int elementID, char label)
        {
            this.elementID = elementID;
            this.label = label;
            leftChild = null;
            rightChild = null;
            state = 0;
            elementName = "input_pin"; 

        }

        //constructor for logic gates
        public element(string elementName, int elementID, element leftChild, element rightChild)
        {
            this.elementID = elementID;
            this.leftChild = leftChild;
            this.rightChild = rightChild;
            this.elementName = elementName;
            label = ' '; 
        }

        public int getElementID()
        {
            return elementID;
        }

        public char getLabel()
        {
            return label; 
        }

        public string getElementName()
        {
            return elementName;
        }

        public void setLogicGate(logicGate logicGate)
        {
            this.logicGate = logicGate; 
        }

        public logicGate getLogicGate()
        {
            return logicGate; 
        }

        public bool getUniqueness()
        {
            return unique; 
        }

        public void setUniqueness(bool unique)
        {
            this.unique = unique;
        }

    }
}
