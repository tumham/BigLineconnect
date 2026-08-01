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

            if (txt.Contains("/*this.dataGrid.GroupColumnDescriptions.Clear*/();")) {
                txt = txt.Replace("/*this.dataGrid.GroupColumnDescriptions.Clear*/();", "/*this.dataGrid.GroupColumnDescriptions.Clear();*/");
                changed = true;
            }
            
            string badBlock = @"/*this.dataGrid.GroupColumnDescriptions.Add*/(/*new GroupColumnDescription()*/
                {
                    /*ColumnName = selected.Data.PropName,*/
                });";
            if (txt.Contains(badBlock)) {
                txt = txt.Replace(badBlock, "/*this.dataGrid.GroupColumnDescriptions.Add(new GroupColumnDescription() { ColumnName = selected.Data.PropName, });*/");
                changed = true;
            } else {
                // Try flexible replacement
                int idx = txt.IndexOf("/*this.dataGrid.GroupColumnDescriptions.Add*/");
                while(idx != -1) {
                    int endIdx = txt.IndexOf("});", idx);
                    if (endIdx != -1) {
                        string block = txt.Substring(idx, endIdx - idx + 3);
                        txt = txt.Replace(block, "/* " + block.Replace("/*", "").Replace("*/", "") + " */");
                        changed = true;
                    }
                    idx = txt.IndexOf("/*this.dataGrid.GroupColumnDescriptions.Add*/", idx + 10);
                }
            }

            if (txt.Contains("FitAllColumnsInOnePage = false,")) {
                txt = txt.Replace("FitAllColumnsInOnePage = false,", "//FitAllColumnsInOnePage = false,");
                changed = true;
            }
            if (txt.Contains("FitAllColumnsInOnePage = true,")) {
                txt = txt.Replace("FitAllColumnsInOnePage = true,", "//FitAllColumnsInOnePage = true,");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}