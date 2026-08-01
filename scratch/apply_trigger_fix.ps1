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

    -- 1. Sonsuz tetiklemeyi (Recursive Trigger) engelleme
    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- 2. STOK_HAREKETLERI Güncellemesi
    UPDATE S
    SET 
        S.sth_tutar = CAST(ISNULL(I.sth_miktar2, 0) AS DECIMAL(18,4)) 
                      * CAST(ISNULL(I.sth_netagirlik, 0) AS DECIMAL(18,4)),

        S.sth_birimfiyat = CASE 
                               WHEN ISNULL(I.sth_miktar, 0) = 0 THEN 0 
                               ELSE (CAST(ISNULL(I.sth_miktar2, 0) AS DECIMAL(18,4)) 
                                     * CAST(ISNULL(I.sth_netagirlik, 0) AS DECIMAL(18,4)))
                                     / CAST(I.sth_miktar AS DECIMAL(18,4)) 
                           END,

        S.sth_vergi = CASE 
                           WHEN ISNULL(I.sth_vergisiz_fl, 0) = 1 THEN 0
                           ELSE (CAST(ISNULL(I.sth_miktar2, 0) AS DECIMAL(18,4)) 
                                 * CAST(ISNULL(I.sth_netagirlik, 0) AS DECIMAL(18,4)))
                                 * CASE I.sth_vergi_pntr
                                       WHEN 2 THEN 0.01   -- %1
                                       WHEN 3 THEN 0.10   -- %10
                                       WHEN 4 THEN 0.20   -- %20
                                       ELSE 0
                                   END
                       END
    FROM dbo.STOK_HAREKETLERI AS S
    INNER JOIN inserted AS I 
        ON S.sth_Guid = I.sth_Guid
    WHERE ISNULL(I.sth_netagirlik, 0) > 0;

    -- 3. CARI_HESAP_HAREKETLERI Güncellemesi (Sadece Ana Satır - cha_satir_no = 0)
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
            WHERE ISNULL(sth_netagirlik, 0) > 0
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
    Write-Host "[+] SUCCESS! Trigger trg_STOK_HAREKETLERI_TutarHesapla updated successfully on SQL Server!"

    # FIA26 - 14 faturasının Cari Hareketlerindeki Satır 1 meblağını temizleyelim
    $fixCari = @"
UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 0, cha_aratoplam = 0
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no > 0 AND cha_evrak_tip = 63
"@
    $cmd.CommandText = $fixCari
    $rowsAffected = $cmd.ExecuteNonQuery()
    Write-Host "[+] Fixed FIA26-14 Cari Hareketleri ($rowsAffected row(s) updated)."

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
