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

            if (txt.Contains("buttons:SfSegmentedControl")) {
                string newTxt = Regex.Replace(txt, @"<buttons:SfSegmentedControl[^>]*>", match => {
                    string inner = match.Value;
                    inner = Regex.Replace(inner, @"\s+Color=""[^""]*""", "");
                    inner = Regex.Replace(inner, @"\s+BorderColor=""[^""]*""", "");
                    inner = Regex.Replace(inner, @"\s+FontSize=""[^""]*""", "");
                    inner = Regex.Replace(inner, @"\s+FontColor=""[^""]*""", "");
                    inner = Regex.Replace(inner, @"\s+SelectionTextColor=""[^""]*""", "");
                    inner = Regex.Replace(inner, @"\s+VisibleSegmentsCount=""[^""]*""", "");
                    return inner;
                });
                
                if (newTxt != txt) {
                    txt = newTxt;
                    changed = true;
                }
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}