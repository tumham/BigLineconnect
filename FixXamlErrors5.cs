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

            if (f.EndsWith("ListView.xaml") && txt.Contains("IsStickyGroupHeader") && !txt.Contains("<syncfusion:SfListView")) {
                txt = Regex.Replace(txt, @"(\s+)(IsStickyGroupHeader=""False"")", "<syncfusion:SfListView Grid.Row=\"2\"");
                changed = true;
            }

            if (txt.Contains("TextColor=\"Black\"") && txt.Contains("Value=\"{Binding")) {
                // If the block contains <syncfusion:SfNumericEntry.Behaviors> but no <syncfusion:SfNumericEntry
                if (txt.Contains("syncfusion:SfNumericEntry.Behaviors>") && !txt.Contains("<syncfusion:SfNumericEntry")) {
                    txt = Regex.Replace(txt, @"(\s+)(TextColor=""Black"")", "<syncfusion:SfNumericEntry");
                    changed = true;
                } else if (txt.Contains("syncfusion:SfNumericEntry.Behaviors>") && txt.Contains("<syncfusion:SfNumericEntry")) {
                    // Check if the number of <syncfusion:SfNumericEntry is less than the number of </syncfusion:SfNumericEntry>
                    int startTags = Regex.Matches(txt, @"<syncfusion:SfNumericEntry\b").Count;
                    int endTags = Regex.Matches(txt, @"</syncfusion:SfNumericEntry>").Count;
                    if (startTags < endTags) {
                        // Just blindly prepend to TextColor="Black" if it's right before VerticalOptions="FillAndExpand"
                        txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\s[^>]*?)(\s+)(TextColor=""Black""\s*VerticalOptions=""FillAndExpand"")", "<syncfusion:SfNumericEntry");
                        changed = true;
                    }
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}