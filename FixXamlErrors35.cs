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

                // Fix ToplanacakMiktar
                txt = Regex.Replace(txt, @"<syncfusion:SfNumericEntry Grid\.Row=""1"" Grid\.Column=""0""\r?\n=""Small""\r?\n=""FillAndExpand""\r?\n=""False""\r?\n=""FillAndExpand""\r?\n=""\{Binding ToplanacakMiktar, Mode=TwoWay\}"" />",
                    "            <syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding ToplanacakMiktar, Mode=TwoWay}\" />");

                // Fix ToplananMiktar
                txt = Regex.Replace(txt, @"<syncfusion:SfNumericEntry Grid\.Row=""1"" Grid\.Column=""1""\r?\n<syncfusion:SfNumericEntry\r?\nFontSize=""Small""\r?\nHorizontalOptions=""FillAndExpand""\r?\nIsEnabled=""False""\r?\nVerticalOptions=""FillAndExpand""\r?\nValue=""\{Binding \}""",
                    "            <syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"1\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding ToplananMiktar, Mode=TwoWay}\"");

                // Fix BirimFiyat
                txt = Regex.Replace(txt, @"<syncfusion:SfNumericEntry Grid\.Row=""3"" Grid\.Column=""0""\r?\n=""Small""\r?\n=""FillAndExpand""\r?\n=""False""\r?\n=""FillAndExpand""\r?\n=""\{Binding BirimFiyat, Mode=TwoWay\}"" />",
                    "            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding BirimFiyat, Mode=TwoWay}\" />");

                // Fix Tutar
                txt = Regex.Replace(txt, @"<syncfusion:SfNumericEntry Grid\.Row=""3"" Grid\.Column=""1""\r?\n=""Small""\r?\n=""FillAndExpand""\r?\n=""False""\r?\n=""FillAndExpand""\r?\n=""\{Binding Tutar, Mode=TwoWay\}"" />",
                    "            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"1\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding Tutar, Mode=TwoWay}\" />");

                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}