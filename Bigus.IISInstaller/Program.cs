using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Data.SqlClient;
using Microsoft.Web.Administration;

namespace Bigus.IISInstaller
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Yönetici Yetkisi Kontrolü
            if (!IsAdministrator())
            {
                Elevate();
                return;
            }

            SafeSetTitle("Bigus - Otomatik IIS Kurulum ve Yapılandırma Aracı");
            ShowHeader();

            // .NET Runtime Kontrolü ve Yükleme
            CheckAndInstallDotNetRuntimes();

            try
            {
                // 2. Kullanıcı Girdileri ve Ayarlar
                SafeSetColor(ConsoleColor.Cyan);
                Console.WriteLine(">>> ADIM 1: Kurulum ve Yapılandırma Parametreleri");
                SafeResetColor();

                Console.WriteLine("Bilgi: API kurulum dosyaları bu çalıştırılabilir dosyanın içerisine gömülüdür.");
                Console.WriteLine();

                string defaultTarget = @"C:\inetpub\wwwroot\BigusCustomerApi";
                Console.Write($"Hedef IIS Klasörü [{defaultTarget}]: ");
                string targetPath = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(targetPath)) targetPath = defaultTarget;

                string defaultPort = "5001";
                Console.Write($"Kullanılacak IIS Portu [{defaultPort}]: ");
                string portInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(portInput)) portInput = defaultPort;
                if (!int.TryParse(portInput, out int port))
                {
                    port = 5001;
                }

                string defaultSql = @".\MYLENOVO";
                Console.Write($"SQL Server Instance Adı [{defaultSql}]: ");
                string sqlServer = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(sqlServer)) sqlServer = defaultSql;

                string defaultDb = "BIGMOBIL";
                Console.Write($"Şirket Veritabanı Adı (BIGMOBIL) [{defaultDb}]: ");
                string dbName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(dbName)) dbName = defaultDb;

                string defaultMasterDb = "BigusMaster";
                Console.Write($"Master Veritabanı Adı (BigusMaster) [{defaultMasterDb}]: ");
                string masterDbName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(masterDbName)) masterDbName = defaultMasterDb;

                Console.Write("SQL Kimlik Doğrulama Tipi - Integrated (Windows Auth) için 'W', SQL Auth için 'S' girin [W]: ");
                string authInput = Console.ReadLine()?.Trim().ToUpper() ?? "W";
                bool isWindowsAuth = authInput != "S";

                string dbUser = "";
                string dbPassword = "";

                if (!isWindowsAuth)
                {
                    Console.Write("SQL Kullanıcı Adı: ");
                    dbUser = Console.ReadLine();
                    Console.Write("SQL Şifresi: ");
                    dbPassword = Console.ReadLine();
                }

                Console.WriteLine();
                SafeSetColor(ConsoleColor.Yellow);
                Console.WriteLine("---------------------------------------------------------");
                Console.WriteLine("KURULUM BAŞLIYOR...");
                Console.WriteLine($"Hedef:  {targetPath}");
                Console.WriteLine($"Port:   {port}");
                Console.WriteLine($"SQL:    {sqlServer} (DB: {dbName}, Master: {masterDbName})");
                Console.WriteLine("---------------------------------------------------------");
                SafeResetColor();

                // 2.5 Veritabanı Kontrolü ve Oluşturma
                Console.WriteLine("\n[ÖN İŞLEM] Veritabanı varlığı kontrol ediliyor ve gerekiyorsa oluşturuluyor...");
                try
                {
                    EnsureDatabaseExists(sqlServer, dbName, dbUser, dbPassword, isWindowsAuth);
                    EnsureDatabaseExists(sqlServer, masterDbName, dbUser, dbPassword, isWindowsAuth);
                }
                catch (Exception ex)
                {
                    SafeSetColor(ConsoleColor.Red);
                    Console.WriteLine($"Hata: Veritabanı kontrolü/oluşturulması sırasında hata oluştu: {ex.Message}");
                    SafeResetColor();
                    Console.Write("Devam etmek istiyor musunuz? (E/H) [E]: ");
                    string answer = Console.ReadLine()?.Trim().ToUpper() ?? "E";
                    if (string.IsNullOrEmpty(answer)) answer = "E";
                    if (answer != "E" && answer != "Y")
                    {
                        throw new OperationCanceledException("Kurulum veritabanı hatası nedeniyle iptal edildi.");
                    }
                }

                // 3. Gömülü Dosyaları Çıkartma
                Console.WriteLine("\n[1/4] Gömülü API dosyaları hedef klasöre çıkartılıyor...");
                if (Directory.Exists(targetPath))
                {
                    Console.WriteLine("Bilgi: Hedef klasör zaten var, üzerine yazılıyor...");
                }
                ExtractEmbeddedApi(targetPath);
                SafeSetColor(ConsoleColor.Green);
                Console.WriteLine("Başarı: API dosyaları çıkartıldı.");
                SafeResetColor();

                // 4. IIS Klasör İzinleri Tanımlama
                Console.WriteLine("\n[2/4] Klasör izinleri ('IIS_IUSRS') yapılandırılıyor...");
                SetFolderPermissions(targetPath);

                // 5. IIS Site ve Uygulama Havuzu Oluşturma
                Console.WriteLine("\n[3/4] IIS Web Sitesi ve Uygulama Havuzu oluşturuluyor...");
                ConfigureIIS(targetPath, port);

                // Güvenlik duvarı iznini otomatik ekleme
                Console.WriteLine($"\nWindows Güvenlik Duvarında {port} portu dış erişime açılıyor...");
                AddFirewallRule(port);

                // 6. appsettings.json Bağlantı Dinesini Güncelleme
                Console.WriteLine("\n[4/4] appsettings.json bağlantı dizeleri güncelleniyor...");
                UpdateAppSettings(targetPath, sqlServer, dbName, masterDbName, dbUser, dbPassword, isWindowsAuth);

                // Bitiş ve IIS Reset
                SafeSetColor(ConsoleColor.Green);
                Console.WriteLine("\n=========================================================");
                Console.WriteLine("TEBRİKLER! KURULUM BAŞARIYLA TAMAMLANDI!");
                Console.WriteLine("=========================================================");
                SafeResetColor();

                Console.WriteLine("\nDeğişikliklerin geçerli olması için IIS servisi yeniden başlatılıyor...");
                RestartIIS();

                SafeSetColor(ConsoleColor.Cyan);
                Console.WriteLine($"\nKurulum tamamlandı. API'yi test etmek için tarayıcınızdan şu adrese gidebilirsiniz:");
                Console.WriteLine($"http://localhost:{port}/swagger");
                SafeResetColor();
            }
            catch (Exception ex)
            {
                SafeSetColor(ConsoleColor.Red);
                Console.WriteLine($"\nHATA OLUŞTU: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Detay: {ex.InnerException.Message}");
                }
                SafeResetColor();
            }

            PressAnyKeyToExit();
        }

        static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void Elevate()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas"
            };
            try
            {
                Process.Start(processInfo);
            }
            catch (Exception)
            {
                SafeSetColor(ConsoleColor.Red);
                Console.WriteLine("HATA: IIS yapılandırması yapabilmek için bu programın YÖNETİCİ yetkisiyle çalıştırılması gerekir.");
                SafeResetColor();
                Console.WriteLine("Lütfen sağ tıklayıp 'Yönetici olarak çalıştır' seçeneğini kullanın.");
                PressAnyKeyToExit();
            }
        }

        static void ShowHeader()
        {
            SafeClear();
            SafeSetColor(ConsoleColor.Magenta);
            Console.WriteLine(@"======================================================================");
            Console.WriteLine(@"           _             _       __  __       _     _ _               ");
            Console.WriteLine(@"          | |__  _  __ _(_)     |  \/  | ___ | |__ (_) |              ");
            Console.WriteLine(@"          | '_ \| |/ _` | |_____| |\/| |/ _ \| '_ \| | |              ");
            Console.WriteLine(@"          | |_) | | (_| | |_____| |  | | (_) | |_) | | |              ");
            Console.WriteLine(@"          |_.__/|_|\__, |_|     |_|  |_|\___/|_.__/|_|_|              ");
            Console.WriteLine(@"                   |___/                                              ");
            Console.WriteLine(@"                IIS OTOMATİK KURULUM VE YAPILANDIRMA                  ");
            Console.WriteLine(@"======================================================================");
            SafeResetColor();
            Console.WriteLine();
        }

        static void ExtractEmbeddedApi(string targetPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "ApiPublish.zip";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Hata: Gömülü '{resourceName}' kaynağı bulunamadı.");
                }

                using (var archive = new ZipArchive(stream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        string destinationPath = Path.GetFullPath(Path.Combine(targetPath, entry.FullName));
                        
                        // Path traversal zafiyeti koruması
                        if (!destinationPath.StartsWith(Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (string.IsNullOrEmpty(entry.Name))
                        {
                            // Klasör oluşturma
                            Directory.CreateDirectory(destinationPath);
                        }
                        else
                        {
                            // Dosya oluşturma ve üzerine yazma
                            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                            entry.ExtractToFile(destinationPath, overwrite: true);
                        }
                    }
                }
            }
        }

        static void SetFolderPermissions(string path)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "icacls.exe",
                Arguments = $"\"{path}\" /grant \"IIS_IUSRS:(OI)(CI)F\" /T",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            try
            {
                using (var process = Process.Start(processInfo))
                {
                    process?.WaitForExit();
                    if (process?.ExitCode != 0)
                    {
                        Console.WriteLine($"Uyarı: Klasör izinleri ayarlanırken icacls hata kodu döndürdü: {process?.ExitCode}");
                    }
                    else
                    {
                        Console.WriteLine("Başarı: IIS_IUSRS grubuna okuma, yazma ve çalıştırma izinleri atandı.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uyarı: Klasör izinleri ayarlanırken hata oluştu: {ex.Message}");
            }
        }

        static void ConfigureIIS(string targetPath, int port)
        {
            using (ServerManager serverManager = new ServerManager())
            {
                // 1. Application Pool Oluşturma
                string poolName = "BigusCustomerPool";
                ApplicationPool appPool = serverManager.ApplicationPools[poolName];
                if (appPool == null)
                {
                    appPool = serverManager.ApplicationPools.Add(poolName);
                    Console.WriteLine($"Bilgi: '{poolName}' uygulama havuzu oluşturuldu.");
                }
                else
                {
                    Console.WriteLine($"Bilgi: '{poolName}' uygulama havuzu zaten mevcut.");
                }

                // .NET CLR Sürümü => No Managed Code (ASP.NET Core için zorunlu)
                appPool.ManagedRuntimeVersion = "";
                appPool.ManagedPipelineMode = ManagedPipelineMode.Integrated;
                
                // SQL Server Integrated Security bağlantısı için uygulama havuzunu LocalSystem kimliğiyle çalıştırıyoruz
                appPool.ProcessModel.IdentityType = ProcessModelIdentityType.LocalSystem;

                // 2. Web Sitesi Oluşturma
                string siteName = "BigusCustomerApi";
                Site site = serverManager.Sites[siteName];
                if (site != null)
                {
                    Console.WriteLine($"Bilgi: Eski '{siteName}' web sitesi algılandı, kaldırılıyor...");
                    serverManager.Sites.Remove(site);
                }

                site = serverManager.Sites.Add(siteName, "http", $"*:{port}:", targetPath);
                site.ApplicationDefaults.ApplicationPoolName = poolName;

                serverManager.CommitChanges();
                SafeSetColor(ConsoleColor.Green);
                Console.WriteLine($"Başarı: '{siteName}' web sitesi IIS üzerinde oluşturuldu.");
                SafeResetColor();
            }
        }

        static void UpdateAppSettings(string targetPath, string sqlServer, string dbName, string masterDbName, string dbUser, string dbPassword, bool isWindowsAuth)
        {
            string jsonPath = Path.Combine(targetPath, "appsettings.json");
            if (!File.Exists(jsonPath))
            {
                SafeSetColor(ConsoleColor.Yellow);
                Console.WriteLine($"Uyarı: appsettings.json bulunamadı. Konum: {jsonPath}");
                SafeResetColor();
                return;
            }

            try
            {
                string jsonString = File.ReadAllText(jsonPath);
                
                // appsettings.json içindeki yorum satırlarını (//) atlayabilmek için DocumentOptions kullanıyoruz.
                var docOptions = new JsonDocumentOptions 
                { 
                    AllowTrailingCommas = true, 
                    CommentHandling = JsonCommentHandling.Skip 
                };

                JsonNode? rootNode = JsonNode.Parse(jsonString, null, docOptions);
                if (rootNode == null)
                {
                    Console.WriteLine("Hata: appsettings.json parse edilemedi.");
                    return;
                }

                // Bağlantı dizelerini hazırlama (TrustServerCertificate ekliyoruz ki SSL hatası vermesin localde)
                string masterConn, dbConn, logConn;
                if (isWindowsAuth)
                {
                    masterConn = $"Server={sqlServer};Database={masterDbName};Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
                    dbConn = $"Server={sqlServer};Database={dbName};Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
                    logConn = $"Server={sqlServer};Database={dbName};Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
                }
                else
                {
                    masterConn = $"Server={sqlServer};Database={masterDbName};User ID={dbUser};Password={dbPassword};MultipleActiveResultSets=true;TrustServerCertificate=True";
                    dbConn = $"Server={sqlServer};Database={dbName};User ID={dbUser};Password={dbPassword};MultipleActiveResultSets=true;TrustServerCertificate=True";
                    logConn = $"Server={sqlServer};Database={dbName};User ID={dbUser};Password={dbPassword};MultipleActiveResultSets=true;TrustServerCertificate=True";
                }

                var connectionStrings = rootNode["ConnectionStrings"] as JsonObject;
                if (connectionStrings == null)
                {
                    connectionStrings = new JsonObject();
                    rootNode["ConnectionStrings"] = connectionStrings;
                }

                connectionStrings["BigusMikroV16MasterDbConnection"] = masterConn;
                connectionStrings["BigusMikroV16Connection"] = dbConn;
                connectionStrings["BigusMikroV16LogConnection"] = logConn;

                var options = new JsonSerializerOptions { WriteIndented = true };
                string updatedJson = rootNode.ToJsonString(options);
                File.WriteAllText(jsonPath, updatedJson);

                SafeSetColor(ConsoleColor.Green);
                Console.WriteLine("Başarı: appsettings.json bağlantı dizeleri güncellendi.");
                SafeResetColor();
            }
            catch (Exception ex)
            {
                SafeSetColor(ConsoleColor.Red);
                Console.WriteLine($"Hata: appsettings.json güncellenirken bir sorun oluştu: {ex.Message}");
                SafeResetColor();
            }
        }

        static void AddFirewallRule(int port)
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "netsh.exe",
                Arguments = $"advfirewall firewall add rule name=\"Bigus API Port {port}\" dir=in action=allow protocol=TCP localport={port} profile=any",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            try
            {
                using (var process = Process.Start(processInfo))
                {
                    process?.WaitForExit();
                    Console.WriteLine($"Başarı: Güvenlik duvarında {port} portu için gelen kuralı otomatik oluşturuldu.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uyarı: Güvenlik duvarı kuralı eklenirken hata oluştu: {ex.Message}");
            }
        }

        static void RestartIIS()
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "iisreset.exe",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            try
            {
                using (var process = Process.Start(processInfo))
                {
                    process?.WaitForExit();
                    Console.WriteLine("IIS başarıyla yeniden başlatıldı.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uyarı: IIS yeniden başlatılamadı: {ex.Message}");
            }
        }

        static void SafeClear()
        {
            try { Console.Clear(); } catch {}
        }

        static void SafeSetTitle(string title)
        {
            try { Console.Title = title; } catch {}
        }

        static void SafeSetColor(ConsoleColor color)
        {
            try { Console.ForegroundColor = color; } catch {}
        }

        static void SafeResetColor()
        {
            try { Console.ResetColor(); } catch {}
        }

        static void CheckAndInstallDotNetRuntimes()
        {
            SafeSetColor(ConsoleColor.Cyan);
            Console.WriteLine(">>> ÖN GEREKSİNİMLER: .NET Core IIS Hosting Runtimes Kontrolü");
            SafeResetColor();

            bool is22Installed = false;
            bool is30Installed = false;
            bool checkSuccessful = true;

            try
            {
                is22Installed = IsAspNetCoreRuntimeInstalled("2.2");
                is30Installed = IsAspNetCoreRuntimeInstalled("3.0");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Uyarı: .NET Core runtimes kontrolü sırasında bir hata oluştu: {ex.Message}");
                checkSuccessful = false;
            }

            bool install22 = false;
            bool install30 = false;

            if (!checkSuccessful)
            {
                Console.Write(".NET Core 2.2 ve 3.0 IIS Hosting Runtimes kurulu mu tespit edilemedi. Bu runtimeları şimdi kurmak ister misiniz? (E/H) [E]: ");
                string answer = Console.ReadLine()?.Trim().ToUpper() ?? "E";
                if (string.IsNullOrEmpty(answer)) answer = "E";
                if (answer == "E" || answer == "Y")
                {
                    install22 = true;
                    install30 = true;
                }
            }
            else
            {
                if (is22Installed)
                {
                    Console.WriteLine("Bilgi: .NET Core 2.2 IIS Hosting Runtime zaten yüklü.");
                }
                else
                {
                    Console.WriteLine("Bilgi: .NET Core 2.2 IIS Hosting Runtime sistemde yüklü değil.");
                    install22 = true;
                }

                if (is30Installed)
                {
                    Console.WriteLine("Bilgi: .NET Core 3.0 IIS Hosting Runtime zaten yüklü.");
                }
                else
                {
                    Console.WriteLine("Bilgi: .NET Core 3.0 IIS Hosting Runtime sistemde yüklü değil.");
                    install30 = true;
                }
            }

            if (install22)
            {
                InstallEmbeddedRuntime("dotnet-hosting-2.2.0-win.exe", ".NET Core 2.2 IIS Hosting Bundle");
            }

            if (install30)
            {
                InstallEmbeddedRuntime("dotnet-hosting-3.0.3-win.exe", ".NET Core 3.0 IIS Hosting Bundle");
            }

            Console.WriteLine();
        }

        static bool IsAspNetCoreRuntimeInstalled(string majorMinorVersion)
        {
            string[] basePaths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "shared", "Microsoft.AspNetCore.App"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "dotnet", "shared", "Microsoft.AspNetCore.App")
            };

            foreach (var basePath in basePaths)
            {
                if (Directory.Exists(basePath))
                {
                    var dirs = Directory.GetDirectories(basePath);
                    foreach (var dir in dirs)
                    {
                        string name = Path.GetFileName(dir);
                        if (name.StartsWith(majorMinorVersion + "."))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static void InstallEmbeddedRuntime(string resourceName, string friendlyName)
        {
            Console.WriteLine($"\n[Girişim] {friendlyName} kuruluyor...");
            string tempPath = Path.Combine(Path.GetTempPath(), resourceName);

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        throw new FileNotFoundException($"Hata: Gömülü '{resourceName}' kaynağı bulunamadı.");
                    }

                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        stream.CopyTo(fileStream);
                    }
                }

                Console.WriteLine($"{friendlyName} yükleyicisi çalıştırılıyor. Lütfen açılan kurulum ekranını tamamlayın veya arka planda tamamlanmasını bekleyin...");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = tempPath,
                    Arguments = "/passive /norestart",
                    UseShellExecute = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process?.WaitForExit();
                    if (process?.ExitCode == 0 || process?.ExitCode == 3010) // 3010 is success but restart required
                    {
                        SafeSetColor(ConsoleColor.Green);
                        Console.WriteLine($"Başarı: {friendlyName} kurulumu tamamlandı.");
                        SafeResetColor();
                    }
                    else
                    {
                        SafeSetColor(ConsoleColor.Yellow);
                        Console.WriteLine($"Uyarı: {friendlyName} yükleyicisi sıradışı bir çıkış kodu döndürdü: {process?.ExitCode}");
                        SafeResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                SafeSetColor(ConsoleColor.Red);
                Console.WriteLine($"Hata: {friendlyName} kurulurken sorun oluştu: {ex.Message}");
                SafeResetColor();
            }
            finally
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch {}
            }
        }

        static void EnsureDatabaseExists(string sqlServer, string dbName, string dbUser, string dbPassword, bool isWindowsAuth)
        {
            var connBuilder = new SqlConnectionStringBuilder
            {
                DataSource = sqlServer,
                InitialCatalog = "master", // Connect to master first to create database
                TrustServerCertificate = true,
                ConnectTimeout = 15
            };

            if (isWindowsAuth)
            {
                connBuilder.IntegratedSecurity = true;
            }
            else
            {
                connBuilder.UserID = dbUser;
                connBuilder.Password = dbPassword;
            }

            string connectionString = connBuilder.ConnectionString;

            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();

                bool dbExists = false;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT database_id FROM sys.databases WHERE name = @dbName";
                    cmd.Parameters.AddWithValue("@dbName", dbName);
                    var result = cmd.ExecuteScalar();
                    dbExists = (result != null && result != DBNull.Value);
                }

                if (!dbExists)
                {
                    Console.WriteLine($"Bilgi: '{dbName}' veritabanı SQL Server üzerinde bulunamadı, oluşturuluyor...");
                    using (var cmd = conn.CreateCommand())
                    {
                        string escapedDbName = dbName.Replace("]", "]]");
                        cmd.CommandText = $"CREATE DATABASE [{escapedDbName}]";
                        cmd.ExecuteNonQuery();
                        SafeSetColor(ConsoleColor.Green);
                        Console.WriteLine($"Başarı: '{dbName}' veritabanı SQL Server üzerinde oluşturuldu.");
                        SafeResetColor();
                    }
                }
                else
                {
                    Console.WriteLine($"Bilgi: '{dbName}' veritabanı zaten mevcut.");
                }
            }
        }

        static void PressAnyKeyToExit()
        {
            Console.WriteLine("\nÇıkmak için bir tuşa basın...");
            try { Console.ReadKey(); } catch {}
        }
    }
}
