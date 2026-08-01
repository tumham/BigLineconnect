using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Bigus.Aktarici.WinApp
{
    public partial class frm_MesajPencere : Form
    {

        private string _mesaj;

        public string Mesaj
        {
            get { return _mesaj; }
            set { _mesaj = value; }
        }

        private string _dugme1= "IPTAL";

        public string Dugme1
        {
            get { return _dugme1; }
            set { _dugme1 = value; }
        }

        private string _dugme2 ="OK";

        public string Dugme2
        {
            get { return _dugme2; }
            set { _dugme2 = value; }
        }

        private Int32 _sonuc;

        public Int32 Sonuc
        {
            get { return _sonuc; }
            set { _sonuc = value; }
        }


        public frm_MesajPencere()
        {
            InitializeComponent();
        }

        private void frm_MesajPencere_Load(object sender, EventArgs e)
        {
            lblmesaj.Text = _mesaj;
            buton1.Text = _dugme1;
            buton2.Text = _dugme2;
        }

        private void buton1_Click(object sender, EventArgs e)
        {
            _sonuc = 0;
            this.Close();
        }

        private void buton2_Click(object sender, EventArgs e)
        {
            _sonuc = 1;
            this.Close();
        }

        private void lblmesaj_EditValueChanged(object sender, EventArgs e)
        {

        }
    }
}