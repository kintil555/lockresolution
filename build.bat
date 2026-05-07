@echo off
:: Build LockResolution.exe
:: Butuh .NET Framework 4.8 atau .NET SDK terinstall

setlocal
set "OUTDIR=%~dp0dist"

echo.
echo ================================================
echo   LockResolution - Build
echo ================================================
echo.

:: Cek dotnet
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] .NET SDK tidak ditemukan.
    echo         Download dari: https://dotnet.microsoft.com/download
    pause & exit /b 1
)

echo [*] Compiling...

if not exist "%OUTDIR%" mkdir "%OUTDIR%"

dotnet publish src\LockResolution.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained false ^
    -o "%OUTDIR%" ^
    /p:PublishSingleFile=false

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Build gagal. Cek pesan error di atas.
    pause & exit /b 1
)

:: Copy batch scripts ke dist
copy /Y installer\install.bat "%OUTDIR%\install.bat" >nul
copy /Y scripts\setup.bat "%OUTDIR%\setup.bat" >nul

echo.
echo [OK] Build selesai! File ada di folder: dist\
echo.
echo File output:
dir /b "%OUTDIR%"
echo.
pause
