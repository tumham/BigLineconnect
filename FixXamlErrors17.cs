using System;
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
                if (lines[i].Trim() == "<syncfusion:SfNumericEntry") {
                    // Check if the previous line is ALSO <syncfusion:SfNumericEntry
                    if (i > 0 && lines[i-1].Trim() == "<syncfusion:SfNumericEntry") {
                        lines[i] = "";
                        changed = true;
                    }
                    else if (i > 0 && lines[i-1].Trim() == "<syncfusion:SfNumericEntry>") { // just in case
                        lines[i] = "";
                        changed = true;
                    }
                }
                
                // If there's an injected <syncfusion:SfNumericEntry inside an attribute list
                // We know it's invalid if the previous line ends with " and the current line is <syncfusion:SfNumericEntry and the next line has =
                // But wait! Multiple consecutive <syncfusion:SfNumericEntry are the main issue now.
                // Let's remove ANY <syncfusion:SfNumericEntry that is preceded by another one.
                // Also, let's fix the ones where the NEXT line is <syncfusion:SfNumericEntry (which we just handled above).
            }

            // Also remove consecutive <syncfusion:SfNumericEntry (even with spaces)
            string text = string.Join("\n", lines);
            if (text.Contains("<syncfusion:SfNumericEntry\n                                             <syncfusion:SfNumericEntry")) {
                // handled by loop
            }

            // Let's use regex to replace two or more consecutive <syncfusion:SfNumericEntry (separated by whitespace)
            string pattern = @"(<syncfusion:SfNumericEntry\s*)+";
            string replaced = Regex.Replace(text, pattern, "<syncfusion:SfNumericEntry\r\n");
            
            if (replaced != text) {
                File.WriteAllText(f, replaced, Encoding.UTF8);
            } else if (changed) {
                File.WriteAllLines(f, lines, Encoding.UTF8);
            }
        }
    }
}