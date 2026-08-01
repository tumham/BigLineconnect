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
                string line = lines[i];
                if (line.Contains("<syncfusion:SfNumericEntry{Binding")) {
                    lines[i] = line.Replace("<syncfusion:SfNumericEntry{Binding", "<syncfusion:SfNumericEntry Value=\"{Binding");
                    changed = true;
                }
                if (line.Contains("<syncfusion:SfNumericEntryBorderColor=")) {
                    lines[i] = line.Replace("<syncfusion:SfNumericEntryBorderColor=", "<syncfusion:SfNumericEntry BorderColor=");
                    changed = true;
                }

                string trimmed = line.Trim();
                
                // Missing start tag detection
                if (trimmed == "TextColor=\"Black\"" || trimmed == "TextColor=\"{StaticResource BlackColor}\"") {
                    if (i > 0) {
                        string prevLine = lines[i-1].Trim();
                        // If prev line is not a start tag of SfNumericEntry
                        if (!prevLine.StartsWith("<syncfusion:SfNumericEntry")) {
                            // Verify this is part of our missing block
                            if (i + 1 < lines.Length && lines[i+1].Trim().StartsWith("VerticalOptions=\"FillAndExpand\"")) {
                                int spaces = line.Length - line.TrimStart().Length;
                                string indent = new string(' ', spaces);
                                lines[i] = indent + "<syncfusion:SfNumericEntry\r\n" + line;
                                changed = true;
                            }
                        }
                    }
                }
                
                // BorderColor missing start tag detection
                if (trimmed.StartsWith("BorderColor=\"{StaticResource BlackColor}\"") && trimmed.Contains("Value=\"{Binding IskMasModel")) {
                    if (i > 0) {
                        string prevLine = lines[i-1].Trim();
                        if (!prevLine.StartsWith("<syncfusion:SfNumericEntry")) {
                            int spaces = line.Length - line.TrimStart().Length;
                            string indent = new string(' ', spaces);
                            lines[i] = indent + "<syncfusion:SfNumericEntry\r\n" + line;
                            changed = true;
                        }
                    }
                }
            }

            if (changed) {
                File.WriteAllLines(f, lines, Encoding.UTF8);
            }
        }
    }
}