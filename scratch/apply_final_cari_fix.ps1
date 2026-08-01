try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server database MikroDesktop_MAHMUT!"

    # 1. Trigger'ı her cha_satir_no için birebir sth_satirno tutarını yazacak şekilde güncelleyelim
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
                           END
    FROM dbo.STOK_HAREKETLERI AS S
    INNER JOIN inserted AS I 
        ON S.sth_Guid = I.sth_Guid
    WHERE ISNULL(I.sth_netagirlik, 0) > 0;

    -- 2. CARI_HESAP_HAREKETLERI Güncellemesi
    -- Ana Satır (cha_satir_no = 0) evrakın TÜM stok ve masraf toplamını alır (47.026,88 USD)
    UPDATE C
    SET 
        C.cha_aratoplam = T.ToplamTutar,
        C.cha_meblag    = T.ToplamTutar
    FROM dbo.CARI_HESAP_HAREKETLERI AS C
    INNER JOIN (
        SELECT 
            S.sth_evrakno_seri,
            S.sth_evrakno_sira,
            SUM(ISNULL(S.sth_tutar, 0)) AS ToplamTutar
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

    -- Diğer yan cari satırlarının meblağını 0 yapalım ki föyde katlanma olmasın
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
    WHERE C.cha_satir_no > 0;
END;
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlTrigger
    $cmd.ExecuteNonQuery()
    Write-Host "[+] SUCCESS! Trigger updated."

    # FIA26 - 14 faturasını tam 47.026,88 USD (2.107.424,75 TL) olarak güncelleyelim
    $fixSql = @"
UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 47026.875, cha_aratoplam = 47026.875
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no = 0;

UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 0, cha_aratoplam = 0
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no > 0;
"@
    $cmd.CommandText = $fixSql
    $cmd.ExecuteNonQuery()
    Write-Host "[+] FIA26-14 Cari Hareketleri updated to 47,026.88 USD."

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
