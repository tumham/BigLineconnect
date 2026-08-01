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

            string p1 = @"<syncfusion:SfNumericEntry Grid\.Row=""1"" Grid\.Column=""0""\s*<syncfusion:SfNumericEntry Grid\.Row=""1"" Grid\.Column=""0""";
            if (Regex.IsMatch(txt, p1)) {
                txt = Regex.Replace(txt, p1, @"<syncfusion:SfNumericEntry Grid.Row=""1"" Grid.Column=""0""");
                changed = true;
            }

            string p2 = @"<syncfusion:SfNumericEntry Grid\.Row=""1"" Grid\.Column=""1""\s*<syncfusion:SfNumericEntry Grid\.Row=""1"" Grid\.Column=""1""";
            if (Regex.IsMatch(txt, p2)) {
                txt = Regex.Replace(txt, p2, @"<syncfusion:SfNumericEntry Grid.Row=""1"" Grid.Column=""1""");
                changed = true;
            }

            string p3 = @"<syncfusion:SfNumericEntry Grid\.Row=""3"" Grid\.Column=""0""\s*<syncfusion:SfNumericEntry Grid\.Row=""3"" Grid\.Column=""0""";
            if (Regex.IsMatch(txt, p3)) {
                txt = Regex.Replace(txt, p3, @"<syncfusion:SfNumericEntry Grid.Row=""3"" Grid.Column=""0""");
                changed = true;
            }

            string p4 = @"<syncfusion:SfNumericEntry Grid\.Row=""3"" Grid\.Column=""1""\s*<syncfusion:SfNumericEntry Grid\.Row=""3"" Grid\.Column=""1""";
            if (Regex.IsMatch(txt, p4)) {
                txt = Regex.Replace(txt, p4, @"<syncfusion:SfNumericEntry Grid.Row=""3"" Grid.Column=""1""");
                changed = true;
            }
            
            // Also check for ToplananMiktar in SIDtyTkpszView.xaml
            // In SIDtyTkpszView.xaml it looked like:
            //                 <syncfusion:SfNumericEntry Grid.Row="1" Grid.Column="1"
            // <syncfusion:SfNumericEntry
            string p5 = @"<syncfusion:SfNumericEntry Grid\.Row=""1"" Grid\.Column=""1""\s*<syncfusion:SfNumericEntry\s*FontSize";
            if (Regex.IsMatch(txt, p5)) {
                txt = Regex.Replace(txt, p5, @"<syncfusion:SfNumericEntry Grid.Row=""1"" Grid.Column=""1"" FontSize");
                changed = true;
            }

            // Let's also just clean up ANY <syncfusion:SfNumericEntry that is immediately followed by <syncfusion:SfNumericEntry
            string p6 = @"<syncfusion:SfNumericEntry\s*(Grid\.Row=""\d"" Grid\.Column=""\d"")?\s*<syncfusion:SfNumericEntry(\s*Grid\.Row=""\d"" Grid\.Column=""\d"")?";
            if (Regex.IsMatch(txt, p6)) {
                txt = Regex.Replace(txt, p6, match => {
                    string rowCol = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                    return "<syncfusion:SfNumericEntry " + rowCol;
                });
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}