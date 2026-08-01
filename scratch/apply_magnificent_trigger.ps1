try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server database MikroDesktop_MAHMUT!"

    $sqlTrigger = @"
CREATE OR ALTER TRIGGER trg_STOK_HAREKETLERI_TutarHesapla
ON dbo.STOK_HAREKETLERI
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    -- Sonsuz tetiklemeyi engelleme
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- 1. STOK HAREKETLERİ GÜNCELLEMESİ (Tüm Evrak Satırları İçin)
    -- Eklenecek veya değiştirilecek faturanın TÜM stok satırlarında sth_netagirlik > 0 ise tutar ve birimfiyatı hesapla.
    -- sth_netagirlik = 0 veya NULL olan (Masraf, Navlun vb.) satırlara HİÇ DOKUNULMAZ ve olduğu gibi korunur.
    UPDATE S
    SET 
        S.sth_tutar = (CAST(ISNULL(S.sth_miktar2, 0) AS DECIMAL(18,4)) * CAST(ISNULL(S.sth_netagirlik, 0) AS DECIMAL(18,4)))
                      * (1.0 - (ISNULL(S.sth_iskonto1, 0) / 100.0))
                      * (1.0 - (ISNULL(S.sth_iskonto2, 0) / 100.0))
                      * (1.0 - (ISNULL(S.sth_iskonto3, 0) / 100.0))
                      * (1.0 - (ISNULL(S.sth_iskonto4, 0) / 100.0))
                      * (1.0 - (ISNULL(S.sth_iskonto5, 0) / 100.0))
                      * (1.0 - (ISNULL(S.sth_iskonto6, 0) / 100.0)),

        S.sth_birimfiyat = CASE 
                               WHEN ISNULL(S.sth_miktar, 0) = 0 THEN 0 
                               ELSE ((CAST(ISNULL(S.sth_miktar2, 0) AS DECIMAL(18,4)) * CAST(ISNULL(S.sth_netagirlik, 0) AS DECIMAL(18,4)))
                                     * (1.0 - (ISNULL(S.sth_iskonto1, 0) / 100.0))
                                     * (1.0 - (ISNULL(S.sth_iskonto2, 0) / 100.0))
                                     * (1.0 - (ISNULL(S.sth_iskonto3, 0) / 100.0)))
                                     / CAST(S.sth_miktar AS DECIMAL(18,4)) 
                           END,

        S.sth_vergi = CASE 
                           WHEN ISNULL(S.sth_vergisiz_fl, 0) = 1 THEN 0
                           ELSE ((CAST(ISNULL(S.sth_miktar2, 0) AS DECIMAL(18,4)) * CAST(ISNULL(S.sth_netagirlik, 0) AS DECIMAL(18,4)))
                                 * (1.0 - (ISNULL(S.sth_iskonto1, 0) / 100.0))
                                 * (1.0 - (ISNULL(S.sth_iskonto2, 0) / 100.0)))
                                 * CASE S.sth_vergi_pntr
                                       WHEN 2 THEN 0.01   -- %1 KDV
                                       WHEN 3 THEN 0.10   -- %10 KDV
                                       WHEN 4 THEN 0.20   -- %20 KDV
                                       ELSE 0
                                   END
                       END
    FROM dbo.STOK_HAREKETLERI AS S
    INNER JOIN (
        SELECT DISTINCT sth_evrakno_seri, sth_evrakno_sira
        FROM inserted
    ) AS I 
        ON S.sth_evrakno_seri = I.sth_evrakno_seri
       AND S.sth_evrakno_sira = I.sth_evrakno_sira
    WHERE ISNULL(S.sth_netagirlik, 0) > 0;

    -- 2. CARİ HAREKETLER GÜNCELLEMESİ (Tüm Evrak İçin Cari Föyü Otomatik Yeniler)
    -- Faturada tek satır bile değişse veya yeni satır eklense, Cari Föydeki Ana Satır (cha_satir_no = 0) TÜM evrakın güncel toplamıyla yenilenir.
    UPDATE C
    SET 
        C.cha_aratoplam = T.ToplamTutar,
        C.cha_meblag    = T.ToplamTutar + T.ToplamVergi
    FROM dbo.CARI_HESAP_HAREKETLERI AS C
    INNER JOIN (
        SELECT 
            S.sth_evrakno_seri,
            S.sth_evrakno_sira,
            SUM(ISNULL(S.sth_tutar, 0)) AS ToplamTutar,
            SUM(ISNULL(S.sth_vergi, 0)) AS ToplamVergi
        FROM dbo.STOK_HAREKETLERI S WITH (NOLOCK)
        INNER JOIN (
            SELECT DISTINCT sth_evrakno_seri, sth_evrakno_sira 
            FROM inserted
        ) I
            ON S.sth_evrakno_seri = I.sth_evrakno_seri
           AND S.sth_evrakno_sira = I.sth_evrakno_sira
        GROUP BY S.sth_evrakno_seri, S.sth_evrakno_sira
    ) AS T
        ON C.cha_evrakno_seri = T.sth_evrakno_seri
       AND C.cha_evrakno_sira = T.sth_evrakno_sira
    WHERE C.cha_satir_no = 0;
END;
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlTrigger
    $cmd.ExecuteNonQuery()
    Write-Host "[+] SUCCESS! Magnificent trigger updated on SQL Server!"

    # TEST: Sadece tek bir satırı güncelleyelim (örneğin 2. satırı netagirlik = 8 yapalım)
    $testEditSql = "UPDATE dbo.STOK_HAREKETLERI SET sth_netagirlik = 8.0 WHERE sth_evrakno_seri = 'FIA26' AND sth_evrakno_sira = 14 AND sth_miktar = 90;"
    $cmd.CommandText = $testEditSql
    $cmd.ExecuteNonQuery()
    Write-Host "[+] Edited ONLY single row 2 (sth_netagirlik = 8.0)."

    # msp_CariFoy çalıştırıp otomatik güncellendi mi görelim
    $tempTable = "CARI_FOYU_TEST_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    $spSql = "EXEC dbo.msp_CariFoy N'0',0,N'USDM016',NULL,'20251231','20260101','20261231',0,N'',$tempTable"
    $cmd.CommandText = $spSql
    $cmd.ExecuteNonQuery()

    $selectSql = "SELECT * FROM $tempTable"
    $cmd.CommandText = $selectSql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "`n--- CARI FOY SP RESULT AFTER SINGLE ROW EDIT ---"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "ANA TL BORC   : $($r['msg_S_0101\T']) TL"
        Write-Host "ALT EUR BORC  : $($r['msg_S_0105\T']) EUR"
        Write-Host "ORJ USD BORC  : $($r['msg_S_0109\T']) USD"
    }

    # Revert test row back to 7.0
    $revertSql = "UPDATE dbo.STOK_HAREKETLERI SET sth_netagirlik = 7.0 WHERE sth_evrakno_seri = 'FIA26' AND sth_evrakno_sira = 14 AND sth_miktar = 90;"
    $cmd.CommandText = $revertSql
    $cmd.ExecuteNonQuery()

    # Drop temp table
    $cmd.CommandText = "IF OBJECT_ID('$tempTable') IS NOT NULL DROP TABLE $tempTable"
    $cmd.ExecuteNonQuery()

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
