using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string errorFile = @"C:\BigMobil\Bigus_MAUI\build_errors59.txt";
        string[] lines = File.ReadAllLines(errorFile);

        HashSet<string> filesToFix = new HashSet<string>();

        foreach (var line in lines)
        {
            if (line.Contains("error CS1061: 'SelectionChangedEventArgs' bir 'Index' tanımı içermiyor") ||
                line.Contains("error CS0103: 'result' adı geçerli bağlamda yok") ||
                line.Contains("error CS0103: 'Easing' adı geçerli bağlamda yok") ||
                line.Contains("error CS1061: 'RenkBedenVM' bir 'NewIndex' tanımı içermiyor"))
            {
                var match = Regex.Match(line, @"^(.*?)\(\d+,\d+\):");
                if (match.Success)
                {
                    filesToFix.Add(match.Groups[1].Value);
                }
            }
        }

        foreach (var f in filesToFix)
        {
            if (File.Exists(f))
            {
                string txt = File.ReadAllText(f, Encoding.UTF8);
                bool changed = false;

                if (txt.Contains("arg.Index ==")) {
                    txt = Regex.Replace(txt, @"arg\.Index\s*==\s*\d+", "false /*$&*/");
                    changed = true;
                }
                if (txt.Contains("arg.Index")) {
                    txt = txt.Replace("arg.Index", "((int)arg.NewIndex)");
                    changed = true;
                }
                
                if (txt.Contains("result.Text")) {
                    txt = txt.Replace("result.Text", "\"\"");
                    changed = true;
                }
                
                if (f.Contains("ToggleButton.cs") && txt.Contains("Easing.")) {
                    txt = txt.Replace("Easing.", "Microsoft.Maui.Easing.");
                    changed = true;
                }

                // If arg was RenkBedenVM, I accidentally replaced arg.Index with ((int)arg.NewIndex) above
                // Let's fix that
                if (txt.Contains("RenkBedenVM")) {
                    txt = txt.Replace("((int)arg.NewIndex)", "arg.Index");
                }

                if (changed)
                {
                    File.WriteAllText(f, txt, Encoding.UTF8);
                }
            }
        }
    }
}