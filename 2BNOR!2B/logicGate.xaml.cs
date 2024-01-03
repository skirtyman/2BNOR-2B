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

        public logicGate(element gate)
        {
            InitializeComponent();
            this.gate = gate;
            setImage();
        }

        public Point getInputPoint1()
        {
            return new Point(Canvas.GetLeft(this), Canvas.GetTop(this) + 10);
        }

        public Point getInputPoint2()
        {
            if (gate.getElementType() == 3)
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
            }
            else
            {
                return new Point(Canvas.GetLeft(this), Canvas.GetTop(this)+30); 
            }
        }

        public Point getOutputPoint()
        {
            return new Point(Canvas.GetLeft(this) + elementImage.Width, Canvas.GetTop(this) + elementImage.Height / 2); 
        }

        public element getGate()
        {
            return gate; 
        }

        private void setImage()
        {
            BitmapImage bitmap = new BitmapImage(); 
            string imageName = gate.getElementName();
            using (FileStream stream = new FileStream($"../../images/{imageName}.png", FileMode.Open))
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
        }
    }
}
