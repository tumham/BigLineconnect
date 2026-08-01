using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        string extFile = Path.Combine(dir, "Helpers", "ObservableCollectionExtensions.cs");
        string extCode = @"using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Bigus.BigMobil.Helpers
{
    public static class ObservableCollectionExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source) action(item);
        }
    }
}";
        File.WriteAllText(extFile, extCode, Encoding.UTF8);

        string[] csFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        foreach(var f in csFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            if (txt.Contains("DataDataGrid")) {
                txt = txt.Replace("DataDataGrid", "DataGrid");
                changed = true;
            }
            if (txt.Contains("DataGridSyncfusion")) {
                txt = txt.Replace("DataGridSyncfusion", "Syncfusion");
                changed = true;
            }

            if (txt.Contains("Color.DarkBlue")) {
                txt = txt.Replace("Color.DarkBlue", "Colors.DarkBlue");
                changed = true;
            }

            // Segment item colors again
            if (txt.Contains("FontColor = Color")) {
                txt = Regex.Replace(txt, @"FontColor\s*=\s*(Color|Colors)\.[A-Za-z]+,", "//FontColor removed");
                changed = true;
            }
            if (txt.Contains("TextColor = Color")) {
                txt = Regex.Replace(txt, @"TextColor\s*=\s*(Color|Colors)\.[A-Za-z]+,", "//TextColor removed");
                changed = true;
            }

            if (f.Contains("StockCardListViewModel.cs")) {
                if (txt.Contains(".OnScanResult +=")) {
                    txt = Regex.Replace(txt, @".*\.OnScanResult \+\=.*", "//OnScanResult");
                    changed = true;
                }
                if (txt.Contains(".OnScanResult -=")) {
                    txt = Regex.Replace(txt, @".*\.OnScanResult \-\=.*", "//OnScanResult");
                    changed = true;
                }
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}