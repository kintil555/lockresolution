@echo off
:: LockResolution - Quick Config via CMD
:: Jalankan sebagai Administrator untuk restart service

setlocal
set "EXEPATH=%~dp0LockResolution.exe"

if not exist "%EXEPATH%" (
    echo [ERROR] LockResolution.exe tidak ditemukan!
    pause & exit /b 1
)

echo.
echo ================================================
echo   LockResolution - Quick Setup
echo ================================================
echo.

:MENU
echo Pilihan:
echo   [1] Set resolusi 1920x1080 (Full HD)
echo   [2] Set resolusi 2560x1440 (QHD)
echo   [3] Set resolusi 3840x2160 (4K)
echo   [4] Set resolusi custom
echo   [5] Lihat status
echo   [6] List monitor
echo   [7] Apply resolusi sekarang
echo   [8] Keluar
echo.
set /p choice="Pilih: "

if "%choice%"=="1" ( "%EXEPATH%" set 1920 1080 & goto RESTART )
if "%choice%"=="2" ( "%EXEPATH%" set 2560 1440 & goto RESTART )
if "%choice%"=="3" ( "%EXEPATH%" set 3840 2160 & goto RESTART )
if "%choice%"=="4" goto CUSTOM
if "%choice%"=="5" ( "%EXEPATH%" status & echo. & pause & goto MENU )
if "%choice%"=="6" ( "%EXEPATH%" list & echo. & pause & goto MENU )
if "%choice%"=="7" ( "%EXEPATH%" apply & echo. & pause & goto MENU )
if "%choice%"=="8" goto END
goto MENU

:CUSTOM
set /p cw="Width  (contoh: 1920): "
set /p ch="Height (contoh: 1080): "
if "%cw%"=="" ( echo [ERROR] Width tidak boleh kosong! & goto MENU )
if "%ch%"=="" ( echo [ERROR] Height tidak boleh kosong! & goto MENU )
set /p cm="Monitor index (0=utama, enter=skip): "
if "%cm%"=="" (
    "%EXEPATH%" set %cw% %ch%
) else (
    "%EXEPATH%" set %cw% %ch% %cm%
)

:RESTART
echo.
echo [*] Merestart service agar interval baru berlaku...
net session >nul 2>&1
if %errorlevel% equ 0 (
    sc stop LockResolution >nul 2>&1
    timeout /t 2 >nul
    sc start LockResolution >nul 2>&1
    echo [OK] Service direstart.
) else (
    echo [INFO] Jalankan sebagai Admin untuk restart service otomatis.
)
echo.
pause
goto MENU

:END
endlocal
