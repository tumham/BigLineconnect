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
            
            if(txt.Contains("using Syncfusion.Maui.DataGridExport;")) {
                txt = txt.Replace("using Syncfusion.Maui.DataGridExport;", "using Syncfusion.Maui.DataGrid.Exporting;");
                changed = true;
            }
            if(txt.Contains("Syncfusion.Maui.DataGridExport.DataGridPdfExportOption")) {
                txt = txt.Replace("Syncfusion.Maui.DataGridExport.DataGridPdfExportOption", "Syncfusion.Maui.DataGrid.Exporting.DataGridPdfExportOption");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}
