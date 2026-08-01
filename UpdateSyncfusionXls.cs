using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            
            if(txt.Contains("Syncfusion.XlsIO")) {
                txt = txt.Replace("using Syncfusion.XlsIORenderer;", "using Syncfusion.Maui.DataGridExport;");
                txt = txt.Replace("using Syncfusion.XlsIO;", "using Syncfusion.XlsIO;");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}