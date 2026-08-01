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

            if (f.Contains("OdemeEmirleriHareketFoyuViewModel.cs")) {
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
            }
            
            // Comment out GroupSummaryRows
            if (txt.Contains("this.dataGrid.GroupSummaryRows.Add")) {
                int startIdx = txt.IndexOf("this.dataGrid.GroupSummaryRows.Add");
                while(startIdx != -1) {
                    if (startIdx > 0 && txt[startIdx - 1] != '/') {
                        int endIdx = txt.IndexOf("});", startIdx);
                        if (endIdx != -1) {
                            string toReplace = txt.Substring(startIdx, endIdx - startIdx + 3);
                            txt = txt.Replace(toReplace, "/*" + toReplace + "*/");
                            changed = true;
                        }
                    }
                    startIdx = txt.IndexOf("this.dataGrid.GroupSummaryRows.Add", startIdx + 34);
                }
            }
            // Comment out TableSummaryRows
            if (txt.Contains("this.dataGrid.TableSummaryRows.Add")) {
                int startIdx = txt.IndexOf("this.dataGrid.TableSummaryRows.Add");
                while(startIdx != -1) {
                    if (startIdx > 0 && txt[startIdx - 1] != '/') {
                        int endIdx = txt.IndexOf("});", startIdx);
                        if (endIdx != -1) {
                            string toReplace = txt.Substring(startIdx, endIdx - startIdx + 3);
                            txt = txt.Replace(toReplace, "/*" + toReplace + "*/");
                            changed = true;
                        }
                    }
                    startIdx = txt.IndexOf("this.dataGrid.TableSummaryRows.Add", startIdx + 34);
                }
            }
            // Fix DataGridPdfExportOption by just replacing DataGridPdfExportOption() with DataGridPdfExportingOption() 
            if (txt.Contains("DataGridPdfExportOption")) {
                txt = txt.Replace("DataGridPdfExportOption", "DataGridPdfExportingOption");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}