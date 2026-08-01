$excelFile = (Get-ChildItem -Path "C:\mahmut" -Filter "*chek*.xlsx")[0].FullName
Write-Host "Target Excel File: $excelFile"

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$wb = $excel.Workbooks.Open($excelFile)
$sheet = $wb.Sheets.Item(1)
$totalRows = $sheet.UsedRange.Rows.Count
$totalCols = $sheet.UsedRange.Columns.Count

Write-Host "Total Rows: $totalRows | Total Cols: $totalCols"

$guidsToFind = @(
    "56b2c92e-b8c7-4cca-b716-9d87cf763117",
    "c84274bb-604b-42dc-a792-aceaac4bce86",
    "8691cf22-6df2-4763-8588-ad7046451dca"
)

for ($r = 1; $r -le $totalRows; $r++) {
    $rowGuid = $sheet.Cells.Item($r, 11).Text.Trim().ToLower()
    if ($rowGuid -ne "") {
        foreach ($g in $guidsToFind) {
            if ($rowGuid.Contains($g)) {
                Write-Host "[MATCH FOUND AT ROW $r!] Col 11: '$rowGuid' matched GUID: $g"
            }
        }
    }
}

$wb.Close($false)
$excel.Quit()
