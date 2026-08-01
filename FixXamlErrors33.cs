using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string[] files = new string[] {
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\SiparisOperasyonlari\SiparisIrsaliyeKontrollu\SatirDonusturme\SISNCikisView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\SiparisOperasyonlari\SiparisIrsaliyeKontrollu\SatirDonusturme\SISNGirisView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\SiparisOperasyonlari\SiparisIrsaliyeKontrollu\SipIrsKntDtyView.xaml"
        };

        foreach(var f in files) {
            if (File.Exists(f)) {
                string txt = File.ReadAllText(f, Encoding.UTF8);
                // Fix orphaned attributes by adding <syncfusion:SfNumericEntry before FontSize="Small" if it's orphaned.
                // It looks like:
                //             
                //                 FontSize="Small"
                //                 HorizontalOptions="FillAndExpand"
                
                txt = Regex.Replace(txt, @"\r?\n(\s+)FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""{Binding ToplanacakMiktar, Mode=TwoWay}""", 
                    "\n<syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"0\"\n=\"Small\"\n=\"FillAndExpand\"\n=\"False\"\n=\"FillAndExpand\"\n=\"{Binding ToplanacakMiktar, Mode=TwoWay}\"");

                txt = Regex.Replace(txt, @"\r?\n(\s+)FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""{Binding BirimFiyat, Mode=TwoWay}""", 
                    "\n<syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\n=\"Small\"\n=\"FillAndExpand\"\n=\"False\"\n=\"FillAndExpand\"\n=\"{Binding BirimFiyat, Mode=TwoWay}\"");

                txt = Regex.Replace(txt, @"\r?\n(\s+)FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""{Binding Tutar, Mode=TwoWay}""", 
                    "\n<syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"1\"\n=\"Small\"\n=\"FillAndExpand\"\n=\"False\"\n=\"FillAndExpand\"\n=\"{Binding Tutar, Mode=TwoWay}\"");

                // Also fix the one that has <syncfusion:SfNumericEntry but missing Grid.Row and Grid.Column!
                // Wait, ToplananMiktar HAS <syncfusion:SfNumericEntry! Let's just add Grid.Row="1" Grid.Column="1" to it.
                txt = txt.Replace("<syncfusion:SfNumericEntry\r\nFontSize=\"Small\"", "<syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"1\"\r\nFontSize=\"Small\"");
                txt = txt.Replace("<syncfusion:SfNumericEntry\nFontSize=\"Small\"", "<syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"1\"\nFontSize=\"Small\"");
                
                // Let's also just try to fix ANY orphaned FontSize="Small" with a generic approach if the above doesn't match perfectly.
                txt = Regex.Replace(txt, @"(?<!<[A-Za-z0-9:]+\s+)\bFontSize=""Small""\s+HorizontalOptions=""FillAndExpand""\s+IsEnabled=""False""\s+VerticalOptions=""FillAndExpand""\s+Value=""{Binding ([^}]+)}""", 
                    "<syncfusion:SfNumericEntry\nFontSize=\"Small\"\nHorizontalOptions=\"FillAndExpand\"\nIsEnabled=\"False\"\nVerticalOptions=\"FillAndExpand\"\nValue=\"{Binding }\"");

                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}