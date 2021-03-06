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

namespace NCC
{
    /// <summary>
    /// Interaction logic for NCCLogs.xaml
    /// </summary>
    public partial class NCCLogs : Window
    {
        public NCCLogs()
        {
            InitializeComponent();
        }
        public void NewLog(string info, NCCLogs tmp2, string color)
        {
            NCCLogs tmp = tmp2;
            string timeNow = DateTime.Now.ToString("h:mm:ss") + "." + DateTime.Now.Millisecond.ToString();
            string restLog = info;
            string fullLog = timeNow + " " + restLog;
            tmp.logBox.AppendText(Environment.NewLine);
            ColorLog(tmp.logBox, fullLog, color);
            tmp.logBox.ScrollToEnd();
        }

        public static void ColorLog(RichTextBox box, string text, string color)
        {
            BrushConverter bc = new BrushConverter();
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.BackgroundProperty,
                    bc.ConvertFromString(color));
            }
            catch (FormatException) { }
        }
    }
}
