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
            
            // Fix Color properties
            if(txt.Contains("Color.Transparent")) { txt = txt.Replace("Color.Transparent", "Colors.Transparent"); changed = true; }
            if(txt.Contains("Color.Blue")) { txt = txt.Replace("Color.Blue", "Colors.Blue"); changed = true; }
            if(txt.Contains("Color.FromHex")) { txt = txt.Replace("Color.FromHex", "Color.FromArgb"); changed = true; }
            
            // Fix Thickness
            if(txt.Contains("Thickness") && !txt.Contains("using Microsoft.Maui;")) {
                if(f.Contains("BarcodeReaderExtension.cs")) {
                    txt = "using Microsoft.Maui;\n" + txt;
                    changed = true;
                }
            }

            // Fix DataGridPdfExportOption using
            if(txt.Contains("DataGridPdfExportOption") && f.Contains("DepoSonDurumRaporlariView.xaml.cs")) {
                if(!txt.Contains("using Syncfusion.Maui.DataGridExport;")) {
                    txt = "using Syncfusion.Maui.DataGridExport;\n" + txt;
                    changed = true;
                }
            }
            
            // Fix Device.OnPlatform
            if(txt.Contains("Device.OnPlatform")) {
                txt = txt.Replace("Device.OnPlatform", "DeviceInfo.Platform == DevicePlatform.Android ? "); // Need manual inspection here but let's try a regex
            }
            
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}
