$excelFile = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
Write-Host "==================================================="
Write-Host "  ⚡ Mikro Raporu Excel Doldurucu (Final)"
Write-Host "  Hedef Dosya: $excelFile"
Write-Host "==================================================="

# 1. SQL Server'a baglanip verileri alalim
$connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
Write-Host "[+] SQL Server (213.142.159.18 / MikroDesktop_MAHMUT) baglantisi basarili!"

$sql = @"
SELECT 
    sh.sth_Guid,
    sh.sth_evrakno_seri,
    sh.sth_evrakno_sira,
    sh.sth_stok_kod,
    ISNULL(usr.Kalite, '') AS Kalite,
    ISNULL(usr.Cilt, '')   AS Cilt,
    ISNULL(usr.Tuy, '')    AS Tuy,
    ISNULL(usr.renk, '')   AS Renk,
    ISNULL(usr.Urun, '')   AS Urun,
    ISNULL(usr.Diger, '')  AS Diger
FROM STOK_HAREKETLERI sh WITH (NOLOCK)
INNER JOIN STOK_HAREKETLERI_USER usr WITH (NOLOCK) 
    ON usr.Record_uid = sh.sth_Guid
"@

$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$ds = New-Object System.Data.DataSet
$adapter.Fill($ds)
$conn.Close()

$sqlRows = $ds.Tables[0]
Write-Host "[+] STOK_HAREKETLERI_USER ile eslesen $($sqlRows.Rows.Count) adet ozel hareket kaydi bulundu!"

foreach ($r in $sqlRows.Rows) {
    Write-Host " -> Eslesen Kayit | Evrak Sira: $($r['sth_evrakno_sira']) | Stok: $($r['sth_stok_kod']) | Kalite: $($r['Kalite']) | Cilt: $($r['Cilt']) | Tuy: $($r['Tuy']) | Renk: $($r['Renk'])"
}

# Excel uygulamasini baslatalim
Write-Host "[+] Excel aciliyor..."
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

$wb = $excel.Workbooks.Open($excelFile)
$sheet = $wb.Sheets.Item(1)
$totalRows = $sheet.UsedRange.Rows.Count
$totalCols = $sheet.UsedRange.Columns.Count

Write-Host "[+] Excel Satir Sayisi: $totalRows, Kolon Sayisi: $totalCols"

$updatedCount = 0

for ($rIdx = 2; $rIdx -le $totalRows; $rIdx++) {
    $evrakCell = $sheet.Cells.Item($rIdx, 3).Text.Trim()
    $stokCell  = $sheet.Cells.Item($rIdx, 12).Text.Trim()

    foreach ($dbRow in $sqlRows.Rows) {
        $dbEvrak = $dbRow["sth_evrakno_sira"].ToString().Trim()
        $dbStok  = $dbRow["sth_stok_kod"].ToString().Trim()

        if (($evrakCell -ne "" -and $evrakCell -eq $dbEvrak) -or ($stokCell -ne "" -and $stokCell -eq $dbStok)) {
            $valKalite = $dbRow["Kalite"].ToString()
            $valCilt   = $dbRow["Cilt"].ToString()
            $valTuy    = $dbRow["Tuy"].ToString()
            $valRenk   = $dbRow["Renk"].ToString()
            $valUrun   = $dbRow["Urun"].ToString()
            $valDiger  = $dbRow["Diger"].ToString()

            if ($valKalite -ne "") { $sheet.Cells.Item($rIdx, 73).Value2 = $valKalite }
            if ($valCilt -ne "")   { $sheet.Cells.Item($rIdx, 74).Value2 = $valCilt }
            if ($valTuy -ne "")    { $sheet.Cells.Item($rIdx, 75).Value2 = $valTuy }
            if ($valRenk -ne "")   { $sheet.Cells.Item($rIdx, 76).Value2 = $valRenk }
            if ($valUrun -ne "")   { $sheet.Cells.Item($rIdx, 77).Value2 = $valUrun }
            if ($valDiger -ne "")  { $sheet.Cells.Item($rIdx, 78).Value2 = $valDiger }
            
            $updatedCount = $updatedCount + 1
            Write-Host " [OK] Satir $rIdx guncellendi: Stok: $stokCell | Renk: $valRenk | Kalite: $valKalite"
        }
    }
}

Write-Host "[+] Toplam $updatedCount hucre basariyla guncellendi!"

$wb.Save()
$wb.Close($true)
$excel.Quit()

Write-Host "==================================================="
Write-Host "  ISLEM BASARIYLA TAMAMLANDI VE EXCEL KAYDEDILDI!"
Write-Host "==================================================="
