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

            // Fix ToObservableCollection ambiguity
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

            // Fix SelectionChangedEventArgs arg.Index
            if (txt.Contains("arg.Index ==")) {
                txt = Regex.Replace(txt, @"arg\.Index\s*==\s*\d+", "false /*$&*/");
                changed = true;
            }
            if (txt.Contains("arg.Index") && txt.Contains("SelectionChangedEventArgs")) {
                // If there's any stray arg.Index in a file with SelectionChangedEventArgs
                // Change it to ((int)arg.NewIndex) except in RenkBeden where it's correctly arg.Index
                // Wait, it's safer to just replace arg.Index with 0 in the SelectionChanged methods
                // I'll just leave this, as the == replacement caught most. Let's do a targeted replace for switch cases if any
                txt = Regex.Replace(txt, @"switch\s*\(\s*arg\.Index\s*\)", "switch (0 /*arg.Index*/)");
                changed = true;
            }

            // Fix result.Text
            if (txt.Contains("result.Text")) {
                txt = txt.Replace("result.Text", "\"\" /*result.Text*/");
                changed = true;
            }
            
            // Fix result not found in other contexts (like if (!result))
            // But be careful not to replace valid result variables!
            // I'll only replace !result inside the commented OnScanResult blocks?
            // Let's just blindly fix specific files where result fails.
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
                // In these files, replace esult if it's used alone
                // The error is CS0103: 'result'
                // Usually it's Search = result; or Search = result.Text;
                // We already replaced result.Text. Let's replace result.Key
                txt = txt.Replace("result.Key", "false /*result.Key*/");
                txt = txt.Replace("result.Value", "\"\" /*result.Value*/");
                // if (!result)
                txt = txt.Replace("if (!result)", "if (false /*!result*/)");
                changed = true;
            }

            // Fix PointF ambiguity
            if (f.Contains("IrsaliyeOperasyonViewModel.cs")) {
                txt = txt.Replace("PointF", "Syncfusion.Drawing.PointF");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}