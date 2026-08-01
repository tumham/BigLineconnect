$excelFiles = Get-ChildItem -Path "C:\mahmut" -Filter "*.xlsx"

foreach ($f in $excelFiles) {
    Write-Host "==================================================="
    Write-Host "FILE: $($f.FullName)"
    Write-Host "==================================================="

    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $wb = $excel.Workbooks.Open($f.FullName)
    $sheet = $wb.Sheets.Item(1)
    $totalCols = $sheet.UsedRange.Columns.Count
    $totalRows = $sheet.UsedRange.Rows.Count

    Write-Host "Total Rows: $totalRows | Total Cols: $totalCols"
    
    # Headers
    Write-Host "HEADERS:"
    for ($c = 1; $c -le [Math]::Min(30, $totalCols); $c++) {
        $val = $sheet.Cells.Item(1, $c).Text
        if ($val) {
            Write-Host " Col $c : '$val'"
        }
    }

    # Inspect first 10 rows Col 11 and Col 12 and Col 3
    Write-Host "`nSAMPLE ROWS (First 10):"
    for ($r = 2; $r -le [Math]::Min(11, $totalRows); $r++) {
        $c3  = $sheet.Cells.Item($r, 3).Text
        $c11 = $sheet.Cells.Item($r, 11).Text
        $c12 = $sheet.Cells.Item($r, 12).Text
        Write-Host " Row $r | Col 3 (EvrakNo): '$c3' | Col 11 (KayıtNo): '$c11' | Col 12 (StokKodu): '$c12'"
    }

    $wb.Close($false)
    $excel.Quit()
}
