$file = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
Write-Host "Opening file: $file"
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open($file)
$sheet = $wb.Sheets.Item(1)
$cols = $sheet.UsedRange.Columns.Count
$rows = $sheet.UsedRange.Rows.Count
Write-Host "Total Rows: $rows, Total Cols: $cols"
for ($i = 1; $i -le $cols; $i++) {
    $val = $sheet.Cells.Item(1, $i).Text
    Write-Host "$i : $val"
}
$wb.Close($false)
$excel.Quit()
