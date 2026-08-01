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

            if (txt.Contains("Icon=\"")) {
                txt = txt.Replace("Icon=\"", "IconImageSource=\"");
                changed = true;
            }

            if (txt.Contains("xmlns:ios=\"clr-namespace:Xamarin.Forms.PlatformConfiguration.iOSSpecific;assembly=Xamarin.Forms.Core\"")) {
                txt = txt.Replace("xmlns:ios=\"clr-namespace:Xamarin.Forms.PlatformConfiguration.iOSSpecific;assembly=Xamarin.Forms.Core\"", 
                                  "xmlns:ios=\"clr-namespace:Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;assembly=Microsoft.Maui.Controls\"");
                changed = true;
            }

            // Remove SelectionIndicatorSettings
            if (txt.Contains("<tabView:SfTabView.SelectionIndicatorSettings>")) {
                txt = Regex.Replace(txt, @"<tabView:SfTabView\.SelectionIndicatorSettings>[\s\S]*?</tabView:SfTabView\.SelectionIndicatorSettings>", "");
                changed = true;
            }

            // Also check for <tabView:SelectionIndicatorSettings if it's placed differently
            if (txt.Contains("<tabView:SelectionIndicatorSettings")) {
                txt = Regex.Replace(txt, @"<tabView:SelectionIndicatorSettings[\s\S]*?/>", "");
                txt = Regex.Replace(txt, @"<tabView:SelectionIndicatorSettings[\s\S]*?</tabView:SelectionIndicatorSettings>", "");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}