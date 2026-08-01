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
            
            if(txt.Contains("Plugin.Multilingual")) {
                txt = txt.Replace("using Plugin.Multilingual;", "");
                changed = true;
            }
            if(txt.Contains("ZXing.Net.Maui.Controls")) {
                txt = txt.Replace("using ZXing.Net.Maui.Controls;", "");
                changed = true;
            }
            if(txt.Contains("Mopups.Extensions")) {
                txt = txt.Replace("using Mopups.Extensions;", "");
                changed = true;
            }
            if(txt.Contains("SizeF")) {
                txt = txt.Replace("SizeF", "Syncfusion.Drawing.SizeF");
                changed = true;
            }
            if(txt.Contains("SfRadialMenu")) {
                txt = txt.Replace("SfRadialMenu", "ContentView");
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
            if(txt.Contains("SfChart")) {
                txt = txt.Replace("chart:SfChart", "chart:SfCartesianChart");
                changed = true;
            }
            if(txt.Contains("SfRadialMenu")) {
                txt = Regex.Replace(txt, @"<syncfusion:SfRadialMenu.*?</syncfusion:SfRadialMenu>", "", RegexOptions.Singleline);
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}