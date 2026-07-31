@echo off
echo ===================================================
echo BigLineconnect Service Setup (New Version)
echo ===================================================
echo.

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [ERROR] Please right click this file and select "Run as Administrator"!
    echo.
    pause
    exit /b
)

echo [1/4] Stopping and deleting old services...
sc stop BigLineconnectSvc >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete BigLineconnectSvc >nul 2>&1
timeout /t 2 /nobreak >nul
sc stop BigLineconnect >nul 2>&1
timeout /t 2 /nobreak >nul
sc delete BigLineconnect >nul 2>&1
timeout /t 2 /nobreak >nul

echo [2/4] Killing running processes...
taskkill /f /im BigLineconnect_v13.exe >nul 2>&1
taskkill /f /im BigLineconnect_v12.exe >nul 2>&1
timeout /t 1 /nobreak >nul

echo [3/4] Creating new service (BigLineconnectSvc)...
sc create BigLineconnectSvc binPath= "\"%~dp0BigLineconnect_v13.exe\" --service" start= auto
sc description BigLineconnectSvc "BigLineconnect Uzaktan Kontrol Servisi"

echo [4/4] Starting service...
sc start BigLineconnectSvc
timeout /t 2 /nobreak >nul

echo.
echo ===================================================
echo SUCCESS! Service has been installed and started.
echo You can close this window now and connect!
echo ===================================================
echo.
pause
