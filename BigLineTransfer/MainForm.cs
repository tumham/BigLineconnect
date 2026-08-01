using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BigLineTransfer
{
    public class MainForm : Form
    {
        // UI Controls
        private Label lblHeaderTitle;
        private Label lblHeaderSub;
        private Panel panelReceiver;
        private Panel panelSender;
        
        // Receiver controls
        private TextBox txtMyCode;
        private TextBox txtSavePath;
        private Button btnBrowseSavePath;
        private ProgressBar pbReceive;
        private Label lblReceiveStatus;
        private ListView lvReceivedFiles;

        // Sender controls
        private TextBox txtTargetCode;
        private Button btnConnectTarget;
        private ComboBox cbRemoteTargetFolder;
        private Panel panelDropZone;
        private Label lblDropHint;
        private ListBox lbSenderFiles;
        private Button btnAddFiles;
        private Button btnAddFolder;
        private Button btnClearList;
        private Button btnSend;
        private ProgressBar pbSend;
        private Label lblSendStatus;

        // Core State
        private string _myCode = "";
        private string _localIp = "";
        private string _saveFolder = "";
        private List<string> _sendPaths = new List<string>();
        private ClientWebSocket? _wsRelay;
        private HttpListener? _httpListener;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        private bool _isConnectedToRemote = false;
        private string _activeRemoteTarget = "";

        private FileStream? _incomingStream;
        private string? _incomingFileName;
        private bool _incomingIsFolder;
        private long _incomingFileSize;
        private long _incomingBytesRead;
        private string _incomingTargetDir = "";

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pChangeFilterStruct);
        private const uint WM_DROPFILES_MSG = 0x0233;
        private const uint WM_COPYDATA_MSG = 0x004A;
        private const uint WM_COPYGLOBALDATA_MSG = 0x0049;
        private const uint MSGFLT_ALLOW = 1;

        public MainForm(string initialTarget = "")
        {
            InitializeComponent();
            _saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            txtSavePath.Text = _saveFolder;
            
            // Generate local code and IP on launch
            GenerateInstantCodeAndIp();

            if (!string.IsNullOrEmpty(initialTarget))
            {
                txtTargetCode.Text = initialTarget;
                this.Shown += async (s, e) =>
                {
                    await Task.Delay(500);
                    BtnConnectTarget_Click(this, EventArgs.Empty);
                };
            }

            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
        }

        private void GenerateInstantCodeAndIp()
        {
            var rnd = new Random();
            int c1 = rnd.Next(100, 999);
            int c2 = rnd.Next(100, 999);
            _myCode = $"{c1} {c2}";
            _localIp = GetLocalIpAddress();

            txtMyCode.Text = $"{_myCode}  ({_localIp})";
        }

        private string GetLocalIpAddress()
        {
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "127.0.0.1";
                }
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        private void InitializeComponent()
        {
            this.Text = "BigLineTransfer v2.0 - Doğrudan HTTP & P2P Gerçek Zamanlı Transfer";
            this.Size = new Size(940, 710);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(15, 16, 22);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);
            }
            catch { }

            // Top Header
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                BackColor = Color.FromArgb(22, 24, 34)
            };
            panelHeader.Paint += (s, e) =>
            {
                using (var brush = new LinearGradientBrush(panelHeader.ClientRectangle, Color.FromArgb(0, 229, 255), Color.FromArgb(213, 0, 249), 0F))
                using (var pen = new Pen(brush, 2F))
                {
                    e.Graphics.DrawLine(pen, 0, panelHeader.Height - 1, panelHeader.Width, panelHeader.Height - 1);
                }
            };

            lblHeaderTitle = new Label
            {
                Text = "⚡ BigLineTransfer v2.0",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(20, 10),
                AutoSize = true
            };

            lblHeaderSub = new Label
            {
                Text = "Doğrulanmış Karşı Bilgisayar Bağlantısı ve Gerçek Sürücü Listeleme İle Kusursuz Transfer",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 190, 210),
                Location = new Point(23, 38),
                AutoSize = true
            };

            panelHeader.Controls.Add(lblHeaderTitle);
            panelHeader.Controls.Add(lblHeaderSub);

            // LEFT PANEL: RECEIVER (DOSYA AL)
            panelReceiver = new Panel
            {
                Location = new Point(15, 80),
                Size = new Size(440, 575),
                BackColor = Color.FromArgb(22, 24, 34)
            };
            panelReceiver.Paint += (s, e) => DrawBorder(e.Graphics, panelReceiver.ClientRectangle, Color.FromArgb(0, 229, 255));

            var lblRecTitle = new Label
            {
                Text = "📥 DOSYA ALICI (Bu Bilgisayar)",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(15, 12),
                AutoSize = true
            };

            var lblCodeHint = new Label
            {
                Text = "Bu Bilgisayarın Kodu ve Yerel Ağ IP Adresi:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 190, 210),
                Location = new Point(15, 42),
                AutoSize = true
            };

            txtMyCode = new TextBox
            {
                Location = new Point(15, 65),
                Size = new Size(408, 40),
                Font = new Font("Consolas", 15F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                ReadOnly = true,
                BackColor = Color.FromArgb(12, 14, 20),
                ForeColor = Color.FromArgb(0, 229, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblSavePath = new Label
            {
                Text = "Gelen Dosyaların Kaydedileceği Klasör (Varsayılan):",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 190, 210),
                Location = new Point(15, 118),
                AutoSize = true
            };

            txtSavePath = new TextBox
            {
                Location = new Point(15, 138),
                Size = new Size(320, 25),
                Font = new Font("Segoe UI", 9F),
                ReadOnly = true,
                BackColor = Color.FromArgb(12, 14, 20),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnBrowseSavePath = new Button
            {
                Text = "Gözat...",
                Location = new Point(340, 137),
                Size = new Size(83, 26),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 42, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnBrowseSavePath.Click += BtnBrowseSavePath_Click;

            lblReceiveStatus = new Label
            {
                Text = "🟢 HTTP Alıcı Servis Dinleniyor (Port: 8999)",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(15, 175),
                Size = new Size(408, 20)
            };

            pbReceive = new ProgressBar
            {
                Location = new Point(15, 198),
                Size = new Size(408, 14)
            };

            var lblRecList = new Label
            {
                Text = "Alınan Dosyalar Geçmişi:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 222),
                AutoSize = true
            };

            lvReceivedFiles = new ListView
            {
                Location = new Point(15, 245),
                Size = new Size(408, 310),
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(12, 14, 20),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            lvReceivedFiles.Columns.Add("Dosya Adı", 230);
            lvReceivedFiles.Columns.Add("Boyut", 85);
            lvReceivedFiles.Columns.Add("Zaman", 85);

            panelReceiver.Controls.Add(lblRecTitle);
            panelReceiver.Controls.Add(lblCodeHint);
            panelReceiver.Controls.Add(txtMyCode);
            panelReceiver.Controls.Add(lblSavePath);
            panelReceiver.Controls.Add(txtSavePath);
            panelReceiver.Controls.Add(btnBrowseSavePath);
            panelReceiver.Controls.Add(lblReceiveStatus);
            panelReceiver.Controls.Add(pbReceive);
            panelReceiver.Controls.Add(lblRecList);
            panelReceiver.Controls.Add(lvReceivedFiles);

            // RIGHT PANEL: SENDER (DOSYA GÖNDER)
            panelSender = new Panel
            {
                Location = new Point(470, 80),
                Size = new Size(440, 575),
                BackColor = Color.FromArgb(22, 24, 34)
            };
            panelSender.Paint += (s, e) => DrawBorder(e.Graphics, panelSender.ClientRectangle, Color.FromArgb(213, 0, 249));

            var lblSendTitle = new Label
            {
                Text = "📤 DOSYA GÖNDERİCİ",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(213, 0, 249),
                Location = new Point(15, 12),
                AutoSize = true
            };

            var lblTargetCode = new Label
            {
                Text = "Hedef Bilgisayarın Kodu veya IP Adresi:",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(180, 190, 210),
                Location = new Point(15, 42),
                AutoSize = true
            };

            txtTargetCode = new TextBox
            {
                Location = new Point(15, 65),
                Size = new Size(250, 40),
                Font = new Font("Consolas", 15F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                BackColor = Color.FromArgb(12, 14, 20),
                ForeColor = Color.FromArgb(213, 0, 249),
                BorderStyle = BorderStyle.FixedSingle
            };

            btnConnectTarget = new Button
            {
                Text = "🔌 BAĞLAN & SÜRÜCÜLERİ ÇEK",
                Location = new Point(272, 65),
                Size = new Size(151, 38),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 229, 255),
                ForeColor = Color.Black,
                Cursor = Cursors.Hand
            };
            btnConnectTarget.Click += BtnConnectTarget_Click;

            var lblRemoteFolder = new Label
            {
                Text = "Karşı Bilgisayarın Gerçek Sürücüleri & Klasörleri:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(15, 115),
                AutoSize = true
            };

            cbRemoteTargetFolder = new ComboBox
            {
                Location = new Point(15, 137),
                Size = new Size(408, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(12, 14, 20),
                ForeColor = Color.Yellow,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            cbRemoteTargetFolder.Items.Add("⚠️ Bağlantı Kurulmadı - Önce '🔌 BAĞLAN' Butonuna Basın");
            cbRemoteTargetFolder.SelectedIndex = 0;

            panelDropZone = new Panel
            {
                Location = new Point(15, 175),
                Size = new Size(408, 75),
                BackColor = Color.FromArgb(16, 18, 26),
                AllowDrop = true
            };
            panelDropZone.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(213, 0, 249), 1.5F) { DashStyle = DashStyle.Dash })
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, panelDropZone.Width - 3, panelDropZone.Height - 3);
                }
            };
            panelDropZone.DragEnter += PanelDropZone_DragEnter;
            panelDropZone.DragDrop += PanelDropZone_DragDrop;

            lblDropHint = new Label
            {
                Text = "📁 Gönderilecek Dosya veya Klasörleri Buraya Sürükleyin\nya da Tıklayarak Seçin",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(160, 170, 190),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            lblDropHint.Click += (s, e) => BtnAddFiles_Click(s, e);
            panelDropZone.Controls.Add(lblDropHint);

            lbSenderFiles = new ListBox
            {
                Location = new Point(15, 258),
                Size = new Size(408, 140),
                BackColor = Color.FromArgb(12, 14, 20),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 8.5F)
            };

            btnAddFiles = new Button
            {
                Text = "+ Dosya Ekle",
                Location = new Point(15, 405),
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 42, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAddFiles.Click += BtnAddFiles_Click;

            btnAddFolder = new Button
            {
                Text = "+ Klasör Ekle",
                Location = new Point(152, 405),
                Size = new Size(130, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 42, 60),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnAddFolder.Click += BtnAddFolder_Click;

            btnClearList = new Button
            {
                Text = "Temizle",
                Location = new Point(290, 405),
                Size = new Size(133, 28),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 30, 40),
                ForeColor = Color.FromArgb(255, 100, 100),
                Cursor = Cursors.Hand
            };
            btnClearList.Click += (s, e) =>
            {
                _sendPaths.Clear();
                lbSenderFiles.Items.Clear();
                lblSendStatus.Text = "Hazır.";
                pbSend.Value = 0;
            };

            lblSendStatus = new Label
            {
                Text = "Önce Karşı Bilgisayara Bağlanın.",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 193, 7),
                Location = new Point(15, 438),
                Size = new Size(408, 20)
            };

            pbSend = new ProgressBar
            {
                Location = new Point(15, 460),
                Size = new Size(408, 14)
            };

            btnSend = new Button
            {
                Text = "🚀 KARŞI BİLGİSAYARA GÖNDER",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(15, 485),
                Size = new Size(408, 60),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(213, 0, 249),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Enabled = false // Disabled until explicit connection!
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += BtnSend_Click;

            panelSender.Controls.Add(lblSendTitle);
            panelSender.Controls.Add(lblTargetCode);
            panelSender.Controls.Add(txtTargetCode);
            panelSender.Controls.Add(btnConnectTarget);
            panelSender.Controls.Add(lblRemoteFolder);
            panelSender.Controls.Add(cbRemoteTargetFolder);
            panelSender.Controls.Add(panelDropZone);
            panelSender.Controls.Add(lbSenderFiles);
            panelSender.Controls.Add(btnAddFiles);
            panelSender.Controls.Add(btnAddFolder);
            panelSender.Controls.Add(btnClearList);
            panelSender.Controls.Add(lblSendStatus);
            panelSender.Controls.Add(pbSend);
            panelSender.Controls.Add(btnSend);

            this.Controls.Add(panelHeader);
            this.Controls.Add(panelReceiver);
            this.Controls.Add(panelSender);

            // Form DragDrop bypass for UIPI
            this.AllowDrop = true;
            this.DragEnter += PanelDropZone_DragEnter;
            this.DragDrop += PanelDropZone_DragDrop;
        }

        private void DrawBorder(Graphics g, Rectangle rect, Color borderColor)
        {
            using (var pen = new Pen(borderColor, 1.5F))
            {
                g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
            }
        }

        private void MainForm_Load(object? sender, EventArgs e)
        {
            try
            {
                ChangeWindowMessageFilterEx(this.Handle, WM_DROPFILES_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(this.Handle, WM_COPYDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(this.Handle, WM_COPYGLOBALDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(panelDropZone.Handle, WM_DROPFILES_MSG, MSGFLT_ALLOW, IntPtr.Zero);
            }
            catch { }

            // Start Embedded HTTP Web Server on Port 8999
            StartHttpServer();

            // Connect to Relay Server in Background
            _ = Task.Run(() => ConnectRelayLoop(_cts.Token));
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _cts.Cancel();
            try { _httpListener?.Stop(); } catch { }
            try { _wsRelay?.Dispose(); } catch { }
        }

        private void BtnBrowseSavePath_Click(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Gelen Dosyaların Kaydedileceği Klasörü Seçin";
                fbd.SelectedPath = _saveFolder;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _saveFolder = fbd.SelectedPath;
                    txtSavePath.Text = _saveFolder;
                }
            }
        }

        // ================= EMBEDDED HTTP SERVER (PORT 8999) =================

        private void StartHttpServer()
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add("http://*:8999/");
                _httpListener.Start();
                _ = Task.Run(() => AcceptHttpRequestsLoop(_cts.Token));
            }
            catch
            {
                try
                {
                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add("http://+:8999/");
                    _httpListener.Start();
                    _ = Task.Run(() => AcceptHttpRequestsLoop(_cts.Token));
                }
                catch { }
            }
        }

        private async Task AcceptHttpRequestsLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _httpListener != null && _httpListener.IsListening)
            {
                try
                {
                    var ctx = await _httpListener.GetContextAsync();
                    _ = Task.Run(() => HandleHttpRequest(ctx));
                }
                catch { break; }
            }
        }

        private async Task HandleHttpRequest(HttpListenerContext ctx)
        {
            try
            {
                var req = ctx.Request;
                var resp = ctx.Response;
                resp.Headers.Add("Access-Control-Allow-Origin", "*");

                if (req.HttpMethod == "GET" && req.Url?.AbsolutePath == "/api/drives")
                {
                    var drives = new List<string>();
                    foreach (var d in DriveInfo.GetDrives())
                    {
                        if (d.IsReady) drives.Add(d.Name);
                    }
                    string desk = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string down = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads";

                    string json = JsonSerializer.Serialize(new
                    {
                        status = "ok",
                        computerName = Environment.MachineName,
                        drives = drives,
                        desktop = desk,
                        downloads = down
                    });

                    byte[] buf = Encoding.UTF8.GetBytes(json);
                    resp.ContentType = "application/json; charset=utf-8";
                    resp.ContentLength64 = buf.Length;
                    await resp.OutputStream.WriteAsync(buf, 0, buf.Length);
                    resp.Close();
                }
                else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/api/upload")
                {
                    string fileName = Uri.UnescapeDataString(req.Headers["X-File-Name"] ?? "file.dat");
                    string targetDir = Uri.UnescapeDataString(req.Headers["X-Target-Dir"] ?? _saveFolder);
                    bool isFolder = req.Headers["X-Is-Folder"] == "true";
                    long fileSize = req.ContentLength64;

                    if (string.IsNullOrEmpty(targetDir)) targetDir = _saveFolder;
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                    string filePath = Path.Combine(targetDir, fileName);
                    int counter = 1;
                    string origName = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);
                    while (File.Exists(filePath))
                    {
                        filePath = Path.Combine(targetDir, $"{origName}({counter}){ext}");
                        counter++;
                    }

                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[64 * 1024];
                        int bytesRead;
                        long totalRead = 0;

                        while ((bytesRead = await req.InputStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fs.WriteAsync(buffer, 0, bytesRead);
                            totalRead += bytesRead;
                            UpdateReceiveProgress(Path.GetFileName(filePath), totalRead, fileSize);
                        }
                        await fs.FlushAsync();
                    }

                    if (isFolder)
                    {
                        string zipPath = filePath;
                        string destFolder = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(fileName));
                        try
                        {
                            ZipFile.ExtractToDirectory(zipPath, destFolder, true);
                            File.Delete(zipPath);
                            filePath = destFolder;
                        }
                        catch { }
                    }

                    AddReceivedItemToListView(Path.GetFileName(filePath), FormatSize(fileSize));
                    UpdateReceiveStatus($"🎉 Alındı ({targetDir}): {Path.GetFileName(filePath)}", isError: false);

                    string ackJson = JsonSerializer.Serialize(new { status = "ok", path = filePath });
                    byte[] ackBuf = Encoding.UTF8.GetBytes(ackJson);
                    resp.ContentType = "application/json";
                    resp.ContentLength64 = ackBuf.Length;
                    await resp.OutputStream.WriteAsync(ackBuf, 0, ackBuf.Length);
                    resp.Close();
                }
                else
                {
                    resp.StatusCode = 404;
                    resp.Close();
                }
            }
            catch
            {
                try
                {
                    ctx.Response.StatusCode = 500;
                    ctx.Response.Close();
                }
                catch { }
            }
        }

        // ================= CONNECT TO REMOTE TARGET =================

        private async void BtnConnectTarget_Click(object? sender, EventArgs e)
        {
            string targetInput = txtTargetCode.Text.Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(targetInput))
            {
                MessageBox.Show("Lütfen Hedef Bilgisayarın Kodu veya IP Adresini Girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnConnectTarget.Enabled = false;
            lblSendStatus.Text = "Karşı bilgisayara bağlanılıyor ve GERÇEK sürücüler çekiliyor...";
            lblSendStatus.ForeColor = Color.FromArgb(255, 193, 7);

            // 1. Direct IP Check
            if (targetInput.Contains(".") || targetInput.Contains(":"))
            {
                try
                {
                    string hostStr = targetInput.Contains(":") ? targetInput : $"{targetInput}:8999";
                    string targetUrl = $"http://{hostStr}/api/drives";

                    using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) })
                    {
                        var res = await http.GetAsync(targetUrl);
                        if (res.IsSuccessStatusCode)
                        {
                            string json = await res.Content.ReadAsStringAsync();
                            using (var doc = JsonDocument.Parse(json))
                            {
                                var root = doc.RootElement;
                                string compName = root.TryGetProperty("computerName", out var cn) ? cn.GetString() ?? "" : "";
                                string desk = root.TryGetProperty("desktop", out var d) ? d.GetString() ?? "" : "";
                                string down = root.TryGetProperty("downloads", out var dw) ? dw.GetString() ?? "" : "";
                                var drives = root.GetProperty("drives");

                                cbRemoteTargetFolder.Items.Clear();
                                if (!string.IsNullOrEmpty(desk)) cbRemoteTargetFolder.Items.Add($"🖥️ Karşı Masaüstü ({desk})");
                                if (!string.IsNullOrEmpty(down)) cbRemoteTargetFolder.Items.Add($"📥 Karşı İndirilenler ({down})");

                                foreach (var drive in drives.EnumerateArray())
                                {
                                    string dName = drive.GetString() ?? "";
                                    if (!string.IsNullOrEmpty(dName)) cbRemoteTargetFolder.Items.Add($"💽 Karşı {dName} Sürücüsü");
                                }

                                if (cbRemoteTargetFolder.Items.Count > 0) cbRemoteTargetFolder.SelectedIndex = 0;

                                _isConnectedToRemote = true;
                                _activeRemoteTarget = targetInput;
                                btnSend.Enabled = true;

                                lblSendStatus.Text = $"🟢 BAĞLANDI: {compName} ({targetInput}) - Real Sürücüler Yüklendi!";
                                lblSendStatus.ForeColor = Color.FromArgb(76, 175, 80);
                                MessageBox.Show($"Karşı Bilgisayara Başarıyla Bağlanıldı!\n\nBilgisayar Adı: {compName}\nBulunan Gerçek Sürücüler: {cbRemoteTargetFolder.Items.Count} adet kayıt konumu yüklendi.", "Bağlantı Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }
                    }
                }
                catch { }
            }

            // 2. Relay Code Connection (213.142.159.18:5080)
            try
            {
                string relayUrl = $"ws://213.142.159.18:5080/connect?target={targetInput}";
                using (var ws = new ClientWebSocket())
                {
                    ws.Options.Proxy = null;
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6)))
                    {
                        await ws.ConnectAsync(new Uri(relayUrl), cts.Token);
                        if (ws.State == WebSocketState.Open)
                        {
                            byte[] reqBytes = Encoding.UTF8.GetBytes("{\"type\":\"fs_list\",\"path\":\"\"}");
                            await ws.SendAsync(new ArraySegment<byte>(reqBytes), WebSocketMessageType.Text, true, cts.Token);

                            byte[] buf = new byte[64 * 1024];
                            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
                            if (result.MessageType == WebSocketMessageType.Text && result.Count > 0)
                            {
                                string respJson = Encoding.UTF8.GetString(buf, 0, result.Count);
                                ProcessIncomingJson(respJson);

                                if (_isConnectedToRemote)
                                {
                                    _activeRemoteTarget = targetInput;
                                    MessageBox.Show($"Karşı Bilgisayara Başarıyla Bağlanıldı!\n\nHedef ID: {targetInput}\nBulunan Gerçek Sürücüler: {cbRemoteTargetFolder.Items.Count} adet kayıt konumu yüklendi.", "Bağlantı Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            // 3. General Relay Broadcast Fallback
            _isConnectedToRemote = false;
            btnSend.Enabled = false;

            _ = SendRelayMessageAsync(JsonSerializer.Serialize(new
            {
                type = "fs_list",
                path = "",
                targetCode = targetInput,
                senderCode = _myCode
            }));
            _ = SendRelayMessageAsync(JsonSerializer.Serialize(new
            {
                type = "get_remote_drives",
                targetCode = targetInput,
                senderCode = _myCode
            }));

            lblSendStatus.Text = "Röle sunucusu üzerinden sürücüler isteniyor...";
            btnConnectTarget.Enabled = true;
        }

        private void PanelDropZone_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void PanelDropZone_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        AddPathToSendList(f);
                    }
                }
            }
        }

        private void BtnAddFiles_Click(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Multiselect = true;
                ofd.Title = "Gönderilecek Dosya(ları) Seçin";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    foreach (var f in ofd.FileNames) AddPathToSendList(f);
                }
            }
        }

        private void BtnAddFolder_Click(object? sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Gönderilecek Klasörü Seçin";
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    AddPathToSendList(fbd.SelectedPath);
                }
            }
        }

        private void AddPathToSendList(string path)
        {
            if (string.IsNullOrEmpty(path) || _sendPaths.Contains(path)) return;
            _sendPaths.Add(path);
            
            if (Directory.Exists(path))
            {
                lbSenderFiles.Items.Add($"📁 [KLASÖR] {Path.GetFileName(path)}");
            }
            else if (File.Exists(path))
            {
                long sz = new FileInfo(path).Length;
                lbSenderFiles.Items.Add($"📄 {Path.GetFileName(path)} ({FormatSize(sz)})");
            }
            lblSendStatus.Text = $"{_sendPaths.Count} öge hazır.";
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024.0):F1} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        // RELAY WEBSOCKET ENGINE WITH REGISTER HANDSHAKE
        private async Task ConnectRelayLoop(CancellationToken token)
        {
            string[] relayUrls = new string[] {
                "wss://connect.bigline.com.tr/ws",
                "ws://85.95.231.78:8080/ws"
            };

            int urlIndex = 0;
            while (!token.IsCancellationRequested)
            {
                string url = relayUrls[urlIndex % relayUrls.Length];
                urlIndex++;

                try
                {
                    _wsRelay = new ClientWebSocket();
                    try { _wsRelay.Options.Proxy = null; } catch { }

                    await _wsRelay.ConnectAsync(new Uri(url), token);

                    string cleanCode = _myCode.Replace(" ", "").Trim();
                    byte[] regBytes = Encoding.UTF8.GetBytes($"REGISTER:{cleanCode}");
                    await _wsRelay.SendAsync(new ArraySegment<byte>(regBytes), WebSocketMessageType.Text, true, token);

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        lblReceiveStatus.Text = $"🟢 HTTP Server + Relay Aktif (Kod: {_myCode})";
                        lblReceiveStatus.ForeColor = Color.FromArgb(76, 175, 80);
                    });

                    await ReceiveRelayLoop(_wsRelay, token);
                }
                catch
                {
                    UpdateReceiveStatus($"🟢 HTTP Server Aktif (Port: 8999 / IP: {_localIp})", isError: false);
                    await Task.Delay(4000, token);
                }
            }
        }

        private async Task ReceiveRelayLoop(ClientWebSocket ws, CancellationToken token)
        {
            byte[] buffer = new byte[64 * 1024];
            using (var ms = new MemoryStream())
            {
                while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    ms.SetLength(0);
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                        if (result.MessageType == WebSocketMessageType.Close) break;
                        ms.Write(buffer, 0, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    if (result.MessageType == WebSocketMessageType.Text && ms.Length > 0)
                    {
                        string msgStr = Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                        
                        if (msgStr.StartsWith("ID:"))
                        {
                            string assignedId = msgStr.Substring(3).Trim();
                            _myCode = assignedId;
                            this.BeginInvoke((MethodInvoker)delegate
                            {
                                txtMyCode.Text = $"{_myCode}  ({_localIp})";
                            });
                        }
                        else
                        {
                            ProcessIncomingJson(msgStr);
                        }
                    }
                }
            }
        }

        private void ProcessIncomingJson(string jsonStr)
        {
            try
            {
                using (var doc = JsonDocument.Parse(jsonStr))
                {
                    var root = doc.RootElement;
                    string type = root.GetProperty("type").GetString() ?? "";

                    if (type == "remote_drives_res" || type == "fs_list_res")
                    {
                        var drivesProp = root.TryGetProperty("drives", out var dp) ? dp : default;
                        var foldersProp = root.TryGetProperty("folders", out var fp) ? fp : default;
                        string desk = root.TryGetProperty("desktop", out var dProp) ? (dProp.GetString() ?? "") : "";
                        string down = root.TryGetProperty("downloads", out var dwProp) ? (dwProp.GetString() ?? "") : "";

                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            cbRemoteTargetFolder.Items.Clear();
                            if (!string.IsNullOrEmpty(desk)) cbRemoteTargetFolder.Items.Add($"🖥️ Karşı Masaüstü ({desk})");
                            if (!string.IsNullOrEmpty(down)) cbRemoteTargetFolder.Items.Add($"📥 Karşı İndirilenler ({down})");
                            
                            if (drivesProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var drive in drivesProp.EnumerateArray())
                                {
                                    string dName = drive.GetString() ?? "";
                                    if (!string.IsNullOrEmpty(dName)) cbRemoteTargetFolder.Items.Add($"💽 Karşı {dName} Sürücüsü");
                                }
                            }

                            if (foldersProp.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var folder in foldersProp.EnumerateArray())
                                {
                                    string fName = folder.GetString() ?? "";
                                    if (!string.IsNullOrEmpty(fName)) cbRemoteTargetFolder.Items.Add($"📁 {fName}");
                                }
                            }

                            if (cbRemoteTargetFolder.Items.Count > 0) cbRemoteTargetFolder.SelectedIndex = 0;
                            
                            _isConnectedToRemote = true;
                            btnSend.Enabled = true;
                            lblSendStatus.Text = "🟢 BAĞLANDI! Karşı bilgisayarın GERÇEK sürücüleri yüklendi!";
                            lblSendStatus.ForeColor = Color.FromArgb(76, 175, 80);
                        });
                    }
                }
            }
            catch { }
        }

        private void UpdateReceiveStatus(string text, bool isError = false)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                lblReceiveStatus.Text = text;
                lblReceiveStatus.ForeColor = isError ? Color.FromArgb(244, 67, 54) : Color.FromArgb(76, 175, 80);
            });
        }

        private void UpdateReceiveProgress(string filename, long current, long total)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                lblReceiveStatus.Text = $"📥 Alınıyor: {filename} ({FormatSize(current)} / {FormatSize(total)})";
                lblReceiveStatus.ForeColor = Color.FromArgb(0, 229, 255);
                if (total > 0)
                {
                    int pct = (int)((current * 100) / total);
                    pbReceive.Value = Math.Min(100, Math.Max(0, pct));
                }
            });
        }

        private void AddReceivedItemToListView(string name, string sizeStr)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                var lvi = new ListViewItem(name);
                lvi.SubItems.Add(sizeStr);
                lvi.SubItems.Add(DateTime.Now.ToString("HH:mm:ss"));
                lvReceivedFiles.Items.Insert(0, lvi);
                pbReceive.Value = 100;
            });
        }

        // ================= SENDER LOGIC =================

        private async void BtnSend_Click(object? sender, EventArgs e)
        {
            if (!_isConnectedToRemote)
            {
                MessageBox.Show("Lütfen Önce '🔌 BAĞLAN & SÜRÜCÜLERİ ÇEK' Butonuna Basarak Karşı Bilgisayara Bağlanın!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetInput = txtTargetCode.Text.Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(targetInput))
            {
                MessageBox.Show("Lütfen Hedef Bilgisayarın Kodu veya IP Adresini Girin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_sendPaths.Count == 0)
            {
                MessageBox.Show("Lütfen Gönderilecek En Az 1 Dosya veya Klasör Seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedRemoteFolder = "";
            if (cbRemoteTargetFolder.SelectedItem != null)
            {
                string itemText = cbRemoteTargetFolder.SelectedItem.ToString() ?? "";
                if (itemText.Contains("(") && itemText.EndsWith(")"))
                {
                    int startIdx = itemText.IndexOf("(") + 1;
                    selectedRemoteFolder = itemText.Substring(startIdx, itemText.Length - startIdx - 1);
                }
                else if (itemText.Contains("Karşı ") && itemText.Contains(" Sürücüsü"))
                {
                    string driveName = itemText.Replace("💽 Karşı ", "").Replace(" Sürücüsü", "").Trim();
                    selectedRemoteFolder = driveName;
                }
            }

            btnSend.Enabled = false;
            lblSendStatus.Text = "Gönderim başlatılıyor...";
            lblSendStatus.ForeColor = Color.FromArgb(255, 193, 7);

            bool allSuccess = true;

            try
            {
                await Task.Run(async () =>
                {
                    foreach (var path in _sendPaths)
                    {
                        if (Directory.Exists(path))
                        {
                            string folderName = Path.GetFileName(path);
                            if (string.IsNullOrEmpty(folderName)) folderName = "Klasor";
                            string tempZip = Path.Combine(Path.GetTempPath(), folderName + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".zip");
                            if (File.Exists(tempZip)) File.Delete(tempZip);
                            ZipFile.CreateFromDirectory(path, tempZip);

                            bool ok = await UploadFileHttpAsync(targetInput, tempZip, folderName + ".zip", isFolder: true, remoteTargetDir: selectedRemoteFolder);
                            try { File.Delete(tempZip); } catch { }
                            if (!ok) { allSuccess = false; break; }
                        }
                        else if (File.Exists(path))
                        {
                            string fileName = Path.GetFileName(path);
                            bool ok = await UploadFileHttpAsync(targetInput, path, fileName, isFolder: false, remoteTargetDir: selectedRemoteFolder);
                            if (!ok) { allSuccess = false; break; }
                        }
                    }
                });

                if (allSuccess)
                {
                    lblSendStatus.Text = "🎉 TÜM DOSYALAR KARŞI BİLGİSAYARA BAŞARIYLA AKTARILDI!";
                    lblSendStatus.ForeColor = Color.FromArgb(76, 175, 80);
                    pbSend.Value = 100;
                    MessageBox.Show($"Tüm Dosyalar Karşı Bilgisayara ({selectedRemoteFolder}) Başarıyla Yüklendi ve Karşı Tarafça Onaylandı!", "Tebrikler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    lblSendStatus.Text = "❌ İletim Başarısız Oldu!";
                    lblSendStatus.ForeColor = Color.FromArgb(244, 67, 54);
                    MessageBox.Show("Dosya iletimi sırasında hata oluştu. Lütfen karşı bilgisayarın açık ve bağlı olduğunu kontrol edin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblSendStatus.Text = $"Hata: {ex.Message}";
                lblSendStatus.ForeColor = Color.FromArgb(244, 67, 54);
                MessageBox.Show($"Gönderim sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSend.Enabled = true;
            }
        }

        private async Task<bool> UploadFileHttpAsync(string targetInput, string localPath, string sendName, bool isFolder, string remoteTargetDir)
        {
            try
            {
                string hostStr = targetInput.Contains(":") ? targetInput : $"{targetInput}:8999";
                string uploadUrl = $"http://{hostStr}/api/upload";

                using (var http = new HttpClient { Timeout = TimeSpan.FromHours(1) })
                using (var fs = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var content = new StreamContent(fs))
                {
                    content.Headers.Add("X-File-Name", Uri.EscapeDataString(sendName));
                    content.Headers.Add("X-Target-Dir", Uri.EscapeDataString(remoteTargetDir));
                    content.Headers.Add("X-Is-Folder", isFolder ? "true" : "false");

                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        lblSendStatus.Text = $"📤 Gönderiliyor: {sendName} ({FormatSize(fs.Length)})";
                        lblSendStatus.ForeColor = Color.FromArgb(0, 229, 255);
                    });

                    var response = await http.PostAsync(uploadUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    lblSendStatus.Text = $"HTTP Upload Hatası: {ex.Message}";
                });
            }
            return false;
        }

        private async Task SendRelayMessageAsync(string jsonPayload)
        {
            if (_wsRelay != null && _wsRelay.State == WebSocketState.Open)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(jsonPayload);
                await _wsRelay.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}
