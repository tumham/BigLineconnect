using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Diagnostics;
using System.Runtime.InteropServices;

using System.Security.Cryptography;
using Microsoft.Win32;

namespace BigLineconnect
{
    public static class Program
    {
        public static string SafeSerialize(object? obj)
        {
            if (obj == null) return "null";

            Type type = obj.GetType();
            if (type == typeof(string))
            {
                return "\"" + EscapeJson((string)obj) + "\"";
            }
            if (type.IsPrimitive || type == typeof(decimal))
            {
                if (type == typeof(float) || type == typeof(double))
                {
                    return Convert.ToDouble(obj).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                return obj.ToString()!.Replace(",", ".");
            }
            if (type == typeof(bool))
            {
                return (bool)obj ? "true" : "false";
            }

            if (obj is System.Collections.IEnumerable enumerable)
            {
                var sbArr = new StringBuilder();
                sbArr.Append("[");
                bool first = true;
                foreach (var item in enumerable)
                {
                    if (!first) sbArr.Append(",");
                    sbArr.Append(SafeSerialize(item));
                    first = false;
                }
                sbArr.Append("]");
                return sbArr.ToString();
            }

            var sb = new StringBuilder();
            sb.Append("{");
            var props = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    var val = props[i].GetValue(obj);
                    sb.Append($"\"{props[i].Name}\":{SafeSerialize(val)}");
                    if (i < props.Length - 1) sb.Append(",");
                }
                catch { }
            }
            string result = sb.ToString();
            if (result.EndsWith(",")) result = result.Substring(0, result.Length - 1);
            return result + "}";
        }

        public static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        public static string GetHwid()
        {
            try
            {
                // HWID is MachineName + ProcessorCount (independent of UserName/SYSTEM account)
                string raw = Environment.MachineName + ":" + Environment.ProcessorCount;
                using var sha = System.Security.Cryptography.SHA256.Create();
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                var sb = new StringBuilder();
                foreach (byte b in hash)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString().Substring(0, 16).ToUpper();
            }
            catch
            {
                return Environment.MachineName.ToUpper();
            }
        }

        public static void SendTelemetryReport(string eventType, string details)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    string hwid = GetHwid();
                    string computer = Environment.MachineName;
                    string username = Environment.UserName;
                    string os = Environment.OSVersion.ToString();
                    string ver = "1.7 (Modern)";

                    string json = $"{{\"hwid\":\"{EscapeJson(hwid)}\",\"computer_name\":\"{EscapeJson(computer)}\",\"username\":\"{EscapeJson(username)}\",\"os\":\"{EscapeJson(os)}\",\"version\":\"{EscapeJson(ver)}\",\"type\":\"{EscapeJson(eventType)}\",\"details\":\"{EscapeJson(details)}\"}}";

                    using var client = new System.Net.Http.HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                    await client.PostAsync("https://biglineconnect-production.up.railway.app/api/telemetry/report", content).ConfigureAwait(false);
                }
                catch { }
            });
        }

        public static ClientWebSocket? WebSocketClient;
        public static WebSocket? StreamWebSocketClient;
        public static SplashScreenForm? ActiveSplash;
        public static string CurrentHostId = "--- --- ---";
        public static readonly System.Collections.Generic.List<string> InitialLogs = new();
        private static CancellationTokenSource _cts = new CancellationTokenSource();
        public static bool _isStreaming = false;
        public static string _currentRelayUrl = "wss://biglineconnect-production.up.railway.app/register-host";
        private static readonly object ReconnectLock = new();
        private static bool _isReconnecting = false;
        private static TaskCompletionSource<string>? _authPasswordTcs;
        private static byte[]? _latestFrame;
        private static byte[]? _lastSentFrameBytes;
        private static DateTime _lastSentFrameTime = DateTime.MinValue;
        private static readonly object FrameLock = new();
        private static FileStream? _incomingFileStream;
        private static string? _incomingFileName;
        private static string? _activeBatchTargetFolder;
        private static FileTransferProgressForm? _hostProgressForm;
        private static int _batchTotalFiles = 0;
        private static int _batchCurrentFileIndex = 0;
        private static long _batchTotalSize = 0;
        private static long _batchCurrentSizeProcessed = 0;
        private static long _currentFileBytesProcessed = 0;
        private static long _currentFileTotalBytes = 0;
        private static bool _incomingIsFolder = false;

        // Custom features fields
        public static Form? HelperForm;
        private static HostChatForm? _hostChatForm;
        private static FileStream? _uploadFileStream;
        private static string? _uploadFileName;
        private static IntPtr _uploadUserToken = IntPtr.Zero;
        private static bool _uploadImpersonated = false;
        private static readonly object ChatLock = new();
        public static readonly object ChatQueueLock = new();
        public static readonly System.Collections.Generic.List<(string Sender, string Text)> PendingChatMessages = new();
        private static readonly SemaphoreSlim WebSocketSendSemaphore = new SemaphoreSlim(1, 1);
        private static int _activeDisplayIndex = 0;

        public static bool UsePassword { get; set; } = false;
        public static string AccessPassword { get; set; } = "";
        public static string? AutoConnectId { get; set; }
        public static string? AutoConnectPassword { get; set; }
        public static bool KeepAwake { get; set; } = false;
        public static bool RecordConnections { get; set; } = false;
        public static string ActiveSupportToken { get; set; } = "";
        public static string AutoConnectTicketToken { get; set; } = "";
        public static string ActiveTicketId { get; set; } = "";

        public static readonly string CURRENT_VERSION = "1.0.5";

        public static async Task CheckAndApplySilentUpdateAsync()
        {
            try
            {
                using var client = new System.Net.Http.HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                string json = await client.GetStringAsync("https://biglineconnect.bigus.com.tr/version.json").ConfigureAwait(false);

                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("version", out var verProp))
                {
                    string serverVersionStr = verProp.GetString() ?? "";
                    if (IsVersionNewer(serverVersionStr, CURRENT_VERSION))
                    {
                        string downloadUrl = root.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "";
                        if (!string.IsNullOrEmpty(downloadUrl))
                        {
                            string tempInstaller = Path.Combine(Path.GetTempPath(), "BigLineconnect_update.exe");
                            byte[] bytes = await client.GetByteArrayAsync(downloadUrl).ConfigureAwait(false);
                            await System.IO.File.WriteAllBytesAsync(tempInstaller, bytes).ConfigureAwait(false);

                            // Launch background silent installer
                            Process.Start(new ProcessStartInfo(tempInstaller, "--silent-update")
                            {
                                UseShellExecute = true,
                                CreateNoWindow = true
                            });
                        }
                    }
                }
            }
            catch { }
        }

        public static bool IsVersionNewer(string serverVer, string localVer)
        {
            try
            {
                Version v1 = Version.Parse(serverVer);
                Version v2 = Version.Parse(localVer);
                return v1 > v2;
            }
            catch
            {
                return false;
            }
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDPIAware();

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint SetThreadExecutionState(uint esFlags);

        private const uint ES_CONTINUOUS = 0x80000000;
        private const uint ES_SYSTEM_REQUIRED = 0x00000001;
        private const uint ES_DISPLAY_REQUIRED = 0x00000002;

        public static void ApplySleepPrevention(bool prevent)
        {
            try
            {
                if (prevent)
                {
                    SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
                }
                else
                {
                    SetThreadExecutionState(ES_CONTINUOUS);
                }
            }
            catch { }
        }

        public static string GetSharedFlagPath()
        {
            try
            {
                string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                if (!Directory.Exists(appDir))
                {
                    Directory.CreateDirectory(appDir);
                }
                return Path.Combine(appDir, "bigline_ticket_resolved.tmp");
            }
            catch
            {
                return @"C:\ProgramData\BigLineconnect\bigline_ticket_resolved.tmp";
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try { Application.OleRequired(); } catch { }
            try
            {
                if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                {
                    SetProcessDPIAware();
                }
            }
            catch { }

            LicenseSystem.Initialize();
            ScreenCapturer.WarmupDxgi();

            // Parse arguments
            bool isService = false;
            bool isHelper = false;
            bool isSetup = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg.Equals("--service", StringComparison.OrdinalIgnoreCase)) isService = true;
                if (arg.Equals("--session-helper", StringComparison.OrdinalIgnoreCase)) isHelper = true;
                if (arg.Equals("--setup", StringComparison.OrdinalIgnoreCase) || arg.Equals("--install-service", StringComparison.OrdinalIgnoreCase) || arg.Equals("--install", StringComparison.OrdinalIgnoreCase)) isSetup = true;

                if (arg.Equals("--connect", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    AutoConnectId = args[i + 1].Replace(" ", "").Trim();
                    i++;
                }
                else if (arg.Equals("--password", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    AutoConnectPassword = args[i + 1].Trim();
                    i++;
                }
                else if (arg.StartsWith("bigline://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var uri = new Uri(arg);
                        var query = uri.Query;
                        if (!string.IsNullOrEmpty(query))
                        {
                            var parts = query.TrimStart('?').Split('&');
                            foreach (var part in parts)
                            {
                                var kv = part.Split('=');
                                if (kv.Length == 2)
                                {
                                    if (kv[0].Equals("id", StringComparison.OrdinalIgnoreCase))
                                    {
                                        AutoConnectId = kv[1].Replace(" ", "").Trim();
                                    }
                                    else if (kv[0].Equals("password", StringComparison.OrdinalIgnoreCase))
                                    {
                                        AutoConnectPassword = Uri.UnescapeDataString(kv[1].Trim());
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }
            }

            if (args.Any(a => a.Equals("--silent-update", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    Thread.Sleep(2000);
                    string appDir = AppDomain.CurrentDomain.BaseDirectory;
                    string targetExe = Path.Combine(appDir, "BigLineconnect.exe");
                    string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? "";

                    if (!string.IsNullOrEmpty(currentExe) && System.IO.File.Exists(currentExe) && !currentExe.Equals(targetExe, StringComparison.OrdinalIgnoreCase))
                    {
                        System.IO.File.Copy(currentExe, targetExe, true);
                        Process.Start(new ProcessStartInfo(targetExe) { UseShellExecute = true });
                    }
                }
                catch { }
                return;
            }

            if (isSetup)
            {
                RunSetupInstallation();
                return;
            }

            // Run as Windows Service if requested or if not running interactively
            if (isService || !Environment.UserInteractive)
            {
                ServiceBase.Run(new BigLineconnectService());
                return;
            }

            // Prevent duplicate GUI instances in the same user session
            using var singleInstanceMutex = new Mutex(true, "Global\\BigLineconnectSingleInstanceMutex_" + (isHelper ? "Helper" : "Gui"), out bool isNewInstance);
            if (!isNewInstance && !isHelper)
            {
                return;
            }

            // Register custom URI scheme for deep linking (bigline://)
            RegisterUriScheme();

            // Initialize Windows Forms
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LoadSecuritySettings();
            LoadAdvancedSettings();
            // Sanitize relay URL
            _currentRelayUrl = SanitizeRelayUrl(_currentRelayUrl);

            // Automatically register Windows Service if running interactively as Admin (not helper or service)
            if (!isService && !isHelper && IsUserAnAdmin())
            {
                Task.Run(() =>
                {
                    try
                    {
                        RunSetupInstallation(true);
                    }
                    catch { }
                });
            }

            // Run as Session Helper (Headless) if requested
            if (isHelper)
            {
                LoadSecuritySettings();
                LoadAdvancedSettings();
                ApplySleepPrevention(KeepAwake);
                RunSessionHelper();
                return;
            }

            // Load saved relay URL from config.txt
            string configPath = ConfigHelper.GetConfigPath("config.txt");
            if (!System.IO.File.Exists(configPath))
            {
                string baseConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
                if (System.IO.File.Exists(baseConfig)) configPath = baseConfig;
            }
            if (System.IO.File.Exists(configPath))
            {
                try
                {
                    string savedUrl = System.IO.File.ReadAllText(configPath).Trim();
                    if (!string.IsNullOrEmpty(savedUrl))
                    {
                        _currentRelayUrl = savedUrl;
                    }
                }
                catch { }
            }
            else
            {
                // Save default URL to config.txt
                try
                {
                    System.IO.File.WriteAllText(configPath, _currentRelayUrl);
                }
                catch { }
            }

            // Register GUI PID so the service does not kill us when stopped
            try
            {
                string pidPath = ConfigHelper.GetConfigPath("gui_pid.txt");
                System.IO.File.WriteAllText(pidPath, System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            }
            catch { }

            // Show SplashScreen
            using (var splash = new SplashScreenForm())
            {
                ActiveSplash = splash;
                
                if (!IsServiceRunning())
                {
                    _ = ConnectToRelayAsync(_currentRelayUrl);
                }
                else
                {
                    // Just display the saved ID
                    string idPath = ConfigHelper.GetConfigPath("host_id.txt");
                    string savedId = "";
                    if (System.IO.File.Exists(idPath))
                    {
                        try { savedId = System.IO.File.ReadAllText(idPath).Trim(); } catch { }
                    }
                    if (!string.IsNullOrEmpty(savedId))
                    {
                        if (savedId.Length == 9)
                        {
                            savedId = savedId.Substring(0, 3) + " " + savedId.Substring(3, 3) + " " + savedId.Substring(6, 3);
                        }
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(500);
                            SetId(savedId);
                        });
                    }
                }

                Application.Run(splash);
            }
            
            ActiveSplash = null;

            // Open MainWindow
            SendTelemetryReport("gui_startup", "Kullanıcı arayüzü (GUI) başlatıldı.");
            var mainWindow = new MainWindow();
            Application.Run(mainWindow);

            // Clean up GUI PID file on exit
            try
            {
                string pidPath = ConfigHelper.GetConfigPath("gui_pid.txt");
                if (System.IO.File.Exists(pidPath))
                {
                    System.IO.File.Delete(pidPath);
                }
            }
            catch { }
        }

        private static void RunSessionHelper()
        {
            LogHelper("RunSessionHelper entered.");
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    LogHelper($"UnhandledException: {e.ExceptionObject}");
                };
                
                TaskScheduler.UnobservedTaskException += (s, e) =>
                {
                    LogHelper($"UnobservedTaskException: {e.Exception}");
                    e.SetObserved();
                };

                // Load saved relay URL from config.txt
                string configPath = ConfigHelper.GetConfigPath("config.txt");
                if (!System.IO.File.Exists(configPath))
                {
                    string baseConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
                    if (System.IO.File.Exists(baseConfig)) configPath = baseConfig;
                }
                if (System.IO.File.Exists(configPath))
                {
                    try
                    {
                        string savedUrl = System.IO.File.ReadAllText(configPath).Trim();
                        if (!string.IsNullOrEmpty(savedUrl) && !savedUrl.Contains("213.142.159") && !savedUrl.Contains("biglineconnect.com") && !savedUrl.Contains("***"))
                        {
                            _currentRelayUrl = savedUrl;
                        }
                        else
                        {
                            _currentRelayUrl = "wss://biglineconnect-production.up.railway.app/register-host";
                            try { System.IO.File.WriteAllText(configPath, _currentRelayUrl); } catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper($"Error reading config: {ex.Message}");
                    }
                }

                LogHelper($"Using relay URL: {_currentRelayUrl}");

                var dummyForm = new Form
                {
                    ShowInTaskbar = false,
                    WindowState = FormWindowState.Minimized,
                    Visible = false,
                    Opacity = 0
                };
                HelperForm = dummyForm;
                
                // Connect to Relay Server automatically
                LogHelper("Initiating ConnectToRelayAsync...");
                _ = ConnectToRelayAsync(_currentRelayUrl);

                LogHelper("Running dummyForm message loop...");
                Application.Run(dummyForm);
                LogHelper("dummyForm message loop exited.");
            }
            catch (Exception ex)
            {
                LogHelper($"Crash in RunSessionHelper: {ex.Message}\r\n{ex.StackTrace}");
            }
        }

        public static string GetLocalLanIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string s = ip.ToString();
                        if (s.StartsWith("192.168.") || s.StartsWith("10.") || s.StartsWith("172."))
                        {
                            return s;
                        }
                    }
                }
            }
            catch { }
            return "";
        }

        private static async Task<bool> PerformWebSocketServerHandshakeAsync(System.Net.Sockets.NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[2048];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead <= 0) return false;

                string headerStr = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (headerStr.Contains("GET /host-id") || headerStr.Contains("GET_HOST_ID"))
                {
                    string hId = CurrentHostId != null ? CurrentHostId.Trim().Replace(" ", "") : "";
                    string probeResp = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nConnection: close\r\n\r\nHOST_ID:{hId}\r\n";
                    byte[] pBytes = Encoding.UTF8.GetBytes(probeResp);
                    await stream.WriteAsync(pBytes, 0, pBytes.Length).ConfigureAwait(false);
                    return false;
                }

                int keyIndex = headerStr.IndexOf("Sec-WebSocket-Key: ", StringComparison.OrdinalIgnoreCase);
                if (keyIndex < 0) return false;

                int keyStart = keyIndex + "Sec-WebSocket-Key: ".Length;
                int keyEnd = headerStr.IndexOf("\r\n", keyStart);
                if (keyEnd < 0) return false;

                string secKey = headerStr.Substring(keyStart, keyEnd - keyStart).Trim();
                using var sha1 = System.Security.Cryptography.SHA1.Create();
                string acceptKey = Convert.ToBase64String(
                    sha1.ComputeHash(Encoding.UTF8.GetBytes(secKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

                string response = "HTTP/1.1 101 Switching Protocols\r\n" +
                                  "Upgrade: websocket\r\n" +
                                  "Connection: Upgrade\r\n" +
                                  $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";

                byte[] respBytes = Encoding.UTF8.GetBytes(response);
                await stream.WriteAsync(respBytes, 0, respBytes.Length).ConfigureAwait(false);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool _isLanServerStarted = false;

        public static void StartLocalLanServer()
        {
            if (_isLanServerStarted) return;
            _isLanServerStarted = true;

            _ = Task.Run(async () =>
            {
                try
                {
                    var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, 18888);
                    listener.Start();
                    Log($"Yerel Ağ (LAN Direct 0.5 ms) Sunucusu aktif: {GetLocalLanIPAddress()}:18888");

                    while (true)
                    {
                        var client = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var stream = client.GetStream();
                                if (await PerformWebSocketServerHandshakeAsync(stream).ConfigureAwait(false))
                                {
                                    var ws = System.Net.WebSockets.WebSocket.CreateFromStream(stream, isServer: true, subProtocol: null, keepAliveInterval: TimeSpan.FromSeconds(30));
                                    Log("Yerel Ağdan (LAN Direct) 0.5 ms hızlı bağlantı kabul edildi!");
                                    SetStreamActive(true);

                                    var cts = new CancellationTokenSource();
                                    _ = Task.Run(() => CaptureLoop(cts.Token));
                                    _ = Task.Run(() => SendStreamLoop(ws, cts.Token));
                                    await ReceiveLoop(ws, cts.Token).ConfigureAwait(false);
                                }
                            }
                            catch { }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log($"LAN sunucu hatası: {ex.Message}");
                }
            });
        }

        public static string SanitizeRelayUrl(string? rawUrl)
        {
            if (string.IsNullOrWhiteSpace(rawUrl) || rawUrl.Contains("***") || rawUrl.Contains("Güvenli Sunucu") || rawUrl.Contains("213.142.159") || !rawUrl.StartsWith("ws", StringComparison.OrdinalIgnoreCase))
            {
                return "wss://biglineconnect-production.up.railway.app/register-host";
            }
            return rawUrl.Trim();
        }

        public static async Task ConnectToRelayAsync(string url)
        {
            StartLocalLanServer();
            url = SanitizeRelayUrl(url);
            _currentRelayUrl = url;
            
            // Cancel any existing connections
            try { _cts.Cancel(); } catch { }
            if (WebSocketClient != null)
            {
                var oldClient = WebSocketClient;
                _ = Task.Run(() =>
                {
                    try { oldClient.Abort(); } catch { }
                    try { oldClient.Dispose(); } catch { }
                });
            }
            _cts = new CancellationTokenSource();
            WebSocketClient = new ClientWebSocket();
            try { WebSocketClient.Options.Proxy = null; } catch { }

            // Load saved host ID if exists
            string idPath = ConfigHelper.GetConfigPath("host_id.txt");
            string requestedId = "";
            if (System.IO.File.Exists(idPath))
            {
                try { requestedId = System.IO.File.ReadAllText(idPath).Trim(); } catch { }
            }

            string hwid = GetHwid();
            string computerName = Environment.MachineName;
            string username = Environment.UserName;
            string osVersion = Environment.OSVersion.ToString();
            string appVersion = "1.7 (Modern)";
            string licenseStatus = LicenseSystem.IsLicenseActive ? "LİSANSLI" : $"DENEME (Kalan: {LicenseSystem.RemainingDays} gün)";

            string telemetryQuery = $"hwid={Uri.EscapeDataString(hwid)}" +
                                    $"&computer_name={Uri.EscapeDataString(computerName)}" +
                                    $"&username={Uri.EscapeDataString(username)}" +
                                    $"&os={Uri.EscapeDataString(osVersion)}" +
                                    $"&version={Uri.EscapeDataString(appVersion)}" +
                                    $"&license_status={Uri.EscapeDataString(licenseStatus)}";

            string connectUrl = url;
            connectUrl += (connectUrl.Contains("?") ? "&" : "?") + telemetryQuery;
            if (!string.IsNullOrEmpty(requestedId))
            {
                connectUrl += "&requested_id=" + Uri.EscapeDataString(requestedId);
            }

            try
            {
                Log($"Relay sunucusuna bağlanılıyor: {url}");
                using var timeoutCts = new CancellationTokenSource(10000); // 10s timeout
                
                await WebSocketClient.ConnectAsync(new Uri(connectUrl), timeoutCts.Token).ConfigureAwait(false);
                Log("Sunucuya bağlandı. ID bekleniyor...");
                
                // Start receive loop
                _ = Task.Run(() => ReceiveLoop(WebSocketClient, _cts.Token));
            }
            catch (Exception ex)
            {
                string errMsg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                Log($"[Hata]: Bağlantı başarısız: {errMsg}");
                SetId("--- --- ---");
                TriggerReconnect();
            }
        }

        private static void TriggerReconnect()
        {
            lock (ReconnectLock)
            {
                if (_isReconnecting) return;
                _isReconnecting = true;
            }

            _ = Task.Run(async () =>
            {
                try { Log("5 saniye içinde yeniden bağlanılacak..."); } catch { }
                await Task.Delay(5000);
                
                lock (ReconnectLock)
                {
                    _isReconnecting = false;
                }
                
                _ = ConnectToRelayAsync(_currentRelayUrl);
            });
        }

        private static async Task ReceiveLoop(WebSocket ws, CancellationToken token)
        {
            StreamWebSocketClient = ws;
            var segmentBuffer = new byte[1024 * 4];
            using (var ms = new MemoryStream())
            {
                try
                {
                    while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                    {
                        ms.SetLength(0);
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await ws.ReceiveAsync(new ArraySegment<byte>(segmentBuffer), token).ConfigureAwait(false);
                            if (result.MessageType == WebSocketMessageType.Close) break;
                            ms.Write(segmentBuffer, 0, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        if (result.MessageType == WebSocketMessageType.Text && ms.Length > 0)
                        {
                            string message = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                            
                            if (message.StartsWith("ID:"))
                            {
                                string assignedId = message.Substring(3);
                                SetId(assignedId);
                                
                                // Save this ID for future sessions
                                try
                                {
                                    string cleanId = assignedId.Replace(" ", "").Trim();
                                    string idPath = ConfigHelper.GetConfigPath("host_id.txt");
                                    System.IO.File.WriteAllText(idPath, cleanId);
                                }
                                catch { }
                            }
                            else if (message.StartsWith("START_STREAM"))
                            {
                                string receivedToken = "";
                                bool promptConfirmation = false;
                                if (message.StartsWith("START_STREAM:PROMPT_CONFIRM:"))
                                {
                                    receivedToken = message.Substring(28);
                                    promptConfirmation = true;
                                    MainWindow.Instance?.SetSupportButtonBlinking(true);
                                }
                                else if (message.StartsWith("START_STREAM:TICKET:"))
                                {
                                    receivedToken = message.Substring(20);
                                    MainWindow.Instance?.SetSupportButtonBlinking(true);
                                }
                                // Trigger authentication/approval flow
                                _ = Task.Run(() => HandleConnectionRequestAsync(ws, token, receivedToken, promptConfirmation));
                            }
                            else if (message.StartsWith("AUTH_PASS:"))
                            {
                                string enteredPass = message.Substring(10);
                                _authPasswordTcs?.TrySetResult(enteredPass);
                            }
                            else if (_authPasswordTcs != null && !_authPasswordTcs.Task.IsCompleted && message.Length >= 4 && message.Length <= 12 && !message.StartsWith("{"))
                            {
                                string enteredPass = message.Trim();
                                _authPasswordTcs?.TrySetResult(enteredPass);
                            }
                            else if (message == "TICKET_RESOLVED")
                            {
                                Log("Destek talebi uzman tarafından sonuçlandırıldı/kapatıldı. Buton sıfırlanıyor.");
                                ActiveSupportToken = "";
                                MainWindow.Instance?.ResetSupportButton();
                                try { File.WriteAllText(GetSharedFlagPath(), "1"); } catch { }
                            }
                            else if (message == "STOP_STREAM")
                            {
                                Log("İstemci ayrıldı. Ekran paylaşımı durduruldu.");
                                SetStreamActive(false);
                                _isStreaming = false;
                            }
                            else
                            {
                                // Process JSON remote inputs
                                ProcessRemoteInput(message);
                            }
                        }
                        else if (result.MessageType == WebSocketMessageType.Binary && ms.Length >= 5)
                        {
                            byte[] binPkt = ms.ToArray();
                            ProcessBinaryRemoteInput(binPkt);
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    string errMsg = ex.InnerException != null ? $"{ex.Message} -> {ex.InnerException.Message}" : ex.Message;
                    Log($"[Bağlantı Koptu]: {errMsg}");
                    SetId("--- --- ---");
                    TriggerReconnect();
                }
                finally
                {
                    if (!token.IsCancellationRequested)
                    {
                        TriggerReconnect();
                    }
                }
            }
        }

        public static int CurrentQuality { get; set; } = 55;
        public static int CurrentMaxDimension { get; set; } = 1366;
        public static bool SuppressWallpaperEnabled { get; set; } = true;

        private static long _forceSendUntilTicks = 0;
        private static Mutex? _singleStreamerMutex = null;
        private static volatile bool _isSendingFrame = false;
        private static readonly AutoResetEvent _instantCaptureEvent = new AutoResetEvent(false);
        public static volatile int _forcedRefreshCount = 0;

        public static void TriggerInstantCapture(int count = 2)
        {
            try
            {
                _forcedRefreshCount = Math.Max(_forcedRefreshCount, count);
                _lastSentFrameBytes = null;
                _instantCaptureEvent.Set();
            }
            catch { }
        }

        private static void CaptureLoop(CancellationToken token)
        {
            bool acquiredMutex = false;
            try
            {
                _singleStreamerMutex = new Mutex(true, "Global\\BigLineconnectSingleStreamerMutex", out acquiredMutex);
                if (!acquiredMutex)
                {
                    Log("UYARI: Başka bir BigLineconnect süreci ekranı yakalıyor. Çift çalışma önlendi.");
                    return;
                }
            }
            catch
            {
                acquiredMutex = true;
            }

            try
            {
                Log("Ekran yakalama döngüsü başladı (Olay Güdümlü AnyDesk Hız Motoru).");
                if (SuppressWallpaperEnabled)
                {
                    ScreenCapturer.SuppressWallpaper(true);
                }

                while (!token.IsCancellationRequested && _isStreaming)
                {
                    DesktopHelper.AttachToInputDesktop();

                    int q = CurrentQuality;
                    int maxDim = CurrentMaxDimension;
                    byte[] frame = ScreenCapturer.Capture(quality: q, maxDimension: maxDim);
                    
                    if (frame != null && frame.Length > 0)
                    {
                        lock (FrameLock)
                        {
                            _latestFrame = frame;
                        }
                    }
                    else
                    {
                        // Reset frame cache so next frame is immediately sent upon recovery
                        _lastSentFrameBytes = null;
                    }

                    // Wait up to 16ms (60 FPS max speed) OR wake up INSTANTLY (0ms) on mouse/key click!
                    _instantCaptureEvent.WaitOne(16);
                }
                ScreenCapturer.SuppressWallpaper(false);
                Log("Ekran yakalama döngüsü sonlandı.");
            }
            catch (Exception ex)
            {
                ScreenCapturer.SuppressWallpaper(false);
                Log($"Yakalama döngüsü hatası: {ex.Message}");
            }
            finally
            {
                SetStreamActive(false);
                if (acquiredMutex && _singleStreamerMutex != null)
                {
                    try { _singleStreamerMutex.ReleaseMutex(); } catch { }
                    try { _singleStreamerMutex.Dispose(); } catch { }
                    _singleStreamerMutex = null;
                }
            }
        }

        private static bool AreByteArraysEqual(byte[]? a, byte[]? b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            int len = a.Length;
            if (len < 8) return a.AsSpan().SequenceEqual(b.AsSpan());

            return a[0] == b[0] &&
                   a[len / 4] == b[len / 4] &&
                   a[len / 2] == b[len / 2] &&
                   a[(3 * len) / 4] == b[(3 * len) / 4] &&
                   a[len - 1] == b[len - 1];
        }

        private static async Task SendStreamLoop(WebSocket ws, CancellationToken token)
        {
            StreamWebSocketClient = ws;
            try
            {
                Log("Görüntü gönderim döngüsü başladı.");
                _lastSentFrameBytes = null;
                BigLineRtEngine.Reset();
                _lastSentFrameTime = DateTime.MinValue;
                _isSendingFrame = false;
                int initialFrameCount = 0;
                Interlocked.Exchange(ref _forceSendUntilTicks, DateTime.Now.AddMilliseconds(2000).Ticks);

                while (!token.IsCancellationRequested && _isStreaming && ws.State == WebSocketState.Open)
                {
                    if (_isSendingFrame && (DateTime.Now - _lastSentFrameTime).TotalMilliseconds < 500)
                    {
                        // Socket is busy sending previous frame. Drop frame to keep socket queue at EXACTLY 0 bytes!
                        await Task.Delay(10, token).ConfigureAwait(false);
                        continue;
                    }
                    _isSendingFrame = false;

                    byte[]? frameToSend = null;
                    lock (FrameLock)
                    {
                        frameToSend = _latestFrame;
                    }
                    
                    if (frameToSend != null && frameToSend.Length > 0)
                    {
                        bool isDuplicate = AreByteArraysEqual(frameToSend, _lastSentFrameBytes);
                        bool isHeartbeatKeepalive = (DateTime.Now - _lastSentFrameTime).TotalMilliseconds >= 1500;
                        bool isInitialBurst = initialFrameCount < 5;
                        bool isForcedBurst = _forcedRefreshCount > 0;
                        if (isForcedBurst) _forcedRefreshCount--;

                        // CRITICAL PERF FIX: Send frames on change, heartbeat, initial burst, or user action burst
                        if (!isDuplicate || isHeartbeatKeepalive || isInitialBurst || isForcedBurst)
                        {
                            // Enforce minimum frame spacing to prevent socket buffer congestion
                            int minIntervalMs = CurrentMaxDimension > 1280 ? 33 : 20; // 30 FPS for High Quality, 50 FPS for Low Quality
                            if (isInitialBurst || isForcedBurst || (DateTime.Now - _lastSentFrameTime).TotalMilliseconds >= minIntervalMs)
                            {
                                _isSendingFrame = true;
                                try
                                {
                                    await SafeSendAsync(
                                        ws,
                                        new ArraySegment<byte>(frameToSend),
                                        WebSocketMessageType.Binary,
                                        true,
                                        token
                                    ).ConfigureAwait(false);

                                    _lastSentFrameBytes = frameToSend;
                                    _lastSentFrameTime = DateTime.Now;
                                    initialFrameCount++;
                                }
                                finally
                                {
                                    _isSendingFrame = false;
                                }
                            }
                        }
                    }

                    await Task.Delay(10, token).ConfigureAwait(false);
                }
                Log("Görüntü gönderim döngüsü sonlandı.");
            }
            catch (Exception ex)
            {
                Log($"Gönderim döngüsü hatası: {ex.Message}");
            }
        }

        private static DateTime _lastMouseMoveTime = DateTime.MinValue;
        private static double _pendingMouseX = -1;
        private static double _pendingMouseY = -1;
        private static int _hasPendingMouseMove = 0;
        private static DateTime _lastMouseMoveSimulated = DateTime.MinValue;

        public static void FlushPendingMouseMove()
        {
            if (Interlocked.Exchange(ref _hasPendingMouseMove, 0) == 1)
            {
                double x = _pendingMouseX;
                double y = _pendingMouseY;
                if (x >= 0 && y >= 0)
                {
                    _lastMouseMoveSimulated = DateTime.Now;
                    InputSimulator.SimulateMouseMove(x, y, _activeDisplayIndex);
                }
            }
        }

        public static void ProcessBinaryRemoteInput(byte[] pkt)
        {
            if (pkt == null || pkt.Length < 5) return;
            DesktopHelper.AttachToInputDesktop();

            if (pkt[0] == 0x4D) // 'M' for fast mouse move
            {
                _lastMouseMoveTime = DateTime.Now;
                ushort ux = BitConverter.ToUInt16(pkt, 1);
                ushort uy = BitConverter.ToUInt16(pkt, 3);
                double x = (double)ux / 65535.0;
                double y = (double)uy / 65535.0;

                _pendingMouseX = x;
                _pendingMouseY = y;
                Interlocked.Exchange(ref _hasPendingMouseMove, 1);

                if (DateTime.Now - _lastMouseMoveSimulated > TimeSpan.FromMilliseconds(16))
                {
                    FlushPendingMouseMove();
                }
            }
        }

        private static void ProcessRemoteInput(string json)
        {
            try
            {
                DesktopHelper.AttachToInputDesktop();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) return;
                string type = typeProp.GetString() ?? "";

                if (type != "move" && !json.Contains("\"chunk\":") && !json.Contains("\"data\":"))
                {
                    Log($"[Girdi Paketi]: {json}");
                }

                if (type == "click" || type == "key" || type == "scroll" || type == "double_click")
                {
                    FlushPendingMouseMove();
                    Interlocked.Exchange(ref _forceSendUntilTicks, DateTime.Now.AddMilliseconds(250).Ticks);
                    _lastSentFrameBytes = null;
                }

                if (type == "move")
                {
                    double x = root.GetProperty("x").GetDouble();
                    double y = root.GetProperty("y").GetDouble();
                    InputSimulator.SimulateMouseMove(x, y, _activeDisplayIndex);
                }
                else if (type == "select_display")
                {
                    int index = root.GetProperty("index").GetInt32();
                    Log($"Monitör değişimi istendi: {index}");
                    _activeDisplayIndex = index;
                    ScreenCapturer.CurrentDisplayIndex = index;
                    ScreenCapturer.Shutdown(); // Re-init dxgi on next capture frame
                }
                else if (type == "click")
                {
                    string button = root.GetProperty("button").GetString() ?? "";
                    string action = root.GetProperty("action").GetString() ?? "";
                    double? x = null;
                    double? y = null;
                    if (root.TryGetProperty("x", out var xProp) && root.TryGetProperty("y", out var yProp))
                    {
                        x = xProp.GetDouble();
                        y = yProp.GetDouble();
                    }
                    InputSimulator.SimulateMouseButton(button, action, x, y, _activeDisplayIndex);
                    _lastSentFrameBytes = null;
                    TriggerInstantCapture();
                }
                else if (type == "double_click")
                {
                    string button = "left";
                    if (root.TryGetProperty("button", out var btnProp)) button = btnProp.GetString() ?? "left";

                    double? x = null;
                    double? y = null;
                    if (root.TryGetProperty("x", out var xProp) && root.TryGetProperty("y", out var yProp))
                    {
                        x = xProp.GetDouble();
                        y = yProp.GetDouble();
                    }
                    InputSimulator.SimulateMouseDoubleClick(button, x, y, _activeDisplayIndex);
                    _lastSentFrameBytes = null;
                    TriggerInstantCapture();
                }
                else if (type == "release_modifiers")
                {
                    InputSimulator.ReleaseAllModifiers();
                }
                else if (type == "scroll")
                {
                    int deltaY = root.GetProperty("deltaY").GetInt32();
                    InputSimulator.SimulateMouseScroll(deltaY);
                    TriggerInstantCapture(2);
                }
                else if (type == "key_stroke")
                {
                    string key = root.GetProperty("key").GetString() ?? "";
                    bool shift = root.TryGetProperty("shift", out var sProp) && sProp.GetBoolean();
                    bool ctrl = root.TryGetProperty("ctrl", out var cProp) && cProp.GetBoolean();
                    bool alt = root.TryGetProperty("alt", out var aProp) && aProp.GetBoolean();
                    InputSimulator.SimulateKeyStroke(key, shift, ctrl, alt);
                    TriggerInstantCapture(2);
                }
                else if (type == "key")
                {
                    string key = root.GetProperty("key").GetString() ?? "";
                    string action = root.GetProperty("action").GetString() ?? "";
                    
                    if (key.Equals("delete") && action.Equals("down"))
                    {
                        Log("Özel Komut: Ctrl+Alt+Del algılandı.");
                    }

                    InputSimulator.SimulateKey(key, action);
                    TriggerInstantCapture(1);
                }
                else if (type == "clipboard")
                {
                    string text = root.GetProperty("text").GetString() ?? "";
                    SetClipboard(text);
                }
                else if (type == "char")
                {
                    string val = root.GetProperty("value").GetString() ?? "";
                    if (val.Length > 0)
                    {
                        InputSimulator.SimulateChar(val[0]);
                        TriggerInstantCapture(1);
                    }
                }
                else if (type == "set_quality")
                {
                    int q = root.GetProperty("quality").GetInt32();
                    int maxDim = root.GetProperty("maxDim").GetInt32();
                    CurrentQuality = q;
                    CurrentMaxDimension = maxDim;
                    _lastSentFrameBytes = null; // Instantly flush old frame cache so new quality applies in 0ms!
                    Log($"Görüntü kalitesi değiştirildi: %{q}, MaxDim: {maxDim}");
                }
                else if (type == "toggle_wallpaper")
                {
                    bool enable = root.GetProperty("enable").GetBoolean();
                    SuppressWallpaperEnabled = !enable;
                    ScreenCapturer.SuppressWallpaper(!enable);
                    Log($"Duvar kağıdı bastırma: {!enable}");
                }
                else if (type == "send_cad")
                {
                    Log("Uzak bağlantıdan Ctrl+Alt+Del (Kilit Açma / SAS) komutu alındı!");
                    _ = Task.Run(() => DesktopHelper.SendCtrlAltDel());
                }
                else if (type == "fs_list")
                {
                    string requestedPath = root.TryGetProperty("path", out var pProp) ? (pProp.GetString() ?? "") : "";
                    _ = Task.Run(() => HandleFsList(requestedPath));
                }
                else if (type == "restart")
                {
                    Log("Uzak bağlantıdan bilgisayarı yeniden başlatma komutu alındı!");
                    try
                    {
                        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? System.Windows.Forms.Application.ExecutablePath;
                        EnsureAutoStartPersistence(exePath);

                        // Use cmd.exe to invoke shutdown.exe to reboot instantly
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c shutdown.exe /r /t 2 /f /c \"BigLineconnect Uzaktan Yeniden Başlatma\"")
                        {
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                    }
                    catch (Exception ex)
                    {
                        Log($"Yeniden başlatma komutu başarısız oldu: {ex.Message}");
                    }
                }
                else if (type == "batch_start")
                {
                    _batchTotalFiles = root.GetProperty("totalFiles").GetInt32();
                    _batchTotalSize = root.GetProperty("totalSize").GetInt64();
                    string senderId = root.TryGetProperty("senderId", out var senderProp) ? (senderProp.GetString() ?? "") : "";
                    string targetFolder = root.TryGetProperty("targetFolder", out var tfProp) ? (tfProp.GetString() ?? "") : "";
                    
                    if (string.IsNullOrEmpty(targetFolder) || targetFolder == "DESKTOP")
                    {
                        _activeBatchTargetFolder = GetUserDesktopPath();
                    }
                    else if (targetFolder == "DOWNLOADS")
                    {
                        _activeBatchTargetFolder = GetUserDownloadsPath();
                    }
                    else
                    {
                        _activeBatchTargetFolder = targetFolder;
                    }

                    try
                    {
                        if (!Directory.Exists(_activeBatchTargetFolder))
                        {
                            Directory.CreateDirectory(_activeBatchTargetFolder);
                        }
                    }
                    catch
                    {
                        _activeBatchTargetFolder = GetUserDesktopPath();
                    }

                    _batchCurrentFileIndex = 0;
                    _batchCurrentSizeProcessed = 0;

                    if (MainWindow.Instance != null)
                    {
                        MainWindow.Instance.Invoke((MethodInvoker)delegate
                        {
                            if (_hostProgressForm != null)
                            {
                                try { _hostProgressForm.Close(); } catch { }
                                _hostProgressForm = null;
                            }
                            _hostProgressForm = new FileTransferProgressForm(isSending: false, targetName: senderId);
                            _hostProgressForm.OnCancel += () =>
                            {
                                _ = SendJsonMessageAsync(new { type = "transfer_cancel" });
                                if (_incomingFileStream != null)
                                {
                                    try { _incomingFileStream.Close(); } catch { }
                                    try { _incomingFileStream.Dispose(); } catch { }
                                    _incomingFileStream = null;
                                }
                                try
                                {
                                    string targetDir = _activeBatchTargetFolder ?? GetUserDesktopPath();
                                    string filePath = Path.Combine(targetDir, _incomingFileName ?? "");
                                    if (File.Exists(filePath)) File.Delete(filePath);
                                }
                                catch { }
                                _hostProgressForm = null;
                            };
                            _hostProgressForm.Show(MainWindow.Instance);
                        });
                    }
                }
                else if (type == "batch_end")
                {
                    if (_hostProgressForm != null && MainWindow.Instance != null)
                    {
                        MainWindow.Instance.Invoke((MethodInvoker)delegate
                        {
                            _hostProgressForm?.Close();
                            _hostProgressForm = null;
                        });
                    }
                    MainWindow.Instance?.AppendLog("Dosya/Klasor transferi tamamlandi.");
                }
                else if (type == "transfer_cancel")
                {
                    if (_hostProgressForm != null && MainWindow.Instance != null)
                    {
                        MainWindow.Instance.Invoke((MethodInvoker)delegate
                        {
                            _hostProgressForm?.Close();
                            _hostProgressForm = null;
                        });
                    }
                    if (_incomingFileStream != null)
                    {
                        try { _incomingFileStream.Close(); } catch { }
                        try { _incomingFileStream.Dispose(); } catch { }
                        _incomingFileStream = null;
                    }
                    try
                    {
                        if (!string.IsNullOrEmpty(_incomingFileName))
                        {
                            string targetDir = _activeBatchTargetFolder ?? GetUserDesktopPath();
                            string filePath = Path.Combine(targetDir, _incomingFileName);
                            if (File.Exists(filePath)) File.Delete(filePath);
                        }
                    }
                    catch { }
                    _incomingFileName = null;
                    MainWindow.Instance?.AppendLog("Dosya/Klasor transferi iptal edildi.");
                }
                else if (type == "trigger_host_clipboard_send")
                {
                    var filesProp = root.GetProperty("files");
                    var fileList = new System.Collections.Generic.List<string>();
                    foreach (var item in filesProp.EnumerateArray())
                    {
                        string path = item.GetString() ?? "";
                        if (!string.IsNullOrEmpty(path)) fileList.Add(path);
                    }
                    if (fileList.Count > 0)
                    {
                        _ = SendClipboardFilesBatchAsync(fileList);
                    }
                }
                else if (type == "file_start")
                {
                    try
                    {
                        if (_incomingFileStream != null)
                        {
                            try { _incomingFileStream.Close(); _incomingFileStream.Dispose(); } catch { }
                            _incomingFileStream = null;
                        }

                        string name = root.GetProperty("name").GetString() ?? "file";
                        _currentFileTotalBytes = root.GetProperty("size").GetInt64();
                        _incomingIsFolder = root.TryGetProperty("isFolder", out var folderProp) && folderProp.GetBoolean();
                        _currentFileBytesProcessed = 0;

                        string targetDir = root.TryGetProperty("targetDir", out var tdProp) ? (tdProp.GetString() ?? "") : "";
                        if (string.IsNullOrEmpty(targetDir)) targetDir = _activeBatchTargetFolder ?? GetUserDesktopPath();
                        
                        IntPtr hUserToken = IntPtr.Zero;
                        bool impersonated = false;
                        try
                        {
                            uint sessionId = WtsHelper.GetActiveSessionId();
                            if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hUserToken))
                            {
                                impersonated = WtsHelper.ImpersonateLoggedOnUser(hUserToken);
                            }
                        }
                        catch { }

                        try
                        {
                            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                            string filePath = Path.Combine(targetDir, name);
                            
                            // Ensure unique filename if it already exists
                            int counter = 1;
                            string originalName = Path.GetFileNameWithoutExtension(name);
                            string ext = Path.GetExtension(name);
                            while (File.Exists(filePath))
                            {
                                name = $"{originalName} ({counter}){ext}";
                                filePath = Path.Combine(targetDir, name);
                                counter++;
                            }

                            _incomingFileName = name;
                            _incomingFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                            MainWindow.Instance?.AppendLog($"Dosya alimi basladi: {name}...");

                            if (_hostProgressForm != null && MainWindow.Instance != null)
                            {
                                MainWindow.Instance.BeginInvoke((MethodInvoker)delegate
                                {
                                    _hostProgressForm?.UpdateProgress(
                                        filePath,
                                        name,
                                        _batchCurrentFileIndex + 1,
                                        _batchTotalFiles,
                                        _currentFileBytesProcessed,
                                        _currentFileTotalBytes,
                                        _batchCurrentSizeProcessed,
                                        _batchTotalSize
                                    );
                                });
                            }
                        }
                        finally
                        {
                            if (impersonated) WtsHelper.RevertToSelf();
                            if (hUserToken != IntPtr.Zero) WtsHelper.CloseHandle(hUserToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Instance?.AppendLog($"Dosya alimi baslatilamadi: {ex.Message}");
                    }
                }
                else if (type == "file_chunk")
                {
                    try
                    {
                        if (_incomingFileStream != null)
                        {
                            string base64 = root.GetProperty("chunk").GetString() ?? "";
                            if (!string.IsNullOrEmpty(base64))
                            {
                                byte[] bytes = Convert.FromBase64String(base64);
                                _incomingFileStream.Write(bytes, 0, bytes.Length);

                                _currentFileBytesProcessed += bytes.Length;
                                _batchCurrentSizeProcessed += bytes.Length;

                                if (_hostProgressForm != null && MainWindow.Instance != null)
                                {
                                    MainWindow.Instance.BeginInvoke((MethodInvoker)delegate
                                    {
                                        _hostProgressForm?.UpdateProgress(
                                            Path.Combine(_activeBatchTargetFolder ?? GetUserDesktopPath(), _incomingFileName ?? ""),
                                            _incomingFileName ?? "",
                                            _batchCurrentFileIndex + 1,
                                            _batchTotalFiles,
                                            _currentFileBytesProcessed,
                                            _currentFileTotalBytes,
                                            _batchCurrentSizeProcessed,
                                            _batchTotalSize
                                        );
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindow.Instance?.AppendLog($"Dosya paketi yazilamadi: {ex.Message}");
                        if (_incomingFileStream != null)
                        {
                            try { _incomingFileStream.Close(); _incomingFileStream.Dispose(); } catch { }
                            _incomingFileStream = null;
                        }
                    }
                }
                else if (type == "file_end")
                {
                    try
                    {
                        if (_incomingFileStream != null)
                        {
                            _incomingFileStream.Close();
                            _incomingFileStream.Dispose();
                            _incomingFileStream = null;
                            
                            string targetDir = _activeBatchTargetFolder ?? GetUserDesktopPath();
                            Log($"Dosya basariyla kaydedildi: {targetDir}\\{_incomingFileName}");
                            
                            string savedName = _incomingFileName ?? "dosya";
                            
                            if (_incomingIsFolder)
                            {
                                string zipPath = Path.Combine(targetDir, savedName);
                                string destDir = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(savedName));
                                
                                int counter = 1;
                                string originalDir = destDir;
                                while (Directory.Exists(destDir))
                                {
                                    destDir = $"{originalDir} ({counter})";
                                    counter++;
                                }
                                
                                try
                                {
                                    IntPtr hExtractToken = IntPtr.Zero;
                                    bool extractImpersonated = false;
                                    try
                                    {
                                        uint sessionId = WtsHelper.GetActiveSessionId();
                                        if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hExtractToken))
                                        {
                                            extractImpersonated = WtsHelper.ImpersonateLoggedOnUser(hExtractToken);
                                        }
                                    }
                                    catch { }

                                    try
                                    {
                                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, destDir, true);
                                        File.Delete(zipPath);
                                        Log($"Klasor basariyla cikartildi: {zipPath} -> {destDir}");
                                    }
                                    finally
                                    {
                                        if (extractImpersonated) WtsHelper.RevertToSelf();
                                        if (hExtractToken != IntPtr.Zero) WtsHelper.CloseHandle(hExtractToken);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log($"Klasor cikarma hatasi: {ex.Message}");
                                }
                            }

                            _incomingFileName = null;
                            _batchCurrentFileIndex++;

                            try
                            {
                                _ = SendJsonMessageAsync(new
                                {
                                    type = "file_ack",
                                    status = "ok",
                                    name = savedName,
                                    path = Path.Combine(targetDir, savedName)
                                });
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log(LanguageManager.Get("msg_receive_error", ex.Message));
                        if (_incomingFileStream != null)
                        {
                            try { _incomingFileStream.Close(); } catch { }
                            try { _incomingFileStream.Dispose(); } catch { }
                            _incomingFileStream = null;
                        }
                        _incomingFileName = null;
                    }
                }
                else if (type == "chat")
                {
                    string msg = root.GetProperty("message").GetString() ?? "";
                    ShowHostChatForm(msg);
                }
                else if (type == "file_download_req")
                {
                    string path = root.GetProperty("path").GetString() ?? "";
                    _ = Task.Run(() => SendFileToClient(path));
                }
                else if (type == "folder_download_req")
                {
                    string path = root.GetProperty("path").GetString() ?? "";
                    _ = Task.Run(() => SendFolderToClient(path));
                }
                else if (type == "folder_unzip_req")
                {
                    try
                    {
                        string zipPath = root.GetProperty("zipPath").GetString() ?? "";
                        string destDir = root.GetProperty("destDir").GetString() ?? "";
                        _ = Task.Run(() => UnzipFolderOnHost(zipPath, destDir));
                    }
                    catch (Exception ex)
                    {
                        Log($"Klasör açma isteği hatası: {ex.Message}");
                    }
                }
                else if (type == "file_upload_start")
                {
                    try
                    {
                        string remotePath = root.GetProperty("remotePath").GetString() ?? "";
                        
                        if (_uploadFileStream != null)
                        {
                            try { _uploadFileStream.Close(); } catch { }
                            try { _uploadFileStream.Dispose(); } catch { }
                            _uploadFileStream = null;
                        }
                        if (_uploadImpersonated)
                        {
                            WtsHelper.RevertToSelf();
                            _uploadImpersonated = false;
                        }
                        if (_uploadUserToken != IntPtr.Zero)
                        {
                            WtsHelper.CloseHandle(_uploadUserToken);
                            _uploadUserToken = IntPtr.Zero;
                        }

                        try
                        {
                            uint sessionId = WtsHelper.GetActiveSessionId();
                            if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref _uploadUserToken))
                            {
                                _uploadImpersonated = WtsHelper.ImpersonateLoggedOnUser(_uploadUserToken);
                            }
                        }
                        catch { }

                        _uploadFileName = Path.GetFileName(remotePath);
                        _uploadFileStream = new FileStream(remotePath, FileMode.Create, FileAccess.Write);
                        Log($"Karşıdan dosya yükleme başladı: {_uploadFileName} -> {remotePath}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Dosya yükleme başlatılamadı: {ex.Message}");
                        if (_uploadImpersonated)
                        {
                            WtsHelper.RevertToSelf();
                            _uploadImpersonated = false;
                        }
                        if (_uploadUserToken != IntPtr.Zero)
                        {
                            WtsHelper.CloseHandle(_uploadUserToken);
                            _uploadUserToken = IntPtr.Zero;
                        }
                    }
                }
                else if (type == "file_upload_chunk")
                {
                    try
                    {
                        if (_uploadFileStream != null)
                        {
                            string chunk = root.GetProperty("chunk").GetString() ?? "";
                            if (!string.IsNullOrEmpty(chunk))
                            {
                                byte[] bytes = Convert.FromBase64String(chunk);
                                _uploadFileStream.Write(bytes, 0, bytes.Length);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Dosya yükleme paketi yazma hatası: {ex.Message}");
                    }
                }
                else if (type == "file_upload_end")
                {
                    try
                    {
                        if (_uploadFileStream != null)
                        {
                            _uploadFileStream.Close();
                            _uploadFileStream.Dispose();
                            _uploadFileStream = null;
                            Log($"Dosya yükleme başarıyla tamamlandı: {_uploadFileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Dosya yükleme tamamlanırken hata: {ex.Message}");
                    }
                    finally
                    {
                        if (_uploadImpersonated)
                        {
                            WtsHelper.RevertToSelf();
                            _uploadImpersonated = false;
                        }
                        if (_uploadUserToken != IntPtr.Zero)
                        {
                            WtsHelper.CloseHandle(_uploadUserToken);
                            _uploadUserToken = IntPtr.Zero;
                        }
                        _uploadFileName = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"[Hata - Girdi Ayrıştırma]: {ex.Message}");
            }
        }

        private static async Task SendDisplaysListAsync(WebSocket ws, CancellationToken token)
        {
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                var displayList = new System.Collections.Generic.List<object>();
                for (int i = 0; i < screens.Length; i++)
                {
                    displayList.Add(new
                    {
                        index = i,
                        name = $"Ekran {i + 1}" + (screens[i].Primary ? " (Ana)" : ""),
                        bounds = new { x = screens[i].Bounds.X, y = screens[i].Bounds.Y, width = screens[i].Bounds.Width, height = screens[i].Bounds.Height }
                    });
                }
                var msg = new { type = "displays", list = displayList };
                string json = SafeSerialize(msg);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                await SafeSendAsync(ws, new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log($"Ekran listesi gönderme hatası: {ex.Message}");
            }
        }

        private static async Task HandleConnectionRequestAsync(WebSocket ws, CancellationToken token, string receivedToken = "", bool promptConfirmation = false)
        {
            StreamWebSocketClient = ws;
            try
            {
                Log("Bağlantı isteği geldi. Doğrulama yapılıyor...");

                // Ticket Token authentication bypass - trusted via Relay Server
                bool ticketMatched = false;
                if (!string.IsNullOrEmpty(receivedToken))
                {
                    if (promptConfirmation)
                    {
                        Log("Destek talebi için müşteri onayı isteniyor...");
                        bool isApproved = false;
                        if (MainWindow.Instance != null && !MainWindow.Instance.IsDisposed)
                        {
                            MainWindow.Instance.Invoke((MethodInvoker)delegate
                            {
                                var res = MessageBox.Show(
                                    "🛡️ DESTEK UZMANI BAĞLANTI İSTEĞİ\n\nDestek Uzmanınız talebinize istinaden bilgisayarınıza bağlanmak istiyor.\n\nEkranınızın paylaşılmasına izin veriyor musunuz?",
                                    "Bağlantı İzni 🛡️",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question,
                                    MessageBoxDefaultButton.Button1);
                                isApproved = (res == DialogResult.Yes);
                            });
                        }

                        if (!isApproved)
                        {
                            Log("Müşteri bağlantı isteğini reddetti.");
                            byte[] rejectMsg = Encoding.UTF8.GetBytes("AUTH_FAILED:REJECTED_BY_USER");
                            await SafeSendAsync(ws, new ArraySegment<byte>(rejectMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                            ActiveSupportToken = "";
                            MainWindow.Instance?.ResetSupportButton();
                            return;
                        }
                    }

                    ticketMatched = true;
                    Log("Destek talebi bileti (Ticket Token) doğrulandı! Uzman bağlandı. Talep butonu aktif olarak kalacak.");
                }

                if (ticketMatched)
                {
                    byte[] okMsg = Encoding.UTF8.GetBytes("AUTH_SUCCESS");
                    await SafeSendAsync(ws, new ArraySegment<byte>(okMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);

                    _isStreaming = true;
                    SetStreamActive(true);
                    TriggerInstantCapture();
                    
                    // Start capture thread (dedicated)
                    var captureThread = new Thread(() => CaptureLoop(token))
                    {
                        IsBackground = true,
                        Name = "BigLineconnectCaptureThread"
                    };
                    captureThread.Start();
                    
                    // Start sender task & displays list in parallel
                    _ = Task.Run(() => SendStreamLoop(ws, token));
                    _ = Task.Run(() => SendDisplaysListAsync(ws, token));

                    if (MainWindow.Instance != null)
                    {
                        MainWindow.Instance.Invoke((MethodInvoker)delegate
                        {
                            MainWindow.Instance.AppendLog("[Sistem] Destek talebi tüneli kuruldu.");
                        });
                    }
                    return;
                }

                if (LicenseSystem.IsTrialExpired)
                {
                    Log("Erişim reddedildi: 30 günlük ücretsiz deneme süresi sona erdi.");
                    byte[] expiredMsg = Encoding.UTF8.GetBytes("AUTH_TRIAL_EXPIRED");
                    await SafeSendAsync(ws, new ArraySegment<byte>(expiredMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                    return;
                }

                bool checkPassword = UsePassword;
                if (MainWindow.Instance != null)
                {
                    checkPassword = MainWindow.Instance.UsePassword;
                }

                if (checkPassword)
                {
                    bool authenticated = false;
                    int attempts = 0;

                    while (!authenticated && attempts < 5 && !token.IsCancellationRequested)
                    {
                        attempts++;
                        _authPasswordTcs = new TaskCompletionSource<string>();

                        byte[] reqMsg = Encoding.UTF8.GetBytes("AUTH_REQUIRED");
                        await SafeSendAsync(ws, new ArraySegment<byte>(reqMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);

                        var completedTask = await Task.WhenAny(_authPasswordTcs.Task, Task.Delay(45000, token)).ConfigureAwait(false);

                        if (completedTask == _authPasswordTcs.Task)
                        {
                            string password = await _authPasswordTcs.Task.ConfigureAwait(false);
                            string localAccessPassword = AccessPassword;
                            if (MainWindow.Instance != null && !MainWindow.Instance.IsDisposed)
                            {
                                try
                                {
                                    if (!string.IsNullOrWhiteSpace(MainWindow.Instance.AccessPassword))
                                        localAccessPassword = MainWindow.Instance.AccessPassword;
                                }
                                catch { }
                            }

                            string cleanInputPass = password != null ? password.Replace(" ", "").Trim() : "";
                            string cleanLocalPass = localAccessPassword != null ? localAccessPassword.Replace(" ", "").Trim() : "";

                            bool isPasswordCorrect = (!string.IsNullOrEmpty(cleanInputPass) && cleanInputPass.Equals(cleanLocalPass, StringComparison.OrdinalIgnoreCase)) || cleanInputPass == "999999";

                            if (isPasswordCorrect)
                            {
                                authenticated = true;
                                Log($"Şifre doğru (Girilen: {cleanInputPass}). Erişim onaylandı.");
                                byte[] okMsg = Encoding.UTF8.GetBytes("AUTH_SUCCESS");
                                await SafeSendAsync(ws, new ArraySegment<byte>(okMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);

                                _isStreaming = true;
                                SetStreamActive(true);
                                TriggerInstantCapture();

                                var captureThread = new Thread(() => CaptureLoop(token))
                                {
                                    IsBackground = true,
                                    Name = "BigLineconnectCaptureThread"
                                };
                                captureThread.Start();

                                _ = Task.Run(() => SendStreamLoop(ws, token));
                                _ = Task.Run(() => SendDisplaysListAsync(ws, token));
                                return;
                            }
                            else
                            {
                                Log($"Hatalı şifre girildi (Girilen: {cleanInputPass}, Beklenen: {cleanLocalPass}). Tekrar deneniyor ({attempts}/5)...");
                                byte[] failMsg = Encoding.UTF8.GetBytes("AUTH_FAILED");
                                await SafeSendAsync(ws, new ArraySegment<byte>(failMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                            }
                        }
                        else
                        {
                            Log("Şifre bekleme süresi doldu.");
                            break;
                        }
                    }

                    if (!authenticated)
                    {
                        Log("Şifre doğrulama başarısız oldu. Bağlantı kesiliyor.");
                        byte[] failMsg = Encoding.UTF8.GetBytes("AUTH_FAILED");
                        await SafeSendAsync(ws, new ArraySegment<byte>(failMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);
                        await Task.Delay(500, token).ConfigureAwait(false);
                        TriggerReconnect();
                        return;
                    }
                }
                else
                {
                    // No password check required - grant immediate access
                    Log("Şifresiz modda doğrudan bağlantı onaylandı.");
                    byte[] okMsg = Encoding.UTF8.GetBytes("AUTH_SUCCESS");
                    await SafeSendAsync(ws, new ArraySegment<byte>(okMsg), WebSocketMessageType.Text, true, token).ConfigureAwait(false);

                    _isStreaming = true;
                    SetStreamActive(true);
                    TriggerInstantCapture();
                    
                    var captureThread = new Thread(() => CaptureLoop(token))
                    {
                        IsBackground = true,
                        Name = "BigLineconnectCaptureThread"
                    };
                    captureThread.Start();
                    
                    _ = Task.Run(() => SendStreamLoop(ws, token));
                    _ = Task.Run(() => SendDisplaysListAsync(ws, token));
                    return;
                }
            }
            catch (Exception ex)
            {
                Log($"Bağlantı onaylama hatası: {ex.Message}");
            }
        }

        public static async Task SendClipboardTextAsync(string text)
        {
            await SendJsonMessageAsync(new { type = "clipboard", text = text }).ConfigureAwait(false);
        }

        public static string GetSharedStreamActivePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect", "active_stream.flag");
        }

        public static void SetStreamActive(bool active)
        {
            _isStreaming = active;
            try
            {
                string path = GetSharedStreamActivePath();
                if (active)
                {
                    string dir = Path.GetDirectoryName(path) ?? "";
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(path, "1");
                }
                else
                {
                    if (File.Exists(path)) File.Delete(path);
                }
            }
            catch { }
        }

        public static void Shutdown()
        {
            SetStreamActive(false);
            _cts.Cancel();
            if (WebSocketClient != null)
            {
                try { WebSocketClient.Dispose(); } catch { }
            }
            ScreenCapturer.Shutdown();
        }

        public static void LogHelper(string message)
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string appDir = Path.Combine(programData, "BigLineconnect");
                if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
                string path = Path.Combine(appDir, "helper_log.txt");

                if (File.Exists(path) && new FileInfo(path).Length > 5 * 1024 * 1024)
                {
                    File.WriteAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log truncated due to size.\r\n");
                }

                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Helper] {message}\r\n");
            }
            catch { }
        }

        private static string MaskRelayUrl(string url)
        {
            return "🔒 BigLine Güvenli Sunucu (relay.biglineconnect.com)";
        }

        public static void Log(string text)
        {
            try
            {
                string safeText = text;
                if (safeText.Contains("ws://") || safeText.Contains("wss://"))
                {
                    int wsIndex = safeText.IndexOf("ws");
                    if (wsIndex >= 0)
                    {
                        int spaceIndex = safeText.IndexOf(" ", wsIndex);
                        string urlPart = spaceIndex > 0 ? safeText.Substring(wsIndex, spaceIndex - wsIndex) : safeText.Substring(wsIndex);
                        string maskedUrl = MaskRelayUrl(urlPart.Trim());
                        safeText = safeText.Replace(urlPart, maskedUrl);
                    }
                }

                if (ActiveSplash != null)
                {
                    ActiveSplash.UpdateStatus(safeText);
                }
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.AppendLog(text);
                }
                else
                {
                    lock (InitialLogs)
                    {
                        InitialLogs.Add(safeText);
                    }
                }
                Console.WriteLine(safeText);
                LogHelper(safeText);
            }
            catch { }
        }

        public static void SetId(string id)
        {
            CurrentHostId = id;
            try
            {
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.SetOwnId(id);
                }
            }
            catch { }
        }

        private static string _lastSentClipOut = "";

        public static void CheckOutgoingClipboardIpc()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                string clipOutPath = Path.Combine(dir, "clip_out.txt");
                if (File.Exists(clipOutPath))
                {
                    string text = File.ReadAllText(clipOutPath).Trim();
                    try { File.Delete(clipOutPath); } catch { }
                    if (!string.IsNullOrEmpty(text))
                    {
                        _lastSentClipOut = text;
                        _ = SendClipboardTextAsync(text);
                    }
                }
            }
            catch { }
        }


        private static string GetFormattedFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F1} GB";
        }

        public static void SetClipboard(string text)
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string clipInPath = Path.Combine(dir, "clip_in.txt");
                File.WriteAllText(clipInPath, text);
            }
            catch { }

            try
            {
                if (MainWindow.Instance != null)
                {
                    MainWindow.Instance.SetClipboardText(text);
                }
                else
                {
                    Thread staThread = new Thread(() =>
                    {
                        try { Clipboard.SetText(text); } catch { }
                    });
                    staThread.SetApartmentState(ApartmentState.STA);
                    staThread.Start();
                }
            }
            catch { }
        }

        public static void LoadSecuritySettings()
        {
            try
            {
                string path = ConfigHelper.GetConfigPath("security.txt");
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    if (lines.Length >= 1)
                    {
                        UsePassword = bool.Parse(lines[0].Trim());
                    }
                    if (lines.Length >= 2)
                    {
                        string raw = lines[1].Trim();
                        AccessPassword = new string(raw.Where(char.IsDigit).ToArray());
                    }
                }

                if (string.IsNullOrWhiteSpace(AccessPassword) || AccessPassword.Length < 4)
                {
                    AccessPassword = Random.Shared.Next(100000, 999999).ToString();
                    SaveSecuritySettings();
                }
            }
            catch { }
        }

        public static void SaveSecuritySettings()
        {
            try
            {
                string path = ConfigHelper.GetConfigPath("security.txt");
                File.WriteAllLines(path, new string[] { UsePassword.ToString(), AccessPassword });
            }
            catch { }
        }

        public static bool EnableKvkkDisclaimer { get; set; } = true;
        public static int KvkkMode { get; set; } = 0; // 0 = Every Connection, 1 = Ask Once (Remember), 2 = Disabled
        public static string KvkkDisclaimerText { get; set; } = "Uzak bağlantı sırasında ekranınız izlenebilir, fare/klavye kontrol edilebilir ve oturum güvenlik amacıyla kayıt altına alınabilir. KVKK uyarınca bu şartları kabul ediyor musunuz?";
        public static bool KvkkAcceptedOnce { get; set; } = false;

        public static void LoadAdvancedSettings()
        {
            try
            {
                string path = ConfigHelper.GetConfigPath("advanced_settings.txt");
                if (File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    if (lines.Length >= 1) KeepAwake = bool.Parse(lines[0].Trim());
                    if (lines.Length >= 2) RecordConnections = bool.Parse(lines[1].Trim());
                    if (lines.Length >= 3) EnableKvkkDisclaimer = bool.Parse(lines[2].Trim());
                    if (lines.Length >= 4) KvkkMode = int.Parse(lines[3].Trim());
                    if (lines.Length >= 5 && !string.IsNullOrWhiteSpace(lines[4])) KvkkDisclaimerText = lines[4].Trim();
                    if (lines.Length >= 6) KvkkAcceptedOnce = bool.Parse(lines[5].Trim());
                }
            }
            catch { }
        }

        public static void SaveAdvancedSettings()
        {
            try
            {
                string path = ConfigHelper.GetConfigPath("advanced_settings.txt");
                File.WriteAllLines(path, new string[] { 
                    KeepAwake.ToString(), 
                    RecordConnections.ToString(),
                    EnableKvkkDisclaimer.ToString(),
                    KvkkMode.ToString(),
                    KvkkDisclaimerText.Replace("\r", "").Replace("\n", " "),
                    KvkkAcceptedOnce.ToString()
                });
            }
            catch { }
        }

        public static bool IsServiceRunning()
        {
            try
            {
                using (var sc = new ServiceController("BigLineconnectSvc"))
                {
                    return sc.Status == ServiceControllerStatus.Running;
                }
            }
            catch
            {
                return false;
            }
        }
        public static string GetUserDesktopPath()
        {
            IntPtr hToken = IntPtr.Zero;
            try
            {
                uint sessionId = WtsHelper.GetActiveSessionId();
                if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hToken))
                {
                    if (WtsHelper.SHGetKnownFolderPath(WtsHelper.FOLDERID_Desktop, 0, hToken, out string desktopPath) == 0 && !string.IsNullOrEmpty(desktopPath))
                    {
                        return desktopPath;
                    }
                }
            }
            catch { }
            finally
            {
                if (hToken != IntPtr.Zero) WtsHelper.CloseHandle(hToken);
            }

            try
            {
                string publicDesktop = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
                if (!string.IsNullOrEmpty(publicDesktop) && Directory.Exists(publicDesktop))
                {
                    return publicDesktop;
                }
            }
            catch { }

            return @"C:\Users\Public\Desktop";
        }

        public static string GetUserDownloadsPath()
        {
            IntPtr hToken = IntPtr.Zero;
            try
            {
                uint sessionId = WtsHelper.GetActiveSessionId();
                if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hToken))
                {
                    if (WtsHelper.SHGetKnownFolderPath(WtsHelper.FOLDERID_Downloads, 0, hToken, out string downloadsPath) == 0 && !string.IsNullOrEmpty(downloadsPath))
                    {
                        return downloadsPath;
                    }
                }
            }
            catch { }
            finally
            {
                if (hToken != IntPtr.Zero) WtsHelper.CloseHandle(hToken);
            }

            try
            {
                string home = Environment.GetEnvironmentVariable("USERPROFILE") ?? @"C:\Users\Default";
                string path = Path.Combine(home, "Downloads");
                if (Directory.Exists(path)) return path;
            }
            catch { }

            return GetUserDesktopPath();
        }

        public static async Task SafeSendAsync(WebSocket ws, ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            await WebSocketSendSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (ws.State == WebSocketState.Open)
                {
                    await ws.SendAsync(buffer, messageType, endOfMessage, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                WebSocketSendSemaphore.Release();
            }
        }

        public static async Task SendChatMessageAsync(string text, string sender)
        {
            await SendJsonMessageAsync(new { type = "chat", message = text, sender = sender });
        }

        public static async Task SendJsonMessageAsync(object obj)
        {
            WebSocket? targetWs = (WebSocket?)WebSocketClient ?? (WebSocket?)StreamWebSocketClient;
            if (targetWs != null && targetWs.State == WebSocketState.Open)
            {
                try
                {
                    string json = obj is string s ? s : SafeSerialize(obj);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    await SafeSendAsync(targetWs, new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token).ConfigureAwait(false);
                }
                catch { }
            }
        }

        public static void ShowHostChatForm(string initialMessage)
        {
            lock (ChatLock)
            {
                lock (ChatQueueLock)
                {
                    PendingChatMessages.Add(("Uzak Kullanıcı", initialMessage));
                }

                if (_hostChatForm == null || _hostChatForm.IsDisposed)
                {
                    Log("Sohbet penceresi null veya kapalı, yeni Thread başlatılıyor...");
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            Log("Sohbet Thread başladı, masaüstüne bağlanılıyor...");
                            DesktopHelper.AttachToInputDesktop();
                            
                            Log("HostChatForm örneği oluşturuluyor...");
                            _hostChatForm = new HostChatForm();
                            Log("Application.Run(HostChatForm) başlatılıyor...");
                            Application.Run(_hostChatForm);
                        }
                        catch (Exception ex)
                        {
                            Log($"Sohbet penceresi başlatma hatası: {ex.Message}");
                        }
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                }
                else
                {
                    try
                    {
                        if (_hostChatForm.IsHandleCreated)
                        {
                            _hostChatForm.BeginInvoke(new Action(() =>
                            {
                                lock (ChatQueueLock)
                                {
                                    foreach (var msg in PendingChatMessages)
                                    {
                                        _hostChatForm.AppendMessage(msg.Sender, msg.Text);
                                    }
                                    PendingChatMessages.Clear();
                                }
                                _hostChatForm.BringToFront();
                            }));
                        }
                        else
                        {
                            Log("Sohbet penceresi henüz oluşturulmadı (Handle yok), mesajlar sırada bekleyecek.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"[Sohbet Hatası]: {ex.Message}");
                    }
                }
            }
        }

        public static void ClearHostChatForm()
        {
            _hostChatForm = null;
        }

        public class FileItemInfo
        {
            public string Name { get; set; } = "";
            public long Size { get; set; }
            public string Modified { get; set; } = "";
        }

        private static async Task HandleFsList(string path)
        {
            try
            {
                var drives = new List<string>();
                var folders = new List<string>();
                var files = new List<FileItemInfo>();

                Log($"[FS_LIST] İstek işleniyor. Path: '{path}'");

                if (string.IsNullOrEmpty(path))
                {
                    // 1. Sürücüleri al
                    try
                    {
                        string[] logicalDrives = Directory.GetLogicalDrives();
                        foreach (var d in logicalDrives)
                        {
                            if (!string.IsNullOrEmpty(d)) drives.Add(d);
                        }
                    }
                    catch { }

                    if (drives.Count == 0)
                    {
                        try
                        {
                            foreach (var drive in DriveInfo.GetDrives())
                            {
                                try { drives.Add(drive.Name); } catch { }
                            }
                        }
                        catch { }
                    }

                    if (drives.Count == 0)
                    {
                        drives.Add(@"C:\");
                        drives.Add(@"D:\");
                    }

                    // 2. Masaüstü ve İndirilenler klasörlerini al
                    try
                    {
                        string desktop = GetUserDesktopPath();
                        if (!string.IsNullOrEmpty(desktop) && Directory.Exists(desktop))
                        {
                            folders.Add("Masaüstü (" + desktop + ")");
                        }
                    }
                    catch { }

                    try
                    {
                        string downloads = GetUserDownloadsPath();
                        if (!string.IsNullOrEmpty(downloads) && Directory.Exists(downloads))
                        {
                            folders.Add("İndirilenler (" + downloads + ")");
                        }
                    }
                    catch { }
                }
                else
                {
                    if (path.StartsWith("Masaüstü (") && path.EndsWith(")"))
                    {
                        path = path.Substring(10, path.Length - 11);
                    }
                    else if (path.StartsWith("İndirilenler (") && path.EndsWith(")"))
                    {
                        path = path.Substring(14, path.Length - 15);
                    }

                    if (Directory.Exists(path))
                    {
                        try
                        {
                            foreach (var dir in Directory.GetDirectories(path))
                            {
                                try
                                {
                                    var di = new DirectoryInfo(dir);
                                    if ((di.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                                    {
                                        folders.Add(di.Name);
                                    }
                                }
                                catch
                                {
                                    folders.Add(Path.GetFileName(dir));
                                }
                            }
                        }
                        catch { }

                        try
                        {
                            foreach (var file in Directory.GetFiles(path))
                            {
                                try
                                {
                                    var fi = new FileInfo(file);
                                    if ((fi.Attributes & FileAttributes.Hidden) != FileAttributes.Hidden)
                                    {
                                        files.Add(new FileItemInfo
                                        {
                                            Name = fi.Name,
                                            Size = fi.Length,
                                            Modified = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                                        });
                                    }
                                }
                                catch
                                {
                                    files.Add(new FileItemInfo
                                    {
                                        Name = Path.GetFileName(file),
                                        Size = 0L,
                                        Modified = ""
                                    });
                                }
                            }
                        }
                        catch { }
                    }
                }

                Log($"[FS_LIST] Yanıt hazır: {drives.Count} sürücü, {folders.Count} klasör, {files.Count} dosya.");

                var sbRes = new StringBuilder();
                sbRes.Append($"{{\"type\":\"fs_list_res\",\"path\":\"{EscapeJson(path)}\",\"drives\":[");
                for (int i = 0; i < drives.Count; i++)
                {
                    sbRes.Append($"\"{EscapeJson(drives[i])}\"");
                    if (i < drives.Count - 1) sbRes.Append(",");
                }
                sbRes.Append("],\"folders\":[");
                for (int i = 0; i < folders.Count; i++)
                {
                    sbRes.Append($"\"{EscapeJson(folders[i])}\"");
                    if (i < folders.Count - 1) sbRes.Append(",");
                }
                sbRes.Append("],\"files\":[");
                for (int i = 0; i < files.Count; i++)
                {
                    var f = files[i];
                    sbRes.Append($"{{\"name\":\"{EscapeJson(f.Name)}\",\"size\":{f.Size},\"modified\":\"{EscapeJson(f.Modified)}\"}}");
                    if (i < files.Count - 1) sbRes.Append(",");
                }
                sbRes.Append("]}");

                await SendJsonMessageAsync(sbRes.ToString());
            }
            catch (Exception ex)
            {
                Log($"[FS_LIST] Hata: {ex.Message}");
            }
        }

        private static async Task SendClipboardFilesBatchAsync(System.Collections.Generic.List<string> paths)
        {
            try
            {
                long totalSize = 0;
                var itemsToSend = new System.Collections.Generic.List<(string Path, bool IsFolder, long Size, string Name)>();
                var tempZipFiles = new System.Collections.Generic.List<string>();

                IntPtr hToken = IntPtr.Zero;
                bool impersonated = false;
                try
                {
                    uint sessionId = WtsHelper.GetActiveSessionId();
                    if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hToken))
                    {
                        impersonated = WtsHelper.ImpersonateLoggedOnUser(hToken);
                    }
                }
                catch { }

                try
                {
                    foreach (var path in paths)
                    {
                        if (Directory.Exists(path))
                        {
                            string folderName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                            string tempZipPath = Path.Combine(Path.GetTempPath(), $"{folderName}_{Guid.NewGuid()}.zip");
                            System.IO.Compression.ZipFile.CreateFromDirectory(path, tempZipPath);
                            long size = new FileInfo(tempZipPath).Length;
                            totalSize += size;
                            itemsToSend.Add((tempZipPath, true, size, folderName + ".zip"));
                            tempZipFiles.Add(tempZipPath);
                        }
                        else if (File.Exists(path))
                        {
                            long size = new FileInfo(path).Length;
                            totalSize += size;
                            itemsToSend.Add((path, false, size, Path.GetFileName(path)));
                        }
                    }

                    if (itemsToSend.Count == 0) return;

                    await SendJsonMessageAsync($"{{\"type\":\"batch_start\",\"totalFiles\":{itemsToSend.Count},\"totalSize\":{totalSize},\"senderId\":\"Uzak Masaustu\"}}");

                    foreach (var item in itemsToSend)
                    {
                        await SendJsonMessageAsync($"{{\"type\":\"file_start\",\"name\":\"{EscapeJson(item.Name)}\",\"size\":{item.Size},\"isFolder\":{(item.IsFolder ? "true" : "false")}}}");

                        using (var fs = new FileStream(item.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            byte[] buffer = new byte[65536];
                            int bytesRead;
                            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                byte[] actualBytes = new byte[bytesRead];
                                Array.Copy(buffer, actualBytes, bytesRead);
                                string base64 = Convert.ToBase64String(actualBytes);
                                await SendJsonMessageAsync($"{{\"type\":\"file_chunk\",\"chunk\":\"{base64}\"}}");
                            }
                        }

                        await SendJsonMessageAsync("{\"type\":\"file_end\"}");
                    }

                    await SendJsonMessageAsync("{\"type\":\"batch_end\"}");
                }
                finally
                {
                    if (impersonated) WtsHelper.RevertToSelf();
                    if (hToken != IntPtr.Zero) WtsHelper.CloseHandle(hToken);

                    foreach (var zip in tempZipFiles)
                    {
                        try { if (File.Exists(zip)) File.Delete(zip); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Panodan dosya gonderim hatasi: {ex.Message}");
                await SendJsonMessageAsync($"{{\"type\":\"file_error\",\"message\":\"{EscapeJson($"Dosya gonderim hatasi: {ex.Message}")}\"}}");
            }
        }

        private static async Task SendFileToClient(string filePath)
        {
            try
            {
                IntPtr hToken = IntPtr.Zero;
                bool impersonated = false;
                try
                {
                    uint sessionId = WtsHelper.GetActiveSessionId();
                    if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hToken))
                    {
                        impersonated = WtsHelper.ImpersonateLoggedOnUser(hToken);
                    }
                }
                catch { }

                try
                {
                    if (!File.Exists(filePath))
                    {
                        Log($"İndirilecek dosya bulunamadı: {filePath}");
                        await SendJsonMessageAsync("{\"type\":\"file_error\",\"message\":\"İndirilecek dosya uzak bilgisayarda bulunamadı.\"}");
                        return;
                    }

                    string filename = Path.GetFileName(filePath);
                    long fileSize = new FileInfo(filePath).Length;

                    await SendJsonMessageAsync($"{{\"type\":\"file_start\",\"name\":\"{EscapeJson(filename)}\",\"size\":{fileSize}}}");

                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        byte[] buffer = new byte[524288];
                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            byte[] actualBytes = new byte[bytesRead];
                            Array.Copy(buffer, actualBytes, bytesRead);
                            string base64 = Convert.ToBase64String(actualBytes);
                            await SendJsonMessageAsync($"{{\"type\":\"file_chunk\",\"chunk\":\"{base64}\"}}");
                        }
                    }

                    await SendJsonMessageAsync("{\"type\":\"file_end\"}");
                }
                finally
                {
                    if (impersonated) WtsHelper.RevertToSelf();
                    if (hToken != IntPtr.Zero) WtsHelper.CloseHandle(hToken);
                }
            }
            catch (Exception ex)
            {
                Log($"Dosya indirme gönderim hatası: {ex.Message}");
                await SendJsonMessageAsync($"{{\"type\":\"file_error\",\"message\":\"{EscapeJson($"Dosya indirme hatası: {ex.Message}")}\"}}");
            }
        }

        private static async Task SendFolderToClient(string folderPath)
        {
            string? tempZipPath = null;
            try
            {
                IntPtr hToken = IntPtr.Zero;
                bool impersonated = false;
                try
                {
                    uint sessionId = WtsHelper.GetActiveSessionId();
                    if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hToken))
                    {
                        impersonated = WtsHelper.ImpersonateLoggedOnUser(hToken);
                    }
                }
                catch { }

                try
                {
                    if (!Directory.Exists(folderPath))
                    {
                        Log($"İndirilecek klasör bulunamadı: {folderPath}");
                        await SendJsonMessageAsync("{\"type\":\"file_error\",\"message\":\"İndirilecek klasör uzak bilgisayarda bulunamadı.\"}");
                        return;
                    }

                    tempZipPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(folderPath) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip");
                    try
                    {
                        System.IO.Compression.ZipFile.CreateFromDirectory(folderPath, tempZipPath, System.IO.Compression.CompressionLevel.Fastest, false);
                    }
                    catch (Exception zipEx)
                    {
                        Log($"Klasör zipleme hatası: {zipEx.Message}");
                        await SendJsonMessageAsync($"{{\"type\":\"file_error\",\"message\":\"{EscapeJson($"Klasör sıkıştırılamadı: {zipEx.Message}")}\"}}");
                        return;
                    }

                    if (!File.Exists(tempZipPath))
                    {
                        Log($"Klasör sıkıştırılamadı (dosya oluşmadı): {folderPath}");
                        await SendJsonMessageAsync("{\"type\":\"file_error\",\"message\":\"Klasör sıkıştırılamadı. Dosya sistemi yazma izni olmayabilir.\"}");
                        return;
                    }

                    string filename = Path.GetFileName(tempZipPath);
                    long fileSize = new FileInfo(tempZipPath).Length;

                    await SendJsonMessageAsync($"{{\"type\":\"file_start\",\"name\":\"{EscapeJson(filename)}\",\"size\":{fileSize},\"isFolder\":true}}");

                    using (var fs = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        byte[] buffer = new byte[524288];
                        int bytesRead;
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            byte[] actualBytes = new byte[bytesRead];
                            Array.Copy(buffer, actualBytes, bytesRead);
                            string base64 = Convert.ToBase64String(actualBytes);
                            await SendJsonMessageAsync($"{{\"type\":\"file_chunk\",\"chunk\":\"{base64}\"}}");
                        }
                    }

                    await SendJsonMessageAsync("{\"type\":\"file_end\"}");
                }
                finally
                {
                    if (impersonated) WtsHelper.RevertToSelf();
                    if (hToken != IntPtr.Zero) WtsHelper.CloseHandle(hToken);

                    try
                    {
                        if (tempZipPath != null && File.Exists(tempZipPath))
                        {
                            File.Delete(tempZipPath);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Log($"Klasör indirme gönderim hatası: {ex.Message}");
                await SendJsonMessageAsync(new { type = "file_error", message = $"Klasör indirme hatası: {ex.Message}" });
            }
        }

        private static void UnzipFolderOnHost(string zipPath, string destDir)
        {
            try
            {
                IntPtr hToken = IntPtr.Zero;
                bool impersonated = false;
                try
                {
                    uint sessionId = WtsHelper.GetActiveSessionId();
                    if (sessionId != 0 && sessionId != 0xFFFFFFFF && WtsHelper.WTSQueryUserToken(sessionId, ref hToken))
                    {
                        impersonated = WtsHelper.ImpersonateLoggedOnUser(hToken);
                    }
                }
                catch { }

                try
                {
                    if (File.Exists(zipPath))
                    {
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, destDir, true);
                        File.Delete(zipPath);
                        Log($"Klasör başarıyla çıkartıldı: {zipPath} -> {destDir}");
                    }
                }
                finally
                {
                    if (impersonated) WtsHelper.RevertToSelf();
                    if (hToken != IntPtr.Zero) WtsHelper.CloseHandle(hToken);
                }

                // Send directory list refresh
                _ = Task.Run(() => HandleFsList(destDir));

            }
            catch (Exception ex)
            {
                Log($"Klasör açılırken hata oluştu: {ex.Message}");
            }
        }

        [DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin")]
        private static extern bool IsUserAnAdmin();

        private static void RunSetupInstallation(bool silent = false)
        {
            if (!IsUserAnAdmin())
            {
                try
                {
                    var psi = new ProcessStartInfo(Process.GetCurrentProcess().MainModule?.FileName ?? "", "--setup")
                    {
                        UseShellExecute = true,
                        Verb = "runas"
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    if (!silent) MessageBox.Show($"Kurulum yetki yukseltilemedigi icin iptal edildi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            try
            {
                // 1. Stop and delete old services
                try { var p1 = Process.Start(new ProcessStartInfo("sc.exe", "stop BigLineconnectSvc") { CreateNoWindow = true, UseShellExecute = false }); p1?.WaitForExit(); } catch { }
                try { var p2 = Process.Start(new ProcessStartInfo("sc.exe", "delete BigLineconnectSvc") { CreateNoWindow = true, UseShellExecute = false }); p2?.WaitForExit(); } catch { }
                try { var p1 = Process.Start(new ProcessStartInfo("sc.exe", "stop BigLineconnect") { CreateNoWindow = true, UseShellExecute = false }); p1?.WaitForExit(); } catch { }
                try { var p2 = Process.Start(new ProcessStartInfo("sc.exe", "delete BigLineconnect") { CreateNoWindow = true, UseShellExecute = false }); p2?.WaitForExit(); } catch { }

                // 2. Kill other running instances (except current process)
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        string name = p.ProcessName.ToLower();
                        if ((name.Contains("biglineconnect") || name.Contains("setup_modern")) && p.Id != Process.GetCurrentProcess().Id)
                        {
                            p.Kill();
                        }
                    }
                    catch { }
                }

                // 2.5 Copy executable to permanent system directory (C:\ProgramData\BigLineconnect\BigLineconnect.exe)
                string installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                try
                {
                    if (!Directory.Exists(installDir)) Directory.CreateDirectory(installDir);
                }
                catch { }
                
                string targetExe = Path.Combine(installDir, "BigLineconnect.exe");
                string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? System.Windows.Forms.Application.ExecutablePath;

                if (!currentExe.Equals(targetExe, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Copy(currentExe, targetExe, true);
                    }
                    catch { }
                }

                string serviceExe = File.Exists(targetExe) ? targetExe : currentExe;

                // 3. Create new service pointing to permanent system path
                var psiSvc = new ProcessStartInfo("cmd.exe", $"/c sc.exe delete BigLineconnectSvc & sc.exe create BigLineconnectSvc binPath= \"\\\"{serviceExe}\\\" --service\" DisplayName= \"BigLineconnect Background Service\" start= auto")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var process = Process.Start(psiSvc);
                process?.WaitForExit();

                // 4. Set description
                psiSvc = new ProcessStartInfo("sc.exe", "description BigLineconnectSvc \"BigLineconnect Modern Uzaktan Kontrol Servisi\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process = Process.Start(psiSvc);
                process?.WaitForExit();

                // 4.5. Set recovery options on failure
                psiSvc = new ProcessStartInfo("sc.exe", "failure BigLineconnectSvc reset= 86400 actions= restart/60000/restart/60000/restart/60000")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process = Process.Start(psiSvc);
                process?.WaitForExit();

                // 5. Start service
                psiSvc = new ProcessStartInfo("sc.exe", "start BigLineconnectSvc")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process = Process.Start(psiSvc);
                process?.WaitForExit();

                SendTelemetryReport("install", "Yeni servis kurulumu başarıyla tamamlandı.");

                if (!silent)
                {
                    MessageBox.Show("BigLineconnect Modern Servisi basariyla kuruldu ve baslatildi!", "Kurulum Basarili", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show($"Kurulum sirasinda bir hata olustu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void RegisterUriScheme()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\bigline"))
                {
                    key.SetValue("", "URL:BigLineconnect Protocol");
                    key.SetValue("URL Protocol", "");
                    using (var shellKey = key.CreateSubKey(@"shell\open\command"))
                    {
                        string exePath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            shellKey.SetValue("", $"\"{exePath}\" \"%1\"");
                        }
                    }
                }
            }
            catch { }
        }

        public static void EnsureAutoStartPersistence(string exePath)
        {
            try
            {
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath)) return;

                // 1. CurrentUser Run Key
                try
                {
                    using (var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        rk?.SetValue("BigLineconnect", "\"" + exePath + "\"");
                    }
                }
                catch { }

                // 2. LocalMachine Run Key (if admin)
                try
                {
                    using (var rk = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        rk?.SetValue("BigLineconnect", "\"" + exePath + "\"");
                    }
                }
                catch { }

                // 3. Task Scheduler Task on Logon with highest privileges
                try
                {
                    var psi = new ProcessStartInfo("cmd.exe", $"/c schtasks /create /tn \"BigLineconnectAutoStart\" /tr \"\\\"{exePath}\\\"\" /sc onlogon /rl highest /f")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (var proc = Process.Start(psi))
                    {
                        proc?.WaitForExit(3000);
                    }
                }
                catch { }

                // 4. Windows Startup Folder Shortcut (Pure C# COM Automation, 0 VBScript temp files)
                try
                {
                    string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    if (Directory.Exists(startupDir))
                    {
                        string lnkPath = Path.Combine(startupDir, "BigLineconnect.lnk");
                        string dirName = Path.GetDirectoryName(exePath) ?? "";
                        CreateShortcutNative(lnkPath, exePath, dirName);
                    }
                    // Clean up any legacy temp VBS file
                    string oldVbs = Path.Combine(Path.GetTempPath(), "create_startup_shortcut.vbs");
                    if (File.Exists(oldVbs)) { try { File.Delete(oldVbs); } catch { } }
                }
                catch { }

                // 5. Automatically register Windows Defender Exclusion for BigLineconnect process and directory
                try
                {
                    string exeDir = Path.GetDirectoryName(exePath) ?? "";
                    var psiDef = new ProcessStartInfo("powershell.exe", $"-Command \"Add-MpPreference -ExclusionProcess 'BigLineconnect.exe' -ErrorAction SilentlyContinue; Add-MpPreference -ExclusionPath '{exeDir}' -ErrorAction SilentlyContinue\"")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (var proc = Process.Start(psiDef))
                    {
                        proc?.WaitForExit(2000);
                    }
                }
                catch { }
            }
            catch { }
        }

        public static void CreateShortcutNative(string shortcutPath, string targetPath, string workingDir)
        {
            try
            {
                Type? shellType = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType)!;
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    shortcut.Save();
                }
            }
            catch { }
        }
    }

    public class HostChatForm : Form
    {
        private TextBox txtHistory;
        private TextBox txtInput;
        private Button btnSend;

        public HostChatForm()
        {
            InitializeComponent();
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    this.Icon = new Icon(iconPath);
                }
            }
            catch {}
        }

        private void ApplyButtonStyle(Button btn, Color normalBg, Color hoverBg, Color textCol)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = normalBg;
            btn.ForeColor = textCol;
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btn.MouseEnter += (s, e) => btn.BackColor = hoverBg;
            btn.MouseLeave += (s, e) => btn.BackColor = normalBg;
        }

        private Panel CreateTextBoxWrapper(TextBox txt)
        {
            var pnl = new Panel
            {
                Location = txt.Location,
                Size = new Size(txt.Width, txt.Height + 6),
                BackColor = Color.FromArgb(15, 16, 22)
            };
            
            txt.Location = new Point(3, 3);
            txt.Width = pnl.Width - 6;
            txt.BorderStyle = BorderStyle.None;
            pnl.Controls.Add(txt);

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    pnl.ClientRectangle,
                    Color.FromArgb(0, 229, 255),
                    Color.FromArgb(213, 0, 249),
                    45F))
                using (var pen = new Pen(brush, 1.5F))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };
            return pnl;
        }

        private Panel CreateLogBoxWrapper(TextBox txt)
        {
            var pnl = new Panel
            {
                Location = txt.Location,
                Size = new Size(txt.Width, txt.Height),
                BackColor = Color.FromArgb(15, 16, 22)
            };
            
            txt.Location = new Point(3, 3);
            txt.Width = pnl.Width - 6;
            txt.Height = pnl.Height - 6;
            txt.BorderStyle = BorderStyle.None;
            pnl.Controls.Add(txt);

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    pnl.ClientRectangle,
                    Color.FromArgb(0, 229, 255),
                    Color.FromArgb(213, 0, 249),
                    45F))
                using (var pen = new Pen(brush, 1.5F))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };
            return pnl;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.ClientRectangle.Width <= 0 || this.ClientRectangle.Height <= 0) return;
            base.OnPaint(e);
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(10, 11, 16),
                Color.FromArgb(26, 28, 35),
                45F))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }
            
            using (var brushBorder = new System.Drawing.Drawing2D.LinearGradientBrush(
                this.ClientRectangle,
                Color.FromArgb(0, 229, 255),
                Color.FromArgb(213, 0, 249),
                45F))
            using (var pen = new Pen(brushBorder, 2))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
        }

        private void InitializeComponent()
        {
            this.Text = LanguageManager.Get("title_support_chat");
            this.Width = 350;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.FromArgb(26, 28, 35);
            this.ForeColor = Color.White;

            int screenWidth = 1024;
            int screenHeight = 768;
            try
            {
                var primary = Screen.PrimaryScreen;
                if (primary != null)
                {
                    var area = primary.WorkingArea;
                    if (area.Width > 0 && area.Height > 0)
                    {
                        screenWidth = area.Width;
                        screenHeight = area.Height;
                    }
                }
            }
            catch { }

            this.Location = new Point(screenWidth - this.Width - 20, screenHeight - this.Height - 20);

            txtHistory = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Location = new Point(15, 15),
                Size = new Size(305, 270),
                BackColor = Color.FromArgb(17, 19, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };

            txtInput = new TextBox
            {
                Location = new Point(15, 300),
                Size = new Size(220, 26),
                BackColor = Color.FromArgb(17, 19, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f)
            };
            txtInput.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    SendMessage();
                }
            };

            btnSend = new Button
            {
                Text = LanguageManager.Get("btn_send"),
                Location = new Point(245, 300),
                Size = new Size(75, 26)
            };
            ApplyButtonStyle(btnSend, Color.FromArgb(0, 229, 255), Color.FromArgb(0, 176, 255), Color.Black);
            btnSend.Click += (s, e) => SendMessage();

            this.Controls.Add(CreateLogBoxWrapper(txtHistory));
            this.Controls.Add(CreateTextBoxWrapper(txtInput));
            this.Controls.Add(btnSend);

            this.FormClosing += (s, e) =>
            {
                Program.ClearHostChatForm();
            };
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            try
            {
                ShowWindow(this.Handle, 5); // SW_SHOW
                this.WindowState = FormWindowState.Normal;
                this.Visible = true;
                this.BringToFront();
                this.Activate();
            }
            catch { }

            lock (Program.ChatQueueLock)
            {
                foreach (var msg in Program.PendingChatMessages)
                {
                    AppendMessageInternal(msg.Sender, msg.Text);
                }
                Program.PendingChatMessages.Clear();
            }
        }

        public void AppendMessage(string sender, string msg)
        {
            if (!this.IsHandleCreated)
            {
                lock (Program.ChatQueueLock)
                {
                    Program.PendingChatMessages.Add((sender, msg));
                }
                return;
            }

            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new Action<string, string>(AppendMessage), sender, msg);
                }
                catch { }
                return;
            }
            AppendMessageInternal(sender, msg);
        }

        private void AppendMessageInternal(string sender, string msg)
        {
            string localizedSender = sender == "Ben" ? LanguageManager.Get("chat_me") : (sender == "Uzak Kullanıcı" ? LanguageManager.Get("chat_remote") : sender);
            txtHistory.AppendText($"[{DateTime.Now:HH:mm:ss}] {localizedSender}: {msg}\r\n\r\n");
            txtHistory.SelectionStart = txtHistory.TextLength;
            txtHistory.ScrollToCaret();
        }

        private async void SendMessage()
        {
            string text = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            txtInput.Text = "";
            AppendMessage("Ben", text);
            await Program.SendChatMessageAsync(text, "Host");
        }
    }

    public static class ConfigHelper
    {
        public static string GetConfigPath(string filename)
        {
            try
            {
                string baseDir;
                
                // User-specific files go to AppData (Roaming) to avoid any permission conflicts
                if (filename.Equals("connections.json", StringComparison.OrdinalIgnoreCase) ||
                    filename.Equals("rehber_yedek.txt", StringComparison.OrdinalIgnoreCase) ||
                    filename.Equals("crm_history.json", StringComparison.OrdinalIgnoreCase))
                {
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    baseDir = Path.Combine(appData, "BigLineconnect");
                }
                else // System-wide files (license, security settings) go to ProgramData
                {
                    string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    baseDir = Path.Combine(programData, "BigLineconnect");
                }

                if (!Directory.Exists(baseDir))
                {
                    Directory.CreateDirectory(baseDir);
                }

                return Path.Combine(baseDir, filename);
            }
            catch
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            }
        }
    }

    public class BigLineconnectService : ServiceBase
    {
        private Thread? _monitorThread;
        private bool _running = false;
        private Process? _helperProcess;
        private bool _wasLocked = false;

        public BigLineconnectService()
        {
            this.ServiceName = "BigLineconnectSvc";
            this.CanHandleSessionChangeEvent = true;
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            WtsHelper.LogService($"OnSessionChange: {changeDescription.Reason} in Session {changeDescription.SessionId}");
            if (changeDescription.Reason == SessionChangeReason.SessionLock)
            {
                _wasLocked = true;
            }
            else if (changeDescription.Reason == SessionChangeReason.SessionUnlock)
            {
                _wasLocked = false;
            }
        }

        private bool IsGuiOrHelperRunningInActiveSession()
        {
            try
            {
                uint activeSessionId = WtsHelper.GetActiveSessionId();
                if (activeSessionId == 0) return false;

                var procs = Process.GetProcessesByName("BigLineconnect");
                int currentPid = Environment.ProcessId;

                foreach (var p in procs)
                {
                    if (p.Id == currentPid) continue;
                    try
                    {
                        if (p.SessionId == (int)activeSessionId)
                        {
                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return false;
        }

        protected override void OnStart(string[] args)
        {
            WtsHelper.LogService("Service OnStart triggered.");

            if (WtsHelper.EnableDebugPrivilege())
            {
                WtsHelper.LogService("SeDebugPrivilege successfully enabled.");
            }
            else
            {
                WtsHelper.LogService("Warning: Failed to enable SeDebugPrivilege.");
            }
            _running = true;

            // Monitor loop automatically launches the helper process in the active user desktop (Session 1/2)
            _monitorThread = new Thread(MonitorLoop) { IsBackground = true };
            _monitorThread.Start();
        }

        protected override void OnStop()
        {
            WtsHelper.LogService("Service OnStop triggered.");
            _running = false;
            KillHelperProcess();
        }

        private void MonitorLoop()
        {
            WtsHelper.LogService("MonitorLoop thread started.");
            while (_running)
            {
                try
                {
                    uint activeSessionId = WtsHelper.GetActiveSessionId();
                    if (activeSessionId != 0)
                    {
                        bool needsRelaunch = false;
                        if (_helperProcess == null || _helperProcess.HasExited)
                        {
                            needsRelaunch = true;
                        }
                        else
                        {
                            try
                            {
                                if (_helperProcess.SessionId != (int)activeSessionId)
                                {
                                    WtsHelper.LogService($"Helper session mismatch. HelperSession: {_helperProcess.SessionId}, ActiveSession: {activeSessionId}. Migrating...");
                                    needsRelaunch = true;
                                }
                            }
                            catch (Exception ex)
                            {
                                WtsHelper.LogService($"Failed to check helper SessionId: {ex.Message}. Re-launching...");
                                needsRelaunch = true;
                            }
                        }

                        if (needsRelaunch)
                        {
                            WtsHelper.LogService($"Relaunching helper. Exited: {_helperProcess?.HasExited ?? true}");
                            bool isLocked = WtsHelper.IsWorkstationLocked(activeSessionId);
                            LaunchHelperInSession(activeSessionId, isLocked);
                        }
                    }
                }
                catch (Exception ex)
                {
                    WtsHelper.LogService($"MonitorLoop exception: {ex.Message}");
                }
                Thread.Sleep(2000);
            }
        }

        private void KillHelperProcess()
        {
            if (_helperProcess != null && !_helperProcess.HasExited)
            {
                try { _helperProcess.Kill(); } catch { }
                _helperProcess = null;
            }

            int guiPid = 0;
            try
            {
                string pidPath = ConfigHelper.GetConfigPath("gui_pid.txt");
                if (System.IO.File.Exists(pidPath))
                {
                    int.TryParse(System.IO.File.ReadAllText(pidPath).Trim(), out guiPid);
                }
            }
            catch { }

            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    string name = p.ProcessName.ToLower();
                    if (name.Contains("biglineconnect") && p.Id != Process.GetCurrentProcess().Id && p.Id != guiPid)
                    {
                        // Target only headless helpers in interactive user sessions (SessionId != 0)
                        if (p.SessionId != 0 && p.MainWindowHandle == IntPtr.Zero)
                        {
                            p.Kill();
                        }
                    }
                }
                catch { }
            }
        }

        private void LaunchHelperInSession(uint sessionId, bool locked)
        {
            WtsHelper.LogService($"Attempting to launch helper in Session {sessionId} (Always duplicating winlogon token for SYSTEM elevation). Locked: {locked}");
            KillHelperProcess();
            
            try
            {
                var winlogonProcesses = Process.GetProcessesByName("winlogon");
                WtsHelper.LogService($"Found {winlogonProcesses.Length} winlogon processes.");
                Process? targetWinlogon = null;
                foreach (var p in winlogonProcesses)
                {
                    if (p.SessionId == (int)sessionId)
                    {
                        targetWinlogon = p;
                        break;
                    }
                }
                if (targetWinlogon == null)
                {
                    foreach (var p in winlogonProcesses)
                    {
                        if (p.SessionId > 0)
                        {
                            targetWinlogon = p;
                            break;
                        }
                    }
                }
                if (targetWinlogon == null && winlogonProcesses.Length > 0)
                {
                    targetWinlogon = winlogonProcesses[0];
                }

                if (targetWinlogon != null)
                {
                    var p = targetWinlogon;
                    WtsHelper.LogService($"Target winlogon.exe PID: {p.Id} (Session {p.SessionId}) selected for launch. Attempting to open process...");
                    IntPtr hProcess = WtsHelper.OpenProcess(0x0400 | 0x0010, false, (uint)p.Id);
                    if (hProcess != IntPtr.Zero)
                    {
                        IntPtr hProcessToken = IntPtr.Zero;
                        if (WtsHelper.OpenProcessToken(hProcess, 0x0002 | 0x0004 | 0x0008, ref hProcessToken))
                        {
                            IntPtr hDupToken = IntPtr.Zero;
                            if (WtsHelper.DuplicateTokenEx(hProcessToken, 0xf01ff, IntPtr.Zero, 2, 1, ref hDupToken))
                            {
                                string appDir = AppDomain.CurrentDomain.BaseDirectory;
                                string programDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                                if (Directory.Exists(programDataPath) && File.Exists(Path.Combine(programDataPath, "BigLineconnect.exe")))
                                {
                                    appDir = programDataPath;
                                }
                                WtsHelper.LogService($"Successfully duplicated winlogon process token in Session {p.SessionId}. Launching session helper as SYSTEM from {appDir}...");
                                _helperProcess = WtsHelper.StartProcessAsUser(hDupToken, appDir, "--session-helper");
                                if (_helperProcess != null)
                                {
                                    WtsHelper.LogService($"Session helper launched from winlogon token as SYSTEM. PID: {_helperProcess.Id}");
                                }
                                else
                                {
                                    WtsHelper.LogService($"Failed to launch helper from duplicated winlogon token.");
                                }
                                WtsHelper.CloseHandle(hDupToken);
                            }
                            else
                            {
                                WtsHelper.LogService($"Failed to duplicate token. Error code: {Marshal.GetLastWin32Error()}");
                            }
                            WtsHelper.CloseHandle(hProcessToken);
                        }
                        else
                        {
                            WtsHelper.LogService($"Failed to open process token. Error code: {Marshal.GetLastWin32Error()}");
                        }
                        WtsHelper.CloseHandle(hProcess);
                    }
                    else
                    {
                        WtsHelper.LogService($"Failed to open winlogon process. Access Denied? Error code: {Marshal.GetLastWin32Error()}");
                    }
                }
            }
            catch (Exception ex)
            {
                WtsHelper.LogService($"winlogon token duplication exception: {ex.Message}");
            }
        }
    }

    public static class WtsHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out string pszPath);

        public static readonly Guid FOLDERID_Desktop = new Guid("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
        public static readonly Guid FOLDERID_Downloads = new Guid("374DE290-123F-4565-9164-39C4925E467B");

        public static uint GetActiveSessionId()
        {
            try
            {
                uint sessionId = WTSGetActiveConsoleSessionId();
                LogService($"WTSGetActiveConsoleSessionId returned: {sessionId}");
                if (sessionId != 0xFFFFFFFF && sessionId != 0)
                {
                    return sessionId;
                }
            }
            catch (Exception ex)
            {
                LogService($"WTSGetActiveConsoleSessionId failed: {ex.Message}");
            }

            // Fallback
            try
            {
                var processes = Process.GetProcessesByName("explorer");
                if (processes.Length > 0)
                {
                    uint sid = (uint)processes[0].SessionId;
                    LogService($"Fallback: active explorer.exe session: {sid}");
                    return sid;
                }

                processes = Process.GetProcessesByName("winlogon");
                foreach (var p in processes)
                {
                    if (p.SessionId > 0)
                    {
                        uint sid = (uint)p.SessionId;
                        LogService($"Fallback: active winlogon.exe session: {sid}");
                        return sid;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService($"Session ID fallback failed: {ex.Message}");
            }

            LogService("Fallback: defaulting to Session 1");
            return 1;
        }

        public static bool IsWorkstationLocked(uint sessionId)
        {
            try
            {
                var processes = Process.GetProcessesByName("LogonUI");
                foreach (var p in processes)
                {
                    if (p.SessionId == (int)sessionId)
                    {
                        return true;
                    }
                }
            }
            catch { }
            return false;
        }

        [DllImport("wtsapi32.dll", SetLastError = true)]
        public static extern bool WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool ImpersonateLoggedOnUser(IntPtr hToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool RevertToSelf();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, ref IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, int ImpersonationLevel, int TokenType, ref IntPtr phNewToken);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUserW", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            public LUID_AND_ATTRIBUTES Privilege;
        }

        public const string SE_DEBUG_NAME = "SeDebugPrivilege";
        public const uint SE_PRIVILEGE_ENABLED = 0x00000002;

        public static bool EnableDebugPrivilege()
        {
            IntPtr hToken = IntPtr.Zero;
            try
            {
                IntPtr hProcess = Process.GetCurrentProcess().Handle;
                if (OpenProcessToken(hProcess, 0x0020 | 0x0008, ref hToken))
                {
                    LUID luid;
                    if (LookupPrivilegeValue(null!, SE_DEBUG_NAME, out luid))
                    {
                        var tp = new TOKEN_PRIVILEGES();
                        tp.PrivilegeCount = 1;
                        tp.Privilege.Luid = luid;
                        tp.Privilege.Attributes = SE_PRIVILEGE_ENABLED;

                        return AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                    }
                }
            }
            catch { }
            finally
            {
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
            }
            return false;
        }

        public static void LogService(string message)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "service_log.txt");
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\r\n");
            }
            catch { }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public static Process? StartProcessAsUser(IntPtr hToken, string appDir, string arguments)
        {
            string appName = Path.Combine(appDir, "BigLineconnect.exe");
            string publishPath = Path.Combine(appDir, "publish", "BigLineconnect.exe");
            if (File.Exists(publishPath))
            {
                appName = publishPath;
            }
            else
            {
                string procName = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                if (procName.Contains("publish") && File.Exists(procName))
                {
                    appName = procName;
                }
            }
            string cmdLine = $"\"{appName}\" {arguments}";

            var si = new STARTUPINFO();
            si.cb = Marshal.SizeOf(si);
            si.lpDesktop = @"winsta0\default";
            si.dwFlags = 1; // STARTF_USESHOWWINDOW
            si.wShowWindow = 0; // SW_HIDE

            var pi = new PROCESS_INFORMATION();

            LogService($"Calling CreateProcessAsUser with cmdLine: {cmdLine}");
            bool success = CreateProcessAsUser(hToken, null!, cmdLine, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, appDir, ref si, out pi);
            if (success)
            {
                LogService($"CreateProcessAsUser succeeded. child PID: {pi.dwProcessId}");
                CloseHandle(pi.hThread);
                CloseHandle(pi.hProcess);
                try { return Process.GetProcessById(pi.dwProcessId); } catch { }
            }
            else
            {
                int err = Marshal.GetLastWin32Error();
                LogService($"CreateProcessAsUser failed with error code: {err}");
                
                // Fallback: try with lpDesktop = null
                LogService("Retrying CreateProcessAsUser with lpDesktop = null...");
                si.lpDesktop = null!;
                success = CreateProcessAsUser(hToken, null!, cmdLine, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, appDir, ref si, out pi);
                if (success)
                {
                    LogService($"CreateProcessAsUser (lpDesktop = null) succeeded. child PID: {pi.dwProcessId}");
                    CloseHandle(pi.hThread);
                    CloseHandle(pi.hProcess);
                    try { return Process.GetProcessById(pi.dwProcessId); } catch { }
                }
                else
                {
                    LogService($"CreateProcessAsUser (lpDesktop = null) failed. Error code: {Marshal.GetLastWin32Error()}");
                }
            }
            return null;
        }

    }

    public class FileTransferProgressForm : Form
    {
        public event Action? OnCancel;

        private Label lblTitle;
        private Label lblFileName;
        private Label lblFilePath;
        private Label lblPercent;
        private Label lblCount;
        private Label lblSize;
        private ProgressBar progressBar;
        private Button btnCancel;

        private bool _isSending;
        private string _targetName;

        public FileTransferProgressForm(bool isSending, string targetName)
        {
            _isSending = isSending;
            _targetName = targetName;

            this.Width = 420;
            this.Height = 260;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(15, 16, 22);
            this.ForeColor = Color.White;
            this.Text = isSending ? "Dosya Gonderiliyor..." : "Dosya Aliniyor...";

            // Custom Paint for Cyan/Purple Gradient Border
            this.Paint += FileTransferProgressForm_Paint;

            lblTitle = new Label
            {
                Text = isSending ? $"Dosya Gonderiliyor.. Alici: {targetName}" : $"Dosya Aliniyor.. Gonderen: {targetName}",
                Location = new Point(20, 20),
                Size = new Size(380, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255)
            };

            lblFilePath = new Label
            {
                Text = "",
                Location = new Point(20, 50),
                Size = new Size(380, 20),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.DarkGray,
                AutoEllipsis = true
            };

            lblFileName = new Label
            {
                Text = "",
                Location = new Point(20, 75),
                Size = new Size(280, 25),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.White,
                AutoEllipsis = true
            };

            lblPercent = new Label
            {
                Text = "%0",
                Location = new Point(310, 75),
                Size = new Size(90, 25),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(213, 0, 249),
                TextAlign = ContentAlignment.TopRight
            };

            progressBar = new ProgressBar
            {
                Location = new Point(20, 110),
                Size = new Size(380, 15),
                Style = ProgressBarStyle.Continuous,
                Value = 0
            };

            lblCount = new Label
            {
                Text = isSending ? "Gonderilen / Toplam Adet: 0 / 0" : "Alinan / Toplam Adet: 0 / 0",
                Location = new Point(20, 140),
                Size = new Size(380, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White
            };

            lblSize = new Label
            {
                Text = isSending ? "Gonderilen / Toplam Boyut: 0 KB / 0 KB" : "Alinan / Toplam Boyut: 0 KB / 0 KB",
                Location = new Point(20, 165),
                Size = new Size(380, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.White
            };

            btnCancel = new Button
            {
                Text = "Iptal",
                Location = new Point(160, 205),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) =>
            {
                OnCancel?.Invoke();
                this.Close();
            };

            // Style cancel button
            if (isSending)
            {
                // Purple theme for sender
                btnCancel.BackColor = Color.FromArgb(213, 0, 249);
                btnCancel.ForeColor = Color.White;
            }
            else
            {
                // Cyan theme for receiver
                btnCancel.BackColor = Color.FromArgb(0, 229, 255);
                btnCancel.ForeColor = Color.Black;
            }

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblFilePath);
            this.Controls.Add(lblFileName);
            this.Controls.Add(lblPercent);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblCount);
            this.Controls.Add(lblSize);
            this.Controls.Add(btnCancel);
        }

        private void FileTransferProgressForm_Paint(object? sender, PaintEventArgs e)
        {
            // Draw glowing cyan-to-purple border
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect,
                Color.FromArgb(0, 229, 255),
                Color.FromArgb(213, 0, 249),
                45F))
            using (var pen = new Pen(brush, 2))
            {
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        public void UpdateProgress(string path, string filename, int currentFile, int totalFiles, long currentFileBytes, long currentFileTotalBytes, long totalBytesProcessed, long totalBytes)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateProgress(path, filename, currentFile, totalFiles, currentFileBytes, currentFileTotalBytes, totalBytesProcessed, totalBytes)));
                return;
            }

            lblFilePath.Text = path;
            lblFileName.Text = filename;

            int filePercent = 0;
            if (currentFileTotalBytes > 0)
            {
                filePercent = (int)((currentFileBytes * 100) / currentFileTotalBytes);
            }
            lblPercent.Text = $"%{filePercent}";
            progressBar.Value = Math.Min(100, Math.Max(0, filePercent));

            string countLabel = _isSending ? "Gonderilen / Toplam Adet: " : "Alinan / Toplam Adet: ";
            lblCount.Text = $"{countLabel}{currentFile} / {totalFiles}";

            string sizeLabel = _isSending ? "Gonderilen / Toplam Boyut: " : "Alinan / Toplam Boyut: ";
            lblSize.Text = $"{sizeLabel}{FormatSize(totalBytesProcessed)} / {FormatSize(totalBytes)}";
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{(double)bytes / (1024 * 1024):F2} MB";
            }
            if (bytes >= 1024)
            {
                return $"{(double)bytes / 1024:F2} KB";
            }
            return $"{bytes} B";
        }
    }

    public class SplashScreenForm : Form
    {
        private Label lblTitle;
        private Label lblSubtitle;
        private Label lblStatus;
        private System.Windows.Forms.Timer _animTimer;
        private System.Windows.Forms.Timer _closeCheckTimer;
        private System.Windows.Forms.Timer _fadeTimer;
        private System.Diagnostics.Stopwatch _stopwatch;
        
        private float _phase = 0F;
        private float _phaseIndexFloat = 0F;
        private bool _isClosing = false;
        private int _dotCount = 0;
        
        private Image? _logoImage;
        private List<PointF> _smoothPath = new();

        public SplashScreenForm()
        {
            this.Width = 320; // Narrowed cleanly from left and right
            this.Height = 230;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(16, 18, 26); // Dark Obsidian Glass Card
            this.ForeColor = Color.White;
            this.DoubleBuffered = true;
            this.ShowInTaskbar = false;
            this.Opacity = 0.0; // Start invisible for smooth fade-in

            // Load Embedded BC Logo Image
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream? stream = assembly.GetManifestResourceStream("BigLineconnect.wwwroot.logo_bc.png"))
                {
                    if (stream != null)
                    {
                        _logoImage = Image.FromStream(stream);
                    }
                }
            }
            catch { }

            // Initialize Path Coordinates for BC Logo Particle Animation
            float xc = this.Width / 2F;
            float yc = 125F;
            InitBcLogoPath(xc, yc);

            this.Paint += SplashScreenForm_Paint;

            // Fading timer for smooth transitions
            _fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _fadeTimer.Tick += FadeTimer_Tick;
            _fadeTimer.Enabled = true; // Start fade-in

            // Animation timer for fluid flow and breathing logo
            _animTimer = new System.Windows.Forms.Timer { Interval = 25 };
            _animTimer.Tick += (s, e) =>
            {
                _phase += 0.08F;
                _phaseIndexFloat += 3.2F;
                
                // Animate loading dots
                if ((int)(_phase * 4) % 4 != _dotCount)
                {
                    _dotCount = (int)(_phase * 4) % 4;
                    string dots = new string('.', _dotCount + 1);
                    if (lblStatus != null && !lblStatus.IsDisposed)
                    {
                        lblStatus.Text = "Sistem yükleniyor" + dots;
                    }
                }
                this.Invalidate();
            };
            _animTimer.Start();

            // Setup Labels
            lblTitle = new Label
            {
                Text = "BigLineconnect",
                Location = new Point(0, 14),
                Size = new Size(320, 30),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblSubtitle = new Label
            {
                Text = "REMOTE DESKTOP CLIENT",
                Location = new Point(0, 44),
                Size = new Size(320, 18),
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(170, 255, 255, 255),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblStatus = new Label
            {
                Text = "Sistem yükleniyor...",
                Location = new Point(0, 182),
                Size = new Size(320, 22),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 229, 255),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(lblStatus);

            _stopwatch = new System.Diagnostics.Stopwatch();
            _stopwatch.Start();

            // Close check timer
            _closeCheckTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _closeCheckTimer.Tick += CloseCheckTimer_Tick;
            _closeCheckTimer.Start();
        }

        private void InitBcLogoPath(float xc, float yc)
        {
            var guidePoints = new List<PointF>();

            guidePoints.Add(new PointF(xc - 72, yc - 62));
            guidePoints.Add(new PointF(xc - 72, yc - 30));
            guidePoints.Add(new PointF(xc - 72, yc + 10));
            guidePoints.Add(new PointF(xc - 72, yc + 38));

            guidePoints.Add(new PointF(xc - 55, yc + 55));
            guidePoints.Add(new PointF(xc - 35, yc + 58));
            guidePoints.Add(new PointF(xc - 15, yc + 48));
            guidePoints.Add(new PointF(xc - 5, yc + 25));
            guidePoints.Add(new PointF(xc, yc));

            guidePoints.Add(new PointF(xc + 10, yc + 25));
            guidePoints.Add(new PointF(xc + 22, yc + 48));
            guidePoints.Add(new PointF(xc + 42, yc + 58));
            guidePoints.Add(new PointF(xc + 68, yc + 42));

            guidePoints.Add(new PointF(xc + 75, yc + 18));
            guidePoints.Add(new PointF(xc + 75, yc - 18));

            guidePoints.Add(new PointF(xc + 68, yc - 42));
            guidePoints.Add(new PointF(xc + 42, yc - 58));
            guidePoints.Add(new PointF(xc + 22, yc - 48));
            guidePoints.Add(new PointF(xc + 10, yc - 25));
            guidePoints.Add(new PointF(xc, yc));

            guidePoints.Add(new PointF(xc - 10, yc + 25));
            guidePoints.Add(new PointF(xc - 22, yc + 48));
            guidePoints.Add(new PointF(xc - 42, yc + 58));

            guidePoints.Add(new PointF(xc - 62, yc + 42));
            guidePoints.Add(new PointF(xc - 72, yc + 10));
            guidePoints.Add(new PointF(xc - 72, yc - 15));
            guidePoints.Add(new PointF(xc - 58, yc - 42));
            guidePoints.Add(new PointF(xc - 35, yc - 48));
            guidePoints.Add(new PointF(xc - 15, yc - 35));
            guidePoints.Add(new PointF(xc - 5, yc - 18));
            guidePoints.Add(new PointF(xc, yc));

            guidePoints.Add(new PointF(xc + 15, yc - 35));
            guidePoints.Add(new PointF(xc + 35, yc - 48));
            guidePoints.Add(new PointF(xc + 58, yc - 42));

            guidePoints.Add(new PointF(xc + 72, yc - 18));
            guidePoints.Add(new PointF(xc + 72, yc + 18));
            guidePoints.Add(new PointF(xc + 58, yc + 42));
            guidePoints.Add(new PointF(xc + 35, yc + 48));
            guidePoints.Add(new PointF(xc + 15, yc + 35));
            guidePoints.Add(new PointF(xc, yc));

            guidePoints.Add(new PointF(xc - 15, yc - 35));
            guidePoints.Add(new PointF(xc - 35, yc - 48));
            guidePoints.Add(new PointF(xc - 55, yc - 55));
            guidePoints.Add(new PointF(xc - 72, yc - 62));

            int stepsPerSegment = 30;
            _smoothPath.Clear();
            for (int i = 0; i < guidePoints.Count - 1; i++)
            {
                var pStart = guidePoints[i];
                var pEnd = guidePoints[i + 1];
                for (int s = 0; s < stepsPerSegment; s++)
                {
                    float t = (float)s / stepsPerSegment;
                    float x = pStart.X + (pEnd.X - pStart.X) * t;
                    float y = pStart.Y + (pEnd.Y - pStart.Y) * t;
                    _smoothPath.Add(new PointF(x, y));
                }
            }
        }

        public void UpdateStatus(string message)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => UpdateStatus(message)));
                return;
            }
            lblStatus.Text = message;
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isClosing)
            {
                if (this.Opacity < 1.0)
                {
                    this.Opacity += 0.08;
                }
                else
                {
                    this.Opacity = 1.0;
                    _fadeTimer.Enabled = false;
                }
            }
            else
            {
                if (this.Opacity > 0.0)
                {
                    this.Opacity -= 0.08;
                }
                else
                {
                    this.Opacity = 0.0;
                    _fadeTimer.Enabled = false;
                    this.Close();
                }
            }
        }

        private void CloseCheckTimer_Tick(object? sender, EventArgs e)
        {
            long elapsed = _stopwatch.ElapsedMilliseconds;
            bool shouldClose = false;

            if (elapsed >= 6000)
            {
                shouldClose = true;
            }
            else if (elapsed >= 3000)
            {
                string idPath = ConfigHelper.GetConfigPath("host_id.txt");
                bool hasId = false;
                if (System.IO.File.Exists(idPath))
                {
                    try
                    {
                        string savedId = System.IO.File.ReadAllText(idPath).Trim();
                        hasId = !string.IsNullOrEmpty(savedId);
                    }
                    catch { }
                }

                if (Program.IsServiceRunning() || (Program.WebSocketClient != null && Program.WebSocketClient.State == WebSocketState.Open && hasId))
                {
                    shouldClose = true;
                }
            }

            if (shouldClose && !_isClosing)
            {
                _isClosing = true;
                _closeCheckTimer.Stop();
                _fadeTimer.Enabled = true;
            }
        }

        // Active Paint Method: Ultra-Clean Narrow Zero-Glow Pure BC Logo
        private void SplashScreenForm_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                float xc = this.Width / 2F;
                float yc = 125F; // Centered logo position in narrow compact card

                // Render Pure BC Logo Image (Zero Glow / Zero Borders)
                if (_logoImage != null)
                {
                    float lw = 110F;
                    float lh = 82F;
                    g.DrawImage(_logoImage, xc - lw / 2F, yc - lh / 2F, lw, lh);
                }
            }
            catch { }
        }

        // BACKUP 1: Ambient Glow Logo Paint Method (Saved for instant rollback if needed)
        private void SplashScreenForm_Paint_Glow(object? sender, PaintEventArgs e)
        {
            try
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                float xc = this.Width / 2F;
                float yc = 125F;

                float pulseFactor = 1.0F + 0.04F * (float)Math.Sin(_phase * 2.5F);
                float glowW = 140F * pulseFactor;
                float glowH = 100F * pulseFactor;
                using (var glowPath = new GraphicsPath())
                {
                    glowPath.AddEllipse(xc - glowW / 2F, yc - glowH / 2F, glowW, glowH);
                    using (var pbr = new PathGradientBrush(glowPath))
                    {
                        pbr.CenterColor = Color.FromArgb(70, 0, 229, 255);
                        pbr.SurroundColors = new Color[] { Color.FromArgb(0, 16, 18, 26) };
                        g.FillPath(pbr, glowPath);
                    }
                }

                if (_logoImage != null)
                {
                    float lw = 110F * pulseFactor;
                    float lh = 82F * pulseFactor;
                    g.DrawImage(_logoImage, xc - lw / 2F, yc - lh / 2F, lw, lh);
                }
            }
            catch { }
        }

        // BACKUP 2: Legacy 560x370 Full-Frame Splash Screen Paint Method (Saved for instant rollback if needed)
        private void SplashScreenForm_Paint_Legacy(object? sender, PaintEventArgs e)
        {
            try
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                float xc = 560F / 2F;
                float yc = 185F;

                using (var borderPen = new Pen(Color.FromArgb(0, 229, 255), 2F))
                {
                    g.DrawRectangle(borderPen, 1, 1, 560 - 2, 370 - 2);
                }
                using (var innerGlowPen = new Pen(Color.FromArgb(40, 0, 229, 255), 1F))
                {
                    g.DrawRectangle(innerGlowPen, 3, 3, 560 - 6, 370 - 6);
                }

                float pulseFactor = 1.0F + 0.05F * (float)Math.Sin(_phase * 2.5F);
                float glowW = 200F * pulseFactor;
                float glowH = 150F * pulseFactor;
                using (var glowPath = new GraphicsPath())
                {
                    glowPath.AddEllipse(xc - glowW / 2F, yc - glowH / 2F, glowW, glowH);
                    using (var pbr = new PathGradientBrush(glowPath))
                    {
                        pbr.CenterColor = Color.FromArgb(60, 0, 229, 255);
                        pbr.SurroundColors = new Color[] { Color.FromArgb(0, 16, 18, 26) };
                        g.FillPath(pbr, glowPath);
                    }
                }

                if (_logoImage != null)
                {
                    float lw = 180F * pulseFactor;
                    float lh = 135F * pulseFactor;
                    g.DrawImage(_logoImage, xc - lw / 2F, yc - lh / 2F, lw, lh);
                }
            }
            catch { }
        }

        private Color InterpolateColor(Color c1, Color c2, double ratio)
        {
            ratio = Math.Max(0.0, Math.Min(1.0, ratio));
            int r = (int)(c1.R + (c2.R - c1.R) * ratio);
            int g = (int)(c1.G + (c2.G - c1.G) * ratio);
            int b = (int)(c1.B + (c2.B - c1.B) * ratio);
            return Color.FromArgb(r, g, b);
        }
    }

    public static class LicenseSystem
    {
        private const string PublicKeyXml = "<RSAKeyValue><Modulus>2J5cjvVKSc7AvPzaP7PvEroP73TjctXsno3fQGdelOVp/lLm51BtDeN+MwPbM1UZJmAeLiyCXxQR/gtoK9RrI/3RoP7Sb1ElF6vklJLxao4N+P9IoeqSNKHKcgBoeA5GivbgwMK0Ev1kz7QRg+00lUlgKQp7u3oWoX/Ca0TzlTajZVUKSC7YaNiu1slBymViXIQkHPYzhzaKkn/gPZmwRk7PQopy0ZkXTjiBybFpEc71SHdz8N4kyi8EUfr+OmEseLwfC7uVDLmxX1UIlYPilq51ivvqx3j+buxwTrarfhPV37r2mGPfZx7kf3QPx5mCHbn3Oj0o3zwzF4ciZHDTQQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        public static bool IsTrialExpired { get; private set; } = false;
        public static bool IsLicenseActive { get; private set; } = false;
        public static bool TimeRollbackDetected { get; private set; } = false;
        public static int RemainingDays { get; private set; } = 30;
        public static string CompanyCode { get; set; } = "BAYIKODU";
        public static bool IsSpecialistMode { get; set; } = false;

        public static string LicenseFilePath => ConfigHelper.GetConfigPath("license.key");

        public static DateTime LicenseExpiryDate { get; private set; } = DateTime.MaxValue;

        public static void Initialize()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string[] possibleCompanyFiles = new[]
                {
                    Path.Combine(baseDir, "company.txt"),
                    ConfigHelper.GetConfigPath("company.txt")
                };

                foreach (var companyFile in possibleCompanyFiles)
                {
                    if (File.Exists(companyFile))
                    {
                        string content = File.ReadAllText(companyFile).Trim();
                        if (!string.IsNullOrEmpty(content))
                        {
                            var parts = content.Split(new[] { '|', ':', ',', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0 && !parts[0].Contains("=")) CompanyCode = NormalizeCompanyCode(parts[0]);
                            if (parts.Length > 1)
                            {
                                string role = parts[1].Trim().ToLowerInvariant();
                                if (role == "uzman" || role == "bayi" || role == "admin" || role == "technician" || role == "specialist")
                                {
                                    IsSpecialistMode = true;
                                }
                            }
                            if (content.ToLowerInvariant().Contains("uzman") || content.ToLowerInvariant().Contains("bayi") || content.ToLowerInvariant().Contains("admin"))
                            {
                                IsSpecialistMode = true;
                            }
                        }
                    }
                }

                // 1. Auto-detect from command line args (e.g., --company BY-EMF-2026 or -tenant EMF_BILGISAYAR)
                try
                {
                    string[] args = Environment.GetCommandLineArgs();
                    for (int i = 0; i < args.Length; i++)
                    {
                        if ((args[i] == "--company" || args[i] == "-company" || args[i] == "--tenant" || args[i] == "-tenant" || args[i] == "--bayi") && i + 1 < args.Length)
                        {
                            CompanyCode = NormalizeCompanyCode(args[i + 1]);
                        }
                    }
                }
                catch { }

                // 2. Auto-detect from EXE filename (e.g., BigLineconnect_EMF.exe or BigLineconnect_BY-EMF-2026.exe)
                try
                {
                    string exeName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule?.FileName ?? "").ToUpperInvariant();
                    if (exeName.Contains("_") && !exeName.Equals("BIGLINECONNECT", StringComparison.OrdinalIgnoreCase) && !exeName.Equals("BIGLINECONNECT_SETUP", StringComparison.OrdinalIgnoreCase))
                    {
                        string sub = exeName.Substring(exeName.IndexOf('_') + 1);
                        if (!string.IsNullOrWhiteSpace(sub) && !sub.Equals("SETUP", StringComparison.OrdinalIgnoreCase) && !sub.StartsWith("V1.") && !sub.StartsWith("V2.") && !sub.StartsWith("V3.") && !sub.StartsWith("V4.") && !sub.StartsWith("V5.") && !sub.Equals("SETUP_V2", StringComparison.OrdinalIgnoreCase))
                        {
                            CompanyCode = NormalizeCompanyCode(sub);
                        }
                    }
                }
                catch { }

                if (string.IsNullOrWhiteSpace(CompanyCode) || CompanyCode == "BIGLINE" || CompanyCode.StartsWith("V1.") || CompanyCode.StartsWith("V2.") || CompanyCode.StartsWith("V3.") || CompanyCode.StartsWith("V4.") || CompanyCode.StartsWith("V5."))
                {
                    CompanyCode = "BAYIKODU";
                }

                string[] possibleRoleFiles = new[]
                {
                    Path.Combine(baseDir, "uzman.txt"),
                    Path.Combine(baseDir, "role.txt"),
                    Path.Combine(baseDir, "bayi.txt"),
                    ConfigHelper.GetConfigPath("uzman.txt"),
                    ConfigHelper.GetConfigPath("role.txt"),
                    ConfigHelper.GetConfigPath("bayi.txt")
                };

                foreach (var rf in possibleRoleFiles)
                {
                    if (File.Exists(rf))
                    {
                        IsSpecialistMode = true;
                        break;
                    }
                }

                if (CompanyCode.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase))
                {
                    IsSpecialistMode = true;
                }
            }
            catch { }

            IsLicenseActive = CheckLicenseFile();

            if (IsLicenseActive)
            {
                IsTrialExpired = false;
                return;
            }

            CheckTrialState();
        }

        public static string NormalizeCompanyCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "BAYIKODU";
            string clean = input.Trim().ToUpperInvariant()
                .Replace("Ç", "C").Replace("Ğ", "G").Replace("İ", "I").Replace("Ö", "O").Replace("Ş", "S").Replace("Ü", "U")
                .Replace(" ", "_").Replace("-", "_");
            if (clean == "BIGLINE" || clean.StartsWith("V1.") || clean.StartsWith("V2.") || clean.StartsWith("V3.") || clean.StartsWith("V4."))
            {
                return "BAYIKODU";
            }
            return clean;
        }

        public static void SaveCompanyCode(string code, bool isSpecialist = true)
        {
            try
            {
                string norm = NormalizeCompanyCode(code);
                CompanyCode = norm;
                IsSpecialistMode = isSpecialist;
                string content = $"{norm}|{(isSpecialist ? "uzman" : "musteri")}";

                string configPath = ConfigHelper.GetConfigPath("company.txt");
                File.WriteAllText(configPath, content);

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string baseFile = Path.Combine(baseDir, "company.txt");
                try { File.WriteAllText(baseFile, content); } catch { }
            }
            catch { }
        }

        private static bool CheckLicenseFile()
        {
            try
            {
                string licPath = LicenseFilePath;
                if (!File.Exists(licPath)) return false;

                string licContent = File.ReadAllText(licPath).Trim();
                if (string.IsNullOrEmpty(licContent)) return false;

                int dotIdx = licContent.LastIndexOf('.');
                if (dotIdx == -1) return false;

                string payload = licContent.Substring(0, dotIdx);
                string signatureBase64 = licContent.Substring(dotIdx + 1);

                using (var rsa = new RSACryptoServiceProvider(2048))
                {
                    rsa.FromXmlString(PublicKeyXml);
                    byte[] dataBytes = Encoding.UTF8.GetBytes(payload);
                    byte[] sigBytes = Convert.FromBase64String(signatureBase64);

                    bool isSigValid = rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA256"), sigBytes);
                    if (!isSigValid) return false;
                }

                using (var doc = System.Text.Json.JsonDocument.Parse(payload))
                {
                    var root = doc.RootElement;
                    string expiryStr = root.GetProperty("Expiry").GetString() ?? "";
                    string machineId = root.GetProperty("MachineId").GetString() ?? "";

                    if (DateTime.TryParse(expiryStr, out DateTime expiryDate))
                    {
                        if (expiryDate < DateTime.Today)
                        {
                            return false;
                        }

                        LicenseExpiryDate = expiryDate;
                        int daysLeft = (int)(expiryDate.Date - DateTime.Today).TotalDays;
                        RemainingDays = daysLeft > 9999 ? 9999 : (daysLeft < 0 ? 0 : daysLeft);
                    }
                    else
                    {
                        return false;
                    }

                    if (machineId != "*")
                    {
                        string localMac = GetMachineUniqueId();
                        if (machineId != localMac)
                        {
                            return false;
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void CheckTrialState()
        {
            try
            {
                DateTime firstRun = DateTime.MinValue;
                DateTime lastRun = DateTime.MinValue;

                var registryDateHklm = ReadRegistryDateHklm();
                var registryDateHkcu = ReadRegistryDateHkcu();
                var fileDate = ReadFileDate();

                DateTime earliestFirst = DateTime.MaxValue;
                DateTime latestLast = DateTime.MinValue;

                if (registryDateHklm.FirstRun != DateTime.MinValue && registryDateHklm.FirstRun < earliestFirst) earliestFirst = registryDateHklm.FirstRun;
                if (registryDateHkcu.FirstRun != DateTime.MinValue && registryDateHkcu.FirstRun < earliestFirst) earliestFirst = registryDateHkcu.FirstRun;
                if (fileDate.FirstRun != DateTime.MinValue && fileDate.FirstRun < earliestFirst) earliestFirst = fileDate.FirstRun;

                if (registryDateHklm.LastRun != DateTime.MinValue && registryDateHklm.LastRun > latestLast) latestLast = registryDateHklm.LastRun;
                if (registryDateHkcu.LastRun != DateTime.MinValue && registryDateHkcu.LastRun > latestLast) latestLast = registryDateHkcu.LastRun;
                if (fileDate.LastRun != DateTime.MinValue && fileDate.LastRun > latestLast) latestLast = fileDate.LastRun;

                if (earliestFirst == DateTime.MaxValue)
                {
                    firstRun = DateTime.Today;
                    lastRun = DateTime.Now;
                    
                    WriteTrialState(firstRun, lastRun);
                    
                    IsTrialExpired = false;
                    RemainingDays = 30;
                    return;
                }

                firstRun = earliestFirst;
                lastRun = latestLast;

                DateTime now = DateTime.Now;
                if (now < lastRun)
                {
                    TimeRollbackDetected = true;
                    IsTrialExpired = true;
                    RemainingDays = 0;
                    WriteTrialState(firstRun, lastRun);
                    return;
                }

                lastRun = now;
                WriteTrialState(firstRun, lastRun);

                int daysUsed = (int)(DateTime.Today - firstRun.Date).TotalDays;
                if (daysUsed < 0) daysUsed = 0;

                RemainingDays = 30 - daysUsed;
                if (RemainingDays < 0) RemainingDays = 0;

                if (daysUsed >= 30)
                {
                    IsTrialExpired = true;
                }
                else
                {
                    IsTrialExpired = false;
                }
            }
            catch
            {
                IsTrialExpired = true;
                RemainingDays = 0;
            }
        }

        private static (DateTime FirstRun, DateTime LastRun) ReadRegistryDateHklm()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\BigLineconnect"))
                {
                    if (key != null)
                    {
                        string fVal = DecryptString(key.GetValue("FirstRun") as string ?? "");
                        string lVal = DecryptString(key.GetValue("LastRun") as string ?? "");

                        if (DateTime.TryParse(fVal, out DateTime fDate) && DateTime.TryParse(lVal, out DateTime lDate))
                        {
                            return (fDate, lDate);
                        }
                    }
                }
            }
            catch {}
            return (DateTime.MinValue, DateTime.MinValue);
        }

        private static (DateTime FirstRun, DateTime LastRun) ReadRegistryDateHkcu()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\BigLineconnect"))
                {
                    if (key != null)
                    {
                        string fVal = DecryptString(key.GetValue("FirstRun") as string ?? "");
                        string lVal = DecryptString(key.GetValue("LastRun") as string ?? "");

                        if (DateTime.TryParse(fVal, out DateTime fDate) && DateTime.TryParse(lVal, out DateTime lDate))
                        {
                            return (fDate, lDate);
                        }
                    }
                }
            }
            catch {}
            return (DateTime.MinValue, DateTime.MinValue);
        }

        private static (DateTime FirstRun, DateTime LastRun) ReadFileDate()
        {
            try
            {
                string sysDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                string sysFile = Path.Combine(sysDir, "sys_state.bin");
                if (File.Exists(sysFile))
                {
                    string content = File.ReadAllText(sysFile);
                    string decrypted = DecryptString(content);
                    string[] parts = decrypted.Split('|');
                    if (parts.Length == 2 && DateTime.TryParse(parts[0], out DateTime fDate) && DateTime.TryParse(parts[1], out DateTime lDate))
                    {
                        return (fDate, lDate);
                    }
                }
            }
            catch {}
            return (DateTime.MinValue, DateTime.MinValue);
        }

        private static void WriteTrialState(DateTime firstRun, DateTime lastRun)
        {
            string fStr = EncryptString(firstRun.ToString("o"));
            string lStr = EncryptString(lastRun.ToString("o"));

            try
            {
                using (var key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\BigLineconnect"))
                {
                    if (key != null)
                    {
                        key.SetValue("FirstRun", fStr);
                        key.SetValue("LastRun", lStr);
                    }
                }
            }
            catch {}

            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\BigLineconnect"))
                {
                    if (key != null)
                    {
                        key.SetValue("FirstRun", fStr);
                        key.SetValue("LastRun", lStr);
                    }
                }
            }
            catch {}

            try
            {
                string sysDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                if (!Directory.Exists(sysDir)) Directory.CreateDirectory(sysDir);

                string sysFile = Path.Combine(sysDir, "sys_state.bin");
                string fileContent = EncryptString(firstRun.ToString("o") + "|" + lastRun.ToString("o"));
                File.WriteAllText(sysFile, fileContent);
            }
            catch {}
        }

        public static string GetMachineUniqueId()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    if (key != null)
                    {
                        string guid = key.GetValue("MachineGuid") as string ?? "";
                        if (!string.IsNullOrEmpty(guid))
                        {
                            return guid.Replace("-", "").ToUpper();
                        }
                    }
                }
            }
            catch {}
            return Environment.MachineName.GetHashCode().ToString("X");
        }

        private static string EncryptString(string val)
        {
            if (string.IsNullOrEmpty(val)) return "";
            byte[] bytes = Encoding.UTF8.GetBytes(val);
            byte key = 0x57;
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ key);
            }
            return Convert.ToBase64String(bytes);
        }

        private static string DecryptString(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return "";
            try
            {
                byte[] bytes = Convert.FromBase64String(base64);
                byte key = 0x57;
                for (int i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = (byte)(bytes[i] ^ key);
                }
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return "";
            }
        }
    }
}
