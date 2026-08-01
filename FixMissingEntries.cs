using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string[] files = new string[] {
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Evraklar\Fatura\HizmetMasrafFaturasi\HizmetMasrafFaturasiNewCardView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Evraklar\Siparis\FuarSiparisEvraki\FuarSiparisEvrakiNewCardView.xaml",
            @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil\Views\SatinalmaVeSatisYonetimi\Evraklar\VerilenTeklif\VerilenTekliflerFisiNewCardView.xaml"
        };

        foreach (var f in files) {
            if (!File.Exists(f)) continue;
            string[] lines = File.ReadAllLines(f, Encoding.UTF8);
            bool changed = false;

            for (int i = 0; i < lines.Length; i++) {
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Iskonto1Yuzde")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""1"" Grid.Column=""0""";
                        changed = true;
                    }
                }
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Iskonto1,")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""1"" Grid.Column=""1""";
                        changed = true;
                    }
                }
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Iskonto2Yuzde")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""3"" Grid.Column=""0""";
                        changed = true;
                    }
                }
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Iskonto2,")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""3"" Grid.Column=""1""";
                        changed = true;
                    }
                }
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Iskonto3Yuzde")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""5"" Grid.Column=""0""";
                        changed = true;
                    }
                }
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Iskonto3,")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""5"" Grid.Column=""1""";
                        changed = true;
                    }
                }
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Masraf1Yuzde")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""7"" Grid.Column=""0""";
                        changed = true;
                    }
                }
                if (lines[i].Contains(@"Value=""{Binding IskMasModel.Masraf1,")) {
                    if (!lines[i-1].Contains("SfNumericEntry")) {
                        lines[i-1] = @"                                <syncfusion:SfNumericEntry Grid.Row=""7"" Grid.Column=""1""";
                        changed = true;
                    }
                }
            }

            if (changed) {
                File.WriteAllLines(f, lines, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}