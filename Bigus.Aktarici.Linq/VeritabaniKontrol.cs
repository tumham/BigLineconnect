using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data.SqlClient;

namespace Bigus.Aktarici.Linq
{
    public class VeritabaniKontrol
    {

        public static bool TableKontrol(string Table,string conn)
        {
            using (SqlConnection cnn = new SqlConnection(conn))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Parameters.AddWithValue("@Table", Table);
                cmd.CommandText = "Select count(*) from sysobjects where name=@Table";
                cmd.Connection = cnn;
                try
                {
                    cnn.Open();
                    int sayi = (int)(cmd.ExecuteScalar());
                    return sayi > 0;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

       public static void KurulumYap()
        {
            string[] filenames = System.IO.Directory.GetFiles("Create_Script");
            string connectionString = DatabaseFacade3.ConnectionString();

            for (int i1 = 0; i1 < filenames.Length; i1++)
            {
                try
                {
                    using (StreamReader dosyaoku = File.OpenText(filenames[i1]))
                    {
                        string file = dosyaoku.ReadToEnd();
                        using (SqlConnection cnn = new SqlConnection(connectionString))
                        using (SqlCommand cmd = new SqlCommand(file, cnn))
                        {
                            cnn.Open();
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

       public static void KurulumYap(string filename)
       {
           string connectionString = DatabaseFacade3.ConnectionString();
           try
           {
               using (StreamReader dosyaoku = File.OpenText("Create_Script\\" + filename + ".sql"))
               {
                   string file = dosyaoku.ReadToEnd();
                   using (SqlConnection cnn = new SqlConnection(connectionString))
                   using (SqlCommand cmd = new SqlCommand(file, cnn))
                   {
                       cnn.Open();
                       cmd.ExecuteNonQuery();
                   }
               }
           }
           catch (Exception)
           {
               throw;
           }
       }
    }
}
