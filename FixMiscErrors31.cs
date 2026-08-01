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

            if (f.EndsWith("ObservableExtension.cs")) {
                if (txt.Contains("ToObservableCollection<T>")) {
                    txt = txt.Replace("ToObservableCollection<T>", "ToObservableCollectionBigus<T>");
                    changed = true;
                }
            } else {
                if (txt.Contains(".ToObservableCollection(")) {
                    txt = txt.Replace(".ToObservableCollection(", ".ToObservableCollectionBigus(");
                    changed = true;
                }
            }

            if (txt.Contains("arg.Index ==")) {
                txt = Regex.Replace(txt, @"arg\.Index\s*==\s*\d+", "false");
                changed = true;
            }
            if (txt.Contains("arg.Index") && txt.Contains("SelectionChangedEventArgs")) {
                txt = Regex.Replace(txt, @"switch\s*\(\s*arg\.Index\s*\)", "switch (0)");
                changed = true;
            }

            if (txt.Contains("result.Text")) {
                txt = txt.Replace("result.Text", "string.Empty");
                changed = true;
            }
            
            if (f.Contains("SayimGirisEvrakiNewCardViewModel.cs") || 
                f.Contains("DepolarArasiKonsinyeSevkNewCardViewModel.cs") ||
                f.Contains("DepolarArasiNakliyeNewCardViewModel.cs") ||
                f.Contains("DepolarArasiSevkNewCardViewModel.cs") ||
                f.Contains("DTIrsaliyeEvrakiNewCardViewModel.cs") ||
                f.Contains("FaturaSeriNumaraHareketleriSatisViewModel.cs") ||
                f.Contains("VerilenTekliflerFisiNewCardViewModel.cs") ||
                f.Contains("IrsaliyeEvrakiNewCardViewModel.cs") ||
                f.Contains("StokVirmanFisiNewCardViewModel.cs") ||
                f.Contains("DTFaturaEvrakiNewCardViewModel.cs") ||
                f.Contains("ProformaSiparislerFisiNewCardViewModel.cs") ||
                f.Contains("IrsaliyeAlisTarafiCihazHareketileriViewModel.cs")) 
            {
                if (txt.Contains("result.Key")) {
                    txt = txt.Replace("result.Key", "false");
                    changed = true;
                }
                if (txt.Contains("result.Value")) {
                    txt = txt.Replace("result.Value", "string.Empty");
                    changed = true;
                }
                if (txt.Contains("if (!result)")) {
                    txt = txt.Replace("if (!result)", "if (false)");
                    changed = true;
                }
            }

            if (f.Contains("IrsaliyeOperasyonViewModel.cs")) {
                if (txt.Contains("PointF")) {
                    txt = txt.Replace("PointF", "Syncfusion.Drawing.PointF");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}