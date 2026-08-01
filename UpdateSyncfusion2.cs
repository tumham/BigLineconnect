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
            
            if(txt.Contains("Syncfusion.")) {
                txt = txt.Replace("Syncfusion.Data.XForms", "Syncfusion.Maui.Data");
                txt = txt.Replace("Syncfusion.SfDataGrid.XForms", "Syncfusion.Maui.DataGrid");
                txt = txt.Replace("Syncfusion.DataSource", "Syncfusion.Maui.DataSource");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}