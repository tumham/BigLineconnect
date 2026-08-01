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

            // Fix ListView
            if (txt.Contains("IsStickyGroupHeader=\"False\"") && !txt.Contains("<syncfusion:SfListView")) {
                txt = Regex.Replace(txt, @"(\s+)(IsStickyGroupHeader=""False"")", "<syncfusion:SfListView Grid.Row=\"2\"");
                changed = true;
            }
            if (txt.Contains("IsStickyGroupHeader=\"False\"") && txt.Contains("<syncfusion:SfListView")) {
                int listViews = Regex.Matches(txt, @"<syncfusion:SfListView").Count;
                int endListViews = Regex.Matches(txt, @"</syncfusion:SfListView>").Count;
                if (listViews < endListViews) {
                    txt = Regex.Replace(txt, @"(?<!<syncfusion:SfListView[^>]*?)(\s+)(IsStickyGroupHeader=""False"")", "<syncfusion:SfListView Grid.Row=\"2\"");
                    changed = true;
                }
            }

            // Fix SfNumericEntry
            string pattern = @"(?<!<syncfusion:SfNumericEntry[^>]*?)(\s+)(TextColor=""Black""\s+VerticalOptions=""FillAndExpand""\s+HorizontalOptions=""FillAndExpand""\s+Value="")";
            if (Regex.IsMatch(txt, pattern)) {
                txt = Regex.Replace(txt, pattern, "<syncfusion:SfNumericEntry");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}