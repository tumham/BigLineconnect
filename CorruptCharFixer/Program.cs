using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace CorruptCharFixer
{
    class Program
    {
        static void Main(string[] args)
        {
            var map = new Dictionary<string, string>()
            {
                { "Ã‡", "Ç" },
                { "Ã§", "ç" },
                { "Äž", "Ğ" },
                { "ÄŸ", "ğ" },
                { "Ä°", "İ" },
                { "Ä±", "ı" },
                { "Ã–", "Ö" },
                { "Ã¶", "ö" },
                { "Åž", "Ş" },
                { "ÅŸ", "ş" },
                { "Ãœ", "Ü" },
                { "Ã¼", "ü" }
            };

            string rootDir = @"C:\Bigus_MAUI\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
            var files = Directory.EnumerateFiles(rootDir, "*.*", SearchOption.AllDirectories)
                .Where(s => (s.EndsWith(".cs") || s.EndsWith(".xaml")) 
                            && !s.Contains("\\obj\\") 
                            && !s.Contains("\\bin\\"))
                .ToList();

            int changedFileCount = 0;

            foreach (var file in files)
            {
                string originalText = File.ReadAllText(file, Encoding.UTF8);
                string text = originalText;
                bool changed = false;

                foreach (var kvp in map)
                {
                    if (text.Contains(kvp.Key))
                    {
                        text = text.Replace(kvp.Key, kvp.Value);
                        changed = true;
                    }
                }

                if (changed)
                {
                    File.WriteAllText(file, text, new UTF8Encoding(true)); // Save with BOM!
                    changedFileCount++;
                }
            }

            Console.WriteLine($"Fixed mojibake in {changedFileCount} files.");
        }
    }
}
