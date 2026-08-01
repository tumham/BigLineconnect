import sys

def modify_aktarimlar2():
    path = r"c:\Projev17YD\DUZ_V17_STD\Bigus.Aktarici.Linq\Aktarimlar2.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # Apply to STOK_HAREKETLERI
    content = content.replace("from T in db.STOK_HAREKETLERIs where (SeriNo", 
                              "from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo")
    
    # Apply to CARI_HESAP_HAREKETLERI
    content = content.replace("from T in db.CARI_HESAP_HAREKETLERIs where (SeriNo", 
                              "from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo")

    # The method
    methods = """
        public static void Stok_Hareketleri_Kilit_Guncelle(Guid guid, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                    db.ExecuteCommand("UPDATE STOK_HAREKETLERI SET sth_kilitli = 1 WHERE sth_Guid = {0}", guid);
                }
            }
            catch { }
        }

        public static void Cari_Hesap_Hareket_Kilit_Guncelle(Guid guid, string conn)
        {
            try
            {
                using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))
                {
                    db.ExecuteCommand("UPDATE CARI_HESAP_HAREKETLERI SET cha_kilitli = 1 WHERE cha_Guid = {0}", guid);
                }
            }
            catch { }
        }
"""
    if "Stok_Hareketleri_Kilit_Guncelle" not in content:
        idx = content.rfind("}")
        idx = content.rfind("}", 0, idx)
        content = content[:idx] + methods + content[idx:]

    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

def modify_frm_aktarim():
    path = r"c:\Projev17YD\DUZ_V17_STD\Bigus.Aktarici.WinApp\frm_Aktarim.cs"
    with open(path, "r", encoding="utf-8") as f:
        content = f.read()

    # Replace end of STOK_HAREKETLERI loop
    stok_target = """                    }
                }
            }
            #endregion
            List<CARI_HESAP_HAREKETLERI> ls_2 = new List<CARI_HESAP_HAREKETLERI>();"""
            
    stok_replacement = """                    }
                    // STOK_HAREKETLERI KAYDI AKTARILDI. KILIT GUNCELLEMESI
                    Aktarimlar2.Stok_Hareketleri_Kilit_Guncelle(_st_recno, DatabaseFacade.ConnectionString());
                }
            }
            #endregion
            List<CARI_HESAP_HAREKETLERI> ls_2 = new List<CARI_HESAP_HAREKETLERI>();"""
            
    if stok_target in content:
        content = content.replace(stok_target, stok_replacement)
    else:
        print("Could not find stok_target")

    # Find where CARI_HESAP_HAREKETLERI foreach ends.
    # We can use Regex to find `foreach (CARI_HESAP_HAREKETLERI cha in ls_2)` and then the closing `#endregion` for "FATURA OLMAYAN CARİ HESAP HAREKETLERI"
    # Actually, in frm_Aktarim.cs there is `#region FATURA OLMAYAN CARİ HESAP HAREKETLERI`
    # We can just look for the end of it:
    
    # Wait, the structure is:
    #             #region FATURA OLMAYAN CARİ HESAP HAREKETLERI
    #             if (ls_2.Count > 0)
    #             {
    #                 foreach (CARI_HESAP_HAREKETLERI cha in ls_2)
    #                 {
    # ...
    #                         #endregion
    #                     }
    #                 }
    #             }
    #             #endregion
    #             SetControlText(lbl_durum, "Aktarım tamamlandı.");
    # It might be followed by SetControlText(lbl_durum, "Aktarım tamamlandı.");
    # Let's try replacing this block:
    
    cari_target = """                        #endregion
                    }
                }
            }
            #endregion
            SetControlText(lbl_durum, "Aktarım tamamlandı.");"""
            
    cari_replacement = """                        #endregion
                    }
                    // CARI_HESAP_HAREKETLERI KAYDI AKTARILDI. KILIT GUNCELLEMESI
                    Aktarimlar2.Cari_Hesap_Hareket_Kilit_Guncelle(cha.cha_Guid, DatabaseFacade.ConnectionString());
                }
            }
            #endregion
            SetControlText(lbl_durum, "Aktarım tamamlandı.");"""
            
    if cari_target in content:
        content = content.replace(cari_target, cari_replacement)
    else:
        print("Could not find cari_target")

    with open(path, "w", encoding="utf-8") as f:
        f.write(content)

modify_aktarimlar2()
modify_frm_aktarim()
print("Done!")
