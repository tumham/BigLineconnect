using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string[] errorLogLines = File.ReadAllLines(@"C:\BigMobil\Bigus_MAUI\build_errors79.txt");
        HashSet<string> missingFixed = new HashSet<string>();

        foreach (string line in errorLogLines)
        {
            if (line.Contains("MAUIG1001") && line.Contains("does not match the end tag of 'syncfusion:SfNumericEntry'") && line.Contains("Line ")) {
                int parIndex = line.IndexOf("(");
                if (parIndex == -1) continue;
                string file = line.Substring(0, parIndex);
                
                int lineIndex = line.LastIndexOf("Line ");
                if (lineIndex == -1) continue;
                string rest = line.Substring(lineIndex + 5);
                int commaIndex = rest.IndexOf(",");
                if (commaIndex == -1) continue;
                int endTagLineNum = int.Parse(rest.Substring(0, commaIndex));

                if (File.Exists(file)) {
                    string[] flines = File.ReadAllLines(file, Encoding.UTF8);
                    
                    int behaviorLine = -1;
                    for (int i = endTagLineNum - 1; i >= Math.Max(0, endTagLineNum - 15); i--) {
                        if (flines[i].Contains("<syncfusion:SfNumericEntry.Behaviors>")) {
                            behaviorLine = i;
                            break;
                        }
                    }

                    if (behaviorLine != -1) {
                        int firstAttrLine = behaviorLine - 1;
                        
                        // If the line right before behaviors ends with > (like IsEnabled="{Binding MiktarEnabled}">), we strip the > to check if it has =
                        while (firstAttrLine >= 0) {
                            string trim = flines[firstAttrLine].Trim();
                            if (trim == "") {
                                // Skip empty lines
                                firstAttrLine--;
                                continue;
                            }
                            if (trim.StartsWith("<") || trim.EndsWith("/>")) {
                                // Hit a proper XML tag, so the attributes belonging to our missing tag ended on the PREVIOUS valid line
                                firstAttrLine++;
                                break;
                            }
                            if (trim.Contains("=")) {
                                // This is an attribute, keep going up
                                firstAttrLine--;
                            } else {
                                // Not an attribute, not a tag... maybe we hit it
                                firstAttrLine++;
                                break;
                            }
                        }

                        // Make sure we are not stuck at behaviorLine
                        if (firstAttrLine >= behaviorLine) firstAttrLine = behaviorLine - 1;

                        // Ensure we don't insert before an empty line if possible
                        while (firstAttrLine < behaviorLine && flines[firstAttrLine].Trim() == "") {
                            firstAttrLine++;
                        }

                        if (firstAttrLine != -1 && !flines[firstAttrLine].Contains("<syncfusion:SfNumericEntry")) {
                            int spaces = flines[firstAttrLine].Length - flines[firstAttrLine].TrimStart().Length;
                            string indent = new string(' ', spaces);
                            flines[firstAttrLine] = indent + "<syncfusion:SfNumericEntry\r\n" + flines[firstAttrLine];
                            File.WriteAllLines(file, flines, Encoding.UTF8);
                            missingFixed.Add(file);
                        }
                    }
                }
            }
        }
        Console.WriteLine("Fixed missing start tag in " + missingFixed.Count + " files.");
    }
}