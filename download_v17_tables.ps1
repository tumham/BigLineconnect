$targetDir = "C:\Projev17YD\DUZ_V17_STD\V17\tablolar"
if (!(Test-Path $targetDir)) {
    New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
}

$html = Get-Content -Path "C:\Projev17YD\DUZ_V17_STD\tablo.htm" -Raw
$htmlClean = $html -replace "\r?\n", " "
$rows = [regex]::Matches($htmlClean, '<tr[^>]*>(.*?)<\/tr>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)

$v17Tables = @{}
foreach ($r in $rows) {
    $rowText = $r.Groups[1].Value
    $tds = [regex]::Matches($rowText, '<td[^>]*>(.*?)<\/td>', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($tds.Count -ge 3) {
        $noText = ($tds[0].Groups[1].Value -replace '<[^>]*>', '').Trim()
        if ($noText -match '^\d+$') {
            $tableName = ($tds[1].Groups[1].Value -replace '<[^>]*>', '').Trim().ToUpper()
            $td3 = $tds[2].Groups[1].Value
            $mUrl = [regex]::Match($td3, 'href="([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($mUrl.Success) {
                $url = $mUrl.Groups[1].Value.Trim()
                if ($tableName) {
                    $v17Tables[$tableName] = "https://www.ozgurguler.net/blog/MikroV17/" + $url
                }
            }
        }
    }
}

$dbContextContent = Get-Content -Path "C:\Bigus_MAUI\Bigus_MAUI\Data\Bigus.Data.Mikro\MikroV16DbContext.cs" -Raw
$dbMatches = [regex]::Matches($dbContextContent, 'entity\.ToTable\("([^"]+)"\)')
$mappedTables = foreach ($m in $dbMatches) { $m.Groups[1].Value.ToUpper() }
$mappedTables = $mappedTables | Sort-Object -Unique

$downloadList = @()
foreach ($tbl in $mappedTables) {
    if ($v17Tables.ContainsKey($tbl)) {
        $downloadList += [PSCustomObject]@{
            TableName = $tbl
            Url = $v17Tables[$tbl]
        }
    }
}

Write-Host "Total matched tables to download: $($downloadList.Count)"

$count = 0
foreach ($tbl in $downloadList) {
    $tableName = $tbl.TableName.ToLower()
    $url = $tbl.Url
    $dest = Join-Path $targetDir "$tableName.htm"
    
    try {
        if (!(Test-Path $dest)) {
            Invoke-WebRequest -Uri $url -OutFile $dest -TimeoutSec 15 -ErrorAction Stop
            $count++
            if ($count % 10 -eq 0) {
                Write-Host "Downloaded $count files..."
            }
        }
    } catch {
        Write-Host "Failed to download $tableName from $url : $_"
    }
}

Write-Host "Download process finished. Total new files downloaded: $count"
