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
            if (Regex.IsMatch(txt, @"\r?\n(\s+)=""([^""]*)""\s*>(\r?\n\s+)<tabView:SfTabItem\.Content>")) {
                txt = Regex.Replace(txt, @"\r?\n(\s+)=""([^""]*)""\s*>(\r?\n\s+)<tabView:SfTabItem\.Content>", "\n<tabView:SfTabItem Header=\"\"><tabView:SfTabItem.Content>");
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}