using System.IO;
using System.Text;
using System.Linq;

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
                
                if (trimmed == "<syncfusion:SfNumericEntry") {
                    // Look ahead to see if it's an erroneous injection
                    bool hasValueBinding = false;
                    bool hasSelfClosing = false;
                    for (int j = i + 1; j < i + 6 && j < lines.Length; j++) {
                        string nextTrimmed = lines[j].Trim();
                        if (nextTrimmed.Contains("Value=\"{Binding")) {
                            hasValueBinding = true;
                        }
                        if (nextTrimmed.EndsWith("/>")) {
                            hasSelfClosing = true;
                            // If it's a self closing tag without Value="{Binding", it's almost certainly the Button or CustomPicker I broke
                            break;
                        }
                        if (nextTrimmed.EndsWith(">")) {
                            break;
                        }
                    }

                    // Also check if the next line is TextColor
                    bool nextIsTextColor = i + 1 < lines.Length && (lines[i+1].Trim().StartsWith("TextColor=") || lines[i+1].Trim().StartsWith("BorderColor="));

                    if (nextIsTextColor && (!hasValueBinding || hasSelfClosing)) {
                        // This is an erroneous injection from my previous scripts!
                        // In some cases (like DTSiparislerFisiNewCardView), it was a Local:CustomBorderPicker ending with VerticalOptions="FillAndExpand" />
                        lines[i] = ""; // Remove the injected line
                        changed = true;
                    }
                }
            }

            if (changed) {
                // Filter out the empty lines we just created
                var newLines = lines.Where(l => l != "").ToArray();
                File.WriteAllLines(f, newLines, Encoding.UTF8);
            }
        }
    }
}