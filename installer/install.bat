@echo off
:: LockResolution - Service Installer
:: Jalankan sebagai Administrator!

setlocal
set "SVCNAME=LockResolution"
set "EXEPATH=%~dp0LockResolution.exe"
set "DISPLAYNAME=Lock Resolution Service"
set "DESC=Menjaga resolusi layar tetap terkunci setelah boot/restart."

:: Cek admin
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Jalankan script ini sebagai Administrator!
    pause & exit /b 1
)

if not exist "%EXEPATH%" (
    echo [ERROR] LockResolution.exe tidak ditemukan di folder ini.
    echo         Pastikan LockResolution.exe ada di folder yang sama dengan install.bat
    pause & exit /b 1
)

echo.
echo ================================================
echo   LockResolution - Installer
echo ================================================
echo.
echo [1] Install Service
echo [2] Uninstall Service
echo [3] Keluar
echo.
set /p choice="Pilih (1/2/3): "

if "%choice%"=="1" goto INSTALL
if "%choice%"=="2" goto UNINSTALL
goto END

:INSTALL
echo.
echo [*] Menginstall service...

:: Stop & hapus kalau sudah ada
sc query "%SVCNAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo [*] Service sudah ada, menghapus dulu...
    sc stop "%SVCNAME%" >nul 2>&1
    timeout /t 2 >nul
    sc delete "%SVCNAME%" >nul 2>&1
    timeout /t 1 >nul
)

:: Install service
sc create "%SVCNAME%" binPath= "\"%EXEPATH%\"" start= auto DisplayName= "%DISPLAYNAME%"
if %errorlevel% neq 0 (
    echo [ERROR] Gagal membuat service!
    pause & exit /b 1
)

:: Set deskripsi
sc description "%SVCNAME%" "%DESC%"

:: Start service
sc start "%SVCNAME%"
if %errorlevel% neq 0 (
    echo [WARN] Service terinstall tapi gagal distart. Coba restart manual.
) else (
    echo [OK] Service berhasil diinstall dan dijalankan!
)

echo.
echo Gunakan CMD untuk mengatur resolusi:
echo   LockResolution.exe set 1920 1080
echo   LockResolution.exe status
echo.
pause
goto END

:UNINSTALL
echo.
echo [*] Menghapus service...
sc stop "%SVCNAME%" >nul 2>&1
timeout /t 2 >nul
sc delete "%SVCNAME%"
if %errorlevel% equ 0 (
    echo [OK] Service berhasil dihapus.
) else (
    echo [ERROR] Gagal menghapus service.
)
echo.
pause

:END
endlocal
