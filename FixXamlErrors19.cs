using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string errorLog = File.ReadAllText(@"C:\BigMobil\Bigus_MAUI\build_errors81.txt");
        var matches = Regex.Matches(errorLog, @"(C:\\BigMobil\\[^\(]+)\(\d+,\d+\): error MAUIG1001: An error occured while parsing Xaml: Name cannot begin with the '<' character.*?Line (\d+)");
        
        int fixedCount = 0;
        foreach (Match m in matches)
        {
            string file = m.Groups[1].Value;
            int lineNum = int.Parse(m.Groups[2].Value);

            if (File.Exists(file)) {
                string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                // LineNum is 1-indexed. Check lineNum - 1
                if (lines[lineNum - 1].Contains("<syncfusion:SfNumericEntry")) {
                    lines[lineNum - 1] = lines[lineNum - 1].Replace("<syncfusion:SfNumericEntry", "");
                    File.WriteAllLines(file, lines, Encoding.UTF8);
                    fixedCount++;
                }
            }
        }
        Console.WriteLine("Fixed Name cannot begin with < in " + fixedCount + " files.");
    }
}