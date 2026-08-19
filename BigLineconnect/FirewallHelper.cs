using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BigLineconnect
{
    /// <summary>
    /// BigLineconnect Windows Defender Firewall Auto UDP Rule Inbound Engine
    /// Automatically ensures Inbound UDP rule exists on Windows Firewall with 0ms UI impact and silent failsafe.
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
                    // Execute netsh command silently in background to add UDP Inbound Rule
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "netsh.exe",
                        Arguments = "advfirewall firewall add rule name=\"BigLineconnect UDP\" dir=in action=allow protocol=UDP localport=any",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    using (Process? p = Process.Start(psi))
                    {
                        p?.WaitForExit(1000);
                    }
                }
                catch { }
            });
        }
    }
}
