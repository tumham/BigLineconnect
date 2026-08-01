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

            if (txt.Contains("//FitAllColumnsInOnePage = true,")) {
                txt = txt.Replace("//FitAllColumnsInOnePage = true,", "/*FitAllColumnsInOnePage = true,*/");
                changed = true;
            }

            // Also for GroupColumnDescriptions
            if (txt.Contains("//this.dataGrid.GroupColumnDescriptions.Add")) {
                txt = txt.Replace("//this.dataGrid.GroupColumnDescriptions.Add", "/*this.dataGrid.GroupColumnDescriptions.Add*/");
                changed = true;
            }
            if (txt.Contains("//this.dataGrid.GroupColumnDescriptions.Clear")) {
                txt = txt.Replace("//this.dataGrid.GroupColumnDescriptions.Clear", "/*this.dataGrid.GroupColumnDescriptions.Clear*/");
                changed = true;
            }
            if (txt.Contains("//new GroupColumnDescription()")) {
                txt = txt.Replace("//new GroupColumnDescription()", "/*new GroupColumnDescription()*/");
                changed = true;
            }
            if (txt.Contains("//ColumnName = selected.Data.PropName,")) {
                txt = txt.Replace("//ColumnName = selected.Data.PropName,", "/*ColumnName = selected.Data.PropName,*/");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}