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

            // For VerilenTekliflerFisiListView.xaml and others
            if (f.EndsWith("ListView.xaml")) {
                if (txt.Contains("IsStickyGroupHeader=\"False\"") && txt.Contains("syncfusion:SfListView")) {
                    int starts = Regex.Matches(txt, @"<syncfusion:SfListView\b").Count;
                    int ends = Regex.Matches(txt, @"</syncfusion:SfListView>").Count;
                    if (starts < ends) {
                        txt = Regex.Replace(txt, @"(\s+)(IsStickyGroupHeader=""False"")", "<syncfusion:SfListView Grid.Row=\"2\"");
                        changed = true;
                    }
                }
            }

            // For SfNumericEntry
            int startsNum = Regex.Matches(txt, @"<syncfusion:SfNumericEntry\b").Count;
            int endsNum = Regex.Matches(txt, @"</syncfusion:SfNumericEntry>").Count;

            if (startsNum < endsNum) {
                // There are missing start tags!
                // We find blocks of TextColor="Black" ... Value="..." followed by <syncfusion:SfNumericEntry.Behaviors>
                // And we inject <syncfusion:SfNumericEntry right before TextColor
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\b[^>]*?)(\s+)(TextColor=""Black""\s+VerticalOptions=""FillAndExpand""\s+HorizontalOptions=""FillAndExpand"")", "<syncfusion:SfNumericEntry");
                
                // Also some might be BorderColor="{StaticResource BlackColor}"
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\b[^>]*?)(\s+)(BorderColor=""\{StaticResource BlackColor\}""\s+Value=""\{Binding IskMasModel.Masraf1, Mode=TwoWay\}"")", "<syncfusion:SfNumericEntry");
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\b[^>]*?)(\s+)(BorderColor=""\{StaticResource BlackColor\}""\s+Value=""\{Binding IskMasModel.Masraf1Yuzde, Mode=TwoWay\}"")", "<syncfusion:SfNumericEntry");

                changed = true;
            }

            // There is also an error in VerilenTekliflerFisiNewCardView.xaml(1,1): error MAUIG1001: An error occured while parsing Xaml: The '{' character, hexadecimal value 0x7B, cannot be included in a name. Line 909, position 164.
            // Let's check if there are any broken bindings that were merged
            if (txt.Contains("=\"{Binding")) {
                // This is generally fine, but we might have a broken tag name like <{Binding ...
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}