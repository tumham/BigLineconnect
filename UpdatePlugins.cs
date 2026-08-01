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
            
            if(txt.Contains("using Plugin.Settings;")) {
                txt = txt.Replace("using Plugin.Settings;", "using Microsoft.Maui.Storage;");
                changed = true;
            }
            if(txt.Contains("using Plugin.Settings.Abstractions;")) {
                txt = txt.Replace("using Plugin.Settings.Abstractions;", "");
                changed = true;
            }
            if(txt.Contains("ISettings AppSettings")) {
                txt = txt.Replace("ISettings AppSettings", "IPreferences AppSettings");
                changed = true;
            }
            if(txt.Contains("CrossSettings.Current")) {
                txt = txt.Replace("CrossSettings.Current", "Preferences.Default");
                changed = true;
            }
            if(txt.Contains("AppSettings.GetValueOrDefault")) {
                txt = txt.Replace("AppSettings.GetValueOrDefault", "AppSettings.Get");
                changed = true;
            }
            if(txt.Contains("AppSettings.AddOrUpdateValue")) {
                txt = txt.Replace("AppSettings.AddOrUpdateValue", "AppSettings.Set");
                changed = true;
            }
            if(txt.Contains("using Plugin.Connectivity;")) {
                txt = txt.Replace("using Plugin.Connectivity;", "using Microsoft.Maui.Networking;");
                changed = true;
            }
            if(txt.Contains("CrossConnectivity.Current.IsConnected")) {
                txt = txt.Replace("CrossConnectivity.Current.IsConnected", "(Connectivity.Current.NetworkAccess == NetworkAccess.Internet)");
                changed = true;
            }
            
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}