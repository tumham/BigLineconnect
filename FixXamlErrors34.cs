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

            // 1. TitleFontSize
            if (txt.Contains("TitleFontSize=")) {
                txt = Regex.Replace(txt, @"\s*TitleFontSize=""[^""]*""", "");
                changed = true;
            }

            // 2. DisplayMode
            if (txt.Contains("buttons:SfSegmentedControl")) {
                string newTxt = Regex.Replace(txt, @"<buttons:SfSegmentedControl[^>]*>", match => {
                    string inner = match.Value;
                    inner = Regex.Replace(inner, @"\s+DisplayMode=""[^""]*""", "");
                    return inner;
                });
                if (newTxt != txt) {
                    txt = newTxt;
                    changed = true;
                }
            }

            // 3. LeftSwipeTemplate -> StartSwipeTemplate
            if (txt.Contains("LeftSwipeTemplate")) {
                txt = txt.Replace("LeftSwipeTemplate", "StartSwipeTemplate");
                changed = true;
            }
            if (txt.Contains("RightSwipeTemplate")) {
                txt = txt.Replace("RightSwipeTemplate", "EndSwipeTemplate");
                changed = true;
            }

            // 4. chart:ChartTitle
            if (txt.Contains("chart:ChartTitle")) {
                txt = Regex.Replace(txt, @"<chart:ChartTitle[^>]*/>", "");
                txt = Regex.Replace(txt, @"<chart:ChartTitle[^>]*>[\s\S]*?</chart:ChartTitle>", "");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}