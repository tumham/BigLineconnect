@echo off
chcp 65001 >nul
echo ===================================================
echo BigLineconnect Kalıntı ve Süreç Temizleme Aracı
echo ===================================================

echo [1/3] Çalışan eski BigLineconnect süreçleri kapatılıyor...
taskkill /F /IM BigLineconnect.exe /T 2>nul
taskkill /F /IM BigLineconnect_App.exe /T 2>nul

echo [2/3] Eski Windows servisleri durduruluyor...
sc stop BigLineconnectSvc 2>nul
sc delete BigLineconnectSvc 2>nul
sc stop BigLineconnect 2>nul
sc delete BigLineconnect 2>nul

echo [3/3] Eski oturum bayrakları ve geçici dosyalar temizleniyor...
del /F /Q "C:\ProgramData\BigLineconnect\active_stream.flag" 2>nul
del /F /Q "%TEMP%\BigLineconnect*" 2>nul

echo.
echo ===================================================
echo TEMİZLİK TAMAMLANDI! Sistem sıfırlandı.
echo Artık yeni BigLineconnect.exe'yi başlatabilirsiniz.
echo ===================================================
pause
