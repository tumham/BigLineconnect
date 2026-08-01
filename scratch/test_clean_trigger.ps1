try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server database MikroDesktop_MAHMUT!"

    # 1. Trigger'ı CARI_HESAP_HAREKETLERI'ne MÜDAHALE ETMEYECEK şekilde sadeleştirelim
    # Sadece STOK_HAREKETLERI tablosunda sth_netagirlik > 0 ise tutar ve birimfiyatı hesaplar.
    $sqlTrigger = @"
CREATE OR ALTER TRIGGER trg_STOK_HAREKETLERI_TutarHesapla
ON dbo.STOK_HAREKETLERI
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF TRIGGER_NESTLEVEL() > 1 RETURN;

    -- Sadece Döviz Birim Fiyatı (sth_netagirlik) > 0 olan stok satırlarında tutar ve birim fiyatı hesapla.
    -- sth_netagirlik = 0 veya NULL olan (Masraf vb.) satırlara HİÇ DOKUNMA!
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
END;
"@

    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlTrigger
    $cmd.ExecuteNonQuery()
    Write-Host "[+] SUCCESS! Trigger updated to only manage STOK_HAREKETLERI without corrupting CARI_HESAP_HAREKETLERI!"

    # 2. FIA26-14 Cari hareketini tam faturadaki 48.060,88 USD (2.153.761,60 TL) değerine getirelim
    $fixCari = @"
UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 46610.875, cha_aratoplam = 46610.875
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no = 0;

UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 0, cha_aratoplam = 0
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no = 1;

UPDATE dbo.CARI_HESAP_HAREKETLERI
SET cha_meblag = 1450, cha_aratoplam = 1450
WHERE cha_evrakno_seri = 'FIA26' AND cha_evrakno_sira = 14 AND cha_satir_no = 2;
"@
    $cmd.CommandText = $fixCari
    $cmd.ExecuteNonQuery()
    Write-Host "[+] FIA26-14 Cari Hareketleri updated to 48,060.88 USD total."

    # 3. msp_CariFoy prosedürünü çalıştırıp sonucu kontrol edelim
    $tempTable = "CARI_FOYU_TEST_" + (Get-Date -Format "yyyyMMdd_HHmmss")
    $spSql = "EXEC dbo.msp_CariFoy N'0',0,N'USDM016',NULL,'20251231','20260101','20261231',0,N'',$tempTable"
    $cmd.CommandText = $spSql
    $cmd.ExecuteNonQuery()

    $selectSql = "SELECT * FROM $tempTable"
    $cmd.CommandText = $selectSql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
    $ds = New-Object System.Data.DataSet
    $adapter.Fill($ds)

    Write-Host "`n--- CARI FOY SP RESULT ---"
    foreach ($r in $ds.Tables[0].Rows) {
        Write-Host "ANA TL BORC   : $($r['msg_S_0101\T']) TL"
        Write-Host "ALT EUR BORC  : $($r['msg_S_0105\T']) EUR"
        Write-Host "ORJ USD BORC  : $($r['msg_S_0109\T']) USD"
    }

    # Drop temp table
    $cmd.CommandText = "IF OBJECT_ID('$tempTable') IS NOT NULL DROP TABLE $tempTable"
    $cmd.ExecuteNonQuery()

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
