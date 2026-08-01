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
                string trimmed = lines[i].Trim();
                
                // Identify missing tags by looking for TextColor or BorderColor which are floating.
                // The key identifier is that within the next 3 lines, there is a Value="{Binding line!
                if (trimmed == "TextColor=\"Black\"" || trimmed == "TextColor=\"{StaticResource BlackColor}\"" || trimmed.StartsWith("BorderColor=\"{StaticResource BlackColor}\"")) {
                    
                    // First, ensure the PREVIOUS line is not a start tag of SfNumericEntry
                    if (i > 0) {
                        string prevLine = lines[i-1].Trim();
                        if (prevLine.StartsWith("<syncfusion:SfNumericEntry")) continue; // Already has it
                    }

                    // Second, look ahead up to 3 lines for Value="{Binding
                    bool isSfNumericEntry = false;
                    for (int j = i; j < i + 4 && j < lines.Length; j++) {
                        if (lines[j].Trim().StartsWith("Value=\"{Binding")) {
                            isSfNumericEntry = true;
                            break;
                        }
                    }

                    if (isSfNumericEntry) {
                        int spaces = lines[i].Length - lines[i].TrimStart().Length;
                        string indent = new string(' ', spaces);
                        lines[i] = indent + "<syncfusion:SfNumericEntry\r\n" + lines[i];
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