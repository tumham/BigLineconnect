using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BigLineconnect
{
    public class ViewerForm : Form
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

        private readonly string _wsUrl;
        public readonly string _targetId;
        public string ActiveTicketId = "";
        
        private ClientWebSocket? _ws;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        private PictureBox? _pictureBox;
        private DateTime _lastMoveSent = DateTime.MinValue;
        private Point _lastSentMousePos = new Point(-1, -1);
        private System.Windows.Forms.Timer? _clipboardTimer;
        private string _lastClipboardText = "";
        private bool _hasConnectedOnce = false;
        private bool _isReconnecting = false;
        private string _savedPassword = "";
        private Bitmap? _rtCanvas = null;
        private readonly object _rtCanvasLock = new object();
        
        // Custom features forms
        private ClientChatForm? _clientChatForm;
        private FileManagerForm? _fileManagerForm;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private ComboBox? _cbDisplays;
        private Form? _activeRestartDialog;

        // Remote clipboard batch receiving state
        private Button? _btnFloatingClipboard;
        private System.Collections.Generic.List<string> _lastRemoteClipboardFiles = new();
        private FileStream? _incomingFileStream;
        private string? _incomingFileName;
        private bool _incomingIsFolder = false;
        private int _batchTotalFiles = 0;
        private int _batchCurrentFileIndex = 0;
        private long _batchTotalSize = 0;
        private long _batchCurrentSizeProcessed = 0;
        private long _currentFileBytesProcessed = 0;
        private long _currentFileTotalBytes = 0;
        private string? _activeBatchTargetFolder;
        private string _recordDir = "";
        private ulong _lastFrameHash = 0;
        private int _savedFrameIndex = 0;
        private int _totalFramesReceivedCount = 0;
        private int _skippedFramesCount = 0;
        private int _fpsCounter = 0;
        private int _currentFps = 0;
        private DateTime _lastFpsCalcTime = DateTime.Now;
        private DateTime _lastFrameReceivedTime = DateTime.Now;
        private Label? _lblFpsStats;
        private string _connectionStatusText = "";

        private static ulong FastBufferHash(byte[] buffer, int count)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                int step = Math.Max(1, count / 128);
                for (int i = 0; i < count; i += step)
                {
                    h = (h ^ buffer[i]) * 1099511628211UL;
                }
                h = (h ^ (ulong)count) * 1099511628211UL;
                return h;
            }
        }

        public ViewerForm(string wsUrl, string targetId, string savedPassword = "")
        {
            _wsUrl = wsUrl;
            _targetId = targetId;
            _savedPassword = savedPassword;

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
            this.Load += ViewerForm_Load;
            this.FormClosing += ViewerForm_FormClosing;
            this.Deactivate += (s, e) => ReleaseAllRemoteModifiers();
            this.Leave += (s, e) => ReleaseAllRemoteModifiers();
        }
        private void InitializeComponent()
        {
            this.Text = LanguageManager.Get("title_viewer", _targetId) + " - v2.6.1 (Instant Mouse Jump)";
            this.Size = new Size(1024, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.Black;
            this.KeyPreview = true;

            var panelTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.FromArgb(20, 22, 28),
                ForeColor = Color.White
            };
            panelTop.Paint += (s, e) =>
            {
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    panelTop.ClientRectangle,
                    Color.FromArgb(0, 229, 255),
                    Color.FromArgb(213, 0, 249),
                    0F))
                using (var pen = new Pen(brush, 1.5F))
                {
                    e.Graphics.DrawLine(pen, 0, panelTop.Height - 1, panelTop.Width, panelTop.Height - 1);
                }
            };

            var btnChat = new NoFocusButton
            {
                Text = LanguageManager.Get("btn_chat"),
                Location = new Point(10, 7),
                Size = new Size(110, 26)
            };
            ModernUIHelper.ApplyButtonStyle(btnChat, Color.FromArgb(0, 229, 255), Color.FromArgb(0, 176, 255), Color.Black);
            btnChat.Click += (s, e) => OpenChat();

            var btnFileManager = new NoFocusButton
            {
                Text = "🗂️ Dosya Yöneticisi",
                Location = new Point(125, 7),
                Size = new Size(165, 26)
            };
            ModernUIHelper.ApplyButtonStyle(btnFileManager, Color.FromArgb(0, 229, 255), Color.FromArgb(0, 176, 255), Color.Black);
            btnFileManager.Click += (s, e) => OpenFileManager();

            var btnRestart = new NoFocusButton
            {
                Text = "Yeniden Başlat 🔄",
                Location = new Point(295, 7),
                Size = new Size(130, 26)
            };

            ModernUIHelper.ApplyButtonStyle(btnRestart, Color.FromArgb(244, 67, 54), Color.FromArgb(211, 47, 47), Color.White);
            btnRestart.Click += (s, e) => TriggerRemoteRestart();

            var lblDisplay = new Label
            {
                Text = "Ekran:",
                Location = new Point(540, 10),
                Size = new Size(45, 20),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            _cbDisplays = new ComboBox
            {
                Location = new Point(590, 7),
                Size = new Size(130, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(15, 16, 22),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                TabStop = false
            };
            _cbDisplays.Items.Add("Ekran 1 (Ana)");
            _cbDisplays.SelectedIndex = 0;
            _cbDisplays.SelectedIndexChanged += CbDisplays_SelectedIndexChanged;

            panelTop.Controls.Add(btnChat);
            panelTop.Controls.Add(btnFileManager);
            panelTop.Controls.Add(btnRestart);
            
            var btnCad = new NoFocusButton
            {
                Text = "🔑 Ctrl+Alt+Del",
                Location = new Point(415, 7),
                Size = new Size(115, 26)
            };
            ModernUIHelper.ApplyButtonStyle(btnCad, Color.FromArgb(231, 76, 60), Color.FromArgb(192, 57, 43), Color.White);
            btnCad.Click += (s, e) => {
                SendJson("{\"type\":\"send_cad\"}");
            };
            panelTop.Controls.Add(btnCad);

            lblDisplay.Location = new Point(535, 10);
            _cbDisplays.Location = new Point(580, 7);

            panelTop.Controls.Add(lblDisplay);
            panelTop.Controls.Add(_cbDisplays);


            var btnQuality = new NoFocusButton
            {
                Text = "Kalite: Düşük 🎨",
                Location = new Point(725, 7),
                Size = new Size(125, 26)
            };
            ModernUIHelper.ApplyButtonStyle(btnQuality, Color.FromArgb(156, 39, 176), Color.FromArgb(123, 31, 162), Color.White);
            
            var cmsQuality = new ContextMenuStrip();
            var itemLow = new ToolStripMenuItem("Ekonomi (Hızlı Hız)", null, (s, e) => {
                btnQuality.Text = "Kalite: Ekonomi 🎨";
                SendJson("{\"type\":\"set_quality\",\"quality\":60,\"maxDim\":1600}");
            });
            var itemMid = new ToolStripMenuItem("Yüksek Kalite (Dengeli - 1080p)", null, (s, e) => {
                btnQuality.Text = "Kalite: Yüksek 🎨";
                SendJson("{\"type\":\"set_quality\",\"quality\":80,\"maxDim\":1920}");
            });
            var itemHigh = new ToolStripMenuItem("En Yüksek (Pırıl Pırıl Orijinal 4K)", null, (s, e) => {
                btnQuality.Text = "Kalite: Pırıl Pırıl (4K) 🎨";
                SendJson("{\"type\":\"set_quality\",\"quality\":95,\"maxDim\":3840}");
            });
            cmsQuality.Items.Add(itemLow);
            cmsQuality.Items.Add(itemMid);
            cmsQuality.Items.Add(itemHigh);
            btnQuality.Click += (s, e) => cmsQuality.Show(btnQuality, new Point(0, btnQuality.Height));

            bool wallpaperEnabled = false;
            var btnWallpaper = new NoFocusButton
            {
                Text = "Arka Plan 🖼️",
                Location = new Point(855, 7),
                Size = new Size(115, 26)
            };
            ModernUIHelper.ApplyButtonStyle(btnWallpaper, Color.FromArgb(76, 175, 80), Color.FromArgb(56, 142, 60), Color.White);
            btnWallpaper.Click += (s, e) => {
                wallpaperEnabled = !wallpaperEnabled;
                if (wallpaperEnabled)
                {
                    btnWallpaper.Text = "Arka Plan: Canlı 🖼️";
                    ModernUIHelper.ApplyButtonStyle(btnWallpaper, Color.FromArgb(255, 152, 0), Color.FromArgb(245, 124, 0), Color.White);
                    SendJson("{\"type\":\"toggle_wallpaper\",\"enable\":true}");
                }
                else
                {
                    btnWallpaper.Text = "Arka Plan: Siyah 🖼️";
                    ModernUIHelper.ApplyButtonStyle(btnWallpaper, Color.FromArgb(76, 175, 80), Color.FromArgb(56, 142, 60), Color.White);
                    SendJson("{\"type\":\"toggle_wallpaper\",\"enable\":false}");
                }
            };

            bool isOriginalMode = false;
            var btnDisplayMode = new NoFocusButton
            {
                Text = "Görünüm: Sığdır 📐",
                Location = new Point(975, 7),
                Size = new Size(130, 26)
            };
            ModernUIHelper.ApplyButtonStyle(btnDisplayMode, Color.FromArgb(0, 188, 212), Color.FromArgb(0, 151, 167), Color.White);

            btnDisplayMode.Click += (s, e) => {
                isOriginalMode = !isOriginalMode;
                if (isOriginalMode)
                {
                    btnDisplayMode.Text = "Görünüm: 1:1 Net 📐";
                    if (_pictureBox != null) _pictureBox.SizeMode = PictureBoxSizeMode.CenterImage;
                    ModernUIHelper.ApplyButtonStyle(btnDisplayMode, Color.FromArgb(233, 30, 99), Color.FromArgb(194, 24, 91), Color.White);
                }
                else
                {
                    btnDisplayMode.Text = "Görünüm: Sığdır 📐";
                    if (_pictureBox != null) _pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    ModernUIHelper.ApplyButtonStyle(btnDisplayMode, Color.FromArgb(0, 188, 212), Color.FromArgb(0, 151, 167), Color.White);
                }
            };

            panelTop.Controls.Add(btnQuality);
            panelTop.Controls.Add(btnWallpaper);
            panelTop.Controls.Add(btnDisplayMode);

            _lblFpsStats = new Label
            {
                Text = "⚡ -- FPS | -- ms",
                Location = new Point(975, 10),
                Size = new Size(130, 22),
                ForeColor = Color.FromArgb(0, 229, 255),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panelTop.Controls.Add(_lblFpsStats);

            _pictureBox = new DoubleBufferedPictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.FromArgb(12, 14, 20)
            };

            // Custom Paint event to enforce high quality interpolation and render modern overlay when image is null
            _pictureBox.Paint += (s, pe) =>
            {
                pe.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                pe.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                pe.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

                if (_pictureBox.Image == null)
                {
                    var g = pe.Graphics;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    using (var bgBrush = new System.Drawing.Drawing2D.LinearGradientBrush(_pictureBox.ClientRectangle, Color.FromArgb(18, 22, 34), Color.FromArgb(8, 10, 16), 45f))
                    {
                        g.FillRectangle(bgBrush, _pictureBox.ClientRectangle);
                    }

                    int cx = _pictureBox.Width / 2;
                    int cy = _pictureBox.Height / 2;

                    using (var titleFont = new Font("Segoe UI", 16f, FontStyle.Bold))
                    using (var subFont = new Font("Segoe UI", 10.5f, FontStyle.Regular))
                    using (var titleBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
                    using (var subBrush = new SolidBrush(Color.FromArgb(180, 205, 230)))
                    {
                        string titleText = "🚀 BigLineconnect Uzaktan Masaüstü";
                        string subText = string.IsNullOrEmpty(_connectionStatusText) 
                            ? "🔒 Uzak bilgisayara bağlanılıyor, canlı masaüstü aktarımı başlatılıyor...\r\nLütfen bekleyiniz." 
                            : _connectionStatusText;

                        var titleSize = g.MeasureString(titleText, titleFont);
                        var subSize = g.MeasureString(subText, subFont);

                        g.DrawString(titleText, titleFont, titleBrush, cx - (titleSize.Width / 2), cy - 35);
                        g.DrawString(subText, subFont, subBrush, cx - (subSize.Width / 2), cy + 15);
                    }
                }
            };

            // Bind mouse events on picture box
            _pictureBox.MouseDown += PictureBox_MouseDown;
            _pictureBox.MouseUp += PictureBox_MouseUp;
            _pictureBox.MouseMove += PictureBox_MouseMove;
            _pictureBox.MouseWheel += PictureBox_MouseWheel;
            _pictureBox.Click += (s, e) => this.Focus();

            this.Controls.Add(_pictureBox);
            this.Controls.Add(panelTop);
            
            // Enable global form key preview so Ctrl+V works everywhere
            this.KeyPreview = true;
            
            // Clipboard Monitoring Timer
            _clipboardTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            _clipboardTimer.Start();

            // Bind keyboard and focus events on form
            this.KeyDown += ViewerForm_KeyDown;
            this.KeyUp += ViewerForm_KeyUp;
            this.KeyPress += ViewerForm_KeyPress;
            this.Deactivate += (s, e) => SendReleaseAllModifiers();
            this.Leave += (s, e) => SendReleaseAllModifiers();
            if (_pictureBox != null)
            {
                _pictureBox.MouseDoubleClick += PictureBox_MouseDoubleClick;
            }

            // Bind Drag and Drop events
            try
            {
                this.AllowDrop = true;
                _pictureBox.AllowDrop = true;
                this.DragEnter += ViewerForm_DragEnter;
                _pictureBox.DragEnter += ViewerForm_DragEnter;
                this.DragOver += ViewerForm_DragOver;
                _pictureBox.DragOver += ViewerForm_DragOver;
                this.DragDrop += ViewerForm_DragDrop;
                _pictureBox.DragDrop += ViewerForm_DragDrop;
            }
            catch { }
        }

        private async void ViewerForm_Load(object? sender, EventArgs e)
        {
            try
            {
                ChangeWindowMessageFilterEx(this.Handle, WM_DROPFILES_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(this.Handle, WM_COPYDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(this.Handle, WM_COPYGLOBALDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                if (_pictureBox != null && _pictureBox.Handle != IntPtr.Zero)
                {
                    ChangeWindowMessageFilterEx(_pictureBox.Handle, WM_DROPFILES_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                    ChangeWindowMessageFilterEx(_pictureBox.Handle, WM_COPYDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                    ChangeWindowMessageFilterEx(_pictureBox.Handle, WM_COPYGLOBALDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                }
            }
            catch { }

            _ws = new ClientWebSocket();
            try
            {
                await _ws.ConnectAsync(new Uri(_wsUrl), CancellationToken.None);
                
                // Start receiving remote screen stream
                _ = Task.Run(async () => {
                    await ReceiveScreenLoop(_ws, _cts.Token);
                    await ReceiveLoop(_ws, _cts.Token);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Bağlantı kurulamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        private async Task ReceiveScreenLoop(ClientWebSocket ws, CancellationToken token)
        {
            if (Program.RecordConnections)
            {
                try
                {
                    string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    string mainDir = Path.Combine(docPath, "BigLineconnect", "Recordings");
                    _recordDir = Path.Combine(mainDir, $"Session_{_targetId}_{DateTime.Now:yyyyMMdd_HHmmss}");
                    Directory.CreateDirectory(_recordDir);
                    
                    // Write play.html
                    string htmlContent = GetPlayHtmlTemplate();
                    File.WriteAllText(Path.Combine(_recordDir, "play.html"), htmlContent, Encoding.UTF8);
                }
                catch {}
            }
        }

        private readonly byte[] _receiveBuffer = new byte[1024 * 1024 * 2];

        private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    int totalReceived = 0;
                    WebSocketReceiveResult result;
                    
                    do
                    {
                        var segment = new ArraySegment<byte>(_receiveBuffer, totalReceived, _receiveBuffer.Length - totalReceived);
                        result = await ws.ReceiveAsync(segment, token);
                        totalReceived += result.Count;
                    } 
                    while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close) break;

                    if (result.MessageType == WebSocketMessageType.Binary && totalReceived > 0)
                    {
                        _hasConnectedOnce = true;
                        DateTime now = DateTime.Now;
                        double frameLatency = (now - _lastFrameReceivedTime).TotalMilliseconds;
                        _lastFrameReceivedTime = now;

                        _fpsCounter++;
                        _totalFramesReceivedCount++;
                        if ((now - _lastFpsCalcTime).TotalMilliseconds >= 1000)
                        {
                            _currentFps = _fpsCounter;
                            _fpsCounter = 0;
                            _lastFpsCalcTime = now;

                            int displayFps = _currentFps;
                            int displayLatency = (int)Math.Max(5, Math.Min(frameLatency, 999));
                            this.BeginInvoke(new Action(() =>
                            {
                                if (_lblFpsStats != null && !_lblFpsStats.IsDisposed)
                                {
                                    _lblFpsStats.Text = $"⚡ {displayFps} FPS | {displayLatency} ms";
                                }
                                string cleanTitle = LanguageManager.Get("title_viewer", _targetId);
                                if (this.Text != cleanTitle)
                                {
                                    this.Text = cleanTitle;
                                }
                            }));
                        }

                        // Deduplication for frame recording: skip identical static frames
                        ulong currentFrameHash = FastBufferHash(_receiveBuffer, totalReceived);
                        bool isDuplicateFrame = (currentFrameHash == _lastFrameHash);
                        _lastFrameHash = currentFrameHash;

                        if (Program.RecordConnections && !string.IsNullOrEmpty(_recordDir))
                        {
                            _totalFramesReceivedCount++;
                            if (isDuplicateFrame)
                            {
                                _skippedFramesCount++;
                            }
                            else
                            {
                                _savedFrameIndex++;
                                int currentSavedIndex = _savedFrameIndex;
                                string framePath = Path.Combine(_recordDir, $"frame_{currentSavedIndex:D6}.jpg");
                                byte[] frameBytes = new byte[totalReceived];
                                Array.Copy(_receiveBuffer, 0, frameBytes, 0, totalReceived);
                                _ = Task.Run(() =>
                                {
                                    try { File.WriteAllBytes(framePath, frameBytes); } catch { }
                                });
                            }
                        }

                        // Thread-safe isolated frame copy for GDI+ JPEG decoding
                        byte[] isolatedFrame = new byte[totalReceived];
                        Buffer.BlockCopy(_receiveBuffer, 0, isolatedFrame, 0, totalReceived);

                        if (this.WindowState == FormWindowState.Minimized)
                        {
                            // Do NOT process or queue GDI+ BeginInvoke calls while minimized to prevent WinForms UI queue deadlocks!
                            continue;
                        }

                        // Load image frame cleanly (Supports both BigLine-RT tiles and fallback JPEG frames)
                        Image? newImg = null;
                        try
                        {
                            if (BigLineRtEngine.IsBigLineRtPacket(isolatedFrame))
                            {
                                lock (_rtCanvasLock)
                                {
                                    var updatedBmp = BigLineRtEngine.ProcessRtPacket(isolatedFrame, ref _rtCanvas);
                                    if (updatedBmp != null)
                                    {
                                        newImg = new Bitmap(updatedBmp);
                                    }
                                }
                            }
                            else
                            {
                                using (var ms = new MemoryStream(isolatedFrame, 0, totalReceived))
                                using (var tempImg = Image.FromStream(ms))
                                {
                                    newImg = new Bitmap(tempImg);
                                }
                            }
                        }
                        catch { }

                        if (newImg != null)
                        {
                            _pictureBox?.BeginInvoke(new Action(() =>
                            {
                                if (this.WindowState == FormWindowState.Minimized)
                                {
                                    newImg.Dispose();
                                    return;
                                }
                                var oldImg = _pictureBox.Image;
                                _pictureBox.Image = newImg;
                                _pictureBox.Invalidate();
                                oldImg?.Dispose();
                            }));
                        }
                    }
                    else if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(_receiveBuffer, 0, totalReceived);
                        if (message.Contains("ERROR:BUSY"))
                        {
                            if (_hasConnectedOnce)
                            {
                                this.BeginInvoke(new Action(() => {
                                    this.Text = LanguageManager.Get("title_viewer", _targetId) + " (" + LanguageManager.Get("msg_busy_waiting") + ")";
                                }));
                                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Host busy", CancellationToken.None); } catch { }
                                break;
                            }
                            else
                            {
                                MessageBox.Show(LanguageManager.Get("msg_remote_busy"), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                this.BeginInvoke(new Action(this.Close));
                            }
                        }
                        else if (message.Contains("ERROR:ID_NOT_FOUND"))
                        {
                            if (_hasConnectedOnce)
                            {
                                this.BeginInvoke(new Action(() => {
                                    string offlineMsg = LanguageManager.CurrentLanguage == "tr" ? "Uzak makine çevrimdışı, bekleniyor..." : "Remote machine offline, waiting...";
                                    this.Text = LanguageManager.Get("title_viewer", _targetId) + " (" + offlineMsg + ")";
                                }));
                                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Host offline", CancellationToken.None); } catch { }
                                break;
                            }
                            else
                            {
                                MessageBox.Show(LanguageManager.Get("msg_id_not_found"), "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                this.BeginInvoke(new Action(this.Close));
                            }
                        }
                        else if (message == "AUTH_REQUIRED")
                        {
                            _hasConnectedOnce = true;
                            _connectionStatusText = "🔑 Uzak bilgisayar erişim şifresi bekleniyor...\r\nLütfen açılan pencereye karşı bilgisayarın 6 haneli erişim şifresini giriniz.";
                            this.BeginInvoke(new Action(async () => {
                                _pictureBox?.Invalidate();
                                if (string.IsNullOrEmpty(_savedPassword))
                                {
                                    _savedPassword = Prompt.ShowDialog(LanguageManager.Get("msg_enter_password"), LanguageManager.Get("title_password_required"));
                                    if (string.IsNullOrEmpty(_savedPassword))
                                    {
                                        this.Close();
                                        return;
                                    }
                                }
                                byte[] passBytes = Encoding.UTF8.GetBytes("AUTH_PASS:" + _savedPassword);
                                if (_ws != null && _ws.State == WebSocketState.Open)
                                {
                                    await _ws.SendAsync(new ArraySegment<byte>(passBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                                }
                            }));
                        }
                        else if (message == "AUTH_WAITING")
                        {
                            _hasConnectedOnce = true;
                            _connectionStatusText = "🛡️ Müşteri bilgisayarından bağlantı onayı bekleniyor...\r\nLütfen karşı bilgisayarın ekrandaki 'İzin Ver' butonuna basmasını bekleyiniz.";
                            this.BeginInvoke(new Action(() => {
                                _pictureBox?.Invalidate();
                                this.Text = $"ID: {_targetId} - " + LanguageManager.Get("msg_waiting_approval");
                            }));
                        }
                        else if (message == "AUTH_SUCCESS")
                        {
                            _hasConnectedOnce = true;
                            _connectionStatusText = "⚡ Bağlantı doğrulandı, canlı ekran karesi aktarılıyor...";
                            this.BeginInvoke(new Action(() => {
                                _pictureBox?.Invalidate();
                                this.Text = LanguageManager.Get("title_viewer", _targetId);
                            }));
                        }
                        else if (message == "AUTH_FAILED")
                        {
                            _savedPassword = ""; // Clear stored password to prompt user again
                            MessageBox.Show(LanguageManager.Get("auth_failed"), LanguageManager.Get("msg_connection_failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                            this.BeginInvoke(new Action(this.Close));
                        }
                        else if (message == "AUTH_REJECTED")
                        {
                            MessageBox.Show(LanguageManager.Get("auth_rejected"), LanguageManager.Get("msg_connection_failed"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            this.BeginInvoke(new Action(this.Close));
                        }
                        else if (message == "AUTH_TRIAL_EXPIRED")
                        {
                            MessageBox.Show("Uzak bilgisayarın 30 günlük ücretsiz deneme süresi dolmuştur.\r\nBağlantı kurmak için uzak bilgisayardan lisans anahtarını etkinleştirmeniz gerekmektedir.", "Deneme Süresi Doldu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            this.BeginInvoke(new Action(this.Close));
                        }
                        else if (message.StartsWith("{"))
                        {
                            try
                            {
                                using var doc = JsonDocument.Parse(message);
                                var root = doc.RootElement;
                                if (root.TryGetProperty("type", out var typeProp))
                                {
                                    string type = typeProp.GetString() ?? "";
                                    if (type == "displays")
                                    {
                                        var listProp = root.GetProperty("list");
                                        var displayNames = new System.Collections.Generic.List<string>();
                                        foreach (var item in listProp.EnumerateArray())
                                        {
                                            string name = item.GetProperty("name").GetString() ?? "Ekran";
                                            displayNames.Add(name);
                                        }
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (_cbDisplays != null)
                                            {
                                                _cbDisplays.SelectedIndexChanged -= CbDisplays_SelectedIndexChanged;
                                                _cbDisplays.Items.Clear();
                                                foreach (var name in displayNames)
                                                {
                                                    _cbDisplays.Items.Add(name);
                                                }
                                                if (_cbDisplays.Items.Count > 0)
                                                {
                                                    _cbDisplays.SelectedIndex = 0;
                                                }
                                                _cbDisplays.SelectedIndexChanged += CbDisplays_SelectedIndexChanged;
                                            }
                                        }));
                                    }
                                    else if (type == "clipboard")
                                    {
                                        string text = root.GetProperty("text").GetString() ?? "";
                                        this.BeginInvoke(new Action(() => {
                                            _lastClipboardText = text;
                                            if (this.ContainsFocus)
                                            {
                                                Clipboard.SetText(text);
                                            }
                                        }));
                                    }
                                    else if (type == "host_clipboard_files")
                                    {
                                        var filesArray = root.GetProperty("files");
                                        var fileList = new System.Collections.Generic.List<string>();
                                        foreach (var item in filesArray.EnumerateArray())
                                        {
                                            string path = item.GetString() ?? "";
                                            if (!string.IsNullOrEmpty(path)) fileList.Add(path);
                                        }
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            ShowFloatingClipboardButton(fileList);
                                        }));
                                    }
                                    else if (type == "chat")
                                    {
                                        string msgText = root.GetProperty("message").GetString() ?? "";
                                        string senderName = root.GetProperty("sender").GetString() ?? "";
                                        ShowClientChatForm(senderName, msgText);
                                    }
                                    else if (type == "fs_list_res")
                                    {
                                        string msgPayload = message;
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (_fileManagerForm != null && !_fileManagerForm.IsDisposed)
                                            {
                                                try
                                                {
                                                    using var docUI = JsonDocument.Parse(msgPayload);
                                                    _fileManagerForm.Populate(docUI.RootElement);
                                                }
                                                catch { }
                                            }
                                        }));
                                    }
                                    else if (type == "file_error")
                                    {
                                        string errorMsg = root.GetProperty("message").GetString() ?? "Hata olustu.";
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (_fileManagerForm != null && !_fileManagerForm.IsDisposed)
                                            {
                                                _fileManagerForm.HandleDownloadError(errorMsg);
                                            }
                                            else
                                            {
                                                MessageBox.Show(errorMsg, "Dosya/Klasör Transfer Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                            }
                                        }));
                                    }
                                    else if (type == "batch_start")
                                    {
                                        _batchTotalFiles = root.GetProperty("totalFiles").GetInt32();
                                        _batchTotalSize = root.GetProperty("totalSize").GetInt64();
                                        _batchCurrentFileIndex = 0;
                                        _batchCurrentSizeProcessed = 0;

                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (_clientProgressForm != null)
                                            {
                                                try { _clientProgressForm.Close(); } catch { }
                                                _clientProgressForm = null;
                                            }
                                            _clientProgressForm = new FileTransferProgressForm(isSending: false, targetName: "Uzak Bilgisayar");
                                            _clientProgressForm.OnCancel += () =>
                                            {
                                                _ = SendJsonAsync(new { type = "transfer_cancel" });
                                                if (_incomingFileStream != null)
                                                {
                                                    try { _incomingFileStream.Close(); } catch { }
                                                    try { _incomingFileStream.Dispose(); } catch { }
                                                    _incomingFileStream = null;
                                                }
                                                try
                                                {
                                                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                                    string filePath = Path.Combine(desktopPath, _incomingFileName ?? "");
                                                    if (File.Exists(filePath)) File.Delete(filePath);
                                                }
                                                catch { }
                                                _clientProgressForm = null;
                                            };
                                            _clientProgressForm.Show(this);
                                        }));
                                    }
                                    else if (type == "batch_end")
                                    {
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (_clientProgressForm != null && !_clientProgressForm.IsDisposed)
                                            {
                                                _clientProgressForm.Close();
                                                _clientProgressForm = null;
                                            }
                                        }));
                                    }
                                    else if (type == "file_start")
                                    {
                                        string name = root.GetProperty("name").GetString() ?? "download";
                                        _currentFileTotalBytes = root.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0;
                                        _incomingIsFolder = root.TryGetProperty("isFolder", out var folderProp) && folderProp.GetBoolean();
                                        _currentFileBytesProcessed = 0;
                                        _incomingFileName = name;

                                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                        string filePath = Path.Combine(desktopPath, _incomingFileName);
                                        int counter = 1;
                                        string originalName = Path.GetFileNameWithoutExtension(_incomingFileName);
                                        string ext = Path.GetExtension(_incomingFileName);
                                        while (File.Exists(filePath))
                                        {
                                            _incomingFileName = $"{originalName}({counter}){ext}";
                                            filePath = Path.Combine(desktopPath, _incomingFileName);
                                            counter++;
                                        }

                                        try
                                        {
                                            if (_incomingFileStream != null)
                                            {
                                                try { _incomingFileStream.Close(); _incomingFileStream.Dispose(); } catch { }
                                            }
                                            _incomingFileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                                        }
                                        catch (Exception ex)
                                        {
                                            _ = SendJsonAsync(new { type = "file_error", message = $"Yerel dosya olusturulamadi: {ex.Message}" });
                                        }

                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (_clientProgressForm != null && !_clientProgressForm.IsDisposed)
                                            {
                                                _clientProgressForm.UpdateProgress(
                                                     filePath,
                                                     _incomingFileName,
                                                     _batchCurrentFileIndex + 1,
                                                     _batchTotalFiles,
                                                     _currentFileBytesProcessed,
                                                     _currentFileTotalBytes,
                                                     _batchCurrentSizeProcessed,
                                                     _batchTotalSize
                                                 );
                                            }
                                            else if (_fileManagerForm != null && !_fileManagerForm.IsDisposed)
                                            {
                                                _fileManagerForm.StartIncomingDownload(name, _currentFileTotalBytes);
                                            }
                                        }));
                                    }
                                    else if (type == "file_chunk")
                                    {
                                        string chunk = root.GetProperty("chunk").GetString() ?? "";
                                        if (!string.IsNullOrEmpty(chunk))
                                        {
                                            if (_fileManagerForm != null && !_fileManagerForm.IsDisposed)
                                            {
                                                _fileManagerForm.WriteDownloadChunk(chunk);
                                            }

                                            if (_incomingFileStream != null)
                                            {
                                                try
                                                {
                                                    byte[] data = Convert.FromBase64String(chunk);
                                                    _incomingFileStream.Write(data, 0, data.Length);
                                                    _currentFileBytesProcessed += data.Length;
                                                    _batchCurrentSizeProcessed += data.Length;

                                                    this.BeginInvoke(new Action(() =>
                                                    {
                                                        if (_clientProgressForm != null && !_clientProgressForm.IsDisposed)
                                                        {
                                                            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                                            string filePath = Path.Combine(desktopPath, _incomingFileName ?? "file");
                                                            _clientProgressForm.UpdateProgress(
                                                                filePath,
                                                                _incomingFileName ?? "file",
                                                                _batchCurrentFileIndex + 1,
                                                                _batchTotalFiles,
                                                                _currentFileBytesProcessed,
                                                                _currentFileTotalBytes,
                                                                _batchCurrentSizeProcessed,
                                                                _batchTotalSize
                                                            );
                                                        }
                                                    }));
                                                }
                                                catch { }
                                            }
                                        }
                                    }
                                    else if (type == "file_end")
                                    {
                                        if (_incomingFileStream != null)
                                        {
                                            try
                                            {
                                                _incomingFileStream.Flush();
                                                _incomingFileStream.Close();
                                                _incomingFileStream.Dispose();
                                            }
                                            catch { }
                                            _incomingFileStream = null;

                                            if (_incomingIsFolder && !string.IsNullOrEmpty(_incomingFileName))
                                            {
                                                try
                                                {
                                                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                                    string zipPath = Path.Combine(desktopPath, _incomingFileName);
                                                    string destDir = Path.Combine(desktopPath, Path.GetFileNameWithoutExtension(_incomingFileName));
                                                    if (File.Exists(zipPath))
                                                    {
                                                        try
                                                        {
                                                            string extractDir = Path.Combine(desktopPath, Path.GetFileNameWithoutExtension(_incomingFileName));
                                                            int counter = 1;
                                                            string originalDir = extractDir;
                                                            while (Directory.Exists(extractDir))
                                                            {
                                                                extractDir = $"{originalDir}({counter})";
                                                                counter++;
                                                            }

                                                            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDir);
                                                        }
                                                        catch { }
                                                        finally
                                                        {
                                                            try { File.Delete(zipPath); } catch { }
                                                        }
                                                    }
                                                }
                                                catch { }

                                                _batchCurrentFileIndex++;
                                                if (_clientProgressForm != null && !_clientProgressForm.IsDisposed)
                                                {
                                                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                                                    string filePath = Path.Combine(desktopPath, _incomingFileName ?? "file");
                                                    _clientProgressForm.UpdateProgress(
                                                        filePath,
                                                        _incomingFileName ?? "file",
                                                        _batchCurrentFileIndex,
                                                        _batchTotalFiles,
                                                        _currentFileBytesProcessed,
                                                        _currentFileTotalBytes,
                                                        _batchCurrentSizeProcessed,
                                                        _batchTotalSize
                                                    );
                                                }
                                            }
                                            else if (_fileManagerForm != null && !_fileManagerForm.IsDisposed)
                                            {
                                                _fileManagerForm.EndIncomingDownload();
                                            }
                                        }
                                    }
                                    else if (type == "transfer_cancel")
                                    {
                                        _cancelActiveTransfer = true;
                                        this.BeginInvoke(new Action(() =>
                                        {
                                            if (_clientProgressForm != null && !_clientProgressForm.IsDisposed)
                                            {
                                                _clientProgressForm.Close();
                                                _clientProgressForm = null;
                                            }
                                        }));
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception) { }
            finally
            {
                if (_hasConnectedOnce && !token.IsCancellationRequested)
                {
                    StartReconnectionLoop();
                }
                else
                {
                    this.BeginInvoke(new Action(this.Close));
                }
            }
        }

        private async void StartReconnectionLoop()
        {
            if (_isReconnecting) return;
            _isReconnecting = true;

            if (_activeRestartDialog != null)
            {
                try
                {
                    this.BeginInvoke(new Action(() => {
                        _activeRestartDialog?.Close();
                    }));
                }
                catch { }
            }

            this.BeginInvoke(new Action(() => {
                this.Text = $"Masaüstü Bağlantısı - ID: {_targetId} (Bağlantı koptu, yeniden bağlanılıyor...)";
            }));

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_ws != null)
                    {
                        try { _ws.Dispose(); } catch { }
                    }
                    _ws = new ClientWebSocket();
                    _ws.Options.Proxy = null;

                    await _ws.ConnectAsync(new Uri(_wsUrl), _cts.Token);
                    
                    this.BeginInvoke(new Action(() => {
                        this.Text = $"Masaüstü Bağlantısı - ID: {_targetId}";
                    }));

                    _isReconnecting = false;
                    _ = Task.Run(async () => {
                        await ReceiveScreenLoop(_ws, _cts.Token);
                        await ReceiveLoop(_ws, _cts.Token);
                    });
                    return;
                }
                catch
                {
                    // Wait 3 seconds before retrying
                    try { await Task.Delay(3000, _cts.Token); } catch { break; }
                }
            }
            _isReconnecting = false;
        }

        private void LogClient(string message)
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string dir = Path.Combine(programData, "BigLineconnect");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "client_log.txt");

                if (File.Exists(path) && new FileInfo(path).Length > 5 * 1024 * 1024)
                {
                    File.WriteAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Log truncated due to size.\r\n");
                }

                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Client] {message}\r\n");
            }
            catch { }
        }

        public async Task SendJsonAsync(object data)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;

            await _sendSemaphore.WaitAsync();
            try
            {
                if (_ws.State == WebSocketState.Open)
                {
                    string json = data is string str ? str : SafeSerialize(data);
                    
                    if (!json.Contains("\"type\":\"move\"") && !json.Contains("\"chunk\":") && !json.Contains("\"data\":"))
                    {
                        LogClient($"Sending: {json}");
                    }
                    
                    byte[] bytes = Encoding.UTF8.GetBytes(json);
                    await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                LogClient($"Send error: {ex.Message}");
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        public void SendJson(object data)
        {
            _ = Task.Run(() => SendJsonAsync(data));
        }

        private void ShowFloatingClipboardButton(System.Collections.Generic.List<string> fileList)
        {
            _lastRemoteClipboardFiles = fileList;

            if (fileList == null || fileList.Count == 0)
            {
                if (_btnFloatingClipboard != null) _btnFloatingClipboard.Visible = false;
                return;
            }

            if (_btnFloatingClipboard == null)
            {
                _btnFloatingClipboard = new Button
                {
                    Size = new Size(260, 42),
                    BackColor = Color.FromArgb(10, 11, 16),
                    ForeColor = Color.FromArgb(0, 229, 255), // Cyan glowing text
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                _btnFloatingClipboard.FlatAppearance.BorderColor = Color.FromArgb(0, 229, 255);
                _btnFloatingClipboard.FlatAppearance.BorderSize = 1;
                _btnFloatingClipboard.Location = new Point((this.ClientSize.Width - _btnFloatingClipboard.Width) / 2, 45);
                _btnFloatingClipboard.Anchor = AnchorStyles.Top;
                _btnFloatingClipboard.Click += BtnFloatingClipboard_Click;

                this.Controls.Add(_btnFloatingClipboard);
                _btnFloatingClipboard.BringToFront();
            }

            _btnFloatingClipboard.Text = $"Uzak Panodan İndir ({fileList.Count} Öğe)";
            _btnFloatingClipboard.Visible = true;
            _btnFloatingClipboard.BringToFront();
        }

        private async void BtnFloatingClipboard_Click(object? sender, EventArgs e)
        {
            if (_btnFloatingClipboard != null) _btnFloatingClipboard.Visible = false;
            
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Uzak bilgisayardan indirilecek dosyaların kaydedileceği yerel dizini seçin:";
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    _activeBatchTargetFolder = fbd.SelectedPath;
                }
                else
                {
                    return; // Canceled
                }
            }

            if (_lastRemoteClipboardFiles != null && _lastRemoteClipboardFiles.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append("{\"type\":\"trigger_host_clipboard_send\",\"files\":[");
                for (int i = 0; i < _lastRemoteClipboardFiles.Count; i++)
                {
                    sb.Append($"\"{EscapeJson(_lastRemoteClipboardFiles[i])}\"");
                    if (i < _lastRemoteClipboardFiles.Count - 1) sb.Append(",");
                }
                sb.Append("]}");
                await SendJsonAsync(sb.ToString());
            }
        }

        private void CbDisplays_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cbDisplays == null || _cbDisplays.SelectedIndex < 0) return;
            SendJson($"{{\"type\":\"select_display\",\"index\":{_cbDisplays.SelectedIndex}}}");
        }

        private (double x, double y) GetNormalizedMousePos(MouseEventArgs e, PictureBox box)
        {
            if (box.Image == null || box.Width <= 0 || box.Height <= 0)
                return (0, 0);

            int imgWidth = box.Image.Width;
            int imgHeight = box.Image.Height;

            if (box.SizeMode == PictureBoxSizeMode.CenterImage)
            {
                int offsetX = (box.Width - imgWidth) / 2;
                int offsetY = (box.Height - imgHeight) / 2;

                int mouseX = e.X - offsetX;
                int mouseY = e.Y - offsetY;

                double normX = (double)mouseX / imgWidth;
                double normY = (double)mouseY / imgHeight;

                return (Math.Max(0, Math.Min(1, normX)), Math.Max(0, Math.Min(1, normY)));
            }
            else // Zoom mode
            {
                double imgAspect = (double)imgWidth / imgHeight;
                double boxAspect = (double)box.Width / box.Height;

                double renderedWidth, renderedHeight, offsetX, offsetY;

                if (boxAspect > imgAspect)
                {
                    renderedHeight = box.Height;
                    renderedWidth = box.Height * imgAspect;
                    offsetX = (box.Width - renderedWidth) / 2;
                    offsetY = 0;
                }
                else
                {
                    renderedWidth = box.Width;
                    renderedHeight = box.Width / imgAspect;
                    offsetX = 0;
                    offsetY = (box.Height - renderedHeight) / 2;
                }

                double mouseX = e.X - offsetX;
                double mouseY = e.Y - offsetY;

                double normX = mouseX / renderedWidth;
                double normY = mouseY / renderedHeight;

                return (Math.Max(0, Math.Min(1, normX)), Math.Max(0, Math.Min(1, normY)));
            }
        }

        private void SendFastMouseMove(double x, double y)
        {
            ushort ux = (ushort)(Math.Max(0, Math.Min(1, x)) * 65535);
            ushort uy = (ushort)(Math.Max(0, Math.Min(1, y)) * 65535);

            byte[] pkt = new byte[5];
            pkt[0] = 0x4D; // 'M' for Move
            BitConverter.TryWriteBytes(new Span<byte>(pkt, 1, 2), ux);
            BitConverter.TryWriteBytes(new Span<byte>(pkt, 3, 2), uy);

            if (P2pDirectEngine.IsP2pConnected)
            {
                P2pDirectEngine.SendP2pPacket(pkt);
            }
            else if (_ws != null && _ws.State == WebSocketState.Open)
            {
                _ws.SendAsync(new ArraySegment<byte>(pkt), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        private void PictureBox_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_pictureBox == null) return;
            if (e.Location == _lastSentMousePos) return;
            int throttleMs = 10; // 100 FPS ultra-fast mouse tracking!
            if (DateTime.Now - _lastMoveSent < TimeSpan.FromMilliseconds(throttleMs)) return;
            _lastMoveSent = DateTime.Now;
            _lastSentMousePos = e.Location;

            var (x, y) = GetNormalizedMousePos(e, _pictureBox);
            SendFastMouseMove(x, y);
        }

        private void PictureBox_MouseDown(object? sender, MouseEventArgs e)
        {
            if (_pictureBox == null) return;
            
            // Focus form to ensure key capture works
            this.Focus();

            var (x, y) = GetNormalizedMousePos(e, _pictureBox);

            string button = "left";
            if (e.Button == MouseButtons.Right) button = "right";
            else if (e.Button == MouseButtons.Middle) button = "middle";

            SendJson($"{{\"type\":\"click\",\"button\":\"{button}\",\"action\":\"down\",\"x\":{x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        }

        private void PictureBox_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_pictureBox == null) return;

            var (x, y) = GetNormalizedMousePos(e, _pictureBox);

            string button = "left";
            if (e.Button == MouseButtons.Right) button = "right";
            else if (e.Button == MouseButtons.Middle) button = "middle";

            SendJson($"{{\"type\":\"click\",\"button\":\"{button}\",\"action\":\"up\",\"x\":{x.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"y\":{y.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        }

        private void PictureBox_MouseWheel(object? sender, MouseEventArgs e)
        {
            // e.Delta is usually +120 or -120
            SendJson($"{{\"type\":\"scroll\",\"deltaY\":{e.Delta}}}");
        }

        private void ViewerForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                try
                {
                    if (Clipboard.ContainsFileDropList())
                    {
                        var fileDrop = Clipboard.GetFileDropList();
                        if (fileDrop != null && fileDrop.Count > 0)
                        {
                            var files = new System.Collections.Generic.List<string>();
                            foreach (string path in fileDrop)
                            {
                                if (!string.IsNullOrEmpty(path)) files.Add(path);
                            }
                            if (files.Count > 0)
                            {
                                _ = Task.Run(async () => await SendPathsBatchAsync(files));
                                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                                return;
                            }
                        }
                    }
                    else if (Clipboard.ContainsText())
                    {
                        string txt = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(txt))
                        {
                            _lastClipboardText = txt;
                            SendJson($"{{\"type\":\"clipboard\",\"text\":\"{EscapeJson(txt)}\"}}");
                        }
                    }
                }
                catch { }

                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"v\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"v\",\"action\":\"up\"}");
                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                return;
            }

            if (e.Control && e.KeyCode == Keys.C)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"c\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"c\",\"action\":\"up\"}");
                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                return;
            }

            if (e.Control && e.KeyCode == Keys.Z)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"z\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"z\",\"action\":\"up\"}");
                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                return;
            }

            if (e.Control && e.KeyCode == Keys.A)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;

                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"a\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"a\",\"action\":\"up\"}");
                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                return;
            }

            bool isSpecial = IsSpecialKey(e.KeyCode);
            bool isShortcut = e.Control || e.Alt;

            if (isSpecial || isShortcut)
            {
                string keyName = MapKey(e.KeyCode);
                if (!string.IsNullOrEmpty(keyName))
                {
                    SendJson($"{{\"type\":\"key\",\"key\":\"{keyName}\",\"action\":\"down\"}}");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys baseKey = keyData & Keys.KeyCode;
            bool hasAlt = keyData.HasFlag(Keys.Alt);
            bool hasControl = keyData.HasFlag(Keys.Control);
            bool hasShift = keyData.HasFlag(Keys.Shift);

            // 1. Intercept TAB & Shift+TAB (Single clean packet to Host)
            if (baseKey == Keys.Tab)
            {
                if (hasShift) SendJson("{\"type\":\"key\",\"key\":\"shift\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"tab\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"tab\",\"action\":\"up\"}");
                if (hasShift) SendJson("{\"type\":\"key\",\"key\":\"shift\",\"action\":\"up\"}");
                return true;
            }

            // 2. Intercept ALT shortcuts (like ALT+D, ALT+F4, ALT+ENTER, ALT+A, etc.)
            if (hasAlt)
            {
                string kName = MapKey(baseKey);
                if (!string.IsNullOrEmpty(kName) && kName != "alt")
                {
                    SendJson("{\"type\":\"key\",\"key\":\"alt\",\"action\":\"down\"}");
                    SendJson($"{{\"type\":\"key\",\"key\":\"{kName}\",\"action\":\"down\"}}");
                    SendJson($"{{\"type\":\"key\",\"key\":\"{kName}\",\"action\":\"up\"}}");
                    SendJson("{\"type\":\"key\",\"key\":\"alt\",\"action\":\"up\"}");
                    return true;
                }
            }

            // 3. Intercept F1-F12 keys (Mikro ERP F10 Save, F9 Lookup, F1 Help, etc.)
            if (baseKey >= Keys.F1 && baseKey <= Keys.F12)
            {
                string fName = MapKey(baseKey);
                if (!string.IsNullOrEmpty(fName))
                {
                    if (hasControl) SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"down\"}");
                    if (hasShift) SendJson("{\"type\":\"key\",\"key\":\"shift\",\"action\":\"down\"}");
                    SendJson($"{{\"type\":\"key\",\"key\":\"{fName}\",\"action\":\"down\"}}");
                    SendJson($"{{\"type\":\"key\",\"key\":\"{fName}\",\"action\":\"up\"}}");
                    if (hasShift) SendJson("{\"type\":\"key\",\"key\":\"shift\",\"action\":\"up\"}");
                    if (hasControl) SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                    return true;
                }
            }

            // 4. Intercept Enter / Return
            if (baseKey == Keys.Enter || baseKey == Keys.Return)
            {
                SendJson("{\"type\":\"key\",\"key\":\"enter\",\"action\":\"down\"}");
                SendJson("{\"type\":\"key\",\"key\":\"enter\",\"action\":\"up\"}");
                return true;
            }

            // 5. Intercept Escape, Arrows, Delete, Backspace, Home, End, PageUp, PageDown, Insert
            if (baseKey == Keys.Escape || baseKey == Keys.Delete || baseKey == Keys.Back || baseKey == Keys.Insert ||
                baseKey == Keys.Left || baseKey == Keys.Right || baseKey == Keys.Up || baseKey == Keys.Down ||
                baseKey == Keys.Home || baseKey == Keys.End || baseKey == Keys.PageUp || baseKey == Keys.PageDown)
            {
                string navKey = MapKey(baseKey);
                if (!string.IsNullOrEmpty(navKey))
                {
                    if (hasControl) SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"down\"}");
                    if (hasShift) SendJson("{\"type\":\"key\",\"key\":\"shift\",\"action\":\"down\"}");
                    SendJson($"{{\"type\":\"key\",\"key\":\"{navKey}\",\"action\":\"down\"}}");
                    SendJson($"{{\"type\":\"key\",\"key\":\"{navKey}\",\"action\":\"up\"}}");
                    if (hasShift) SendJson("{\"type\":\"key\",\"key\":\"shift\",\"action\":\"up\"}");
                    if (hasControl) SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ViewerForm_KeyUp(object? sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.V || e.KeyCode == Keys.Z || e.KeyCode == Keys.A))
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            bool isSpecial = IsSpecialKey(e.KeyCode);
            bool isShortcut = e.Control || e.Alt;

            if (isSpecial || isShortcut)
            {
                string keyName = MapKey(e.KeyCode);
                if (!string.IsNullOrEmpty(keyName))
                {
                    SendJson($"{{\"type\":\"key\",\"key\":\"{keyName}\",\"action\":\"up\"}}");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            }
        }

        private void PictureBox_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            // MouseDown and MouseUp sequence already streams native clean clicks to host.
        }

        private void SendReleaseAllModifiers()
        {
            SendJson("{\"type\":\"release_modifiers\"}");
        }

        private void ViewerForm_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= 32)
            {
                SendJson($"{{\"type\":\"char\",\"value\":\"{EscapeJson(e.KeyChar.ToString())}\"}}");
                e.Handled = true;
            }
        }

        private bool IsSpecialKey(Keys key)
        {
            Keys baseKey = key & Keys.KeyCode;
            switch (baseKey)
            {
                case Keys.Enter:
                case Keys.Back:
                case Keys.Tab:
                case Keys.Escape:
                case Keys.Space:
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.Control:
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.Shift:
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.Alt:
                case Keys.Left:
                case Keys.Up:
                case Keys.Right:
                case Keys.Down:
                case Keys.Delete:
                case Keys.Insert:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.F1: case Keys.F2: case Keys.F3: case Keys.F4:
                case Keys.F5: case Keys.F6: case Keys.F7: case Keys.F8:
                case Keys.F9: case Keys.F10: case Keys.F11: case Keys.F12:
                    return true;
                default:
                    return false;
            }
        }

        private string MapKey(Keys key)
        {
            Keys baseKey = key & Keys.KeyCode;
            switch (baseKey)
            {
                case Keys.Enter: return "enter";
                case Keys.Back: return "backspace";
                case Keys.Tab: return "tab";
                case Keys.Escape: return "escape";
                case Keys.Space: return "space";
                case Keys.ControlKey:
                case Keys.LControlKey:
                case Keys.RControlKey:
                case Keys.Control:
                    return "control";
                case Keys.ShiftKey:
                case Keys.LShiftKey:
                case Keys.RShiftKey:
                case Keys.Shift:
                    return "shift";
                case Keys.Menu:
                case Keys.LMenu:
                case Keys.RMenu:
                case Keys.Alt:
                    return "alt";
                case Keys.Left: return "arrowleft";
                case Keys.Up: return "arrowup";
                case Keys.Right: return "arrowright";
                case Keys.Down: return "arrowdown";
                case Keys.Delete: return "delete";
                case Keys.Insert: return "insert";
                case Keys.Home: return "home";
                case Keys.End: return "end";
                case Keys.PageUp: return "pageup";
                case Keys.PageDown: return "pagedown";
                case Keys.F1: return "f1";
                case Keys.F2: return "f2";
                case Keys.F3: return "f3";
                case Keys.F4: return "f4";
                case Keys.F5: return "f5";
                case Keys.F6: return "f6";
                case Keys.F7: return "f7";
                case Keys.F8: return "f8";
                case Keys.F9: return "f9";
                case Keys.F10: return "f10";
                case Keys.F11: return "f11";
                case Keys.F12: return "f12";
                default:
                    if (baseKey >= Keys.A && baseKey <= Keys.Z)
                    {
                        return ((char)baseKey).ToString().ToLower();
                    }
                    if (baseKey >= Keys.D0 && baseKey <= Keys.D9)
                    {
                        return ((char)('0' + (baseKey - Keys.D0))).ToString();
                    }
                    return "";
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private void ReleaseAllRemoteModifiers()
        {
            try
            {
                SendJson("{\"type\":\"key\",\"key\":\"control\",\"action\":\"up\"}");
                SendJson("{\"type\":\"key\",\"key\":\"shift\",\"action\":\"up\"}");
                SendJson("{\"type\":\"key\",\"key\":\"alt\",\"action\":\"up\"}");
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern void DragAcceptFiles(IntPtr hWnd, bool fAccept);

        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern uint DragQueryFile(IntPtr hDrop, uint iFile, System.Text.StringBuilder? lpszFile, uint cch);

        [System.Runtime.InteropServices.DllImport("shell32.dll")]
        private static extern void DragFinish(IntPtr hDrop);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ChangeWindowMessageFilter(uint message, uint flags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint msg, uint action, IntPtr pObsFilter);

        private const uint MSGFLT_ALLOW = 1;
        private const uint MSGFLT_ADD = 1;
        private const uint WM_DROPFILES_MSG = 0x0233;
        private const uint WM_COPYDATA_MSG = 0x004A;
        private const uint WM_COPYGLOBALDATA_MSG = 0x0049;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                AddClipboardFormatListener(this.Handle);
            }
            catch { }

            try
            {
                DragAcceptFiles(this.Handle, true);
                if (_pictureBox != null && _pictureBox.IsHandleCreated)
                {
                    DragAcceptFiles(_pictureBox.Handle, true);
                }

                ChangeWindowMessageFilter(WM_DROPFILES_MSG, MSGFLT_ADD);
                ChangeWindowMessageFilter(WM_COPYDATA_MSG, MSGFLT_ADD);
                ChangeWindowMessageFilter(WM_COPYGLOBALDATA_MSG, MSGFLT_ADD);
                ChangeWindowMessageFilterEx(this.Handle, WM_DROPFILES_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(this.Handle, WM_COPYDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
                ChangeWindowMessageFilterEx(this.Handle, WM_COPYGLOBALDATA_MSG, MSGFLT_ALLOW, IntPtr.Zero);
            }
            catch { }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_CLIPBOARDUPDATE = 0x031D;
            const int WM_DROPFILES = 0x0233;

            if (m.Msg == WM_CLIPBOARDUPDATE)
            {
                OnLocalClipboardChanged();
            }
            else if (m.Msg == WM_DROPFILES)
            {
                try
                {
                    IntPtr hDrop = m.WParam;
                    uint count = DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                    var fileList = new System.Collections.Generic.List<string>();
                    for (uint i = 0; i < count; i++)
                    {
                        var sb = new System.Text.StringBuilder(512);
                        DragQueryFile(hDrop, i, sb, (uint)sb.Capacity);
                        string path = sb.ToString();
                        if (!string.IsNullOrEmpty(path))
                        {
                            fileList.Add(path);
                        }
                    }
                    DragFinish(hDrop);

                    if (fileList.Count > 0)
                    {
                        _ = Task.Run(async () => await SendPathsBatchAsync(fileList));
                    }
                }
                catch (Exception ex)
                {
                    Program.Log($"WM_DROPFILES hatası: {ex.Message}");
                }
            }
            base.WndProc(ref m);
        }

        private void OnLocalClipboardChanged()
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText();
                        if (text != _lastClipboardText && !string.IsNullOrEmpty(text))
                        {
                            _lastClipboardText = text;
                            SendJson($"{{\"type\":\"clipboard\",\"text\":\"{EscapeJson(text)}\"}}");
                        }
                    }
                }
                catch { }
            }
        }

        private void ClipboardTimer_Tick(object? sender, EventArgs e)
        {
            OnLocalClipboardChanged();
        }

        private void ViewerForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_clipboardTimer != null)
            {
                _clipboardTimer.Stop();
                _clipboardTimer.Dispose();
            }

            _cts.Cancel();
            if (_ws != null)
            {
                try
                {
                    _ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Form Closed", CancellationToken.None);
                }
                catch { }
                _ws.Dispose();
            }

            string ticketIdToResolve = !string.IsNullOrEmpty(this.ActiveTicketId) ? this.ActiveTicketId : Program.ActiveTicketId;
            if (!string.IsNullOrEmpty(ticketIdToResolve))
            {
                string ticketId = ticketIdToResolve;
                this.ActiveTicketId = "";
                Program.ActiveTicketId = ""; // Reset immediately
                
                using (var resolutionForm = new SupportResolutionForm(_targetId))
                {
                    if (resolutionForm.ShowDialog() == DialogResult.OK)
                    {
                        string status = resolutionForm.SelectedStatus;
                        string notes = resolutionForm.TechnicianNotes;
                        
                        Task.Run(async () =>
                        {
                            try
                            {
                                string httpUrlBase = _wsUrl.Replace("ws://", "http://").Replace("wss://", "https://");
                                int idx = httpUrlBase.IndexOf("/connect-client");
                                if (idx > 0)
                                {
                                    string resolveUrl = httpUrlBase.Substring(0, idx) + "/api/support/resolve";
                                    string updateUrl = httpUrlBase.Substring(0, idx) + "/api/support/history/update";
                                    
                                    using (var client = new System.Net.Http.HttpClient())
                                    {
                                        // 1. Move ticket from Active to SupportHistory
                                        var json1 = $"{{\"id\":\"{Program.EscapeJson(ticketId)}\"}}";
                                        var content1 = new System.Net.Http.StringContent(json1, Encoding.UTF8, "application/json");
                                        await client.PostAsync(resolveUrl, content1);

                                        // 2. Update status and technician notes in SupportHistory
                                        var json2 = $"{{\"id\":\"{Program.EscapeJson(ticketId)}\",\"status\":\"{Program.EscapeJson(status)}\",\"notes\":\"{Program.EscapeJson(notes)}\"}}";
                                        var content2 = new System.Net.Http.StringContent(json2, Encoding.UTF8, "application/json");
                                        await client.PostAsync(updateUrl, content2);
                                    }

                                    MainWindow.Instance?.Invoke((MethodInvoker)delegate
                                    {
                                        MainWindow.Instance?.RefreshSupportTickets();
                                        MainWindow.Instance?.RefreshCrmHistory();
                                    });
                                }
                            }
                            catch { }
                        });
                    }
                }
            }
        }

        public class SupportResolutionForm : Form
        {
            private ComboBox cbStatus;
            private TextBox txtNotes;
            private Button btnSave;
            private Button btnSkip;

            public string SelectedStatus => cbStatus.SelectedItem?.ToString() ?? "Çözüldü";
            public string TechnicianNotes => txtNotes.Text.Trim();

            public SupportResolutionForm(string targetId)
            {
                this.Text = $"Destek Oturumu Raporu (ID: {targetId})";
                this.Size = new Size(420, 280);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Color.FromArgb(10, 11, 16);
                this.ForeColor = Color.White;

                var lblStatus = new Label
                {
                    Text = "Destek Talebi Durumu:",
                    Location = new Point(20, 20),
                    Size = new Size(360, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 229, 255)
                };
                this.Controls.Add(lblStatus);

                cbStatus = new ComboBox
                {
                    Location = new Point(20, 45),
                    Size = new Size(360, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(20, 22, 30),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 9.5F),
                    FlatStyle = FlatStyle.Flat
                };
                cbStatus.Items.Add("Çözüldü");
                cbStatus.Items.Add("Çözülemedi");
                cbStatus.Items.Add("Takipte");
                cbStatus.SelectedIndex = 0;
                this.Controls.Add(cbStatus);

                var lblNotes = new Label
                {
                    Text = "Destek Uzmanı İşlem Notları:",
                    Location = new Point(20, 85),
                    Size = new Size(360, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(0, 229, 255)
                };
                this.Controls.Add(lblNotes);

                txtNotes = new TextBox
                {
                    Location = new Point(20, 110),
                    Size = new Size(360, 70),
                    Multiline = true,
                    BackColor = Color.FromArgb(20, 22, 30),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9.5F)
                };
                this.Controls.Add(txtNotes);

                btnSave = new Button
                {
                    Text = "Kaydet",
                    Location = new Point(90, 195),
                    Size = new Size(110, 32),
                    BackColor = Color.FromArgb(0, 229, 255),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSave.FlatAppearance.BorderSize = 0;
                btnSave.Click += (s, e) => {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(btnSave);

                btnSkip = new Button
                {
                    Text = "Raporlama",
                    Location = new Point(210, 195),
                    Size = new Size(110, 32),
                    BackColor = Color.FromArgb(30, 35, 45),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSkip.FlatAppearance.BorderColor = Color.Gray;
                btnSkip.Click += (s, e) => {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };
                this.Controls.Add(btnSkip);
            }
        }

        private void ViewerForm_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ViewerForm_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ViewerForm_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    _ = Task.Run(async () => await SendPathsBatchAsync(new System.Collections.Generic.List<string>(files)));
                }
            }
        }

        private bool _cancelActiveTransfer = false;
        private FileTransferProgressForm? _clientProgressForm;

        public class FileBatchItem
        {
            public string LocalPath { get; set; } = "";
            public string Name { get; set; } = "";
            public long Size { get; set; }
            public bool IsFolder { get; set; }
            public string? TempZipPath { get; set; }
        }

        public async Task SendPathsBatchAsync(System.Collections.Generic.List<string> paths, bool promptFolder = false, string overrideTargetFolder = "")
        {
            _cancelActiveTransfer = false;

            string targetFolder = !string.IsNullOrEmpty(overrideTargetFolder) ? overrideTargetFolder : "DESKTOP";

            if (promptFolder)
            {
                bool dialogCanceled = false;

                this.Invoke((MethodInvoker)delegate
                {
                    using (var dialog = new RemoteFolderSelectionDialog())
                    {
                        if (dialog.ShowDialog(this) == DialogResult.OK)
                        {
                            targetFolder = dialog.SelectedPath;
                        }
                        else
                        {
                            dialogCanceled = true;
                        }
                    }
                });

                if (dialogCanceled) return;
            }

            var items = new System.Collections.Generic.List<FileBatchItem>();
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path)) continue;

                if (Directory.Exists(path))
                {
                    string folderName = Path.GetFileName(path);
                    if (string.IsNullOrEmpty(folderName)) folderName = "Klasor";
                    
                    string tempZip = Path.Combine(Path.GetTempPath(), folderName + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".zip");
                    try
                    {
                        if (File.Exists(tempZip)) File.Delete(tempZip);
                        System.IO.Compression.ZipFile.CreateFromDirectory(path, tempZip);
                        long size = new FileInfo(tempZip).Length;
                        items.Add(new FileBatchItem { LocalPath = tempZip, Name = folderName + ".zip", Size = size, IsFolder = true, TempZipPath = tempZip });
                    }
                    catch (Exception ex)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show($"Klasor sikistirilip hazirlanirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }));
                        return;
                    }
                }
                else if (File.Exists(path))
                {
                    long size = new FileInfo(path).Length;
                    items.Add(new FileBatchItem { LocalPath = path, Name = Path.GetFileName(path), Size = size, IsFolder = false, TempZipPath = null });
                }
            }

            if (items.Count == 0) return;

            long totalSize = 0;
            foreach (var item in items)
            {
                totalSize += item.Size;
            }

            // Start batch on host
            await SendJsonAsync($"{{\"type\":\"batch_start\",\"senderId\":\"Uzak Yonetici\",\"totalFiles\":{items.Count},\"totalSize\":{totalSize},\"targetFolder\":\"{EscapeJson(targetFolder)}\"}}");

            // Show client progress dialog
            this.BeginInvoke(new Action(() =>
            {
                if (_clientProgressForm != null && !_clientProgressForm.IsDisposed)
                {
                    _clientProgressForm.Close();
                }
                _clientProgressForm = new FileTransferProgressForm(isSending: true, targetName: _targetId);
                _clientProgressForm.OnCancel += () =>
                {
                    _cancelActiveTransfer = true;
                    _ = SendJsonAsync("{\"type\":\"transfer_cancel\"}");
                };
                _clientProgressForm.Show(this);
            }));

            int currentFileIndex = 0;
            long totalBytesProcessed = 0;

            foreach (var item in items)
            {
                if (_cancelActiveTransfer) break;

                // Update progress on client
                this.BeginInvoke(new Action(() =>
                {
                    _clientProgressForm?.UpdateProgress(
                        item.LocalPath,
                        item.Name,
                        currentFileIndex + 1,
                        items.Count,
                        0,
                        item.Size,
                        totalBytesProcessed,
                        totalSize
                    );
                }));

                // Start file on host
                await SendJsonAsync($"{{\"type\":\"file_start\",\"name\":\"{EscapeJson(item.Name)}\",\"size\":{item.Size},\"isFolder\":{(item.IsFolder ? "true" : "false")}}}");

                try
                {
                    using (var fs = new FileStream(item.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        byte[] buffer = new byte[65536];
                        int bytesRead;
                        long fileBytesProcessed = 0;

                        while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            if (_cancelActiveTransfer) break;

                            byte[] actualBytes = new byte[bytesRead];
                            Array.Copy(buffer, actualBytes, bytesRead);
                            string base64 = Convert.ToBase64String(actualBytes);

                            await SendJsonAsync($"{{\"type\":\"file_chunk\",\"chunk\":\"{base64}\"}}");

                            fileBytesProcessed += bytesRead;
                            totalBytesProcessed += bytesRead;

                            // Update progress on client
                            this.BeginInvoke(new Action(() =>
                            {
                                _clientProgressForm?.UpdateProgress(
                                    item.LocalPath,
                                    item.Name,
                                    currentFileIndex + 1,
                                    items.Count,
                                    fileBytesProcessed,
                                    item.Size,
                                    totalBytesProcessed,
                                    totalSize
                                );
                            }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show($"Dosya okunurken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                    _cancelActiveTransfer = true;
                    await SendJsonAsync("{\"type\":\"transfer_cancel\"}");
                    break;
                }

                if (_cancelActiveTransfer) break;

                // End file on host
                await SendJsonAsync("{\"type\":\"file_end\"}");

                // Delete temp zip if it was a folder
                if (item.TempZipPath != null)
                {
                    try { if (File.Exists(item.TempZipPath)) File.Delete(item.TempZipPath); } catch { }
                }

                currentFileIndex++;
            }

            // End batch or cancel on host
            if (_cancelActiveTransfer)
            {
                await SendJsonAsync("{\"type\":\"transfer_cancel\"}");
            }
            else
            {
                await SendJsonAsync("{\"type\":\"batch_end\"}");
            }

            // Close client progress dialog
            this.BeginInvoke(new Action(() =>
            {
                _clientProgressForm?.Close();
                _clientProgressForm = null;
            }));
        }

        private async Task SendFileAsync(string filePath)
        {
            try
            {
                string filename = Path.GetFileName(filePath);
                long fileSize = new FileInfo(filePath).Length;

                await SendJsonAsync(new { type = "file_start", name = filename, size = fileSize });

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byte[] buffer = new byte[65536]; // 64KB packages
                    int bytesRead;
                    long totalSent = 0;

                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] actualBytes = new byte[bytesRead];
                        Array.Copy(buffer, actualBytes, bytesRead);
                        string base64 = Convert.ToBase64String(actualBytes);
                        
                        await SendJsonAsync(new { type = "file_chunk", chunk = base64 });
                        totalSent += bytesRead;
                    }
                }

                await SendJsonAsync(new { type = "file_end" });
                
                this.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"'{filename}' dosyası başarıyla karşı tarafa gönderildi.", "Dosya Gönderildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
            catch (Exception ex)
            {
                this.BeginInvoke(new Action(() =>
                {
                    MessageBox.Show($"Dosya gönderilirken hata oluştu: {ex.Message}", "Dosya Gönderme Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }));
            }
        }

        private void OpenChat()
        {
            if (_clientChatForm == null || _clientChatForm.IsDisposed)
            {
                _clientChatForm = new ClientChatForm(this);
            }
            _clientChatForm.Show();
            _clientChatForm.BringToFront();
        }

        private void OpenFileManager()
        {
            if (_fileManagerForm == null || _fileManagerForm.IsDisposed)
            {
                _fileManagerForm = new FileManagerForm(this);
            }
            _fileManagerForm.TopMost = true;
            _fileManagerForm.Show(this);
            _fileManagerForm.BringToFront();
            _fileManagerForm.Activate();
            _fileManagerForm.RefreshList();
        }

        private void TriggerRemoteRestart()
        {
            var res = MessageBox.Show(
                "Uzaktaki bilgisayarı yeniden başlatmak istediğinizden emin misiniz?\n\n(Açık olan tüm kaydedilmemiş çalışmalar kapanabilir.)", 
                "Uzaktan Yeniden Başlatma Onayı", 
                MessageBoxButtons.YesNo, 
                MessageBoxIcon.Warning, 
                MessageBoxDefaultButton.Button2
            );

            if (res == DialogResult.Yes)
            {
                SendJson("{\"type\":\"restart\"}");
            }
        }

        public void SendChatMessage(string text)
        {
            SendJson($"{{\"type\":\"chat\",\"message\":\"{EscapeJson(text)}\",\"sender\":\"Client\"}}");
        }

        private void ShowClientChatForm(string sender, string msg)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (_clientChatForm == null || _clientChatForm.IsDisposed)
                {
                    _clientChatForm = new ClientChatForm(this);
                    _clientChatForm.Show();
                }
                _clientChatForm.AppendMessage(sender, msg);
                _clientChatForm.BringToFront();
            }));
        }

        public static class Prompt
        {
            public static string ShowDialog(string text, string caption)
            {
                Form prompt = new Form()
                {
                    Width = 320,
                    Height = 150,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = caption,
                    StartPosition = FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(26, 28, 35),
                    ForeColor = Color.White
                };
                Label textLabel = new Label() { Left = 20, Top = 15, Text = text, Width = 280, ForeColor = Color.White };
                TextBox textBox = new TextBox() { Left = 20, Top = 45, Width = 260, PasswordChar = '*' };
                Button confirmation = new Button() { Text = "Tamam", Left = 180, Width = 100, Top = 75, DialogResult = DialogResult.OK };
                confirmation.Click += (sender, e) => { prompt.Close(); };
                prompt.Controls.Add(textBox);
                prompt.Controls.Add(confirmation);
                prompt.Controls.Add(textLabel);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
            }
        }

        private static string GetPlayHtmlTemplate()
        {
            return @"<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <title>BigLineconnect Oturum Kayıt Oynatıcısı</title>
    <style>
        body {
            margin: 0;
            padding: 0;
            background-color: #1a1c23;
            color: #ffffff;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            display: flex;
            flex-direction: column;
            align-items: center;
            height: 100vh;
            overflow: hidden;
        }
        #header {
            width: 100%;
            padding: 15px 30px;
            background-color: #15161c;
            border-bottom: 2px solid #00e5ff;
            display: flex;
            justify-content: space-between;
            align-items: center;
            box-sizing: border-box;
        }
        #logo {
            font-size: 20px;
            font-weight: bold;
            color: #00e5ff;
            text-shadow: 0 0 10px rgba(0,229,255,0.4);
        }
        #title {
            font-size: 14px;
            color: #888;
        }
        #viewer-container {
            flex: 1;
            width: 100%;
            display: flex;
            justify-content: center;
            align-items: center;
            padding: 20px;
            box-sizing: border-box;
            background-color: #0c0d12;
            position: relative;
        }
        #screen-frame {
            max-width: 100%;
            max-height: 100%;
            box-shadow: 0 0 30px rgba(0,0,0,0.8);
            border-radius: 4px;
            border: 1px solid #2a2d37;
        }
        #controls {
            width: 100%;
            padding: 20px 30px;
            background-color: #15161c;
            border-top: 1px solid #2a2d37;
            display: flex;
            flex-direction: column;
            gap: 15px;
            box-sizing: border-box;
        }
        #slider-container {
            display: flex;
            align-items: center;
            gap: 15px;
            width: 100%;
        }
        #timeline {
            flex: 1;
            height: 6px;
            -webkit-appearance: none;
            background: #2a2d37;
            border-radius: 3px;
            outline: none;
            cursor: pointer;
        }
        #timeline::-webkit-slider-thumb {
            -webkit-appearance: none;
            width: 16px;
            height: 16px;
            border-radius: 50%;
            background: #00e5ff;
            cursor: pointer;
            box-shadow: 0 0 8px rgba(0,229,255,0.8);
            transition: transform 0.1s;
        }
        #timeline::-webkit-slider-thumb:hover {
            transform: scale(1.2);
        }
        #buttons-container {
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        .btn {
            background-color: #00e5ff;
            color: #000000;
            border: none;
            padding: 8px 20px;
            font-size: 14px;
            font-weight: bold;
            border-radius: 4px;
            cursor: pointer;
            transition: all 0.2s;
            outline: none;
        }
        .btn:hover {
            background-color: #00b0ff;
            box-shadow: 0 0 10px rgba(0,229,255,0.5);
        }
        .btn-secondary {
            background-color: #2a2d37;
            color: #fff;
        }
        .btn-secondary:hover {
            background-color: #3b3f4e;
            box-shadow: none;
        }
        #play-state {
            display: flex;
            align-items: center;
            gap: 10px;
        }
        #status-info {
            font-size: 14px;
            color: #888;
        }
        #fps-control {
            display: flex;
            align-items: center;
            gap: 10px;
            font-size: 14px;
        }
        select {
            background-color: #2a2d37;
            color: white;
            border: 1px solid #3b3f4e;
            padding: 5px;
            border-radius: 4px;
            outline: none;
            cursor: pointer;
        }
    </style>
</head>
<body>
    <div id=""header"">
        <div id=""logo"">🖖 BigLineconnect Player</div>
        <div id=""title"">Oturum Kayıt Oynatıcısı</div>
    </div>
    
    <div id=""viewer-container"">
        <img id=""screen-frame"" src=""frame_000001.jpg"" alt=""Screen Frame"">
    </div>

    <div id=""controls"">
        <div id=""slider-container"">
            <span id=""current-time"">00:00</span>
            <input type=""range"" id=""timeline"" min=""1"" max=""1"" value=""1"">
            <span id=""total-time"">00:00</span>
        </div>
        <div id=""buttons-container"">
            <div id=""play-state"">
                <button class=""btn"" id=""btn-play"">Oynat</button>
                <span id=""status-info"">Kare: 1 / 1</span>
            </div>
            <div id=""fps-control"">
                <span>Oynatma Hızı:</span>
                <select id=""speed"">
                    <option value=""100"">Çok Yavaş (5 FPS)</option>
                    <option value=""66"">Yavaş (15 FPS)</option>
                    <option value=""33"" selected>Normal (30 FPS)</option>
                    <option value=""16"">Hızlı (60 FPS)</option>
                </select>
            </div>
        </div>
    </div>

    <script>
        let currentFrame = 1;
        let totalFrames = 1;
        let isPlaying = false;
        let playInterval = null;
        
        const frameImg = document.getElementById('screen-frame');
        const timeline = document.getElementById('timeline');
        const btnPlay = document.getElementById('btn-play');
        const statusInfo = document.getElementById('status-info');
        const speedSelect = document.getElementById('speed');
        const totalTimeSpan = document.getElementById('total-time');
        const currentTimeSpan = document.getElementById('current-time');

        function scanFrames() {
            let index = 1;
            let checkNext = () => {
                let img = new Image();
                img.onload = () => {
                    totalFrames = index;
                    timeline.max = totalFrames;
                    updateUI();
                    index++;
                    checkNext();
                };
                img.onerror = () => {
                    if (totalFrames === 1 && index === 1) {
                        setTimeout(scanFrames, 1000);
                    }
                };
                img.src = 'frame_' + String(index).padStart(6, '0') + '.jpg';
            };
            checkNext();
        }

        function updateUI() {
            timeline.value = currentFrame;
            frameImg.src = 'frame_' + String(currentFrame).padStart(6, '0') + '.jpg';
            statusInfo.innerText = 'Kare: ' + currentFrame + ' / ' + totalFrames;
            
            let curSec = Math.floor(currentFrame / 30);
            let totSec = Math.floor(totalFrames / 30);
            currentTimeSpan.innerText = formatTime(curSec);
            totalTimeSpan.innerText = formatTime(totSec);
        }

        function formatTime(sec) {
            let m = Math.floor(sec / 60).toString().padStart(2, '0');
            let s = (sec % 60).toString().padStart(2, '0');
            return m + ':' + s;
        }

        function play() {
            if (isPlaying) return;
            isPlaying = true;
            btnPlay.innerText = 'Durdur';
            let speed = parseInt(speedSelect.value);
            playInterval = setInterval(() => {
                if (currentFrame >= totalFrames) {
                    currentFrame = 1;
                } else {
                    currentFrame++;
                }
                updateUI();
            }, speed);
        }

        function pause() {
            if (!isPlaying) return;
            isPlaying = false;
            btnPlay.innerText = 'Oynat';
            clearInterval(playInterval);
        }

        btnPlay.addEventListener('click', () => {
            if (isPlaying) {
                pause();
            } else {
                play();
            }
        });

        timeline.addEventListener('input', () => {
            pause();
            currentFrame = parseInt(timeline.value);
            updateUI();
        });

        speedSelect.addEventListener('change', () => {
            if (isPlaying) {
                pause();
                play();
            }
        });

        scanFrames();
    </script>
</body>
</html>";
        }
    }

    public class ClientChatForm : Form
    {
        private ViewerForm _parent;
        private TextBox txtHistory;
        private TextBox txtInput;
        private Button btnSend;

        public ClientChatForm(ViewerForm parent)
        {
            _parent = parent;
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

        private void InitializeComponent()
        {
            this.Text = LanguageManager.Get("title_chat");
            this.Width = 350;
            this.Height = 400;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(26, 28, 35);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterParent;

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
            ModernUIHelper.ApplyButtonStyle(btnSend, Color.FromArgb(0, 229, 255), Color.FromArgb(0, 176, 255), Color.Black);
            btnSend.Click += (s, e) => SendMessage();

            this.Controls.Add(ModernUIHelper.CreateLogBoxWrapper(txtHistory));
            this.Controls.Add(ModernUIHelper.CreateTextBoxWrapper(txtInput));
            this.Controls.Add(btnSend);
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

        public void AppendMessage(string sender, string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string, string>(AppendMessage), sender, msg);
                return;
            }
            txtHistory.AppendText($"[{DateTime.Now:HH:mm:ss}] {sender}: {msg}\r\n\r\n");
            txtHistory.SelectionStart = txtHistory.TextLength;
            txtHistory.ScrollToCaret();
        }

        private void SendMessage()
        {
            string text = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            txtInput.Text = "";
            AppendMessage(LanguageManager.Get("chat_me"), text);
            _parent.SendChatMessage(text);
        }
    }

    public class FileManagerForm : Form
    {
        private ViewerForm _parent;
        
        // Header
        private Label? lblHeaderTitle;
        private Label? lblHeaderSub;

        // Left Panel - Receiver (Bu Bilgisayar / İndirme)
        private Panel? panelReceiver;
        private TextBox? txtSavePath;
        private Button? btnBrowseSavePath;
        private ListView? lvReceivedFiles;
        private Button? btnDownloadSelected;
        private ProgressBar? pbReceive;

        // Right Panel - Sender (Uzak Bilgisayar / Yükleme)
        private Panel? panelSender;
        private ComboBox? cbRemoteTargetFolder;
        private Panel? panelDropZone;
        private Label? lblDropHint;
        private ListBox? lbSenderFiles;
        private Button? btnAddFiles;
        private Button? btnAddFolder;
        private Button? btnClearList;
        private Button? btnSend;
        private ProgressBar? pbSend;
        private Label? lblSendStatus;

        private List<string> _sendPaths = new List<string>();
        private string _currentSavePath = "";
        private FileStream? _downloadStream;
        private string? _downloadFileName;
        private long _downloadTotalBytes = 0;
        private long _downloadBytesProcessed = 0;

        public FileManagerForm(ViewerForm parent)
        {
            _parent = parent;
            InitializeComponent();
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);
            }
            catch {}

            _currentSavePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (txtSavePath != null) txtSavePath.Text = _currentSavePath;

            this.Shown += (s, e) => RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "⚡ BigLineTransfer v2.0 - Gerçek Zamanlı Dosya Yöneticisi";
            this.Width = 980;
            this.Height = 650;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(20, 22, 28);
            this.ForeColor = Color.White;
            this.StartPosition = FormStartPosition.CenterParent;

            // Header Banner
            lblHeaderTitle = new Label
            {
                Text = "⚡ BigLineTransfer v2.0",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(20, 15),
                AutoSize = true
            };

            lblHeaderSub = new Label
            {
                Text = $"🟢 BAĞLANDI (ID: {_parent._targetId}) - Gerçek Sürücüler Yüklendi!",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(76, 175, 80),
                Location = new Point(250, 20),
                AutoSize = true
            };

            // LEFT PANEL (RECEIVER)
            panelReceiver = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(450, 530),
                BackColor = Color.FromArgb(26, 28, 35)
            };

            var lblRecTitle = new Label
            {
                Text = "📥 DOSYA ALICI (Bu Bilgisayar)",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(15, 15),
                AutoSize = true
            };

            var lblSaveHint = new Label
            {
                Text = "İndirilen Dosyaların Kaydedileceği Klasör:",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.LightGray,
                Location = new Point(15, 50),
                AutoSize = true
            };

            txtSavePath = new TextBox
            {
                Location = new Point(15, 75),
                Size = new Size(320, 25),
                ReadOnly = true,
                BackColor = Color.FromArgb(17, 19, 24),
                ForeColor = Color.White
            };

            btnBrowseSavePath = new Button
            {
                Text = "Gözat...",
                Location = new Point(345, 74),
                Size = new Size(90, 27)
            };
            ModernUIHelper.ApplyButtonStyle(btnBrowseSavePath, Color.FromArgb(40, 42, 54), Color.FromArgb(60, 62, 74), Color.White);
            btnBrowseSavePath.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    _currentSavePath = fbd.SelectedPath;
                    txtSavePath.Text = _currentSavePath;
                }
            };

            var lblHistory = new Label
            {
                Text = "Karşı Bilgisayardan İndirilebilecek Sürücüler / Klasörler:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 115),
                AutoSize = true
            };

            lvReceivedFiles = new ListView
            {
                Location = new Point(15, 140),
                Size = new Size(420, 310),
                View = View.Details,
                FullRowSelect = true,
                BackColor = Color.FromArgb(17, 19, 24),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            lvReceivedFiles.Columns.Add("Adı / Sürücü", 220);
            lvReceivedFiles.Columns.Add("Tür", 80);
            lvReceivedFiles.Columns.Add("Boyut", 100);
            lvReceivedFiles.DoubleClick += LvReceivedFiles_DoubleClick;

            btnDownloadSelected = new Button
            {
                Text = "📥 Seçili Dosyayı Bu Bilgisayara İndir",
                Location = new Point(15, 460),
                Size = new Size(420, 32),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            ModernUIHelper.ApplyButtonStyle(btnDownloadSelected, Color.FromArgb(0, 229, 255), Color.FromArgb(0, 176, 255), Color.Black);
            btnDownloadSelected.Click += (s, e) => DownloadFile();

            pbReceive = new ProgressBar
            {
                Location = new Point(15, 498),
                Size = new Size(420, 10),
                Visible = false
            };

            panelReceiver.Controls.Add(lblRecTitle);
            panelReceiver.Controls.Add(lblSaveHint);
            panelReceiver.Controls.Add(txtSavePath);
            panelReceiver.Controls.Add(btnBrowseSavePath);
            panelReceiver.Controls.Add(lblHistory);
            panelReceiver.Controls.Add(lvReceivedFiles);
            panelReceiver.Controls.Add(btnDownloadSelected);
            panelReceiver.Controls.Add(pbReceive);

            // RIGHT PANEL (SENDER)
            panelSender = new Panel
            {
                Location = new Point(490, 60),
                Size = new Size(465, 530),
                BackColor = Color.FromArgb(26, 28, 35)
            };

            var lblSendTitle = new Label
            {
                Text = "📤 DOSYA GÖNDERİCİ (Uzak Bilgisayar)",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(213, 0, 249),
                Location = new Point(15, 15),
                AutoSize = true
            };

            var lblTargetFolderHint = new Label
            {
                Text = "Karşı Bilgisayarın Kayıt Konumu / Sürücüsü:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 50),
                AutoSize = true
            };

            cbRemoteTargetFolder = new ComboBox
            {
                Location = new Point(15, 75),
                Size = new Size(435, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(17, 19, 24),
                ForeColor = Color.White
            };

            panelDropZone = new Panel
            {
                Location = new Point(15, 115),
                Size = new Size(435, 120),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.FromArgb(17, 19, 24),
                AllowDrop = true
            };
            panelDropZone.DragEnter += PanelDropZone_DragEnter;
            panelDropZone.DragDrop += PanelDropZone_DragDrop;

            lblDropHint = new Label
            {
                Text = "📁 Dosyaları veya Klasörleri Buraya Sürükleyin\nveya Aşağıdaki Butonları Kullanarak Seçin",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 180, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            lblDropHint.Click += (s, e) => AddFiles();
            panelDropZone.Controls.Add(lblDropHint);

            lbSenderFiles = new ListBox
            {
                Location = new Point(15, 245),
                Size = new Size(435, 150),
                BackColor = Color.FromArgb(17, 19, 24),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            btnAddFiles = new Button
            {
                Text = "+ Dosya Ekle",
                Location = new Point(15, 405),
                Size = new Size(130, 30)
            };
            ModernUIHelper.ApplyButtonStyle(btnAddFiles, Color.FromArgb(40, 42, 54), Color.FromArgb(60, 62, 74), Color.White);
            btnAddFiles.Click += (s, e) => AddFiles();

            btnAddFolder = new Button
            {
                Text = "+ Klasör Ekle",
                Location = new Point(155, 405),
                Size = new Size(130, 30)
            };
            ModernUIHelper.ApplyButtonStyle(btnAddFolder, Color.FromArgb(40, 42, 54), Color.FromArgb(60, 62, 74), Color.White);
            btnAddFolder.Click += (s, e) => AddFolder();

            btnClearList = new Button
            {
                Text = "Temizle",
                Location = new Point(295, 405),
                Size = new Size(155, 30)
            };
            ModernUIHelper.ApplyButtonStyle(btnClearList, Color.FromArgb(180, 40, 40), Color.FromArgb(210, 50, 50), Color.White);
            btnClearList.Click += (s, e) => ClearSendList();

            btnSend = new Button
            {
                Text = "🚀 KARŞI BİLGİSAYARA GÖNDER",
                Location = new Point(15, 445),
                Size = new Size(435, 45),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold)
            };
            ModernUIHelper.ApplyButtonStyle(btnSend, Color.FromArgb(213, 0, 249), Color.FromArgb(170, 0, 255), Color.White);
            btnSend.Click += (s, e) => StartSendBatch();

            pbSend = new ProgressBar
            {
                Location = new Point(15, 498),
                Size = new Size(435, 10),
                Visible = false
            };

            lblSendStatus = new Label
            {
                Text = "Hazır",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.Gray,
                Location = new Point(15, 510),
                AutoSize = true
            };

            panelSender.Controls.Add(lblSendTitle);
            panelSender.Controls.Add(lblTargetFolderHint);
            panelSender.Controls.Add(cbRemoteTargetFolder);
            panelSender.Controls.Add(panelDropZone);
            panelSender.Controls.Add(lbSenderFiles);
            panelSender.Controls.Add(btnAddFiles);
            panelSender.Controls.Add(btnAddFolder);
            panelSender.Controls.Add(btnClearList);
            panelSender.Controls.Add(btnSend);
            panelSender.Controls.Add(pbSend);
            panelSender.Controls.Add(lblSendStatus);

            this.Controls.Add(lblHeaderTitle);
            this.Controls.Add(lblHeaderSub);
            this.Controls.Add(panelReceiver);
            this.Controls.Add(panelSender);
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

        public void RefreshList()
        {
            _parent.SendJson("{\"type\":\"fs_list\",\"path\":\"\"}");
        }

        public void Populate(JsonElement root)
        {
            cbRemoteTargetFolder.Items.Clear();
            lvReceivedFiles.Items.Clear();

            string driveLabel = "[" + (LanguageManager.CurrentLanguage == "tr" ? "Sürücü" : "Drive") + "]";
            string folderLabel = "[" + (LanguageManager.CurrentLanguage == "tr" ? "Klasör" : "Folder") + "]";

            string currentPath = root.TryGetProperty("path", out var pProp) ? (pProp.GetString() ?? "") : "";
            if (!string.IsNullOrEmpty(currentPath))
            {
                string parentPath = Path.GetDirectoryName(currentPath) ?? "";
                var backItem = new ListViewItem("⬅️ [ .. Üst Klasör ]");
                backItem.SubItems.Add(folderLabel);
                backItem.SubItems.Add(parentPath);
                lvReceivedFiles.Items.Add(backItem);
            }

            if (root.TryGetProperty("drives", out var drivesProp) && drivesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var d in drivesProp.EnumerateArray())
                {
                    string driveName = d.GetString() ?? "";
                    if (!string.IsNullOrEmpty(driveName))
                    {
                        cbRemoteTargetFolder.Items.Add($"💽 Karşı {driveName} Sürücüsü");
                        
                        var item = new ListViewItem(driveName);
                        item.SubItems.Add(driveLabel);
                        item.SubItems.Add("");
                        lvReceivedFiles.Items.Add(item);
                    }
                }
            }

            if (root.TryGetProperty("folders", out var foldersProp) && foldersProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in foldersProp.EnumerateArray())
                {
                    string folderName = f.GetString() ?? "";
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        if (folderName.StartsWith("Masaüstü"))
                        {
                            cbRemoteTargetFolder.Items.Insert(0, $"🖥️ Karşı {folderName}");
                        }
                        else if (folderName.StartsWith("İndirilenler"))
                        {
                            cbRemoteTargetFolder.Items.Insert(Math.Min(1, cbRemoteTargetFolder.Items.Count), $"📥 Karşı {folderName}");
                        }
                        else
                        {
                            cbRemoteTargetFolder.Items.Add($"📁 {folderName}");
                        }

                        string fullFolderPath = !string.IsNullOrEmpty(currentPath) ? Path.Combine(currentPath, folderName) : folderName;
                        var item = new ListViewItem(fullFolderPath);
                        item.SubItems.Add(folderLabel);
                        item.SubItems.Add("");
                        lvReceivedFiles.Items.Add(item);
                    }
                }
            }

            if (root.TryGetProperty("files", out var filesProp) && filesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var f in filesProp.EnumerateArray())
                {
                    string name = f.TryGetProperty("name", out var nProp) ? (nProp.GetString() ?? "") : "";
                    long size = f.TryGetProperty("size", out var sProp) ? sProp.GetInt64() : 0;
                    string modified = f.TryGetProperty("modified", out var mProp) ? (mProp.GetString() ?? "") : "";

                    string fullFilePath = !string.IsNullOrEmpty(currentPath) ? Path.Combine(currentPath, name) : name;
                    var item = new ListViewItem(fullFilePath);
                    item.SubItems.Add(FormatSize(size));
                    item.SubItems.Add(modified);
                    lvReceivedFiles.Items.Add(item);
                }
            }

            if (cbRemoteTargetFolder.Items.Count > 0) cbRemoteTargetFolder.SelectedIndex = 0;
        }

        private string FormatSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            double val = bytes;
            int i = 0;
            while (val >= 1024 && i < suffixes.Length - 1)
            {
                val /= 1024;
                i++;
            }
            return $"{val:0.##} {suffixes[i]}";
        }

        private void LvReceivedFiles_DoubleClick(object? sender, EventArgs e)
        {
            if (lvReceivedFiles.SelectedItems.Count == 0) return;
            var item = lvReceivedFiles.SelectedItems[0];
            string name = item.Text;
            string type = item.SubItems[1].Text;

            if (name.Contains("Üst Klasör"))
            {
                string parentPath = item.SubItems[2].Text;
                _parent.SendJson($"{{\"type\":\"fs_list\",\"path\":\"{ViewerForm.EscapeJson(parentPath)}\"}}");
                return;
            }

            if (type.Contains("Sürücü") || type.Contains("Drive") || type.Contains("Klasör") || type.Contains("Folder"))
            {
                _parent.SendJson($"{{\"type\":\"fs_list\",\"path\":\"{ViewerForm.EscapeJson(name)}\"}}");
            }
            else
            {
                DownloadFile();
            }
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
                        if (!_sendPaths.Contains(f))
                        {
                            _sendPaths.Add(f);
                            lbSenderFiles.Items.Add(f);
                        }
                    }
                    lblSendStatus.Text = $"{_sendPaths.Count} öğe gönderilmek üzere eklendi.";
                }
            }
        }

        private void AddFiles()
        {
            using var ofd = new OpenFileDialog { Multiselect = true };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                foreach (var f in ofd.FileNames)
                {
                    if (!_sendPaths.Contains(f))
                    {
                        _sendPaths.Add(f);
                        lbSenderFiles.Items.Add(f);
                    }
                }
                lblSendStatus.Text = $"{_sendPaths.Count} öğe gönderilmek üzere eklendi.";
            }
        }

        private void AddFolder()
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                if (!_sendPaths.Contains(fbd.SelectedPath))
                {
                    _sendPaths.Add(fbd.SelectedPath);
                    lbSenderFiles.Items.Add(fbd.SelectedPath);
                }
                lblSendStatus.Text = $"{_sendPaths.Count} öğe gönderilmek üzere eklendi.";
            }
        }

        private void ClearSendList()
        {
            _sendPaths.Clear();
            lbSenderFiles.Items.Clear();
            lblSendStatus.Text = "Gönderim listesi temizlendi.";
        }

        private string GetCleanTargetFolder()
        {
            if (cbRemoteTargetFolder == null || cbRemoteTargetFolder.SelectedItem == null) return "DESKTOP";
            string raw = cbRemoteTargetFolder.SelectedItem.ToString() ?? "";
            if (raw.Contains("Masaüstü")) return "DESKTOP";
            if (raw.Contains("İndirilenler")) return "DOWNLOADS";
            
            string clean = raw.Replace("🖥️", "").Replace("📥", "").Replace("💽", "").Replace("📁", "").Replace("Karşı ", "").Trim();
            return string.IsNullOrEmpty(clean) ? "DESKTOP" : clean;
        }

        private async void StartSendBatch()
        {
            if (_sendPaths.Count == 0)
            {
                MessageBox.Show("Lütfen önce gönderilecek dosya veya klasör ekleyin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cleanTargetFolder = GetCleanTargetFolder();

            if (btnSend != null) btnSend.Enabled = false;
            if (pbSend != null) { pbSend.Visible = true; pbSend.Value = 0; }
            if (lblSendStatus != null) lblSendStatus.Text = "Aktarım başlatılıyor...";

            try
            {
                var pathsToSend = new List<string>(_sendPaths);
                await _parent.SendPathsBatchAsync(pathsToSend, promptFolder: false, overrideTargetFolder: cleanTargetFolder);

                if (lblSendStatus != null)
                {
                    lblSendStatus.Text = "🎉 TÜM DOSYALAR BAŞARIYLA GÖNDERİLDİ!";
                    lblSendStatus.ForeColor = Color.FromArgb(76, 175, 80);
                }
                MessageBox.Show("Seçilen tüm dosya ve klasörler karşı bilgisayara başarıyla aktarıldı!", "Aktarım Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearSendList();
            }
            catch (Exception ex)
            {
                if (lblSendStatus != null)
                {
                    lblSendStatus.Text = $"Gönderim Hatası: {ex.Message}";
                    lblSendStatus.ForeColor = Color.Red;
                }
            }
            finally
            {
                if (btnSend != null) btnSend.Enabled = true;
                if (pbSend != null) pbSend.Visible = false;
            }
        }

        private void DownloadFile()
        {
            if (lvReceivedFiles.SelectedItems.Count == 0)
            {
                MessageBox.Show("Lütfen sol listeden indirilecek bir dosya seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var item = lvReceivedFiles.SelectedItems[0];
            string name = item.Text;
            string type = item.SubItems[1].Text;

            if (type.Contains("Sürücü") || type.Contains("Drive")) return;

            bool isFolder = type.Contains("Klasör") || type.Contains("Folder");
            string fileNameOnly = Path.GetFileName(name);
            if (string.IsNullOrEmpty(fileNameOnly)) fileNameOnly = "download";
            string suggestedName = isFolder ? (fileNameOnly.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ? fileNameOnly : fileNameOnly + ".zip") : fileNameOnly;

            using var sfd = new SaveFileDialog { FileName = suggestedName, InitialDirectory = _currentSavePath };
            if (sfd.ShowDialog(this) == DialogResult.OK)
            {
                _downloadFileName = sfd.FileName;
                try
                {
                    if (_downloadStream != null)
                    {
                        _downloadStream.Close();
                        _downloadStream.Dispose();
                        _downloadStream = null;
                    }
                    _downloadStream = new FileStream(_downloadFileName, FileMode.Create, FileAccess.Write);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Dosya yazma hatası: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                btnDownloadSelected.Enabled = false;
                btnDownloadSelected.Text = "Hazırlanıyor...";

                if (isFolder)
                {
                    _parent.SendJson($"{{\"type\":\"folder_download_req\",\"path\":\"{ViewerForm.EscapeJson(name)}\"}}");
                }
                else
                {
                    _parent.SendJson($"{{\"type\":\"file_download_req\",\"path\":\"{ViewerForm.EscapeJson(name)}\"}}");
                }
            }
        }

        public void StartIncomingDownload(string name, long totalSize = 0)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    _downloadTotalBytes = totalSize;
                    _downloadBytesProcessed = 0;
                    if (btnDownloadSelected != null) btnDownloadSelected.Text = "Hazırlanıyor...";
                    if (pbReceive != null)
                    {
                        pbReceive.Visible = true;
                        pbReceive.Value = 0;
                    }
                });
            }
            catch { }
        }

        public void WriteDownloadChunk(string base64)
        {
            try
            {
                if (_downloadStream != null && !string.IsNullOrEmpty(base64))
                {
                    byte[] bytes = Convert.FromBase64String(base64);
                    _downloadStream.Write(bytes, 0, bytes.Length);
                    _downloadBytesProcessed += bytes.Length;

                    if (_downloadTotalBytes > 0)
                    {
                        int percent = (int)Math.Min(100, (_downloadBytesProcessed * 100) / _downloadTotalBytes);
                        string procMB = (_downloadBytesProcessed / (1024.0 * 1024.0)).ToString("0.0");
                        string totalMB = (_downloadTotalBytes / (1024.0 * 1024.0)).ToString("0.0");
                        string statusText = $"İndiriliyor: %{percent} ({procMB} MB / {totalMB} MB)";

                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (btnDownloadSelected != null) btnDownloadSelected.Text = statusText;
                            if (pbReceive != null)
                            {
                                pbReceive.Visible = true;
                                pbReceive.Value = percent;
                            }
                        });
                    }
                    else
                    {
                        string procMB = (_downloadBytesProcessed / (1024.0 * 1024.0)).ToString("0.0");
                        string statusText = $"İndiriliyor: {procMB} MB";

                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            if (btnDownloadSelected != null) btnDownloadSelected.Text = statusText;
                            if (pbReceive != null) pbReceive.Visible = true;
                        });
                    }
                }
            }
            catch { }
        }

        public void EndIncomingDownload()
        {
            try
            {
                if (_downloadStream != null)
                {
                    _downloadStream.Flush();
                    _downloadStream.Close();
                    _downloadStream.Dispose();
                    _downloadStream = null;
                }

                this.Invoke((MethodInvoker)delegate
                {
                    if (btnDownloadSelected != null)
                    {
                        btnDownloadSelected.Enabled = true;
                        btnDownloadSelected.Text = "📥 Seçili Dosyayı Bu Bilgisayara İndir";
                    }
                    if (pbReceive != null)
                    {
                        pbReceive.Value = 100;
                        pbReceive.Visible = false;
                    }
                    MessageBox.Show("🎉 Dosya başarıyla bilgisayarınıza indirildi:\r\n" + Path.GetFileName(_downloadFileName ?? ""), "İndirme Tamamlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                });
            }
            catch { }
        }

        public void HandleDownloadError(string message)
        {
            try
            {
                if (_downloadStream != null)
                {
                    _downloadStream.Close();
                    _downloadStream.Dispose();
                    _downloadStream = null;
                }
                this.Invoke((MethodInvoker)delegate
                {
                    btnDownloadSelected.Enabled = true;
                    btnDownloadSelected.Text = "📥 Seçili Dosyayı Bu Bilgisayara İndir";
                    pbReceive.Visible = false;
                    MessageBox.Show(message, "Transfer Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
            catch { }
        }
    }

    public static class ModernUIHelper
    {
        public static void ApplyButtonStyle(Button btn, Color normalBg, Color hoverBg, Color textCol)
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

        public static Panel CreateTextBoxWrapper(TextBox txt)
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

        public static Panel CreateLogBoxWrapper(TextBox txt)
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

        public static void DrawCard(Graphics g, Rectangle rect, string title)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (var fillBrush = new SolidBrush(Color.FromArgb(30, 20, 22, 28)))
            {
                FillRoundedRectangle(g, fillBrush, rect, 10);
            }

            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect,
                Color.FromArgb(120, 0, 229, 255),
                Color.FromArgb(120, 213, 0, 249),
                45F))
            using (var pen = new Pen(brush, 1.2F))
            {
                DrawRoundedRectangle(g, pen, rect, 10);
            }

            if (!string.IsNullOrEmpty(title))
            {
                using (var titleFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.FromArgb(0, 229, 255)))
                {
                    g.DrawString(title, titleFont, textBrush, rect.X + 15, rect.Y - 8);
                }
            }
        }

        private static void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private static void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RemoteFolderSelectionDialog : Form
    {
        private RadioButton rbDesktop;
        private RadioButton rbDownloads;
        private RadioButton rbCustom;
        private TextBox txtCustomPath;
        private Button btnOk;
        private Button btnCancel;
        
        public string SelectedPath { get; private set; } = "DESKTOP";

        public RemoteFolderSelectionDialog()
        {
            this.Text = "Uzak Hedef Dizin Seçimi";
            this.Size = new Size(400, 240);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(20, 22, 30);
            this.ForeColor = Color.White;

            Label lblInfo = new Label
            {
                Text = "Dosyalar uzak bilgisayarda nereye kaydedilsin?",
                Location = new Point(15, 15),
                Size = new Size(370, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };

            rbDesktop = new RadioButton
            {
                Text = "Masaüstü (Desktop)",
                Location = new Point(20, 45),
                Size = new Size(350, 20),
                Checked = true,
                ForeColor = Color.FromArgb(0, 229, 255)
            };

            rbDownloads = new RadioButton
            {
                Text = "İndirilenler (Downloads)",
                Location = new Point(20, 70),
                Size = new Size(350, 20),
                ForeColor = Color.FromArgb(0, 229, 255)
            };

            rbCustom = new RadioButton
            {
                Text = "Özel Yol Belirtin:",
                Location = new Point(20, 95),
                Size = new Size(130, 20),
                ForeColor = Color.FromArgb(0, 229, 255)
            };

            txtCustomPath = new TextBox
            {
                Location = new Point(150, 95),
                Size = new Size(220, 23),
                BackColor = Color.FromArgb(30, 32, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = @"C:\BigLineTransfers"
            };

            btnOk = new Button
            {
                Text = "Gönder",
                DialogResult = DialogResult.OK,
                Location = new Point(210, 150),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(30, 35, 45),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(0, 229, 255);

            btnCancel = new Button
            {
                Text = "İptal",
                DialogResult = DialogResult.Cancel,
                Location = new Point(300, 150),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(30, 35, 45),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.Red;

            rbDesktop.CheckedChanged += (s, e) => txtCustomPath.Enabled = rbCustom.Checked;
            rbDownloads.CheckedChanged += (s, e) => txtCustomPath.Enabled = rbCustom.Checked;
            rbCustom.CheckedChanged += (s, e) => txtCustomPath.Enabled = rbCustom.Checked;
            txtCustomPath.Enabled = false;

            this.Controls.AddRange(new Control[] { lblInfo, rbDesktop, rbDownloads, rbCustom, txtCustomPath, btnOk, btnCancel });

            btnOk.Click += (s, e) =>
            {
                if (rbDesktop.Checked)
                {
                    SelectedPath = "DESKTOP";
                }
                else if (rbDownloads.Checked)
                {
                    SelectedPath = "DOWNLOADS";
                }
                else
                {
                    SelectedPath = txtCustomPath.Text.Trim();
                    if (string.IsNullOrEmpty(SelectedPath))
                    {
                        MessageBox.Show("Lütfen geçerli bir özel dizin yolu girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        this.DialogResult = DialogResult.None;
                    }
                }
            };
        }
    }

    public class RestartConfirmForm : Form
    {
        public RestartConfirmForm(string message)
        {
            this.Text = "Uzaktan Yeniden Başlatma";
            this.Size = new Size(420, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(20, 20, 30);
            this.ForeColor = Color.White;

            var lblText = new Label
            {
                Text = message,
                Location = new Point(20, 20),
                Size = new Size(360, 80),
                Font = new Font("Segoe UI", 9.5F)
            };

            var btnYes = new Button
            {
                Text = "Evet",
                Location = new Point(200, 115),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Yes
            };

            var btnNo = new Button
            {
                Text = "Hayır",
                Location = new Point(300, 115),
                Size = new Size(90, 30),
                DialogResult = DialogResult.No
            };

            btnYes.FlatStyle = FlatStyle.Flat;
            btnYes.BackColor = Color.FromArgb(244, 67, 54);
            btnYes.ForeColor = Color.White;
            btnYes.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnYes.Cursor = Cursors.Hand;
            btnYes.FlatAppearance.BorderSize = 0;

            btnNo.FlatStyle = FlatStyle.Flat;
            btnNo.BackColor = Color.FromArgb(40, 42, 60);
            btnNo.ForeColor = Color.White;
            btnNo.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            btnNo.Cursor = Cursors.Hand;
            btnNo.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblText);
            this.Controls.Add(btnYes);
            this.Controls.Add(btnNo);

            this.CancelButton = btnNo;
        }
    }

    public class NoFocusButton : Button
    {
        public NoFocusButton()
        {
            SetStyle(ControlStyles.Selectable, false);
            this.TabStop = false;
        }

        protected override bool ShowFocusCues => false;
    }

    public class DoubleBufferedPictureBox : PictureBox
    {
        public DoubleBufferedPictureBox()
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (this.Image == null)
            {
                base.OnPaintBackground(e);
            }
        }
    }
}
