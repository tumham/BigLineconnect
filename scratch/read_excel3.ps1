$file = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
Write-Host "Opening file: $file"
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open($file)
$sheet = $wb.Sheets.Item(1)
$cols = $sheet.UsedRange.Columns.Count
$rows = $sheet.UsedRange.Rows.Count
Write-Host "Total Rows: $rows, Total Cols: $cols"

for ($r = 1; $r -le [Math]::Min(5, $rows); $r++) {
    Write-Host "--- ROW $r ---"
    for ($c = 70; $c -le $cols; $c++) {
        $val = $sheet.Cells.Item($r, $c).Text
        $h = $sheet.Cells.Item(1, $c).Text
        Write-Host "Col ${c} (${h}): ${val}"
    }
}
$wb.Close($false)
$excel.Quit()
