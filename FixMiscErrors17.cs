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

            if (f.Contains("ViewModel")) {
                if (txt.Contains("GridDateTimeColumn")) {
                    txt = txt.Replace("GridDateTimeColumn", "DataGridDateColumn");
                    changed = true;
                }
                if (txt.Contains("GridTextColumn")) {
                    txt = txt.Replace("GridTextColumn", "DataGridTextColumn");
                    changed = true;
                }
                if (txt.Contains("GridNumericColumn")) {
                    txt = txt.Replace("GridNumericColumn", "DataGridNumericColumn");
                    changed = true;
                }
                if (txt.Contains("Color.LightSkyBlue")) {
                    txt = txt.Replace("Color.LightSkyBlue", "Colors.LightSkyBlue");
                    changed = true;
                }
                if (txt.Contains("Color.Yellow")) {
                    txt = txt.Replace("Color.Yellow", "Colors.Yellow");
                    changed = true;
                }
            }
            
            if (f.Contains("EnCokSatilanUrunlerRaporuView.xaml.cs")) {
                if (txt.Contains("e.ItemData")) {
                    txt = txt.Replace("e.ItemData", "e.DataItem");
                    changed = true;
                }
                if (txt.Contains("Color.LightSkyBlue")) {
                    txt = txt.Replace("Color.LightSkyBlue", "Colors.LightSkyBlue");
                    changed = true;
                }
                if (txt.Contains("Color.LightGray")) {
                    txt = txt.Replace("Color.LightGray", "Colors.LightGray");
                    changed = true;
                }
            }

            if (f.Contains("FuarSiparislerFisiServerSync.cs")) {
                if (txt.Contains("GetDataGridSyncfusion")) {
                    txt = txt.Replace("GetDataGridSyncfusion", "GetSync");
                    changed = true;
                }
            }

            if (txt.Contains("FitAllColumnsInOnePage")) {
                txt = txt.Replace("FitAllColumnsInOnePage = true,", "//FitAllColumnsInOnePage = true,");
                changed = true;
            }
            
            if (txt.Contains("this.dataGrid.GroupColumnDescriptions.Add")) {
                txt = txt.Replace("this.dataGrid.GroupColumnDescriptions.Add", "//this.dataGrid.GroupColumnDescriptions.Add");
                changed = true;
            }
            if (txt.Contains("this.dataGrid.GroupColumnDescriptions.Clear")) {
                txt = txt.Replace("this.dataGrid.GroupColumnDescriptions.Clear", "//this.dataGrid.GroupColumnDescriptions.Clear");
                changed = true;
            }
            
            if (txt.Contains("new GroupColumnDescription()")) {
                txt = txt.Replace("new GroupColumnDescription()", "//new GroupColumnDescription()");
                changed = true;
            }
            if (txt.Contains("ColumnName = selected.Data.PropName,")) {
                txt = txt.Replace("ColumnName = selected.Data.PropName,", "//ColumnName = selected.Data.PropName,");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}