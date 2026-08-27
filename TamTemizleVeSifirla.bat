@echo off
chcp 65001 >nul
title BigLineconnect %100 Tam Temizleyici
echo ================================================================
echo   BigLineconnect %100 Garanti Temizleyici ve Sifirlayici
echo ================================================================
echo.
echo [1/4] Windows Servisleri Kaldiriliyor...
sc.exe stop BigLineconnectSvc >nul 2>&1
sc.exe delete BigLineconnectSvc >nul 2>&1
sc.exe stop BigLineTransferSvc >nul 2>&1
sc.exe delete BigLineTransferSvc >nul 2>&1

echo [2/4] Calisan Surecler Kapatiliyor...
taskkill /F /IM BigLineconnect* /T >nul 2>&1
taskkill /F /IM BigLineTransfer* /T >nul 2>&1
taskkill /F /IM EastDesktop* /T >nul 2>&1

echo [3/4] Klasor ve Cache Dosyalari Temizleniyor...
rmdir /S /Q "%ProgramData%\BigLineconnect" >nul 2>&1
rmdir /S /Q "%LocalAppData%\BigLineconnect" >nul 2>&1
rmdir /S /Q "%AppData%\BigLineconnect" >nul 2>&1

echo [4/4] Baslangic Kayitlari Temizleniyor...
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v BigLineconnect /f >nul 2>&1
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v BigLineconnect /f >nul 2>&1
reg delete "HKCU\Software\BigLineconnect" /f >nul 2>&1
reg delete "HKLM\Software\BigLineconnect" /f >nul 2>&1

echo.
echo ================================================================
echo   TEBRIKLER! ESKIYE AIT HICBIR KIRINTI VE SUREC KALMADI.
echo ================================================================
echo.
pause
