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

            if (txt.Contains("AllowSwiping=")) {
                txt = Regex.Replace(txt, @"\s*AllowSwiping=""[^""]*""", "");
                changed = true;
            }

            if (txt.Contains("MaximumDecimalDigits=")) {
                txt = Regex.Replace(txt, @"\s*MaximumDecimalDigits=""[^""]*""", "");
                changed = true;
            }

            if (txt.Contains("buttons:SfSegmentedControl")) {
                // Delete properties from SfSegmentedControl
                txt = Regex.Replace(txt, @"(<buttons:SfSegmentedControl[^>]*?)(\s+Color=""[^""]*"")", "");
                txt = Regex.Replace(txt, @"(<buttons:SfSegmentedControl[^>]*?)(\s+BorderColor=""[^""]*"")", "");
                txt = Regex.Replace(txt, @"(<buttons:SfSegmentedControl[^>]*?)(\s+FontSize=""[^""]*"")", "");
                txt = Regex.Replace(txt, @"(<buttons:SfSegmentedControl[^>]*?)(\s+FontColor=""[^""]*"")", "");
                txt = Regex.Replace(txt, @"(<buttons:SfSegmentedControl[^>]*?)(\s+SelectionTextColor=""[^""]*"")", "");
                txt = Regex.Replace(txt, @"(<buttons:SfSegmentedControl[^>]*?)(\s+VisibleSegmentsCount=""[^""]*"")", "");
                
                // Since Regex.Replace processes from left to right, it might miss multiple attributes if we don't loop
                // but actually since properties can be on different lines, [^>]*? will match across lines and find the property.
                // However, doing this multiple times is fine.
                // Wait, if I replace , it leaves the rest of the tag untouched.
                
                // Let's just do it simpler: just replace those specific properties anywhere in the file if they are known to cause errors.
                // Wait! BorderColor, FontSize, Color are used widely in Grid, Frame, Label! I CANNOT delete them globally!
                // I must delete them ONLY within <buttons:SfSegmentedControl
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}