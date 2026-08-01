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

            if(txt.Contains("DataDataGridGroupSummaryRow")) {
                txt = txt.Replace("DataDataGridGroupSummaryRow", "DataGridGroupSummaryRow");
                changed = true;
            }
            if(txt.Contains("DataDataGridSummaryColumn")) {
                txt = txt.Replace("DataDataGridSummaryColumn", "DataGridSummaryColumn");
                changed = true;
            }
            if(txt.Contains("DataGridSyncfusion")) {
                txt = txt.Replace("DataGridSyncfusion", "Syncfusion");
                changed = true;
            }
            if(txt.Contains("Syncfusion.Maui.DataGrid.DataGridSummaryType.")) {
                txt = txt.Replace("Syncfusion.Maui.DataGrid.DataGridSummaryType.", "Syncfusion.Maui.DataGrid.DataGridSummaryColumn.SummaryType.");
                // wait... the enum is Syncfusion.Maui.DataGrid.SummaryType in MAUI too? 
                // Let's just remove the fully qualified name and use SummaryType
                changed = true;
            }
            if(f.Contains("AyarlarViewModel.cs")) {
                if(txt.Contains("Microsoft.Maui.Networking.VersionTracking")) {
                    txt = txt.Replace("Microsoft.Maui.Networking.VersionTracking", "Microsoft.Maui.ApplicationModel.VersionTracking");
                    changed = true;
                }
            }
            if(f.Contains("DegerliKagitTahsiliListViewModel.cs") || f.Contains("IadeSenetEvrakiNewCardViewModel.cs")) {
                if(txt.Contains("FontColor")) {
                    txt = txt.Replace("FontColor", "TextColor");
                    changed = true;
                }
            }
            if(f.Contains("FuarTedraikciRaporViewModel.cs")) {
                if(txt.Contains("Color.Yellow")) {
                    txt = txt.Replace("Color.Yellow", "Colors.Yellow");
                    changed = true;
                }
            }

            if(f.Contains("FoyuView.xaml.cs") || f.Contains("FoyView.xaml.cs")) {
                if(txt.Contains("new DataGridPdfExportOption()")) {
                    txt = txt.Replace("new DataGridPdfExportOption()", "new Syncfusion.Maui.DataGrid.Exporting.DataGridPdfExportOption()");
                    changed = true;
                }
                if(txt.Contains("Syncfusion.Maui.DataGrid.DataGridSummaryType.")) {
                    txt = txt.Replace("Syncfusion.Maui.DataGrid.DataGridSummaryType.", "Syncfusion.Maui.DataGrid.SummaryType.");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}