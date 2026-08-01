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

            if (f.Contains("FuarSiparislerFisiServerSync.cs")) {
                if (txt.Contains("GetSyncfusion")) {
                    txt = txt.Replace("GetSyncfusion", "GetDataGridSyncfusion");
                    changed = true;
                }
            }

            if (txt.Contains("Syncfusion.Maui.DataGrid.SummaryType.")) {
                txt = txt.Replace("Syncfusion.Maui.DataGrid.SummaryType.", "Syncfusion.Maui.DataGrid.DataGridSummaryType.");
                changed = true;
            }
            if (txt.Contains("DataGridSummaryType.CountAggregate")) {
                // Keep it
            }

            if (txt.Contains("DataGridPdfExportOption") || txt.Contains("DataGridExcelExportingController") || txt.Contains("DataGridPdfExportingController")) {
                if (!txt.Contains("using Syncfusion.Maui.DataGrid.Exporting;")) {
                    txt = "using Syncfusion.Maui.DataGrid.Exporting;\n" + txt;
                    changed = true;
                }
            }
            if (txt.Contains("DataGridGroupSummaryRow") || txt.Contains("DataGridSummaryColumn") || txt.Contains("DataGridSummaryType")) {
                if (!txt.Contains("using Syncfusion.Maui.DataGrid;")) {
                    txt = "using Syncfusion.Maui.DataGrid;\n" + txt;
                    changed = true;
                }
            }
            if (f.Contains("OfferCariDetailSlideMenuView.xaml.cs")) {
                if (txt.Contains("IsFullScreen")) {
                    txt = txt.Replace("IsFullScreen", "//IsFullScreen");
                    changed = true;
                }
                if (txt.Contains("MenuOrientations")) {
                    txt = txt.Replace("MenuOrientations", "//MenuOrientations");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}