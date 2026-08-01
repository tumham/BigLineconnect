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
            
            // XForms ContentView issue
            if(txt.Contains("ContentView.XForms")) {
                txt = txt.Replace("Microsoft.Maui.Controls.ContentView.XForms.ItemTappedEventArgs", "ItemTappedEventArgs"); // Adjusting this generic fix
                txt = txt.Replace("Syncfusion.ContentView.XForms", "Microsoft.Maui.Controls.ContentView");
                changed = true;
            }
            if(txt.Contains("Syncfusion.Maui.ContentView")) {
                txt = txt.Replace("Syncfusion.Maui.ContentView", "Microsoft.Maui.Controls.ContentView");
                changed = true;
            }

            // CrossMultilingual issue
            if(txt.Contains("CrossMultilingual")) {
                txt = txt.Replace("CrossMultilingual.Current.CurrentCultureInfo", "System.Globalization.CultureInfo.CurrentUICulture");
                changed = true;
            }

            // BarcodeReaderExtension issues
            if(f.Contains("BarcodeReaderExtension.cs")) {
                txt = txt.Replace("GridLength", "Microsoft.Maui.GridLength");
                txt = txt.Replace("SfBorder", "Microsoft.Maui.Controls.Border"); // MAUI Border
                // Replace invalid Add overloads for Grids
                txt = Regex.Replace(txt, @"grid\.Children\.Add\(([^,]+),\s*([^,]+),\s*([^)]+)\);", "grid.Add($1, $2, $3);");
                changed = true;
            }

            // CustomEntry issue
            if(f.Contains("CustomEntry.cs")) {
                txt = txt.Replace("Device.OnPlatform", "DeviceInfo.Platform == DevicePlatform.Android ? ");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}
