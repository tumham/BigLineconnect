using System;
using System.Windows.Forms;
using Tugi.Object;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Bigus.Aktarici.WinApp
{
    static class Program
    {
        // <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler m_Handler = new Tugi.DepotManagement.UnhandledExceptionManagement.MyExceptionHandler();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(m_Handler.OnUnhandledException);
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(m_Handler.OnThreadException);


            Application.Run(new frm_Aktarim());

            //ShowLoginForm(true);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool ShowWindow(IntPtr hWnd, int cmdShow);

        public static Process GetRunningInstance()
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);

            foreach (Process proc in processes)
            {
                if ((proc.Id != current.Id))
                {
                    return proc;
                }
            }

            return null;
        }


        static void ShowLoginForm(bool sif)
        {
            if (sif)
            {
                string gen = "B-AKT-V100";
                string k1 = "VIA BSG";
                string k2 = "ZETAKT";
                string urun_ad = "BİGUS AKTARICI";
                string akt_kod;

                akt_kod = Registry.Oku(k1, k2);

                if (akt_kod == "xxx")
                {
                    Registry.Yaz("bos", "bos", k1, k2);
                }

                Key _form = new Key();
                _form._key = gen;
                _form._urun_adi = urun_ad;

                if (Registry.Kontrol(akt_kod, gen) == false)
                {
                    if (_form.ShowDialog() == DialogResult.OK)
                    {
                        if (_form._akt_key != null)
                        {
                            if (Registry.Kontrol(_form._akt_key, gen) == true)
                            {
                                Registry.Yaz(_form._akt_key, "", k1, k2);
                                Process runningInstance = GetRunningInstance();

                                if (runningInstance == null)
                                {
                                    frm_Aktarim mainForm = new frm_Aktarim();
                                    Application.Run(mainForm);
                                }
                                else
                                {
                                    ShowWindow(runningInstance.MainWindowHandle, 1);
                                }
                            }
                            else
                            {
                                ShowLoginForm(true);
                            }
                        }
                        else
                        {
                            ShowLoginForm(true);
                        }

                    }

                }
                else
                {
                    Application.Run(new frm_Aktarim());
                }
            }
            else
            {
                Process runningInstance = GetRunningInstance();

                if (runningInstance == null)
                {
                    frm_Aktarim mainForm = new frm_Aktarim();
                    Application.Run(mainForm);
                }
                else
                {
                    ShowWindow(runningInstance.MainWindowHandle, 1);
                }
            }
        }


    }
}
