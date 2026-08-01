using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string extFile = Path.Combine(dir, "Helpers", "EnumerableExtensions.cs");
        string extCode = @"using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Bigus.BigMobil.Helpers
{
    public static class EnumerableExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
        {
            if (source == null) return new ObservableCollection<T>();
            return new ObservableCollection<T>(source);
        }
    }
}";
        File.WriteAllText(extFile, extCode, Encoding.UTF8);

        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains("Color.Aqua")) {
                txt = txt.Replace("Color.Aqua", "Colors.Aqua");
                changed = true;
            }

            if (txt.Contains("SfSegmentItem")) {
                string bad1 = "new SfSegmentItem(){Text=\"Tekli\",} /*FontColor removed*/,";
                string good1 = "new SfSegmentItem { Text=\"Tekli\" },";
                if (txt.Contains(bad1)) {
                    txt = txt.Replace(bad1, good1);
                    changed = true;
                }
                
                string bad2 = "new SfSegmentItem(){Text=\"Çoklu\",} /*FontColor removed*/,";
                string good2 = "new SfSegmentItem { Text=\"Çoklu\" },";
                if (txt.Contains(bad2)) {
                    txt = txt.Replace(bad2, good2);
                    changed = true;
                }

                // Handle oklu
                string bad3 = "new SfSegmentItem(){Text=\"oklu\",} /*FontColor removed*/,";
                if (txt.Contains(bad3)) {
                    txt = txt.Replace(bad3, good2);
                    changed = true;
                }

                // Generalized replace for any other SfSegmentItem FontColor removed
                txt = Regex.Replace(txt, @"new\s+SfSegmentItem\(\)\{Text=""(.*?)""\,\}\s*/\*FontColor removed\*/\,", "new SfSegmentItem { Text=\"\" },");
                txt = Regex.Replace(txt, @"new\s+SfSegmentItem\(\)\{Text=""(.*?)""\,\}\s*/\*TextColor removed\*/\,", "new SfSegmentItem { Text=\"\" },");
            }

            if (f.Contains("ViewModel")) {
                if (txt.Contains(".OnScanResult +=")) {
                    txt = Regex.Replace(txt, @".*\.OnScanResult \+\=.*", "//OnScanResult");
                    changed = true;
                }
                if (txt.Contains(".OnScanResult -=")) {
                    txt = Regex.Replace(txt, @".*\.OnScanResult \-\=.*", "//OnScanResult");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}