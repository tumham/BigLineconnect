using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains("TabHeaderPosition")) {
                txt = Regex.Replace(txt, @"TabHeaderPosition=""[^""]*""", "");
                changed = true;
            }
            if (txt.Contains("RightSwipeTemplate")) {
                txt = Regex.Replace(txt, @"RightSwipeTemplate=""[^""]*""", "");
                changed = true;
            }
            if (txt.Contains("LeftSwipeTemplate")) {
                txt = Regex.Replace(txt, @"LeftSwipeTemplate=""[^""]*""", "");
                changed = true;
            }
            
            // Remove FontAttributes from SfSegmentedControl and SfNumericEntry
            if (txt.Contains("syncfusion:SfSegmentedControl") || txt.Contains("syncfusion:SfNumericEntry")) {
                // regex that replaces FontAttributes inside the tag.
                // a simpler way is to just look for FontAttributes="Bold" around these elements.
                // since XAML formatting can have it on a new line, it's easier to just do:
                if (txt.Contains("FontAttributes=\"")) {
                    // Let's replace FontAttributes="[^"]*" ONLY if it's preceeded by SegmentHeight, HeightRequest, or similar syncfusion properties.
                    // Or we can just read file by file and fix the specific ones from the error list.
                    // Let's just use a naive approach: remove FontAttributes="Bold" if the file has SfSegmentedControl
                    // Actually, there could be Labels with FontAttributes="Bold".
                    // Let's do a regex that replaces FontAttributes="[^"]*" inside <syncfusion:[^>]*>
                    txt = Regex.Replace(txt, @"(<syncfusion:[a-zA-Z0-9_]+[^>]*?)FontAttributes=""[^""]*""", "");
                    changed = true;
                }
            }

            // Same for IsBusy in SfListView
            if (txt.Contains("syncfusion:SfListView")) {
                txt = Regex.Replace(txt, @"(<syncfusion:SfListView[^>]*?)IsBusy=""[^""]*""", "");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}