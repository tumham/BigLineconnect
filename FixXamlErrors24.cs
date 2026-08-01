using System;
using System.IO;
using System.Text;
using System.Linq;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);

        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            if (txt.Contains("xmlns:numeric=") && !txt.Contains("xmlns:syncfusion=") && txt.Contains("syncfusion:SfNumericEntry")) {
                txt = txt.Replace("<syncfusion:SfNumericEntry", "<numeric:SfNumericEntry");
                txt = txt.Replace("</syncfusion:SfNumericEntry", "</numeric:SfNumericEntry");
                txt = txt.Replace("<syncfusion:SfNumericEntry.Behaviors>", "<numeric:SfNumericEntry.Behaviors>");
                txt = txt.Replace("</syncfusion:SfNumericEntry.Behaviors>", "</numeric:SfNumericEntry.Behaviors>");
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}