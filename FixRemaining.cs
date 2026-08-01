using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        // Fix GetSizeFuarList
        string f1 = Path.Combine(dir, @"Services\Endpoints\ISiparislerFisiEndPoint.cs");
        if(File.Exists(f1)) {
            string t = File.ReadAllText(f1, Encoding.UTF8);
            t = t.Replace("GetSyncfusion.Drawing.SizeFuarList", "GetSizeFuarList");
            File.WriteAllText(f1, t, Encoding.UTF8);
        }
        
        // Fix RefitStubs.g.cs if it exists
        string f2 = Path.Combine(dir, @"obj\Debug\net9.0-android\RefitStubs.g.cs");
        if(File.Exists(f2)) {
            string t = File.ReadAllText(f2, Encoding.UTF8);
            t = t.Replace("GetSyncfusion.Drawing.SizeFuarList", "GetSizeFuarList");
            File.WriteAllText(f2, t, Encoding.UTF8);
        }
        
        // Add using Microsoft.Maui; to EasingHelper and CustomFrame
        string f3 = Path.Combine(dir, @"Helpers\EasingHelper.cs");
        if(File.Exists(f3)) {
            string t = File.ReadAllText(f3, Encoding.UTF8);
            if(!t.Contains("using Microsoft.Maui;")) t = "using Microsoft.Maui;\n" + t;
            File.WriteAllText(f3, t, Encoding.UTF8);
        }
        
        string f4 = Path.Combine(dir, @"Controls\CustomFrame.cs");
        if(File.Exists(f4)) {
            string t = File.ReadAllText(f4, Encoding.UTF8);
            if(!t.Contains("using Microsoft.Maui;")) t = "using Microsoft.Maui;\n" + t;
            File.WriteAllText(f4, t, Encoding.UTF8);
        }
        
        // Add using Controls.UserDialogs.Maui; to Dialog services
        string[] dialogFiles = {
            Path.Combine(dir, @"Services\Dialog\DialogService.cs"),
            Path.Combine(dir, @"Services\Dialog\IDialogService.cs"),
            Path.Combine(dir, @"ViewModels\Base\ViewModelBase.cs")
        };
        foreach(var df in dialogFiles) {
            if(File.Exists(df)) {
                string t = File.ReadAllText(df, Encoding.UTF8);
                if(!t.Contains("using Controls.UserDialogs.Maui;")) t = "using Controls.UserDialogs.Maui;\n" + t;
                File.WriteAllText(df, t, Encoding.UTF8);
            }
        }
        
        // Fix EasingType in AnimationBase.cs
        string f5 = Path.Combine(dir, @"Animations\Base\AnimationBase.cs");
        if(File.Exists(f5)) {
            string t = File.ReadAllText(f5, Encoding.UTF8);
            t = t.Replace("public EasingType", "public Microsoft.Maui.Easing");
            File.WriteAllText(f5, t, Encoding.UTF8);
        }
        
        // Clean StockCardDetailView
        string xamlFile = Path.Combine(dir, @"Views\StockCardDetailView.xaml");
        if(File.Exists(xamlFile)) {
            string t = File.ReadAllText(xamlFile, Encoding.UTF8);
            t = Regex.Replace(t, @"<syncfusion:SfRadialMenu.*?</syncfusion:SfRadialMenu>", "", RegexOptions.Singleline);
            File.WriteAllText(xamlFile, t, Encoding.UTF8);
        }
        
        string csFile = Path.Combine(dir, @"Views\StockCardDetailView.xaml.cs");
        if(File.Exists(csFile)) {
            string t = File.ReadAllText(csFile, Encoding.UTF8);
            t = Regex.Replace(t, @"using Syncfusion.*?;", "");
            t = t.Replace("private ContentView", "//private ContentView");
            t = t.Replace("new ContentView()", "//new ContentView()");
            t = Regex.Replace(t, @"radialMenu.*?;", ""); // Clean radial menu instantiations if any
            File.WriteAllText(csFile, t, Encoding.UTF8);
        }
    }
}