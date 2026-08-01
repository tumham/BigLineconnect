$excelFile = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open($excelFile)
$sheet = $wb.Sheets.Item(1)
$totalRows = $sheet.UsedRange.Rows.Count

Write-Host "--- SEARCHING GUIDS IN EXCEL ---"
$g1 = "c84274bb-604b-42dc-a792-aceaac4bce86"
$g2 = "8691cf22-6df2-4763-8588-ad7046451dca"

for ($r = 2; $r -le $totalRows; $r++) {
    $val = $sheet.Cells.Item($r, 11).Text.Trim().ToLower()
    if ($val.Contains("8691") -or $val.Contains("c842") -or $val.Contains("56b2")) {
        Write-Host "FOUND MATCH AT ROW $r | Col 11: '$val'"
    }
}

$wb.Close($false)
$excel.Quit()
