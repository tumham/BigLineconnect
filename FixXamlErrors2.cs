using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains("SfNumericTextBox")) {
                txt = txt.Replace("SfNumericTextBox", "SfNumericEntry");
                changed = true;
            }

            if (txt.Contains(" Image=\"")) {
                txt = txt.Replace(" Image=\"", " ImageSource=\"");
                changed = true;
            }

            if (txt.Contains("BorderRadius=\"")) {
                txt = txt.Replace("BorderRadius=\"", "CornerRadius=\"");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}