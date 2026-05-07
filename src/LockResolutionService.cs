using System;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.IO;

namespace LockResolution
{
    public class LockResolutionService : ServiceBase
    {
        private Timer _timer;
        private static readonly string ConfigPath = Path.Combine(
            AppContext.BaseDirectory,
            "lockresolution.cfg"
        );
        private static readonly string LogPath = Path.Combine(
            AppContext.BaseDirectory,
            "lockresolution.log"
        );

        // ── Win32 Display API ──────────────────────────────────────────────

        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        const int ENUM_CURRENT_SETTINGS = -1;
        const int CDS_UPDATEREGISTRY     = 0x01;
        const int CDS_NORESET            = 0x10;
        const int DISP_CHANGE_SUCCESSFUL  = 0;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
            public int dmFields;
            public int dmPositionX, dmPositionY;
            public int dmDisplayOrientation, dmDisplayFixedOutput;
            public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel, dmPelsWidth, dmPelsHeight;
            public int dmDisplayFlags, dmDisplayFrequency;
            public int dmICMMethod, dmICMIntent, dmMediaType, dmDitherType;
            public int dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]  public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        // ── Config ─────────────────────────────────────────────────────────

        public static Config LoadConfig()
        {
            var cfg = new Config { Width = 1920, Height = 1080, Interval = 10, Enabled = true, Monitor = 0 };
            if (!File.Exists(ConfigPath)) { SaveConfig(cfg); return cfg; }

            foreach (var line in File.ReadAllLines(ConfigPath))
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                var k = parts[0].Trim().ToLower();
                var v = parts[1].Trim();
                if (k == "width"    && int.TryParse(v, out int w))  cfg.Width    = w;
                if (k == "height"   && int.TryParse(v, out int h))  cfg.Height   = h;
                if (k == "interval" && int.TryParse(v, out int i))  cfg.Interval = Math.Max(1, i);
                if (k == "monitor"  && int.TryParse(v, out int m))  cfg.Monitor  = m;
                if (k == "enabled") cfg.Enabled = v.ToLower() == "true" || v == "1";
            }
            return cfg;
        }

        public static void SaveConfig(Config cfg)
        {
            File.WriteAllText(ConfigPath,
                $"width={cfg.Width}\nheight={cfg.Height}\ninterval={cfg.Interval}\nmonitor={cfg.Monitor}\nenabled={cfg.Enabled}\n");
        }

        // ── Resolution Lock ────────────────────────────────────────────────

        static string GetDeviceName(int monitorIndex)
        {
            var dd = new DISPLAY_DEVICE();
            dd.cb = Marshal.SizeOf(dd);
            if (EnumDisplayDevices(null, (uint)monitorIndex, ref dd, 0))
                return dd.DeviceName;
            return null;
        }

        public static bool SetResolution(int width, int height, int monitorIndex = 0)
        {
            string deviceName = GetDeviceName(monitorIndex);
            if (deviceName == null) return false;

            var dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(dm);
            if (!EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm)) return false;

            if (dm.dmPelsWidth == width && dm.dmPelsHeight == height) return true; // already set

            dm.dmPelsWidth  = width;
            dm.dmPelsHeight = height;
            dm.dmFields     = 0x80000 | 0x100000; // DM_PELSWIDTH | DM_PELSHEIGHT

            int result = ChangeDisplaySettings(ref dm, CDS_UPDATEREGISTRY | CDS_NORESET);
            ChangeDisplaySettings(ref dm, 0); // apply
            return result == DISP_CHANGE_SUCCESSFUL;
        }

        public static (int w, int h) GetCurrentResolution(int monitorIndex = 0)
        {
            string deviceName = GetDeviceName(monitorIndex);
            if (deviceName == null) return (0, 0);
            var dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(dm);
            EnumDisplaySettings(deviceName, ENUM_CURRENT_SETTINGS, ref dm);
            return (dm.dmPelsWidth, dm.dmPelsHeight);
        }

        // ── Service Lifecycle ──────────────────────────────────────────────

        public LockResolutionService() { ServiceName = "LockResolution"; }

        protected override void OnStart(string[] args)
        {
            Log("Service started.");
            var cfg = LoadConfig();
            int intervalMs = cfg.Interval * 1000;
            _timer = new Timer(CheckAndLock, null, 2000, intervalMs);
        }

        protected override void OnStop()
        {
            _timer?.Dispose();
            Log("Service stopped.");
        }

        private void CheckAndLock(object state)
        {
            var cfg = LoadConfig();
            if (!cfg.Enabled) return;

            var (cw, ch) = GetCurrentResolution(cfg.Monitor);
            if (cw != cfg.Width || ch != cfg.Height)
            {
                Log($"Resolution mismatch detected ({cw}x{ch}). Restoring to {cfg.Width}x{cfg.Height}...");
                bool ok = SetResolution(cfg.Width, cfg.Height, cfg.Monitor);
                Log(ok ? "Resolution restored." : "Failed to restore resolution!");
            }
        }

        static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n"); }
            catch { }
        }

        // ── Entry Point ────────────────────────────────────────────────────

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                HandleCLI(args);
                return;
            }
            ServiceBase.Run(new LockResolutionService());
        }

        static void HandleCLI(string[] args)
        {
            string cmd = args[0].ToLower().TrimStart('-', '/');

            switch (cmd)
            {
                case "set":
                    if (args.Length >= 3
                        && int.TryParse(args[1], out int w)
                        && int.TryParse(args[2], out int h))
                    {
                        var cfg = LoadConfig();
                        cfg.Width  = w;
                        cfg.Height = h;
                        if (args.Length >= 4 && int.TryParse(args[3], out int m)) cfg.Monitor = m;
                        SaveConfig(cfg);
                        Console.WriteLine($"[OK] Lock resolution set to {w}x{h} (Monitor {cfg.Monitor})");
                    }
                    else Console.WriteLine("Usage: LockResolution.exe set <width> <height> [monitor_index]");
                    break;

                case "interval":
                    if (args.Length >= 2 && int.TryParse(args[1], out int sec))
                    {
                        var cfg = LoadConfig();
                        cfg.Interval = Math.Max(1, sec);
                        SaveConfig(cfg);
                        Console.WriteLine($"[OK] Check interval set to {cfg.Interval} second(s)");
                    }
                    else Console.WriteLine("Usage: LockResolution.exe interval <seconds>");
                    break;

                case "enable":
                    { var cfg = LoadConfig(); cfg.Enabled = true; SaveConfig(cfg); Console.WriteLine("[OK] Lock enabled"); }
                    break;

                case "disable":
                    { var cfg = LoadConfig(); cfg.Enabled = false; SaveConfig(cfg); Console.WriteLine("[OK] Lock disabled"); }
                    break;

                case "status":
                    {
                        var cfg = LoadConfig();
                        var (cw, ch) = GetCurrentResolution(cfg.Monitor);
                        Console.WriteLine($"Lock Target : {cfg.Width}x{cfg.Height}");
                        Console.WriteLine($"Current     : {cw}x{ch}");
                        Console.WriteLine($"Monitor     : {cfg.Monitor}");
                        Console.WriteLine($"Interval    : {cfg.Interval}s");
                        Console.WriteLine($"Enabled     : {cfg.Enabled}");
                        Console.WriteLine($"Config file : {ConfigPath}");
                        Console.WriteLine($"Log file    : {LogPath}");
                    }
                    break;

                case "apply":
                    {
                        var cfg = LoadConfig();
                        bool ok = SetResolution(cfg.Width, cfg.Height, cfg.Monitor);
                        Console.WriteLine(ok
                            ? $"[OK] Resolution applied: {cfg.Width}x{cfg.Height}"
                            : "[FAIL] Could not apply resolution.");
                    }
                    break;

                case "list":
                    {
                        Console.WriteLine("Detected monitors:");
                        for (int i = 0; i < 8; i++)
                        {
                            var name = GetDeviceName(i);
                            if (name == null) break;
                            var (cw, ch) = GetCurrentResolution(i);
                            Console.WriteLine($"  [{i}] {name}  ({cw}x{ch})");
                        }
                    }
                    break;

                default:
                    Console.WriteLine("LockResolution — Windows Resolution Lock Service");
                    Console.WriteLine();
                    Console.WriteLine("CMD Usage:");
                    Console.WriteLine("  LockResolution.exe set <width> <height> [monitor]  — Set resolution target");
                    Console.WriteLine("  LockResolution.exe interval <seconds>              — Set check interval");
                    Console.WriteLine("  LockResolution.exe enable                          — Enable lock");
                    Console.WriteLine("  LockResolution.exe disable                         — Disable lock");
                    Console.WriteLine("  LockResolution.exe status                          — Show current config & status");
                    Console.WriteLine("  LockResolution.exe apply                           — Apply resolution now");
                    Console.WriteLine("  LockResolution.exe list                            — List all monitors");
                    break;
            }
        }
    }

    public class Config
    {
        public int Width, Height, Interval, Monitor;
        public bool Enabled;
    }
}
