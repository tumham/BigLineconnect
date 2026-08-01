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
            
            // Fix Rg -> MopupService
            if(txt.Contains("Rg.Plugins.Popup.Services.PopupNavigation")) {
                txt = txt.Replace("Rg.Plugins.Popup.Services.PopupNavigation", "Mopups.Services.MopupService");
                changed = true;
            }
            if(txt.Contains("Rg.")) {
                txt = txt.Replace("Rg.Plugins.Popup.", "Mopups.");
                changed = true;
            }
            
            // Fix Color.Default and Color.Gray
            if(txt.Contains("Color.Default")) {
                txt = txt.Replace("Color.Default", "Colors.Transparent");
                changed = true;
            }
            if(txt.Contains("Color.Gray")) {
                txt = txt.Replace("Color.Gray", "Colors.Gray");
                changed = true;
            }
            if(txt.Contains("Color.Black")) {
                txt = txt.Replace("Color.Black", "Colors.Black");
                changed = true;
            }
            if(txt.Contains("Color.White")) {
                txt = txt.Replace("Color.White", "Colors.White");
                changed = true;
            }
            if(txt.Contains("Color.Red")) {
                txt = txt.Replace("Color.Red", "Colors.Red");
                changed = true;
            }
            if(txt.Contains("Color.Green")) {
                txt = txt.Replace("Color.Green", "Colors.Green");
                changed = true;
            }

            // Fix Device.OS == TargetPlatform.Android
            if(txt.Contains("Device.OS == TargetPlatform.Android")) {
                txt = txt.Replace("Device.OS == TargetPlatform.Android", "DeviceInfo.Platform == DevicePlatform.Android");
                changed = true;
            }
            if(txt.Contains("Device.OS == TargetPlatform.iOS")) {
                txt = txt.Replace("Device.OS == TargetPlatform.iOS", "DeviceInfo.Platform == DevicePlatform.iOS");
                changed = true;
            }
            
            // Fix Xamarin.Essentials
            if(txt.Contains("Xamarin.Essentials")) {
                txt = txt.Replace("Xamarin.Essentials", "Microsoft.Maui.Networking");
                changed = true;
            }
            
            // Fix Microsoft.Maui.Controls.Easing -> Microsoft.Maui.Easing
            if(txt.Contains("Microsoft.Maui.Controls.Easing")) {
                txt = txt.Replace("Microsoft.Maui.Controls.Easing", "Microsoft.Maui.Easing");
                changed = true;
            }
            
            // Fix Syncfusion.ContentView
            if(txt.Contains("Syncfusion.ContentView")) {
                txt = txt.Replace("Syncfusion.ContentView", "Microsoft.Maui.Controls.ContentView");
                changed = true;
            }

            // Fix DataGridPdfExportOption
            if(txt.Contains("DataGridPdfExportOption")) {
                txt = txt.Replace("DataGridPdfExportOption", "Syncfusion.Maui.DataGridExport.DataGridPdfExportOption");
                changed = true;
            }

            if(changed) {
                // Ensure Microsoft.Maui is present if Easing is used
                if(f.Contains("ToggleButton.cs") && !txt.Contains("using Microsoft.Maui;")) {
                    txt = "using Microsoft.Maui;\n" + txt;
                }
                
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}
