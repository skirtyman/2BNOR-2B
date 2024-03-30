using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _2BNOR_2B.Code
{
    /// <summary>
    /// Acts as the node for the binary tree in the logic gate diagram. This can represent
    /// input pins, outputs and logic gates. It is important to note that an element is a
    /// non-visual node that pairs with the visual through the logicGate class. This allows 
    /// for repeated inputs to either be displayed or not. 
    /// </summary>
    public class Element
    {
        // The ID of the node within the tree. This is also the position within the string
        // produced by a postorder traversal.
        private readonly int elementID;
        public Element leftChild;
        public Element rightChild;
        public Element parent;
        // The visual gate that is paired with the node in the tree. This is null if the 
        // node is repeated. 
        private LogicGate logicGate;
        // The binary state of the element (1 or 0), used to colour the wires when the diagram
        // is interacted with. 
        private int state;
        // If the node is an input, then it has a respective label which should be displayed. 
        // Only ever null when a logic gate as they have no label. 
        private readonly char label;
        // The string name of the node "and_gate", etc. 
        private readonly string elementName;
        // The number of occurrences within the tree. Such as (A.B)^A => A would have 2 instances.
        private int instances = 0;

        public Element()
        {

        }
        /// <summary>
        /// Constructor for generating the output pin of the logic gate diagram. Simply 
        /// sets the default values for it. 
        /// </summary>
        /// <param name="elementID">The output pin has an ID of -1 as it is not in the tree.</param>
        public Element(int elementID)
        {
            this.elementID = elementID;
            state = 0;
            label = 'Q';
            elementName = "output_pin";
        }

        /// <summary>
        /// Constructor for creating input pins within the diagram. As it is a leaf node, 
        /// the children of the input are always null. The inputs also default to a state of 0. 
        /// </summary>
        /// <param name="elementID"> The postorder position within the tree.</param>
        /// <param name="label"> The label of the input. Used to decide whether to display
        /// the element or not. </param>
        public Element(int elementID, char label)
        {
            this.elementID = elementID;
            this.label = label;
            leftChild = null;
            rightChild = null;
            state = 0;
            elementName = "input_pin";

        }

        /// <summary>
        /// Constructor used to create logic gates. 
        /// </summary>
        /// <param name="elementName">The name and hence, type, of logic gate</param>
        /// <param name="elementID">The postorder position of the gate within the tree.</param>
        /// <param name="leftChild"> The left child of the gate, this could be an input 
        /// or gate. If the element is a NOT gate then this is null as a NOT only has one 
        /// child</param>
        /// <param name="rightChild"> The right child of the gate, this could be an input
        /// or a gate.</param>
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
