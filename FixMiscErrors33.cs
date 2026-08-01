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

            if (txt.Contains("new PointF(")) {
                txt = txt.Replace("new PointF(", "new Syncfusion.Drawing.PointF(");
                changed = true;
            }

            // Fix renderer and settings for XlsIORenderer
            if (txt.Contains("/* XlsIORenderer")) {
                txt = Regex.Replace(txt, @"(.*settings.*)", "/*  */");
                txt = Regex.Replace(txt, @"(.*renderer.*)", "/*  */");
                // Also device info
                txt = Regex.Replace(txt, @"(.*DeviceInfo\.Platform.*)", "/*  */");
                changed = true;
            }
            
            // Clean up any Syncfusion.Drawing.Syncfusion.Drawing.PointF that might be created
            if (txt.Contains("Syncfusion.Drawing.Syncfusion.Drawing.")) {
                txt = txt.Replace("Syncfusion.Drawing.Syncfusion.Drawing.", "Syncfusion.Drawing.");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}