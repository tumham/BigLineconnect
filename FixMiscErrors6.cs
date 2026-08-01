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

            // Fix View Models
            if(f.Contains("DepoSonDurumRaporlariViewModel.cs")) {
                if(!txt.Contains("using Syncfusion.Maui.DataGrid;")) {
                    txt = "using Syncfusion.Maui.DataGrid;\n" + txt;
                    changed = true;
                }
            }
            
            // Fix Views
            if(f.Contains("StokSiparisFoyuView.xaml.cs") || f.Contains("StokProformaSiparisFoyuView.xaml.cs")) {
                if(txt.Contains("e.ItemData")) {
                    txt = txt.Replace("e.ItemData", "e.RowData");
                    changed = true;
                }
                if(txt.Contains("GridGroupSummaryRow")) {
                    txt = txt.Replace("GridGroupSummaryRow", "DataGridGroupSummaryRow");
                    changed = true;
                }
                if(txt.Contains("GridSummaryColumn")) {
                    txt = txt.Replace("GridSummaryColumn", "DataGridSummaryColumn");
                    changed = true;
                }
                if(txt.Contains("ISummaryColumn")) {
                    txt = txt.Replace("ISummaryColumn", "DataGridSummaryColumn");
                    changed = true;
                }
                if(txt.Contains("Color.LightSkyBlue")) {
                    txt = txt.Replace("Color.LightSkyBlue", "Colors.LightSkyBlue");
                    changed = true;
                }
                if(txt.Contains("Color.LightGray")) {
                    txt = txt.Replace("Color.LightGray", "Colors.LightGray");
                    changed = true;
                }
                if(txt.Contains("Syncfusion.Maui.DataGrid.Exporting.DataGridPdfExportOption")) {
                    txt = txt.Replace("Syncfusion.Maui.DataGrid.Exporting.DataGridPdfExportOption", "DataGridPdfExportOption");
                    changed = true;
                }
                if(txt.Contains("DataDataGridSummaryColumn")) {
                    txt = txt.Replace("DataDataGridSummaryColumn", "DataGridSummaryColumn");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}