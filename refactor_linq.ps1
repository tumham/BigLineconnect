$files = Get-ChildItem -Path "c:\Projev17YD\DUZ_V17_STD\Bigus.Aktarici.Linq\" -Filter "*.cs"
foreach ($file in $files) {
    if ($file.Name -like "*Aktarim*.cs" -or $file.Name -like "*Kart*.cs") {
        $lines = Get-Content $file.FullName
        $newLines = @()
        $insideMethod = $false
        $addedUsing = $false
        $braceCount = 0

        foreach ($line in $lines) {
            if ($line -match "BigusAktarimDataContext\s+db\s*=\s*new\s+BigusAktarimDataContext\(conn\);") {
                $indent = $line.Substring(0, $line.IndexOf("BigusAktarimDataContext"))
                $newLines += ($indent + "using (BigusAktarimDataContext db = new BigusAktarimDataContext(conn))")
                $newLines += ($indent + "{")
                $addedUsing = $true
                $braceCount = 1
                $insideMethod = $true
            }
            elseif ($insideMethod) {
                # count occurrences of { and }
                $openBraces = ([regex]::Matches($line, "\{")).Count
                $closeBraces = ([regex]::Matches($line, "\}")).Count
                
                $braceCount += $openBraces
                $braceCount -= $closeBraces
                
                if ($braceCount -eq 0 -and $addedUsing) {
                    $indent = $line.Substring(0, $line.IndexOf("}"))
                    if ([string]::IsNullOrWhiteSpace($indent)) {
                        $newLines += "            }"
                    } else {
                        $newLines += ($indent + "    }")
                    }
                    $newLines += $line
                    $addedUsing = $false
                    $insideMethod = $false
                } else {
                    $newLines += $line
                }
            } else {
                $newLines += $line
            }
        }
        $newLines | Set-Content $file.FullName
        Write-Host "Processed $($file.Name)"
    }
}
Write-Host "Done"
