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
                if (txt.Contains("_server.GetSync.Drawing.SizeFuarList")) {
                    txt = txt.Replace("_server.GetSync.Drawing.SizeFuarList", "_server.GetSizeFuarList");
                    changed = true;
                }
            }

            if (txt.Contains("DataDataGridTextColumn")) {
                txt = txt.Replace("DataDataGridTextColumn", "DataGridTextColumn");
                changed = true;
            }
            if (txt.Contains("DataDataGridNumericColumn")) {
                txt = txt.Replace("DataDataGridNumericColumn", "DataGridNumericColumn");
                changed = true;
            }
            if (txt.Contains("DataDataGridDateTimeColumn")) {
                txt = txt.Replace("DataDataGridDateTimeColumn", "DataGridDateColumn");
                changed = true;
            }

            // Remove SfSegmentItem FontColor/TextColor
            if (txt.Contains("FontColor =")) {
                txt = Regex.Replace(txt, @"FontColor\s*=\s*(Color|Colors)\.[A-Za-z]+,", "//FontColor removed");
                changed = true;
            }
            if (txt.Contains("TextColor =")) {
                txt = Regex.Replace(txt, @"TextColor\s*=\s*(Color|Colors)\.[A-Za-z]+,", "//TextColor removed");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}