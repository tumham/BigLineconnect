@echo off
chcp 65001 >nul
echo ===================================================
echo   ⚡ BigLineconnect Hizmet & Uygulama Kurulumu (Sürüm 17.8)
echo ===================================================

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [!] Yönetici izinleri alınıyor...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

echo [*] Arka plan işlemleri ve eski servis durduruluyor...
sc.exe stop BigLineconnectSvc >nul 2>&1
taskkill /F /IM BigLineconnect.exe /T >nul 2>&1
timeout /t 1 /nobreak >nul

echo [*] Windows Servisi (BigLineconnectSvc) kaydediliyor...
sc.exe delete BigLineconnectSvc >nul 2>&1
sc.exe create BigLineconnectSvc binPath= "%~dp0BigLineconnect.exe --service" start= auto DisplayName= "BigLineconnect Background Service"
sc.exe config BigLineconnectSvc start= auto
sc.exe failure BigLineconnectSvc reset= 0 actions= restart/3000/restart/3000/restart/3000
sc.exe start BigLineconnectSvc

echo [*] Ana uygulama başlatılıyor...
start "" "%~dp0BigLineconnect.exe"

echo ===================================================
echo   ✅ Kurulum Başarıyla Tamamlandı!
echo ===================================================
timeout /t 2 >nul
