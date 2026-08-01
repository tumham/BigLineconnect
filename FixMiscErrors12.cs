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

            if (txt.Contains("this.dataGrid.GroupSummaryRows.Add")) {
                int startIdx = txt.IndexOf("this.dataGrid.GroupSummaryRows.Add");
                while(startIdx != -1) {
                    int endIdx = txt.IndexOf("});", startIdx);
                    if (endIdx != -1) {
                        string toReplace = txt.Substring(startIdx, endIdx - startIdx + 3);
                        txt = txt.Replace(toReplace, "/*" + toReplace + "*/");
                        changed = true;
                    }
                    startIdx = txt.IndexOf("this.dataGrid.GroupSummaryRows.Add", startIdx + 1);
                }
            }
            if (txt.Contains("this.dataGrid.TableSummaryRows.Add")) {
                int startIdx = txt.IndexOf("this.dataGrid.TableSummaryRows.Add");
                while(startIdx != -1) {
                    int endIdx = txt.IndexOf("});", startIdx);
                    if (endIdx != -1) {
                        string toReplace = txt.Substring(startIdx, endIdx - startIdx + 3);
                        txt = txt.Replace(toReplace, "/*" + toReplace + "*/");
                        changed = true;
                    }
                    startIdx = txt.IndexOf("this.dataGrid.TableSummaryRows.Add", startIdx + 1);
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}