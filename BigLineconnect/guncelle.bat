@echo off
setlocal enabledelayedexpansion

net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo =========================================================
    echo ERROR: Please right click this file and select
    echo        "Run as Administrator"
    echo =========================================================
    echo.
    pause
    exit /b
)

echo ===================================================
echo BigLineconnect Dynamic Update Sihirbazi
echo ===================================================
echo.

:: Detect folder path dynamically using sc qc
set RawPath=
for /f "tokens=2* delims=:" %%a in ('sc qc BigLineconnect 2^>nul ^| findstr BINARY_PATH_NAME') do (
    set RawPath=%%b
)

if "!RawPath!"=="" (
    echo HATA: BigLineconnect service not found!
    pause
    exit /b
)

:: Trim leading spaces
for /f "tokens=*" %%x in ("!RawPath!") do set TempPath=%%x

:: Strip arguments and quotes
set TempPath=!TempPath: --service=!
set TempPath=!TempPath: --session-helper=!
set TempPath=!TempPath:"=!
:: Trim trailing spaces
for /f "tokens=*" %%x in ("!TempPath!") do set TempPath=%%x

:: Extract directory path
for %%F in ("!TempPath!") do set ServDir=%%~dpF

if "!ServDir!"=="" (
    echo ERROR: Could not determine BigLineconnect installation path!
    pause
    exit /b
)

echo Target Directory Resolved to: !ServDir!
echo.

:: Resolve source file location
set SrcFile=
if exist "%~dp0BigLineconnect_v13.exe" (
    set SrcFile=%~dp0BigLineconnect_v13.exe
)

if "!SrcFile!"=="" (
    echo ERROR: Source BigLineconnect_v13.exe not found in current folder!
    pause
    exit /b
)

echo Source File Found at: !SrcFile!
echo.

echo 1. Stopping BigLineconnect Service...
net stop BigLineconnect
taskkill /f /im BigLineconnect_v13.exe
timeout /t 2 /nobreak >nul

echo.
echo 2. Updating files...
copy /y "!SrcFile!" "!ServDir!BigLineconnect_v13.exe"
if not exist "!ServDir!publish" mkdir "!ServDir!publish"
copy /y "!SrcFile!" "!ServDir!publish\BigLineconnect_v13.exe"

echo.
echo 3. Starting BigLineconnect Service...
net start BigLineconnect

echo.
echo ===================================================
echo Update Completed Successfully!
echo ===================================================
echo.
pause
