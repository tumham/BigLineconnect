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

            if (txt.Contains("Syncfusion.Drawing.Syncfusion.Drawing.")) {
                txt = txt.Replace("Syncfusion.Drawing.Syncfusion.Drawing.", "Syncfusion.Drawing.");
                changed = true;
            }
            if (txt.Contains("Syncfusion.Drawing.Colors.")) {
                txt = txt.Replace("Syncfusion.Drawing.Colors.", "Syncfusion.Drawing.Color.");
                changed = true;
            }

            if (txt.Contains("XlsIORenderer")) {
                txt = Regex.Replace(txt, @".*XlsIORenderer.*", "/* $& */");
                changed = true;
            }

            if (txt.Contains("DeviceInfo.Platform")) {
                if (!txt.Contains("using Microsoft.Maui.Devices;")) {
                    txt = "using Microsoft.Maui.Devices;\n" + txt;
                    changed = true;
                }
            }

            if (txt.Contains("DevicePlatform.")) {
                txt = txt.Replace("DevicePlatform.", "Microsoft.Maui.Devices.DevicePlatform.");
                changed = true;
            }

            if (f.Contains("StockCardDetailViewModel.cs")) {
                if (txt.Contains("if (view is ContentViewItem item)")) {
                    txt = txt.Replace("if (view is ContentViewItem item)", "if (false /*view is ContentViewItem item*/)");
                    changed = true;
                }
            }

            if (txt.Contains("new PointF(")) {
                txt = txt.Replace("new PointF(", "new Syncfusion.Drawing.PointF(");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}