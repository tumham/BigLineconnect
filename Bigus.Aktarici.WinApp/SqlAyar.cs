using System;
using System.Xml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;

using System.IO;

namespace Bigus.Aktarici.WinApp
{
    public partial class SqlAyar : DevExpress.XtraEditors.XtraForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        public SqlAyar()
        {
            // Required for Windows Form Designer support
            InitializeComponent();
           

            // TODO: Add any constructor code after InitializeComponent call
        }

        public string FilePath { get; set; }

        public string Baslik { get; set; }


        void InitData()
        {
            string security;
            byte sec;
            DataSet ds = new DataSet();
            if (File.Exists(FilePath) == false)
            {
                DosyaOlustur();
            }
                 ds.ReadXml(FilePath);
                serv.Text = ds.Tables[0].Rows[0][0].ToString();
                textEdit1.Text = ds.Tables[0].Rows[0][1].ToString();
                kul.Text = ds.Tables[0].Rows[0][2].ToString();
                pas.Text = ds.Tables[0].Rows[0][3].ToString();
                security = ds.Tables[0].Rows[0][4].ToString();

                if (security != "")
                {
                    sec = Convert.ToByte(security);

                    if (sec == 1)
                        WindowsA.Checked = true;
                    else
                        SqlA.Checked = true;
                }

                BaglantiCumlesiOlustur();
          
        }

        void DosyaOlustur()
        {
            XmlTextWriter textWriter = new XmlTextWriter("Settings.xml",System.Text.Encoding.GetEncoding(1254));
            textWriter.Formatting=Formatting.Indented;
            textWriter.WriteStartDocument(false);
            textWriter.WriteStartElement("ConnSettings");
            textWriter.WriteStartElement("Settings");
            textWriter.WriteElementString("Server", "");
            textWriter.WriteElementString("DataBase","");
            textWriter.WriteElementString("User", "");
            textWriter.WriteElementString("Password", "");
            textWriter.WriteElementString("Security", "");

            textWriter.Flush();
            textWriter.Close();
        }

        private void WindowsA_CheckedChanged(object sender, EventArgs e)
        {
            kul.Enabled = false;
            pas.Enabled = false;
        }

        private void SqlA_CheckedChanged(object sender, EventArgs e)
        {
            kul.Enabled = true;
            pas.Enabled = true;
        }

        private void smpkaydet_Click(object sender, EventArgs e)
        {
            if (textEdit1.Text == "")
            {
                XtraMessageBox.Show("DATABASE ALANINI GÝRMELÝSÝNÝZ");
            }

            if (SqlA.Checked == true && kul.Text == "")
            {
                XtraMessageBox.Show("KULLANICI ADINI TANIMLAMASINIZ");
            }

            byte security;
            string _kul;
            string _sifre;
            if (SqlA.Checked == true)
            {
                security=0;
                textEdit2.Text = "Server=" +  serv.Text + ";initial catalog=" +  textEdit1.Text + ";User ID=" +  kul.Text + ";Password=" + pas.Text  + ";Min Pool Size=2";
                _kul = kul.Text;
                _sifre = pas.Text;
            }
            else
            {
                security = 1;
                textEdit2.Text = "Data Source=" + serv.Text + 
                    ";Initial Catalog=" + textEdit1.Text + 
                    ";Integrated Security=SSPI";
                _kul = "";
                _sifre = "";
            }

            DataSet ds_firmbilgi = new DataSet();
            ds_firmbilgi.ReadXml(FilePath);

            ds_firmbilgi.Tables[0].Rows[0][0] = serv.Text;
            ds_firmbilgi.Tables[0].Rows[0][1] = textEdit1.Text;
            ds_firmbilgi.Tables[0].Rows[0][2] = _kul;
            ds_firmbilgi.Tables[0].Rows[0][3] = _sifre;
            ds_firmbilgi.Tables[0].Rows[0][4] = security.ToString();

            ds_firmbilgi.WriteXml(FilePath);



        }

        void BaglantiCumlesiOlustur()
        {
            byte security;
            string _kul;
            string _sifre;
            if (SqlA.Checked == true)
            {
                security = 0;
                textEdit2.Text = "Server=" + serv.Text + ";initial catalog=" + textEdit1.Text + ";User ID=" + kul.Text + ";Password=" + pas.Text + ";Min Pool Size=2";
                _kul = kul.Text;
                _sifre = pas.Text;
            }
            else
            {
                security = 1;
                textEdit2.Text = "Data Source=" + serv.Text +
                    ";Initial Catalog=" + textEdit1.Text +
                    ";Integrated Security=SSPI";
                _kul = "";
                _sifre = "";
            }

        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            //if (textEdit2.Text == "")
            //{
            //    XtraMessageBox.Show("BAÐLANTI CÜMLESÝNÝ TANIMLAMALISINIZ");
            //    return;
            //}
            //try
            //{
            //    AyarFacade.ConnectionStringYaz(textEdit2.Text);
            //    this.Close();
            //}
            //catch (Exception ex)
            //{

            //    throw ex;
            //}


        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
          
            if (Tools.TextConnection(textEdit2.Text) == true)
                XtraMessageBox.Show("BAÐLANTI BAÞARILI");
            else
                XtraMessageBox.Show("BAÐLANTI BAÞARISIZ");
        }

        private void simpleButton3_Click(object sender, EventArgs e)
        {
            //Tugi.Sql.SqlDuzenleyici frm = new Tugi.Sql.SqlDuzenleyici();
            //frm.ShowDialog();
        }

        private void serv_EditValueChanged(object sender, EventArgs e)
        {

        }

        private void simpleButton1_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void SqlAyar_Load(object sender, EventArgs e)
        {
            grb_baslik.Text = Baslik;
            InitData();
        }

        private void simpleButton4_Click(object sender, EventArgs e)
        {
            Application.ExitThread();
            Application.Exit();

        }
    }
}