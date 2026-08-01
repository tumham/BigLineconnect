using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string[] errorLogLines = File.ReadAllLines(@"C:\BigMobil\Bigus_MAUI\build_errors81.txt");
        
        HashSet<string> filesFixed = new HashSet<string>();

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
        }
        Console.WriteLine("Fixed Name cannot begin with < in " + filesFixed.Count + " files.");
    }
}