$files = Get-ChildItem -Path "C:\mahmut"

Write-Host "--- ALL FILES IN C:\mahmut ---"
foreach ($f in $files) {
    Write-Host "File: '$($f.FullName)' | Size: $($f.Length) bytes | LastWrite: $($f.LastWriteTime)"
}

# Inspect each excel file in C:\mahmut
$excelFiles = Get-ChildItem -Path "C:\mahmut" -Filter "*.xlsx"

foreach ($f in $excelFiles) {
    Write-Host "`n==================================================="
    Write-Host "INSPECTING FILE: '$($f.FullName)'"
    Write-Host "==================================================="

    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $wb = $excel.Workbooks.Open($f.FullName)
    $sheet = $wb.Sheets.Item(1)
    $totalRows = $sheet.UsedRange.Rows.Count
    $totalCols = $sheet.UsedRange.Columns.Count

    Write-Host "Total Rows: $totalRows | Total Cols: $totalCols"

    # Find where Kalite, Cilt, Tuy, Renk, Urun, Diger columns are
    $cols = @{}
    for ($c = 1; $c -le $totalCols; $c++) {
        $txt = $sheet.Cells.Item(1, $c).Text.Trim()
        if ($txt) {
            $cols[$c] = $txt
        }
    }

    Write-Host "`nCOLUMNS IN EXCEL:"
    foreach ($k in $cols.Keys | Sort-Object) {
        if ($k -ge 70 -or $cols[$k] -like "*Kalite*" -or $cols[$k] -like "*Renk*" -or $cols[$k] -like "*Cilt*" -or $cols[$k] -like "*T*y*" -or $cols[$k] -like "*Stok hareket*") {
            Write-Host " Col $k : '$($cols[$k])'"
        }
    }

    Write-Host "`nCHECKING NON-EMPTY ROWS IN USER COLUMNS (Kalite, Cilt, Tuy, Renk, Urun, Diger):"
    $nonEmptyCount = 0
    for ($r = 2; $r -le $totalRows; $r++) {
        $c11 = $sheet.Cells.Item($r, 11).Text.Trim()
        $c73 = $sheet.Cells.Item($r, 73).Text.Trim()
        $c74 = $sheet.Cells.Item($r, 74).Text.Trim()
        $c75 = $sheet.Cells.Item($r, 75).Text.Trim()
        $c76 = $sheet.Cells.Item($r, 76).Text.Trim()
        $c77 = $sheet.Cells.Item($r, 77).Text.Trim()
        $c78 = $sheet.Cells.Item($r, 78).Text.Trim()

        if ($c73 -ne "" -or $c74 -ne "" -or $c75 -ne "" -or $c76 -ne "" -or $c77 -ne "" -or $c78 -ne "") {
            $nonEmptyCount++
            Write-Host " Row $r | GUID: '$c11' | Kalite: '$c73' | Cilt: '$c74' | Tüy: '$c75' | Renk: '$c76' | Ürün: '$c77' | Diğer: '$c78'"
        }
    }

    Write-Host "Total Rows With User Data in Excel: $nonEmptyCount"

    $wb.Close($false)
    $excel.Quit()
}
