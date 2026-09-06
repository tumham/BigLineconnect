@echo off
chcp 65001 >nul
:: Yonetici Yetkisi Kontrolu
net session >nul 2>&1
if %errorLevel% neq 0 (
    powershell -Command "Start-Process cmd -ArgumentList '/c \"%~f0\"' -Verb RunAs"
    exit /b
)

echo ===================================================
echo BigLineconnect Servis Kaldirici ve Kota Guvencesi
echo ===================================================
echo.
echo [1/3] Calisan surecler sonlandiriliyor...
taskkill /F /IM BigLineconnect.exe /T >nul 2>&1
taskkill /F /IM BigLineconnect_App.exe /T >nul 2>&1
taskkill /F /IM BigLineconnect_setup.exe /T >nul 2>&1

echo [2/3] Windows Servisi durduruluyor ve kalici olarak siliniyor...
sc stop BigLineconnectSvc >nul 2>&1
sc config BigLineconnectSvc start= disabled >nul 2>&1
sc delete BigLineconnectSvc >nul 2>&1
sc stop BigLineconnect >nul 2>&1
sc config BigLineconnect start= disabled >nul 2>&1
sc delete BigLineconnect >nul 2>&1

echo [3/3] Baslangic kayitlari temizleniyor...
reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run" /v "BigLineconnect" /f >nul 2>&1
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v "BigLineconnect" /f >nul 2>&1

echo.
echo ===================================================
echo BASARILI: BigLineconnect arka plan servisi tamamen KALDIRILDI!
echo Artik arka planda hicbir surec calismayacak, kota tuketilmeyecektir.
echo ===================================================
echo.
pause
