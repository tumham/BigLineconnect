using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text;

namespace BigLineconnect
{
    public class MainWindow : Form
    {
        private static MainWindow? _instance;
        public static MainWindow? Instance => _instance;

        // Windows Service & Address Book Controls
        private Panel? _addressBookGroup;
        private ListView? _addressBookListView;
        private Button? _addAddressButton;
        private Button? _editAddressButton;
        private Button? _deleteAddressButton;

        public class SavedConnection
        {
            public string Name { get; set; } = "";
            public string Id { get; set; } = "";
            public string Password { get; set; } = "";
            public string Group { get; set; } = "Müşteriler";
        }

        private List<SavedConnection> _savedConnections = new();
        private bool _isAdminsExpanded = true;
        private bool _isTeamExpanded = true;
        private bool _isClientsExpanded = true;

        public PictureBox? _logoBox;
        private Label? _titleLabel;
        
        // Relay Server settings group
        private Panel? _serverGroup;
        private TextBox? _relayUrlTextBox;
        public string _actualRelayUrl = "wss://relay.biglineconnect.com/register-host";
        private Button? _reconnectButton;

        // AnyDesk columns
        private Panel? _thisDeskGroup;
        private Label? _idLabel;
        
        private Panel? _remoteDeskGroup;
        private TextBox? _remoteIdTextBox;
        private Button? _connectButton;

        private Label? _logLabel;
        private TextBox? _logTextBox;
        private NotifyIcon? _notifyIcon;

        // Unattended Access & Clipboard Sync
        private Panel? _securityGroup;
        private CheckBox? _usePasswordCheckBox;
        private TextBox? _passwordTextBox;
        private Label? _passwordLabel;
        private System.Windows.Forms.Timer? _clipboardTimer;
        private string _lastClipboardText = "";
        private System.Collections.Specialized.StringCollection? _lastClipboardFiles;

        private Button? _tabRehberButton;
        private Button? _tabDestekButton;
        private Button? _tabCrmButton;
        private TextBox? _txtSearchAddress;
        private Panel? _pnlDateFilter;
        private Button? _btnDatePrev;
        private Button? _btnDateNext;
        private Label? _lblDateFilter;
        private int _dateFilterIndex = 0; // 0: Tümü, 1: Bugün, 2: Dün, 3: Eski
        private int _currentTabMode = 0; // 0: Rehber, 1: Talepler, 2: CRM Geçmişi
        private bool _isShowingTickets => _currentTabMode == 1;
        private List<SupportTicket> _activeTickets = new();
        private List<SupportHistoryItem> _crmHistoryItems = new();
        private HashSet<string> _connectedTicketIds = new();
        private System.Windows.Forms.Timer? _ticketsTimer;
        private Button? _btnSupport;
        private Button? _btnMyTickets;
        private bool _hasActiveSubmittedTicket = false;
        private bool _hasAutoMinimizedForRemoteSession = false;
        private static RemoteOverlayBannerForm? _overlayBannerForm = null;
        public static bool IsBannerDismissedByUser = false;

        public class SupportTicket
        {
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public string Issue { get; set; } = "";
            public string Priority { get; set; } = "Orta";
            public string Token { get; set; } = "";
            public bool RequiresConfirmation { get; set; } = false;
            public DateTime CreatedAt { get; set; }
        }

        public class SupportHistoryItem
        {
            public string Id { get; set; } = "";
            public string HostId { get; set; } = "";
            public string Token { get; set; } = "";
            public string Name { get; set; } = "";
            public string Issue { get; set; } = "";
            public string TenantId { get; set; } = "";
            public string CreatedAt { get; set; } = "";
            public string ResolvedAt { get; set; } = "";
            public string Status { get; set; } = "";
            public string Notes { get; set; } = "";
        }

        // Licensing GUI elements
        private Panel? _licensingOverlay;
        private LinkLabel? _btnLic;
        private LinkLabel? _btnHelp;

        public bool UsePassword => _usePasswordCheckBox?.Checked ?? false;
        public string AccessPassword => _passwordTextBox?.Text ?? "";

        public MainWindow()
        {
            _instance = this;
            try { Program.SetStreamActive(false); } catch { }
            InitializeComponent();
            LoadLogoAndIcon();

            // Populate connection logs that occurred during splash screen connection
            lock (Program.InitialLogs)
            {
                foreach (var log in Program.InitialLogs)
                {
                    AppendLog(log);
                }
                Program.InitialLogs.Clear();
            }

            LoadAddressBook();
            CheckLicensingOnLoad();

            if (!string.IsNullOrEmpty(Program.CurrentHostId) && Program.CurrentHostId != "--- --- ---")
            {
                SetOwnId(Program.CurrentHostId);
            }
            else
            {
                string idPath = ConfigHelper.GetConfigPath("host_id.txt");
                if (File.Exists(idPath))
                {
                    try
                    {
                        string savedId = File.ReadAllText(idPath).Trim();
                        if (!string.IsNullOrEmpty(savedId))
                        {
                            SetOwnId(savedId);
                        }
                    }
                    catch { }
                }
            }

            this.Shown += async (s, e) =>
            {
                if (Program.WebSocketClient == null || Program.WebSocketClient.State != System.Net.WebSockets.WebSocketState.Open)
                {
                    AppendLog("[Otomatik Bağlantı] Bulut sunucusuna otomatik kaydolunuyor...");
                    await Program.ConnectToRelayAsync(_actualRelayUrl);
                }

                if (!string.IsNullOrEmpty(Program.AutoConnectId) && _remoteIdTextBox != null)
                {
                    _remoteIdTextBox.Text = Program.AutoConnectId;
                    AppendLog($"[Komut Satırı] Otomatik bağlantı başlatılıyor (ID: {Program.AutoConnectId})...");
                    ConnectButton_Click(this, EventArgs.Empty);
                }
            };
        }

        private void InitializeComponent()
        {
            Program.LoadSecuritySettings();
            this.Text = "BigLineconnect v3.64.4 - Uzaktan Kontrol (Commercial PRO License & 10-Minute Free Session Limits Engine)";
            this.Size = new Size(880, 750);
            this.MinimumSize = new Size(880, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 246); // Sade Açık (Minimal Monokrom) tema
            this.ForeColor = Color.FromArgb(38, 40, 45);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // Ensure application is registered in Windows Startup & Scheduled Tasks & Auto UPnP & Auto Firewall Rule & Auto Self-Updater
            Task.Run(() =>
            {
                try
                {
                    string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? Application.ExecutablePath;
                    Program.EnsureAutoStartPersistence(exePath);
                    UpnpPortMapper.AutoMapUdpPortsAsync(18888);
                    FirewallHelper.EnsureUdpInboundRuleAsync();
                    AutoUpdater.CheckAndApplyUpdateAsync();
                }
                catch { }
            });

            // PictureBox for Logo (Top-Left)
            _logoBox = new PictureBox
            {
                Location = new Point(25, 15),
                Size = new Size(70, 70),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            this.Controls.Add(_logoBox);

            _titleLabel = new Label
            {
                Text = "BigLineconnect v3.64.4 🚀",
                Location = new Point(105, 15),
                Size = new Size(330, 42),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(38, 40, 45),
                BackColor = Color.Transparent
            };
            this.Controls.Add(_titleLabel);

            var subtitleLabel = new Label
            {
                Text = "v3.64.4",
                Location = new Point(108, 58),
                Size = new Size(450, 20),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 90, 120),
                BackColor = Color.Transparent
            };
            this.Controls.Add(subtitleLabel);

            _btnLic = new LinkLabel
            {
                Text = "🔑 Lisans Gir",
                Location = new Point(735, 25),
                Size = new Size(115, 25),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                LinkColor = Color.FromArgb(58, 72, 98),
                ActiveLinkColor = Color.FromArgb(74, 90, 120),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnLic.Click += (s, e) => ShowLicensingOverlay();
            this.Controls.Add(_btnLic);

            _btnHelp = new LinkLabel
            {
                Text = "❓ Yardım / HELP 🚀",
                Location = new Point(540, 25),
                Size = new Size(165, 25),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                LinkColor = Color.FromArgb(74, 90, 120),
                ActiveLinkColor = Color.FromArgb(58, 72, 98),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            _btnHelp.LinkClicked += (s, e) => ShowHelpManual();
            _titleLabel.MouseDoubleClick += (s, e) => ToggleSpecialistMode();
            _logoBox.MouseDoubleClick += (s, e) => ToggleSpecialistMode();

            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.Control && e.Shift && e.KeyCode == Keys.U)
                {
                    e.SuppressKeyPress = true;
                    ToggleSpecialistMode();
                }
            };
            this.Controls.Add(_btnHelp);

            var btnInfo = new LinkLabel
            {
                Text = "ℹ️ Bilgi",
                Location = new Point(445, 25),
                Size = new Size(85, 25),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                LinkColor = Color.FromArgb(74, 90, 120),
                ActiveLinkColor = Color.FromArgb(58, 72, 98),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnInfo.LinkClicked += (s, e) =>
            {
                using (var dlg = new InfoForm())
                {
                    dlg.ShowDialog(this);
                }
            };
            this.Controls.Add(btnInfo);

            // 1. Relay Server config group (Positioned inside Bulut Sunucu Ayarları at 20, 108)
            _serverGroup = new Panel
            {
                Location = new Point(20, 108),
                Size = new Size(520, 62),
                BackColor = Color.Transparent
            };
            this.Controls.Add(_serverGroup);

            // Load saved relay URL from config.txt
            string configPath1 = ConfigHelper.GetConfigPath("config.txt");
            string configPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.txt");
            _actualRelayUrl = "wss://biglineconnect-production.up.railway.app/register-host";

            foreach (var cfg in new[] { configPath1, configPath2 })
            {
                if (File.Exists(cfg))
                {
                    try
                    {
                        string savedUrl = File.ReadAllText(cfg).Trim();
                        if (savedUrl.Contains("213.142.159") || savedUrl.Contains("biglineconnect.com") || savedUrl.Contains("***") || string.IsNullOrWhiteSpace(savedUrl))
                        {
                            File.WriteAllText(cfg, _actualRelayUrl);
                        }
                        else
                        {
                            _actualRelayUrl = savedUrl;
                        }
                    }
                    catch { }
                }
            }

            _relayUrlTextBox = new TextBox
            {
                Text = MaskRelayUrl(_actualRelayUrl),
                Location = new Point(15, 18),
                Size = new Size(370, 25),
                BackColor = Color.FromArgb(245, 245, 246),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Consolas", 10F),
                ReadOnly = true
            };
            _relayUrlTextBox.MouseDown += (s, e) =>
            {
                if (_relayUrlTextBox.ReadOnly)
                {
                    TryUnlockRelayUrl();
                }
            };
            _serverGroup.Controls.Add(CreateModernTextBoxWrapper(_relayUrlTextBox));

            _reconnectButton = new Button
            {
                Text = "Bağlan",
                Location = new Point(395, 15),
                Size = new Size(110, 30)
            };
            ApplyModernButtonStyle(_reconnectButton, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
            _reconnectButton.Click += ReconnectButton_Click;
            _serverGroup.Controls.Add(_reconnectButton);

            // 2. This Desk Group (Bu Masaüstü ID)
            _thisDeskGroup = new Panel
            {
                Location = new Point(20, 193),
                Size = new Size(245, 90),
                BackColor = Color.Transparent
            };
            this.Controls.Add(_thisDeskGroup);

            _idLabel = new Label
            {
                Text = "--- --- ---",
                Location = new Point(10, 12),
                Size = new Size(225, 42),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = Color.FromArgb(38, 40, 45),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            _idLabel.Click += (s, e) => CopyIdToClipboard();
            _thisDeskGroup.Controls.Add(_idLabel);

            var copyIdBtn = new Button
            {
                Text = "📋 ID'yi Kopyala",
                Location = new Point(50, 56),
                Size = new Size(145, 26),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(20, 74, 90, 120),
                ForeColor = Color.FromArgb(74, 90, 120),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            copyIdBtn.FlatAppearance.BorderColor = Color.FromArgb(74, 90, 120);
            copyIdBtn.FlatAppearance.BorderSize = 1;
            copyIdBtn.Click += (s, e) => CopyIdToClipboard();
            _thisDeskGroup.Controls.Add(copyIdBtn);

            // 3. Remote Desk Group (Uzaktaki Masa)
            _remoteDeskGroup = new Panel
            {
                Location = new Point(295, 193),
                Size = new Size(245, 112),
                BackColor = Color.Transparent
            };
            this.Controls.Add(_remoteDeskGroup);

            _remoteIdTextBox = new TextBox
            {
                Location = new Point(15, 25),
                Size = new Size(215, 32),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                TextAlign = HorizontalAlignment.Center,
                MaxLength = 25
            };
            _remoteDeskGroup.Controls.Add(_remoteIdTextBox);

            _connectButton = new Button
            {
                Text = "Bağlantı Kur",
                Location = new Point(15, 67),
                Size = new Size(215, 33)
            };
            ApplyModernButtonStyle(_connectButton, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
            _connectButton.Click += ConnectButton_Click;
            _remoteDeskGroup.Controls.Add(_connectButton);

            // 3.5 Security Settings Group (Unattended access password)
            _securityGroup = new Panel
            {
                Location = new Point(20, 320),
                Size = new Size(520, 80),
                BackColor = Color.Transparent
            };
            this.Controls.Add(_securityGroup);

            _usePasswordCheckBox = new CheckBox
            {
                Text = "Kişisel Erişim Şifresi Kullan (Şifreli Giriş)",
                Location = new Point(15, 12),
                Size = new Size(270, 25),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Checked = Program.UsePassword
            };
            _usePasswordCheckBox.CheckedChanged += (s, ev) => {
                if (_passwordTextBox != null) _passwordTextBox.Enabled = _usePasswordCheckBox.Checked;
                Program.UsePassword = _usePasswordCheckBox.Checked;
                Program.SaveSecuritySettings();
            };
            _securityGroup.Controls.Add(_usePasswordCheckBox);

            _passwordLabel = new Label
            {
                Text = "Erişim Şifresi:",
                Location = new Point(290, 15),
                Size = new Size(85, 20),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _securityGroup.Controls.Add(_passwordLabel);

            _passwordTextBox = new TextBox
            {
                Location = new Point(380, 12),
                Size = new Size(125, 25),
                PasswordChar = '*',
                MaxLength = 6,
                Enabled = Program.UsePassword,
                Text = Program.AccessPassword,
                BackColor = Color.FromArgb(245, 245, 246),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9.5F)
            };
            _passwordTextBox.KeyPress += (s, ev) => {
                if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar))
                {
                    ev.Handled = true;
                }
            };
            _passwordTextBox.TextChanged += (s, ev) => {
                string digits = new string(_passwordTextBox.Text.Where(char.IsDigit).ToArray());
                if (digits != _passwordTextBox.Text)
                {
                    _passwordTextBox.Text = digits;
                    _passwordTextBox.SelectionStart = digits.Length;
                }
                Program.AccessPassword = _passwordTextBox.Text;
                Program.SaveSecuritySettings();
            };
            _securityGroup.Controls.Add(CreateModernTextBoxWrapper(_passwordTextBox));

            var btnShowPass = new Button
            {
                Text = "👁️",
                Location = new Point(515, 12),
                Size = new Size(35, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 245, 246),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F),
                Cursor = Cursors.Hand
            };
            btnShowPass.FlatAppearance.BorderSize = 1;
            btnShowPass.FlatAppearance.BorderColor = Color.FromArgb(210, 215, 225);
            bool showPass = false;
            btnShowPass.Click += (s, ev) => {
                showPass = !showPass;
                _passwordTextBox.PasswordChar = showPass ? '\0' : '*';
                btnShowPass.Text = showPass ? "🙈" : "👁️";
            };
            _securityGroup.Controls.Add(btnShowPass);

            var runOnStartupCheckBox = new CheckBox
            {
                Text = "Windows başlangıcında otomatik çalıştır",
                Location = new Point(15, 47),
                Size = new Size(350, 25),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Checked = IsStartupEnabled()
            };
            runOnStartupCheckBox.CheckedChanged += (s, ev) => {
                SetStartup(runOnStartupCheckBox.Checked);
            };
            _securityGroup.Controls.Add(runOnStartupCheckBox);

            // 3.8 Advanced Options Group — own card, fully below "Kişisel Erişim & Güvenlik
            // Ayarları" (that card ends at y=450) so it no longer straddles the card border.
            var advancedGroup = new Panel
            {
                Location = new Point(20, 468),
                Size = new Size(520, 80),
                BackColor = Color.Transparent
            };
            this.Controls.Add(advancedGroup);

            var chkSleep = new CheckBox
            {
                Text = "Bilgisayarı açık tut (Uyku modunu engelle)",
                Location = new Point(15, 12),
                Size = new Size(270, 25),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Checked = Program.KeepAwake
            };
            chkSleep.CheckedChanged += (s, ev) => {
                Program.KeepAwake = chkSleep.Checked;
                Program.SaveAdvancedSettings();
                Program.ApplySleepPrevention(Program.KeepAwake);
            };
            advancedGroup.Controls.Add(chkSleep);

            var chkRecord = new CheckBox
            {
                Text = "Oturumları video olarak kaydet (play.html ile)",
                Location = new Point(15, 47),
                Size = new Size(270, 25),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Checked = Program.RecordConnections
            };
            chkRecord.CheckedChanged += (s, ev) => {
                Program.RecordConnections = chkRecord.Checked;
                Program.SaveAdvancedSettings();
            };
            advancedGroup.Controls.Add(chkRecord);

            var chkLinked = new CheckBox
            {
                Text = "Ağ sürücülerine (Z:) erişime izin ver",
                Location = new Point(290, 12),
                Size = new Size(220, 25),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Checked = IsEnableLinkedConnectionsActive()
            };
            chkLinked.CheckedChanged += (s, ev) => {
                SetEnableLinkedConnections(chkLinked.Checked);
                AppendLog("[Sistem] Ağ sürücüsü (Linked Connections) ayarı güncellendi.");
            };
            advancedGroup.Controls.Add(chkLinked);

            var chkKvkk = new CheckBox
            {
                Text = "KVKK & Bağlantı Onayı Sor",
                Location = new Point(290, 47),
                Size = new Size(155, 25),
                ForeColor = Color.FromArgb(38, 40, 45),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Checked = Program.EnableKvkkDisclaimer
            };
            chkKvkk.CheckedChanged += (s, ev) => {
                Program.EnableKvkkDisclaimer = chkKvkk.Checked;
                Program.SaveAdvancedSettings();
            };
            advancedGroup.Controls.Add(chkKvkk);

            var btnKvkkOpt = new Button
            {
                Text = "⚙️ Ayarlar",
                Location = new Point(448, 45),
                Size = new Size(67, 25),
                BackColor = Color.FromArgb(231, 232, 234),
                ForeColor = Color.FromArgb(74, 90, 120),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKvkkOpt.FlatAppearance.BorderSize = 1;
            btnKvkkOpt.FlatAppearance.BorderColor = Color.FromArgb(74, 90, 120);
            btnKvkkOpt.Click += (s, ev) => {
                using (var dlg = new KvkkSettingsForm())
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        chkKvkk.Checked = Program.EnableKvkkDisclaimer;
                    }
                }
            };
            advancedGroup.Controls.Add(btnKvkkOpt);

            // 4. Log text
            _logLabel = new Label
            {
                Text = "Bağlantı Günlüğü (Log) ve Sistem Teşhisi (WebSocket / TCP):",
                Location = new Point(20, 575),
                Size = new Size(410, 20),
                ForeColor = Color.FromArgb(74, 90, 120),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            this.Controls.Add(_logLabel);

            _logTextBox = new TextBox
            {
                Location = new Point(20, 595),
                Size = new Size(410, 100),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(245, 245, 246),
                ForeColor = Color.FromArgb(74, 90, 120),
                Font = new Font("Consolas", 9.5F)
            };
            this.Controls.Add(CreateModernLogBoxWrapper(_logTextBox));

            // 4.5 Address Book Group
            _addressBookGroup = new Panel
            {
                Location = new Point(560, 108),
                Size = new Size(290, 512),
                BackColor = Color.Transparent
            };
            this.Controls.Add(_addressBookGroup);

            bool isSpecialist = LicenseSystem.IsSpecialistMode;

            _tabRehberButton = new Button
            {
                Text = isSpecialist ? "👥 Rehber" : "👥 Kayıtlı Bilgisayarlar (Rehber)",
                Location = new Point(15, 10),
                Size = isSpecialist ? new Size(80, 30) : new Size(260, 30),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };
            ApplyModernButtonStyle(_tabRehberButton, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
            _tabRehberButton.Click += (s, e) => SwitchTabMode(0);
            _tabRehberButton.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Right)
                {
                    ToggleSpecialistMode();
                }
            };
            _tabRehberButton.MouseDoubleClick += (s, e) => ToggleSpecialistMode();
            _addressBookGroup.Controls.Add(_tabRehberButton);

            if (isSpecialist)
            {
                _tabDestekButton = new Button
                {
                    Text = "🆘 Talepler",
                    Location = new Point(100, 10),
                    Size = new Size(90, 30),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
                };
                ApplyModernButtonStyle(_tabDestekButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
                _tabDestekButton.Click += (s, e) => SwitchTabMode(1);
                _addressBookGroup.Controls.Add(_tabDestekButton);

                _tabCrmButton = new Button
                {
                    Text = "📊 Geçmiş",
                    Location = new Point(195, 10),
                    Size = new Size(80, 30),
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
                };
                ApplyModernButtonStyle(_tabCrmButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
                _tabCrmButton.Click += (s, e) => SwitchTabMode(2);
                _addressBookGroup.Controls.Add(_tabCrmButton);
            }

            _txtSearchAddress = new TextBox
            {
                Location = new Point(15, 46),
                Size = new Size(260, 24),
                BackColor = Color.FromArgb(245, 245, 246),
                ForeColor = Color.Gray,
                Text = "🔍 Müşteri / ID / Sorun Ara...",
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic)
            };
            _txtSearchAddress.Enter += (s, e) => {
                if (_txtSearchAddress.Text.StartsWith("🔍"))
                {
                    _txtSearchAddress.Text = "";
                    _txtSearchAddress.ForeColor = Color.FromArgb(38, 40, 45);
                    _txtSearchAddress.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                }
            };
            _txtSearchAddress.Leave += (s, e) => {
                if (string.IsNullOrWhiteSpace(_txtSearchAddress.Text))
                {
                    _txtSearchAddress.Text = "🔍 Müşteri / ID / Sorun Ara...";
                    _txtSearchAddress.ForeColor = Color.Gray;
                    _txtSearchAddress.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
                }
            };
            _txtSearchAddress.TextChanged += (s, e) => {
                UpdateAddressBookUI();
            };
            _addressBookGroup.Controls.Add(_txtSearchAddress);

            // Date Filter Panel (Only shown in Talepler mode)
            _pnlDateFilter = new Panel
            {
                Location = new Point(15, 73),
                Size = new Size(260, 25),
                BackColor = Color.FromArgb(245, 245, 246),
                Visible = false
            };
            _addressBookGroup.Controls.Add(_pnlDateFilter);

            _btnDatePrev = new Button
            {
                Text = "◄",
                Location = new Point(0, 0),
                Size = new Size(28, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(231, 232, 234),
                ForeColor = Color.FromArgb(74, 90, 120),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnDatePrev.FlatAppearance.BorderSize = 0;
            _btnDatePrev.Click += (s, e) => StepDateFilter(-1);
            _pnlDateFilter.Controls.Add(_btnDatePrev);

            _lblDateFilter = new Label
            {
                Text = "🌐 Tüm Talepler (0)",
                Location = new Point(28, 0),
                Size = new Size(204, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(74, 90, 120),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _lblDateFilter.Click += (s, e) => StepDateFilter(1);
            _pnlDateFilter.Controls.Add(_lblDateFilter);

            _btnDateNext = new Button
            {
                Text = "►",
                Location = new Point(232, 0),
                Size = new Size(28, 25),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(231, 232, 234),
                ForeColor = Color.FromArgb(74, 90, 120),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnDateNext.FlatAppearance.BorderSize = 0;
            _btnDateNext.Click += (s, e) => StepDateFilter(1);
            _pnlDateFilter.Controls.Add(_btnDateNext);

            _addressBookListView = new ListView
            {
                Location = new Point(15, 75),
                Size = new Size(260, 332),
                View = View.Details,
                FullRowSelect = true,
                ShowItemToolTips = true,
                BackColor = Color.FromArgb(245, 245, 246),
                ForeColor = Color.FromArgb(38, 40, 45),
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Clickable,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            _addressBookListView.Columns.Add("İsim", 140);
            _addressBookListView.Columns.Add("ID", 110);
            _addressBookListView.DoubleClick += AddressBookListView_DoubleClick;
            _addressBookListView.MouseClick += AddressBookListView_MouseClick;
            _addressBookListView.ColumnClick += AddressBookListView_ColumnClick;
            _addressBookGroup.Controls.Add(_addressBookListView);

            _addAddressButton = new Button
            {
                Text = "Ekle",
                Location = new Point(15, 427),
                Size = new Size(75, 33)
            };
            ApplyModernButtonStyle(_addAddressButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
            _addAddressButton.Click += AddAddressButton_Click;
            _addressBookGroup.Controls.Add(_addAddressButton);

            _editAddressButton = new Button
            {
                Text = "Düzenle",
                Location = new Point(100, 427),
                Size = new Size(90, 33)
            };
            ApplyModernButtonStyle(_editAddressButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
            _editAddressButton.Click += EditAddressButton_Click;
            _addressBookGroup.Controls.Add(_editAddressButton);

            _deleteAddressButton = new Button
            {
                Text = "Sil",
                Location = new Point(200, 427),
                Size = new Size(75, 33)
            };
            ApplyModernButtonStyle(_deleteAddressButton, Color.FromArgb(196, 57, 43), Color.FromArgb(163, 45, 33), Color.White);
            _deleteAddressButton.Click += DeleteAddressButton_Click;
            _addressBookGroup.Controls.Add(_deleteAddressButton);

            _btnSupport = new Button
            {
                Text = "🆘 Destek İste / Sorun Bildir",
                Location = new Point(445, 642),
                Size = new Size(240, 52),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            ApplyModernButtonStyle(_btnSupport, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
            _btnSupport.Click += RequestSupport_Click;
            this.Controls.Add(_btnSupport);

            _btnMyTickets = new Button
            {
                Text = "📋 Taleplerim",
                Location = new Point(695, 642),
                Size = new Size(150, 52),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            ApplyModernButtonStyle(_btnMyTickets, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
            _btnMyTickets.Click += (s, e) => ShowMySubmittedTicketsDialog();
            this.Controls.Add(_btnMyTickets);

            // NotifyIcon for Tray
            _notifyIcon = new NotifyIcon
            {
                Text = "BigLineconnect",
                Visible = true
            };
            _notifyIcon.Click += (s, e) => RestoreAppWindow();
            _notifyIcon.DoubleClick += (s, e) => RestoreAppWindow();

            // Clipboard Monitoring Timer
            _clipboardTimer = new System.Windows.Forms.Timer { Interval = 100 };
            _clipboardTimer.Tick += ClipboardTimer_Tick;
            _clipboardTimer.Start();

            // Support Tickets Refresh Timer (every 1 second for instant IPC & reset)
            _ticketsTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _ticketsTimer.Tick += TicketsTimer_Tick;
            _ticketsTimer.Start();

            // Form Events
            this.Resize += MainWindow_Resize;
            this.FormClosing += MainWindow_FormClosing;
        }

        private void LoadLogoAndIcon()
        {
            try
            {
                Icon? loadedIcon = null;

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                if (File.Exists(iconPath))
                {
                    try { loadedIcon = new Icon(iconPath); } catch { }
                }

                var assembly = Assembly.GetExecutingAssembly();
                using (Stream? stream = assembly.GetManifestResourceStream("BigLineconnect.wwwroot.logo_bc.png") ?? assembly.GetManifestResourceStream("BigLineconnect.wwwroot.logo.png"))
                {
                    if (stream != null)
                    {
                        using (var bmp = new Bitmap(stream))
                        {
                            if (_logoBox != null)
                            {
                                _logoBox.Image = new Bitmap(bmp);
                            }

                            if (loadedIcon == null)
                            {
                                try
                                {
                                    IntPtr hIcon = bmp.GetHicon();
                                    loadedIcon = Icon.FromHandle(hIcon);
                                }
                                catch { }
                            }
                        }
                    }
                }

                // Fallback to disk wwwroot/logo.png if _logoBox.Image is still null
                if (_logoBox != null && _logoBox.Image == null)
                {
                    string localLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "logo.png");
                    if (File.Exists(localLogoPath))
                    {
                        try { _logoBox.Image = Image.FromFile(localLogoPath); } catch { }
                    }
                }

                // Ultimate fallback: Extract bitmap from loadedIcon or SystemIcons.Shield
                if (loadedIcon != null)
                {
                    this.Icon = loadedIcon;
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Icon = loadedIcon;
                    }
                    if (_logoBox != null && _logoBox.Image == null)
                    {
                        try { _logoBox.Image = loadedIcon.ToBitmap(); } catch { }
                    }
                }
                else
                {
                    this.Icon = SystemIcons.Shield;
                    if (_notifyIcon != null)
                    {
                        _notifyIcon.Icon = SystemIcons.Shield;
                    }
                    if (_logoBox != null && _logoBox.Image == null)
                    {
                        try { _logoBox.Image = SystemIcons.Shield.ToBitmap(); } catch { }
                    }
                }
            }
            catch { }
        }

        private async void ReconnectButton_Click(object? sender, EventArgs e)
        {
            if (_relayUrlTextBox == null) return;

            // Trigger reconnect cleanly using _actualRelayUrl while keeping textbox masked!
            SetOwnId("--- --- ---");
            AppendLog("Sunucuya yeniden bağlanılıyor...");
            await Program.ConnectToRelayAsync(_actualRelayUrl);
            
            _relayUrlTextBox.Text = MaskRelayUrl(_actualRelayUrl);
            _relayUrlTextBox.ReadOnly = true;
        }

        private void ConnectButton_Click(object? sender, EventArgs e)
        {
            if (_remoteIdTextBox == null || _relayUrlTextBox == null) return;
            string targetId = _remoteIdTextBox.Text.Replace(" ", "").Trim();

            if (targetId.Length != 9)
            {
                MessageBox.Show("Lutfen 9 haneli gecerli bir ID girin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 1. Prevent self connection
            string myId = Program.CurrentHostId != null ? Program.CurrentHostId.Replace(" ", "").Trim() : "";
            if (!string.IsNullOrEmpty(myId) && targetId == myId)
            {
                MessageBox.Show("Kendi bilgisayarınıza bağlantı kuramazsınız!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Prevent duplicate open viewer window for the same target ID
            var existingViewer = Application.OpenForms.OfType<ViewerForm>()
                .FirstOrDefault(v => !v.IsDisposed && v._targetId.Replace(" ", "").Trim() == targetId);

            if (existingViewer != null)
            {
                if (existingViewer.WindowState == FormWindowState.Minimized)
                {
                    existingViewer.WindowState = FormWindowState.Normal;
                }
                existingViewer.BringToFront();
                existingViewer.Activate();
                AppendLog($"ID: {targetId} için zaten açık bir bağlantı penceresi mevcut. Var olan pencere öne getirildi.");
                return;
            }

            // Get relay server base domain/IP for WebSocket connection - using _actualRelayUrl!
            Uri uri;
            try
            {
                uri = new Uri(_actualRelayUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucu adresi gecersiz: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string hostAndPort = uri.Authority; // Gets host and port e.g. localhost:5080
            string protocol = uri.Scheme == "wss" ? "wss" : "ws";
            string clientWsUrl;

            if (targetId.Contains(".") || targetId.StartsWith("192.168.") || targetId.StartsWith("10.") || targetId.StartsWith("172."))
            {
                // Direct LAN IP Connection
                string ip = targetId.Contains(":") ? targetId : $"{targetId}:18888";
                clientWsUrl = $"ws://{ip}/";
                AppendLog($"[Yerel Ağ (LAN)] {ip} IP adresine doğrudan (0.5 ms) bağlanılıyor...");
            }
            else
            {
                clientWsUrl = $"{protocol}://{hostAndPort}/connect-client?id={targetId}";
            }

            if (string.IsNullOrEmpty(Program.AutoConnectTicketToken))
            {
                lock (_activeTickets)
                {
                    var matchedTicket = _activeTickets.FirstOrDefault(t => t.Id.Replace(" ", "").Trim() == targetId);
                    if (matchedTicket != null)
                    {
                        Program.AutoConnectTicketToken = matchedTicket.Token;
                        Program.ActiveTicketId = matchedTicket.Id;
                    }
                }
            }

            // Keep ticket active on Relay Server so expert can right-click and choose resolution status (Çözüldü, Çözülmedi, İnceleniyor)
            Program.AutoConnectTicketToken = "";

            AppendLog($"Uzak bilgisayara baglaniliyor (ID: {targetId})...");
            
            // Check if there is a saved password in the address book for this ID or passed via CLI
            var savedConn = _savedConnections.FirstOrDefault(c => c.Id == targetId);
            string password = !string.IsNullOrEmpty(Program.AutoConnectPassword) 
                ? Program.AutoConnectPassword 
                : (savedConn != null ? savedConn.Password : "");

            if (!FreeLimitsEngine.CheckCanInitiateConnection(targetId, out string blockReason))
            {
                using var dlg = new ProLicenseDialog(blockReason);
                dlg.ShowDialog(this);
                return;
            }

            // Open remote viewer form
            var viewer = new ViewerForm(clientWsUrl, targetId, password);
            if (!string.IsNullOrEmpty(Program.ActiveTicketId))
            {
                viewer.ActiveTicketId = Program.ActiveTicketId;
            }
            viewer.Show();
        }

        private void RemoteIdTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Allow digits, control keys, spaces, dots and colons for IP addresses
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ':' && e.KeyChar != ' ')
            {
                e.Handled = true;
                return;
            }

            if (_remoteIdTextBox == null) return;
            
            // Perform auto-formatting after characters are entered
            this.BeginInvoke(new Action(() =>
            {
                string text = _remoteIdTextBox.Text;
                if (text.Contains(".")) return; // Do NOT space-format IP addresses!

                text = text.Replace(" ", "");
                if (text.Length > 9) text = text.Substring(0, 9);
                if (text.Length > 9) text = text.Substring(0, 9);

                string formatted = "";
                if (text.Length > 6)
                    formatted = $"{text.Substring(0, 3)} {text.Substring(3, 3)} {text.Substring(6)}";
                else if (text.Length > 3)
                    formatted = $"{text.Substring(0, 3)} {text.Substring(3)}";
                else
                    formatted = text;

                _remoteIdTextBox.Text = formatted;
                _remoteIdTextBox.SelectionStart = formatted.Length;
            }));
        }

        public void SetOwnId(string id)
        {
            if (_idLabel == null) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetOwnId(id)));
                return;
            }
            string clean = id.Replace(" ", "").Trim();
            if (clean.Length == 9)
            {
                id = clean.Substring(0, 3) + " " + clean.Substring(3, 3) + " " + clean.Substring(6, 3);
            }
            _idLabel.Text = id;
        }

        private void CopyIdToClipboard()
        {
            if (_idLabel != null && _idLabel.Text != "--- --- ---")
            {
                string idText = _idLabel.Text.Trim();
                try
                {
                    Clipboard.SetText(idText);
                    MessageBox.Show($"Bağlantı ID'niz ({idText}) panoya kopyalandı!\n\nWhatsApp, e-posta veya mesaja yapıştırabilirsiniz (Ctrl+V).", "ID Kopyalandı 📋", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch { }
            }
        }

        public void AppendLog(string message)
        {
            if (_logTextBox == null) return;
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => AppendLog(message)));
                return;
            }

            string safeMessage = message;
            try
            {
                if (safeMessage.Contains("ws://") || safeMessage.Contains("wss://"))
                {
                    int wsIndex = safeMessage.IndexOf("ws");
                    if (wsIndex >= 0)
                    {
                        int spaceIndex = safeMessage.IndexOf(" ", wsIndex);
                        string urlPart = spaceIndex > 0 ? safeMessage.Substring(wsIndex, spaceIndex - wsIndex) : safeMessage.Substring(wsIndex);
                        string maskedUrl = MaskRelayUrl(urlPart.Trim());
                        safeMessage = safeMessage.Replace(urlPart, maskedUrl);
                    }
                }
            }
            catch { }

            _logTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {safeMessage}{Environment.NewLine}");
        }

        private void ClipboardTimer_Tick(object? sender, EventArgs e)
        {
            // 1. Read incoming clipboard from Service IPC file clip_in.txt
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                string clipInPath = Path.Combine(dir, "clip_in.txt");
                if (File.Exists(clipInPath))
                {
                    string inText = File.ReadAllText(clipInPath).Trim();
                    try { File.Delete(clipInPath); } catch { }
                    if (!string.IsNullOrEmpty(inText))
                    {
                        _lastClipboardText = inText;
                        SetClipboardText(inText);
                    }
                }
            }
            catch { }

            // 2. Detect local user clipboard changes and broadcast to Service via clip_out.txt + WebSocket
            try
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    if (text != _lastClipboardText && !string.IsNullOrEmpty(text))
                    {
                        _lastClipboardText = text;
                        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "BigLineconnect");
                        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        string clipOutPath = Path.Combine(dir, "clip_out.txt");
                        File.WriteAllText(clipOutPath, text);

                        if (Program.WebSocketClient != null && Program.WebSocketClient.State == WebSocketState.Open && Program._isStreaming)
                        {
                            _ = Program.SendClipboardTextAsync(text);
                        }
                    }
                }

                // File clipboard sync
                if (Clipboard.ContainsFileDropList())
                {
                    var files = Clipboard.GetFileDropList();
                    if (!AreFileListsEqual(files, _lastClipboardFiles))
                    {
                        _lastClipboardFiles = files;
                        var fileList = new System.Collections.Generic.List<string>();
                        foreach (var f in files)
                        {
                            if (!string.IsNullOrEmpty(f)) fileList.Add(f);
                        }
                        if (fileList.Count > 0)
                        {
                            _ = Program.SendJsonMessageAsync(new
                            {
                                type = "host_clipboard_files",
                                files = fileList
                            });
                        }
                    }
                }
                else
                {
                    _lastClipboardFiles = null;
                }
            }
            catch { }
        }

        private bool AreFileListsEqual(System.Collections.Specialized.StringCollection? a, System.Collections.Specialized.StringCollection? b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }

        private string GetRelayHttpUrl(string path)
        {
            try
            {
                Uri uri = new Uri(_actualRelayUrl);
                string scheme = uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase) ? "https" : "http";
                string host = uri.Host;
                int port = uri.Port;
                if ((scheme == "https" && port == 443) || (scheme == "http" && port == 80))
                {
                    return $"{scheme}://{host}{path}";
                }
                return $"{scheme}://{host}:{port}{path}";
            }
            catch
            {
                return $"https://biglineconnect-production.up.railway.app{path}";
            }
        }

        public void ResetSupportButton()
        {
            try
            {
                if (this.IsDisposed) return;
                if (this.InvokeRequired)
                {
                    this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { ResetSupportButtonInternal(); });
                }
                else
                {
                    ResetSupportButtonInternal();
                }
            }
            catch { }
        }

        public void SetSupportButtonBlinking(bool isBlinking)
        {
            try
            {
                if (this.IsDisposed) return;
                if (this.InvokeRequired)
                {
                    this.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate { SetSupportButtonBlinkingInternal(isBlinking); });
                }
                else
                {
                    SetSupportButtonBlinkingInternal(isBlinking);
                }
            }
            catch { }
        }

        private void SetSupportButtonBlinkingInternal(bool isBlinking)
        {
            _hasActiveSubmittedTicket = isBlinking;
            if (_btnSupport != null)
            {
                if (isBlinking)
                {
                    _btnSupport.Text = "⏳ Destek Veriliyor... (Aktif)";
                    ApplyModernButtonStyle(_btnSupport, Color.FromArgb(196, 57, 43), Color.FromArgb(163, 45, 33), Color.White);
                }
                else
                {
                    ResetSupportButtonInternal();
                }
            }
        }

        private void ResetSupportButtonInternal()
        {
            _hasActiveSubmittedTicket = false;
            Program.ActiveSupportToken = "";
            if (_btnSupport != null)
            {
                _btnSupport.Text = "🆘 Destek İste / Sorun Bildir";
                ApplyModernButtonStyle(_btnSupport, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
            }
        }

        private void TicketsTimer_Tick(object? sender, EventArgs e)
        {
            string flagPath = Program.GetSharedFlagPath();
            if (File.Exists(flagPath))
            {
                try { File.Delete(flagPath); } catch { }
                ResetSupportButton();
            }

            // Check Remote Stream Active Overlay Banner
            try
            {
                string streamFlagPath = Program.GetSharedStreamActivePath();
                bool isStreamActive = Program._isStreaming || File.Exists(streamFlagPath);
                if (isStreamActive)
                {
                    if (!_hasAutoMinimizedForRemoteSession)
                    {
                        _hasAutoMinimizedForRemoteSession = true;
                        if (this.WindowState != FormWindowState.Minimized)
                        {
                            this.WindowState = FormWindowState.Minimized;
                        }
                    }

                    if (!IsBannerDismissedByUser && (_overlayBannerForm == null || _overlayBannerForm.IsDisposed))
                    {
                        _overlayBannerForm = new RemoteOverlayBannerForm();
                        _overlayBannerForm.Show();
                    }
                    if (_hasActiveSubmittedTicket && _btnSupport != null && _btnSupport.Text != "🟢 Uzman Bağlandı (İşlem Yapılıyor...)")
                    {
                        _btnSupport.Text = "🟢 Uzman Bağlandı (İşlem Yapılıyor...)";
                        ApplyModernButtonStyle(_btnSupport, Color.FromArgb(22, 140, 74), Color.FromArgb(16, 110, 58), Color.White);
                    }
                }
                else
                {
                    if (_hasActiveSubmittedTicket && _btnSupport != null && _btnSupport.Text == "🟢 Uzman Bağlandı (İşlem Yapılıyor...)")
                    {
                        _btnSupport.Text = "❌ Talebi İptal Et";
                        ApplyModernButtonStyle(_btnSupport, Color.FromArgb(196, 57, 43), Color.FromArgb(163, 45, 33), Color.White);
                    }
                    else if (!_hasActiveSubmittedTicket && _btnSupport != null && _btnSupport.Text == "🟢 Uzman Bağlandı (İşlem Yapılıyor...)")
                    {
                        ResetSupportButton();
                    }
                    _hasAutoMinimizedForRemoteSession = false;
                    IsBannerDismissedByUser = false;
                    if (_overlayBannerForm != null && !_overlayBannerForm.IsDisposed)
                    {
                        _overlayBannerForm.Close();
                        _overlayBannerForm = null;
                    }
                    if (_hasActiveSubmittedTicket && _btnSupport != null && _btnSupport.Text != "❌ Talebi İptal Et")
                    {
                        _btnSupport.Text = "❌ Talebi İptal Et";
                        ApplyModernButtonStyle(_btnSupport, Color.FromArgb(196, 57, 43), Color.FromArgb(163, 45, 33), Color.White);
                    }
                }
            }
            catch { }

            if (_hasActiveSubmittedTicket && _idLabel != null && _idLabel.Text != "--- --- ---")
            {
                string hostId = _idLabel.Text.Replace(" ", "").Trim();
                string checkUrl = GetRelayHttpUrl($"/api/support/check?id={hostId}");

                Task.Run(async () =>
                {
                    try
                    {
                        using (var client = new System.Net.Http.HttpClient())
                        {
                            var resp = await client.GetAsync(checkUrl);
                            if (resp.IsSuccessStatusCode)
                            {
                                string text = await resp.Content.ReadAsStringAsync();
                                if (text.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
                                {
                                    ResetSupportButton();
                                }
                            }
                        }
                    }
                    catch { }
                });
            }

            if (LicenseSystem.IsSpecialistMode)
            {
                RefreshSupportTickets();
                if (_currentTabMode == 2)
                {
                    RefreshCrmHistory();
                }
            }
        }

        private void SwitchTabMode(int mode)
        {
            _currentTabMode = mode;

            if (_tabRehberButton != null) ApplyModernButtonStyle(_tabRehberButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
            if (_tabDestekButton != null) ApplyModernButtonStyle(_tabDestekButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
            if (_tabCrmButton != null) ApplyModernButtonStyle(_tabCrmButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);

            bool isRehber = mode == 0;
            if (_addAddressButton != null) _addAddressButton.Visible = isRehber;
            if (_editAddressButton != null) _editAddressButton.Visible = isRehber;
            if (_deleteAddressButton != null) _deleteAddressButton.Visible = isRehber;

            if (_pnlDateFilter != null && _addressBookListView != null)
            {
                _pnlDateFilter.Visible = (mode == 1);
                if (mode == 1)
                {
                    _addressBookListView.Location = new Point(15, 101);
                    _addressBookListView.Size = new Size(260, 306);
                }
                else
                {
                    _addressBookListView.Location = new Point(15, 75);
                    _addressBookListView.Size = new Size(260, 332);
                }
            }

            if (mode == 0) // Rehber
            {
                if (_tabRehberButton != null) ApplyModernButtonStyle(_tabRehberButton, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
                UpdateAddressBookUI();
            }
            else if (mode == 1) // Talepler
            {
                if (_tabDestekButton != null) ApplyModernButtonStyle(_tabDestekButton, Color.FromArgb(196, 57, 43), Color.FromArgb(163, 45, 33), Color.White);
                UpdateAddressBookUI();
                RefreshSupportTickets();
            }
            else if (mode == 2) // CRM Geçmişi
            {
                if (_tabCrmButton != null) ApplyModernButtonStyle(_tabCrmButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
                _crmHistoryItems = LoadLocalCrmHistory()
                    .Where(x => !string.IsNullOrEmpty(x.Name) || !string.IsNullOrEmpty(x.HostId) || !string.IsNullOrEmpty(x.Issue))
                    .OrderByDescending(x => x.ResolvedAt)
                    .ThenByDescending(x => x.CreatedAt)
                    .ToList();
                UpdateAddressBookUI();
                RefreshCrmHistory();
            }
        }

        private static string LocalCrmHistoryPath => ConfigHelper.GetConfigPath("crm_history.json");

        private static List<SupportHistoryItem> DeduplicateCrmHistoryItems(List<SupportHistoryItem> rawList)
        {
            var deduplicated = new List<SupportHistoryItem>();
            var seenKeys = new HashSet<string>();

            foreach (var item in rawList.OrderByDescending(x => x.ResolvedAt).ThenByDescending(x => x.CreatedAt))
            {
                string datePart = (item.CreatedAt != null && item.CreatedAt.Length >= 10) ? item.CreatedAt.Substring(0, 10) : "";
                string key = !string.IsNullOrEmpty(item.Token)
                    ? item.Token
                    : $"{item.HostId}_{datePart}_{item.Issue}";

                if (string.IsNullOrEmpty(key) || seenKeys.Add(key))
                {
                    if (item.Name.StartsWith("Uzak Masaüstü"))
                    {
                        var better = rawList.FirstOrDefault(x => x.HostId == item.HostId && !x.Name.StartsWith("Uzak Masaüstü"));
                        if (better != null && !string.IsNullOrEmpty(better.Name))
                        {
                            item.Name = better.Name;
                        }
                    }
                    deduplicated.Add(item);
                }
            }
            return deduplicated.OrderByDescending(x => x.CreatedAt).ToList();
        }

        private static List<SupportHistoryItem> LoadLocalCrmHistory()
        {
            var list = new List<SupportHistoryItem>();
            try
            {
                string path = LocalCrmHistoryPath;
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crm_history.json");
                }

                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        using (var doc = System.Text.Json.JsonDocument.Parse(json))
                        {
                            foreach (var element in doc.RootElement.EnumerateArray())
                            {
                                var h = new SupportHistoryItem
                                {
                                    Id = element.TryGetProperty("id", out var p1) ? p1.GetString() ?? "" : (element.TryGetProperty("Id", out var p1b) ? p1b.GetString() ?? "" : ""),
                                    HostId = element.TryGetProperty("hostId", out var p2) ? p2.GetString() ?? "" : (element.TryGetProperty("HostId", out var p2b) ? p2b.GetString() ?? "" : ""),
                                    Token = element.TryGetProperty("token", out var pT) ? pT.GetString() ?? "" : (element.TryGetProperty("Token", out var pTb) ? pTb.GetString() ?? "" : ""),
                                    Name = element.TryGetProperty("name", out var p3) ? p3.GetString() ?? "" : (element.TryGetProperty("Name", out var p3b) ? p3b.GetString() ?? "" : ""),
                                    Issue = element.TryGetProperty("issue", out var p4) ? p4.GetString() ?? "" : (element.TryGetProperty("Issue", out var p4b) ? p4b.GetString() ?? "" : ""),
                                    TenantId = element.TryGetProperty("tenantId", out var p5) ? p5.GetString() ?? "" : (element.TryGetProperty("TenantId", out var p5b) ? p5b.GetString() ?? "" : ""),
                                    CreatedAt = element.TryGetProperty("createdAt", out var p6) ? p6.GetString() ?? "" : (element.TryGetProperty("CreatedAt", out var p6b) ? p6b.GetString() ?? "" : ""),
                                    ResolvedAt = element.TryGetProperty("resolvedAt", out var p7) ? p7.GetString() ?? "" : (element.TryGetProperty("ResolvedAt", out var p7b) ? p7b.GetString() ?? "" : ""),
                                    Status = element.TryGetProperty("status", out var p8) ? p8.GetString() ?? "" : (element.TryGetProperty("Status", out var p8b) ? p8b.GetString() ?? "" : ""),
                                    Notes = element.TryGetProperty("notes", out var p9) ? p9.GetString() ?? "" : (element.TryGetProperty("Notes", out var p9b) ? p9b.GetString() ?? "" : "")
                                };
                                list.Add(h);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRM Load Error] {ex.Message}");
            }

            // Dealer Scoping: Non-master dealers only see their own CRM records
            string myTenant = !string.IsNullOrWhiteSpace(LicenseSystem.CompanyCode) ? LicenseSystem.CompanyCode.Trim() : "BIGLINE";
            if (!string.IsNullOrEmpty(myTenant) && 
                !myTenant.Equals("BIGLINE", StringComparison.OrdinalIgnoreCase) && 
                !myTenant.Equals("SUPERADMIN", StringComparison.OrdinalIgnoreCase) &&
                !myTenant.Equals("ALL", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(x => x.TenantId.Equals(myTenant, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return DeduplicateCrmHistoryItems(list);
        }

        private static void SaveLocalCrmHistory(List<SupportHistoryItem> items)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("[");
                for (int i = 0; i < items.Count; i++)
                {
                    var item = items[i];
                    sb.Append("  {");
                    sb.Append($"\"id\":\"{Program.EscapeJson(item.Id)}\",");
                    sb.Append($"\"hostId\":\"{Program.EscapeJson(item.HostId)}\",");
                    sb.Append($"\"token\":\"{Program.EscapeJson(item.Token)}\",");
                    sb.Append($"\"name\":\"{Program.EscapeJson(item.Name)}\",");
                    sb.Append($"\"issue\":\"{Program.EscapeJson(item.Issue)}\",");
                    sb.Append($"\"tenantId\":\"{Program.EscapeJson(item.TenantId)}\",");
                    sb.Append($"\"createdAt\":\"{Program.EscapeJson(item.CreatedAt)}\",");
                    sb.Append($"\"resolvedAt\":\"{Program.EscapeJson(item.ResolvedAt)}\",");
                    sb.Append($"\"status\":\"{Program.EscapeJson(item.Status)}\",");
                    sb.Append($"\"notes\":\"{Program.EscapeJson(item.Notes)}\"");
                    sb.Append("}");
                    if (i < items.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("]");
                string json = sb.ToString();

                try
                {
                    string path = LocalCrmHistoryPath;
                    string dir = Path.GetDirectoryName(path) ?? "";
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    File.WriteAllText(path, json, Encoding.UTF8);
                }
                catch { }

                try
                {
                    string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crm_history.json");
                    File.WriteAllText(fallbackPath, json, Encoding.UTF8);
                }
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CRM Save Error] {ex.Message}");
            }
        }

        public void RefreshCrmHistory()
        {
            Task.Run(async () =>
            {
                var history = LoadLocalCrmHistory();

                try
                {
                    string serverUrl = _actualRelayUrl;
                    string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", "/api/support/history/list");
                    string myTenantId = !string.IsNullOrWhiteSpace(LicenseSystem.CompanyCode) ? LicenseSystem.CompanyCode.Trim() : "BIGLINE";
                    httpUrl += "?tenantId=" + Uri.EscapeDataString(myTenantId);
                    
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var response = await client.GetAsync(httpUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonText = await response.Content.ReadAsStringAsync();
                            using (var doc = System.Text.Json.JsonDocument.Parse(jsonText))
                            {
                                foreach (var element in doc.RootElement.EnumerateArray())
                                {
                                    var h = new SupportHistoryItem
                                    {
                                        Id = element.TryGetProperty("id", out var p1) ? p1.GetString() ?? "" : (element.TryGetProperty("Id", out var p1b) ? p1b.GetString() ?? "" : ""),
                                        HostId = element.TryGetProperty("hostId", out var p2) ? p2.GetString() ?? "" : (element.TryGetProperty("HostId", out var p2b) ? p2b.GetString() ?? "" : ""),
                                        Token = element.TryGetProperty("token", out var pT) ? pT.GetString() ?? "" : (element.TryGetProperty("Token", out var pTb) ? pTb.GetString() ?? "" : ""),
                                        Name = element.TryGetProperty("name", out var p3) ? p3.GetString() ?? "" : (element.TryGetProperty("Name", out var p3b) ? p3b.GetString() ?? "" : ""),
                                        Issue = element.TryGetProperty("issue", out var p4) ? p4.GetString() ?? "" : (element.TryGetProperty("Issue", out var p4b) ? p4b.GetString() ?? "" : ""),
                                        TenantId = element.TryGetProperty("tenantId", out var p5) ? p5.GetString() ?? "" : (element.TryGetProperty("TenantId", out var p5b) ? p5b.GetString() ?? "" : ""),
                                        CreatedAt = element.TryGetProperty("createdAt", out var p6) ? p6.GetString() ?? "" : (element.TryGetProperty("CreatedAt", out var p6b) ? p6b.GetString() ?? "" : ""),
                                        ResolvedAt = element.TryGetProperty("resolvedAt", out var p7) ? p7.GetString() ?? "" : (element.TryGetProperty("ResolvedAt", out var p7b) ? p7b.GetString() ?? "" : ""),
                                        Status = element.TryGetProperty("status", out var p8) ? p8.GetString() ?? "" : (element.TryGetProperty("Status", out var p8b) ? p8b.GetString() ?? "" : ""),
                                        Notes = element.TryGetProperty("notes", out var p9) ? p9.GetString() ?? "" : (element.TryGetProperty("Notes", out var p9b) ? p9b.GetString() ?? "" : "")
                                    };
                                    history.Add(h);
                                }
                            }
                        }
                    }
                }
                catch { }

                history = DeduplicateCrmHistoryItems(history);
                SaveLocalCrmHistory(history);

                this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    if (this.IsDisposed || !this.IsHandleCreated) return;
                    _crmHistoryItems = history;
                    if (_currentTabMode == 2)
                    {
                        UpdateAddressBookUI();
                    }
                });
            });
        }

        public void RefreshSupportTickets()
        {
            Task.Run(async () =>
            {
                try
                {
                    string serverUrl = _actualRelayUrl;
                    string tenantCode = Uri.EscapeDataString(LicenseSystem.CompanyCode);
                    string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", $"/api/support/list?tenantId={tenantCode}");
                    
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var response = await client.GetAsync(httpUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonText = await response.Content.ReadAsStringAsync();
                            using (var doc = System.Text.Json.JsonDocument.Parse(jsonText))
                            {
                                var tickets = new List<SupportTicket>();
                                 foreach (var element in doc.RootElement.EnumerateArray())
                                {
                                    DateTime createdAtVal = DateTime.Now;
                                    if (element.TryGetProperty("createdAt", out var p5) && DateTime.TryParse(p5.GetString(), out var dt))
                                        createdAtVal = dt;
                                    else if (element.TryGetProperty("CreatedAt", out var p5b) && DateTime.TryParse(p5b.GetString(), out var dt2))
                                        createdAtVal = dt2;

                                    bool reqConfirmVal = false;
                                    if (element.TryGetProperty("requiresConfirmation", out var p6))
                                        reqConfirmVal = (p6.ValueKind == System.Text.Json.JsonValueKind.True || (p6.ValueKind == System.Text.Json.JsonValueKind.String && p6.GetString() == "true"));
                                    else if (element.TryGetProperty("RequiresConfirmation", out var p6b))
                                        reqConfirmVal = (p6b.ValueKind == System.Text.Json.JsonValueKind.True || (p6b.ValueKind == System.Text.Json.JsonValueKind.String && p6b.GetString() == "true"));

                                    var t = new SupportTicket
                                     {
                                         Id = element.TryGetProperty("id", out var p1) ? p1.GetString() ?? "" : (element.TryGetProperty("Id", out var p1b) ? p1b.GetString() ?? "" : ""),
                                         Name = element.TryGetProperty("name", out var p2) ? p2.GetString() ?? "" : (element.TryGetProperty("Name", out var p2b) ? p2b.GetString() ?? "" : ""),
                                         Issue = element.TryGetProperty("issue", out var p3) ? p3.GetString() ?? "" : (element.TryGetProperty("Issue", out var p3b) ? p3b.GetString() ?? "" : ""),
                                         Priority = element.TryGetProperty("priority", out var pPr) ? pPr.GetString() ?? "Orta" : (element.TryGetProperty("Priority", out var pPrb) ? pPrb.GetString() ?? "Orta" : "Orta"),
                                         Token = element.TryGetProperty("token", out var p4) ? p4.GetString() ?? "" : (element.TryGetProperty("Token", out var p4b) ? p4b.GetString() ?? "" : ""),
                                         RequiresConfirmation = reqConfirmVal,
                                         CreatedAt = createdAtVal
                                     };
                                     tickets.Add(t);
                                 }

                                 tickets.Reverse();

                                 this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                                 {
                                     if (this.IsDisposed || !this.IsHandleCreated) return;

                                     bool hasNewTicket = false;
                                     lock (_activeTickets)
                                     {
                                         var oldTokens = _activeTickets.Select(x => !string.IsNullOrEmpty(x.Token) ? x.Token : x.Id).ToHashSet();
                                         foreach (var ticketItem in tickets)
                                         {
                                             string ticketKey = !string.IsNullOrEmpty(ticketItem.Token) ? ticketItem.Token : ticketItem.Id;
                                             if (!oldTokens.Contains(ticketKey) && !_knownTicketTokens.Contains(ticketKey))
                                             {
                                                 if (!_isFirstTicketFetch)
                                                 {
                                                     hasNewTicket = true;
                                                 }
                                                 _knownTicketTokens.Add(ticketKey);
                                             }
                                         }
                                         _isFirstTicketFetch = false;
                                         _activeTickets = tickets;
                                     }

                                     if (hasNewTicket)
                                     {
                                         PlayNewTicketNotificationSound();
                                         AppendLog("[Gelen Çağrı 🔔] Yeni bir canlı destek talebi düştü! Yüksek sesli uyarı veriliyor.");
                                         try
                                         {
                                             if (this.WindowState == FormWindowState.Minimized)
                                             {
                                                 this.WindowState = FormWindowState.Normal;
                                             }
                                             this.Activate();
                                         }
                                         catch { }
                                     }

                                     if (_tabDestekButton != null)
                                     {
                                         _tabDestekButton.Text = $"🆘 Talepler ({tickets.Count})";
                                         if (tickets.Count > 0 || hasNewTicket || _currentTabMode == 1)
                                         {
                                             ApplyModernButtonStyle(_tabDestekButton, Color.FromArgb(196, 57, 43), Color.FromArgb(163, 45, 33), Color.White);
                                         }
                                         else
                                         {
                                             ApplyModernButtonStyle(_tabDestekButton, Color.FromArgb(58, 62, 70), Color.FromArgb(74, 78, 88), Color.White);
                                         }
                                     }

                                     if (_isShowingTickets)
                                     {
                                         UpdateAddressBookUI();
                                     }
                                 });
                            }
                        }
                    }
                }
                catch { }
            });
        }

        private static HashSet<string> _knownTicketTokens = new();
        private static bool _isFirstTicketFetch = true;

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Beep(uint dwFreq, uint dwDuration);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        public static extern bool MessageBeep(uint uType);

        private void PlayNewTicketNotificationSound()
        {
            Task.Run(() =>
            {
                try
                {
                    MessageBeep(0x00000030); // MB_ICONEXCLAMATION

                    try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
                    try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
                    try { System.Media.SystemSounds.Beep.Play(); } catch { }

                    Beep(1046, 180); // C6 (1046 Hz)
                    Thread.Sleep(40);
                    Beep(1318, 200); // E6 (1318 Hz)
                    Thread.Sleep(40);
                    Beep(1568, 280); // G6 (1568 Hz)
                }
                catch { }
            });
        }

        public void SetClipboardText(string text)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => SetClipboardText(text)));
                return;
            }
            try
            {
                _lastClipboardText = text;
                Clipboard.SetText(text);
            }
            catch { }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        public void RestoreAppWindow()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(RestoreAppWindow));
                return;
            }

            try
            {
                this.Show();
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                ShowWindow(this.Handle, SW_RESTORE);
                SetForegroundWindow(this.Handle);
                this.Activate();
                this.BringToFront();
            }
            catch { }
        }

        private void MainWindow_Resize(object? sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                this.Activate();
                this.BringToFront();
            }
        }

        private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            RestoreAppWindow();
        }

        private void MainWindow_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (_clipboardTimer != null)
            {
                _clipboardTimer.Stop();
                _clipboardTimer.Dispose();
            }

            Program.Shutdown();
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
        private void LoadAddressBook()
        {
            string path = ConfigHelper.GetConfigPath("connections.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    using var doc = JsonDocument.Parse(json);
                    var list = new List<SavedConnection>();
                    foreach (var element in doc.RootElement.EnumerateArray())
                    {
                        string name = "";
                        string id = "";
                        string password = "";
                        string group = "Müşteriler & Cariler";

                        if (element.TryGetProperty("Name", out var nameProp)) name = nameProp.GetString() ?? "";
                        if (element.TryGetProperty("Id", out var idProp)) id = idProp.GetString() ?? "";
                        if (element.TryGetProperty("Password", out var passProp)) password = passProp.GetString() ?? "";
                        if (element.TryGetProperty("Group", out var groupProp)) group = groupProp.GetString() ?? "Müşteriler & Cariler";
                        if (string.IsNullOrEmpty(group)) group = "Müşteriler & Cariler";

                        list.Add(new SavedConnection { Name = name, Id = id, Password = password, Group = group });
                    }
                    _savedConnections = list;
                }
                catch (Exception ex)
                {
                    Program.LogHelper($"Rehber yukleme hatası: {ex.Message}");
                }
            }
            UpdateAddressBookUI();
        }

        private void StepDateFilter(int delta)
        {
            _dateFilterIndex = (_dateFilterIndex + delta + 4) % 4;
            UpdateAddressBookUI();
        }

        private void UpdateDateFilterCounts(int total, int today, int yesterday, int older)
        {
            if (_lblDateFilter == null) return;
            string filterText = "";
            switch (_dateFilterIndex)
            {
                case 0:
                    filterText = $"🌐 Tüm Talepler ({total})";
                    break;
                case 1:
                    filterText = $"☀️ Bugün Gelenler ({today})";
                    break;
                case 2:
                    filterText = $"📅 Dün Gelenler ({yesterday})";
                    break;
                case 3:
                    filterText = $"⌛ Eski Talepler ({older})";
                    break;
            }
            _lblDateFilter.Text = filterText;
        }

        private void UpdateAddressBookUI()
        {
            if (_addressBookListView == null) return;
            _addressBookListView.Items.Clear();

            string search = (_txtSearchAddress != null && !_txtSearchAddress.Text.StartsWith("🔍")) ? _txtSearchAddress.Text.Trim().ToLowerInvariant() : "";

            if (_currentTabMode == 1) // Talepler
            {
                if (_addressBookListView.Columns.Count < 3)
                {
                    _addressBookListView.Columns.Clear();
                    _addressBookListView.Columns.Add("Müşteri / Sorun", 125);
                    _addressBookListView.Columns.Add("Zaman", 70);
                    _addressBookListView.Columns.Add("ID", 65);
                }
                else
                {
                    _addressBookListView.Columns[0].Text = "Müşteri / Sorun";
                    _addressBookListView.Columns[1].Text = "Zaman";
                    _addressBookListView.Columns[2].Text = "ID";
                }

                int totalCount = 0;
                int todayCount = 0;
                int yesterdayCount = 0;
                int olderCount = 0;

                lock (_activeTickets)
                {
                    var now = DateTime.Now;
                    foreach (var ticket in _activeTickets)
                    {
                        totalCount++;
                        int daysAgo = 0;
                        if (ticket.CreatedAt != default(DateTime))
                        {
                            daysAgo = (now.Date - ticket.CreatedAt.Date).Days;
                            if (daysAgo < 0) daysAgo = 0;
                        }

                        if (daysAgo == 0) todayCount++;
                        else if (daysAgo == 1) yesterdayCount++;
                        else olderCount++;

                        // Apply Date Filter Index
                        if (_dateFilterIndex == 1 && daysAgo != 0) continue; // Bugün
                        if (_dateFilterIndex == 2 && daysAgo != 1) continue; // Dün
                        if (_dateFilterIndex == 3 && daysAgo < 2) continue; // Eski

                        if (!string.IsNullOrEmpty(search))
                        {
                            string pStr = GetNormalizedPriority(ticket.Priority, ticket.Issue).ToLowerInvariant();
                            string dtStr = ticket.CreatedAt != default ? ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm").ToLowerInvariant() : "";
                            bool matches = ticket.Name.ToLowerInvariant().Contains(search) ||
                                           ticket.Issue.ToLowerInvariant().Contains(search) ||
                                           ticket.Id.ToLowerInvariant().Contains(search) ||
                                           (ticket.Token != null && ticket.Token.ToLowerInvariant().Contains(search)) ||
                                           pStr.Contains(search) ||
                                           dtStr.Contains(search);
                            if (!matches) continue;
                        }

                        string displayIssue = ticket.Issue;
                        if (displayIssue.Length > 16) displayIssue = displayIssue.Substring(0, 13) + "...";
                        
                        string priorityStr = GetNormalizedPriority(ticket.Priority, ticket.Issue);
                        string priorityTag = "🟡 ";
                        Color priorityColor = Color.FromArgb(191, 140, 15); // Yellow default

                        if (priorityStr.Contains("Düşük") || priorityStr.Contains("🟢"))
                        {
                            priorityTag = "🟢 ";
                            priorityColor = Color.FromArgb(22, 140, 74); // Vibrant Green
                        }
                        else if (priorityStr.Contains("Yüksek") || priorityStr.Contains("🔴"))
                        {
                            priorityTag = "🔴 ";
                            priorityColor = Color.FromArgb(196, 57, 43); // Vibrant Red
                        }

                        string statusTag = (ticket.RequiresConfirmation ? "🛡️ " : "") + priorityTag;
                        
                        // Format Time & Days Tag
                        string timeLabel = "";
                        if (ticket.CreatedAt != default(DateTime))
                        {
                            if (daysAgo == 0)
                            {
                                timeLabel = ticket.CreatedAt.ToString("HH:mm") + " (0g)";
                            }
                            else if (daysAgo == 1)
                            {
                                timeLabel = ticket.CreatedAt.ToString("HH:mm") + " (1g)";
                            }
                            else
                            {
                                timeLabel = ticket.CreatedAt.ToString("dd.MM") + $" ({daysAgo}g)";
                            }
                        }
                        else
                        {
                            timeLabel = "--:-- (0g)";
                        }

                        var item = new ListViewItem($"{statusTag}{ticket.Name} ({displayIssue})");
                        item.SubItems.Add(timeLabel);
                        item.SubItems.Add(ticket.Id);
                        item.Tag = ticket; // Store SupportTicket object
                        item.ToolTipText = $"Talep Önceliği: {priorityStr}\nTalep Zamanı: {(ticket.CreatedAt != default(DateTime) ? ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss") : "Bilinmiyor")}\nGün Sayısı: {daysAgo} gün önce\nSorun: {ticket.Issue}";
                        item.ForeColor = priorityColor;
                        _addressBookListView.Items.Add(item);
                    }
                }

                UpdateDateFilterCounts(totalCount, todayCount, yesterdayCount, olderCount);
                return;
            }
            else if (_currentTabMode == 2) // CRM Geçmişi
            {
                if (_addressBookListView.Columns.Count != 3)
                {
                    _addressBookListView.Columns.Clear();
                    _addressBookListView.Columns.Add("Müşteri / Firma", 130);
                    _addressBookListView.Columns.Add("Durum", 80);
                    _addressBookListView.Columns.Add("İşleyen Bayi", 80);
                }
                _addressBookListView.Columns[0].Text = "Müşteri / Firma";
                _addressBookListView.Columns[1].Text = "Durum";
                _addressBookListView.Columns[2].Text = "İşleyen Bayi";

                lock (_crmHistoryItems)
                {
                    foreach (var h in _crmHistoryItems)
                    {
                        if (!string.IsNullOrEmpty(search))
                        {
                            bool matches = h.Name.ToLowerInvariant().Contains(search) ||
                                           h.Issue.ToLowerInvariant().Contains(search) ||
                                           h.HostId.ToLowerInvariant().Contains(search) ||
                                           h.Status.ToLowerInvariant().Contains(search) ||
                                           h.TenantId.ToLowerInvariant().Contains(search) ||
                                           h.Notes.ToLowerInvariant().Contains(search);
                            if (!matches) continue;
                        }

                        string icon = h.Status == "Çözüldü" ? "✅" : (h.Status.Contains("İptal") ? "🚫" : "⚠️");
                        string displayName = !string.IsNullOrEmpty(h.Name) ? h.Name : (!string.IsNullOrEmpty(h.HostId) ? $"Müşteri ({h.HostId})" : "Müşteri Destek Kaydı");
                        string displayStatus = !string.IsNullOrEmpty(h.Status) ? h.Status : "İşlem Yapıldı";
                        string displayTenant = !string.IsNullOrWhiteSpace(h.TenantId) ? h.TenantId : "BIGLINE";

                        var item = new ListViewItem($"{icon} {displayName}");
                        item.SubItems.Add(displayStatus);
                        item.SubItems.Add(displayTenant);
                        item.Tag = h; // Store SupportHistoryItem object
                        item.ForeColor = h.Status == "Çözüldü" ? Color.FromArgb(22, 140, 74) : (h.Status.Contains("İptal") ? Color.FromArgb(196, 57, 43) : Color.FromArgb(191, 140, 15));
                        _addressBookListView.Items.Add(item);
                    }
                }
                return;
            }

            if (_addressBookListView.Columns.Count != 2)
            {
                _addressBookListView.Columns.Clear();
                _addressBookListView.Columns.Add("İsim", 140);
                _addressBookListView.Columns.Add("ID", 110);
            }
            _addressBookListView.Columns[0].Text = "İsim";
            _addressBookListView.Columns[1].Text = "ID";

            // Filter items by group and search query
            var admins = _savedConnections.Where(c => c.Group == "Yöneticiler (Admin)" && (string.IsNullOrEmpty(search) || c.Name.ToLowerInvariant().Contains(search) || c.Id.ToLowerInvariant().Contains(search))).ToList();
            var team = _savedConnections.Where(c => c.Group == "Ekip Arkadaşlarım" && (string.IsNullOrEmpty(search) || c.Name.ToLowerInvariant().Contains(search) || c.Id.ToLowerInvariant().Contains(search))).ToList();
            var clients = _savedConnections.Where(c => (c.Group == "Müşteriler & Cariler" || string.IsNullOrEmpty(c.Group)) && (string.IsNullOrEmpty(search) || c.Name.ToLowerInvariant().Contains(search) || c.Id.ToLowerInvariant().Contains(search))).ToList();

            // 1. Admins Section
            string adminArrow = _isAdminsExpanded ? "▼" : "►";
            var hAdmin = new ListViewItem($"{adminArrow} YÖNETİCİLER (ADMİN) ({admins.Count})")
            {
                ForeColor = Color.FromArgb(191, 140, 15), // Gold/Yellow
                Font = new Font("Segoe UI", 9.0F, FontStyle.Bold),
                Tag = "HEADER",
                BackColor = Color.FromArgb(245, 245, 246),
                UseItemStyleForSubItems = true
            };
            hAdmin.SubItems.Add("");
            _addressBookListView.Items.Add(hAdmin);

            if (_isAdminsExpanded)
            {
                foreach (var conn in admins)
                {
                    var item = new ListViewItem("  " + conn.Name);
                    item.SubItems.Add(conn.Id);
                    item.ForeColor = Color.FromArgb(38, 40, 45);
                    _addressBookListView.Items.Add(item);
                }
            }

            // 2. Team Section
            string teamArrow = _isTeamExpanded ? "▼" : "►";
            var hTeam = new ListViewItem($"{teamArrow} EKİP ARKADAŞLARIM ({team.Count})")
            {
                ForeColor = Color.FromArgb(74, 90, 120), // Cyan
                Font = new Font("Segoe UI", 9.0F, FontStyle.Bold),
                Tag = "HEADER",
                BackColor = Color.FromArgb(245, 245, 246),
                UseItemStyleForSubItems = true
            };
            hTeam.SubItems.Add("");
            _addressBookListView.Items.Add(hTeam);

            if (_isTeamExpanded)
            {
                foreach (var conn in team)
                {
                    var item = new ListViewItem("  " + conn.Name);
                    item.SubItems.Add(conn.Id);
                    item.ForeColor = Color.FromArgb(38, 40, 45);
                    _addressBookListView.Items.Add(item);
                }
            }

            // 3. Clients Section
            string clientsArrow = _isClientsExpanded ? "▼" : "►";
            var hClients = new ListViewItem($"{clientsArrow} MÜŞTERİLER & CARİLER ({clients.Count})")
            {
                ForeColor = Color.FromArgb(107, 118, 132), // Silver/Gray
                Font = new Font("Segoe UI", 9.0F, FontStyle.Bold),
                Tag = "HEADER",
                BackColor = Color.FromArgb(245, 245, 246),
                UseItemStyleForSubItems = true
            };
            hClients.SubItems.Add("");
            _addressBookListView.Items.Add(hClients);

            if (_isClientsExpanded)
            {
                foreach (var conn in clients)
                {
                    var item = new ListViewItem("  " + conn.Name);
                    item.SubItems.Add(conn.Id);
                    item.ForeColor = Color.FromArgb(38, 40, 45);
                    _addressBookListView.Items.Add(item);
                }
            }
        }

        private void AddressBookListView_DoubleClick(object? sender, EventArgs e)
        {
            if (_addressBookListView != null && _addressBookListView.SelectedItems.Count > 0)
            {
                var item = _addressBookListView.SelectedItems[0];
                if (item.Tag?.ToString() == "HEADER") return;

                if (_currentTabMode == 2 && item.Tag is SupportHistoryItem h)
                {
                    ShowCrmUpdateDialog(h);
                    return;
                }

                string id = item.SubItems.Count > 1 ? item.SubItems[1].Text : "";

                if (_currentTabMode == 1 && item.Tag is SupportTicket ticket)
                {
                    _connectedTicketIds.Add(ticket.Id);
                    if (!string.IsNullOrEmpty(ticket.Token)) _connectedTicketIds.Add(ticket.Token);
                    Program.AutoConnectTicketToken = ticket.Token;
                    Program.ActiveTicketId = ticket.Id;
                    if (_remoteIdTextBox != null) _remoteIdTextBox.Text = ticket.Id;
                    ConnectButton_Click(this, EventArgs.Empty);
                    return;
                }

                if (_remoteIdTextBox != null && !string.IsNullOrEmpty(id))
                {
                    _remoteIdTextBox.Text = id;
                }

                // Trigger connection
                ConnectButton_Click(this, EventArgs.Empty);
            }
        }

        private void AddressBookListView_MouseClick(object? sender, MouseEventArgs e)
        {
            if (_addressBookListView != null && _addressBookListView.SelectedItems.Count > 0)
            {
                var item = _addressBookListView.SelectedItems[0];
                if (item.Tag?.ToString() == "HEADER")
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        string headerText = item.Text;
                        if (headerText.Contains("YÖNETİCİLER"))
                        {
                            _isAdminsExpanded = !_isAdminsExpanded;
                        }
                        else if (headerText.Contains("EKİP ARKADAŞLARIM"))
                        {
                            _isTeamExpanded = !_isTeamExpanded;
                        }
                        else if (headerText.Contains("MÜŞTERİLER & CARİLER"))
                        {
                            _isClientsExpanded = !_isClientsExpanded;
                        }

                        UpdateAddressBookUI();
                    }
                    return;
                }

                if (e.Button == MouseButtons.Left && _currentTabMode == 1 && item.Tag is SupportTicket ticketLeft)
                {
                    if (_remoteIdTextBox != null) _remoteIdTextBox.Text = ticketLeft.Id;
                    Program.AutoConnectTicketToken = ticketLeft.Token;
                    Program.ActiveTicketId = ticketLeft.Id;
                }

                if (e.Button == MouseButtons.Right)
                {
                    var cms = new ContextMenuStrip
                    {
                        BackColor = Color.FromArgb(245, 245, 246),
                        ForeColor = Color.FromArgb(38, 40, 45),
                        ShowImageMargin = false,
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                    };

                    if (_currentTabMode == 1 && item.Tag is SupportTicket ticket)
                    {
                        bool isConnected = _connectedTicketIds.Contains(ticket.Id) || (!string.IsNullOrEmpty(ticket.Token) && _connectedTicketIds.Contains(ticket.Token));

                        var itemConnect = new ToolStripMenuItem("🔌 Uzaktan Bağlan (Çift Tık)");
                        itemConnect.Click += (s, ev) => AddressBookListView_DoubleClick(sender, e);
                        cms.Items.Add(itemConnect);

                        if (isConnected)
                        {
                            cms.Items.Add(new ToolStripSeparator());

                            var itemResolve = new ToolStripMenuItem("✅ Çözüldü – Sorun yok");
                            itemResolve.Click += (s, ev) => ResolveTicketWithStatus(ticket, "Çözüldü");
                            cms.Items.Add(itemResolve);

                            var itemFailed = new ToolStripMenuItem("❌ Çözülmedi");
                            itemFailed.Click += (s, ev) => ResolveTicketWithStatus(ticket, "Çözülmedi");
                            cms.Items.Add(itemFailed);

                            var itemPending = new ToolStripMenuItem("⏳ Talep incelenip dönülecek");
                            itemPending.Click += (s, ev) => ResolveTicketWithStatus(ticket, "Bekliyor / İnceleniyor");
                            cms.Items.Add(itemPending);
                        }
                        else
                        {
                            cms.Items.Add(new ToolStripSeparator());
                            var itemDisabled = new ToolStripMenuItem("🔒 Önce Çift Tıklayıp Karşıya Bağlanın") { Enabled = false };
                            cms.Items.Add(itemDisabled);
                        }
                    }
                    else if (_currentTabMode == 2 && item.Tag is SupportHistoryItem h)
                    {
                        var itemEditCrm = new ToolStripMenuItem("✏️ Durum & Çözüm Notunu Güncelle");
                        itemEditCrm.Click += (s, ev) => ShowCrmUpdateDialog(h);
                        cms.Items.Add(itemEditCrm);

                        var itemDeleteCrm = new ToolStripMenuItem("🗑️ Kaydı Veritabanından Sil");
                        itemDeleteCrm.Click += (s, ev) => DeleteCrmHistoryItem(h);
                        cms.Items.Add(itemDeleteCrm);

                        var itemClearCrm = new ToolStripMenuItem("🧹 Tüm Geçmişi Sıfırla / Temizle");
                        itemClearCrm.Click += (s, ev) => ClearAllCrmHistory();
                        cms.Items.Add(itemClearCrm);
                    }
                    else if (_currentTabMode == 0)
                    {
                        var itemConnect = new ToolStripMenuItem("🔌 Uzaktan Bağlan");
                        itemConnect.Click += (s, ev) => AddressBookListView_DoubleClick(sender, e);
                        cms.Items.Add(itemConnect);

                        var itemEdit = new ToolStripMenuItem("✏️ Düzenle");
                        itemEdit.Click += EditAddressButton_Click;
                        cms.Items.Add(itemEdit);

                        var itemDelete = new ToolStripMenuItem("🗑️ Sil");
                        itemDelete.Click += DeleteAddressButton_Click;
                        cms.Items.Add(itemDelete);
                    }

                    if (cms.Items.Count > 0)
                    {
                        cms.Show(_addressBookListView, e.Location);
                    }
                }
            }
        }

        private int _addressBookSortColumn = -1;
        private bool _addressBookSortAscending = true;

        private void AddressBookListView_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            if (_addressBookListView == null) return;
            if (_addressBookSortColumn == e.Column)
            {
                _addressBookSortAscending = !_addressBookSortAscending;
            }
            else
            {
                _addressBookSortColumn = e.Column;
                _addressBookSortAscending = true;
            }

            _addressBookListView.ListViewItemSorter = new ListViewItemComparer(e.Column, _addressBookSortAscending);
            _addressBookListView.Sort();
        }

        public class ListViewItemComparer : System.Collections.IComparer
        {
            private int _col;
            private bool _ascending;

            public ListViewItemComparer(int column, bool ascending)
            {
                _col = column;
                _ascending = ascending;
            }

            public int Compare(object? x, object? y)
            {
                var itemX = x as ListViewItem;
                var itemY = y as ListViewItem;

                if (itemX == null || itemY == null) return 0;
                if (itemX.Tag?.ToString() == "HEADER") return -1;
                if (itemY.Tag?.ToString() == "HEADER") return 1;

                string valX = _col < itemX.SubItems.Count ? itemX.SubItems[_col].Text : "";
                string valY = _col < itemY.SubItems.Count ? itemY.SubItems[_col].Text : "";

                int result = string.Compare(valX, valY, StringComparison.OrdinalIgnoreCase);
                return _ascending ? result : -result;
            }
        }

        private void ResolveTicketWithStatus(SupportTicket ticket, string status)
        {
            string timeNow = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
            string itemUniqueId = !string.IsNullOrEmpty(ticket.Token) ? ticket.Token : Guid.NewGuid().ToString();
            string createdTimeStr = (ticket.CreatedAt != default(DateTime) && ticket.CreatedAt.Year > 2000) ? ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss") : timeNow;

            var newItem = new SupportHistoryItem
            {
                Id = itemUniqueId,
                HostId = ticket.Id,
                Token = ticket.Token,
                Name = ticket.Name,
                Issue = ticket.Issue,
                TenantId = LicenseSystem.CompanyCode,
                CreatedAt = createdTimeStr,
                ResolvedAt = timeNow,
                Status = status,
                Notes = $"Destek işlemi tamamlandı: {status}"
            };

            // 1. Write to local CRM history on disk & in-memory list immediately (100% guarantee)
            try
            {
                var localList = LoadLocalCrmHistory();
                localList.RemoveAll(x => !string.IsNullOrEmpty(x.Id) && x.Id == newItem.Id);
                localList.Add(newItem);
                SaveLocalCrmHistory(localList);

                lock (_crmHistoryItems)
                {
                    _crmHistoryItems.RemoveAll(x => !string.IsNullOrEmpty(x.Id) && x.Id == newItem.Id);
                    _crmHistoryItems.Add(newItem);
                    _crmHistoryItems = _crmHistoryItems
                        .Where(x => !string.IsNullOrEmpty(x.Name) || !string.IsNullOrEmpty(x.HostId) || !string.IsNullOrEmpty(x.Issue))
                        .OrderByDescending(x => x.ResolvedAt)
                        .ThenByDescending(x => x.CreatedAt)
                        .ToList();
                }

                if (_currentTabMode == 2)
                {
                    UpdateAddressBookUI();
                }
            }
            catch { }

            string serverUrl = _actualRelayUrl;
            string resolveUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", "/api/support/resolve");

            Task.Run(async () =>
            {
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var json = $"{{\"id\":\"{Program.EscapeJson(ticket.Id)}\",\"token\":\"{Program.EscapeJson(ticket.Token)}\",\"name\":\"{Program.EscapeJson(ticket.Name)}\",\"issue\":\"{Program.EscapeJson(ticket.Issue)}\",\"priority\":\"{Program.EscapeJson(ticket.Priority)}\",\"status\":\"{Program.EscapeJson(status)}\",\"notes\":\"Destek işlemi tamamlandı: {Program.EscapeJson(status)}\",\"tenantId\":\"{Program.EscapeJson(LicenseSystem.CompanyCode)}\",\"resolvedAt\":\"{Program.EscapeJson(timeNow)}\"}}";
                        var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                        await client.PostAsync(resolveUrl, content);
                    }
                }
                catch { }

                this.BeginInvoke(new Action(() =>
                {
                    RefreshSupportTickets();
                    RefreshCrmHistory();
                }));
            });
        }

        private void ShowTicketResolveDialog(SupportTicket ticket)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Destek Talebini Yönet & Sonuçlandır";
                dlg.Size = new Size(440, 430);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(245, 245, 246);
                dlg.ForeColor = Color.FromArgb(38, 40, 45);

                Label lblHeader = new Label
                {
                    Text = $"📋 Müşteri: {ticket.Name} ({ticket.Id})\n📝 Sorun: {ticket.Issue}",
                    Location = new Point(20, 15),
                    Size = new Size(385, 55),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120)
                };
                dlg.Controls.Add(lblHeader);

                Label lblStatus = new Label { Text = "📌 Son Durum:", Location = new Point(20, 80), Size = new Size(385, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                dlg.Controls.Add(lblStatus);

                ComboBox cbStatus = new ComboBox
                {
                    Location = new Point(20, 105),
                    Size = new Size(385, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                };
                cbStatus.Items.Add("Çözüldü");
                cbStatus.Items.Add("Takipte");
                cbStatus.Items.Add("Çözülemedi");
                cbStatus.SelectedIndex = 0;
                dlg.Controls.Add(cbStatus);

                Label lblNotes = new Label { Text = "📄 Destek Uzmanı Çözüm Notu:", Location = new Point(20, 140), Size = new Size(385, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                dlg.Controls.Add(lblNotes);

                TextBox txtNotes = new TextBox
                {
                    Location = new Point(20, 165),
                    Size = new Size(385, 100),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                };
                dlg.Controls.Add(txtNotes);

                Button btnConnect = new Button
                {
                    Text = "🔌 Talebe Uzaktan Bağlan",
                    Location = new Point(20, 275),
                    Size = new Size(385, 42),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                ApplyModernButtonStyle(btnConnect, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
                btnConnect.Click += (s, e) => {
                    Program.AutoConnectTicketToken = ticket.Token;
                    Program.ActiveTicketId = ticket.Id;
                    if (_remoteIdTextBox != null) _remoteIdTextBox.Text = ticket.Id;
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    ConnectButton_Click(this, EventArgs.Empty);
                };
                dlg.Controls.Add(btnConnect);

                Button btnSave = new Button
                {
                    Text = "💾 Talebi Çözüldü Olarak Kaydet & CRM'e Aktar",
                    Location = new Point(20, 325),
                    Size = new Size(385, 42),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                ApplyModernButtonStyle(btnSave, Color.FromArgb(22, 140, 74), Color.FromArgb(16, 110, 58), Color.White);
                btnSave.Click += (s, e) => {
                    string newStatus = cbStatus.SelectedItem?.ToString() ?? "Çözüldü";
                    string newNotes = txtNotes.Text.Trim();

                    string serverUrl = _actualRelayUrl;
                    string resolveUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", "/api/support/resolve");

                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                var json = $"{{\"id\":\"{Program.EscapeJson(ticket.Id)}\",\"token\":\"{Program.EscapeJson(ticket.Token)}\",\"status\":\"{Program.EscapeJson(newStatus)}\",\"notes\":\"{Program.EscapeJson(newNotes)}\"}}";
                                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                                await client.PostAsync(resolveUrl, content);
                            }
                        }
                        catch { }

                        this.BeginInvoke(new Action(() =>
                        {
                            RefreshSupportTickets();
                            RefreshCrmHistory();
                        }));
                    });

                    MessageBox.Show("Destek talebi başarıyla kapatıldı ve CRM geçmişine kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                    RefreshSupportTickets();
                    RefreshCrmHistory();
                };
                dlg.Controls.Add(btnSave);

                dlg.ShowDialog(this);
            }
        }

        private void ShowCrmUpdateDialog(SupportHistoryItem h)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "CRM Destek Kaydı Durum Güncelleme";
                dlg.Size = new Size(460, 440);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(245, 245, 246);
                dlg.ForeColor = Color.FromArgb(38, 40, 45);

                Label lblCustomer = new Label
                {
                    Text = $"📋 Müşteri: {(!string.IsNullOrEmpty(h.Name) ? h.Name : "Bilinmiyor")} ({h.HostId})",
                    Location = new Point(20, 15),
                    Size = new Size(405, 22),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120)
                };
                dlg.Controls.Add(lblCustomer);

                Label lblIssue = new Label
                {
                    Text = $"📝 Sorun: {h.Issue}",
                    Location = new Point(20, 39),
                    Size = new Size(405, 22),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(191, 140, 15)
                };
                dlg.Controls.Add(lblIssue);

                string dateText = !string.IsNullOrEmpty(h.ResolvedAt) ? h.ResolvedAt : (!string.IsNullOrEmpty(h.CreatedAt) ? h.CreatedAt : "Bilinmiyor");
                Label lblDate = new Label
                {
                    Text = $"⏱️ Tarih: {dateText}",
                    Location = new Point(20, 63),
                    Size = new Size(405, 22),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(22, 140, 74)
                };
                dlg.Controls.Add(lblDate);

                Label lblStatus = new Label { Text = "📌 Son Durum:", Location = new Point(20, 95), Size = new Size(405, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                dlg.Controls.Add(lblStatus);

                ComboBox cbStatus = new ComboBox
                {
                    Location = new Point(20, 118),
                    Size = new Size(405, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                };
                cbStatus.Items.Add("Çözüldü");
                cbStatus.Items.Add("Takipte");
                cbStatus.Items.Add("Çözülemedi");
                cbStatus.SelectedItem = h.Status.Contains("Çözüldü") ? "Çözüldü" : (h.Status.Contains("Takipte") ? "Takipte" : (h.Status.Contains("İptal") ? "Çözülemedi" : "Çözüldü"));
                dlg.Controls.Add(cbStatus);

                Label lblNotes = new Label { Text = "📄 Destek Uzmanı Çözüm Notu:", Location = new Point(20, 155), Size = new Size(405, 20), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                dlg.Controls.Add(lblNotes);

                TextBox txtNotes = new TextBox
                {
                    Location = new Point(20, 178),
                    Size = new Size(405, 110),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    Text = h.Notes,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
                };
                dlg.Controls.Add(txtNotes);

                Button btnSave = new Button
                {
                    Text = "💾 Kaydet ve Güncelle",
                    Location = new Point(20, 305),
                    Size = new Size(405, 42),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                ApplyModernButtonStyle(btnSave, Color.FromArgb(22, 140, 74), Color.FromArgb(16, 110, 58), Color.White);
                btnSave.Click += (s, e) => {
                    string newStatus = cbStatus.SelectedItem?.ToString() ?? "Çözüldü";
                    string newNotes = txtNotes.Text.Trim();

                    h.Status = newStatus;
                    h.Notes = newNotes;

                    // 1. Save to local memory & disk permanently
                    lock (_crmHistoryItems)
                    {
                        var existing = _crmHistoryItems.FirstOrDefault(x => (!string.IsNullOrEmpty(x.Id) && x.Id == h.Id) || (x.HostId == h.HostId && x.CreatedAt == h.CreatedAt));
                        if (existing != null)
                        {
                            existing.Status = newStatus;
                            existing.Notes = newNotes;
                        }
                        SaveLocalCrmHistory(_crmHistoryItems);
                    }

                    UpdateAddressBookUI();

                    // 2. Transmit update to relay server
                    string serverUrl = _actualRelayUrl;
                    string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", "/api/support/history/update");
                    Task.Run(async () =>
                    {
                        try
                        {
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                var json = $"{{\"id\":\"{Program.EscapeJson(h.Id)}\",\"hostId\":\"{Program.EscapeJson(h.HostId)}\",\"status\":\"{Program.EscapeJson(newStatus)}\",\"notes\":\"{Program.EscapeJson(newNotes)}\"}}";
                                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                                await client.PostAsync(httpUrl, content);
                            }
                        }
                        catch { }
                    });

                    MessageBox.Show("Destek kaydı çözümler notuyla başarıyla güncellendi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                    dlg.Close();
                };
                dlg.Controls.Add(btnSave);

                dlg.ShowDialog(this);
            }
        }

        private async void DeleteCrmHistoryItem(SupportHistoryItem h)
        {
            var res = MessageBox.Show(
                $"'{h.Name}' müşterisine ait destek kaydını veritabanından ve listeden kalıcı olarak silmek istediğinize emin misiniz?",
                "Kayıt Silme Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (res == DialogResult.Yes)
            {
                // 1. Remove from local disk file immediately
                try
                {
                    var localList = LoadLocalCrmHistory();
                    localList.RemoveAll(x => (!string.IsNullOrEmpty(h.Id) && x.Id == h.Id) ||
                                             (!string.IsNullOrEmpty(h.HostId) && x.HostId == h.HostId && x.CreatedAt == h.CreatedAt) ||
                                             (x.Name == h.Name && x.Issue == h.Issue && x.CreatedAt == h.CreatedAt));
                    SaveLocalCrmHistory(localList);
                }
                catch { }

                // 2. Remove from in-memory list
                lock (_crmHistoryItems)
                {
                    _crmHistoryItems.RemoveAll(x => (!string.IsNullOrEmpty(h.Id) && x.Id == h.Id) ||
                                                    (!string.IsNullOrEmpty(h.HostId) && x.HostId == h.HostId && x.CreatedAt == h.CreatedAt) ||
                                                    (x.Name == h.Name && x.Issue == h.Issue && x.CreatedAt == h.CreatedAt));
                }

                // 3. Update UI instantly
                if (_currentTabMode == 2)
                {
                    UpdateAddressBookUI();
                }

                // 4. Notify relay server asynchronously
                string deleteUrl = GetRelayHttpUrl($"/api/support/history/delete?id={Uri.EscapeDataString(h.Id)}");
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var content = new System.Net.Http.StringContent($"{{\"id\":\"{Program.EscapeJson(h.Id)}\"}}", Encoding.UTF8, "application/json");
                        await client.PostAsync(deleteUrl, content);
                    }
                }
                catch { }

                MessageBox.Show("Destek kaydı başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void ClearAllCrmHistory()
        {
            var res = MessageBox.Show(
                "TÜM destek geçmişi kayıtlarını veritabanından ve sunucudan kalıcı olarak silmek istediğinize emin misiniz?\n\nBu işlem geri alınamaz!",
                "Tüm Geçmişi Temizleme Onayı",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (res == DialogResult.Yes)
            {
                // 1. Wipe local disk storage immediately
                try
                {
                    SaveLocalCrmHistory(new List<SupportHistoryItem>());
                }
                catch { }

                // 2. Clear in-memory list
                lock (_crmHistoryItems)
                {
                    _crmHistoryItems.Clear();
                }

                // 3. Update UI instantly
                if (_currentTabMode == 2)
                {
                    UpdateAddressBookUI();
                }

                // 4. Notify relay server asynchronously
                string clearUrl = GetRelayHttpUrl("/api/support/history/clear");
                try
                {
                    using (var client = new System.Net.Http.HttpClient())
                    {
                        var content = new System.Net.Http.StringContent("{}", Encoding.UTF8, "application/json");
                        await client.PostAsync(clearUrl, content);
                    }
                }
                catch { }

                MessageBox.Show("Tüm destek kayıtları başarıyla silindi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SaveAddressBook()
        {
            try
            {
                string path = ConfigHelper.GetConfigPath("connections.json");
                var sbJson = new StringBuilder();
                sbJson.Append("[");
                for (int i = 0; i < _savedConnections.Count; i++)
                {
                    var c = _savedConnections[i];
                    sbJson.Append($"{{\"Name\":\"{EscapeJsonString(c.Name)}\",\"Id\":\"{EscapeJsonString(c.Id)}\",\"Password\":\"{EscapeJsonString(c.Password)}\",\"Group\":\"{EscapeJsonString(c.Group)}\"}}");
                    if (i < _savedConnections.Count - 1) sbJson.Append(",");
                }
                sbJson.Append("]");
                string json = sbJson.ToString();
                File.WriteAllText(path, json);

                // Write human-readable plain text backup file
                try
                {
                    string txtPath = ConfigHelper.GetConfigPath("rehber_yedek.txt");
                    var sb = new StringBuilder();
                    sb.AppendLine("==================================================");
                    sb.AppendLine("   BigLineconnect - BAĞLANTI REHBERİ YEDEĞİ (TXT)");
                    sb.AppendLine($"   Son Güncelleme: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                    sb.AppendLine("==================================================");
                    sb.AppendLine();

                    int index = 1;
                    foreach (var conn in _savedConnections)
                    {
                        sb.AppendLine($"{index}. Bağlantı:");
                        sb.AppendLine($"   İsim / Firma : {conn.Name}");
                        sb.AppendLine($"   Grup / Sınıf : {conn.Group}");
                        sb.AppendLine($"   Bağlantı ID  : {conn.Id}");
                        sb.AppendLine($"   Şifre        : {(string.IsNullOrEmpty(conn.Password) ? "[Yok - Karşı Onay Gerekli]" : conn.Password)}");
                        sb.AppendLine();
                        index++;
                    }
                    sb.AppendLine("==================================================");
                    File.WriteAllText(txtPath, sb.ToString());
                }
                catch { }
            }
            catch { }
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private void AddAddressButton_Click(object? sender, EventArgs e)
        {
            if (_remoteIdTextBox == null) return;
            string currentId = _remoteIdTextBox.Text.Trim().Replace(" ", "");
            if (string.IsNullOrEmpty(currentId) || currentId.Length != 9)
            {
                MessageBox.Show("Lütfen önce sol taraftaki kutuya 9 haneli geçerli bir ID yazın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var dialogResult = AddConnectionDialog.ShowDialog(currentId);
            if (!dialogResult.Success || string.IsNullOrEmpty(dialogResult.Name)) return;

            string targetId = !string.IsNullOrEmpty(dialogResult.Id) ? dialogResult.Id : currentId;

            // Check if already exists
            var existing = _savedConnections.FirstOrDefault(c => c.Id == targetId);
            if (existing != null)
            {
                existing.Name = dialogResult.Name;
                existing.Password = dialogResult.Password;
                existing.Group = dialogResult.Group;
            }
            else
            {
                _savedConnections.Add(new SavedConnection { Name = dialogResult.Name, Id = targetId, Password = dialogResult.Password, Group = dialogResult.Group });
            }

            SaveAddressBook();
            UpdateAddressBookUI();
        }

        private void EditAddressButton_Click(object? sender, EventArgs e)
        {
            if (_addressBookListView == null || _addressBookListView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Lütfen düzenlemek istediğiniz kaydı listeden seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var item = _addressBookListView.SelectedItems[0];
            if (item.Tag?.ToString() == "HEADER") return;
            string id = item.SubItems[1].Text;
            var target = _savedConnections.FirstOrDefault(c => c.Id == id);
            if (target != null)
            {
                var dialogResult = AddConnectionDialog.ShowDialog(id, target.Name, target.Password, target.Group);
                if (dialogResult.Success && !string.IsNullOrEmpty(dialogResult.Name))
                {
                    string newId = !string.IsNullOrEmpty(dialogResult.Id) ? dialogResult.Id : id;
                    target.Id = newId;
                    target.Name = dialogResult.Name;
                    target.Password = dialogResult.Password;
                    target.Group = dialogResult.Group;
                    SaveAddressBook();
                    UpdateAddressBookUI();
                }
            }
        }

        private void DeleteAddressButton_Click(object? sender, EventArgs e)
        {
            if (_addressBookListView != null && _addressBookListView.SelectedItems.Count > 0)
            {
                var item = _addressBookListView.SelectedItems[0];
                if (item.Tag?.ToString() == "HEADER") return;
                string id = item.SubItems[1].Text;
                var target = _savedConnections.FirstOrDefault(c => c.Id == id);
                if (target != null)
                {
                    _savedConnections.Remove(target);
                    SaveAddressBook();
                    UpdateAddressBookUI();
                }
            }
        }

        public static class AddConnectionDialog
        {
            public static (bool Success, string Id, string Name, string Password, string Group) ShowDialog(string id, string initialName = "", string initialPassword = "", string initialGroup = "Müşteriler & Cariler")
            {
                Form prompt = new Form()
                {
                    Width = 360,
                    Height = 350,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    Text = string.IsNullOrEmpty(initialName) ? $"Rehbere Kayıt Ekle" : $"Kaydı Düzenle - ID: {id}",
                    StartPosition = FormStartPosition.CenterScreen,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45)
                };

                Label idLabel = new Label() { Left = 20, Top = 15, Text = "Uzak Bilgisayar ID (9 Hane):", Width = 300, ForeColor = Color.FromArgb(38, 40, 45) };
                TextBox idTextBox = new TextBox() { Left = 20, Top = 35, Width = 300, Text = id, BackColor = Color.FromArgb(245, 245, 246), ForeColor = Color.FromArgb(74, 90, 120), Font = new Font("Segoe UI", 10F, FontStyle.Bold), BorderStyle = BorderStyle.FixedSingle };

                Label nameLabel = new Label() { Left = 20, Top = 70, Text = "Bağlantı Adı / Firma / Müşteri:", Width = 300, ForeColor = Color.FromArgb(38, 40, 45) };
                TextBox nameTextBox = new TextBox() { Left = 20, Top = 90, Width = 300, Text = initialName, BackColor = Color.FromArgb(245, 245, 246), ForeColor = Color.FromArgb(38, 40, 45), BorderStyle = BorderStyle.FixedSingle };

                Label passLabel = new Label() { Left = 20, Top = 125, Text = "Erişim Şifresi (Onaysız Giriş İçin - İsteğe Bağlı):", Width = 300, ForeColor = Color.FromArgb(38, 40, 45) };
                TextBox passTextBox = new TextBox() { Left = 20, Top = 145, Width = 300, Text = initialPassword, PasswordChar = '*', BackColor = Color.FromArgb(245, 245, 246), ForeColor = Color.FromArgb(38, 40, 45), BorderStyle = BorderStyle.FixedSingle };

                Label groupLabel = new Label() { Left = 20, Top = 180, Text = "Grup / Kategori:", Width = 300, ForeColor = Color.FromArgb(38, 40, 45) };
                ComboBox groupComboBox = new ComboBox()
                {
                    Left = 20,
                    Top = 200,
                    Width = 300,
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    FlatStyle = FlatStyle.Flat,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                groupComboBox.Items.AddRange(new object[] { "Yöneticiler (Admin)", "Ekip Arkadaşlarım", "Müşteriler & Cariler" });
                groupComboBox.SelectedItem = string.IsNullOrEmpty(initialGroup) ? "Müşteriler & Cariler" : initialGroup;

                Button okBtn = new Button() { Text = "Kaydet", Left = 100, Width = 100, Top = 250, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(22, 140, 74), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
                Button cancelBtn = new Button() { Text = "İptal", Left = 210, Width = 100, Top = 250, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(196, 57, 43), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                okBtn.FlatAppearance.BorderSize = 0;
                cancelBtn.FlatAppearance.BorderSize = 0;

                okBtn.Click += (sender, e) => { prompt.Close(); };
                cancelBtn.Click += (sender, e) => { prompt.Close(); };

                prompt.Controls.Add(idLabel);
                prompt.Controls.Add(idTextBox);
                prompt.Controls.Add(nameLabel);
                prompt.Controls.Add(nameTextBox);
                prompt.Controls.Add(passLabel);
                prompt.Controls.Add(passTextBox);
                prompt.Controls.Add(groupLabel);
                prompt.Controls.Add(groupComboBox);
                prompt.Controls.Add(okBtn);
                prompt.Controls.Add(cancelBtn);
                prompt.AcceptButton = okBtn;
                prompt.CancelButton = cancelBtn;

                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    string newId = idTextBox.Text.Trim().Replace(" ", "");
                    return (true, newId, nameTextBox.Text.Trim(), passTextBox.Text.Trim(), groupComboBox.SelectedItem?.ToString() ?? "Müşteriler & Cariler");
                }
                return (false, "", "", "", "");
            }
        }

        private void InstallServiceButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!IsUserAnAdmin())
                {
                    MessageBox.Show("Servis yüklemek için lütfen uygulamayı sağ tıklayıp 'Yönetici Olarak Çalıştır' seçeneği ile başlatın.", "Yönetici Yetkisi Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "BigLineconnect Uzak Kontrol Servisini sisteminize kurmak istediğinize emin misiniz?\n\nBu işlem, bilgisayara arka plandan 7/24 kalıcı uzaktan erişim sağlanmasına izin verecektir.",
                    "Servis Yükleme Onayı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );
                if (confirm != DialogResult.Yes) return;

                // Check if assembly name changed to construct the correct binPath
                string assemblyPath = Application.ExecutablePath;
                string binPath = $"\\\"{assemblyPath}\\\" --service";

                // First stop and delete the old BigLineconnectSvc and BigLineconnect service names
                try { var p1 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sc.exe", "stop BigLineconnectSvc") { CreateNoWindow = true, UseShellExecute = false }); p1?.WaitForExit(); } catch { }
                try { var p2 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sc.exe", "delete BigLineconnectSvc") { CreateNoWindow = true, UseShellExecute = false }); p2?.WaitForExit(); } catch { }
                try { var p1 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sc.exe", "stop BigLineconnect") { CreateNoWindow = true, UseShellExecute = false }); p1?.WaitForExit(); } catch { }
                try { var p2 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sc.exe", "delete BigLineconnect") { CreateNoWindow = true, UseShellExecute = false }); p2?.WaitForExit(); } catch { }

                // sc.exe create BigLineconnectSvc binPath= "..." start= auto
                var psi = new System.Diagnostics.ProcessStartInfo("sc.exe", $"create BigLineconnectSvc binPath= \"{binPath}\" start= auto")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();

                // sc.exe description BigLineconnectSvc "..."
                psi = new System.Diagnostics.ProcessStartInfo("sc.exe", "description BigLineconnectSvc \"BigLineconnect Uzaktan Kontrol Servisi\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();

                // sc.exe start BigLineconnectSvc
                psi = new System.Diagnostics.ProcessStartInfo("sc.exe", "start BigLineconnectSvc")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();

                AppendLog("Windows Servisi başarıyla yüklendi ve başlatıldı.");
                MessageBox.Show("Uzak kontrol servisi başarıyla sisteme yüklendi ve arka planda çalıştırıldı.", "Servis Yüklendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"[Servis Yükleme Hatası]: {ex.Message}");
                MessageBox.Show($"Servis yüklenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UninstallServiceButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!IsUserAnAdmin())
                {
                    MessageBox.Show("Servis kaldırmak için lütfen uygulamayı sağ tıklayıp 'Yönetici Olarak Çalıştır' seçeneği ile başlatın.", "Yönetici Yetkisi Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    "UYARI: BigLineconnect Uzak Kontrol Servisini sistemden kaldırmak istediğinize emin misiniz?\n\nKaldırıldığında, bu bilgisayara arka plandan uzaktan erişim kalıcı olarak kesilecektir.",
                    "Servis Kaldırma Onayı",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2
                );
                if (confirm != DialogResult.Yes) return;

                // sc.exe stop BigLineconnectSvc
                var psi = new System.Diagnostics.ProcessStartInfo("sc.exe", "stop BigLineconnectSvc")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();

                // sc.exe delete BigLineconnectSvc
                psi = new System.Diagnostics.ProcessStartInfo("sc.exe", "delete BigLineconnectSvc")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                p = System.Diagnostics.Process.Start(psi);
                p?.WaitForExit();

                // Also delete old BigLineconnect service just in case
                try { var p1 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sc.exe", "stop BigLineconnect") { CreateNoWindow = true, UseShellExecute = false }); p1?.WaitForExit(); } catch { }
                try { var p2 = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("sc.exe", "delete BigLineconnect") { CreateNoWindow = true, UseShellExecute = false }); p2?.WaitForExit(); } catch { }

                AppendLog("Windows Servisi başarıyla durduruldu ve sistemden kaldırıldı.");
                MessageBox.Show("Uzak kontrol servisi sistemden kaldırıldı.", "Servis Kaldırıldı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                AppendLog($"[Servis Kaldırma Hatası]: {ex.Message}");
                MessageBox.Show($"Servis kaldırılırken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        [System.Runtime.InteropServices.DllImport("shell32.dll", EntryPoint = "IsUserAnAdmin")]
        private static extern bool IsUserAnAdmin();

        private void SetStartup(bool runOnStartup)
        {
            try
            {
                using (var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (rk != null)
                    {
                        if (runOnStartup)
                        {
                            rk.SetValue("BigLineconnect", "\"" + Application.ExecutablePath + "\"");
                            // Also trigger Windows Service registration
                            Task.Run(() =>
                            {
                                try
                                {
                                    var psi = new System.Diagnostics.ProcessStartInfo(Application.ExecutablePath, "--install-service")
                                    {
                                        UseShellExecute = true,
                                        Verb = "runas"
                                    };
                                    System.Diagnostics.Process.Start(psi);
                                }
                                catch { }
                            });
                        }
                        else
                        {
                            rk.DeleteValue("BigLineconnect", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[Başlangıç Ayarı Hatası]: {ex.Message}");
            }
        }

        private bool IsStartupEnabled()
        {
            try
            {
                using (var rk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (rk != null)
                    {
                        return rk.GetValue("BigLineconnect") != null;
                    }
                }
            }
            catch { }
            return false;
        }
        private void ApplyModernButtonStyle(Button btn, Color normalBg, Color hoverBg, Color textCol)
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

        private Panel CreateModernTextBoxWrapper(TextBox txt)
        {
            var pnl = new Panel
            {
                Location = txt.Location,
                Size = new Size(txt.Width, txt.Height + 6),
                BackColor = Color.FromArgb(245, 245, 246)
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
                    Color.FromArgb(74, 90, 120),
                    Color.FromArgb(58, 72, 98),
                    45F))
                using (var pen = new Pen(brush, 1.5F))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1);
                }
            };
            return pnl;
        }

        private Panel CreateModernLogBoxWrapper(TextBox txt)
        {
            var pnl = new Panel
            {
                Location = txt.Location,
                Size = new Size(txt.Width, txt.Height),
                BackColor = Color.FromArgb(245, 245, 246)
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
                    Color.FromArgb(74, 90, 120),
                    Color.FromArgb(58, 72, 98),
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
                Color.FromArgb(245, 245, 246),
                Color.FromArgb(245, 245, 246),
                45F))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            DrawCard(e.Graphics, new Rectangle(20, 100, 520, 70), "Bulut Sunucu Ayarları");
            DrawCard(e.Graphics, new Rectangle(20, 185, 245, 120), "Bu Bilgisayar");
            DrawCard(e.Graphics, new Rectangle(295, 185, 245, 120), "Karşı Bilgisayar");
            DrawCard(e.Graphics, new Rectangle(20, 315, 520, 135), "Kişisel Erişim & Güvenlik Ayarları");
            DrawCard(e.Graphics, new Rectangle(20, 462, 520, 95), "Gelişmiş Ayarlar ve Oturum Seçenekleri");
            DrawCard(e.Graphics, new Rectangle(560, 100, 290, 520), "Kayıtlı Bilgisayarlar (Rehber)");

            // Draw a border around the Address Book ListView
            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(560 + 15, 100 + 20, 260, 395),
                Color.FromArgb(74, 90, 120),
                Color.FromArgb(58, 72, 98),
                45F))
            using (var pen = new Pen(brush, 1))
            {
                e.Graphics.DrawRectangle(pen, 560 + 15 - 1, 100 + 20 - 1, 260 + 1, 395 + 1);
            }
        }

        private void DrawCard(Graphics g, Rectangle rect, string title)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            using (var fillBrush = new SolidBrush(Color.FromArgb(30, 245, 245, 246)))
            {
                FillRoundedRectangle(g, fillBrush, rect, 10);
            }

            using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect,
                Color.FromArgb(120, 74, 90, 120),
                Color.FromArgb(120, 74, 90, 120),
                45F))
            using (var pen = new Pen(brush, 1.2F))
            {
                DrawRoundedRectangle(g, pen, rect, 10);
            }

            using (var titleFont = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.FromArgb(74, 90, 120)))
            using (var bgBrush = new SolidBrush(Color.FromArgb(245, 245, 246)))
            {
                SizeF textSize = g.MeasureString(title, titleFont);
                g.FillRectangle(bgBrush, rect.X + 12, rect.Y - 10, textSize.Width + 6, textSize.Height);
                g.DrawString(title, titleFont, textBrush, rect.X + 15, rect.Y - 10);
            }
        }

        private void FillRoundedRectangle(Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        private void DrawRoundedRectangle(Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
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

        public DialogResult ShowModalWithDimmedOverlay(Form modal)
        {
            try
            {
                using (var overlay = new Form())
                {
                    overlay.FormBorderStyle = FormBorderStyle.None;
                    overlay.BackColor = Color.Black;
                    overlay.Opacity = 0.70; // 70% Dark Dimmed Overlay
                    overlay.ShowInTaskbar = false;
                    overlay.StartPosition = FormStartPosition.Manual;
                    overlay.Location = this.Location;
                    overlay.Size = this.Size;
                    overlay.Owner = this;
                    overlay.Show();

                    modal.Owner = overlay;
                    modal.StartPosition = FormStartPosition.CenterParent;
                    var result = modal.ShowDialog(overlay);
                    overlay.Close();
                    return result;
                }
            }
            catch
            {
                modal.StartPosition = FormStartPosition.CenterParent;
                return modal.ShowDialog(this);
            }
        }

        private string MaskRelayUrl(string url)
        {
            return "🔒 BigLine Güvenli Sunucu (relay.biglineconnect.com)";
        }

        private bool PromptForAdminPassword()
        {
            using (var promptForm = new Form())
            {
                promptForm.Width = 370;
                promptForm.Height = 160;
                promptForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                promptForm.Text = "Yönetici / Uzman Girişi";
                promptForm.StartPosition = FormStartPosition.CenterParent;
                promptForm.MaximizeBox = false;
                promptForm.MinimizeBox = false;
                promptForm.BackColor = Color.FromArgb(245, 245, 246);
                promptForm.ForeColor = Color.FromArgb(38, 40, 45);

                var textLabel = new Label() { Left = 20, Top = 15, Width = 320, Text = "Lütfen yönetici şifresini giriniz:", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = Color.FromArgb(74, 90, 120) };
                var textBox = new TextBox() { Left = 20, Top = 42, Width = 310, PasswordChar = '*', UseSystemPasswordChar = true, Font = new Font("Segoe UI", 10F) };
                var confirmation = new Button() { Text = "Doğrula & Giriş", Left = 130, Width = 110, Height = 30, Top = 80, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(74, 90, 120), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
                var cancelBtn = new Button() { Text = "İptal", Left = 250, Width = 80, Height = 30, Top = 80, DialogResult = DialogResult.Cancel, BackColor = Color.FromArgb(58, 62, 70), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };

                textBox.KeyPress += (s, e) =>
                {
                    if (e.KeyChar == (char)13) // Enter
                    {
                        promptForm.DialogResult = DialogResult.OK;
                        promptForm.Close();
                    }
                };

                promptForm.Controls.Add(textLabel);
                promptForm.Controls.Add(textBox);
                promptForm.Controls.Add(confirmation);
                promptForm.Controls.Add(cancelBtn);
                promptForm.AcceptButton = confirmation;
                promptForm.CancelButton = cancelBtn;

                if (ShowModalWithDimmedOverlay(promptForm) == DialogResult.OK)
                {
                    string input = textBox.Text.Trim();
                    if (string.IsNullOrEmpty(input))
                    {
                        MessageBox.Show("⚠️ Şifre girmeden kayıt / yetkilendirme yapamazsınız.\nLütfen yönetici şifrenizi giriniz.", "Şifre Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    if (input.Equals("Bm1453", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("1453") ||
                        input.Equals("bigline", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Hatali sifre girildi!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            return false;
        }

        private void ToggleSpecialistMode()
        {
            if (!LicenseSystem.IsSpecialistMode)
            {
                if (PromptForAdminPassword())
                {
                    using (var guide = new SpecialistSetupGuideForm())
                    {
                        if (ShowModalWithDimmedOverlay(guide) == DialogResult.OK)
                        {
                            try
                            {
                                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uzman.txt"), "uzman");
                                File.WriteAllText(ConfigHelper.GetConfigPath("uzman.txt"), "uzman");
                            }
                            catch { }

                            Application.Restart();
                        }
                    }
                }
            }
            else
            {
                var res = MessageBox.Show("Müşteri Moduna geçiş yapmak istiyor musunuz?\n(Talepler ve CRM geçmişi sekmeleri gizlenecektir.)", "Müşteri Moduna Geç", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    try
                    {
                        if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uzman.txt")))
                            File.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "uzman.txt"));
                        if (File.Exists(ConfigHelper.GetConfigPath("uzman.txt")))
                            File.Delete(ConfigHelper.GetConfigPath("uzman.txt"));
                    }
                    catch { }

                    MessageBox.Show("👤 Müşteri Modu Aktif Edildi!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Restart();
                }
            }
        }

        private bool TryUnlockRelayUrl()
        {
            if (PromptForAdminPassword())
            {
                if (_relayUrlTextBox != null)
                {
                    _relayUrlTextBox.ReadOnly = false;
                    _relayUrlTextBox.Text = _actualRelayUrl;
                    _relayUrlTextBox.Focus();
                    _relayUrlTextBox.SelectAll();
                }
                return true;
            }
            return false;
        }

        private void CheckLicensingOnLoad()
        {
            if (LicenseSystem.IsLicenseActive)
            {
                if (_btnLic != null)
                {
                    _btnLic.Text = "Lisans: Aktif";
                    _btnLic.Enabled = false;
                    _btnLic.LinkColor = Color.FromArgb(74, 90, 120); // Cyan
                }
                HideLicensingOverlay();
            }
            else
            {
                if (LicenseSystem.IsTrialExpired)
                {
                    ShowLicensingOverlay();
                }
                else
                {
                    if (_btnLic != null)
                    {
                        _btnLic.Text = "Lisans Gir";
                        _btnLic.Enabled = true;
                        _btnLic.LinkColor = Color.FromArgb(58, 72, 98); // Pink
                    }
                    HideLicensingOverlay();
                }
            }
        }

        private void ShowLicensingOverlay()
        {
            if (_licensingOverlay == null)
            {
                _licensingOverlay = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Name = "LicensingOverlay"
                };

                // Blur / transparent dark background painting
                _licensingOverlay.Paint += (s, e) =>
                {
                    using (var overlayBrush = new SolidBrush(Color.FromArgb(200, 245, 245, 246)))
                    {
                        e.Graphics.FillRectangle(overlayBrush, _licensingOverlay.ClientRectangle);
                    }
                };

                var container = new Panel
                {
                    Size = new Size(520, 400),
                    BackColor = Color.FromArgb(245, 245, 246)
                };
                _licensingOverlay.Controls.Add(container);

                // Dynamically center the container on resize
                _licensingOverlay.Resize += (s, e) =>
                {
                    container.Location = new Point(
                        (_licensingOverlay.Width - container.Width) / 2,
                        (_licensingOverlay.Height - container.Height) / 2
                    );
                };

                container.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        container.ClientRectangle,
                        Color.FromArgb(74, 90, 120),
                        Color.FromArgb(58, 72, 98),
                        45F))
                    using (var pen = new Pen(brush, 2f))
                    {
                        e.Graphics.DrawRectangle(pen, 0, 0, container.Width - 1, container.Height - 1);
                    }
                };

                var titleLabel = new Label
                {
                    Text = "🔑 DİJİTAL LİSANS VE BAYİ AYARLARI",
                    Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120),
                    Location = new Point(20, 15),
                    Size = new Size(480, 32),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                container.Controls.Add(titleLabel);

                // Section 1: Bayi / Firma Kodu Ayarı
                var lblCompanyTitle = new Label
                {
                    Text = "🏢 Bayi / Firma Kodunuz (Parametrik Destek Kanalı):",
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(191, 140, 15),
                    Location = new Point(30, 52),
                    Size = new Size(460, 20)
                };
                container.Controls.Add(lblCompanyTitle);

                var txtCompanyCode = new TextBox
                {
                    Text = (string.IsNullOrWhiteSpace(LicenseSystem.CompanyCode) || LicenseSystem.CompanyCode == "BIGLINE" || LicenseSystem.CompanyCode.StartsWith("V1.") || LicenseSystem.CompanyCode.StartsWith("V2.") || LicenseSystem.CompanyCode.StartsWith("V3.") || LicenseSystem.CompanyCode.StartsWith("V4.")) ? "BAYIKODU" : LicenseSystem.CompanyCode,
                    Size = new Size(310, 28),
                    Location = new Point(30, 75),
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    PlaceholderText = "Örn: BY-EMF-2026 veya EMF_BILGISAYAR"
                };
                container.Controls.Add(txtCompanyCode);

                var btnSaveCompany = new Button
                {
                    Text = "💾 Kaydet",
                    Size = new Size(120, 28),
                    Location = new Point(350, 75),
                    BackColor = Color.FromArgb(22, 140, 74),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSaveCompany.FlatAppearance.BorderSize = 0;
                container.Controls.Add(btnSaveCompany);

                btnSaveCompany.Click += (s, e) =>
                {
                    string input = txtCompanyCode.Text.Trim();
                    if (string.IsNullOrEmpty(input))
                    {
                        MessageBox.Show("Lütfen geçerli bir Bayi / Firma Kodu giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    LicenseSystem.SaveCompanyCode(input, true);
                    MessageBox.Show($"✅ Bayi Kodu başarıyla '{LicenseSystem.CompanyCode}' olarak kaydedildi!\n\nMüşterilerinizin gönderdiği talepler doğrudan sizin ekranınıza düşecektir.", "Bayi Kodu Güncellendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateAddressBookUI();
                };

                // Section 2: Machine ID
                string machineId = LicenseSystem.GetMachineUniqueId();
                var lblMachineTitle = new Label
                {
                    Text = "💻 Bu Bilgisayarın Kodu (Machine ID):",
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(22, 140, 74),
                    Location = new Point(30, 115),
                    Size = new Size(460, 20)
                };
                container.Controls.Add(lblMachineTitle);

                var txtMachineId = new TextBox
                {
                    Text = machineId,
                    ReadOnly = true,
                    Size = new Size(310, 28),
                    Location = new Point(30, 138),
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.LightGray,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Consolas", 9.5F, FontStyle.Bold)
                };
                container.Controls.Add(txtMachineId);

                var btnCopyMachineId = new Button
                {
                    Text = "📋 Kopyala",
                    Size = new Size(120, 28),
                    Location = new Point(350, 138),
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCopyMachineId.FlatAppearance.BorderSize = 1;
                btnCopyMachineId.FlatAppearance.BorderColor = Color.Gray;
                btnCopyMachineId.Click += (s, e) =>
                {
                    Clipboard.SetText(machineId);
                    MessageBox.Show("Makine Kodu (Machine ID) panoya kopyalandı!\nLisans sağlayıcınıza gönderebilirsiniz.", "Kopyalandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
                container.Controls.Add(btnCopyMachineId);

                // Section 3: Pro Lisans Key
                var descLabel = new Label
                {
                    Text = "🔑 Pro Lisans Anahtarınız (Opsiyonel / Tam Sürüm):",
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120),
                    Location = new Point(30, 178),
                    Size = new Size(460, 20)
                };
                container.Controls.Add(descLabel);

                var txtKey = new TextBox
                {
                    Multiline = true,
                    Size = new Size(440, 75),
                    Location = new Point(30, 201),
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Consolas", 9F, FontStyle.Regular),
                    PlaceholderText = "Buraya lisans anahtarınızı yapıştırın..."
                };
                container.Controls.Add(txtKey);

                var btnActivate = new Button
                {
                    Text = "Lisansı Etkinleştir",
                    Size = new Size(180, 40),
                    Location = new Point(90, 330),
                    BackColor = Color.FromArgb(74, 90, 120),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnActivate.FlatAppearance.BorderSize = 0;
                container.Controls.Add(btnActivate);

                bool isRequired = !LicenseSystem.IsLicenseActive && (LicenseSystem.IsTrialExpired || LicenseSystem.TimeRollbackDetected);

                var btnCancel = new Button
                {
                    Text = isRequired ? "Çıkış" : "İptal",
                    Size = new Size(130, 40),
                    Location = new Point(280, 330),
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCancel.FlatAppearance.BorderColor = isRequired ? Color.Red : Color.Gray;
                btnCancel.FlatAppearance.BorderSize = 1;
                container.Controls.Add(btnCancel);

                btnActivate.Click += (s, e) =>
                {
                    string keyText = txtKey.Text.Trim();
                    if (string.IsNullOrEmpty(keyText))
                    {
                        MessageBox.Show("Lütfen geçerli bir lisans anahtarı girin.", "Lisans Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    try
                    {
                        File.WriteAllText(LicenseSystem.LicenseFilePath, keyText);
                        LicenseSystem.Initialize();
                        if (LicenseSystem.IsLicenseActive)
                        {
                            MessageBox.Show("BigLineconnect Pro başarıyla etkinleştirildi! Teşekkür ederiz.", "Lisans Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            CheckLicensingOnLoad();
                        }
                        else
                        {
                            MessageBox.Show("Geçersiz veya süresi dolmuş lisans anahtarı.", "Lisans Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lisans kaydedilemedi: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                btnCancel.Click += (s, e) =>
                {
                    bool currentRequired = !LicenseSystem.IsLicenseActive && (LicenseSystem.IsTrialExpired || LicenseSystem.TimeRollbackDetected);
                    if (currentRequired)
                    {
                        Application.Exit();
                    }
                    else
                    {
                        HideLicensingOverlay();
                    }
                };

                this.Controls.Add(_licensingOverlay);
                _licensingOverlay.BringToFront();
            }

            // Dynamically update Cancel button text and style on every show
            var overlayPanel = _licensingOverlay.Controls.OfType<Panel>().FirstOrDefault();
            if (overlayPanel != null)
            {
                var cancelBtn = overlayPanel.Controls.OfType<Button>().FirstOrDefault(b => b.Text == "Çıkış" || b.Text == "İptal");
                if (cancelBtn != null)
                {
                    bool isReq = !LicenseSystem.IsLicenseActive && (LicenseSystem.IsTrialExpired || LicenseSystem.TimeRollbackDetected);
                    cancelBtn.Text = isReq ? "Çıkış" : "İptal";
                    cancelBtn.FlatAppearance.BorderColor = isReq ? Color.Red : Color.Gray;
                }
            }

            _licensingOverlay.Size = this.ClientSize;
            _licensingOverlay.Visible = true;
            _licensingOverlay.BringToFront();
        }

        private void HideLicensingOverlay()
        {
            if (_licensingOverlay != null)
            {
                _licensingOverlay.Visible = false;
            }
        }

        private void ShowHelpManual()
        {
            try
            {
                string helpPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "yardim_kilavuzu.html");
                string html = @"<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>BigLineconnect - Kullanım Kılavuzu & Yardım Merkezi</title>
    <style>
        :root {
            --bg-color: #f8fafc;
            --card-bg: #ffffff;
            --text-primary: #1e293b;
            --text-secondary: #64748b;
            --brand-blue: #2563eb;
            --brand-dark: #0f172a;
            --border-color: #e2e8f0;
            --accent-bg: #f1f5f9;
        }
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
        }
        body {
            background-color: var(--bg-color);
            color: var(--text-primary);
            padding: 40px 20px;
            display: flex;
            justify-content: center;
            align-items: flex-start;
            min-height: 100vh;
            line-height: 1.6;
        }
        .container {
            width: 100%;
            max-width: 820px;
            background-color: var(--card-bg);
            border: 1px solid var(--border-color);
            border-radius: 12px;
            padding: 45px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.04);
        }
        header {
            border-bottom: 1px solid var(--border-color);
            padding-bottom: 24px;
            margin-bottom: 32px;
        }
        header h1 {
            color: var(--brand-dark);
            font-size: 26px;
            font-weight: 700;
            margin-bottom: 8px;
            letter-spacing: -0.5px;
        }
        header h1 span {
            color: var(--brand-blue);
        }
        .subtitle {
            font-size: 15px;
            color: var(--text-secondary);
        }
        .step-section {
            margin-bottom: 24px;
            padding: 22px;
            background-color: #ffffff;
            border-radius: 8px;
            border: 1px solid var(--border-color);
        }
        .step-title {
            font-size: 16px;
            color: var(--brand-dark);
            margin-bottom: 12px;
            display: flex;
            align-items: center;
            gap: 12px;
            font-weight: 600;
        }
        .step-title span {
            background-color: var(--brand-blue);
            color: #ffffff;
            width: 24px;
            height: 24px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 13px;
            font-weight: 700;
            flex-shrink: 0;
        }
        p {
            font-size: 14px;
            color: var(--text-secondary);
            margin-bottom: 10px;
        }
        ol, ul {
            padding-left: 20px;
            font-size: 14px;
            color: var(--text-primary);
        }
        li {
            margin-bottom: 8px;
        }
        strong {
            color: var(--brand-dark);
        }
        .badge {
            background-color: #eff6ff;
            color: var(--brand-blue);
            border: 1px solid #bfdbfe;
            padding: 2px 8px;
            border-radius: 4px;
            font-size: 12px;
            font-weight: 600;
        }
        .faq-section {
            background-color: var(--accent-bg);
            border: 1px solid var(--border-color);
            padding: 28px;
            border-radius: 10px;
            margin-top: 36px;
        }
        .faq-section h2 {
            color: var(--brand-dark);
            font-size: 18px;
            margin-bottom: 20px;
            border-bottom: 1px solid var(--border-color);
            padding-bottom: 10px;
            font-weight: 600;
        }
        .faq-item {
            margin-bottom: 18px;
        }
        .faq-q {
            font-weight: 600;
            color: var(--brand-dark);
            font-size: 14px;
            margin-bottom: 4px;
        }
        .faq-a {
            font-size: 13.5px;
            color: var(--text-secondary);
        }
        footer {
            text-align: center;
            margin-top: 36px;
            padding-top: 20px;
            border-top: 1px solid var(--border-color);
            font-size: 13px;
            color: var(--text-secondary);
        }
        footer a {
            color: var(--brand-blue);
            text-decoration: none;
            font-weight: 500;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <header>
            <h1><span>BigLineconnect</span> Kullanım Kılavuzu</h1>
            <p class=""subtitle"">Uzaktan Masaüstü Bağlantısı ve Teknik Destek Rehberi</p>
        </header>

        <div class=""step-section"">
            <div class=""step-title""><span>1</span> Bilgisayarımı Başka Biri Yönetecek (Ekran Paylaşımı)</div>
            <p>Bir teknik destek uzmanının bilgisayarınıza bağlanmasını istiyorsanız:</p>
            <ol>
                <li>Arayüzün sol tarafındaki <span class=""badge"">Bu Bilgisayar</span> başlığı altında yer alan 9 haneli ID numarasını kopyalayın.</li>
                <li>Bu numarayı destek uzmanına iletin.</li>
                <li>Bağlantı kurulduğunda ekranda onay penceresi belirecektir. ""Onayla"" butonuna basarak masaüstünüzü paylaşabilirsiniz.</li>
            </ol>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>2</span> Başka Bir Bilgisayara Bağlanacağım</div>
            <p>Uzaktaki bir bilgisayarı kontrol etmek için:</p>
            <ol>
                <li>Arayüzün sağındaki <strong>""Karşı Bilgisayar""</strong> kutusuna hedef cihazın 9 haneli ID numarasını yazın.</li>
                <li><strong>""Bağlantı Kur""</strong> butonuna tıklayın.</li>
                <li>Hedef cihaz şifre ile korunuyorsa erişim şifresini girerek oturumu başlatın.</li>
            </ol>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>3</span> Dosya Transferi (Dosya Yöneticisi)</div>
            <p>İki bilgisayar arasında dosya aktarımı yapmak için:</p>
            <ul>
                <li>Uzak masaüstü penceresindeki üst menüden <strong>""Dosya Yöneticisi""</strong> butonuna basın.</li>
                <li>Kendi bilgisayarınızdan dosyaları sürükleyip hedef klasöre bırakabilir veya indirme/yükleme butonlarını kullanabilirsiniz.</li>
            </ul>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>4</span> Otomatik Pano Senkronizasyonu (Ctrl+C / Ctrl+V)</div>
            <p>Metin ve pano verileri iki cihaz arasında otomatik olarak eşlenir:</p>
            <ul>
                <li>Kendi bilgisayarınızda kopyaladığınız metni (Ctrl+C), uzak masaüstü ekranında doğrudan yapıştırabilirsiniz (Ctrl+V).</li>
            </ul>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>5</span> Canlı Sohbet (Chat)</div>
            <p>Bağlantı esnasında kullanıcı ile iletişim kurmak için:</p>
            <ul>
                <li>Uzak bağlantı araç çubuğunda yer alan <strong>""Sohbet""</strong> simgesine tıklayarak anlık mesajlaşma penceresini açabilirsiniz.</li>
            </ul>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>6</span> Çoklu Monitör Desteği</div>
            <p>Uzaktaki bilgisayarda birden fazla ekran bağlıysa:</p>
            <ul>
                <li>Üst paneldeki monitör seçim açılır menüsünden dilediğiniz ekranı (Ekran 1, Ekran 2) seçerek anında geçiş yapabilirsiniz.</li>
            </ul>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>7</span> Kayıtlı Bilgisayarlar (Rehber)</div>
            <p>Sık bağlandığınız bilgisayarları rehbere kaydederek erişimi kolaylaştırın:</p>
            <ul>
                <li>Rehber panelindeki <strong>""Ekle""</strong> butonuna tıklayarak ID ve müşteri adını kaydedin.</li>
                <li>Sonraki bağlantılarda rehberden çift tıklayarak anında oturum açabilirsiniz.</li>
            </ul>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>8</span> Şifreli Erişim ve İnsansız Erişim</div>
            <p>Bilgisayarınızın başında kimse yokken güvenli erişim sağlamak için:</p>
            <ul>
                <li>Arayüzdeki <strong>""Kişisel Erişim Şifresi Kullan""</strong> kutusunu işaretleyin ve 6 haneli özel bir şifre belirleyin.</li>
            </ul>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>9</span> Uzaktan Yeniden Başlatma ve Otomatik Yeniden Bağlanma</div>
            <p>Bakım işlemlerinde sistemi uzaktan yeniden başlatmak için:</p>
            <ul>
                <li>Araç çubuğundaki <strong>""Yeniden Başlat""</strong> seçeneğini kullanın. Bilgisayar yeniden açıldığında servis otomatik çalışır ve bağlantınız kesintisiz olarak tekrar kurulur.</li>
            </ul>
        </div>

        <div class=""step-section"">
            <div class=""step-title""><span>10</span> Canlı Destek İsteği Gönderme</div>
            <p>Teknik uzmandan yardım almak için:</p>
            <ul>
                <li>Sağ alttaki <strong>""Destek İste / Sorun Bildir""</strong> butonuna basarak talebinizi iletin. İsteğiniz doğrudan uzman ekranına düşecektir.</li>
            </ul>
        </div>

        <div class=""faq-section"">
            <h2>Sık Sorulan Sorular</h2>
            
            <div class=""faq-item"">
                <div class=""faq-q"">Fare imleci hareket ediyor, bilgisayarım güvende mi?</div>
                <div class=""faq-a"">Evet, tamamen güvendesiniz. Bağlandığınız destek uzmanı işlem yapmaktadır. Bağlantıyı dilediğiniz an ekran üzerindeki menüden kapatabilirsiniz.</div>
            </div>

            <div class=""faq-item"">
                <div class=""faq-q"">Verilerim ve ekran görüntülerim saklanıyor mu?</div>
                <div class=""faq-a"">Hayır. Tüm görüntü ve veri aktarımları uçtan uca şifreli tünel üzerinden anlık olarak akar, sunucularımızda hiçbir ekran görüntüsü veya içerik kaydedilmez.</div>
            </div>

            <div class=""faq-item"">
                <div class=""faq-q"">Lisansımı nasıl aktif edebilirim?</div>
                <div class=""faq-a"">Ana ekranın sağ üst köşesinde bulunan ""Lisans Gir"" bağlantısına tıklayarak lisans anahtarınızı tanımlayabilirsiniz.</div>
            </div>
        </div>

        <footer>
            <p>BigLineconnect Bilgi Teknolojileri © 2026</p>
            <p style=""margin-top: 6px;"">Destek ve İletişim: <a href=""mailto:my@bigus.com.tr"">my@bigus.com.tr</a></p>
        </footer>
    </div>
</body>
</html>";
                File.WriteAllText(helpPath, html, Encoding.UTF8);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(helpPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Yardım kılavuzu açılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsEnableLinkedConnectionsActive()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("EnableLinkedConnections");
                        if (val != null)
                        {
                            return Convert.ToInt32(val) == 1;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private void SetEnableLinkedConnections(bool enable)
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    if (key != null)
                    {
                        key.SetValue("EnableLinkedConnections", enable ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
                MessageBox.Show("Ağ sürücüsü erişim ayarı başarıyla güncellendi! Değişikliklerin etkili olması için lütfen bilgisayarınızı yeniden başlatın.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show("Bu ayarı değiştirmek için lütfen uygulamayı Yönetici (Administrator) olarak çalıştırın veya Windows Kayıt Defterinden HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Policies\\System\\EnableLinkedConnections değerini 1 yapın.", "Yönetici Yetkisi Gerekli", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        public void RequestSupport_Click(object? sender, EventArgs e)
        {
            if (_idLabel == null || _idLabel.Text == "--- --- ---")
            {
                MessageBox.Show("Henüz sunucuya bağlanılamadı. Lütfen sunucu bağlantısının kurulmasını bekleyin.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hostId = _idLabel.Text.Replace(" ", "").Trim();

            // Cancellation flow if ticket is already active
            if (_hasActiveSubmittedTicket)
            {
                var result = MessageBox.Show("Destek talebinizi iptal etmek istediğinizden emin misiniz?", "Talebi İptal Et", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            string serverUrl = _actualRelayUrl;
                            string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", "/api/support/cancel");
                            
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                var json = $"{{\"id\":\"{Program.EscapeJson(hostId)}\"}}";
                                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                                await client.PostAsync(httpUrl, content);
                            }
                        }
                        catch { }
                    });

                    _hasActiveSubmittedTicket = false;
                    Program.ActiveSupportToken = "";
                    
                    if (_btnSupport != null)
                    {
                        _btnSupport.Text = "🆘 Destek İste / Sorun Bildir";
                        ApplyModernButtonStyle(_btnSupport, Color.FromArgb(74, 90, 120), Color.FromArgb(58, 72, 98), Color.White);
                    }
                    MessageBox.Show("Destek talebiniz başarıyla iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    AppendLog("[Destek] Destek talebi müşteri tarafından iptal edildi.");
                }
                return;
            }

            using (var dlg = new SupportRequestDialog())
            {
                if (ShowModalWithDimmedOverlay(dlg) == DialogResult.OK)
                {
                    if (!string.IsNullOrEmpty(dlg.CompanyCodeInput))
                    {
                        LicenseSystem.SaveCompanyCode(dlg.CompanyCodeInput, false);
                    }

                    string name = dlg.CustomerName;
                    string issue = dlg.IssueDescription;
                    string priority = GetNormalizedPriority(dlg.Priority, issue);
                    bool reqConfirm = dlg.RequiresConfirmation;
                    string token = Guid.NewGuid().ToString();

                    Program.ActiveSupportToken = token;

                    // Send to server asynchronously
                    Task.Run(async () =>
                    {
                        try
                        {
                            string serverUrl = _actualRelayUrl;
                            string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", "/api/support/create");
                            
                            using (var client = new System.Net.Http.HttpClient())
                            {
                                var json = $"{{\"id\":\"{Program.EscapeJson(hostId)}\",\"name\":\"{Program.EscapeJson(name)}\",\"issue\":\"{Program.EscapeJson(issue)}\",\"priority\":\"{Program.EscapeJson(priority)}\",\"token\":\"{Program.EscapeJson(token)}\",\"tenantId\":\"{Program.EscapeJson(LicenseSystem.CompanyCode)}\",\"requiresConfirmation\":{(reqConfirm ? "true" : "false")}}}";
                                var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
                                var response = await client.PostAsync(httpUrl, content);
                                string responseBody = await response.Content.ReadAsStringAsync();
                                
                                this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                                {
                                    if (response.IsSuccessStatusCode)
                                    {
                                        _hasActiveSubmittedTicket = true;
                                        if (_btnSupport != null)
                                        {
                                            _btnSupport.Text = "❌ Talebi İptal Et";
                                            ApplyModernButtonStyle(_btnSupport, Color.FromArgb(196, 57, 43), Color.FromArgb(163, 45, 33), Color.White);
                                        }

                                        SaveLocalSubmittedTicket(new LocalSubmittedTicket { Token = token, HostId = hostId, Name = name, Issue = issue, Priority = priority, TenantId = LicenseSystem.CompanyCode, CreatedAt = DateTime.Now, Status = "⏳ Sırada Bekliyor", Notes = "Destek uzmanının bağlanması bekleniyor..." });
                                        MessageBox.Show($"Destek talebiniz başarıyla '{LicenseSystem.CompanyCode}' kanalına iletildi.\n\nAçtığınız talepleri '📋 Taleplerim' butonundan takip edebilirsiniz.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        AppendLog($"[Destek] Destek talebi oluşturuldu: {name} ({LicenseSystem.CompanyCode}) - Öncelik: {priority} - {issue}");
                                    }
                                    else
                                    {
                                        MessageBox.Show($"Destek talebi iletilemedi. (Sunucu yanıtı {response.StatusCode}: {responseBody})", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                            {
                                MessageBox.Show($"Destek talebi gönderilirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            });
                        }
                    });
                }
            }
        }

        public class SupportRequestDialog : Form
        {
            private TextBox txtName;
            private TextBox txtCompanyCode;
            private TextBox txtIssue;
            private ComboBox cmbPriority;
            private CheckBox chkRequiresConfirmation;
            private Button btnSubmit;
            private Button btnCancel;
            private string _selectedPriority = "🟡 Orta";

            public string CustomerName => txtName.Text.Trim();
            public string CompanyCodeInput => txtCompanyCode.Text.Trim();
            public string IssueDescription => txtIssue.Text.Trim();
            public string Priority
            {
                get
                {
                    if (cmbPriority != null)
                    {
                        int idx = cmbPriority.SelectedIndex;
                        string txt = cmbPriority.SelectedItem?.ToString() ?? cmbPriority.Text ?? "";
                        if (idx == 0 || txt.Contains("Yüksek") || txt.Contains("Acil")) return "🔴 Yüksek";
                        if (idx == 2 || txt.Contains("Düşük") || txt.Contains("Rutin")) return "🟢 Düşük";
                        return "🟡 Orta";
                    }
                    return _selectedPriority;
                }
            }
            public bool RequiresConfirmation => chkRequiresConfirmation.Checked;

            public SupportRequestDialog()
            {
                this.Text = "Destek Talebi Oluştur";
                this.Size = new Size(420, 440);
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.StartPosition = FormStartPosition.CenterParent;
                this.BackColor = Color.FromArgb(245, 245, 246);
                this.ForeColor = Color.FromArgb(38, 40, 45);

                var lblName = new Label
                {
                    Text = "Ad Soyad / Firma Adı:",
                    Location = new Point(25, 15),
                    Size = new Size(355, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120)
                };
                this.Controls.Add(lblName);

                txtName = new TextBox
                {
                    Location = new Point(25, 36),
                    Size = new Size(355, 26),
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9.5F),
                    Text = System.Environment.MachineName
                };
                this.Controls.Add(txtName);

                var lblCompany = new Label
                {
                    Text = "Destek Alınan Bayi / Firma Kodu:",
                    Location = new Point(25, 68),
                    Size = new Size(355, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(191, 140, 15)
                };
                this.Controls.Add(lblCompany);

                txtCompanyCode = new TextBox
                {
                    Location = new Point(25, 90),
                    Size = new Size(355, 26),
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Text = (string.IsNullOrWhiteSpace(LicenseSystem.CompanyCode) || LicenseSystem.CompanyCode == "BIGLINE" || LicenseSystem.CompanyCode.StartsWith("V1.") || LicenseSystem.CompanyCode.StartsWith("V2.") || LicenseSystem.CompanyCode.StartsWith("V3.") || LicenseSystem.CompanyCode.StartsWith("V4.")) ? "BAYIKODU" : LicenseSystem.CompanyCode
                };
                this.Controls.Add(txtCompanyCode);

                var lblIssue = new Label
                {
                    Text = "Yaşadığınız Sorun (Açıklama):",
                    Location = new Point(25, 122),
                    Size = new Size(355, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120)
                };
                this.Controls.Add(lblIssue);

                txtIssue = new TextBox
                {
                    Location = new Point(25, 144),
                    Size = new Size(355, 60),
                    Multiline = true,
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Segoe UI", 10F)
                };
                this.Controls.Add(txtIssue);

                var lblPriority = new Label
                {
                    Text = "Talep Önceliği / Aciliyet Seviyesi:",
                    Location = new Point(25, 212),
                    Size = new Size(355, 20),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(191, 140, 15)
                };
                this.Controls.Add(lblPriority);

                cmbPriority = new ComboBox
                {
                    Location = new Point(25, 234),
                    Size = new Size(355, 28),
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
                };
                cmbPriority.Items.Add("🔴 Yüksek (Acil / Fatura-Kasa Kilitlendi)");
                cmbPriority.Items.Add("🟡 Orta (Normal Destek)");
                cmbPriority.Items.Add("🟢 Düşük (Bilgi / Rutin İşlem)");
                cmbPriority.SelectedIndex = 1;
                cmbPriority.SelectedIndexChanged += (s, e) => {
                    int idx = cmbPriority.SelectedIndex;
                    string txt = cmbPriority.SelectedItem?.ToString() ?? cmbPriority.Text ?? "";
                    if (idx == 0 || txt.Contains("Yüksek") || txt.Contains("Acil")) _selectedPriority = "🔴 Yüksek";
                    else if (idx == 2 || txt.Contains("Düşük") || txt.Contains("Rutin")) _selectedPriority = "🟢 Düşük";
                    else _selectedPriority = "🟡 Orta";
                };
                this.Controls.Add(cmbPriority);

                chkRequiresConfirmation = new CheckBox
                {
                    Text = "🛡️ Onaylı Bağlantı (Uzman bağlandığında onay iste)",
                    Location = new Point(25, 275),
                    Size = new Size(355, 26),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120),
                    Cursor = Cursors.Hand,
                    Checked = false
                };
                this.Controls.Add(chkRequiresConfirmation);

                btnSubmit = new Button
                {
                    Text = "🚀 Talebi Gönder",
                    Location = new Point(25, 296),
                    Size = new Size(172, 38),
                    BackColor = Color.FromArgb(74, 90, 120),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnSubmit.FlatAppearance.BorderSize = 0;
                btnSubmit.Click += (s, e) => {
                    if (string.IsNullOrEmpty(CustomerName))
                    {
                        MessageBox.Show("Lütfen adınızı veya firma adını girin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (string.IsNullOrEmpty(IssueDescription))
                    {
                        MessageBox.Show("Lütfen yaşadığınız sorunu kısaca açıklayın.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    int selIdx = cmbPriority.SelectedIndex;
                    string selStr = cmbPriority.SelectedItem?.ToString() ?? cmbPriority.Text ?? "";
                    if (selIdx == 0 || selStr.Contains("Yüksek") || selStr.Contains("Acil")) _selectedPriority = "🔴 Yüksek";
                    else if (selIdx == 2 || selStr.Contains("Düşük") || selStr.Contains("Rutin")) _selectedPriority = "🟢 Düşük";
                    else _selectedPriority = "🟡 Orta";

                    this.DialogResult = DialogResult.OK;
                    this.Close();
                };
                this.Controls.Add(btnSubmit);

                btnCancel = new Button
                {
                    Text = "İptal",
                    Location = new Point(208, 296),
                    Size = new Size(172, 38),
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCancel.FlatAppearance.BorderColor = Color.Gray;
                btnCancel.Click += (s, e) => {
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                };
                this.Controls.Add(btnCancel);

                var btnHistory = new Button
                {
                    Text = "📋 Daha Önce Açtığım Taleplerim ve Durumları",
                    Location = new Point(25, 344),
                    Size = new Size(355, 34),
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(74, 90, 120),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnHistory.FlatAppearance.BorderColor = Color.FromArgb(74, 90, 120);
                btnHistory.Click += (s, e) => {
                    MainWindow.Instance?.ShowMySubmittedTicketsDialog();
                };
                this.Controls.Add(btnHistory);
            }
        }

        public void ShowMySubmittedTicketsDialog()
        {
            try
            {
                using (var form = new MySubmittedTicketsForm(this))
                {
                    form.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Taleplerim ekranı açılamadı: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static string GetNormalizedPriority(string? priorityText, string? issueText)
        {
            string p = (priorityText ?? "").ToLowerInvariant();

            // 1. Explicit Dropdown Selection Priority Check (Always Respect User Choice)
            if (p.Contains("yüksek") || p.Contains("yuksek") || p.Contains("high") || p.Contains("🔴"))
            {
                return "🔴 Yüksek";
            }
            if (p.Contains("düşük") || p.Contains("dusuk") || p.Contains("low") || p.Contains("🟢"))
            {
                return "🟢 Düşük";
            }
            if (p.Contains("orta") || p.Contains("medium") || p.Contains("🟡"))
            {
                return "🟡 Orta";
            }

            // 2. Fallback: Auto-Detect from Issue Description ONLY if priority was not explicitly specified
            string iss = (issueText ?? "").ToLowerInvariant();
            if (iss.Contains("çok acil") || iss.Contains("kilitlendi") || iss.Contains("fatura kesemiyoruz") || iss.Contains("kasa kilit"))
            {
                return "🔴 Yüksek";
            }
            if (iss.Contains("rutin") || iss.Contains("bilgi almak"))
            {
                return "🟢 Düşük";
            }

            return "🟡 Orta";
        }

        public class LocalSubmittedTicket
        {
            public string Token { get; set; } = "";
            public string HostId { get; set; } = "";
            public string Name { get; set; } = "";
            public string Issue { get; set; } = "";
            public string Priority { get; set; } = "Orta";
            public string TenantId { get; set; } = "";
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public string Status { get; set; } = "⏳ Sırada Bekliyor";
            public string Notes { get; set; } = "";
        }

        public static string GetLocalSubmittedTicketsFilePath()
        {
            string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BigLineconnect");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, "my_submitted_tickets.json");
        }

        public static void SaveLocalSubmittedTicket(LocalSubmittedTicket ticket)
        {
            try
            {
                ticket.Priority = GetNormalizedPriority(ticket.Priority, ticket.Issue);
                var tickets = LoadLocalSubmittedTickets();
                tickets.RemoveAll(t => 
                    !string.IsNullOrEmpty(ticket.Token) && !string.IsNullOrEmpty(t.Token) && t.Token.Trim() == ticket.Token.Trim()
                );
                tickets.Insert(0, ticket);
                if (tickets.Count > 100) tickets = tickets.Take(100).ToList();

                string json = System.Text.Json.JsonSerializer.Serialize(tickets, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(GetLocalSubmittedTicketsFilePath(), json);
            }
            catch { }
        }

        public static List<LocalSubmittedTicket> LoadLocalSubmittedTickets()
        {
            try
            {
                string path = GetLocalSubmittedTicketsFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var list = System.Text.Json.JsonSerializer.Deserialize<List<LocalSubmittedTicket>>(json);
                    if (list != null)
                    {
                        foreach (var item in list)
                        {
                            item.Priority = GetNormalizedPriority(item.Priority, item.Issue);
                        }
                        return list;
                    }
                }
            }
            catch { }
            return new List<LocalSubmittedTicket>();
        }

        public class MySubmittedTicketsForm : Form
        {
            private MainWindow _main;
            private ListView lstTickets;
            private TextBox txtSearchSubmitted;
            private Button btnRefresh;
            private Button btnNewTicket;
            private Button btnCancelTicket;
            private Button btnClose;

            private List<LocalSubmittedTicket> _masterSubmittedList = new();
            private int _sortColumnIndex = 0;
            private bool _sortAscending = false;

            public MySubmittedTicketsForm(MainWindow main)
            {
                _main = main;
                this.Text = "📋 Açtığım Destek Taleplerim ve Durumları";
                this.Size = new Size(680, 460);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.FromArgb(245, 245, 246);
                this.ForeColor = Color.FromArgb(38, 40, 45);

                var lblTitle = new Label
                {
                    Text = "📋 Açtığım Destek Talepleri Geçmişi",
                    Location = new Point(20, 12),
                    Size = new Size(620, 28),
                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120)
                };
                this.Controls.Add(lblTitle);

                txtSearchSubmitted = new TextBox
                {
                    Location = new Point(20, 44),
                    Size = new Size(625, 26),
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(74, 90, 120),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                    BorderStyle = BorderStyle.FixedSingle,
                    Text = "🔍 Tüm Kolonlarda Ara (Tarih, Öncelik, Sorun, Durum, Not...)"
                };

                txtSearchSubmitted.GotFocus += (s, e) => {
                    if (txtSearchSubmitted.Text.StartsWith("🔍"))
                    {
                        txtSearchSubmitted.Text = "";
                        txtSearchSubmitted.ForeColor = Color.FromArgb(38, 40, 45);
                        txtSearchSubmitted.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
                    }
                };

                txtSearchSubmitted.LostFocus += (s, e) => {
                    if (string.IsNullOrWhiteSpace(txtSearchSubmitted.Text))
                    {
                        txtSearchSubmitted.Text = "🔍 Tüm Kolonlarda Ara (Tarih, Öncelik, Sorun, Durum, Not...)";
                        txtSearchSubmitted.ForeColor = Color.FromArgb(74, 90, 120);
                        txtSearchSubmitted.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                    }
                };

                txtSearchSubmitted.TextChanged += (s, e) => {
                    FilterAndRenderTickets();
                };
                this.Controls.Add(txtSearchSubmitted);

                lstTickets = new ListView
                {
                    Location = new Point(20, 76),
                    Size = new Size(625, 274),
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = false,
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(107, 118, 132),
                    Font = new Font("Segoe UI", 9.5F),
                    BorderStyle = BorderStyle.FixedSingle,
                    OwnerDraw = true
                };
                lstTickets.Columns.Add("Tarih / Saat ↕", 115);
                lstTickets.Columns.Add("Öncelik ↕", 85);
                lstTickets.Columns.Add("Sorun / Açıklama ↕", 185);
                lstTickets.Columns.Add("Durum ↕", 115);
                lstTickets.Columns.Add("Uzman Çözüm Notu ↕", 125);

                lstTickets.ColumnClick += (s, e) =>
                {
                    if (_sortColumnIndex == e.Column)
                    {
                        _sortAscending = !_sortAscending;
                    }
                    else
                    {
                        _sortColumnIndex = e.Column;
                        _sortAscending = true;
                    }
                    FilterAndRenderTickets();
                };

                // Eye-pleasing soft-dark custom drawing (Zero stark white lines, soft zebra rows)
                lstTickets.DrawColumnHeader += (s, e) =>
                {
                    using var headerBrush = new SolidBrush(Color.FromArgb(245, 245, 246));
                    e.Graphics.FillRectangle(headerBrush, e.Bounds);

                    using var borderPen = new Pen(Color.FromArgb(231, 232, 234));
                    e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    if (e.ColumnIndex < lstTickets.Columns.Count - 1)
                    {
                        e.Graphics.DrawLine(borderPen, e.Bounds.Right - 1, e.Bounds.Top + 4, e.Bounds.Right - 1, e.Bounds.Bottom - 4);
                    }

                    Color headerColor = (e.ColumnIndex == _sortColumnIndex) ? Color.FromArgb(22, 140, 74) : Color.FromArgb(74, 90, 120);
                    string sortArrow = (e.ColumnIndex == _sortColumnIndex) ? (_sortAscending ? " ▲" : " ▼") : "";
                    string headerText = (e.Header?.Text ?? "").Replace(" ↕", "") + sortArrow;

                    TextRenderer.DrawText(e.Graphics, headerText, new Font("Segoe UI", 9F, FontStyle.Bold),
                        new Rectangle(e.Bounds.X + 6, e.Bounds.Y + 4, e.Bounds.Width - 10, e.Bounds.Height - 8),
                        headerColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                };

                lstTickets.DrawSubItem += (s, e) =>
                {
                    bool isSelected = e.Item != null && e.Item.Selected;
                    Color rowBg = isSelected ? Color.FromArgb(74, 90, 120) :
                                  (e.ItemIndex % 2 == 0 ? Color.FromArgb(245, 245, 246) : Color.FromArgb(245, 245, 246));

                    using var bgBrush = new SolidBrush(rowBg);
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);

                    using var dividerPen = new Pen(Color.FromArgb(231, 232, 234));
                    e.Graphics.DrawLine(dividerPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                    e.Graphics.DrawLine(dividerPen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom - 1);

                    Color textColor = isSelected ? Color.White : Color.FromArgb(107, 118, 132);
                    if (!isSelected)
                    {
                        if (e.ColumnIndex == 1) // Priority Column
                        {
                            string pText = e.SubItem?.Text ?? "";
                            if (pText.Contains("Yüksek")) textColor = Color.FromArgb(196, 57, 43);
                            else if (pText.Contains("Orta")) textColor = Color.FromArgb(191, 140, 15);
                            else textColor = Color.FromArgb(22, 140, 74);
                        }
                        else if (e.ColumnIndex == 3) // Status Column
                        {
                            textColor = e.Item?.ForeColor ?? Color.FromArgb(107, 118, 132);
                        }
                    }

                    TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", e.Item?.Font ?? lstTickets.Font,
                        new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 10, e.Bounds.Height),
                        textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                };

                lstTickets.MouseDoubleClick += (s, e) =>
                {
                    if (lstTickets.SelectedItems.Count > 0 && lstTickets.SelectedItems[0].Tag is LocalSubmittedTicket t)
                    {
                        ShowTicketDetailModal(t);
                    }
                };

                this.Controls.Add(lstTickets);

                btnRefresh = new Button
                {
                    Text = "🔄 Yenile",
                    Location = new Point(20, 365),
                    Size = new Size(110, 35),
                    BackColor = Color.FromArgb(58, 62, 70),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnRefresh.Click += (s, e) => LoadAndRefreshTickets();
                this.Controls.Add(btnRefresh);

                btnNewTicket = new Button
                {
                    Text = "➕ Yeni Talep Aç",
                    Location = new Point(140, 365),
                    Size = new Size(130, 35),
                    BackColor = Color.FromArgb(22, 140, 74),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnNewTicket.Click += (s, e) =>
                {
                    this.Close();
                    _main.RequestSupport_Click(s, e);
                };
                this.Controls.Add(btnNewTicket);

                btnCancelTicket = new Button
                {
                    Text = "❌ Talebi İptal Et",
                    Location = new Point(280, 365),
                    Size = new Size(130, 35),
                    BackColor = Color.FromArgb(196, 57, 43),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCancelTicket.Click += (s, e) => CancelSelectedTicket();
                this.Controls.Add(btnCancelTicket);

                btnClose = new Button
                {
                    Text = "Kapat",
                    Location = new Point(535, 365),
                    Size = new Size(110, 35),
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);

                this.Load += (s, e) => LoadAndRefreshTickets();
            }

            private void FilterAndRenderTickets()
            {
                if (lstTickets == null || _masterSubmittedList == null) return;
                lstTickets.Items.Clear();

                string search = (txtSearchSubmitted != null && !txtSearchSubmitted.Text.StartsWith("🔍")) 
                    ? txtSearchSubmitted.Text.Trim().ToLowerInvariant() : "";

                var filtered = _masterSubmittedList.Where(t => {
                    if (string.IsNullOrEmpty(search)) return true;
                    string timeStr = t.CreatedAt != default ? t.CreatedAt.ToString("dd.MM.yyyy HH:mm").ToLowerInvariant() : "";
                    string priorityStr = (t.Priority ?? "").ToLowerInvariant();
                    string issueStr = (t.Issue ?? "").ToLowerInvariant();
                    string statusStr = (t.Status ?? "").ToLowerInvariant();
                    string notesStr = (t.Notes ?? "").ToLowerInvariant();
                    string tokenStr = (t.Token ?? "").ToLowerInvariant();
                    string hostIdStr = (t.HostId ?? "").ToLowerInvariant();

                    return timeStr.Contains(search) ||
                           priorityStr.Contains(search) ||
                           issueStr.Contains(search) ||
                           statusStr.Contains(search) ||
                           notesStr.Contains(search) ||
                           tokenStr.Contains(search) ||
                           hostIdStr.Contains(search);
                }).ToList();

                int GetPriorityRank(string p)
                {
                    if (string.IsNullOrEmpty(p)) return 1;
                    if (p.Contains("Yüksek") || p.Contains("🔴")) return 0;
                    if (p.Contains("Düşük") || p.Contains("🟢")) return 2;
                    return 1;
                }

                IEnumerable<LocalSubmittedTicket> sorted = filtered;
                switch (_sortColumnIndex)
                {
                    case 0: // Tarih / Saat
                        sorted = _sortAscending ? filtered.OrderBy(x => x.CreatedAt) : filtered.OrderByDescending(x => x.CreatedAt);
                        break;
                    case 1: // Öncelik
                        sorted = _sortAscending ? filtered.OrderBy(x => GetPriorityRank(x.Priority)) : filtered.OrderByDescending(x => GetPriorityRank(x.Priority));
                        break;
                    case 2: // Sorun / Açıklama
                        sorted = _sortAscending ? filtered.OrderBy(x => x.Issue) : filtered.OrderByDescending(x => x.Issue);
                        break;
                    case 3: // Durum
                        sorted = _sortAscending ? filtered.OrderBy(x => x.Status) : filtered.OrderByDescending(x => x.Status);
                        break;
                    case 4: // Uzman Çözüm Notu
                        sorted = _sortAscending ? filtered.OrderBy(x => x.Notes) : filtered.OrderByDescending(x => x.Notes);
                        break;
                    default:
                        sorted = filtered;
                        break;
                }

                string streamFlagPath = Program.GetSharedStreamActivePath();
                bool isStreamActive = Program._isStreaming || File.Exists(streamFlagPath);

                foreach (var t in sorted)
                {
                    t.Priority = GetNormalizedPriority(t.Priority, t.Issue);
                    string timeStr = t.CreatedAt != default ? t.CreatedAt.ToString("dd.MM.yyyy HH:mm") : "---";
                    var item = new ListViewItem(timeStr);
                    
                    string pText = t.Priority;
                    if (pText.Contains("Yüksek")) item.SubItems.Add("🔴 Yüksek");
                    else if (pText.Contains("Düşük")) item.SubItems.Add("🟢 Düşük");
                    else item.SubItems.Add("🟡 Orta");

                    item.SubItems.Add(t.Issue);

                    string statusText = t.Status ?? "";

                    if (statusText.Contains("Çözüldü") && !statusText.Contains("Çözülmedi"))
                    {
                        item.SubItems.Add("✅ Çözüldü");
                        item.ForeColor = Color.FromArgb(22, 140, 74);
                    }
                    else if (statusText.Contains("Çözülmedi"))
                    {
                        item.SubItems.Add("❌ Çözülemedi");
                        item.ForeColor = Color.FromArgb(196, 57, 43);
                    }
                    else if (statusText.Contains("Takip") || statusText.Contains("Takipte"))
                    {
                        item.SubItems.Add("📌 Takip Edilecek");
                        item.ForeColor = Color.FromArgb(191, 140, 15);
                    }
                    else if (isStreamActive || statusText.Contains("Bağlandı") || statusText.Contains("İşlemde"))
                    {
                        item.SubItems.Add("🟢 Uzman Bağlandı (İşlem Yapılıyor...)");
                        item.ForeColor = Color.FromArgb(22, 140, 74);
                        if (string.IsNullOrEmpty(t.Notes) || t.Notes == "—")
                        {
                            t.Notes = "Uzman bilgisayarınıza bağlı, işlem gerçekleştiriliyor...";
                        }
                    }
                    else
                    {
                        item.SubItems.Add(string.IsNullOrEmpty(statusText) ? "⏳ Sırada Bekliyor" : statusText);
                        item.ForeColor = Color.FromArgb(191, 140, 15);
                    }

                    item.SubItems.Add(string.IsNullOrEmpty(t.Notes) ? "—" : t.Notes);
                    item.Tag = t;
                    lstTickets.Items.Add(item);
                }
            }

            private void LoadAndRefreshTickets()
            {
                var localTickets = LoadLocalSubmittedTickets();
                string myHostId = Program.CurrentHostId != null ? Program.CurrentHostId.Replace(" ", "").Trim() : "";

                Task.Run(async () =>
                {
                    List<LocalSubmittedTicket> serverHistoryList = new();
                    try
                    {
                        string serverUrl = _main._actualRelayUrl;
                        string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", $"/api/support/history/list?hostId={myHostId}");
                        using var client = new System.Net.Http.HttpClient();
                        var res = await client.GetAsync(httpUrl);
                        if (res.IsSuccessStatusCode)
                        {
                            string json = await res.Content.ReadAsStringAsync();
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                string GetProp(JsonElement el, string p1, string p2)
                                {
                                    if (el.TryGetProperty(p1, out var val1) && val1.ValueKind == JsonValueKind.String) return val1.GetString() ?? "";
                                    if (el.TryGetProperty(p2, out var val2) && val2.ValueKind == JsonValueKind.String) return val2.GetString() ?? "";
                                    return "";
                                }

                                string token = GetProp(elem, "token", "Token");
                                string issue = GetProp(elem, "issue", "Issue");
                                string priority = GetProp(elem, "priority", "Priority");
                                if (string.IsNullOrEmpty(priority)) priority = "Orta";
                                string status = GetProp(elem, "status", "Status");
                                string notes = GetProp(elem, "notes", "Notes");
                                string createdAtStr = GetProp(elem, "createdAt", "CreatedAt");
                                DateTime dt = !string.IsNullOrEmpty(createdAtStr) && DateTime.TryParse(createdAtStr, out var dtParsed) ? dtParsed :
                                              (elem.TryGetProperty("resolvedAt", out var p4) && DateTime.TryParse(p4.GetString(), out var dtVal) ? dtVal :
                                              (elem.TryGetProperty("ResolvedAt", out var p4b) && DateTime.TryParse(p4b.GetString(), out var dtValB) ? dtValB : DateTime.Now));

                                serverHistoryList.Add(new LocalSubmittedTicket
                                {
                                    Token = token,
                                    Issue = issue,
                                    Priority = priority,
                                    Status = status,
                                    Notes = notes,
                                    CreatedAt = dt
                                });
                            }
                        }
                    }
                    catch { }

                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        if (this.IsDisposed) return;
                        string streamFlagPath = Program.GetSharedStreamActivePath();
                        bool isStreamActive = Program._isStreaming || File.Exists(streamFlagPath);
                        if (isStreamActive && btnCancelTicket != null)
                        {
                            btnCancelTicket.Text = "🟢 Uzman Bağlı";
                            btnCancelTicket.BackColor = Color.FromArgb(22, 140, 74);
                            btnCancelTicket.Enabled = false;
                        }
                        var combinedList = new List<LocalSubmittedTicket>(localTickets);

                        foreach (var item in combinedList)
                        {
                            item.Priority = GetNormalizedPriority(item.Priority, item.Issue);
                        }

                        foreach (var sh in serverHistoryList)
                        {
                            sh.Priority = GetNormalizedPriority(sh.Priority, sh.Issue);
                            var existing = combinedList.FirstOrDefault(t => 
                                (!string.IsNullOrEmpty(t.Token) && !string.IsNullOrEmpty(sh.Token) && t.Token.Trim().Equals(sh.Token.Trim(), StringComparison.OrdinalIgnoreCase)) ||
                                (!string.IsNullOrEmpty(t.Issue) && !string.IsNullOrEmpty(sh.Issue) && t.Issue.Trim().Equals(sh.Issue.Trim(), StringComparison.OrdinalIgnoreCase))
                            );
                            if (existing != null)
                            {
                                existing.Status = sh.Status;
                                existing.Notes = sh.Notes;
                                if (!string.IsNullOrEmpty(sh.Token)) existing.Token = sh.Token;

                                // Respect local user explicit priority if local priority is Yüksek or Düşük
                                if (existing.Priority.Contains("Yüksek") || existing.Priority.Contains("🔴"))
                                {
                                    // Local Yüksek stays Yüksek
                                }
                                else if (existing.Priority.Contains("Düşük") || existing.Priority.Contains("🟢"))
                                {
                                    // Local Düşük stays Düşük
                                }
                                else if (!string.IsNullOrEmpty(sh.Priority) && (sh.Priority.Contains("Yüksek") || sh.Priority.Contains("🔴")))
                                {
                                    existing.Priority = "🔴 Yüksek";
                                }
                                else if (!string.IsNullOrEmpty(sh.Priority) && (sh.Priority.Contains("Düşük") || sh.Priority.Contains("🟢")))
                                {
                                    existing.Priority = "🟢 Düşük";
                                }
                            }
                            else
                            {
                                combinedList.Add(sh);
                            }
                        }

                        try
                        {
                            string updatedJson = System.Text.Json.JsonSerializer.Serialize(combinedList, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                            File.WriteAllText(GetLocalSubmittedTicketsFilePath(), updatedJson);
                        }
                        catch { }

                        int GetRank(string p)
                        {
                            if (string.IsNullOrEmpty(p)) return 1;
                            if (p.Contains("Yüksek") || p.Contains("🔴")) return 0;
                            if (p.Contains("Düşük") || p.Contains("🟢")) return 2;
                            return 1;
                        }

                        _masterSubmittedList = combinedList
                            .OrderBy(x => GetRank(x.Priority))
                            .ThenByDescending(x => x.CreatedAt)
                            .ToList();

                        FilterAndRenderTickets();
                    });
                });
            }

            private void CancelSelectedTicket()
            {
                if (lstTickets.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Lütfen iptal etmek istediğiniz talebi seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var item = lstTickets.SelectedItems[0];
                if (item.Tag is LocalSubmittedTicket t)
                {
                    var res = MessageBox.Show($"'({t.Issue})' başlıklı talebinizi iptal etmek istiyor musunuz?", "Talep İptali", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        var localList = LoadLocalSubmittedTickets();
                        localList.RemoveAll(x => x.Token == t.Token || x.Issue == t.Issue);
                        string json = System.Text.Json.JsonSerializer.Serialize(localList, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(GetLocalSubmittedTicketsFilePath(), json);
                        LoadAndRefreshTickets();
                        MessageBox.Show("Talebiniz iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }

            private void ShowTicketDetailModal(LocalSubmittedTicket ticket)
            {
                using (Form dlg = new Form())
                {
                    dlg.Text = "📄 Talep ve İşlem Detayları";
                    dlg.Size = new Size(500, 440);
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                    dlg.MaximizeBox = false;
                    dlg.MinimizeBox = false;
                    dlg.BackColor = Color.FromArgb(245, 245, 246);
                    dlg.ForeColor = Color.FromArgb(38, 40, 45);

                    Label lblCustomer = new Label
                    {
                        Text = $"📋 Ad Soyad / Firma: {(!string.IsNullOrEmpty(ticket.Name) ? ticket.Name : "Bilinmiyor")}",
                        Location = new Point(20, 15),
                        Size = new Size(445, 22),
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(74, 90, 120)
                    };
                    dlg.Controls.Add(lblCustomer);

                    string timeStr = ticket.CreatedAt != default ? ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss") : "Bilinmiyor";
                    Label lblDate = new Label
                    {
                        Text = $"⏱️ Tarih / Saat: {timeStr}",
                        Location = new Point(20, 42),
                        Size = new Size(445, 20),
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(22, 140, 74)
                    };
                    dlg.Controls.Add(lblDate);

                    string pText = ticket.Priority ?? "Orta";
                    Color pColor = Color.FromArgb(191, 140, 15);
                    if (pText.Contains("Yüksek")) { pText = "🔴 Yüksek (Acil / Fatura-Kasa)"; pColor = Color.FromArgb(196, 57, 43); }
                    else if (pText.Contains("Düşük")) { pText = "🟢 Düşük (Rutin İşlem)"; pColor = Color.FromArgb(22, 140, 74); }
                    else { pText = "🟡 Orta (Normal Destek)"; }

                    Label lblPriority = new Label
                    {
                        Text = $"🎯 Öncelik Seviyesi: {pText}",
                        Location = new Point(20, 67),
                        Size = new Size(445, 20),
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                        ForeColor = pColor
                    };
                    dlg.Controls.Add(lblPriority);

                    Label lblStatus = new Label
                    {
                        Text = $"📌 Mevcut Durum: {ticket.Status}",
                        Location = new Point(20, 92),
                        Size = new Size(445, 20),
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(74, 90, 120)
                    };
                    dlg.Controls.Add(lblStatus);

                    Label lblIssueHeader = new Label
                    {
                        Text = "📝 Bildirilen Sorun / Açıklama:",
                        Location = new Point(20, 122),
                        Size = new Size(445, 18),
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(107, 118, 132)
                    };
                    dlg.Controls.Add(lblIssueHeader);

                    TextBox txtIssue = new TextBox
                    {
                        Text = ticket.Issue,
                        Location = new Point(20, 143),
                        Size = new Size(445, 75),
                        Multiline = true,
                        ReadOnly = true,
                        ScrollBars = ScrollBars.Vertical,
                        BackColor = Color.FromArgb(245, 245, 246),
                        ForeColor = Color.FromArgb(38, 40, 45),
                        BorderStyle = BorderStyle.FixedSingle,
                        Font = new Font("Segoe UI", 9.5F)
                    };
                    dlg.Controls.Add(txtIssue);

                    Label lblNotesHeader = new Label
                    {
                        Text = "📄 Destek Uzmanı Çözüm Notu:",
                        Location = new Point(20, 228),
                        Size = new Size(445, 18),
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(107, 118, 132)
                    };
                    dlg.Controls.Add(lblNotesHeader);

                    TextBox txtNotes = new TextBox
                    {
                        Text = string.IsNullOrEmpty(ticket.Notes) ? "Henüz uzman çözüm notu eklenmedi." : ticket.Notes,
                        Location = new Point(20, 249),
                        Size = new Size(445, 75),
                        Multiline = true,
                        ReadOnly = true,
                        ScrollBars = ScrollBars.Vertical,
                        BackColor = Color.FromArgb(245, 245, 246),
                        ForeColor = Color.FromArgb(74, 90, 120),
                        BorderStyle = BorderStyle.FixedSingle,
                        Font = new Font("Segoe UI", 9.5F)
                    };
                    dlg.Controls.Add(txtNotes);

                    Button btnClose = new Button
                    {
                        Text = "Kapat",
                        Location = new Point(185, 345),
                        Size = new Size(130, 36),
                        BackColor = Color.FromArgb(58, 62, 70),
                        ForeColor = Color.White,
                        FlatStyle = FlatStyle.Flat,
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                        Cursor = Cursors.Hand
                    };
                    btnClose.Click += (s, e) => dlg.Close();
                    dlg.Controls.Add(btnClose);

                    dlg.ShowDialog(this);
                }
            }
        }
    }

    public class RemoteOverlayBannerForm : Form
    {
        private Point _dragCursorPoint;
        private Point _dragFormPoint;
        private bool _isDragging = false;
        private System.Windows.Forms.Timer? _autoDismissTimer;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE: Never steal focus from Windows Taskbar or Desktop
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW: Hide from Taskbar / Alt-Tab
                cp.ExStyle |= 0x00000008; // WS_EX_TOPMOST: Stay on top quietly
                return cp;
            }
        }

        public RemoteOverlayBannerForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.Size = new Size(420, 48);
            this.BackColor = Color.FromArgb(10, 11, 16); // Orijinal koyu tema (bu bildirim kutusu hariç tutuldu)
            this.Padding = new Padding(1);

            // Position in bottom-right corner of primary working area
            var wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(wa.Right - 440, wa.Bottom - 65);

            Panel pnlMain = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 11, 16),
                BorderStyle = BorderStyle.None
            };
            pnlMain.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(0, 229, 255), 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, pnlMain.Width - 1, pnlMain.Height - 1);
                }
            };

            Label lblIcon = new Label
            {
                Text = "⚡",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 204, 113),
                AutoSize = true,
                Location = new Point(10, 13),
                Cursor = Cursors.SizeAll
            };

            Label lblTitle = new Label
            {
                Text = "BigLineconnect • Uzaktan Bağlantı Aktif",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(32, 6),
                Cursor = Cursors.SizeAll
            };

            Label lblSub = new Label
            {
                Text = "Uzman bilgisayarınıza bağlandı • Oturum aktif",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = Color.FromArgb(0, 229, 255),
                AutoSize = true,
                Location = new Point(32, 26),
                Cursor = Cursors.SizeAll
            };

            Label lblOpenApp = new Label
            {
                Text = "Göster",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 229, 255),
                BackColor = Color.FromArgb(30, 35, 45),
                Location = new Point(320, 10),
                Size = new Size(60, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblOpenApp.MouseEnter += (s, e) => { lblOpenApp.BackColor = Color.FromArgb(0, 229, 255); lblOpenApp.ForeColor = Color.Black; };
            lblOpenApp.MouseLeave += (s, e) => { lblOpenApp.BackColor = Color.FromArgb(30, 35, 45); lblOpenApp.ForeColor = Color.FromArgb(0, 229, 255); };
            lblOpenApp.Click += (s, e) =>
            {
                MainWindow.Instance?.RestoreAppWindow();
            };

            Label lblClose = new Label
            {
                Text = "✖",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(180, 190, 200),
                Location = new Point(388, 10),
                Size = new Size(24, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblClose.MouseEnter += (s, e) => { lblClose.ForeColor = Color.FromArgb(231, 76, 60); };
            lblClose.MouseLeave += (s, e) => { lblClose.ForeColor = Color.FromArgb(180, 190, 200); };
            lblClose.Click += (s, e) =>
            {
                MainWindow.IsBannerDismissedByUser = true;
                this.Close();
            };

            pnlMain.Controls.Add(lblIcon);
            pnlMain.Controls.Add(lblTitle);
            pnlMain.Controls.Add(lblSub);
            pnlMain.Controls.Add(lblOpenApp);
            pnlMain.Controls.Add(lblClose);
            this.Controls.Add(pnlMain);

            BindDragEvents(this);
            BindDragEvents(pnlMain);
            BindDragEvents(lblIcon);
            BindDragEvents(lblTitle);
            BindDragEvents(lblSub);
        }

        private void BindDragEvents(Control ctrl)
        {
            ctrl.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    _isDragging = true;
                    _dragCursorPoint = Cursor.Position;
                    _dragFormPoint = this.Location;
                }
            };
            ctrl.MouseMove += (s, e) =>
            {
                if (_isDragging)
                {
                    Point diff = Point.Subtract(Cursor.Position, new Size(_dragCursorPoint));
                    this.Location = Point.Add(_dragFormPoint, new Size(diff));
                }
            };
            ctrl.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left) _isDragging = false;
            };
        }

        protected override bool ShowWithoutActivation => true;
    }

    public class SpecialistSetupGuideForm : Form
    {
        private TextBox txtCompanyCode;

        public SpecialistSetupGuideForm()
        {
            this.Text = "👨‍💻 Bayi & Uzman Modu Kurulum Sihirbazı";
            this.Size = new Size(500, 420);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 246);
            this.ForeColor = Color.FromArgb(38, 40, 45);

            var titleLabel = new Label
            {
                Text = "⚡ Destek Uzmanı & Bayi Yetkilendirmesi",
                Location = new Point(25, 20),
                Size = new Size(440, 30),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(74, 90, 120)
            };
            this.Controls.Add(titleLabel);

            var descLabel = new Label
            {
                Text = "Müşterilerinizin açacağı canlı destek taleplerinin (Ticket) ekranınıza düşmesi için lütfen aşağıdaki yönergeleri tamamlayınız:",
                Location = new Point(25, 52),
                Size = new Size(440, 38),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(107, 118, 132)
            };
            this.Controls.Add(descLabel);

            // Step 1 Panel: Bayi / Şirket Kodu Belirleme
            var step1Panel = new Panel
            {
                Location = new Point(25, 95),
                Size = new Size(435, 110),
                BackColor = Color.FromArgb(245, 245, 246),
                BorderStyle = BorderStyle.FixedSingle
            };

            var step1Title = new Label
            {
                Text = "📌 ADIM 1: Bayi / Şirket Kodunuzu Belirleyin",
                Location = new Point(12, 10),
                Size = new Size(410, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(191, 140, 15)
            };
            step1Panel.Controls.Add(step1Title);

            var step1Sub = new Label
            {
                Text = "Müşterileriniz destek talebi açarken bu Bayi Kodunu girecektir:",
                Location = new Point(12, 34),
                Size = new Size(410, 20),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(107, 118, 132)
            };
            step1Panel.Controls.Add(step1Sub);

            txtCompanyCode = new TextBox
            {
                Text = (string.IsNullOrWhiteSpace(LicenseSystem.CompanyCode) || LicenseSystem.CompanyCode == "BIGLINE" || LicenseSystem.CompanyCode.StartsWith("V1.") || LicenseSystem.CompanyCode.StartsWith("V2.") || LicenseSystem.CompanyCode.StartsWith("V3.") || LicenseSystem.CompanyCode.StartsWith("V4.")) ? "BAYIKODU" : LicenseSystem.CompanyCode,
                Location = new Point(12, 60),
                Size = new Size(405, 30),
                BackColor = Color.FromArgb(245, 245, 246),
                ForeColor = Color.FromArgb(74, 90, 120),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
            step1Panel.Controls.Add(txtCompanyCode);
            this.Controls.Add(step1Panel);

            // Step 2 Panel: Canlı Talepler Görsel Açıklama
            var step2Panel = new Panel
            {
                Location = new Point(25, 215),
                Size = new Size(435, 100),
                BackColor = Color.FromArgb(245, 245, 246),
                BorderStyle = BorderStyle.FixedSingle
            };

            var step2Title = new Label
            {
                Text = "🔔 ADIM 2: Talepler Anında Ekranınıza Düşsün",
                Location = new Point(12, 10),
                Size = new Size(410, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 140, 74)
            };
            step2Panel.Controls.Add(step2Title);

            var step2Desc = new Label
            {
                Text = "Bu Bayi Kodunu yazarak talep açan tüm müşterilerin çağrıları, sesli uyarı ile ekranınızdaki 🆘 Talepler sekmesine düşecek ve tek tıkla şifresiz bağlanabileceksiniz!",
                Location = new Point(12, 34),
                Size = new Size(410, 58),
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(107, 118, 132)
            };
            step2Panel.Controls.Add(step2Desc);
            this.Controls.Add(step2Panel);

            // Action Button: Save & Activate
            var btnSave = new Button
            {
                Text = "💾 Kaydet ve Uzman Ekranını Başlat 🚀",
                Location = new Point(25, 330),
                Size = new Size(435, 42),
                BackColor = Color.FromArgb(74, 90, 120),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.Click += (s, e) =>
            {
                string code = txtCompanyCode.Text.Trim();
                if (string.IsNullOrEmpty(code))
                {
                    MessageBox.Show("Lütfen bir Bayi / Firma Kodu giriniz.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                LicenseSystem.SaveCompanyCode(code, true);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };
            this.Controls.Add(btnSave);
        }
    }

        public class MySubmittedTicketsForm : Form
        {
            private MainWindow _main;
            private ListView lstTickets;
            private Button btnRefresh;
            private Button btnNewTicket;
            private Button btnCancelTicket;
            private Button btnClose;

            public MySubmittedTicketsForm(MainWindow main)
            {
                _main = main;
                this.Text = "📋 Açtığım Destek Taleplerim ve Durumları";
                this.Size = new Size(680, 460);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.FromArgb(245, 245, 246);
                this.ForeColor = Color.FromArgb(38, 40, 45);

                var lblTitle = new Label
                {
                    Text = "📋 Açtığım Destek Talepleri Geçmişi",
                    Location = new Point(20, 15),
                    Size = new Size(620, 28),
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(74, 90, 120)
                };
                this.Controls.Add(lblTitle);

                lstTickets = new ListView
                {
                    Location = new Point(20, 50),
                    Size = new Size(625, 300),
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = false,
                    BackColor = Color.FromArgb(245, 245, 246),
                    ForeColor = Color.FromArgb(107, 118, 132),
                    Font = new Font("Segoe UI", 9.5F),
                    BorderStyle = BorderStyle.FixedSingle
                };
                lstTickets.Columns.Add("Tarih / Saat", 130);
                lstTickets.Columns.Add("Sorun / Açıklama", 210);
                lstTickets.Columns.Add("Durum", 130);
                lstTickets.Columns.Add("Uzman Çözüm Notu", 140);
                this.Controls.Add(lstTickets);

                btnRefresh = new Button
                {
                    Text = "🔄 Yenile",
                    Location = new Point(20, 365),
                    Size = new Size(110, 35),
                    BackColor = Color.FromArgb(58, 62, 70),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnRefresh.Click += (s, e) => LoadAndRefreshTickets();
                this.Controls.Add(btnRefresh);

                btnNewTicket = new Button
                {
                    Text = "➕ Yeni Talep Aç",
                    Location = new Point(140, 365),
                    Size = new Size(130, 35),
                    BackColor = Color.FromArgb(22, 140, 74),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnNewTicket.Click += (s, e) =>
                {
                    this.Close();
                    _main.RequestSupport_Click(s, e);
                };
                this.Controls.Add(btnNewTicket);

                btnCancelTicket = new Button
                {
                    Text = "❌ Talebi İptal Et",
                    Location = new Point(280, 365),
                    Size = new Size(130, 35),
                    BackColor = Color.FromArgb(196, 57, 43),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnCancelTicket.Click += (s, e) => CancelSelectedTicket();
                this.Controls.Add(btnCancelTicket);

                btnClose = new Button
                {
                    Text = "Kapat",
                    Location = new Point(535, 365),
                    Size = new Size(110, 35),
                    BackColor = Color.FromArgb(231, 232, 234),
                    ForeColor = Color.FromArgb(38, 40, 45),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnClose.Click += (s, e) => this.Close();
                this.Controls.Add(btnClose);

                this.Load += (s, e) => LoadAndRefreshTickets();
            }

            private void LoadAndRefreshTickets()
            {
                lstTickets.Items.Clear();
                var localTickets = MainWindow.LoadLocalSubmittedTickets();
                string myHostId = Program.CurrentHostId != null ? Program.CurrentHostId.Replace(" ", "").Trim() : "";

                Task.Run(async () =>
                {
                    List<MainWindow.LocalSubmittedTicket> serverHistoryList = new();
                    try
                    {
                        string serverUrl = _main._actualRelayUrl;
                        string httpUrl = serverUrl.Replace("ws://", "http://").Replace("wss://", "https://").Replace("/register-host", $"/api/support/history/list?hostId={myHostId}");
                        using var client = new System.Net.Http.HttpClient();
                        var res = await client.GetAsync(httpUrl);
                        if (res.IsSuccessStatusCode)
                        {
                            string json = await res.Content.ReadAsStringAsync();
                            using var doc = System.Text.Json.JsonDocument.Parse(json);
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                string token = elem.TryGetProperty("token", out var pT) ? pT.GetString() ?? "" : (elem.TryGetProperty("Token", out var pTb) ? pTb.GetString() ?? "" : "");
                                string issue = elem.TryGetProperty("issue", out var p1) ? p1.GetString() ?? "" : (elem.TryGetProperty("Issue", out var p1b) ? p1b.GetString() ?? "" : "");
                                string status = elem.TryGetProperty("status", out var p2) ? p2.GetString() ?? "" : (elem.TryGetProperty("Status", out var p2b) ? p2b.GetString() ?? "" : "");
                                string notes = elem.TryGetProperty("notes", out var p3) ? p3.GetString() ?? "" : (elem.TryGetProperty("Notes", out var p3b) ? p3b.GetString() ?? "" : "");
                                
                                DateTime dt = DateTime.Now;
                                if (elem.TryGetProperty("createdAt", out var pC) && DateTime.TryParse(pC.GetString(), out var dtCreate))
                                    dt = dtCreate;
                                else if (elem.TryGetProperty("CreatedAt", out var pCb) && DateTime.TryParse(pCb.GetString(), out var dtCreateb))
                                    dt = dtCreateb;
                                else if (elem.TryGetProperty("resolvedAt", out var p4) && DateTime.TryParse(p4.GetString(), out var dtVal))
                                    dt = dtVal;

                                serverHistoryList.Add(new MainWindow.LocalSubmittedTicket
                                {
                                    Token = token,
                                    Issue = issue,
                                    Status = status,
                                    Notes = notes,
                                    CreatedAt = dt
                                });
                            }
                        }
                    }
                    catch { }

                    this.Invoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        if (this.IsDisposed) return;
                        var combinedList = new List<MainWindow.LocalSubmittedTicket>(localTickets);

                        foreach (var sh in serverHistoryList)
                        {
                            var existing = combinedList.FirstOrDefault(t => 
                                (!string.IsNullOrEmpty(sh.Token) && !string.IsNullOrEmpty(t.Token) && t.Token.Trim() == sh.Token.Trim()) ||
                                (t.Issue.Trim() == sh.Issue.Trim() && Math.Abs((t.CreatedAt - sh.CreatedAt).TotalMinutes) < 3)
                            );

                            if (existing != null)
                            {
                                existing.Status = sh.Status;
                                existing.Notes = sh.Notes;
                                if (sh.CreatedAt != default && (existing.CreatedAt == default || Math.Abs((existing.CreatedAt - sh.CreatedAt).TotalHours) > 24))
                                {
                                    existing.CreatedAt = sh.CreatedAt;
                                }
                            }
                            else
                            {
                                combinedList.Add(sh);
                            }
                        }

                        int rowIndex = 0;
                        foreach (var t in combinedList.OrderByDescending(x => x.CreatedAt))
                        {
                            string timeStr = t.CreatedAt != default ? t.CreatedAt.ToString("dd.MM.yyyy HH:mm") : "---";
                            var item = new ListViewItem(timeStr);
                            item.SubItems.Add(t.Issue);

                            string statusText = t.Status;
                            if (statusText.Contains("Çözüldü"))
                            {
                                item.SubItems.Add("✅ Çözüldü");
                            }
                            else if (statusText.Contains("Çözülmedi"))
                            {
                                item.SubItems.Add("❌ Çözülmedi");
                            }
                            else if (statusText.Contains("Bağlandı") || statusText.Contains("İşlemde"))
                            {
                                item.SubItems.Add("⚡ Uzman İşlemde");
                            }
                            else
                            {
                                item.SubItems.Add(string.IsNullOrEmpty(statusText) ? "⏳ Sırada Bekliyor" : statusText);
                            }

                            item.SubItems.Add(string.IsNullOrEmpty(t.Notes) ? "—" : t.Notes);
                            item.Tag = t;

                            // Alternating soft background for zero eye strain
                            Color rowBg = (rowIndex % 2 == 0) ? Color.FromArgb(245, 245, 246) : Color.FromArgb(245, 245, 246);
                            item.BackColor = rowBg;
                            item.UseItemStyleForSubItems = false;

                            item.SubItems[0].BackColor = rowBg;
                            item.SubItems[0].ForeColor = Color.FromArgb(107, 118, 132);

                            item.SubItems[1].BackColor = rowBg;
                            item.SubItems[1].ForeColor = Color.FromArgb(107, 118, 132);

                            item.SubItems[2].BackColor = rowBg;
                            if (statusText.Contains("Çözüldü"))
                                item.SubItems[2].ForeColor = Color.FromArgb(22, 140, 74);
                            else if (statusText.Contains("Çözülmedi"))
                                item.SubItems[2].ForeColor = Color.FromArgb(196, 57, 43);
                            else if (statusText.Contains("Bağlandı") || statusText.Contains("İşlemde"))
                                item.SubItems[2].ForeColor = Color.FromArgb(74, 90, 120);
                            else
                                item.SubItems[2].ForeColor = Color.FromArgb(191, 140, 15);

                            item.SubItems[3].BackColor = rowBg;
                            item.SubItems[3].ForeColor = Color.FromArgb(107, 118, 132);

                            lstTickets.Items.Add(item);
                            rowIndex++;
                        }
                    });
                });
            }

            private void CancelSelectedTicket()
            {
                if (lstTickets.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Lütfen iptal etmek istediğiniz talebi seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var item = lstTickets.SelectedItems[0];
                if (item.Tag is MainWindow.LocalSubmittedTicket t)
                {
                    var res = MessageBox.Show($"'({t.Issue})' başlıklı talebinizi iptal etmek istiyor musunuz?", "Talep İptali", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        var localList = MainWindow.LoadLocalSubmittedTickets();
                        localList.RemoveAll(x => x.Token == t.Token || x.Issue == t.Issue);
                        string json = System.Text.Json.JsonSerializer.Serialize(localList, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(MainWindow.GetLocalSubmittedTicketsFilePath(), json);
                        LoadAndRefreshTickets();
                        MessageBox.Show("Talebiniz iptal edildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        public static class FreeLimitsEngine
        {
            public static bool IsProUser()
            {
                try
                {
                    if (LicenseSystem.IsSpecialistMode || LicenseSystem.IsLicenseActive) return true;
                    string proFile = ConfigHelper.GetConfigPath("pro_license.key");
                    if (File.Exists(proFile))
                    {
                        string key = File.ReadAllText(proFile).Trim();
                        if (!string.IsNullOrEmpty(key) && key.Length >= 6) return true;
                    }
                }
                catch { }
                return false;
            }

            public static int GetActiveViewerCount()
            {
                int count = 0;
                try
                {
                    foreach (Form f in Application.OpenForms)
                    {
                        if (f is ViewerForm && !f.IsDisposed) count++;
                    }
                }
                catch { }
                return count;
            }

            public static bool CheckCanInitiateConnection(string targetId, out string blockReason)
            {
                blockReason = "";
                if (IsProUser()) return true;

                // Rule 1: Max 1 simultaneous active connection
                if (GetActiveViewerCount() >= 1)
                {
                    blockReason = "⚠️ Ücretsiz kullanımda aynı anda yalnızca 1 bilgisayara bağlanabilirsiniz.\r\nSınırsız ve çoklu sekme bağlantısı için PRO Lisansa yükseltin.";
                    return false;
                }

                // Rule 2: Check daily quota (max 30 mins = 1800s total per day)
                string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
                string dailyFile = ConfigHelper.GetConfigPath("daily_free_usage.json");
                long dailyUsedSeconds = 0;
                try
                {
                    if (File.Exists(dailyFile))
                    {
                        string json = File.ReadAllText(dailyFile);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("date", out var dProp) && dProp.GetString() == todayStr)
                        {
                            if (doc.RootElement.TryGetProperty("seconds", out var sProp))
                            {
                                dailyUsedSeconds = sProp.GetInt64();
                            }
                        }
                    }
                }
                catch { }

                if (dailyUsedSeconds >= 1800) // 30 minutes total daily quota
                {
                    blockReason = "⚠️ Gün içindeki ücretsiz bağlantı sürenizi (30 Dakika) doldurdunuz.\r\nSınırsız ve kesintisiz bağlantı için PRO Lisansa yükseltin.";
                    return false;
                }

                // Rule 3: Check max 3 unique target IDs in 7 days
                string cleanId = targetId != null ? targetId.Replace(" ", "").Trim() : "";
                string idsFile = ConfigHelper.GetConfigPath("free_target_ids.json");
                var idList = new List<string>();
                try
                {
                    if (File.Exists(idsFile))
                    {
                        string json = File.ReadAllText(idsFile);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var elem in doc.RootElement.EnumerateArray())
                            {
                                string id = elem.GetString() ?? "";
                                if (!string.IsNullOrEmpty(id) && !idList.Contains(id)) idList.Add(id);
                            }
                        }
                    }
                }
                catch { }

                if (!idList.Contains(cleanId) && idList.Count >= 3)
                {
                    blockReason = "⚠️ Ücretsiz kullanımda 7 gün içinde en fazla 3 farklı bilgisayara bağlanabilirsiniz.\r\nFarklı bilgisayarlara sınırsız destek vermek için PRO Lisansa yükseltin.";
                    return false;
                }

                // Record target ID
                if (!idList.Contains(cleanId))
                {
                    idList.Add(cleanId);
                    try
                    {
                        string jsonOut = JsonSerializer.Serialize(idList);
                        File.WriteAllText(idsFile, jsonOut);
                    }
                    catch { }
                }

                return true;
            }

            public static void AddDailyUsageSeconds(long seconds)
            {
                if (IsProUser()) return;
                string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
                string dailyFile = ConfigHelper.GetConfigPath("daily_free_usage.json");
                long dailyUsedSeconds = 0;
                try
                {
                    if (File.Exists(dailyFile))
                    {
                        string json = File.ReadAllText(dailyFile);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("date", out var dProp) && dProp.GetString() == todayStr)
                        {
                            if (doc.RootElement.TryGetProperty("seconds", out var sProp))
                            {
                                dailyUsedSeconds = sProp.GetInt64();
                            }
                        }
                    }
                }
                catch { }

                dailyUsedSeconds += seconds;
                try
                {
                    var data = new { date = todayStr, seconds = dailyUsedSeconds };
                    File.WriteAllText(dailyFile, JsonSerializer.Serialize(data));
                }
                catch { }
            }
        }

        public class ProLicenseDialog : Form
        {
            public ProLicenseDialog(string message = "")
            {
                this.Text = "BigLineconnect PRO Lisans Aktivasyonu 🔑";
                this.Size = new Size(550, 440);
                this.StartPosition = FormStartPosition.CenterParent;
                this.FormBorderStyle = FormBorderStyle.FixedDialog;
                this.MaximizeBox = false;
                this.MinimizeBox = false;
                this.BackColor = Color.FromArgb(248, 249, 250);
                this.ForeColor = Color.FromArgb(33, 37, 41);
                this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

                var cardPanel = new Panel
                {
                    Location = new Point(20, 20),
                    Size = new Size(494, 340),
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                this.Controls.Add(cardPanel);

                var titleLbl = new Label
                {
                    Text = "⚡ PRO Lisans Aktivasyonu",
                    Location = new Point(20, 18),
                    Size = new Size(454, 30),
                    Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(15, 23, 42)
                };
                cardPanel.Controls.Add(titleLbl);

                var msgLbl = new Label
                {
                    Text = string.IsNullOrEmpty(message) 
                        ? "Gün içindeki ücretsiz bağlantı sürenizi doldurdunuz.\r\nSınırsız, kesintisiz ve çoklu bağlantı için PRO Lisansa yükseltin." 
                        : message,
                    Location = new Point(20, 56),
                    Size = new Size(454, 55),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(71, 85, 105)
                };
                cardPanel.Controls.Add(msgLbl);

                // Price Badge
                var priceBadge = new Label
                {
                    Text = "  🏷️ Aylık 149 TL'den Başlayan Fiyatlarla  ",
                    Location = new Point(20, 115),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                    BackColor = Color.FromArgb(220, 252, 231),
                    ForeColor = Color.FromArgb(22, 101, 52),
                    Padding = new Padding(8, 6, 8, 6)
                };
                cardPanel.Controls.Add(priceBadge);

                // Features list
                var featLbl = new Label
                {
                    Text = "✔ 10 Dakika Oturum Zaman Aşımı Yok\r\n✔ Günlük Kota ve Cihaz Sayısı Sınırı Yok (Sınırsız)\r\n✔ Aynı Anda Çoklu Bilgisayar Bağlantısı (Sekme Modu)\r\n✔ 7/24 Kesintisiz Yüksek Hızlı Donanım İvmesi",
                    Location = new Point(20, 165),
                    Size = new Size(454, 90),
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(30, 41, 59)
                };
                cardPanel.Controls.Add(featLbl);

                // Buy License Web Button
                var btnBuy = new Button
                {
                    Text = "💳 Aylık 149 TL ile PRO Lisans Al (Web Sayfasına Git)",
                    Location = new Point(20, 275),
                    Size = new Size(454, 42),
                    BackColor = Color.FromArgb(16, 185, 129),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnBuy.FlatAppearance.BorderSize = 0;
                btnBuy.Click += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://bigus.com.tr",
                            UseShellExecute = true
                        });
                    }
                    catch { }
                };
                cardPanel.Controls.Add(btnBuy);

                // Enter License Key Button
                var btnEnterKey = new Button
                {
                    Text = "🔑 Lisans Anahtarı Etkinleştir",
                    Location = new Point(20, 368),
                    Size = new Size(240, 32),
                    BackColor = Color.FromArgb(15, 23, 42),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                btnEnterKey.FlatAppearance.BorderSize = 0;
                btnEnterKey.Click += (s, e) => PromptAndActivateKey();
                this.Controls.Add(btnEnterKey);

                // Close Button
                var btnClose = new Button
                {
                    Text = "Kapat",
                    Location = new Point(414, 368),
                    Size = new Size(100, 32),
                    BackColor = Color.FromArgb(203, 213, 225),
                    ForeColor = Color.FromArgb(51, 65, 85),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    DialogResult = DialogResult.OK
                };
                btnClose.FlatAppearance.BorderSize = 0;
                this.Controls.Add(btnClose);
                this.AcceptButton = btnClose;
            }

            private void PromptAndActivateKey()
            {
                string key = Microsoft.VisualBasic.Interaction.InputBox("Lütfen Bigus Bilişim PRO Lisans Anahtarınızı Giriniz:", "PRO Lisans Aktivasyonu", "");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    try
                    {
                        string proFile = ConfigHelper.GetConfigPath("pro_license.key");
                        File.WriteAllText(proFile, key.Trim());
                        MessageBox.Show("🎉 PRO Lisansınız başarıyla etkinleştirildi! Tüm sınırlamalar kaldırıldı.", "Lisans Etkinleştirildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lisans kaydedilirken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }

    public class InfoForm : Form
    {
        public InfoForm()
        {
            this.Text = "BigLineconnect - Kurumsal Sürüm Bilgileri ℹ️";
            this.Size = new Size(540, 485);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(248, 249, 250);
            this.ForeColor = Color.FromArgb(33, 37, 41);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            // Main Corporate Card Panel
            var cardPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(484, 380),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(cardPanel);

            // Header Icon / Title Box
            var headerIcon = new PictureBox
            {
                Location = new Point(20, 20),
                Size = new Size(48, 48),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            try
            {
                if (BigLineconnect.MainWindow.Instance != null && BigLineconnect.MainWindow.Instance._logoBox != null && BigLineconnect.MainWindow.Instance._logoBox.Image != null)
                {
                    headerIcon.Image = BigLineconnect.MainWindow.Instance._logoBox.Image;
                }
            }
            catch { }
            cardPanel.Controls.Add(headerIcon);

            var titleLbl = new Label
            {
                Text = "BigLineconnect Enterprise",
                Location = new Point(78, 18),
                Size = new Size(380, 28),
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            cardPanel.Controls.Add(titleLbl);

            var versionBadge = new Label
            {
                Text = "v3.61.0 (60 FPS Ultra Fast & LAN Direct)",
                Location = new Point(80, 46),
                Size = new Size(380, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(14, 165, 233)
            };
            cardPanel.Controls.Add(versionBadge);

            // Divider Line
            var div1 = new Panel
            {
                Location = new Point(20, 80),
                Size = new Size(444, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };
            cardPanel.Controls.Add(div1);

            // License Status Badge
            var badgePill = new Label
            {
                Text = "  🟢 DİJİTAL PRO LİSANS (Kurumsal Lisanslı)  ",
                Location = new Point(20, 95),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(220, 252, 231),
                ForeColor = Color.FromArgb(22, 101, 52),
                Padding = new Padding(6, 4, 6, 4)
            };
            cardPanel.Controls.Add(badgePill);

            // Information Grid Panel
            var infoGrid = new TableLayoutPanel
            {
                Location = new Point(20, 135),
                Size = new Size(444, 170),
                ColumnCount = 2,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            infoGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            string[,] details = {
                { "Geliştirici:", "Bigus Bilişim Ltd.Şti." },
                { "Şirket Unvanı:", "Bigus Bilişim Geliştirme ve Uygulama Sistemleri Ltd.Şti." },
                { "E-Posta:", "my@bigus.com.tr" },
                { "Web Adresi:", "https://bigus.com.tr" },
                { "Sertifika:", "Certum EV Digital Signed Code Signing" }
            };

            for (int r = 0; r < 5; r++)
            {
                var lblKey = new Label
                {
                    Text = details[r, 0],
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var lblVal = new Label
                {
                    Text = details[r, 1],
                    Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(15, 23, 42),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };
                infoGrid.Controls.Add(lblKey, 0, r);
                infoGrid.Controls.Add(lblVal, 1, r);
            }
            cardPanel.Controls.Add(infoGrid);

            // Divider Line 2
            var div2 = new Panel
            {
                Location = new Point(20, 320),
                Size = new Size(444, 1),
                BackColor = Color.FromArgb(226, 232, 240)
            };
            cardPanel.Controls.Add(div2);

            // Footer Copyright
            var copyrightLbl = new Label
            {
                Text = "Tüm hakları saklıdır. © 2026 Bigus Bilişim Ltd.Şti.",
                Location = new Point(20, 335),
                Size = new Size(444, 25),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                TextAlign = ContentAlignment.MiddleCenter
            };
            cardPanel.Controls.Add(copyrightLbl);

            // Close Button
            var btnClose = new Button
            {
                Text = "Tamam",
                Location = new Point(404, 408),
                Size = new Size(100, 32),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                DialogResult = DialogResult.OK
            };
            btnClose.FlatAppearance.BorderSize = 0;
            this.Controls.Add(btnClose);
            this.AcceptButton = btnClose;
        }
    }
