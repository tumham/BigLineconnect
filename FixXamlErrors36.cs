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

            // 1. Fix orphaned blocks
            string orphanedBlockRegex = @"\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>\s*VerticalOptions=""FillAndExpand""\s*HorizontalOptions=""FillAndExpand""\s*IsEnabled=""False""/>";
            if (Regex.IsMatch(txt, orphanedBlockRegex)) {
                txt = Regex.Replace(txt, orphanedBlockRegex, @"
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

            // 2. Remove CornerRadius from buttons:SfSegmentedControl
            if (txt.Contains("buttons:SfSegmentedControl") && txt.Contains("CornerRadius=")) {
                string newTxt = Regex.Replace(txt, @"<buttons:SfSegmentedControl[^>]*>", match => {
                    string inner = match.Value;
                    inner = Regex.Replace(inner, @"\s+CornerRadius=""[^""]*""", "");
                    return inner;
                });
                if (newTxt != txt) {
                    txt = newTxt;
                    changed = true;
                }
            }

            // 3. Remove StartSwipeTemplate and EndSwipeTemplate
            if (txt.Contains("StartSwipeTemplate") || txt.Contains("EndSwipeTemplate")) {
                txt = Regex.Replace(txt, @"<syncfusion:SfListView\.StartSwipeTemplate>[\s\S]*?</syncfusion:SfListView\.StartSwipeTemplate>", "");
                txt = Regex.Replace(txt, @"<syncfusion:SfListView\.EndSwipeTemplate>[\s\S]*?</syncfusion:SfListView\.EndSwipeTemplate>", "");
                changed = true;
            }

            // 4. Remove chart:ChartDataMarker
            if (txt.Contains("chart:ChartDataMarker")) {
                txt = Regex.Replace(txt, @"<chart:ChartDataMarker[^>]*/>", "");
                txt = Regex.Replace(txt, @"<chart:ChartDataMarker[^>]*>[\s\S]*?</chart:ChartDataMarker>", "");
                changed = true;
            }

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}