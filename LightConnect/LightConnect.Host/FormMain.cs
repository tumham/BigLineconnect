using System;
using System.Drawing;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LightConnect.Host
{
    public class FormMain : Form
    {
        private Label _lblStatus;
        private Label _lblId;
        private Button _btnReconnect;
        private RichTextBox _txtLog;
        private string _hostId = "";
        private ClientWebSocket? _ws;
        private CancellationTokenSource _cts = new();
        private bool _isStreaming = false;
        private readonly object _frameLock = new();
        private byte[]? _latestFrame;

        public FormMain()
        {
            this.Text = "LightConnect - Uzak Masaüstü Ajanı";
            this.Size = new Size(520, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(15, 16, 22);

            _hostId = GenerateHostId();

            InitializeUI();
            this.Load += (s, e) => StartConnectionLoop();
            this.FormClosing += (s, e) => _cts.Cancel();
        }

        private string GenerateHostId()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LightConnect", "host_id.txt");
            try
            {
                if (File.Exists(path))
                {
                    string id = File.ReadAllText(path).Trim();
                    if (id.Length == 6 && int.TryParse(id, out _)) return id;
                }
            }
            catch { }

            string newId = Random.Shared.Next(100000, 999999).ToString();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, newId);
            }
            catch { }
            return newId;
        }

        private void InitializeUI()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(22, 24, 34)
            };

            Label title = new Label
            {
                Text = "⚡ LightConnect",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(20, 15),
                AutoSize = true
            };

            Label subtitle = new Label
            {
                Text = "Ultra Hafif & Hızlı Uzaktan Erişim Ajanı",
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(160, 174, 192),
                Location = new Point(22, 43),
                AutoSize = true
            };

            header.Controls.Add(title);
            header.Controls.Add(subtitle);
            this.Controls.Add(header);

            Panel cardPanel = new Panel
            {
                Location = new Point(20, 85),
                Size = new Size(465, 140),
                BackColor = Color.FromArgb(26, 29, 42)
            };

            Label lblIdTitle = new Label
            {
                Text = "LIGHTCONNECT MASAÜSTÜ ID'NİZ:",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                Location = new Point(15, 15),
                AutoSize = true
            };

            _lblId = new Label
            {
                Text = $"{_hostId.Substring(0, 3)} {_hostId.Substring(3)}",
                Font = new Font("Segoe UI", 26F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(12, 38),
                AutoSize = true
            };

            _lblStatus = new Label
            {
                Text = "🟡 Sunucuya bağlanılıyor...",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(241, 196, 15),
                Location = new Point(15, 95),
                AutoSize = true
            };

            _btnReconnect = new Button
            {
                Text = "Yeniden Bağlan",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.FromArgb(0, 229, 255),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(330, 90),
                Size = new Size(120, 32),
                Cursor = Cursors.Hand
            };
            _btnReconnect.FlatAppearance.BorderSize = 0;
            _btnReconnect.Click += (s, e) => StartConnectionLoop();

            cardPanel.Controls.Add(lblIdTitle);
            cardPanel.Controls.Add(_lblId);
            cardPanel.Controls.Add(_lblStatus);
            cardPanel.Controls.Add(_btnReconnect);
            this.Controls.Add(cardPanel);

            _txtLog = new RichTextBox
            {
                Location = new Point(20, 240),
                Size = new Size(465, 185),
                BackColor = Color.FromArgb(10, 11, 16),
                ForeColor = Color.FromArgb(46, 204, 113),
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };
            this.Controls.Add(_txtLog);
        }

        private void Log(string msg)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() => Log(msg)));
                return;
            }
            _txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _txtLog.SelectionStart = _txtLog.Text.Length;
            _txtLog.ScrollToCaret();
        }

        private void UpdateStatus(string text, Color color)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)(() => UpdateStatus(text, color)));
                return;
            }
            _lblStatus.Text = text;
            _lblStatus.ForeColor = color;
        }

        private async void StartConnectionLoop()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                if (_ws != null)
                {
                    try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Reconnecting", CancellationToken.None); } catch { }
                    _ws.Dispose();
                }

                _ws = new ClientWebSocket();
                Uri serverUri = new Uri($"wss://biglineconnect.bigus.com.tr/lc-host?id={_hostId}");
                UpdateStatus("🟡 Bulut sunucuya bağlanılıyor...", Color.FromArgb(241, 196, 15));
                Log($"Sunucuya bağlanılıyor: {serverUri}");

                await _ws.ConnectAsync(serverUri, token);
                UpdateStatus("🟢 Sunucuya Bağlandı (Hazır)", Color.FromArgb(46, 204, 113));
                Log("LightConnect sunucusuna başarıyla bağlandı. Bağlantı bekleniyor.");

                _ = Task.Run(() => ReceiveLoop(_ws, token));
            }
            catch (Exception ex)
            {
                UpdateStatus("🔴 Bağlantı Hatası", Color.FromArgb(231, 76, 60));
                Log($"Bağlantı hatası: {ex.Message}");
            }
        }

        private async Task ReceiveLoop(ClientWebSocket ws, CancellationToken token)
        {
            var buffer = new byte[8192];
            try
            {
                while (!token.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            if (result.MessageType == WebSocketMessageType.Close) break;
                            ms.Write(buffer, 0, result.Count);
                        }
                        while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close) break;

                        if (result.MessageType == WebSocketMessageType.Text && ms.Length > 0)
                        {
                            string msg = Encoding.UTF8.GetString(ms.ToArray()).Trim();
                            if (msg == "START_STREAM")
                            {
                                Log("İstemci bağlandı! Ekran yayını başlatılıyor...");
                                _isStreaming = true;
                                StartCaptureThreads(ws, token);
                            }
                            else if (msg == "STOP_STREAM")
                            {
                                Log("İstemci ayrıldı. Ekran yayını durduruldu.");
                                _isStreaming = false;
                            }
                            else
                            {
                                ProcessInputJson(msg);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Alım döngüsü kapandı: {ex.Message}");
                UpdateStatus("🔴 Bağlantı Koptu", Color.FromArgb(231, 76, 60));
            }
        }

        private void StartCaptureThreads(ClientWebSocket ws, CancellationToken token)
        {
            var captureThread = new Thread(() =>
            {
                while (!token.IsCancellationRequested && _isStreaming && ws.State == WebSocketState.Open)
                {
                    byte[] frame = ScreenCapturer.CaptureScreenJpeg(1280, 45);
                    if (frame.Length > 0)
                    {
                        lock (_frameLock)
                        {
                            _latestFrame = frame;
                        }
                    }
                    Thread.Sleep(30); // ~30 FPS stream
                }
            })
            { IsBackground = true };
            captureThread.Start();

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested && _isStreaming && ws.State == WebSocketState.Open)
                {
                    byte[]? frameToSend = null;
                    lock (_frameLock)
                    {
                        frameToSend = _latestFrame;
                    }

                    if (frameToSend != null && frameToSend.Length > 0)
                    {
                        try
                        {
                            await ws.SendAsync(new ArraySegment<byte>(frameToSend), WebSocketMessageType.Binary, true, token);
                        }
                        catch { break; }
                    }
                    await Task.Delay(30, token);
                }
            });
        }

        private void ProcessInputJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var typeProp)) return;
                string type = typeProp.GetString() ?? "";

                if (type == "move")
                {
                    double x = root.GetProperty("x").GetDouble();
                    double y = root.GetProperty("y").GetDouble();
                    InputSimulator.SimulateMouseMove(x, y);
                }
                else if (type == "click")
                {
                    string button = root.GetProperty("button").GetString() ?? "left";
                    string action = root.GetProperty("action").GetString() ?? "down";
                    double? x = root.TryGetProperty("x", out var xp) ? xp.GetDouble() : null;
                    double? y = root.TryGetProperty("y", out var yp) ? yp.GetDouble() : null;
                    InputSimulator.SimulateMouseButton(button, action, x, y);
                }
                else if (type == "scroll")
                {
                    int deltaY = root.GetProperty("deltaY").GetInt32();
                    InputSimulator.SimulateMouseScroll(deltaY);
                }
                else if (type == "key")
                {
                    string key = root.GetProperty("key").GetString() ?? "";
                    string action = root.GetProperty("action").GetString() ?? "down";
                    InputSimulator.SimulateKey(key, action);
                }
            }
            catch { }
        }
    }
}
