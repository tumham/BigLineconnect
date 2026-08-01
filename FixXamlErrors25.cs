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
            string oldTxt = txt;

            // 1. TitleFontColor
            txt = Regex.Replace(txt, @"\s*TitleFontColor=""[^""]*""", "");

            // 2. TextAlignment -> HorizontalTextAlignment (inside numeric:SfNumericEntry)
            // It's easier to just replace it globally if it's TextAlignment="Center", but Label uses HorizontalTextAlignment too. 
            // Xamarin Label uses HorizontalTextAlignment. TextAlignment was used in some Syncfusion controls.
            // Let's replace TextAlignment= with HorizontalTextAlignment=
            txt = txt.Replace("TextAlignment=", "HorizontalTextAlignment=");

            // 3. TabHeight
            txt = Regex.Replace(txt, @"\s*TabHeight=""[^""]*""", "");

            // 4. AutoGenerateColumns="False" -> AutoGenerateColumnsMode="None"
            txt = txt.Replace("AutoGenerateColumns=\"False\"", "AutoGenerateColumnsMode=\"None\"");
            txt = txt.Replace("AutoGenerateColumns=\"True\"", "AutoGenerateColumnsMode=\"SmartReset\"");

            // 5. Title in SfTabItem
            // My previous script replaced <tabView:SfTabItem Title=. What if it is on a newline?
            txt = Regex.Replace(txt, @"(<tabView:SfTabItem[^>]*?)(\s+)Title=", "=");

            if (txt != oldTxt) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}