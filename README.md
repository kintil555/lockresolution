# LockResolution

**Windows Service ringan** untuk mengunci resolusi layar agar tidak berubah setelah restart/boot.

Cocok untuk laptop/PC yang resolusinya balik ke native (misal 2560x1600) padahal kamu mau tetap di 1920x1080.

---

## ✅ Fitur

- Berjalan sebagai **Windows Service** — otomatis aktif sejak boot, tidak perlu login
- Monitor resolusi setiap N detik, langsung restore kalau berubah
- Support **multi-monitor** — pilih monitor mana yang mau dikunci
- Konfigurasi sepenuhnya via **CMD** (tidak perlu GUI)
- Sangat ringan — tidak ada dependency tambahan selain .NET Framework 4.8

---

## 📦 Requirement

- Windows 10 / 11
- .NET Framework 4.8 (sudah include di Windows 10/11)
- Jika mau build sendiri: [.NET SDK](https://dotnet.microsoft.com/download)

---

## 🚀 Cara Install

### Opsi A: Download Release (Recommended)
1. Download `LockResolution.exe`, `install.bat`, `setup.bat` dari [Releases](../../releases)
2. Letakkan semua file di satu folder (misal `C:\Tools\LockResolution\`)
3. Jalankan `install.bat` sebagai **Administrator** → pilih [1] Install
4. Selesai! Service langsung aktif

### Opsi B: Build Sendiri
```
git clone https://github.com/kintil555/lockresolution
cd lockresolution
build.bat
```
Hasil build ada di folder `dist\`

---

## 💻 Penggunaan via CMD

> Buka CMD / PowerShell, **cd** ke folder LockResolution.exe

### Set resolusi target
```cmd
LockResolution.exe set 1920 1080
```

### Set resolusi untuk monitor tertentu (multi-monitor)
```cmd
LockResolution.exe set 1920 1080 0    ← monitor utama (index 0)
LockResolution.exe set 1920 1080 1    ← monitor kedua (index 1)
```

### Lihat semua monitor yang terdeteksi
```cmd
LockResolution.exe list
```
Output:
```
Detected monitors:
  [0] \\.\DISPLAY1  (2560x1600)
  [1] \\.\DISPLAY2  (1920x1080)
```

### Lihat status & konfigurasi sekarang
```cmd
LockResolution.exe status
```
Output:
```
Lock Target : 1920x1080
Current     : 1920x1080
Monitor     : 0
Interval    : 10s
Enabled     : True
Config file : C:\Tools\LockResolution\lockresolution.cfg
Log file    : C:\Tools\LockResolution\lockresolution.log
```

### Apply resolusi sekarang (tanpa nunggu interval)
```cmd
LockResolution.exe apply
```

### Ubah interval cek (default: 10 detik)
```cmd
LockResolution.exe interval 5    ← cek setiap 5 detik
LockResolution.exe interval 30   ← cek setiap 30 detik
```

### Enable / Disable lock sementara
```cmd
LockResolution.exe disable   ← pause lock, resolusi bebas berubah
LockResolution.exe enable    ← aktifkan kembali
```

---

## ⚡ Quick Setup (pakai setup.bat)

Jalankan `setup.bat` sebagai Administrator untuk menu interaktif:
```
[1] Set 1920x1080
[2] Set 2560x1440
[3] Set 3840x2160
[4] Custom
[5] Status
...
```

---

## ⚙️ File Konfigurasi

Config disimpan di `lockresolution.cfg` (di folder yang sama dengan .exe):
```
width=1920
height=1080
interval=10
monitor=0
enabled=True
```

Bisa diedit manual atau via CMD.

---

## 📋 Manage Service (CMD Administrator)

```cmd
sc start LockResolution      ← start service
sc stop LockResolution       ← stop service
sc query LockResolution      ← cek status
```

Atau pakai `install.bat` → pilih [2] Uninstall untuk hapus service.

---

## 🔍 Log

Service mencatat setiap kali resolusi di-restore di file `lockresolution.log`:
```
[2025-08-01 08:00:15] Service started.
[2025-08-01 08:00:25] Resolution mismatch detected (2560x1600). Restoring to 1920x1080...
[2025-08-01 08:00:25] Resolution restored.
```

---

## ❓ FAQ

**Q: Kenapa resolusi balik ke native setelah restart?**  
A: Beberapa driver GPU (terutama Intel & AMD di laptop) override resolusi saat boot. Service ini akan deteksi dan restore dalam hitungan detik.

**Q: Apakah aman?**  
A: Ya. Service hanya memanggil Windows API `ChangeDisplaySettings` — API standar yang sama dipakai aplikasi display setting bawaan Windows.

**Q: Service tidak mau start?**  
A: Pastikan .exe ada di folder yang tidak butuh UAC setiap akses (hindari `Program Files`). Disarankan taruh di `C:\Tools\LockResolution\`.
