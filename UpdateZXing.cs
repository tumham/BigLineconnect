using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            
            if(txt.Contains("ZXing")) {
                txt = txt.Replace("using ZXing.Net.Mobile.Forms;", "using ZXing.Net.Maui;\nusing ZXing.Net.Maui.Controls;");
                txt = txt.Replace("using ZXing.Mobile;", "using ZXing.Net.Maui;");
                txt = txt.Replace("ZXingScannerView", "CameraBarcodeReaderView");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            if(txt.Contains("ZXing")) {
                txt = Regex.Replace(txt, @"clr-namespace:ZXing.Net.Mobile.Forms;assembly=ZXing.Net.Mobile.Forms", "clr-namespace:ZXing.Net.Maui.Controls;assembly=ZXing.Net.Maui.Controls");
                txt = txt.Replace("ZXingScannerView", "CameraBarcodeReaderView");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}