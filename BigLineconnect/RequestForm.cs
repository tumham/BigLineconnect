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
        private Label _lblMessage;
        private Label _lblCountdown;
        private Button _btnAccept;
        private Button _btnReject;

        public RequestForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Uzaktan Bağlantı İsteği";
            this.Size = new Size(400, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 28, 35);
            this.ForeColor = Color.White;

            _lblTitle = new Label
            {
                Text = "Giriş İsteği Algılandı",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 15),
                Size = new Size(360, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(241, 196, 15) // Gold accent
            };

            _lblMessage = new Label
            {
                Text = "Uzak bir istemci bilgisayarınıza bağlanmak istiyor.\nEkranınızı izleme ve kontrol etme izni veriyor musunuz?",
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(20, 50),
                Size = new Size(360, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            _lblCountdown = new Label
            {
                Text = "Otomatik reddedilmesine 30 saniye kaldı...",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Location = new Point(20, 105),
                Size = new Size(360, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(231, 76, 60)
            };

            _btnAccept = new Button
            {
                Text = "Kabul Et (Accept)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 36),
                Location = new Point(30, 130),
                Cursor = Cursors.Hand
            };
            _btnAccept.FlatAppearance.BorderSize = 0;
            _btnAccept.Click += (s, e) => {
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            _btnReject = new Button
            {
                Text = "Reddet (Reject)",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 36),
                Location = new Point(210, 130),
                Cursor = Cursors.Hand
            };
            _btnReject.FlatAppearance.BorderSize = 0;
            _btnReject.Click += (s, e) => {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(_lblTitle);
            this.Controls.Add(_lblMessage);
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
