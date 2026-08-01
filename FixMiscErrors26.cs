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

            if (f.Contains("StockCardListViewModel.cs")) {
                if (txt.Contains("using Xamarin.Forms;")) {
                    txt = txt.Replace("using Xamarin.Forms;", "using Microsoft.Maui.Controls;\nusing Microsoft.Maui.Graphics;");
                    changed = true;
                }
                if (txt.Contains("using Xamarin.Essentials;")) {
                    txt = txt.Replace("using Xamarin.Essentials;", "using Microsoft.Maui.ApplicationModel;\nusing Microsoft.Maui.Storage;\nusing Microsoft.Maui.Devices;");
                    changed = true;
                }
                if (txt.Contains("using Syncfusion.XForms.Buttons;")) {
                    txt = txt.Replace("using Syncfusion.XForms.Buttons;", "using Syncfusion.Maui.Buttons;");
                    changed = true;
                }

                // Handle the colors
                if (txt.Contains("Color.Yellow")) {
                    txt = txt.Replace("Color.Yellow", "Colors.Yellow");
                    changed = true;
                }
                if (txt.Contains("Color.LightSkyBlue")) {
                    txt = txt.Replace("Color.LightSkyBlue", "Colors.LightSkyBlue");
                    changed = true;
                }
                if (txt.Contains("Color.Aqua")) {
                    txt = txt.Replace("Color.Aqua", "Colors.Aqua");
                    changed = true;
                }
                if (txt.Contains("Color.DarkBlue")) {
                    txt = txt.Replace("Color.DarkBlue", "Colors.DarkBlue");
                    changed = true;
                }

                if (txt.Contains("SfSegmentItem")) {
                    txt = Regex.Replace(txt, @"new\s+SfSegmentItem\(\)\{Text=""(.*?)"",\s*FontColor.*?\}", "new SfSegmentItem { Text=\"\" }");
                }

                if (txt.Contains(".OnScanResult +=")) {
                    txt = Regex.Replace(txt, @".*\.OnScanResult \+\=.*", "//OnScanResult");
                    changed = true;
                }
                if (txt.Contains(".OnScanResult -=")) {
                    txt = Regex.Replace(txt, @".*\.OnScanResult \-\=.*", "//OnScanResult");
                    changed = true;
                }
                if (txt.Contains("Device.BeginInvokeOnMainThread")) {
                    txt = txt.Replace("Device.BeginInvokeOnMainThread", "Application.Current.Dispatcher.Dispatch");
                    changed = true;
                }
            }

            if (txt.Contains("Vibration.")) {
                txt = txt.Replace("Vibration.", "Microsoft.Maui.Devices.Vibration.Default.");
                changed = true;
            }

            // SelectionChangedEventArgs Index
            if (txt.Contains("arg.Index")) {
                txt = txt.Replace("arg.Index", "(int?)arg.NewIndex");
                changed = true;
            }

            if (txt.Contains("Color.ForestGreen")) {
                txt = txt.Replace("Color.ForestGreen", "Colors.ForestGreen");
                changed = true;
            }
            if (txt.Contains("Color.DarkOrange")) {
                txt = txt.Replace("Color.DarkOrange", "Colors.DarkOrange");
                changed = true;
            }
            if (txt.Contains("Color.Purple")) {
                txt = txt.Replace("Color.Purple", "Colors.Purple");
                changed = true;
            }
            if (txt.Contains("Color.Brown")) {
                txt = txt.Replace("Color.Brown", "Colors.Brown");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}