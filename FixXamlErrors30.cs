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

            if (txt.Contains("VerticalHorizontalTextAlignment")) {
                txt = txt.Replace("VerticalHorizontalTextAlignment=", "VerticalTextAlignment=");
                changed = true;
            }

            if (txt.Contains("<tabView:SfTabItem.HeaderContent>")) {
                txt = Regex.Replace(txt, @"<tabView:SfTabItem\.HeaderContent>[\s\S]*?</tabView:SfTabItem\.HeaderContent>", "");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}