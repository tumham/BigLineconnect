@echo off
echo ===================================================
echo BigLineconnect Modern Service Setup
echo ===================================================
echo.

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Lutfen bu dosyaya sag tiklayip "Yonetici Olarak Calistir" secenegini secin!
    echo.
    pause
    exit /b
)

echo [1/4] Eski servisler durduruluyor ve siliniyor...
sc stop BigLineconnectSvc >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete BigLineconnectSvc >nul 2>&1
timeout /t 2 /nobreak >nul
sc stop BigLineconnect >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete BigLineconnect >nul 2>&1
timeout /t 2 /nobreak >nul

echo [2/4] Calisan uygulamalar kapatiliyor...
taskkill /f /im BigLineconnect_modern.exe >nul 2>&1
taskkill /f /im BigLineconnect_v13.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo [3/4] Yeni servis kuruluyor (BigLineconnectSvc -> BigLineconnect_modern.exe)...
sc create BigLineconnectSvc binPath= "\"%~dp0BigLineconnect_modern.exe\" --service" start= auto
sc description BigLineconnectSvc "BigLineconnect Modern Uzaktan Kontrol Servisi"

echo [4/4] Servis baslatiliyor...
sc start BigLineconnectSvc
timeout /t 2 /nobreak >nul

echo.
echo ===================================================
echo BASARILI! Modern Servis kuruldu ve baslatildi.
echo ===================================================
echo.
pause
