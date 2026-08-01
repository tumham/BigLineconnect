using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string errorLog = File.ReadAllText(@"C:\BigMobil\Bigus_MAUI\build_errors79.txt");
        var matches = Regex.Matches(errorLog, @"(C:\\BigMobil\\[^\(]+)\(\d+,\d+\): error MAUIG1001: An error occured while parsing Xaml: Name cannot begin with the '<' character.*?Line (\d+)");
        
        HashSet<string> filesFixed = new HashSet<string>();

        foreach (Match m in matches)
        {
            string file = m.Groups[1].Value;
            int lineNum = int.Parse(m.Groups[2].Value);

            if (File.Exists(file)) {
                string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                // LineNum is 1-indexed. The injected tag is at lineNum - 1
                if (lines[lineNum - 1].Contains("<syncfusion:SfNumericEntry")) {
                    lines[lineNum - 1] = lines[lineNum - 1].Replace("<syncfusion:SfNumericEntry", "");
                    File.WriteAllLines(file, lines, Encoding.UTF8);
                    filesFixed.Add(file);
                }
            }
        }

        Console.WriteLine("Fixed Name cannot begin with < in " + filesFixed.Count + " files.");

        // Now fix the missing tags
        var missingMatches = Regex.Matches(errorLog, @"(C:\\BigMobil\\[^\(]+)\(\d+,\d+\): error MAUIG1001: An error occured while parsing Xaml: The '[^']+' start tag on line \d+.*?does not match the end tag of 'syncfusion:SfNumericEntry'\. Line (\d+)");
        HashSet<string> missingFixed = new HashSet<string>();

        foreach (Match m in missingMatches)
        {
            string file = m.Groups[1].Value;
            int endTagLineNum = int.Parse(m.Groups[2].Value);

            if (File.Exists(file)) {
                string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                
                // We know the end tag is at endTagLineNum - 1.
                // The SfNumericEntry.Behaviors is usually right above it.
                // We scan backwards to find the line that starts with VerticalOptions or TextColor or IsEnabled or Grid.Row that lacks a start tag.
                // Better: find <syncfusion:SfNumericEntry.Behaviors>
                int behaviorLine = -1;
                for (int i = endTagLineNum - 1; i >= Math.Max(0, endTagLineNum - 15); i--) {
                    if (lines[i].Contains("<syncfusion:SfNumericEntry.Behaviors>")) {
                        behaviorLine = i;
                        break;
                    }
                }

                if (behaviorLine != -1) {
                    // The attributes are above behaviorLine
                    // Let's go up until we hit a blank line or a line with > or /> or </
                    int insertLine = -1;
                    for (int i = behaviorLine - 1; i >= Math.Max(0, behaviorLine - 15); i--) {
                        string trim = lines[i].Trim();
                        if (trim.EndsWith(">") || trim.StartsWith("<") || trim == "") {
                            insertLine = i + 1;
                            break;
                        }
                    }

                    if (insertLine != -1 && !lines[insertLine].Contains("<syncfusion:SfNumericEntry")) {
                        int spaces = lines[insertLine].Length - lines[insertLine].TrimStart().Length;
                        string indent = new string(' ', spaces);
                        lines[insertLine] = indent + "<syncfusion:SfNumericEntry\r\n" + lines[insertLine];
                        File.WriteAllLines(file, lines, Encoding.UTF8);
                        missingFixed.Add(file);
                    }
                }
            }
        }

        Console.WriteLine("Fixed missing start tag in " + missingFixed.Count + " files.");
    }
}