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

            if(txt.Contains("Syncfusion.Maui.DataGrid.Syncfusion.Maui.DataGrid.DataGridSummaryColumn.SummaryType.")) {
                txt = txt.Replace("Syncfusion.Maui.DataGrid.Syncfusion.Maui.DataGrid.DataGridSummaryColumn.SummaryType.", "Syncfusion.Maui.DataGrid.SummaryType.");
                changed = true;
            }
            if(txt.Contains("Syncfusion.Maui.DataGrid.SummaryType.SummaryType.")) {
                txt = txt.Replace("Syncfusion.Maui.DataGrid.SummaryType.SummaryType.", "Syncfusion.Maui.DataGrid.SummaryType.");
                changed = true;
            }
            if(txt.Contains("Syncfusion.Maui.DataGrid.DataGridSummaryColumn.SummaryType.")) {
                txt = txt.Replace("Syncfusion.Maui.DataGrid.DataGridSummaryColumn.SummaryType.", "Syncfusion.Maui.DataGrid.SummaryType.");
                changed = true;
            }
            if (txt.Contains("DataGridPdfExportOption")) {
                if(!txt.Contains("Syncfusion.Maui.DataGrid.Exporting.DataGridPdfExportOption") && !txt.Contains("using Syncfusion.Maui.DataGrid.Exporting;")) {
                    txt = "using Syncfusion.Maui.DataGrid.Exporting;\n" + txt;
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}