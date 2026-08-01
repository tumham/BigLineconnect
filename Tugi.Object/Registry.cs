using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tugi.Object
{
    public class Registry
    {

        public static string Oku(string klasor1, string klasor2)
        {
           Tugi.Dot.achead a = new Tugi.Dot.achead();
            try
            {
                return a.Rd(klasor1, klasor2);
            }
            catch (Exception)
            {
                return "xxx";
            }
        }

        public static void Yaz(string kod, string rn, string klasor1, string klasor2)
        {
            Tugi.Dot.achead a = new Tugi.Dot.achead();
            a.Wr(kod, rn, klasor1, klasor2);
        }

        public static bool Kontrol(string key,string gen)
        {
            Tugi.Dot.News n = new Tugi.Dot.News();
            return n.Gt(key, gen);
        }

        public static bool Kontrol(string key, string gen,string url)
        {
            Tugi.Dot.News n = new Tugi.Dot.News();
            return n.Gt(key, gen,url);
        }

        public static string AktivKod(string kod, string key)
        {
            Tugi.Dot.News n = new Tugi.Dot.News();
            return n.KodUret(kod, key);
        }
    }
}
