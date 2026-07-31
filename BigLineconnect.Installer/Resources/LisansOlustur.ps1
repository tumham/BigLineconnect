# BigLineconnect Lisans Oluşturucu (RSA-2048)
# Bu betik, müşterilerinize lisans anahtarı üretmek için kullanılır.

$privateKeyXml = @"
<RSAKeyValue><Modulus>uSHCnsWG/JNo9Hqp3KdUiruhzPNZiHzewKZ8oemQf0XSxOt2yJx/eaeXcyWigiVUNOZEev+t/acsAUjSMnF/u/8MMo43q+IL6Ex497r6GOzdpW1GYWivYgMITqY9O4zbJ1X16Mj4O6StALSwaXEgjkBtZ5J0874bOi9nme1hfptF6mA6/uw/WqQe1wq1YSJBHkQnIW0aBDMF/pas4PhNlIwJiZKRm2e04yqY+ONn/mqDigsyenx5qe88B8dcNic7h2qbhJnysdkyZaFP4eCqwNWylB6wmxCMKKktkBFuLQxKG3p78bEfkpVrYtIBHJ3zIoWziO87vTCOjH6XuW2fsQ==</Modulus><Exponent>AQAB</Exponent><P>3F9/YBKipwTy7IPcut2X8+PEWHEjBqLj0hEX1fd52WWnCbYn45ClxScHDpGOxSb1X/ffm22/qAW4DioHT8xlgmFEwCAnmpMZWqk6HlVuBPGc8ZNhnhBtIrDemOSLOOmt1/yWYgeY3Iv9prEXxSKDSfQpYx4lfqZCdeADIqDitg8=</P><Q>1w+/u3Xbd2aAFXbLfKPtwb6ih8/Qenc/wYzUTIEIFAUC1kqgrvH5QdrnARWaBwEfmFd6fYiDnjLDgCfCicLlXVrPMQxJpqlKQ37o1Tpc2OYSqcm9Gs2XrR+NHgAEU/Jxj9Gl7P5YM6UwD4X40cBm95SV4ZWq56HlVuBPGc8ZNhnhBtIrDemOSLOOmt1/yWYgeY3Iv9prEXxSKDSfQpYx4lfqZCdeADIqDitg8=</Q><DP>zFccxBfjjF1RZ7BJn1hUSwj7Cks2ADRQbXv+DyAfc08H2UB3slJw8+PhbkfYC8W5Jxiv1e6pFYLgDPo5t2u/AGtzFXd7YaBelStvwarTjm//aB2SGb//gnhgn4Lj+Yzs7ua/Bv0mZ/LC66swcFI1TQDC066jc9F0tNpX7eci8U8=</DP><DQ>HbsLonq0kFkXM+BsWRrAb5xPE79i7ss1gSha4QileT8IGV/Pvt+subHtZXT7CiTZnVHamSgaKfCSnlDgz/KlirZXFkzAkc2teo882N1soH+N7PL6tY3efgxykccm0gQeNuegrJas0tbvQfYfY7/ZfDiKT7V928FBlBTQP3ZHCjk=</DQ><InverseQ>ByodhDkjUYo8axUFdE0rg9VCKGwI2ZUBhlJxdwXF3JoDJKdCZ9zl0vTqlQBztA9fo9SnPzXl2xaGA+j4QU1jFdM3VicpDOti3gV8KvgPNxYpEz3URCjic4iy5fhRDpV+2ifXDc+/zwSA5oDXcfSs5GYAPaxo6R/DzykgiMHKVp8=</InverseQ><D>Gfrn4focWGOjbfOs1L9SuOld6nIYFXRYmjaC9QHe+k1lJ6dXkw2LQhpHwav9Y0Az0fyijZRF6XvFTCTZAqU05MAFCS82FtPxEddxfmpp4IeVDzsHjnHvctS0HL1tiGba9mk0ykcxtsUEVj4FJ7btd6kI8Wj3KqF6Fq9CXbKXZPnOOaNLVAZebtK4lyDnQzMny+kgoi3OTMKALZqhM8mw8w++/mhCeUroQ9yH9MU4S1rT4BesWgbg9kHildCOvjXcV+vAmgCT7sTiWcLa19YhXHKw/VHshUYZV1TAy469fOrRUz+vdCIOxn+USCCrdI+IE+lqFqYxg67E/zs3smzHlQ==</D></RSAKeyValue>
"@

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   BigLineconnect Lisans Oluşturucu      " -ForegroundColor Magenta
Write-Host "==========================================" -ForegroundColor Cyan

$expiry = Read-Host "Son Kullanma Tarihi girin (Format: YYYY-MM-DD, Örn: 2027-12-31)"
if ([string]::IsNullOrEmpty($expiry)) {
    $expiry = "2027-12-31"
}

$machineId = Read-Host "Uzak Bilgisayarın Benzersiz ID'sini girin (Her makinede çalışması için * girin)"
if ([string]::IsNullOrEmpty($machineId)) {
    $machineId = "*"
}

# Create JSON payload
$payload = '{"Expiry":"' + $expiry + '","MachineId":"' + $machineId + '"}'

# Sign via RSA
$rsa = [System.Security.Cryptography.RSACryptoServiceProvider]::new(2048)
$rsa.FromXmlString($privateKeyXml)

$dataBytes = [System.Text.Encoding]::UTF8.GetBytes($payload)
$sigBytes = $rsa.SignData($dataBytes, [System.Security.Cryptography.CryptoConfig]::MapNameToOID("SHA256"))
$signatureBase64 = [System.Convert]::ToBase64String($sigBytes)

$licenseKey = $payload + "." + $signatureBase64

Write-Host "`nÜRETİLEN LİSANS ANAHTARI:" -ForegroundColor Green
Write-Host "------------------------------------------"
Write-Host $licenseKey -ForegroundColor Yellow
Write-Host "------------------------------------------"

$save = Read-Host "Anahtarı 'license.key' dosyası olarak kaydetmek ister misiniz? (E/H)"
if ($save -eq "E" -or $save -eq "e" -or $save -eq "y" -or $save -eq "Y") {
    $licenseKey | Out-File -FilePath "license.key" -Encoding utf8
    Write-Host "Lisans başarıyla 'license.key' olarak kaydedildi." -ForegroundColor Green
}
