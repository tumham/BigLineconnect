using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string extFile = Path.Combine(dir, "Helpers", "EnumerableExtensions.cs");
        if (File.Exists(extFile)) {
            File.Delete(extFile);
        }

        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains(".ToObservableCollection(")) {
                if (!txt.Contains("using Bigus.BigMobil.Extensions;")) {
                    txt = "using Bigus.BigMobil.Extensions;\n" + txt;
                    changed = true;
                }
            }

            if (txt.Contains("SfSegmentItem")) {
                string oldTxt = txt;
                txt = Regex.Replace(txt, @"new\s+SfSegmentItem\(\)\{Text=""(.*?)"",\s*FontColor.*?\}", "new SfSegmentItem { Text=\"\" }");
                txt = Regex.Replace(txt, @"new\s+SfSegmentItem\(\)\{Text=""(.*?)"",\s*TextColor.*?\}", "new SfSegmentItem { Text=\"\" }");
                if (txt != oldTxt) changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}