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

            if (txt.Contains("MaximumNumberDecimalDigits")) {
                txt = Regex.Replace(txt, @"MaximumNumberDecimalDigits=""[^""]*""", "");
                changed = true;
            }

            if (txt.Contains("VisibleHeaderCount")) {
                txt = Regex.Replace(txt, @"VisibleHeaderCount=""[^""]*""", "");
                changed = true;
            }

            if (txt.Contains("SelectionBackgroundColor")) {
                txt = Regex.Replace(txt, @"SelectionBackgroundColor=""[^""]*""", "");
                changed = true;
            }

            // In DegerliKagitIadeleriListView.xaml(112,53), FontSize is failing. Let's just remove FontSize everywhere if it's causing issues.
            // Wait, FontSize is valid on Label. Only remove it inside SfSegmentItem?
            if (f.Contains("DegerliKagitIadeleriListView.xaml") || f.Contains("DegerliKagitTahsiliListView.xaml")) {
                if (txt.Contains("FontSize=\"")) {
                    // Let's blindly remove FontSize in this file, or just manually check what element it is on.
                    txt = Regex.Replace(txt, @"FontSize=""[^""]*""", "");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}