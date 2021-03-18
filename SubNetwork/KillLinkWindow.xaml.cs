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

namespace SubNetwork
{
    /// <summary>
    /// Interaction logic for KillLinkWindow.xaml
    /// </summary>
    public partial class KillLinkWindow : Window
    {
        public RC MyRC1 { get; set; }

        // public RC MyRC2 { get; set; }

        public KillLinkWindow()
        {
            InitializeComponent();
        }

        private void repairButton_Click(object sender, RoutedEventArgs e)
        {
            MyRC1.LinkRestore();
        }

        private void breakButton_Click(object sender, RoutedEventArgs e)
        {
            string str1 = node1Text.Text;
            string str2 = node2Text.Text;
            int a = Int32.Parse(str1);
            int b = Int32.Parse(str2);
            MyRC1.KillLinkReq(a, b);

        }
    }
}
