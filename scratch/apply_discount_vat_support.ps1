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

    -- 1. STOK_HAREKETLERI Güncellemesi
    -- Sadece Döviz Birim Fiyatı (sth_netagirlik) > 0 olan stok satırlarında hesaplama yapılır.
    -- İskonto 1..6 düşülür, KDV (Vergi) net tutar üzerinden hesaplanır.
    -- sth_netagirlik = 0 veya NULL olan (Masraf vb.) satırlara HİÇ DOKUNULMAZ ve olduğu gibi korunur.
    UPDATE S
    SET 
        S.sth_tutar = (CAST(ISNULL(I.sth_miktar2, 0) AS DECIMAL(18,4)) * CAST(ISNULL(I.sth_netagirlik, 0) AS DECIMAL(18,4)))
                      * (1.0 - (ISNULL(I.sth_iskonto1, 0) / 100.0))
                      * (1.0 - (ISNULL(I.sth_iskonto2, 0) / 100.0))
                      * (1.0 - (ISNULL(I.sth_iskonto3, 0) / 100.0))
                      * (1.0 - (ISNULL(I.sth_iskonto4, 0) / 100.0))
                      * (1.0 - (ISNULL(I.sth_iskonto5, 0) / 100.0))
                      * (1.0 - (ISNULL(I.sth_iskonto6, 0) / 100.0)),

        S.sth_birimfiyat = CASE 
                               WHEN ISNULL(I.sth_miktar, 0) = 0 THEN 0 
                               ELSE ((CAST(ISNULL(I.sth_miktar2, 0) AS DECIMAL(18,4)) * CAST(ISNULL(I.sth_netagirlik, 0) AS DECIMAL(18,4)))
                                     * (1.0 - (ISNULL(I.sth_iskonto1, 0) / 100.0))
                                     * (1.0 - (ISNULL(I.sth_iskonto2, 0) / 100.0))
                                     * (1.0 - (ISNULL(I.sth_iskonto3, 0) / 100.0)))
                                     / CAST(I.sth_miktar AS DECIMAL(18,4)) 
                           END,

        S.sth_vergi = CASE 
                           WHEN ISNULL(I.sth_vergisiz_fl, 0) = 1 THEN 0
                           ELSE ((CAST(ISNULL(I.sth_miktar2, 0) AS DECIMAL(18,4)) * CAST(ISNULL(I.sth_netagirlik, 0) AS DECIMAL(18,4)))
                                 * (1.0 - (ISNULL(I.sth_iskonto1, 0) / 100.0))
                                 * (1.0 - (ISNULL(I.sth_iskonto2, 0) / 100.0)))
                                 * CASE I.sth_vergi_pntr
                                       WHEN 2 THEN 0.01   -- %1 KDV
                                       WHEN 3 THEN 0.10   -- %10 KDV
                                       WHEN 4 THEN 0.20   -- %20 KDV
                                       ELSE 0
                                   END
                       END
    FROM dbo.STOK_HAREKETLERI AS S
    INNER JOIN inserted AS I 
        ON S.sth_Guid = I.sth_Guid
    WHERE ISNULL(I.sth_netagirlik, 0) > 0;
END;
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlTrigger
    $cmd.ExecuteNonQuery()
    Write-Host "[+] SUCCESS! Trigger updated with Discount (İskonto 1..6) and KDV calculation support!"

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
