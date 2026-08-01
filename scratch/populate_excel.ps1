param (
    [string]$ExcelPath = "C:\mahmut\chekstresistokdetaylı.xlsx"
)

Write-Host "==================================================="
Write-Host "  Mikro Raporu Excel Doldurucu (Sürüm 1.0)"
Write-Host "  Hedef Dosya: $ExcelPath"
Write-Host "==================================================="

# 1. SQL Server'a bağlanıp STOK_HAREKETLERI ve STOK_HAREKETLERI_USER verilerini çekelim
$connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()
Write-Host "[+] SQL Server (213.142.159.18 / MikroDesktop_MAHMUT) bağlantısı başarılı!"

$sql = @"
SELECT 
    sh.sth_RECno,
    sh.sth_Guid,
    sh.sth_evrakno_sira,
    sh.sth_stok_kod,
    ISNULL(usr.Kalite, '') AS Kalite,
    ISNULL(usr.Cilt, '')   AS Cilt,
    ISNULL(usr.Tuy, '')    AS Tuy,
    ISNULL(usr.renk, '')   AS Renk,
    ISNULL(usr.Urun, '')   AS Urun,
    ISNULL(usr.Diger, '')  AS Diger
FROM STOK_HAREKETLERI sh WITH (NOLOCK)
LEFT OUTER JOIN STOK_HAREKETLERI_USER usr WITH (NOLOCK) 
    ON usr.Record_uid = sh.sth_Guid
"@

$cmd = $conn.CreateCommand()
$cmd.CommandText = $sql
$adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
$ds = New-Object System.Data.DataSet
$adapter.Fill($ds)
$conn.Close()

$sqlRows = $ds.Tables[0]
Write-Host "[+] SQL'den toplam $($sqlRows.Rows.Count) adet hareket kaydı alındı."

# Dictionary mapping RECno string -> DataRow, and (EvrakNo+StokKod) -> DataRow
$mapByRecNo = @{}
$mapByEvrakStok = @{}

foreach ($r in $sqlRows.Rows) {
    $recNoStr = $r["sth_RECno"].ToString().Trim()
    $evrakStr = $r["sth_evrakno_sira"].ToString().Trim()
    $stokKodStr = $r["sth_stok_kod"].ToString().Trim()
    
    if ($recNoStr -ne "") {
        $mapByRecNo[$recNoStr] = $r
    }
    $key = "${evrakStr}_${stokKodStr}"
    if (-not $mapByEvrakStok.ContainsKey($key)) {
        $mapByEvrakStok[$key] = $r
    }
}

# 2. Excel dosyasını açıp kolonları dolduralım
Write-Host "[+] Excel dosyası açılıyor..."
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false

$wb = $excel.Workbooks.Open($ExcelPath)
$sheet = $wb.Sheets.Item(1)
$totalRows = $sheet.UsedRange.Rows.Count
$totalCols = $sheet.UsedRange.Columns.Count

Write-Host "[+] Excel Satır Sayısı: $totalRows, Kolon Sayısı: $totalCols"

# Kolon indekslerini bulalım
# Col 3: Evrak no
# Col 11: Stok hareket Kayıt no
# Col 12: Stok Kodu
# Col 73..78: Kalite, Cilt, Tüy, Renk, Ürün, Diğer

$updatedCount = 0

for ($row = 2; $row -le $totalRows; $row++) {
    $recNoCell = $sheet.Cells.Item($row, 11).Text.Trim()
    $evrakCell = $sheet.Cells.Item($row, 3).Text.Trim()
    $stokCell  = $sheet.Cells.Item($row, 12).Text.Trim()

    $match = $null

    if ($recNoCell -ne "" -and $mapByRecNo.ContainsKey($recNoCell)) {
        $match = $mapByRecNo[$recNoCell]
    } else {
        $key = "${evrakCell}_${stokCell}"
        if ($mapByEvrakStok.ContainsKey($key)) {
            $match = $mapByEvrakStok[$key]
        }
    }

    if ($match -ne $null) {
        $sheet.Cells.Item($row, 73).Value2 = $match["Kalite"].ToString()
        $sheet.Cells.Item($row, 74).Value2 = $match["Cilt"].ToString()
        $sheet.Cells.Item($row, 75).Value2 = $match["Tuy"].ToString()
        $sheet.Cells.Item($row, 76).Value2 = $match["Renk"].ToString()
        $sheet.Cells.Item($row, 77).Value2 = $match["Urun"].ToString()
        $sheet.Cells.Item($row, 78).Value2 = $match["Diger"].ToString()
        $updatedCount++
    }
}

Write-Host "[+] Toplam $updatedCount satır başarıyla dolduruldu!"

$wb.Save()
$wb.Close($true)
$excel.Quit()

Write-Host "==================================================="
Write-Host "  ✅ Rapor Tamamlandı ve Kaydedildi!"
Write-Host "==================================================="
