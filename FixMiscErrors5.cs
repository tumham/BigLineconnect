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

            // Fix StokSiparisFoyuView.xaml.cs
            if(f.Contains("StokSiparisFoyuView.xaml.cs")) {
                if(txt.Contains("ItemTappedEventArgs")) {
                    txt = txt.Replace("e.ItemData", "e.RowData");
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
                if(txt.Contains("GridGroupSummaryRow")) {
                    txt = txt.Replace("GridGroupSummaryRow", "DataGridGroupSummaryRow");
                    changed = true;
                }
                if(txt.Contains("ISummaryColumn")) {
                    txt = txt.Replace("ISummaryColumn", "DataGridSummaryColumn");
                    changed = true;
                }
                if(txt.Contains("GridSummaryColumn")) {
                    txt = txt.Replace("GridSummaryColumn", "DataGridSummaryColumn");
                    changed = true;
                }
                if(txt.Contains("SummaryType.DoubleAggregate")) {
                    txt = txt.Replace("SummaryType.DoubleAggregate", "Syncfusion.Maui.DataGrid.SummaryType.DoubleAggregate");
                    changed = true;
                }
            }
            
            // Fix FocusItem.cs
            if(f.Contains("FocusItem.cs") || f.Contains("ItemTappedEventArgsConverter.cs")) {
                if(txt.Contains("ContentView.XForms")) {
                    txt = txt.Replace("ContentView.XForms.", "");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}