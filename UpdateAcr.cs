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
            
            if(txt.Contains("Acr.UserDialogs")) {
                txt = txt.Replace("using Acr.UserDialogs;", "using Controls.UserDialogs.Maui;");
                changed = true;
            }
            if(txt.Contains("Xamarin.Essentials")) {
                txt = txt.Replace("using Xamarin.Essentials;", "using Microsoft.Maui.Storage;\nusing Microsoft.Maui.ApplicationModel;");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}