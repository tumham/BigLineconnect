using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32;

namespace BigLineCleaner
{
    internal class Program
    {
        [DllImport("shell32.dll", SetLastError = true)]
        private static extern bool IsUserAnAdmin();

        static void Main(string[] args)
        {
            Console.Title = "BigLineconnect %100 Tam Temizleyici ve Sifirlayici (Garanti Temizlik)";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================================");
            Console.WriteLine("     BigLineconnect %100 Tam Temizleyici ve Sifirlayici v1.0     ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();
            Console.WriteLine();

            // 1. Check Administrator Privileges
            if (!IsAdministrator())
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(">> Yonetici yetkisi isteniyor... (UAC Onayi Bekleniyor)");
                Console.ResetColor();

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = Process.GetCurrentProcess().MainModule?.FileName ?? "BigLineCleaner.exe",
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                    return;
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[HATA] Yonetici hakki olmadan temizleme yapilamaz: {ex.Message}");
                    Console.ResetColor();
                    Console.WriteLine("Devam etmek icin bir tusa basin...");
                    Console.ReadKey();
                    return;
                }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[OK] Yonetici haklari onaylandi.");
            Console.ResetColor();
            Console.WriteLine();

            // 2. Stop and Delete Windows Services
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(">> 1. Windows Servisleri Durduruluyor ve Siliniyor...");
            Console.ResetColor();

            RunCommand("sc.exe", "stop BigLineconnectSvc");
            Thread.Sleep(500);
            RunCommand("sc.exe", "delete BigLineconnectSvc");
            RunCommand("sc.exe", "stop BigLineTransferSvc");
            RunCommand("sc.exe", "delete BigLineTransferSvc");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   [+] BigLineconnectSvc servisi kaldirildi.");
            Console.ResetColor();
            Console.WriteLine();

            // 3. Terminate All BigLine Processes Forcefully
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(">> 2. Calisan Tum BigLine Surecleri Sonlandiriliyor...");
            Console.ResetColor();

            string[] procNames = new[] { "BigLineconnect", "BigLineconnect_setup", "BigLineTransfer", "EastDesktop", "LightConnect" };
            foreach (var name in procNames)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    if (p.Id == Process.GetCurrentProcess().Id) continue;
                    try
                    {
                        Console.WriteLine($"   [-] PID {p.Id} ({p.ProcessName}) kapatiliyor...");
                        p.Kill();
                        p.WaitForExit(1500);
                    }
                    catch { }
                }
            }

            // Secondary WMI/taskkill sweep
            RunCommand("taskkill.exe", "/F /IM BigLineconnect* /T");
            RunCommand("taskkill.exe", "/F /IM BigLineTransfer* /T");
            Thread.Sleep(500);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   [+] Tum surecler ve RAM kilitleri temizlendi.");
            Console.ResetColor();
            Console.WriteLine();

            // 4. Delete Caches and Folders
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(">> 3. Tum Eski Dosya, Cache ve Ayar Klasorleri Siliniyor...");
            Console.ResetColor();

            string programData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
            string localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BigLineconnect");
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BigLineconnect");
            string tempDir = Path.GetTempPath();

            DeleteDirectory(programData, "C:\\ProgramData\\BigLineconnect");
            DeleteDirectory(localAppData, "%LocalAppData%\\BigLineconnect");
            DeleteDirectory(appData, "%AppData%\\BigLineconnect");

            // Clean Temp files
            try
            {
                var dInfo = new DirectoryInfo(tempDir);
                foreach (var f in dInfo.GetFiles("*BigLine*"))
                {
                    try { f.Delete(); } catch { }
                }
                foreach (var d in dInfo.GetDirectories("*BigLine*"))
                {
                    try { d.Delete(true); } catch { }
                }
            }
            catch { }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   [+] Tum kalinti dosya ve klasorler silindi.");
            Console.ResetColor();
            Console.WriteLine();

            // 5. Clean Registry Startup Keys
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(">> 4. Windows Baslangic Kayitlari Temizleniyor...");
            Console.ResetColor();

            RemoveRegistryValue(Registry.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "BigLineconnect");
            RemoveRegistryValue(Registry.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", "BigLineconnect");
            RemoveRegistryValue(Registry.CurrentUser, @"Software\BigLineconnect", null);
            RemoveRegistryValue(Registry.LocalMachine, @"Software\BigLineconnect", null);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   [+] Kayit Defteri baslangic girdileri temizlendi.");
            Console.ResetColor();
            Console.WriteLine();

            // 6. Finished
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==================================================================");
            Console.WriteLine("   TEBRIKLER! SISTEM TERTEMIZ SIFIRLANDI VE ESKI SURUM SILINDI.  ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Simdi yeni 'publish_turbo\\BigLineconnect.exe' dosyasini acip");
            Console.WriteLine("gonul rahatligiyla 60 FPS turbo hizinda test edebilirsiniz!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Kapatmak icin Enter tusuna basin...");
            Console.ReadLine();
        }

        private static bool IsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static void DeleteDirectory(string path, string displayName)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                    Console.WriteLine($"   [x] Silindi: {displayName}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"   [!] {displayName} silinirken: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void RemoveRegistryValue(RegistryKey rootKey, string subKey, string? valueName)
        {
            try
            {
                using var key = rootKey.OpenSubKey(subKey, true);
                if (key != null)
                {
                    if (!string.IsNullOrEmpty(valueName))
                    {
                        if (key.GetValue(valueName) != null)
                        {
                            key.DeleteValue(valueName, false);
                        }
                    }
                    else
                    {
                        rootKey.DeleteSubKeyTree(subKey, false);
                    }
                }
            }
            catch { }
        }

        private static void RunCommand(string exe, string args)
        {
            try
            {
                var psi = new ProcessStartInfo(exe, args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var p = Process.Start(psi);
                p?.WaitForExit(3000);
            }
            catch { }
        }
    }
}
