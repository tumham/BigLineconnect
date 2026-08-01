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
            
            if(txt.Contains("SlideOverKit")) {
                txt = txt.Replace("using SlideOverKit;", "");
                txt = txt.Replace("SlideMenuView", "ContentView"); // fallback
                changed = true;
            }
            if(txt.Contains("IProgressDialog")) {
                txt = "using Controls.UserDialogs.Maui;\n" + txt;
                changed = true;
            }
            if(txt.Contains("using Rg.Plugins.Popup")) {
                txt = txt.Replace("using Rg.Plugins.Popup", "using Mopups");
                changed = true;
            }
            if(txt.Contains("SfRadialMenu")) {
                // Comment out SfRadialMenu properties for now
                txt = Regex.Replace(txt, @"public SfRadialMenu", "//public SfRadialMenu");
                txt = Regex.Replace(txt, @"private SfRadialMenu", "//private SfRadialMenu");
                changed = true;
            }
            if(txt.Contains("public Columns")) {
                txt = txt.Replace("public Columns", "public Syncfusion.Maui.DataGrid.ColumnCollection");
                changed = true;
            }
            if(txt.Contains("private Columns")) {
                txt = txt.Replace("private Columns", "private Syncfusion.Maui.DataGrid.ColumnCollection");
                changed = true;
            }
            if(txt.Contains("new Columns()")) {
                txt = txt.Replace("new Columns()", "new Syncfusion.Maui.DataGrid.ColumnCollection()");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
        
        // Also fix XAML SlideOverKit
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);
        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;
            if(txt.Contains("SlideOverKit")) {
                txt = Regex.Replace(txt, @"clr-namespace:SlideOverKit.*?SlideOverKit", "http://schemas.microsoft.com/dotnet/2021/maui");
                txt = txt.Replace("t:SlideMenuView", "ContentView");
                txt = txt.Replace("</t:SlideMenuView>", "</ContentView>");
                changed = true;
            }
            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}