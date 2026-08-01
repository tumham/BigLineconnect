using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string[] files = {
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\ViewModels\LoginViewModel.cs",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\ViewModels\DemoRegisterViewModel.cs",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\ViewModels\SatinalmaVeSatisYonetimi\Operasyonlar\SiparisOperasyonlari\SiparisIrsaliyeKontrollu\SiparisIrsaliyeKntrlOprsynViewModel.cs"
        };
        
        foreach(var f in files) {
            if(!File.Exists(f)) continue;
            string txt = File.ReadAllText(f, Encoding.UTF8);
            txt = txt.Replace("using Plugin.DeviceInfo;", "using Microsoft.Maui.Devices;");
            txt = txt.Replace("CrossDeviceInfo.Current.Id", "Microsoft.Maui.Devices.DeviceInfo.Current.Name");
            txt = txt.Replace("CrossDeviceInfo.Current.DeviceName", "Microsoft.Maui.Devices.DeviceInfo.Current.Name");
            txt = txt.Replace("CrossDeviceInfo.Current.Version", "Microsoft.Maui.Devices.DeviceInfo.Current.VersionString");
            txt = txt.Replace("Xamarin.Essentials.DeviceInfo", "Microsoft.Maui.Devices.DeviceInfo");
            File.WriteAllText(f, txt, Encoding.UTF8);
        }
        
        // Fix Plugin.Messaging
        string f1 = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\UrunVeStokYonetimi\Raporlar\DepoSonDurumRaporlari\DepoSonDurumRaporlariView.xaml.cs";
        if(File.Exists(f1)) {
            string txt = File.ReadAllText(f1, Encoding.UTF8);
            txt = txt.Replace("using Plugin.Messaging;", "using Microsoft.Maui.ApplicationModel.Communication;");
            File.WriteAllText(f1, txt, Encoding.UTF8);
        }
        string f2 = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Raporlar\SiparisRaporlari\GenelAmacliSiparisRaporu\GenelAmacliSiparisRaporuView.xaml.cs";
        if(File.Exists(f2)) {
            string txt = File.ReadAllText(f2, Encoding.UTF8);
            txt = txt.Replace("using Plugin.Messaging;", "using Microsoft.Maui.ApplicationModel.Communication;");
            File.WriteAllText(f2, txt, Encoding.UTF8);
        }
    }
}