using System;
using System.IO;
using System.Text.RegularExpressions;

namespace RefactorUI
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = @"c:\Projev17YD\DUZ_V17_STD\Bigus.Aktarici.WinApp\frm_Aktarim.cs";
            string content = File.ReadAllText(filePath);
            
            // Introduce a helper method at the beginning of the class
            string helperMethod = @"
        private void SetControlText(System.Windows.Forms.Control ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                ctrl.Invoke(new Action(() => SetControlText(ctrl, text)));
            }
            else
            {
                ctrl.Text = text;
            }
        }
";
            if (!content.Contains("SetControlText(System.Windows.Forms.Control")) {
                content = content.Replace("public partial class frm_Aktarim : Form\r\n    {\r\n", "public partial class frm_Aktarim : Form\r\n    {\r\n" + helperMethod);
            }

            // Replace simple assignments like   lbl_sure1.Text = DateTime.Now.ToLongTimeString();
            // with SetControlText(lbl_sure1, DateTime.Now.ToLongTimeString());
            
            string pattern = @"^\s*(lbl_[a-zA-Z0-9_üşıöçğüÜŞİÖÇĞ\.]+)\.Text\s*=\s*(.+?);";
            
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                Match m = Regex.Match(lines[i], pattern);
                if (m.Success)
                {
                    string ctrlName = m.Groups[1].Value;
                    string valueExpr = m.Groups[2].Value;
                    
                    // keep original indentation
                    string indent = lines[i].Substring(0, lines[i].IndexOf(ctrlName));
                    
                    // replace line
                    lines[i] = indent + string.Format("SetControlText({0}, {1});", ctrlName, valueExpr);
                }
            }

            File.WriteAllText(filePath, string.Join("\r\n", lines));
            Console.WriteLine("Refactored frm_Aktarim.cs to use Thread-Safe Invoke helper.");
        }
    }
}
