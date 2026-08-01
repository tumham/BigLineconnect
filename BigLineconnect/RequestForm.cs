using System;
using System.Drawing;
using System.Windows.Forms;

#pragma warning disable CS8618

namespace BigLineconnect
{
    public class RequestForm : Form
    {
        private int _countdown = 30;
        private System.Windows.Forms.Timer _timer;
        private Label _lblTitle;
        private TextBox _txtKvkkText;
        private CheckBox _chkAgree;
        private CheckBox _chkRemember;
        private Label _lblCountdown;
        private Button _btnAccept;
        private Button _btnReject;

        public RequestForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "🛡️ BigLineconnect • Uzaktan Bağlantı & KVKK Onayı";
            this.Size = new Size(460, 340);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 26, 32);
            this.ForeColor = Color.White;
            this.TopMost = true;

            _lblTitle = new Label
            {
                Text = "🛡️ UZAKTAN BAĞLANTI İSTEĞİ VE YASAL UYARI",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(15, 12),
                Size = new Size(415, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0, 229, 255) // Cyan accent
            };

            // KVKK Disclaimer Text Box
            _txtKvkkText = new TextBox
            {
                Text = Program.KvkkDisclaimerText,
                Font = new Font("Segoe UI", 9f),
                Location = new Point(20, 42),
                Size = new Size(405, 100),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 17, 22),
                ForeColor = Color.FromArgb(220, 225, 230),
                BorderStyle = BorderStyle.FixedSingle
            };

            // KVKK Agreement Checkbox
            _chkAgree = new CheckBox
            {
                Text = "KVKK ve Güvenlik Aydınlatma Metnini okudum, kabul ediyorum.",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(20, 150),
                Size = new Size(405, 24),
                Checked = true,
                ForeColor = Color.FromArgb(46, 204, 113),
                Cursor = Cursors.Hand
            };
            _chkAgree.CheckedChanged += (s, e) => {
                _btnAccept.Enabled = _chkAgree.Checked;
            };

            // Remember Choice Checkbox (if mode is 1: Ask Once)
            _chkRemember = new CheckBox
            {
                Text = "Kararımı hatırla (Bu bilgisayar için bir daha sorma)",
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(20, 178),
                Size = new Size(405, 22),
                Checked = (Program.KvkkMode == 1),
                Visible = (Program.KvkkMode == 1 || Program.KvkkMode == 0),
                ForeColor = Color.FromArgb(180, 190, 200),
                Cursor = Cursors.Hand
            };

            _lblCountdown = new Label
            {
                Text = "Otomatik reddedilmesine 30 saniye kaldı...",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Location = new Point(20, 208),
                Size = new Size(405, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(231, 76, 60)
            };

            _btnAccept = new Button
            {
                Text = "✅ Kabul Et (Accept)",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(185, 40),
                Location = new Point(20, 235),
                Cursor = Cursors.Hand
            };
            _btnAccept.FlatAppearance.BorderSize = 0;
            _btnAccept.Click += (s, e) => {
                if (!_chkAgree.Checked)
                {
                    MessageBox.Show("Lütfen önce KVKK uyarısını onaylayınız.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (_chkRemember.Checked)
                {
                    Program.KvkkAcceptedOnce = true;
                    Program.SaveAdvancedSettings();
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            _btnReject = new Button
            {
                Text = "❌ Reddet (Reject)",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(185, 40),
                Location = new Point(240, 235),
                Cursor = Cursors.Hand
            };
            _btnReject.FlatAppearance.BorderSize = 0;
            _btnReject.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(_lblTitle);
            this.Controls.Add(_txtKvkkText);
            this.Controls.Add(_chkAgree);
            this.Controls.Add(_chkRemember);
            this.Controls.Add(_lblCountdown);
            this.Controls.Add(_btnAccept);
            this.Controls.Add(_btnReject);

            // Timer for auto-rejection
            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => {
                _countdown--;
                if (_countdown <= 0)
                {
                    _timer.Stop();
                    this.DialogResult = DialogResult.Cancel;
                    this.Close();
                }
                else
                {
                    _lblCountdown.Text = $"Otomatik reddedilmesine {_countdown} saniye kaldı...";
                }
            };
            
            this.Load += (s, e) => _timer.Start();
            this.FormClosing += (s, e) => _timer.Stop();
        }
    }
}
