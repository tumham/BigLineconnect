using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace BigLineconnect
{
    /// <summary>
    /// BigLineconnect Windows Defender Firewall Auto Rule Registration Engine
    /// Automatically ensures Inbound and Outbound Firewall rules exist on Windows Firewall with 0ms UI impact.
    /// </summary>
    public static class FirewallHelper
    {
        private static bool _isRuleChecked = false;

        public static void EnsureUdpInboundRuleAsync()
        {
            if (_isRuleChecked) return;
            _isRuleChecked = true;

            Task.Run(() =>
            {
                try
                {
                    string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    string[] pathsToProtect = new string[]
                    {
                        currentExe,
                        @"C:\yoldas\biglineconnect.exe",
                        @"C:\ProgramData\BigLineconnect\BigLineconnect.exe",
                        @"C:\Program Files\Bigus Bilisim\BigLineconnect\BigLineconnect_App.exe"
                    };

                    foreach (var exePath in pathsToProtect)
                    {
                        if (string.IsNullOrWhiteSpace(exePath)) continue;

                        string ruleName = "BigLineconnect (" + Path.GetFileName(exePath) + ")";
                        
                        // Add Inbound Rule (TCP & UDP, All Profiles)
                        RunNetshCommand($"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow program=\"{exePath}\" enable=yes profile=any");
                        
                        // Add Outbound Rule (TCP & UDP, All Profiles)
                        RunNetshCommand($"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=allow program=\"{exePath}\" enable=yes profile=any");
                    }
                }
                catch { }
            });
        }

        private static void RunNetshCommand(string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using (Process? p = Process.Start(psi))
                {
                    p?.WaitForExit(500);
                }
            }
            catch { }
        }
    }
}
