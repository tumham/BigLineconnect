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

            if (f.Contains("FadeToAnimation.cs")) {
                if (txt.Contains("GetEasing(Easing)")) {
                    txt = txt.Replace("GetEasing(Easing)", "GetEasing(this.Easing)");
                    changed = true;
                }
            }

            if (f.Contains("ToggleButton.cs")) {
                if (txt.Contains("Easing.Linear")) {
                    txt = txt.Replace("Easing.Linear", "Microsoft.Maui.Easing.Linear");
                    changed = true;
                }
            }

            if (f.Contains("CustomBorderDatePicker.cs")) {
                if (txt.Contains("Device.OnPlatform<double>(6, 7, 7)")) {
                    txt = txt.Replace("Device.OnPlatform<double>(6, 7, 7)", "7.0");
                    changed = true;
                }
            }

            if (f.Contains("FocusItem.cs")) {
                if (txt.Contains("_menu.IsOpen = true;")) {
                    txt = txt.Replace("_menu.IsOpen = true;", "//_menu.IsOpen = true;");
                    changed = true;
                }
            }

            if (f.Contains("SiparisIrsaliyeKntrlOprsynViewModel.cs") || 
                f.Contains("FaturaEvrakiNewCardViewModel.cs")) {
                if (txt.Contains("settings.PdfConformanceLevel")) {
                    txt = txt.Replace("settings.PdfConformanceLevel", "//settings.PdfConformanceLevel");
                    changed = true;
                }
                if (txt.Contains("settings.EmbedFonts")) {
                    txt = txt.Replace("settings.EmbedFonts", "//settings.EmbedFonts");
                    changed = true;
                }
                if (txt.Contains("renderer.ConvertToPDF(worksheet, settings)")) {
                    txt = txt.Replace("renderer.ConvertToPDF(worksheet, settings)", "null");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}