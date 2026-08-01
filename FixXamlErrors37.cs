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

            // Fix orphaned attributes for ToplanacakMiktar
            string p1 = @"\r?\n(\s+)FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding ToplanacakMiktar, Mode=TwoWay\}""\s*/>";
            if (Regex.IsMatch(txt, p1)) {
                txt = Regex.Replace(txt, p1, "\n<syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"0\"\nFontSize=\"Small\"\nHorizontalOptions=\"FillAndExpand\"\nIsEnabled=\"False\"\nVerticalOptions=\"FillAndExpand\"\nValue=\"{Binding ToplanacakMiktar, Mode=TwoWay}\" />");
                changed = true;
            }

            // Fix orphaned attributes for BirimFiyat
            string p2 = @"\r?\n(\s+)FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding BirimFiyat, Mode=TwoWay\}""\s*/>";
            if (Regex.IsMatch(txt, p2)) {
                txt = Regex.Replace(txt, p2, "\n<syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"0\"\nFontSize=\"Small\"\nHorizontalOptions=\"FillAndExpand\"\nIsEnabled=\"False\"\nVerticalOptions=\"FillAndExpand\"\nValue=\"{Binding BirimFiyat, Mode=TwoWay}\" />");
                changed = true;
            }

            // Fix orphaned attributes for Tutar
            string p3 = @"\r?\n(\s+)FontSize=""Small""\r?\n\s+HorizontalOptions=""FillAndExpand""\r?\n\s+IsEnabled=""False""\r?\n\s+VerticalOptions=""FillAndExpand""\r?\n\s+Value=""\{Binding Tutar, Mode=TwoWay\}""\s*/>";
            if (Regex.IsMatch(txt, p3)) {
                txt = Regex.Replace(txt, p3, "\n<syncfusion:SfNumericEntry Grid.Row=\"3\" Grid.Column=\"1\"\nFontSize=\"Small\"\nHorizontalOptions=\"FillAndExpand\"\nIsEnabled=\"False\"\nVerticalOptions=\"FillAndExpand\"\nValue=\"{Binding Tutar, Mode=TwoWay}\" />");
                changed = true;
            }

            // Fix ToplananMiktar missing Grid.Row and Grid.Column
            if (txt.Contains("<syncfusion:SfNumericEntry\r\nFontSize=\"Small\"")) {
                txt = txt.Replace("<syncfusion:SfNumericEntry\r\nFontSize=\"Small\"", "<syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"1\"\r\nFontSize=\"Small\"");
                changed = true;
            }
            if (txt.Contains("<syncfusion:SfNumericEntry\nFontSize=\"Small\"")) {
                txt = txt.Replace("<syncfusion:SfNumericEntry\nFontSize=\"Small\"", "<syncfusion:SfNumericEntry Grid.Row=\"1\" Grid.Column=\"1\"\nFontSize=\"Small\"");
                changed = true;
            }

            // Also check for the other pattern where Value= is missing:
            string p4 = @"\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>";
            if (Regex.IsMatch(txt, p4)) {
                txt = Regex.Replace(txt, p4, @"
            <syncfusion:SfNumericEntry Grid.Row=""1"" Grid.Column=""0""
                FontSize=""Small""
                HorizontalOptions=""FillAndExpand""
                IsEnabled=""False""
                VerticalOptions=""FillAndExpand""
                Value=""{Binding ToplanacakMiktar, Mode=TwoWay}"" />
            <syncfusion:SfNumericEntry Grid.Row=""1"" Grid.Column=""1""
                FontSize=""Small""
                HorizontalOptions=""FillAndExpand""
                IsEnabled=""False""
                VerticalOptions=""FillAndExpand""
                Value=""{Binding ToplananMiktar, Mode=TwoWay}"" />
            <syncfusion:SfNumericEntry Grid.Row=""3"" Grid.Column=""0""
                FontSize=""Small""
                HorizontalOptions=""FillAndExpand""
                IsEnabled=""False""
                VerticalOptions=""FillAndExpand""
                Value=""{Binding BirimFiyat, Mode=TwoWay}"" />
            <syncfusion:SfNumericEntry Grid.Row=""3"" Grid.Column=""1""
                FontSize=""Small""
                HorizontalOptions=""FillAndExpand""
                IsEnabled=""False""
                VerticalOptions=""FillAndExpand""
                Value=""{Binding Tutar, Mode=TwoWay}"" />");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}