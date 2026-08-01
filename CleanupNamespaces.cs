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
            
            if(txt.Contains("Syncfusion.SfNumericTextBox.XForms")) {
                txt = txt.Replace("using Syncfusion.SfNumericTextBox.XForms;", "using Syncfusion.Maui.Inputs;");
                changed = true;
            }
            if(txt.Contains("Syncfusion.Maui.DataGridExport")) {
                txt = txt.Replace("using Syncfusion.Maui.DataGridExport;", "using Syncfusion.Maui.DataGrid.Exporting;");
                changed = true;
            }
            if(txt.Contains("Syncfusion.XlsIO")) {
                txt = txt.Replace("using Syncfusion.XlsIO;", "using Syncfusion.XlsIO;"); // keep it
            }
            if(txt.Contains("Syncfusion.XForms")) {
                txt = Regex.Replace(txt, @"using Syncfusion\..*?XForms.*?;", ""); // Remove any lingering XForms using
                changed = true;
            }
            if(txt.Contains("Xamarin.Forms")) {
                txt = txt.Replace("using Xamarin.Forms;", "using Microsoft.Maui.Controls;\nusing Microsoft.Maui.Graphics;");
                txt = txt.Replace("using Xamarin.Forms.Xaml;", "");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}