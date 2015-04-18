using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Turnover
{
    public class LogBox : TextBox
    {
        string filePath;
        public LogBox(string userIP)
        {
            this.Name = "privateBox_" + userIP;
            this.Multiline = true;
            this.ReadOnly = true;
            this.BackColor = Color.White;
            this.ScrollBars = ScrollBars.Vertical;

            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
                filePath = Path.Combine(path, userIP + ".txt");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    File.Create(filePath);
                }
                else
                {
                    this.Text = File.ReadAllText(filePath);
                }
            }
            catch { }
        }
        public void Append(string text)
        {
            this.AppendText(text);
            using (StreamWriter sw = File.AppendText(filePath))
            {
                sw.Write(text);
            }
        }
    }
}
