namespace BigLineTransfer;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        string initialTarget = (args != null && args.Length > 0) ? string.Join(" ", args).Trim() : "";
        Application.Run(new MainForm(initialTarget));
    }    
}