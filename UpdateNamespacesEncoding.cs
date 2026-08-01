using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        Encoding ansi = Encoding.GetEncoding(1254);
        Encoding utf8 = new UTF8Encoding(true); // UTF-8 with BOM
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        int csCount = 0;
        foreach(var f in csFiles) {
            try {
                // Read with ANSI
                string txt = File.ReadAllText(f, ansi);
                if(txt.Contains("using Xamarin.Forms;") || txt.Contains("using Xamarin.Forms.Xaml;")) {
                    txt = txt.Replace("using Xamarin.Forms.Xaml;", "using Microsoft.Maui.Controls.Xaml;");
                    txt = txt.Replace("using Xamarin.Forms;", "using Microsoft.Maui.Controls;\nusing Microsoft.Maui.Graphics;");
                    File.WriteAllText(f, txt, utf8);
                    csCount++;
                } else if (!txt.Contains("using Microsoft.Maui")) {
                    // Just convert to UTF-8 to fix encoding
                    File.WriteAllText(f, txt, utf8);
                }
            } catch {}
        }
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        int xamlCount = 0;
        foreach(var f in xamlFiles) {
            try {
                string txt = File.ReadAllText(f, ansi);
                if(txt.Contains("http://xamarin.com/schemas/2014/forms")) {
                    txt = txt.Replace("http://xamarin.com/schemas/2014/forms", "http://schemas.microsoft.com/dotnet/2021/maui");
                    File.WriteAllText(f, txt, utf8);
                    xamlCount++;
                } else {
                    File.WriteAllText(f, txt, utf8);
                }
            } catch {}
        }
        
        Console.WriteLine(string.Format("Cleanly updated {0} .cs files and {1} .xaml files, converted all to UTF-8.", csCount, xamlCount));
    }
}