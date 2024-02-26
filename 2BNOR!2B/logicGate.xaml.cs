﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _2BNOR_2B
{
    /// <summary>
    /// Interaction logic for logicGate.xaml
    /// </summary>
    public partial class logicGate : UserControl
    {
        private element gate;
        private double labelWidth;
        //Store the wires connected to the logic gate so that they can moved when the logic gate is dragged. 
        private wire leftChildWire;
        private wire rightChildWire;
        private wire parentWire; 

        public logicGate(element gate)
        {
            InitializeComponent();
            this.gate = gate;
            setImage();
            //if (gate.leftChild == null &&  gate.rightChild == null)
            //{
            //    setLabel(); 
            //}
            this.PreviewMouseDown += LogicGate_PreviewMouseDown;
        }

        private void LogicGate_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (gate.getElementName() == "input_pin")
            {
                if (gate.getState() == 1)
                {
                    gate.setState(0);

                }
                else
                {
                    gate.setState(1);
                }
            }

        }

        public Point getInputPoint1()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this) + 10);
        }

        public Point getInputPoint2()
        {
            if (gate.leftChild == null && gate.rightChild != null)
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)+20);
            }
            else
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)+30); 
            }
        }

        //Returns the point that is drawn too, for connecting the input and root node together. 
        public Point getInputForOutput()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this) + elementImage.Height / 2 - 1);
        }

        public Point getOutputPoint()
        {
            if (gate.getElementName() == "input_pin")
            {
                return new Point(Canvas.GetLeft(this) + elementImage.Width + labelWidth - 43, Canvas.GetTop(this) + elementImage.Height / 2);
            }
            else
            {
                return new Point(Canvas.GetLeft(this) + elementImage.Width + labelWidth - 5, Canvas.GetTop(this) + elementImage.Height / 2);
            }
        }

        public element getGate()
        {
            return gate; 
        }

        private void setImage()
        {
            BitmapImage bitmap = new BitmapImage(); 
            string imageName = gate.getElementName();
            string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", imageName+".png");
            using (FileStream stream = new FileStream(@$"C:\Users\andreas\source\repos\2BNOR!2B - Copy\2BNOR!2B\images\{imageName}.png", FileMode.Open))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            elementImage.Stretch = Stretch.Uniform;
            elementImage.Height = bitmap.Height / 5;
            elementImage.Width = bitmap.Width / 5;
            elementImage.Source = bitmap;
            elementLabel.Content = gate.getLabel(); 
        }

        //private void setLabel()
        //{
        //    Label l = new Label();
        //    l.BorderThickness = new Thickness(2);
        //    l.BorderBrush = Brushes.LightGray; 
        //    l.Width = elementImage.Width - (elementImage.Width - 30);
        //    labelWidth = l.Width;
        //    //Do not need to adjust height as this doesn't matter. 
        //    l.Height = elementImage.Height;
        //    l.HorizontalAlignment = HorizontalAlignment.Center;
        //    l.VerticalAlignment = VerticalAlignment.Center;
        //    l.FontFamily = new FontFamily("Consolas");
        //    l.FontSize = 24;
        //    l.Content = gate.getLabel();
        //    //If gate is an output then the label must be prepended to the stack panel for correct formatting. 
        //    if (gate.getElementName() == "output_pin")
        //    {
        //        elementPanel.Children.Add(l);
        //    }
        //    else
        //    {
        //        elementPanel.Children.Insert(0, l);
        //    }
        //}

    }
}
