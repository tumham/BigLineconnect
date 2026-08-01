$excelFile = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open($excelFile)
$sheet = $wb.Sheets.Item(1)
$totalRows = $sheet.UsedRange.Rows.Count

Write-Host "--- PRINTING ROWS 45 TO 65 OF EXCEL ---"
for ($r = 45; $r -le [Math]::Min(65, $totalRows); $r++) {
    $c3 = $sheet.Cells.Item($r, 3).Text.Trim()
    $c11 = $sheet.Cells.Item($r, 11).Text.Trim()
    $c12 = $sheet.Cells.Item($r, 12).Text.Trim()
    $c73 = $sheet.Cells.Item($r, 73).Text.Trim()
    $c76 = $sheet.Cells.Item($r, 76).Text.Trim()
    Write-Host "Row $r | Col 3 (Evrak): '$c3' | Col 11 (RecNo): '$c11' | Col 12 (Stok): '$c12' | Col 73 (Kalite): '$c73' | Col 76 (Renk): '$c76'"
}

$wb.Close($false)
$excel.Quit()
