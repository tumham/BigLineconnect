try {
    $connStr = "Server=213.142.159.18;Database=MikroDesktop_MAHMUT;User Id=yoldas;Password=123456;Encrypt=False;Connection Timeout=15;"
    $conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
    $conn.Open()
    Write-Host "[+] Connected to SQL Server!"

    $excelFile = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
    Write-Host "[+] Reading Excel: $excelFile"

    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $wb = $excel.Workbooks.Open($excelFile)
    $sheet = $wb.Sheets.Item(1)
    $totalRows = $sheet.UsedRange.Rows.Count

    Write-Host "Total Excel Rows: $totalRows"

    $excelGuids = @()
    for ($r = 2; $r -le $totalRows; $r++) {
        $g = $sheet.Cells.Item($r, 11).Text.Trim()
        if ($g -ne "") {
            $excelGuids += [PSCustomObject]@{ Row = $r; Guid = $g; Evrak = $sheet.Cells.Item($r, 3).Text.Trim(); Stok = $sheet.Cells.Item($r, 12).Text.Trim() }
        }
    }

    $wb.Close($false)
    $excel.Quit()

    Write-Host "[+] Found $($excelGuids.Count) rows with GUIDs in Col 11."

    Write-Host "`n--- CHECKING WHICH OF THESE EXCEL GUIDS EXIST IN STOK_HAREKETLERI_USER ---"
    $foundInUserTable = 0

    foreach ($item in $excelGuids) {
        $sql = "SELECT Record_uid, Kalite, Cilt, Tuy, renk, Urun, Diger FROM STOK_HAREKETLERI_USER WITH (NOLOCK) WHERE Record_uid = '$($item.Guid)'"
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $sql
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
        $ds = New-Object System.Data.DataSet
        $adapter.Fill($ds)

        if ($ds.Tables[0].Rows.Count -gt 0) {
            $foundInUserTable++
            $r = $ds.Tables[0].Rows[0]
            Write-Host " [MATCH IN SQL USER TABLE!] Excel Row $($item.Row) | Evrak: $($item.Evrak) | Stok: $($item.Stok) | GUID: $($item.Guid)"
            Write-Host "   -> Kalite: '$($r['Kalite'])' | Cilt: '$($r['Cilt'])' | Tüy: '$($r['Tuy'])' | Renk: '$($r['renk'])' | Ürün: '$($r['Urun'])' | Diğer: '$($r['Diger'])'"
        }
    }

    Write-Host "`nTOTAL MATCHED ROWS FROM THIS EXCEL FILE IN STOK_HAREKETLERI_USER: $foundInUserTable"

    $conn.Close()
} catch {
    Write-Host "[!] SQL Error: $_"
}
