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
            
            if(txt.Contains("ZXingScannerPage")) {
                txt = txt.Replace("ZXingScannerPage", "ContentPage");
                changed = true;
            }
            if(txt.Contains("Syncfusion.ListView")) {
                txt = txt.Replace("Syncfusion.ListView.XForms.SelectionChangedEventArgs", "Syncfusion.Maui.ListView.ItemTappedEventArgs"); // approximation
                txt = txt.Replace("Syncfusion.ListView.XForms", "Syncfusion.Maui.ListView");
                changed = true;
            }
            if(txt.Contains("ObservableRangeCollection")) {
                txt = txt.Replace("ObservableRangeCollection", "System.Collections.ObjectModel.ObservableCollection");
                txt = txt.Replace("AddRange(", "foreach(var i in "); // this might break, let's just replace the type first
                changed = true;
            }
            if(txt.Contains("public CornerRadius")) {
                txt = txt.Replace("CornerRadius", "Microsoft.Maui.CornerRadius");
                changed = true;
            }
            if(txt.Contains("public Easing")) {
                txt = txt.Replace("public Easing", "public Microsoft.Maui.Easing");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
        
        // Remove SfRadialMenu from XAML
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            if(txt.Contains("SfRadialMenu")) {
                txt = Regex.Replace(txt, @"<syncfusion:SfRadialMenu.*?</syncfusion:SfRadialMenu>", "", RegexOptions.Singleline);
                txt = Regex.Replace(txt, @"<syncfusion:SfRadialMenuItem.*?</syncfusion:SfRadialMenuItem>", "", RegexOptions.Singleline);
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}