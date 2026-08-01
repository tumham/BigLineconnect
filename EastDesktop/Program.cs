using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EastDesktop
{
    static class Program
    {
        private static string RELAY_URL = "ws://213.142.159.18:5080/register-host";
        private static ClientWebSocket? _ws;
        private static CancellationTokenSource? _streamCts;
        private static readonly object _streamLock = new object();
        private static Mutex? _singleInstanceMutex;

        private static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EastDesktop");
        private static readonly string IdFile = Path.Combine(DataDir, "host_id.txt");

        [STAThread]
        static void Main(string[] args)
        {
            // SINGLE INSTANCE GUARD: Prevent multiple instances of EastDesktop running in parallel
            _singleInstanceMutex = new Mutex(true, @"Global\EastDesktop_SingleInstance_Mutex_2026", out bool createdNew);
            if (!createdNew)
            {
                // Already running! Exit immediately so only 1 process exists!
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CleanAndEnsureSingleAutoStart();

            if (args.Length > 0 && args[0] == "--service")
            {
                ServiceBase.Run(new EastDesktopService());
                return;
            }

            Application.Run(new MainForm());
        }

        private static void CleanAndEnsureSingleAutoStart()
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                string exePath = Application.ExecutablePath;

                // Remove old duplicate startup shortcuts & tasks
                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string startupExe = Path.Combine(startupFolder, "EastDesktop.exe");
                if (File.Exists(startupExe))
                {
                    try { File.Delete(startupExe); } catch { }
                }

                try
                {
                    Process.Start(new ProcessStartInfo("schtasks.exe", "/delete /tn \"EastDesktopAutoStart\" /f") { CreateNoWindow = true, UseShellExecute = false });
                }
                catch { }

                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                    {
                        key?.DeleteValue("EastDesktop", false);
                    }
                }
                catch { }

                // Single Clean HKCU Registry Auto-Start
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    key?.SetValue("EastDesktop", $"\"{exePath}\"");
                }
            }
            catch { }
        }

        public class MainForm : Form
        {
            public static MainForm? Instance { get; private set; }
            private TextBox txtHostId;
            private Button btnConnect;
            private Button btnReboot;
            private PictureBox picScreen;
            private Label lblStatus;
            private Label lblMyId;
            private ClientWebSocket? _clientWs;
            private CancellationTokenSource? _clientCts;
            private string _lastConnectedTargetId = "";
            private bool _isRebootingMode = false;

            public MainForm()
            {
                Instance = this;
                this.Text = "EastDesktop - Hızlı Uzak Masaüstü & Kesintisiz Yeniden Başlatma";
                this.Size = new Size(1000, 650);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.BackColor = Color.FromArgb(18, 24, 38);
                this.ForeColor = Color.White;

                var pnlTop = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.FromArgb(28, 36, 54)
                };

                var lblTitle = new Label
                {
                    Text = "⚡ EastDesktop",
                    Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 229, 255),
                    Location = new Point(15, 12),
                    AutoSize = true
                };

                lblMyId = new Label
                {
                    Text = "ID'niz: Yükleniyor...",
                    Location = new Point(15, 36),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 230, 118)
                };

                var lblIdLabel = new Label
                {
                    Text = "Bağlanılacak ID:",
                    Location = new Point(230, 20),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9.5f)
                };

                txtHostId = new TextBox
                {
                    Location = new Point(340, 16),
                    Size = new Size(140, 26),
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    TextAlign = HorizontalAlignment.Center
                };

                btnConnect = new Button
                {
                    Text = "Connect",
                    Location = new Point(490, 15),
                    Size = new Size(100, 28),
                    BackColor = Color.FromArgb(0, 200, 83),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnConnect.Click += BtnConnect_Click;

                btnReboot = new Button
                {
                    Text = "🔄 REBOOT REMOTE",
                    Location = new Point(600, 15),
                    Size = new Size(160, 28),
                    BackColor = Color.FromArgb(217, 4, 41),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnReboot.Click += BtnReboot_Click;

                lblStatus = new Label
                {
                    Text = "Durum: Sunucuya Bağlanıyor...",
                    Location = new Point(770, 20),
                    AutoSize = true,
                    ForeColor = Color.FromArgb(255, 214, 10)
                };

                pnlTop.Controls.Add(lblTitle);
                pnlTop.Controls.Add(lblMyId);
                pnlTop.Controls.Add(lblIdLabel);
                pnlTop.Controls.Add(txtHostId);
                pnlTop.Controls.Add(btnConnect);
                pnlTop.Controls.Add(btnReboot);
                pnlTop.Controls.Add(lblStatus);
                this.Controls.Add(pnlTop);

                picScreen = new PictureBox
                {
                    Dock = DockStyle.Fill,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Black
                };
                picScreen.MouseDown += PicScreen_MouseDown;
                picScreen.MouseUp += PicScreen_MouseUp;
                picScreen.MouseMove += PicScreen_MouseMove;
                this.Controls.Add(picScreen);

                // Start local host service client in background
                Task.Run(() => StartHostLoop());
            }

            public void SetMyId(string id)
            {
                lblMyId.Text = $"ID'niz: {id}";
                lblStatus.Text = "Durum: Hazır";
            }

            private async void BtnConnect_Click(object? sender, EventArgs e)
            {
                string targetId = txtHostId.Text.Trim().Replace(" ", "");
                if (string.IsNullOrEmpty(targetId))
                {
                    MessageBox.Show("Lütfen bağlanılacak ID'yi girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _lastConnectedTargetId = targetId;
                _isRebootingMode = false;
                await ConnectToTarget(targetId);
            }

            private async Task<bool> ConnectToTarget(string targetId)
            {
                btnConnect.Enabled = false;
                lblStatus.Text = "Bağlanılıyor...";

                _clientCts?.Cancel();
                _clientCts = new CancellationTokenSource();
                _clientWs?.Dispose();
                _clientWs = new ClientWebSocket();

                try
                {
                    string connectUrl = $"ws://213.142.159.18:5080/connect-client?id={targetId}";
                    using var connectCts = new CancellationTokenSource(5000);
                    await _clientWs.ConnectAsync(new Uri(connectUrl), connectCts.Token);
                    
                    lblStatus.Text = "BAĞLANDI! 🚀";

                    _ = Task.Run(() => ReceiveLoop(_clientWs, _clientCts.Token));
                    return true;
                }
                catch
                {
                    lblStatus.Text = "Bağlanamadı.";
                    btnConnect.Enabled = true;
                    return false;
                }
            }

            private async void BtnReboot_Click(object? sender, EventArgs e)
            {
                if (_clientWs == null || _clientWs.State != WebSocketState.Open)
                {
                    MessageBox.Show("Önce karşı bilgisayara bağlanmalısınız!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show("Karşı bilgisayarı YENİDEN BAŞLATMAK istediğinize emin misiniz?\nAçılışta OTOMATİK GERİ BAĞLANACAKTIR.", "EastDesktop Reboot", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm == DialogResult.Yes)
                {
                    _isRebootingMode = true;
                    byte[] bytes = Encoding.UTF8.GetBytes("{\"type\":\"reboot\"}");
                    await _clientWs.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    lblStatus.Text = "Reboot Gönderildi! Karşı Makine Açılır Açılmaz Bağlanacak... ⏳";
                    
                    _ = Task.Run(() => AutoReconnectLoop(_lastConnectedTargetId));
                }
            }

            private async Task AutoReconnectLoop(string targetId)
            {
                int attempt = 1;
                while (_isRebootingMode)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        lblStatus.Text = $"⏳ Karşı Makine Açılması Bekleniyor... Deneme: {attempt}";
                    }));

                    await Task.Delay(2500);

                    try
                    {
                        var ws = new ClientWebSocket();
                        using var connectCts = new CancellationTokenSource(4000);
                        string connectUrl = $"ws://213.142.159.18:5080/connect-client?id={targetId}";
                        await ws.ConnectAsync(new Uri(connectUrl), connectCts.Token);

                        // Check initial message from Relay to verify host is online
                        var buffer = new byte[1024 * 64];
                        using var readCts = new CancellationTokenSource(3000);
                        var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), readCts.Token);

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            if (msg.Contains("ERROR") || msg.Contains("ID_NOT_FOUND"))
                            {
                                ws.Dispose();
                                attempt++;
                                continue;
                            }
                        }

                        if (ws.State == WebSocketState.Open)
                        {
                            _clientCts?.Cancel();
                            _clientCts = new CancellationTokenSource();
                            _clientWs = ws;
                            _isRebootingMode = false;

                            this.BeginInvoke(new Action(() =>
                            {
                                lblStatus.Text = "OTOMATİK GERİ BAĞLANDI! 🎉🚀";
                                btnConnect.Enabled = false;
                            }));

                            if (result.MessageType == WebSocketMessageType.Binary && result.Count > 0)
                            {
                                using var ms = new MemoryStream(buffer, 0, result.Count);
                                try
                                {
                                    Image img = Image.FromStream(ms);
                                    picScreen.BeginInvoke(new Action(() =>
                                    {
                                        var old = picScreen.Image;
                                        picScreen.Image = img;
                                        old?.Dispose();
                                    }));
                                }
                                catch { }
                            }

                            _ = Task.Run(() => ReceiveLoop(_clientWs, _clientCts.Token));
                            break;
                        }
                    }
                    catch
                    {
                        attempt++;
                    }
                }
            }

            private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken token)
            {
                var buffer = new byte[1024 * 1024 * 2];
                while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    try
                    {
                        int totalReceived = 0;
                        WebSocketReceiveResult result;
                        do
                        {
                            var segment = new ArraySegment<byte>(buffer, totalReceived, buffer.Length - totalReceived);
                            result = await ws.ReceiveAsync(segment, token);
                            totalReceived += result.Count;
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        if (result.MessageType == WebSocketMessageType.Binary && totalReceived > 0)
                        {
                            using var ms = new MemoryStream(buffer, 0, totalReceived);
                            Image img = Image.FromStream(ms);
                            picScreen.BeginInvoke(new Action(() =>
                            {
                                var old = picScreen.Image;
                                picScreen.Image = img;
                                old?.Dispose();
                            }));
                        }
                    }
                    catch { break; }
                }

                if (!_isRebootingMode && !string.IsNullOrEmpty(_lastConnectedTargetId))
                {
                    _isRebootingMode = true;
                    _ = Task.Run(() => AutoReconnectLoop(_lastConnectedTargetId));
                }
            }

            private void SendInput(string type, int x, int y, string btn = "left", string action = "down")
            {
                if (_clientWs == null || _clientWs.State != WebSocketState.Open) return;
                if (picScreen.Image == null) return;

                int imgW = picScreen.Image.Width;
                int imgH = picScreen.Image.Height;
                int boxW = picScreen.Width;
                int boxH = picScreen.Height;

                float normX = (float)x / boxW;
                float normY = (float)y / boxH;

                var payload = new
                {
                    type = type,
                    x = (int)(normX * imgW),
                    y = (int)(normY * imgH),
                    button = btn,
                    action = action
                };

                string json = JsonSerializer.Serialize(payload);
                byte[] bytes = Encoding.UTF8.GetBytes(json);
                _ = _clientWs.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }

            private void PicScreen_MouseDown(object? sender, MouseEventArgs e)
            {
                string btn = e.Button == MouseButtons.Right ? "right" : "left";
                SendInput("click", e.X, e.Y, btn, "down");
            }

            private void PicScreen_MouseUp(object? sender, MouseEventArgs e)
            {
                string btn = e.Button == MouseButtons.Right ? "right" : "left";
                SendInput("click", e.X, e.Y, btn, "up");
            }

            private void PicScreen_MouseMove(object? sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.None)
                {
                    SendInput("move", e.X, e.Y, "none", "move");
                }
            }
        }

        // Host Background Loop with Persistent Fixed Host ID & Single Capture Loop
        private static async Task StartHostLoop()
        {
            while (true)
            {
                try
                {
                    string savedId = "";
                    if (File.Exists(IdFile))
                    {
                        savedId = File.ReadAllText(IdFile).Trim().Replace(" ", "");
                    }

                    string connectUrl = RELAY_URL;
                    if (!string.IsNullOrEmpty(savedId))
                    {
                        connectUrl += $"?id={savedId}";
                    }

                    _ws = new ClientWebSocket();
                    await _ws.ConnectAsync(new Uri(connectUrl), CancellationToken.None);

                    var buffer = new byte[8192];
                    while (_ws.State == WebSocketState.Open)
                    {
                        var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close) break;

                        string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        if (msg.StartsWith("ID:") || msg.StartsWith("REGISTERED_ID:"))
                        {
                            string formattedId = msg.Substring(msg.IndexOf(':') + 1).Trim();
                            string rawId = formattedId.Replace(" ", "");

                            try
                            {
                                Directory.CreateDirectory(DataDir);
                                File.WriteAllText(IdFile, rawId);
                            }
                            catch { }

                            Console.WriteLine($"[EastDesktop Host ID Saved]: {rawId} ({formattedId})");
                            MainForm.Instance?.BeginInvoke(new Action(() =>
                            {
                                MainForm.Instance.SetMyId(formattedId);
                            }));
                        }
                        else if (msg.StartsWith("START_STREAM"))
                        {
                            lock (_streamLock)
                            {
                                _streamCts?.Cancel();
                                _streamCts = new CancellationTokenSource();
                                var token = _streamCts.Token;
                                Task.Run(() => StreamCaptureLoop(_ws, token));
                            }
                        }
                        else if (msg.StartsWith("{"))
                        {
                            ProcessCommand(msg);
                        }
                    }
                }
                catch { }
                await Task.Delay(2000);
            }
        }

        private static void ProcessCommand(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                string type = root.GetProperty("type").GetString() ?? "";

                if (type == "reboot")
                {
                    // Instant force reboot
                    Process.Start(new ProcessStartInfo("cmd.exe", "/c shutdown.exe /r /t 0 /f")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else if (type == "click" || type == "move")
                {
                    int x = root.GetProperty("x").GetInt32();
                    int y = root.GetProperty("y").GetInt32();
                    SetCursorPos(x, y);

                    if (type == "click")
                    {
                        string btn = root.GetProperty("button").GetString() ?? "";
                        string action = root.GetProperty("action").GetString() ?? "";
                        uint flag = 0;
                        if (btn == "left") flag = action == "down" ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
                        else if (btn == "right") flag = action == "down" ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;

                        if (flag != 0) mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
                    }
                }
            }
            catch { }
        }

        private static async Task StreamCaptureLoop(ClientWebSocket ws, CancellationToken token)
        {
            while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                try
                {
                    byte[]? frame = CaptureScreenJpg();
                    if (frame != null && frame.Length > 0)
                    {
                        await ws.SendAsync(new ArraySegment<byte>(frame), WebSocketMessageType.Binary, true, token);
                    }
                    await Task.Delay(60, token);
                }
                catch { break; }
            }
        }

        private static byte[]? CaptureScreenJpg()
        {
            try
            {
                int w = Screen.PrimaryScreen?.Bounds.Width ?? 1920;
                int h = Screen.PrimaryScreen?.Bounds.Height ?? 1080;

                using var bmp = new Bitmap(w, h);
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, new Size(w, h));
                }

                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
            catch { return null; }
        }

        // P/Invoke Win32
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        public class EastDesktopService : ServiceBase
        {
            public EastDesktopService()
            {
                this.ServiceName = "EastDesktopSvc";
            }

            protected override void OnStart(string[] args)
            {
                Task.Run(() => StartHostLoop());
            }

            protected override void OnStop() { }
        }
    }
}
