using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Tugi.DepotManagement.UnHandledExceptionManagement
{
    public partial class frmException : Form
    {

        #region Fields
        string m_ExceptionName;
        string m_ExceptionMessage;
        object m_ExceptionSource;
        #endregion

        #region Properties
        public string ExceptionName
        {
            set { m_ExceptionName = value; }
        }
        public string ExceptionMessage
        {
            set { m_ExceptionMessage = value; }
        }
        //public object ExceptionSource
        //{
        //    set { m_ExceptionSource = value; }
        //}
        #endregion

        public frmException()
        {
            InitializeComponent();
        }

        private void frmException_Load(object sender, EventArgs e)
        {

            txDetails.Text = m_ExceptionMessage;
            lblExceptionSource.Text = m_ExceptionName;
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tmr.Enabled = false;
            this.Close();
        }

        private void tmr_Tick(object sender, EventArgs e)
        {
            Int32 sayi = Convert.ToInt32(gerisay.Text);

            sayi -= 1;
            gerisay.Text = sayi.ToString();
            if (sayi == 0)
            {
                tmr.Enabled = false;
                this.Close();
            }

        }

        private void btn_exit_Click(object sender, EventArgs e)
        {
          
            Application.Exit();
        }

        private void frmException_FormClosing(object sender, FormClosingEventArgs e)
        {
            //System.Threading.Thread.CurrentThread.Abort();
        }
    }
}