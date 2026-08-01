using System;
using System.Data;
using System.Data.SqlClient;

class Program
{
    static void Main()
    {
        string connStr = "Server=localhost;Database=MikroDesktop_SPMEDIKAL;Integrated Security=True;";
        using (SqlConnection conn = new SqlConnection(connStr))
        {
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT TOP 1 * FROM BEDEN_HAREKETLERI", conn);
            SqlDataReader reader = cmd.ExecuteReader();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                Console.WriteLine(reader.GetName(i));
            }
        }
    }
}