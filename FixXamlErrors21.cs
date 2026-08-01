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
            if (Regex.IsMatch(txt, @"(\r?\n\s+)>\r?\n\s+<syncfusion:SfListView\.ItemTemplate>")) {
                txt = Regex.Replace(txt, @"(\r?\n\s+)>\r?\n\s+<syncfusion:SfListView\.ItemTemplate>", "<syncfusion:SfListView>    <syncfusion:SfListView.ItemTemplate>");
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}