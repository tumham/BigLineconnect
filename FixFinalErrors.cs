using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        // 1. ISiparislerFisiEndPoint.cs
        string f1 = Path.Combine(dir, @"Services\Endpoints\ISiparislerFisiEndPoint.cs");
        if(File.Exists(f1)) {
            string t = File.ReadAllText(f1, Encoding.UTF8);
            t = t.Replace("GetSyncfusion.Drawing.SizeFuarList", "GetSizeFuarList");
            t = t.Replace("Task<CommonApiResponse<List<SiparislerModel>>> Syncfusion.Drawing.SizeFuarList", "Task<CommonApiResponse<List<SiparislerModel>>> GetSizeFuarList");
            File.WriteAllText(f1, t, Encoding.UTF8);
        }
        
        // 2. IProgressDialog -> IDisposable
        string[] dialogFiles = {
            Path.Combine(dir, @"Services\Dialog\DialogService.cs"),
            Path.Combine(dir, @"Services\Dialog\IDialogService.cs"),
            Path.Combine(dir, @"ViewModels\Base\ViewModelBase.cs")
        };
        foreach(var df in dialogFiles) {
            if(File.Exists(df)) {
                string t = File.ReadAllText(df, Encoding.UTF8);
                t = t.Replace("IProgressDialog", "IDisposable");
                File.WriteAllText(df, t, Encoding.UTF8);
            }
        }
        
        // 3. AnimationBase.cs
        string f2 = Path.Combine(dir, @"Animations\Base\AnimationBase.cs");
        if(File.Exists(f2)) {
            string t = File.ReadAllText(f2, Encoding.UTF8);
            t = t.Replace("Microsoft.Maui.EasingType", "Easing"); // or something
            File.WriteAllText(f2, t, Encoding.UTF8);
        }
        
        // 4. StockCardDetailViewModel and View
        string f3 = Path.Combine(dir, @"ViewModels\StockCardDetailViewModel.cs");
        if(File.Exists(f3)) {
            string t = File.ReadAllText(f3, Encoding.UTF8);
            t = Regex.Replace(t, @"using Syncfusion.*?ContentView.*?;", "");
            File.WriteAllText(f3, t, Encoding.UTF8);
        }
        
        string f4 = Path.Combine(dir, @"Views\StockCardDetailView.xaml.cs");
        if(File.Exists(f4)) {
            string t = File.ReadAllText(f4, Encoding.UTF8);
            t = Regex.Replace(t, @"using Syncfusion.*?ContentView.*?;", "");
            t = t.Replace("Syncfusion.ContentView", "Microsoft.Maui.Controls.ContentView");
            File.WriteAllText(f4, t, Encoding.UTF8);
        }
        
        // 5. Clean XAML for SfRadialMenu completely
        string xamlFile = Path.Combine(dir, @"Views\StockCardDetailView.xaml");
        if(File.Exists(xamlFile)) {
            string t = File.ReadAllText(xamlFile, Encoding.UTF8);
            // Replace everything inside syncfusion:SfRadialMenu to the end tag
            t = Regex.Replace(t, @"<syncfusion:SfRadialMenu[\s\S]*?</syncfusion:SfRadialMenu>", "");
            File.WriteAllText(xamlFile, t, Encoding.UTF8);
        }
    }
}