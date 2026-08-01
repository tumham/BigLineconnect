using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string[] errorLogLines = File.ReadAllLines(@"C:\BigMobil\Bigus_MAUI\build_errors79.txt");
        
        HashSet<string> filesFixed = new HashSet<string>();
        HashSet<string> missingFixed = new HashSet<string>();

        foreach (string line in errorLogLines)
        {
            if (line.Contains("MAUIG1001") && line.Contains("Name cannot begin with the '<' character") && line.Contains("Line ")) {
                // Parse file
                int parIndex = line.IndexOf("(");
                if (parIndex == -1) continue;
                string file = line.Substring(0, parIndex);
                
                // Parse Line
                int lineIndex = line.LastIndexOf("Line ");
                if (lineIndex == -1) continue;
                string rest = line.Substring(lineIndex + 5);
                int commaIndex = rest.IndexOf(",");
                if (commaIndex == -1) continue;
                int lineNum = int.Parse(rest.Substring(0, commaIndex));

                if (File.Exists(file)) {
                    string[] flines = File.ReadAllLines(file, Encoding.UTF8);
                    if (lineNum - 1 < flines.Length && flines[lineNum - 1].Contains("<syncfusion:SfNumericEntry")) {
                        flines[lineNum - 1] = flines[lineNum - 1].Replace("<syncfusion:SfNumericEntry", "");
                        File.WriteAllLines(file, flines, Encoding.UTF8);
                        filesFixed.Add(file);
                    }
                }
            }
            else if (line.Contains("MAUIG1001") && line.Contains("does not match the end tag of 'syncfusion:SfNumericEntry'") && line.Contains("Line ")) {
                // Parse file
                int parIndex = line.IndexOf("(");
                if (parIndex == -1) continue;
                string file = line.Substring(0, parIndex);
                
                // Parse Line
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
                        int insertLine = -1;
                        for (int i = behaviorLine - 1; i >= Math.Max(0, behaviorLine - 15); i--) {
                            string trim = flines[i].Trim();
                            if (trim.EndsWith(">") || trim.StartsWith("<") || trim == "") {
                                insertLine = i + 1;
                                break;
                            }
                        }

                        if (insertLine != -1 && !flines[insertLine].Contains("<syncfusion:SfNumericEntry")) {
                            int spaces = flines[insertLine].Length - flines[insertLine].TrimStart().Length;
                            string indent = new string(' ', spaces);
                            flines[insertLine] = indent + "<syncfusion:SfNumericEntry\r\n" + flines[insertLine];
                            File.WriteAllLines(file, flines, Encoding.UTF8);
                            missingFixed.Add(file);
                        }
                    }
                }
            }
        }

        Console.WriteLine("Fixed Name cannot begin with < in " + filesFixed.Count + " files.");
        Console.WriteLine("Fixed missing start tag in " + missingFixed.Count + " files.");
    }
}