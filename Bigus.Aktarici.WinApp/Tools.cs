
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Bigus.Aktarici.WinApp
{
    public class Tools
    {
        public static void MesajPenceresi(string mesaj)
        {

            frm_MesajPencere mes = new frm_MesajPencere();
            mes.buton1.Visible = false;
            StringBuilder strb = new StringBuilder();
            strb.AppendLine(mesaj);
            mes.Mesaj = strb.ToString();
            mes.ShowDialog();

        }

        public static int MesajPenceresi_Return(string mesaj, string b1, string b2)
        {

            frm_MesajPencere mes = new frm_MesajPencere();
            mes.buton2.Visible = true;
            mes.buton1.Visible = true;
            mes.Dugme2 = b2;
            mes.Dugme1 = b1;
            StringBuilder strb = new StringBuilder();
            strb.AppendLine(mesaj);
            mes.Mesaj = strb.ToString();
            mes.ShowDialog();

            return mes.Sonuc;

        }

        public static bool TextConnection(string ConnectionString)
        {
            SqlConnection conn = new SqlConnection(ConnectionString);
            try
            {

                conn.Open();
                conn.Close();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

    }
}
