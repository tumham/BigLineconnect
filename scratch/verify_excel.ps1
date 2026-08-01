$excelFile = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open($excelFile)
$sheet = $wb.Sheets.Item(1)

Write-Host "--- CHECKING ROW 59 IN EXCEL ---"
Write-Host "Col 3 (Evrak): " $sheet.Cells.Item(59, 3).Text
Write-Host "Col 12 (Stok): " $sheet.Cells.Item(59, 12).Text
Write-Host "Col 73 (Kalite): " $sheet.Cells.Item(59, 73).Text
Write-Host "Col 74 (Cilt): " $sheet.Cells.Item(59, 74).Text
Write-Host "Col 75 (Tüy): " $sheet.Cells.Item(59, 75).Text
Write-Host "Col 76 (Renk): " $sheet.Cells.Item(59, 76).Text
Write-Host "Col 77 (Ürün): " $sheet.Cells.Item(59, 77).Text
Write-Host "Col 78 (Diğer): " $sheet.Cells.Item(59, 78).Text

$wb.Close($false)
$excel.Quit()
