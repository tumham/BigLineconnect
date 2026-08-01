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
            if(txt.Contains("Xamarin.CommunityToolkit")) {
                txt = txt.Replace("Xamarin.CommunityToolkit.Effects", "CommunityToolkit.Maui.Behaviors"); // basic mapping
                txt = txt.Replace("Xamarin.CommunityToolkit.Behaviors", "CommunityToolkit.Maui.Behaviors");
                txt = txt.Replace("Xamarin.CommunityToolkit.Converters", "CommunityToolkit.Maui.Converters");
                txt = txt.Replace("Xamarin.CommunityToolkit.UI.Views", "CommunityToolkit.Maui.Views");
                txt = txt.Replace("Xamarin.CommunityToolkit.ObjectModel", "CommunityToolkit.Mvvm.ComponentModel");
                txt = txt.Replace("Xamarin.CommunityToolkit", "CommunityToolkit.Maui");
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
        
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            if(txt.Contains("http://xamarin.com/schemas/2020/toolkit")) {
                txt = txt.Replace("http://xamarin.com/schemas/2020/toolkit", "http://schemas.microsoft.com/dotnet/2022/maui/toolkit");
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}