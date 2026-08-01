$renkCols = 1..60 | ForEach-Object { "sr.rnk_kirilim_$_" }
$bedenCols = 1..60 | ForEach-Object { "sb.bdn_kirilim_$_" }

$renkChoose = "ISNULL(CHOOSE(h.sth_renk_no, " + ($renkCols -join ", ") + "), '') AS Renk"
$bedenChoose = "ISNULL(CHOOSE(b.BdnHar_BedenNo, " + ($bedenCols -join ", ") + "), '') AS Beden"

Write-Output $renkChoose
Write-Output $bedenChoose
