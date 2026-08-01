$batContent = @"
@echo off
title Mikro Rapor Veri Doldurucu (user_ekle)
cls

echo =======================================================================
echo   MIKRO RAPOR VERI DOLDURUCU (user_ekle)
echo =======================================================================
echo.
echo  Bu arac Mikro'dan aldiginiz Excel raporuna Veritabanindaki
echo  Kalite, Cilt, Tuy, Renk, Urun ve Diger sutunlarini otomatik isler.
echo.
echo =======================================================================
echo.

set EXCEL_PATH=
set /p EXCEL_PATH="Lutfen Excel dosyasini buraya surukleyip birakin (veya Enter'a basin): "

if defined EXCEL_PATH set EXCEL_PATH=%EXCEL_PATH:"=%

if "%EXCEL_PATH%"=="" (
    set EXCEL_PATH=C:\mahmut\chekstresistokdetaylı.xlsx
)

if not exist "%EXCEL_PATH%" (
    echo.
    echo [!] HATA: Belirtilen Excel dosyasi bulunamadi!
    echo Yolu kontrol edip tekrar deneyin: %EXCEL_PATH%
    echo.
    pause
    exit /b
)

echo.
echo [*] Islem Baslatiliyor... Lutfen bekleyin...
echo [*] Hedef Dosya: "%EXCEL_PATH%"
echo.

powershell -ExecutionPolicy Bypass -File "%~dp0user_ekle_engine.ps1" -ExcelPath "%EXCEL_PATH%"

echo.
echo =======================================================================
echo   ISLEM BASARIYLA TAMAMLANDI!
echo   Excel dosyasi ekraniniza getiriliyor...
echo =======================================================================
echo.

start "" "%EXCEL_PATH%"

timeout /t 3 >nul
"@

[System.IO.File]::WriteAllText("C:\Users\MAHMUT\Desktop\user_ekle.bat", $batContent, [System.Text.Encoding]::ASCII)
Write-Host "user_ekle.bat written in clean ASCII encoding!"
