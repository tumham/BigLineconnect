try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server database MikroDesktop_MAHMUT!"

    # Trigger: Ana Satır (cha_satir_no = 0) sadece Stoklar Toplamını alır.
    # Masraf Satırları (cha_satir_no = 2) kendi masraf meblağını alır (1.450 USD).
    # msp_CariFoy ikisini toplayınca TAM 47.026,88 USD (2.107.424,75 TL) eder!
    $sqlTrigger = @"
CREATE OR ALTER TRIGGER trg_STOK_HAREKETLERI_TutarHesapla
ON dbo.STOK_HAREKETLERI
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- 1. STOK_HAREKETLERI Güncellemesi
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

    -- 2. CARI_HESAP_HAREKETLERI Güncellemesi
    -- Ana Fatura Başlık Satırı (cha_satir_no = 0) Sadece Stoklar Toplamını alır
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

    -- Stok Detay Cari Satırının (cha_cinsi = 15) meblağını 0 yapalım ki Föyde katlanma olmasın
    UPDATE C
    SET 
        C.cha_aratoplam = 0,
        C.cha_meblag    = 0
    FROM dbo.CARI_HESAP_HAREKETLERI AS C
    INNER JOIN (
        SELECT DISTINCT sth_evrakno_seri, sth_evrakno_sira 
        FROM inserted
    ) I
        ON C.cha_evrakno_seri = I.sth_evrakno_seri
       AND C.cha_evrakno_sira = I.sth_evrakno_sira
    WHERE C.cha_cinsi = 15;
END;
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlTrigger
    $cmd.ExecuteNonQuery()
    Write-Host "[+] SUCCESS! Trigger updated."

    # FIA26 - 14 faturasını tam 45.576,875 (Stok) + 1.450 (Masraf) = 47.026,875 USD olarak güncelleyelim
    $fixSql = @"
UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 45576.875, cha_aratoplam = 45576.875
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no = 0;

UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 0, cha_aratoplam = 0
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_cinsi = 15;

UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 1450, cha_aratoplam = 1450
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no = 2;
"@
    $cmd.CommandText = $fixSql
    $cmd.ExecuteNonQuery()
    Write-Host "[+] FIA26-14 Cari Hareketleri updated to exact 47,026.88 USD total."

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
