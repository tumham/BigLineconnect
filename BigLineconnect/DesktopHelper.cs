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

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool LockWorkStation();

        public static void LockScreen()
        {
            try
            {
                LogHelper("LockScreen invoked.");
                LockWorkStation();
                Program.TriggerInstantCapture(5);
            }
            catch (Exception ex)
            {
                LogHelper($"LockScreen exception: {ex.Message}");
            }
        }

        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private const uint DESKTOP_ALL_ACCESS = 0x1ff;
        private const uint MAXIMUM_ALLOWED = 0x02000000;
        private const uint GENERIC_ALL = 0x10000000;

        private static readonly object _desktopLock = new object();
        private static DateTime _lastDesktopAttachTime = DateTime.MinValue;

        public static void EnableSoftwareSAS()
        {
            try
            {
                // Write to both 64-bit and 32-bit registry views to ensure Winlogon / Services pick it up
                using (var base64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var key64 = base64.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key64?.SetValue("SoftwareSASGeneration", 3, RegistryValueKind.DWord);
                }
                using (var base32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                using (var key32 = base32.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key32?.SetValue("SoftwareSASGeneration", 3, RegistryValueKind.DWord);
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
                LogHelper("SendCtrlAltDel invoked.");
                EnableSoftwareSAS();
                AttachToInputDesktop(true);

                // 1. Official Windows SAS API via sas.dll (Try both service mode and user mode)
                try
                {
                    SendSAS(false);
                    LogHelper("SendSAS(false) executed.");
                }
                catch (Exception ex)
                {
                    LogHelper($"SendSAS(false) exception: {ex.Message}");
                }

                try
                {
                    SendSAS(true);
                    LogHelper("SendSAS(true) executed.");
                }
                catch (Exception ex)
                {
                    LogHelper($"SendSAS(true) exception: {ex.Message}");
                }

                // 2. Direct Win32 Hardware Scan Code Injection
                // (CTRL: VK 0x11, Scan 0x1D | ALT: VK 0x12, Scan 0x38 | DEL: VK 0x2E, Scan 0x53 extended)
                keybd_event(0x11, 0x1D, 0, UIntPtr.Zero);
                keybd_event(0x12, 0x38, 0, UIntPtr.Zero);
                keybd_event(0x2E, 0x53, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
                System.Threading.Thread.Sleep(80);
                keybd_event(0x2E, 0x53, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(0x12, 0x38, KEYEVENTF_KEYUP, UIntPtr.Zero);
                keybd_event(0x11, 0x1D, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Also execute via InputSimulator
                InputSimulator.SimulateKey("control", "down");
                InputSimulator.SimulateKey("alt", "down");
                InputSimulator.SimulateKey("delete", "down");
                System.Threading.Thread.Sleep(80);
                InputSimulator.SimulateKey("delete", "up");
                InputSimulator.SimulateKey("alt", "up");
                InputSimulator.SimulateKey("control", "up");

                // 3. If the remote session is locked or disconnected RDP, trigger tscon console recovery
                FixHeadlessVpsScreen();

                // 4. Force screen capture so the Viewer immediately sees the newly opened security screen or logon UI
                Program.TriggerInstantCapture(5);
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

        [ThreadStatic]
        private static IntPtr _currentThreadDesktop = IntPtr.Zero;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetThreadDesktop(int dwThreadId);

        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();

        private static DateTime _lastAttachTime = DateTime.MinValue;

        public static void ForceAttachToInputDesktop()
        {
            AttachToInputDesktop(true);
        }

        public static void AttachToInputDesktop(bool force = false)
        {
            try
            {
                if (!force && (DateTime.Now - _lastAttachTime).TotalMilliseconds < 1000)
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
                    if (_currentThreadDesktop != hDesk)
                    {
                        if (SetThreadDesktop(hDesk))
                        {
                            if (_currentThreadDesktop != IntPtr.Zero)
                            {
                                try { CloseDesktop(_currentThreadDesktop); } catch { }
                            }
                            _currentThreadDesktop = hDesk;
                        }
                        else
                        {
                            try { CloseDesktop(hDesk); } catch { }
                        }
                    }
                    else
                    {
                        try { CloseDesktop(hDesk); } catch { }
                    }
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
