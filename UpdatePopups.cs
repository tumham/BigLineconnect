using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            
            if(txt.Contains("Rg.Plugins.Popup")) {
                txt = txt.Replace("using Rg.Plugins.Popup.Pages;", "using Mopups.Pages;");
                txt = txt.Replace("using Rg.Plugins.Popup.Services;", "using Mopups.Services;");
                txt = txt.Replace("using Rg.Plugins.Popup.Interfaces;", "using Mopups.Interfaces;");
                txt = txt.Replace("PopupNavigation.Instance", "MopupService.Instance");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            if(txt.Contains("Rg.Plugins.Popup")) {
                txt = txt.Replace("clr-namespace:Rg.Plugins.Popup.Pages;assembly=Rg.Plugins.Popup", "clr-namespace:Mopups.Pages;assembly=Mopups");
                txt = txt.Replace("clr-namespace:Rg.Plugins.Popup.Animations;assembly=Rg.Plugins.Popup", "clr-namespace:Mopups.Animations;assembly=Mopups");
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}