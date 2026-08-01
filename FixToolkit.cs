using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string f = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\BossMaker\BossReport\BossReportFilterView.xaml";
        string txt = File.ReadAllText(f, Encoding.UTF8);
        
        if (!txt.Contains("xmlns:toolkit")) {
            txt = txt.Replace("xmlns:exct=""clr-namespace:Bigus.BigMobil.Controls""", "xmlns:exct=""clr-namespace:Bigus.BigMobil.Controls""\n             xmlns:toolkit=""http://schemas.microsoft.com/dotnet/2022/maui/toolkit""");
            File.WriteAllText(f, txt, Encoding.UTF8);
            Console.WriteLine("Fixed toolkit xmlns");
        }
    }
}