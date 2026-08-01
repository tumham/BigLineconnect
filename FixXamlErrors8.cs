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

            int startsNum = Regex.Matches(txt, @"<syncfusion:SfNumericEntry\b").Count;
            int endsNum = Regex.Matches(txt, @"</syncfusion:SfNumericEntry>").Count;

            if (startsNum < endsNum) {
                // Find all places where </syncfusion:SfNumericEntry> is closed
                // Check if the closest tag is NOT <syncfusion:SfNumericEntry>
                // We know exactly what it looks like:
                // TextColor="Black"
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\s[^>]*?)(\s+)(TextColor=""Black""\s+VerticalOptions=""FillAndExpand"")", "<syncfusion:SfNumericEntry");
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\s[^>]*?)(\s+)(TextColor=""Black""\s*Value=""\{Binding IskMasModelSatir)", "<syncfusion:SfNumericEntry");
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\s[^>]*?)(\s+)(BorderColor=""\{StaticResource BlackColor\}""\s*Value=""\{Binding IskMasModel)", "<syncfusion:SfNumericEntry");
                
                // If it STILL doesn't match:
                // Sometimes it's BorderColor="{StaticResource BlackColor}"
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\s[^>]*?)(\s+)(BorderColor=""\{StaticResource BlackColor\}""\s+Value=""\{Binding IskMasModel\.Masraf1)", "<syncfusion:SfNumericEntry");
                txt = Regex.Replace(txt, @"(?<!<syncfusion:SfNumericEntry\s[^>]*?)(\s+)(BorderColor=""\{StaticResource BlackColor\}""\s+Value=""\{Binding IskMasModel\.Masraf1Yuzde)", "<syncfusion:SfNumericEntry");

                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}