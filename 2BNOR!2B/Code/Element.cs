using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2BNOR_2B.Code
{
    public class Element
    {
        private readonly int elementID;
        public Element leftChild;
        public Element rightChild;
        public Element parent;
        private LogicGate logicGate;
        private int state;
        private readonly char label;
        private readonly string elementName;
        private int instances = 0;

        public Element()
        {

        }
        public Element(int elementID)
        {
            this.elementID = elementID;
            state = 0;
            label = 'Q';
            elementName = "output_pin";
        }
        public Element(int elementID, char label)
        {
            this.elementID = elementID;
            this.label = label;
            leftChild = null;
            rightChild = null;
            state = 0;
            elementName = "input_pin";

        }
        public Element(string elementName, int elementID, Element leftChild, Element rightChild)
        {
            this.elementID = elementID;
            this.leftChild = leftChild;
            this.rightChild = rightChild;
            this.elementName = elementName;
            label = ' ';
        }

        public int GetElementID()
        {
            return elementID;
        }

        public char GetLabel()
        {
            return label;
        }

        public string GetElementName()
        {
            return elementName;
        }

        public void SetLogicGate(LogicGate logicGate)
        {
            this.logicGate = logicGate;
        }

        public LogicGate GetLogicGate()
        {
            return logicGate;
        }

        public int GetInstances()
        {
            return instances;
        }

        public void SetInstances(int instances)
        {
            this.instances = instances;
        }

        public void AddInstance()
        {
            instances++;
        }

        public void SetState(int state)
        {
            this.state = state;
        }

        public int GetState()
        {
            return state;
        }

    }
}
