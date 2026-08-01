using System;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);

        foreach(var f in xamlFiles) {
            string[] lines = File.ReadAllLines(f, Encoding.UTF8);
            bool changed = false;

            for (int i = 0; i < lines.Length; i++) {
                if (lines[i].Trim() == "<syncfusion:SfNumericEntry") {
                    // Look back up to 5 lines
                    bool isDuplicate = false;
                    for (int j = i - 1; j >= Math.Max(0, i - 10); j--) {
                        string trim = lines[j].Trim();
                        if (trim.StartsWith("<syncfusion:SfNumericEntry") && !trim.Contains("Behaviors")) {
                            isDuplicate = true;
                            break;
                        }
                        if (trim.EndsWith(">") || trim.EndsWith("/>")) {
                            break;
                        }
                    }

                    if (isDuplicate) {
                        lines[i] = ""; // remove the duplicate injected tag
                        changed = true;
                    }
                }
            }

            if (changed) {
                File.WriteAllLines(f, lines, Encoding.UTF8);
            }
        }
    }
}