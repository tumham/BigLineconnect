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

            if (txt.Contains("Microsoft.Maui.Devices.Microsoft.Maui.Devices.")) {
                txt = txt.Replace("Microsoft.Maui.Devices.Microsoft.Maui.Devices.", "Microsoft.Maui.Devices.");
                changed = true;
            }

            if (txt.Contains("PointF startPoint") && !txt.Contains("Syncfusion.Drawing.PointF startPoint")) {
                txt = txt.Replace("PointF startPoint", "Syncfusion.Drawing.PointF startPoint");
                changed = true;
            }
            if (txt.Contains("PointF endPoint") && !txt.Contains("Syncfusion.Drawing.PointF endPoint")) {
                txt = txt.Replace("PointF endPoint", "Syncfusion.Drawing.PointF endPoint");
                changed = true;
            }
            if (txt.Contains("PointF textPosition") && !txt.Contains("Syncfusion.Drawing.PointF textPosition")) {
                txt = txt.Replace("PointF textPosition", "Syncfusion.Drawing.PointF textPosition");
                changed = true;
            }
            if (txt.Contains("PointF Koordinat") && !txt.Contains("Syncfusion.Drawing.PointF Koordinat")) {
                txt = txt.Replace("PointF Koordinat", "Syncfusion.Drawing.PointF Koordinat");
                changed = true;
            }

            if (f.Contains("SiparislerFisiNewCardViewModel.cs") || 
                f.Contains("DTFaturaEvrakiNewCardViewModel.cs") ||
                f.Contains("FuarSiparisEvrakiNewCardViewModel.cs")) {
                
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