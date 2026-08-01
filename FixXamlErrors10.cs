using System.IO;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    static void Main()
    {
        string dir = @"C:\BigMobil\Bigus_MAUI\Client\Mobile\Bigus.BigMobil\Bigus.BigMobil";
        string[] xamlFiles = Directory.GetFiles(dir, "*.xaml", SearchOption.AllDirectories);

        foreach(var f in xamlFiles) {
            string txt = File.ReadAllText(f, Encoding.UTF8);
            bool changed = false;

            // Fix the syntax error I introduced previously:
            if (txt.Contains("<syncfusion:SfNumericEntry{Binding")) {
                txt = txt.Replace("<syncfusion:SfNumericEntry{Binding", "<syncfusion:SfNumericEntry Value=\"{Binding");
                changed = true;
            }
            if (txt.Contains("<syncfusion:SfNumericEntryBorderColor=")) {
                txt = txt.Replace("<syncfusion:SfNumericEntryBorderColor=", "<syncfusion:SfNumericEntry BorderColor=");
                changed = true;
            }

            // Find missing tags by looking at standalone TextColor="Black" on a new line
            // We use Regex to find lines that start with spaces, then TextColor="Black", then spaces, then VerticalOptions="FillAndExpand"
            string pattern1 = @"(\r?\n)(\s+)TextColor=""Black""(\r?\n\s+)VerticalOptions=""FillAndExpand""";
            if (Regex.IsMatch(txt, pattern1)) {
                // To avoid replacing things that already have <syncfusion:SfNumericEntry on the previous line,
                // we can just check if the text before this match contains <syncfusion:SfNumericEntry without a closing >
                // But it's easier to just do a naive replace and see if it compiles, since TextColor="Black" alone on a line is 99% the missing tag!
                // Wait! If it's already fixed, what is before the match?
                // \n<syncfusion:SfNumericEntry\n    TextColor="Black"
                // My pattern matches \n \s+ TextColor="Black". It WOULD match!
                // So I must ensure that the line BEFORE does NOT contain <syncfusion:SfNumericEntry
            }

            // Let's do a line-by-line approach, it is infallible.
            string[] lines = txt.Split(new[] { '\r', '\n' }, System.StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++) {
                string trimmed = lines[i].Trim();
                
                // If line is TextColor="Black" and the previous non-empty line does not end with a tag or start a tag
                if (trimmed == "TextColor=\"Black\"" || trimmed == "TextColor=\"{StaticResource BlackColor}\"") {
                    if (i > 0) {
                        string prevLine = lines[i-1].Trim();
                        // If previous line is a Label closing or something, we are missing our start tag!
                        if (prevLine.EndsWith("/>") || prevLine == "") {
                            // Insert start tag!
                            int spaces = lines[i].Length - lines[i].TrimStart().Length;
                            string indent = new string(' ', spaces);
                            lines[i] = indent + "<syncfusion:SfNumericEntry\r\n" + lines[i];
                            changed = true;
                        }
                    }
                }
                
                // Another variation: BorderColor="{StaticResource BlackColor}"
                if (trimmed.StartsWith("BorderColor=\"{StaticResource BlackColor}\"") && trimmed.Contains("Value=\"{Binding IskMasModel")) {
                    if (i > 0) {
                        string prevLine = lines[i-1].Trim();
                        if (prevLine.EndsWith("/>") || prevLine == "" || prevLine.EndsWith("Black\"/>")) {
                            int spaces = lines[i].Length - lines[i].TrimStart().Length;
                            string indent = new string(' ', spaces);
                            lines[i] = indent + "<syncfusion:SfNumericEntry\r\n" + lines[i];
                            changed = true;
                        }
                    }
                }
            }

            if (changed) {
                txt = string.Join("\n", lines); // We split by \r\n but we can just join by \n, it will be fine. Actually, since we split by both \r and \n, we will have empty lines for \r.
                // Better way to do line by line without losing \r\n:
            }
        }
    }
}