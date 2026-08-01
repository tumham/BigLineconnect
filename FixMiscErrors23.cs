using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        
        // Restore custom ForEach extension
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

            if (txt.Contains("using Syncfusion.Maui.DataSource.Extensions;")) {
                txt = txt.Replace("using Syncfusion.Maui.DataSource.Extensions;", "");
                changed = true;
            }

            if (txt.Contains("//FontColor removed,")) {
                txt = txt.Replace("//FontColor removed,", "} /*FontColor removed*/,");
                changed = true;
            }
            if (txt.Contains("//TextColor removed,")) {
                txt = txt.Replace("//TextColor removed,", "} /*TextColor removed*/,");
                changed = true;
            }

            if (txt.Contains("if (false //result")) {
                txt = txt.Replace("if (false //result", "if (false /*result");
                txt = txt.Replace("!= Common.Enums.SynchronizationStatus.Success)", "!= Common.Enums.SynchronizationStatus.Success*/)");
                changed = true;
            }

            if(changed) {
                File.WriteAllText(f, txt, Encoding.UTF8);
            }
        }
    }
}