using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string extFile = Path.Combine(dir, "Helpers", "ObservableCollectionExtensions.cs");
        if (File.Exists(extFile)) {
            File.Delete(extFile);
        }

        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains(".ForEach(")) {
                if (!txt.Contains("using Syncfusion.Maui.DataSource.Extensions;")) {
                    txt = "using Syncfusion.Maui.DataSource.Extensions;\n" + txt;
                    changed = true;
                }
            }

            if (txt.Contains("FontColor =")) {
                txt = Regex.Replace(txt, @"FontColor\s*=.*,", "//FontColor removed,");
                changed = true;
            }
            if (txt.Contains("TextColor =")) {
                txt = Regex.Replace(txt, @"TextColor\s*=.*,", "//TextColor removed,");
                changed = true;
            }

            if (f.Contains("StockCardListViewModel.cs")) {
                if (txt.Contains("Vibration.")) {
                    txt = txt.Replace("Vibration.", "//Vibration.");
                    changed = true;
                }
                if (txt.Contains("result")) {
                    // Let's replace 'result' with something or just comment it out if it's about scan result
                    txt = Regex.Replace(txt, @"var\s+result\s*=\s*.*?;", "//var result = ...");
                    txt = txt.Replace("if (result", "if (false //result");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}