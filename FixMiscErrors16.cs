using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains("/*/*")) {
                txt = txt.Replace("/*/*", "/*");
                changed = true;
            }
            if (txt.Contains("*/*/")) {
                txt = txt.Replace("*/*/", "*/");
                changed = true;
            }
            if (txt.Contains("/* /*")) {
                txt = txt.Replace("/* /*", "/*");
                changed = true;
            }
            if (txt.Contains("*/ */")) {
                txt = txt.Replace("*/ */", "*/");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}