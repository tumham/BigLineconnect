using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace BigLineconnect
{
    public static class DesktopHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("sas.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        private static extern void SendSAS(bool asUser);

        private const uint DESKTOP_ALL_ACCESS = 0x1ff;
        private const uint MAXIMUM_ALLOWED = 0x02000000;
        private const uint GENERIC_ALL = 0x10000000;

        private static readonly object _desktopLock = new object();
        private static DateTime _lastDesktopAttachTime = DateTime.MinValue;

        public static void EnableSoftwareSAS()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        key.SetValue("SoftwareSASGeneration", 3, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper($"EnableSoftwareSAS failed: {ex.Message}");
            }
        }

        public static void SendCtrlAltDel()
        {
            try
            {
                EnableSoftwareSAS();
                AttachToInputDesktop();

                try
                {
                    SendSAS(false);
                    LogHelper("SendSAS API executed successfully.");
                }
                catch (Exception ex)
                {
                    LogHelper($"SendSAS API exception: {ex.Message}");
                }

                InputSimulator.SimulateKey("ctrl", "down");
                InputSimulator.SimulateKey("alt", "down");
                InputSimulator.SimulateKey("delete", "down");
                System.Threading.Thread.Sleep(50);
                InputSimulator.SimulateKey("delete", "up");
                InputSimulator.SimulateKey("alt", "up");
                InputSimulator.SimulateKey("ctrl", "up");
            }
            catch (Exception ex)
            {
                LogHelper($"SendCtrlAltDel exception: {ex.Message}");
            }
        }

        public static void EnsureRdpRegistrySettings()
        {
            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Terminal Server Client"))
                {
                    key?.SetValue("RemoteDesktop_UnsetMinimizedState", 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Terminal Server Client"))
                {
                    key?.SetValue("RemoteDesktop_UnsetMinimizedState", 1, RegistryValueKind.DWord);
                }
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services"))
                {
                    key?.SetValue("KeepAliveEnable", 1, RegistryValueKind.DWord);
                    key?.SetValue("KeepAliveInterval", 1, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        public static void FixHeadlessVpsScreen()
        {
            try
            {
                EnsureRdpRegistrySettings();

                int sessionId = System.Diagnostics.Process.GetCurrentProcess().SessionId;
                string tsconPath = Path.Combine(Environment.SystemDirectory, "tscon.exe");
                if (File.Exists(tsconPath))
                {
                    if (sessionId > 0)
                    {
                        using (var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = tsconPath,
                            Arguments = $"{sessionId} /dest:console",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }))
                        {
                            p?.WaitForExit(1000);
                        }
                    }

                    string? sessName = Environment.GetEnvironmentVariable("SESSIONNAME");
                    if (!string.IsNullOrEmpty(sessName) && sessName != "Console")
                    {
                        using (var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = tsconPath,
                            Arguments = $"{sessName} /dest:console",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }))
                        {
                            p?.WaitForExit(1000);
                        }
                    }
                }
            }
            catch { }
        }

        public static void AttachToInputDesktop()
        {
            if ((DateTime.UtcNow - _lastDesktopAttachTime).TotalMilliseconds < 50)
            {
                return;
            }

            lock (_desktopLock)
            {
                if ((DateTime.UtcNow - _lastDesktopAttachTime).TotalMilliseconds < 50)
                {
                    return;
                }
                _lastDesktopAttachTime = DateTime.UtcNow;

                try
                {
                    IntPtr hDesk = OpenInputDesktop(0, true, MAXIMUM_ALLOWED);
                    if (hDesk == IntPtr.Zero)
                    {
                        hDesk = OpenInputDesktop(0, true, DESKTOP_ALL_ACCESS);
                    }

                    if (hDesk != IntPtr.Zero)
                    {
                        SetThreadDesktop(hDesk);
                        CloseDesktop(hDesk);
                    }
                }
                catch { }
            }
        }

        private static void LogHelper(string message)
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string path = Path.Combine(programData, "BigLineconnect", "helper_log.txt");
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DesktopHelper] {message}\r\n");
            }
            catch { }
        }
    }
}
