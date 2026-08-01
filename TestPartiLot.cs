using System;
using System.Data;
using System.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Data Source=localhost;Initial Catalog=MikroDesktop_MEST;Integrated Security=True;";
        string BarkodKod = "8690188371737";
        
        using (SqlConnection con = new SqlConnection(connStr))
        {
            SqlCommand comm = new SqlCommand("SELECT bar_partikodu, bar_lotno FROM dbo.BARKOD_TANIMLARI WHERE bar_kodu='" + BarkodKod + "'", con);
            try
            {
                con.Open();
                SqlDataReader rdr = comm.ExecuteReader();
                if (rdr.Read())
                {
                    string PartiKodu = rdr["bar_partikodu"] == DBNull.Value ? "" : rdr["bar_partikodu"].ToString();
                    int LNo = rdr["bar_lotno"] == DBNull.Value ? 0 : Convert.ToInt32(rdr["bar_lotno"]);
                    Console.WriteLine("PartiKodu: " + PartiKodu + ", LNo: " + LNo);
                    if (PartiKodu != "" || LNo > 0)
                    {
                        Console.WriteLine("Return True");
                    }
                    else
                    {
                        Console.WriteLine("Return False (Empty)");
                    }
                }
                else
                {
                    Console.WriteLine("Return False (Not Found)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }
        }
    }
}