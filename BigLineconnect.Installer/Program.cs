using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BigLineconnect.Installer
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Global Exception Handling
            Application.ThreadException += (s, e) => HandleFatalException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => HandleFatalException(e.ExceptionObject as Exception);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // Ensure Administrator privileges
                if (!IsAdministrator())
                {
                    MessageBox.Show("BigLineconnect kurulumu için yönetici (Administrator) izni gereklidir.\n\nLütfen dosyaya sağ tıklayıp 'Yönetici olarak çalıştır' seçeneğini kullanın.", "Yönetici İzni Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Unblock Zone.Identifier if downloaded / copied
                try
                {
                    string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    if (!string.IsNullOrEmpty(currentExe) && File.Exists(currentExe + ":Zone.Identifier"))
                    {
                        File.Delete(currentExe + ":Zone.Identifier");
                    }
                }
                catch { }

                // Launch Graphical Installation Form
                Application.Run(new InstallerProgressForm());
            }
            catch (Exception ex)
            {
                HandleFatalException(ex);
            }
        }

        private static void HandleFatalException(Exception? ex)
        {
            if (ex == null) return;
            LogException(ex);
            MessageBox.Show($"Kurulum sırasında bir hata oluştu:\n{ex.Message}\n\nDetaylar log dosyasına yazıldı.", "Kurulum Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void LogException(Exception? ex)
        {
            if (ex == null) return;
            try
            {
                string logDir = @"C:\ProgramData\BigLineconnect";
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                string logFile = Path.Combine(logDir, "installer_error.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex}\n\n");
            }
            catch { }
        }

        private static bool IsAdministrator()
        {
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return true;
            }
        }

        public class InstallerProgressForm : Form
        {
            private ProgressBar _progressBar;
            private Label _lblTitle;
            private Label _lblStatus;
            private Button _btnFinish;

            public InstallerProgressForm()
            {
                this.Text = "BigLineconnect - Kurulum Sihirbazı";
                this.Size = new Size(500, 260);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.FromArgb(15, 23, 42);
                this.ForeColor = Color.White;

                var pnlHeader = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.FromArgb(20, 24, 38)
                };

                _lblTitle = new Label
                {
                    Text = "⚡ BigLineconnect (Sürüm: 17.8 - Win32 KnownFolder Fix)",
                    Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 229, 255),
                    Location = new Point(20, 15),
                    AutoSize = true
                };

                pnlHeader.Controls.Add(_lblTitle);
                this.Controls.Add(pnlHeader);

                _lblStatus = new Label
                {
                    Text = "Kurulum başlatılıyor...",
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    ForeColor = Color.FromArgb(220, 225, 235),
                    Location = new Point(25, 80),
                    Size = new Size(440, 45)
                };
                this.Controls.Add(_lblStatus);

                _progressBar = new ProgressBar
                {
                    Location = new Point(25, 135),
                    Size = new Size(435, 24),
                    Style = ProgressBarStyle.Continuous,
                    Value = 0
                };
                this.Controls.Add(_progressBar);

                _btnFinish = new Button
                {
                    Text = "Lütfen Bekleyin...",
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Location = new Point(320, 175),
                    Size = new Size(140, 35),
                    Enabled = false,
                    BackColor = Color.FromArgb(30, 40, 55),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                _btnFinish.Click += (s, e) => this.Close();
                this.Controls.Add(_btnFinish);

                this.Load += async (s, e) => await StartInstallationAsync();
            }

            private async Task StartInstallationAsync()
            {
                try
                {
                    UpdateStatus("Yönetici izinleri doğrulandı...", 15);
                    await Task.Delay(300);

                    string installDir = @"C:\Program Files\BigLineconnect";
                    if (!Directory.Exists(installDir))
                    {
                        Directory.CreateDirectory(installDir);
                    }

                    UpdateStatus("Eski servis ve tüm işlemler tamamen durduruluyor...", 30);
                    await Task.Run(() =>
                    {
                        try
                        {
                            foreach (var proc in Process.GetProcessesByName("BigLineconnect"))
                            {
                                try { proc.Kill(true); } catch { }
                            }
                            foreach (var proc in Process.GetProcessesByName("BigLine"))
                            {
                                try { proc.Kill(true); } catch { }
                            }
                        }
                        catch { }

                        ExecuteCommand("sc.exe", "stop BigLineconnectSvc");
                        ExecuteCommand("taskkill.exe", "/F /IM BigLineconnect.exe /T");
                        ExecuteCommand("taskkill.exe", "/F /IM BigLine.exe /T");
                        System.Threading.Thread.Sleep(1500);

                        try
                        {
                            foreach (var proc in Process.GetProcessesByName("BigLineconnect"))
                            {
                                try { proc.Kill(true); } catch { }
                            }
                            foreach (var proc in Process.GetProcessesByName("BigLine"))
                            {
                                try { proc.Kill(true); } catch { }
                            }
                        }
                        catch { }
                    });

                    UpdateStatus($"Yeni uygulama dosyaları kopyalanıyor ({installDir})...", 55);
                    await Task.Run(() =>
                    {
                        ExtractResource("BigLineconnect.exe", Path.Combine(installDir, "BigLineconnect.exe"));
                        ExtractResource("BigLineTransfer.exe", Path.Combine(installDir, "BigLineTransfer.exe"));
                        ExtractResource("icon.ico", Path.Combine(installDir, "icon.ico"));
                        ExtractResource("LisansOlustur.ps1", Path.Combine(installDir, "LisansOlustur.ps1"));
                        try { ExtractResource("company.txt", Path.Combine(installDir, "company.txt")); } catch { }
                    });

                    UpdateStatus("Windows Servisi (BigLineconnectSvc) kaydediliyor...", 75);
                    string exePath = Path.Combine(installDir, "BigLineconnect.exe");
                    await Task.Run(() =>
                    {
                        ExecuteCommand("sc.exe", "stop BigLineconnectSvc");
                        ExecuteCommand("sc.exe", "delete BigLineconnectSvc");
                        System.Threading.Thread.Sleep(500);
                        ExecuteCommand("sc.exe", $"create BigLineconnectSvc binPath= \"{exePath} --service\" start= auto DisplayName= \"BigLineconnect Background Service\"");
                        ExecuteCommand("sc.exe", "config BigLineconnectSvc start= auto");
                        ExecuteCommand("sc.exe", "failure BigLineconnectSvc reset= 0 actions= restart/3000/restart/3000/restart/3000");
                        ExecuteCommand("sc.exe", "start BigLineconnectSvc");
                    });

                    UpdateStatus("Masaüstü kısayolu oluşturuluyor...", 90);
                    await Task.Run(() => CreateShortcut(exePath, installDir));

                    UpdateStatus("✅ Kurulum başarıyla tamamlandı!", 100);
                    _lblTitle.Text = "🎉 BigLineconnect Başarıyla Kuruldu!";
                    _lblTitle.ForeColor = Color.FromArgb(46, 204, 113);
                    _lblStatus.Text = "Uygulama otomatik olarak açılıyor, lütfen bekleyin...";

                    await Task.Delay(1500);

                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true
                        });
                    }
                    catch { }

                    this.Close();
                }
                catch (Exception ex)
                {
                    LogException(ex);
                    _lblStatus.Text = $"❌ Kurulum Hatası: {ex.Message}";
                    _lblStatus.ForeColor = Color.FromArgb(231, 76, 60);
                    _btnFinish.Text = "Kapat";
                    _btnFinish.Enabled = true;
                }
            }

            private void UpdateStatus(string message, int progress)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateStatus(message, progress)));
                    return;
                }
                _lblStatus.Text = message;
                _progressBar.Value = Math.Min(100, Math.Max(0, progress));
            }
        }

        private static void ExtractResource(string targetFileName, string outputPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            string? match = Array.Find(resourceNames, r => r.EndsWith(targetFileName, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                throw new FileNotFoundException($"Gömülü kaynak bulunamadı: {targetFileName}. Mevcut kaynaklar: {string.Join(", ", resourceNames)}");
            }

            using var stream = assembly.GetManifestResourceStream(match);
            if (stream == null)
            {
                throw new FileNotFoundException($"Gömülü kaynak okunamadı: {match}");
            }

            string? dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Retry loop up to 5 attempts for locked files
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                try
                {
                    if (File.Exists(outputPath))
                    {
                        try { File.Delete(outputPath); } catch { }
                    }
                    using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        stream.CopyTo(fileStream);
                    }
                    return; // Success!
                }
                catch
                {
                    if (attempt == 5)
                    {
                        // Fallback: Move running/locked file away to a unique temp old path
                        if (File.Exists(outputPath))
                        {
                            string tempOld = outputPath + ".old_" + Guid.NewGuid().ToString("N");
                            try { File.Move(outputPath, tempOld); } catch { }
                        }
                        using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }

        private static void ExecuteCommand(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(5000);
            }
            catch { }
        }

        private static void CreateShortcut(string targetExePath, string workingDir)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, "BigLineconnect.lnk");
                string iconPath = Path.Combine(workingDir, "icon.ico");

                // Native COM Automation without PowerShell
                Type? shellType = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
                if (shellType != null)
                {
                    dynamic? shell = Activator.CreateInstance(shellType);
                    if (shell != null)
                    {
                        dynamic shortcut = shell.CreateShortcut(shortcutPath);
                        shortcut.TargetPath = targetExePath;
                        shortcut.WorkingDirectory = workingDir;
                        shortcut.IconLocation = iconPath;
                        shortcut.Save();
                    }
                }
            }
            catch { }
        }
    }
}
