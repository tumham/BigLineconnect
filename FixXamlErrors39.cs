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

            // 1. sfgrid:GridTextColumn -> sfgrid:DataGridTextColumn
            if (txt.Contains("sfgrid:GridTextColumn")) {
                txt = txt.Replace("sfgrid:GridTextColumn", "sfgrid:DataGridTextColumn");
                changed = true;
            }

            // 2. CornerRadius inside SfSegmentedControl or SelectionIndicatorSettings
            if (txt.Contains("CornerRadius=")) {
                if (txt.Contains("SelectionIndicatorSettings")) {
                    txt = Regex.Replace(txt, @"<buttons:SelectionIndicatorSettings[^>]*>", match => {
                        string inner = match.Value;
                        inner = Regex.Replace(inner, @"\s+CornerRadius=""[^""]*""", "");
                        inner = Regex.Replace(inner, @"\s+Color=""[^""]*""", "");
                        return inner;
                    });
                    changed = true;
                }
                
                // Also check if any CornerRadius="" is left inside SfSegmentedControl tag itself
                string segRegex = @"<buttons:SfSegmentedControl[^>]*>";
                if (Regex.IsMatch(txt, segRegex)) {
                    string newTxt = Regex.Replace(txt, segRegex, match => {
                        string inner = match.Value;
                        inner = Regex.Replace(inner, @"\s+CornerRadius=""[^""]*""", "");
                        return inner;
                    });
                    if (newTxt != txt) {
                        txt = newTxt;
                        changed = true;
                    }
                }
            }

            // 3. StartSwipeTemplate / EndSwipeTemplate inside SfDataGrid
            if (txt.Contains("StartSwipeTemplate") || txt.Contains("EndSwipeTemplate")) {
                txt = Regex.Replace(txt, @"<sfgrid:SfDataGrid\.StartSwipeTemplate>[\s\S]*?</sfgrid:SfDataGrid\.StartSwipeTemplate>", "");
                txt = Regex.Replace(txt, @"<sfgrid:SfDataGrid\.EndSwipeTemplate>[\s\S]*?</sfgrid:SfDataGrid\.EndSwipeTemplate>", "");
                changed = true;
            }

            // 4. Children error? This means there are still some orphaned elements inside Grid or something.
            // Let's check if the file has Value="" /> missing for some reason.
            // Wait, in ProToSipPartiLotTakipliView.xaml, I verified there are no orphaned tags anymore!
            // But what about the other files?
            // "Children" error usually happens if a component has multiple child elements directly inside it but it doesn't support them.
            // Let's blindly remove <syncfusion:SfNumericEntry.Behaviors> if it's broken? No, behaviors are fine.
            // Wait, what if there's <syncfusion:SfNumericEntry... and then <syncfusion:SfNumericEntry.Behaviors> but the entry wasn't closed properly?
            // Let's just fix the files directly.

            if (changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
                Console.WriteLine("Fixed " + f);
            }
        }
    }
}