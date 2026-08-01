using System;
using System.Windows.Forms;



namespace Tugi.Object
{
    public partial class Key : Form
    {
        public Key()
        {
            InitializeComponent();
        }

        public string _key { get; set; }

        public string _urun_adi { get; set; }

        public string _akt_key { get; set; }
      
        void MakIID()
        {
            string MakID = "";

            MakID = Tugi.Dot.temin.tex().ToString();

            string m="";


            for (int a = 0; a < MakID.Length;a = a+3 )
            {
                m = m + "-" + MakID.Substring(a, 3);
            }

            lbl_mak_id.Text = m.Substring(1, m.Length - 1);

        }


        private void Key_Load(object sender, EventArgs e)
        {
            MakIID();

            lbl_urun_kod.Text = _urun_adi;

        }

        private void btn_aktive_Click(object sender, EventArgs e)
        {
            if (s1.Text == "" || s2.Text == "" || s3.Text == "" || s4.Text == "" || s5.Text == "" || s6.Text == "" || s7.Text == "")
            {
                MessageBox.Show("Eksik girdiniz.");
                return;
            }


            if (s1.Text.Length != 8 || s2.Text.Length != 6 || s3.Text.Length != 6 || s4.Text.Length != 6 || s5.Text.Length != 6 || s6.Text.Length != 6 || s7.Text.Length != 6)
            {
                MessageBox.Show("Eksik girdiniz.");
                return;
            }

            string tz = s1.Text + s2.Text + s3.Text + s4.Text + s5.Text + s6.Text + s7.Text + "3D3D";

            Tugi.Dot.News n = new Tugi.Dot.News();

            _akt_key = tz;
            if (n.Gt(tz,_key ) == true)
            {
                MessageBox.Show("ÜRÜN DOĞRULANDI");
                this.Close();
            }

            else
            {
                MessageBox.Show("GEÇERSİZ AKTİVASYON KODU");
            }
        }
    }
}
