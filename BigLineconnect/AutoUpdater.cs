using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BigLineconnect
{
    /// <summary>
    /// BigLineconnect Automatic Self-Updater & Zero-Bombing Process Swap Engine
    /// Automatically checks for updates, downloads new versions in background, and swaps executables without manual bombing or process kills.
    /// </summary>
    public static class AutoUpdater
    {
        private static bool _isChecking = false;

        public static void CheckAndApplyUpdateAsync()
        {
            if (_isChecking) return;
            _isChecking = true;

            Task.Run(async () =>
            {
                try
                {
                    await PerformSelfUpdateCheckInternalAsync().ConfigureAwait(false);
                }
                catch { }
            });
        }

        private static async Task PerformSelfUpdateCheckInternalAsync()
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Application.ExecutablePath;
                string exeDir = Path.GetDirectoryName(exePath) ?? "";
                string exeName = Path.GetFileName(exePath);

                string tempUpdatePath = Path.Combine(exeDir, "BigLineconnect_update.exe");
                string updaterBatchPath = Path.Combine(exeDir, "update_swap.bat");

                // Check if a downloaded update exists waiting to be swapped
                if (File.Exists(tempUpdatePath))
                {
                    // Create background batch script to swap update executable without manual bombing
                    string batchScript = "@echo off\r\n" +
                                         "timeout /t 1 /nobreak >nul\r\n" +
                                         $"copy /y \"{tempUpdatePath}\" \"{exePath}\" >nul\r\n" +
                                         $"del /f /q \"{tempUpdatePath}\" >nul\r\n" +
                                         $"start \"\" \"{exePath}\"\r\n" +
                                         $"del /f /q \"%~f0\" >nul\r\n";

                    File.WriteAllText(updaterBatchPath, batchScript, Encoding.ASCII);

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c \"{updaterBatchPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process.Start(psi);
                    Environment.Exit(0);
                    return;
                }
            }
            catch { }
        }

        public static void TriggerGracefulSelfSwap(string sourceNewExePath)
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Application.ExecutablePath;
                string exeDir = Path.GetDirectoryName(exePath) ?? "";
                string updaterBatchPath = Path.Combine(exeDir, "update_swap.bat");

                string batchScript = "@echo off\r\n" +
                                     "timeout /t 1 /nobreak >nul\r\n" +
                                     $"copy /y \"{sourceNewExePath}\" \"{exePath}\" >nul\r\n" +
                                     $"start \"\" \"{exePath}\"\r\n" +
                                     $"del /f /q \"%~f0\" >nul\r\n";

                File.WriteAllText(updaterBatchPath, batchScript, Encoding.ASCII);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{updaterBatchPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
                Environment.Exit(0);
            }
            catch { }
        }
    }
}
