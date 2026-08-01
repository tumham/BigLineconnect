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

            // Look for PartiLotMiktar orphaned block
            string p1 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding PartiLotMiktar\}""\s*/>";
            if (Regex.IsMatch(txt, p1)) {
                txt = Regex.Replace(txt, p1, "\n            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding PartiLotMiktar}\" />");
                changed = true;
            }

            // Look for ANY other orphaned block ending with Value="..." />
            // Wait, I can just use a regex that matches \n \s* FontSize="Small" \n \s* HorizontalOptions... \n \s* IsEnabled... \n \s* VerticalOptions... \n \s* Value="{Binding [a-zA-Z]+}" />
            // But we don't know Grid.Row and Grid.Column.
            // But wait, the only broken files were those 5 listed in build_errors96.txt!
            // ProToSipKontrolluSatirDetayView, ProToSipPartiLotTakipliView, ProToSipRenkBedenView, SIPLTakipliView, SIRBTakipliView
            // And FixXamlErrors40.cs fixed ALL OF THEM except maybe SIPLTakipliView, because it didn't match the regex.

            // Wait, let's also fix ProToSipPartiLotTakipliView just in case PartiLotMiktar is also there
            string p2 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\s*\r?\n\s*VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding PartiLotMiktar\}""\s*/>";
            if (Regex.IsMatch(txt, p2)) {
                txt = Regex.Replace(txt, p2, "\n            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding PartiLotMiktar}\" />");
                changed = true;
            }
            
            // Wait! In SIPLTakipliView.xaml, there's a BLANK LINE!
            // \n                \n                VerticalOptions=...
            string p3 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s*\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding PartiLotMiktar\}""\s*/>";
            if (Regex.IsMatch(txt, p3)) {
                txt = Regex.Replace(txt, p3, "\n            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding PartiLotMiktar}\" />");
                changed = true;
            }

            // Also check for "RenkBedenMiktar" ?
            string p4 = @"\s+FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s*\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding RenkBedenMiktar\}""\s*/>";
            if (Regex.IsMatch(txt, p4)) {
                txt = Regex.Replace(txt, p4, "\n            <syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\n                FontSize=\"Small\"\n                HorizontalOptions=\"FillAndExpand\"\n                VerticalOptions=\"FillAndExpand\"\n                Value=\"{Binding RenkBedenMiktar}\" />");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}