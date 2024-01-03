using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace _2BNOR_2B
{
    /// <summary>
    /// Interaction logic for displayWindow.xaml
    /// </summary>
    public partial class displayWindow : Window
    {
        public displayWindow()
        {
            InitializeComponent();
        }

        public Canvas getCanvas()
        {
            return truthTableCanvas; 
        }

        public void setStatusForTables(string status)
        {
            statusBox_mainWindow.Text = "Current boolean expression: " + status;
        }
    }
}
