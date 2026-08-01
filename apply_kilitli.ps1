$path1 = "c:\Projev17YD\DUZ_V17_STD\Bigus.Aktarici.Linq\Aktarimlar2.cs"
$content1 = [System.IO.File]::ReadAllText($path1)

$content1 = $content1.Replace('from T in db.STOK_HAREKETLERIs where (SeriNo', 'from T in db.STOK_HAREKETLERIs where (T.sth_kilitli == false || T.sth_kilitli == null) && (SeriNo')
$content1 = $content1.Replace('from T in db.CARI_HESAP_HAREKETLERIs where (SeriNo', 'from T in db.CARI_HESAP_HAREKETLERIs where (T.cha_kilitli == false || T.cha_kilitli == null) && (SeriNo')

$methods = @"
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
"@

if ($content1.IndexOf("Stok_Hareketleri_Kilit_Guncelle") -eq -1) {
    $idx = $content1.LastIndexOf('}')
    $idx = $content1.LastIndexOf('}', $idx - 1)
    $content1 = $content1.Substring(0, $idx) + $methods + "`r`n" + $content1.Substring($idx)
}

[System.IO.File]::WriteAllText($path1, $content1)

$path2 = "c:\Projev17YD\DUZ_V17_STD\Bigus.Aktarici.WinApp\frm_Aktarim.cs"
$content2 = [System.IO.File]::ReadAllText($path2)

$stok_target = @"
                    }
                }
            }
            #endregion
            List<CARI_HESAP_HAREKETLERI> ls_2 = new List<CARI_HESAP_HAREKETLERI>();
"@

$stok_replacement = @"
                    }
                    // STOK_HAREKETLERI KAYDI AKTARILDI. KILIT GUNCELLEMESI
                    Aktarimlar2.Stok_Hareketleri_Kilit_Guncelle(_st_recno, DatabaseFacade.ConnectionString());
                }
            }
            #endregion
            List<CARI_HESAP_HAREKETLERI> ls_2 = new List<CARI_HESAP_HAREKETLERI>();
"@

$content2 = $content2.Replace($stok_target, $stok_replacement)

$cari_target = @"
                        #endregion
                    }
                }
            }
            #endregion
            SetControlText(lbl_durum, `"Aktarım tamamlandı.`");
"@

$cari_replacement = @"
                        #endregion
                    }
                    // CARI_HESAP_HAREKETLERI KAYDI AKTARILDI. KILIT GUNCELLEMESI
                    Aktarimlar2.Cari_Hesap_Hareket_Kilit_Guncelle(cha.cha_Guid, DatabaseFacade.ConnectionString());
                }
            }
            #endregion
            SetControlText(lbl_durum, `"Aktarım tamamlandı.`");
"@

$content2 = $content2.Replace($cari_target, $cari_replacement)

[System.IO.File]::WriteAllText($path2, $content2)

Write-Host "Done!"
