using System;
using System.Drawing;
using System.Windows.Forms;

#pragma warning disable CS8618

namespace BigLineconnect
{
    public class KvkkSettingsForm : Form
    {
        private CheckBox _chkEnableKvkk;
        private RadioButton _rbEveryTime;
        private RadioButton _rbAskOnce;
        private RadioButton _rbDisabled;
        private Button _btnResetChoice;
        private TextBox _txtDisclaimerText;
        private Label _lblStatus;
        private Button _btnSave;
        private Button _btnCancel;

        public KvkkSettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "🛡️ BigLineconnect • KVKK & Güvenlik Aydınlatma Ayarları";
            this.Size = new Size(500, 440);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(24, 26, 32);
            this.ForeColor = Color.White;
            this.TopMost = true;

            var lblTitle = new Label
            {
                Text = "🛡️ KVKK & BAĞLANTI ONAY YAPILANDIRMASI",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(15, 15),
                Size = new Size(455, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0, 229, 255)
            };

            _chkEnableKvkk = new CheckBox
            {
                Text = "Gelen bağlantılarda KVKK ve Bağlantı Onay Penceresi Göster",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(20, 50),
                Size = new Size(445, 25),
                Checked = Program.EnableKvkkDisclaimer,
                ForeColor = Color.FromArgb(46, 204, 113),
                Cursor = Cursors.Hand
            };

            // Mode Selection Group Box
            var grpMode = new GroupBox
            {
                Text = " Onay Modu ve Davranış Seçeneği ",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Location = new Point(20, 85),
                Size = new Size(445, 115),
                ForeColor = Color.FromArgb(0, 229, 255)
            };

            _rbEveryTime = new RadioButton
            {
                Text = "Her gelen uzaktan bağlantıda sor (Varsayılan)",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(15, 25),
                Size = new Size(410, 22),
                Checked = (Program.KvkkMode == 0),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };

            _rbAskOnce = new RadioButton
            {
                Text = "Bir kez sor (Kullanıcı 'Kararımı Hatırla' dediğinde sakla)",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(15, 52),
                Size = new Size(410, 22),
                Checked = (Program.KvkkMode == 1),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };

            _rbDisabled = new RadioButton
            {
                Text = "Devre dışı bırak (Otomatik Kabul Et - Uyarı gösterme)",
                Font = new Font("Segoe UI", 9f),
                Location = new Point(15, 79),
                Size = new Size(410, 22),
                Checked = (Program.KvkkMode == 2),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };

            grpMode.Controls.Add(_rbEveryTime);
            grpMode.Controls.Add(_rbAskOnce);
            grpMode.Controls.Add(_rbDisabled);

            // Reset Remembered Decision Button
            _btnResetChoice = new Button
            {
                Text = "🔄 Saklanan 'Bir Daha Sorma' Tercihini Sıfırla",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, 210),
                Size = new Size(445, 30),
                Cursor = Cursors.Hand
            };
            _btnResetChoice.FlatAppearance.BorderSize = 0;

            _lblStatus = new Label
            {
                Text = Program.KvkkAcceptedOnce ? " Durum: Kayıtlı tercih var ('Bir Daha Sorma' aktif)." : " Durum: Henüz saklanmış tercih yok.",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                Location = new Point(20, 243),
                Size = new Size(445, 20),
                ForeColor = Program.KvkkAcceptedOnce ? Color.FromArgb(241, 196, 15) : Color.FromArgb(149, 165, 166)
            };

            _btnResetChoice.Click += (s, e) =>
            {
                Program.KvkkAcceptedOnce = false;
                Program.SaveAdvancedSettings();
                _lblStatus.Text = " Durum: Saklanan tercih başarıyla sıfırlandı.";
                _lblStatus.ForeColor = Color.FromArgb(46, 204, 113);
                MessageBox.Show("Saklanan 'Bir daha sorma / Kararımı hatırla' tercihi sıfırlandı. Artık bağlantı istekleri tekrar onay penceresi açacaktır.", "Sıfırlandı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Disclaimer Text Label & Box
            var lblTextTitle = new Label
            {
                Text = "KVKK & Güvenlik Aydınlatma Metni:",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(20, 268),
                Size = new Size(445, 18),
                ForeColor = Color.FromArgb(180, 190, 200)
            };

            _txtDisclaimerText = new TextBox
            {
                Text = Program.KvkkDisclaimerText,
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(20, 288),
                Size = new Size(445, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(15, 17, 22),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Buttons
            _btnSave = new Button
            {
                Text = "💾 Kaydet",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 35),
                Location = new Point(200, 358),
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += (s, e) =>
            {
                Program.EnableKvkkDisclaimer = _chkEnableKvkk.Checked;
                if (_rbEveryTime.Checked) Program.KvkkMode = 0;
                else if (_rbAskOnce.Checked) Program.KvkkMode = 1;
                else if (_rbDisabled.Checked) Program.KvkkMode = 2;

                if (!string.IsNullOrWhiteSpace(_txtDisclaimerText.Text))
                    Program.KvkkDisclaimerText = _txtDisclaimerText.Text.Trim();

                Program.SaveAdvancedSettings();
                MessageBox.Show("KVKK & Bağlantı onay ayarları başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            _btnCancel = new Button
            {
                Text = "❌ İptal",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(108, 122, 137),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(130, 35),
                Location = new Point(335, 358),
                Cursor = Cursors.Hand
            };
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(_chkEnableKvkk);
            this.Controls.Add(grpMode);
            this.Controls.Add(_btnResetChoice);
            this.Controls.Add(_lblStatus);
            this.Controls.Add(lblTextTitle);
            this.Controls.Add(_txtDisclaimerText);
            this.Controls.Add(_btnSave);
            this.Controls.Add(_btnCancel);
        }
    }
}
