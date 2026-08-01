@echo off
chcp 65001 >nul
title BigLineconnect Lisans Oluşturucu
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0BigLineconnect.Installer\Resources\LisansOlustur.ps1"
pause
