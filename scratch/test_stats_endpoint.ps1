try {
    $res = Invoke-WebRequest -Uri "http://destek.bigus.com.tr:5080/api/support/stats" -UseBasicParsing
    Write-Host "Success:" $res.Content
} catch {
    Write-Host "Caught WebException:"
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        Write-Host "Response Body:" $reader.ReadToEnd()
    } else {
        Write-Host "Error:" $_.Exception.Message
    }
}
