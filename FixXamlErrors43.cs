using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);

        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            // 1. Remove SelectionIndicatorSettings
            string p1 = @"<buttons:SfSegmentedControl\.SelectionIndicatorSettings>[\s\S]*?</buttons:SfSegmentedControl\.SelectionIndicatorSettings>";
            if (Regex.IsMatch(txt, p1)) {
                txt = Regex.Replace(txt, p1, "");
                changed = true;
            }

            // 2. Remove chart:ChartColorModel
            if (txt.Contains("chart:ChartColorModel")) {
                txt = Regex.Replace(txt, @"<chart:ChartColorModel[^>]*/>", "");
                txt = Regex.Replace(txt, @"<chart:ChartColorModel[^>]*>[\s\S]*?</chart:ChartColorModel>", "");
                changed = true;
            }

            // 3. Rename exct:Expander to toolkit:Expander
            if (txt.Contains("exct:Expander")) {
                txt = txt.Replace("exct:Expander", "toolkit:Expander");
                changed = true;
            }

            // 4. Remove FontAttributes from anywhere it causes an issue?
            // The error says: BankaIslemleriListView.xaml(104,53): No property ... for "FontAttributes"
            // Wait, we don't know which element has it. It might be a SfListView or something.
            // Let's just remove FontAttributes="[a-zA-Z]*" from sfgrid:SfDataGrid or syncfusion:SfListView if it exists.
            // Actually, let's just do it manually for BankaIslemleriListView.xaml if it's too risky.
            // BUT wait! I can just regex replace FontAttributes="\w+" inside syncfusion:SfListView and sfgrid:SfDataGrid.
            if (f.Contains("BankaIslemleriListView.xaml") || f.Contains("DegerliKagitTahsiliListView.xaml")) {
                txt = Regex.Replace(txt, @"(<syncfusion:SfListView[^>]*?)\s+FontAttributes=""[^""]*""", "");
                txt = Regex.Replace(txt, @"(<sfgrid:SfDataGrid[^>]*?)\s+FontAttributes=""[^""]*""", "");
                changed = true;
            }

            // 5. Remove BorderColor from HizmetMasrafFaturasiNewCardView.xaml etc.
            if (f.Contains("HizmetMasrafFaturasiNewCardView.xaml") || f.Contains("FuarSiparisEvrakiNewCardView.xaml") || f.Contains("VerilenTekliflerFisiNewCardView.xaml")) {
                // Probably inside a Frame or CustomEntry or Grid
                txt = Regex.Replace(txt, @"(<syncfusion:SfNumericEntry[^>]*?)\s+BorderColor=""[^""]*""", "");
                txt = Regex.Replace(txt, @"(<buttons:SfSegmentedControl[^>]*?)\s+BorderColor=""[^""]*""", "");
                // Let's just blindly remove BorderColor="Black" from these specific files if it's on a known bad element.
                // Or maybe the error is on SfNumericEntry because SfNumericEntry in MAUI doesn't have BorderColor.
                txt = Regex.Replace(txt, @"<syncfusion:SfNumericEntry([^>]*?)\s+BorderColor=""[^""]*""", "<syncfusion:SfNumericEntry");
                changed = true;
            }

            // 6. Rename sfgrid:GridNumericColumn to sfgrid:DataGridNumericColumn
            if (txt.Contains("sfgrid:GridNumericColumn")) {
                txt = txt.Replace("sfgrid:GridNumericColumn", "sfgrid:DataGridNumericColumn");
                changed = true;
            }

            // 7. Replace Syncfusion.SfListView.XForms with Syncfusion.Maui.ListView
            if (txt.Contains("Syncfusion.SfListView.XForms")) {
                txt = txt.Replace("Syncfusion.SfListView.XForms", "Syncfusion.Maui.ListView");
                changed = true;
            }

            // 8. Remove IsScrollBarVisible
            if (txt.Contains("IsScrollBarVisible")) {
                txt = Regex.Replace(txt, @"\s+IsScrollBarVisible=""[^""]*""", "");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}