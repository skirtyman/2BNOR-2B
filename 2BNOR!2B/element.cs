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
        //0 - 5 = AND to NOR, 6 = input pin, 7 = output pin
        private int elementType;
        public element leftChild;
        public element rightChild;
        public element parent; 
        private logicGate logicGate;
        private int state;
        private string label;
        private string elementName; 

        public element()
        {

        }

        //constructor for creating output pins 
        public element(int elementID)
        {
            //7 = outputpin
            this.elementID = elementID;
            elementType = 7;
            state = 0;
            label = "Q";
            elementName = "output_pin";
        }

        //constructor for creating input pins 
        public element(int elementID, string label)
        {
            //6 = inputpin
            this.elementID = elementID;
            this.label = label;
            elementType = 6;
            leftChild = null;
            rightChild = null;
            state = 0;
            elementName = "input_pin"; 

        }

        //constructor for logic gates
        public element(string elementName, int elementID, int elementType, element leftChild, element rightChild)
        {
            this.elementID = elementID;
            this.leftChild = leftChild;
            this.rightChild = rightChild;
            this.elementType = elementType;
            this.elementName = elementName;
            label = " "; 
        }

        public int getElementID()
        {
            return elementID;
        }

        public int getElementType()
        {
            return elementType;
        }

        public string getLabel()
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
    }
}
