SELECT TOP 20
    s.sto_kod AS Sto_Kod,
    s.sto_isim AS Sto_Isim,
    ISNULL(VK_Renk.VaryantKrlm_Isim, '') AS Renk,
    ISNULL(VK_Beden.VaryantKrlm_Isim, '') AS Beden,
    CASE 
        WHEN (h.sth_tip = 0) OR ((h.sth_tip = 2) AND (h.sth_giris_depo_no IS NOT NULL)) 
            THEN h.sth_giris_depo_no 
        WHEN (h.sth_tip = 1) OR ((h.sth_tip = 2) AND (h.sth_cikis_depo_no IS NOT NULL)) 
            THEN h.sth_cikis_depo_no 
        ELSE NULL 
    END AS DepoNo,
    d.dep_adi AS DepoAdi,
    SUM(
        CASE 
            WHEN (h.sth_tip = 0) OR ((h.sth_tip = 2) AND (h.sth_giris_depo_no IS NOT NULL)) 
                THEN ISNULL(b.BdnHar_HarGor, h.sth_miktar) 
            WHEN (h.sth_tip = 1) OR ((h.sth_tip = 2) AND (h.sth_cikis_depo_no IS NOT NULL)) 
                THEN (-1) * ISNULL(b.BdnHar_HarGor, h.sth_miktar) 
            ELSE 0 
        END
    ) AS DepodakiMiktar
FROM dbo.STOK_HAREKETLERI h WITH (NOLOCK)
INNER JOIN dbo.STOKLAR s WITH (NOLOCK)
        ON s.sto_kod = h.sth_stok_kod
LEFT JOIN dbo.DEPOLAR d WITH (NOLOCK)
        ON d.dep_no = CASE 
                        WHEN (h.sth_tip = 0) OR ((h.sth_tip = 2) AND (h.sth_giris_depo_no IS NOT NULL)) 
                            THEN h.sth_giris_depo_no 
                        WHEN (h.sth_tip = 1) OR ((h.sth_tip = 2) AND (h.sth_cikis_depo_no IS NOT NULL)) 
                            THEN h.sth_cikis_depo_no 
                        ELSE NULL 
                      END
LEFT JOIN dbo.BEDEN_HAREKETLERI b WITH (NOLOCK)
        ON h.sth_Guid = b.BdnHar_Har_uid
LEFT JOIN dbo.VARYANT_BAGLANTI_TANIMLARI VB_Renk WITH (NOLOCK)
        ON VB_Renk.VBag_Guid IN (b.BdnHar_VarBaglantiUId1, b.BdnHar_VarBaglantiUId2, b.BdnHar_VarBaglantiUId3, b.BdnHar_VarBaglantiUId4, b.BdnHar_VarBaglantiUId5)
       AND VB_Renk.VBag_Tip = 0
LEFT JOIN dbo.VARYANT_KIRILIM_TANIMLARI VK_Renk WITH (NOLOCK)
        ON VK_Renk.VaryantKrlm_Kod = VB_Renk.VBag_KirilimKod
       AND VK_Renk.VaryantKrlm_Tip = 0
LEFT JOIN dbo.VARYANT_BAGLANTI_TANIMLARI VB_Beden WITH (NOLOCK)
        ON VB_Beden.VBag_Guid IN (b.BdnHar_VarBaglantiUId1, b.BdnHar_VarBaglantiUId2, b.BdnHar_VarBaglantiUId3, b.BdnHar_VarBaglantiUId4, b.BdnHar_VarBaglantiUId5)
       AND VB_Beden.VBag_Tip = 1
LEFT JOIN dbo.VARYANT_KIRILIM_TANIMLARI VK_Beden WITH (NOLOCK)
        ON VK_Beden.VaryantKrlm_Kod = VB_Beden.VBag_KirilimKod
       AND VK_Beden.VaryantKrlm_Tip = 1
WHERE NOT (h.sth_cins IN (9, 15)) 
GROUP BY 
    s.sto_kod, s.sto_isim,
    VK_Renk.VaryantKrlm_Isim,
    VK_Beden.VaryantKrlm_Isim,
    CASE 
        WHEN (h.sth_tip = 0) OR ((h.sth_tip = 2) AND (h.sth_giris_depo_no IS NOT NULL)) 
            THEN h.sth_giris_depo_no 
        WHEN (h.sth_tip = 1) OR ((h.sth_tip = 2) AND (h.sth_cikis_depo_no IS NOT NULL)) 
            THEN h.sth_cikis_depo_no 
        ELSE NULL 
    END,
    d.dep_adi
HAVING SUM(
        CASE 
            WHEN (h.sth_tip = 0) OR ((h.sth_tip = 2) AND (h.sth_giris_depo_no IS NOT NULL)) 
                THEN ISNULL(b.BdnHar_HarGor, h.sth_miktar) 
            WHEN (h.sth_tip = 1) OR ((h.sth_tip = 2) AND (h.sth_cikis_depo_no IS NOT NULL)) 
                THEN (-1) * ISNULL(b.BdnHar_HarGor, h.sth_miktar) 
            ELSE 0 
        END
    ) <> 0
ORDER BY d.dep_adi, s.sto_isim, VK_Renk.VaryantKrlm_Isim, VK_Beden.VaryantKrlm_Isim;
