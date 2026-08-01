using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        Encoding utf8 = new UTF8Encoding(false);
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        int csCount = 0;
        foreach(var f in csFiles) {
            try {
                string txt = File.ReadAllText(f, utf8);
                if(txt.Contains("using Xamarin.Forms;") || txt.Contains("using Xamarin.Forms.Xaml;")) {
                    txt = txt.Replace("using Xamarin.Forms.Xaml;", "using Microsoft.Maui.Controls.Xaml;");
                    txt = txt.Replace("using Xamarin.Forms;", "using Microsoft.Maui.Controls;\nusing Microsoft.Maui.Graphics;");
                    File.WriteAllText(f, txt, utf8);
                    csCount++;
                }
            } catch {}
        }
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        int xamlCount = 0;
        foreach(var f in xamlFiles) {
            try {
                string txt = File.ReadAllText(f, utf8);
                if(txt.Contains("http://xamarin.com/schemas/2014/forms")) {
                    txt = txt.Replace("http://xamarin.com/schemas/2014/forms", "http://schemas.microsoft.com/dotnet/2021/maui");
                    File.WriteAllText(f, txt, utf8);
                    xamlCount++;
                }
            } catch {}
        }
        
        Console.WriteLine(string.Format("Cleanly updated {0} .cs files and {1} .xaml files with UTF-8.", csCount, xamlCount));
    }
}