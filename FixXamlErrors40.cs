using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string[] files = new string[] {
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\ProToSipKontrolluOperasyon\ProToSipKontrolluSatirDetayView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\ProToSipKontrolluOperasyon\SatirDonusturme\ProToSipPartiLotTakipliView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\ProToSipKontrolluOperasyon\SatirDonusturme\ProToSipRenkBedenView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\SiparisOperasyonlari\SiparisIrsaliyeKontrollu\SatirDonusturme\SIPLTakipliView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Operasyonlar\SiparisOperasyonlari\SiparisIrsaliyeKontrollu\SatirDonusturme\SIRBTakipliView.xaml"
        };

        foreach(var f in files) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            // 1. Fix orphaned FontSize="Small" floating
            // If we see \nFontSize="Small"\nHorizontalOptions...
            // We need to look above it to see which Grid.Row and Grid.Column it belongs to.
            // Wait, we know ToplanacakMiktar = Grid.Row="1" Grid.Column="0"
            // ToplananMiktar = Grid.Row="1" Grid.Column="1"
            // Let's replace any missing tags explicitly.
            
            // For ProToSipKontrolluSatirDetayView: ToplananMiktar
            string p1 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding ToplananMiktar, Mode=TwoWay\}""\s*/>";
            if (Regex.IsMatch(txt, p1)) {
                txt = Regex.Replace(txt, p1, "\n            <syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"1\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding ToplananMiktar, Mode=TwoWay}\" />");
                changed = true;
            }
            
            // For ProToSipPartiLotTakipliView and others: ToplanacakMiktar
            string p2 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding ToplanacakMiktar, Mode=TwoWay\}""\s*/>";
            if (Regex.IsMatch(txt, p2)) {
                txt = Regex.Replace(txt, p2, "\n            <syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding ToplanacakMiktar, Mode=TwoWay}\" />");
                changed = true;
            }

            // Also check for BirimFiyat just in case
            string p3 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding BirimFiyat, Mode=TwoWay\}""\s*/>";
            if (Regex.IsMatch(txt, p3) && !txt.Contains("<syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\nFontSize=\"Small\"")) { // Only replace if tag is missing
                txt = Regex.Replace(txt, p3, "\n            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding BirimFiyat, Mode=TwoWay}\" />");
                changed = true;
            }

            // Also check for Tutar just in case
            string p4 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding Tutar, Mode=TwoWay\}""\s*/>";
            if (Regex.IsMatch(txt, p4) && !txt.Contains("<syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"1\"\nFontSize=\"Small\"")) {
                txt = Regex.Replace(txt, p4, "\n            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"1\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                IsEnabled=\"False\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding Tutar, Mode=TwoWay}\" />");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}