using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string f = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Helpers\Settings.cs";
        string txt = File.ReadAllText(f, Encoding.UTF8);
        
        txt = txt.Replace("using Plugin.Settings;", "using Microsoft.Maui.Storage;");
        txt = txt.Replace("using Plugin.Settings.Abstractions;", "");
        txt = txt.Replace("private static ISettings AppSettings", "private static IPreferences AppSettings");
        txt = txt.Replace("return CrossSettings.Current;", "return Preferences.Default;");
        txt = txt.Replace("AppSettings.GetValueOrDefault", "AppSettings.Get");
        txt = txt.Replace("AppSettings.AddOrUpdateValue", "AppSettings.Set");
        
        File.WriteAllText(f, txt, Encoding.UTF8);
    }
}