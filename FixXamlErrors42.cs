using System;
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

            // Remove double SfNumericEntry tags
            string p1 = @"<syncfusion:SfNumericEntry([^>]*?)\r?\n\s*<syncfusion:SfNumericEntry\1";
            if (Regex.IsMatch(txt, p1)) {
                txt = Regex.Replace(txt, p1, "<syncfusion:SfNumericEntry");
                changed = true;
            }
            
            // Just in case the attributes are slightly different (e.g. no attributes or different spacing)
            string p2 = @"<syncfusion:SfNumericEntry([^>]*?)\r?\n\s*<syncfusion:SfNumericEntry(\s*[^>]*?)?\r?\n";
            // Wait, this is dangerous. Let's explicitly fix the known ones.
            string p3 = @"<syncfusion:SfNumericEntry\s*Grid\.Row=""\d""\s*Grid\.Column=""\d""\s*\r?\n\s*<syncfusion:SfNumericEntry\s*Grid\.Row=""\d""\s*Grid\.Column=""\d""";
            if (Regex.IsMatch(txt, p3)) {
                txt = Regex.Replace(txt, p3, match => {
                    // Just take the first one
                    return match.Value.Substring(0, match.Value.IndexOf("<syncfusion:SfNumericEntry", 1));
                });
                changed = true;
            }
            
            // Also if there's <syncfusion:SfNumericEntry Grid.Row="1" Grid.Column="0"\n<syncfusion:SfNumericEntry Grid.Row="1" Grid.Column="0"
            string p4 = @"<syncfusion:SfNumericEntry(.*?)\s*<syncfusion:SfNumericEntry\1";
            if (Regex.IsMatch(txt, p4)) {
                txt = Regex.Replace(txt, p4, "<syncfusion:SfNumericEntry");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}