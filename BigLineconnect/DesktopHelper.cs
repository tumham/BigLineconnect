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

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenDesktop(string lpszDesktop, uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint SetThreadExecutionState(uint esFlags);
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
        public const uint ES_DISPLAY_REQUIRED = 0x00000002;
        public const uint ES_CONTINUOUS = 0x80000000;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref uint pvParam, uint fWinIni);

        private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
        private const int ASFW_ANY = -1;

        public static void DisableForegroundLock()
        {
            try
            {
                SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
                AllowSetForegroundWindow(ASFW_ANY);
                uint zero = 0;
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref zero, 0);
            }
            catch { }
        }

        private static DateTime _lastAttachTime = DateTime.MinValue;

        public static void ForceAttachToInputDesktop()
        {
            AttachToInputDesktop(true);
        }

        public static void AttachToInputDesktop(bool force = false)
        {
            try
            {
                if (!force && (DateTime.Now - _lastAttachTime).TotalMilliseconds < 2000)
                {
                    return;
                }
                _lastAttachTime = DateTime.Now;

                DisableForegroundLock();

                IntPtr hDesk = OpenInputDesktop(0, false, MAXIMUM_ALLOWED);
                if (hDesk == IntPtr.Zero)
                {
                    hDesk = OpenInputDesktop(0, false, GENERIC_ALL);
                }
                if (hDesk == IntPtr.Zero)
                {
                    hDesk = OpenDesktop("Default", 0, false, GENERIC_ALL);
                }

                if (hDesk != IntPtr.Zero)
                {
                    SetThreadDesktop(hDesk);
                    CloseDesktop(hDesk);
                }
            }
            catch { }
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
