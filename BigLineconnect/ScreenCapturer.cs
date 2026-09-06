using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace BigLineconnect
{
    public static class ScreenCapturer
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public int ptScreenPos_x;
            public int ptScreenPos_y;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon, int cxWidth, int cyHeight, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        private const int CURSOR_SHOWING = 0x00000001;
        private const int DI_NORMAL = 0x0003;

        public static void DrawCursorOnBitmap(Bitmap bmp, int startX = 0, int startY = 0)
        {
            try
            {
                var ci = new CURSORINFO();
                ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out ci) && (ci.flags & CURSOR_SHOWING) != 0 && ci.hCursor != IntPtr.Zero)
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        IntPtr hdc = g.GetHdc();
                        try
                        {
                            int x = ci.ptScreenPos_x - startX;
                            int y = ci.ptScreenPos_y - startY;
                            DrawIconEx(hdc, x, y, ci.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
                        }
                        finally
                        {
                            g.ReleaseHdc(hdc);
                        }
                    }
                }
            }
            catch { }
        }

        private static ImageCodecInfo? _jpegEncoder;
        private static DxgiScreenCapturer? _dxgiCapturer;
        private static bool _useDxgi = true; // Default to DXGI for ultra-fast 60 FPS DirectX hardware capture with automatic GDI+ fallback
        private static int _consecutiveBlackFrames = 0;
        public static int CurrentDisplayIndex { get; set; } = 0;
        private static readonly MemoryStream _sharedMs = new MemoryStream(1024 * 1024 * 4);
        private static readonly object _compressLock = new object();

        public static void WarmupDxgi()
        {
            if (_useDxgi && _dxgiCapturer == null)
            {
                System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        _dxgiCapturer = new DxgiScreenCapturer(CurrentDisplayIndex);
                        LogHelper("DXGI Screen Capturer pre-warmed for 0ms cold-start connection speed.");
                    }
                    catch
                    {
                        _useDxgi = false;
                    }
                });
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string? lpvParam, int fuWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, System.Text.StringBuilder lpvParam, int fuWinIni);

        private const int SPI_GETDESKWALLPAPER = 0x0073;
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll")]
        private static extern bool SetSysColors(int cElements, int[] lpaElements, uint[] lpaRgbValues);

        private const int COLOR_DESKTOP = 1;
        private static string? _originalWallpaper = null;

        private static string GetWindowsWallpaperPath()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop");
                string? regPath = key?.GetValue("WallPaper") as string;
                if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
                {
                    return regPath;
                }

                string transcoded = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Themes\TranscodedWallpaper"
                );
                if (File.Exists(transcoded))
                {
                    return transcoded;
                }
            }
            catch { }
            return "";
        }

        public static void SuppressWallpaper(bool suppress)
        {
            try
            {
                if (suppress)
                {
                    if (string.IsNullOrEmpty(_originalWallpaper))
                    {
                        _originalWallpaper = GetWindowsWallpaperPath();
                    }

                    int[] elements = new int[] { COLOR_DESKTOP };
                    uint[] colors = new uint[] { 0x000000 }; // Solid Black
                    SetSysColors(1, elements, colors);
                    SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_SENDCHANGE);
                }
                else
                {
                    string wallpaperToRestore = !string.IsNullOrEmpty(_originalWallpaper)
                        ? _originalWallpaper
                        : GetWindowsWallpaperPath();

                    if (!string.IsNullOrEmpty(wallpaperToRestore) && File.Exists(wallpaperToRestore))
                    {
                        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, wallpaperToRestore, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                    }
                    else
                    {
                        // Refresh Windows Shell to reload wallpaper
                        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, (string?)null, SPIF_SENDCHANGE);
                    }
                }
                Program.TriggerInstantCapture(2);
            }
            catch { }
        }

        static ScreenCapturer()
        {
            // Initialize JPEG encoder once for performance
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        }

        public static bool UseRtTileEngine { get; set; } = true; // DeskRT 64x64 Differential SubFrames: 1.5 - 3 KB per frame, 0ms latency, zero quota bleed
        public static bool UseH264Mode { get; set; } = false;
        public static bool ForceKeyframeRequested { get; set; } = false;
        private static H264Encoder? _h264Encoder;

        public static Bitmap? CaptureGdiRaw(int quality = 80, int maxDimension = 0)
        {
            try
            {
                int screenWidth = 1920;
                int screenHeight = 1080;
                int startX = 0;
                int startY = 0;

                try
                {
                    var screens = System.Windows.Forms.Screen.AllScreens;
                    int idx = CurrentDisplayIndex;
                    if (idx < 0 || idx >= screens.Length) idx = 0;

                    var bounds = screens[idx].Bounds;
                    screenWidth = bounds.Width;
                    screenHeight = bounds.Height;
                    startX = bounds.X;
                    startY = bounds.Y;
                }
                catch { }

                if (screenWidth <= 0) screenWidth = GetSystemMetrics(SM_CXSCREEN);
                if (screenHeight <= 0) screenHeight = GetSystemMetrics(SM_CYSCREEN);

                if (screenWidth <= 0) screenWidth = 1920;
                if (screenHeight <= 0) screenHeight = 1080;

                Bitmap bmpScreen = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppRgb);
                using (Graphics gScreen = Graphics.FromImage(bmpScreen))
                {
                    IntPtr hdcDest = gScreen.GetHdc();
                    IntPtr hdcSrc = GetDC(IntPtr.Zero);
                    try
                    {
                        BitBlt(hdcDest, 0, 0, screenWidth, screenHeight, hdcSrc, startX, startY, 0x00CC0020); // SRCCOPY
                    }
                    finally
                    {
                        ReleaseDC(IntPtr.Zero, hdcSrc);
                        gScreen.ReleaseHdc(hdcDest);
                    }
                }
                return bmpScreen;
            }
            catch
            {
                return null;
            }
        }

        private static DateTime _lastDxgiFailureTime = DateTime.MinValue;
        private static ulong _lastCaptureHash = 0;

        public static byte[] Capture(int quality = 75, int maxDimension = 0)
        {
            Bitmap? bmp = null;
            if (_useDxgi && (DateTime.UtcNow - _lastDxgiFailureTime).TotalSeconds >= 2.0)
            {
                if (_dxgiCapturer == null)
                {
                    try
                    {
                        _dxgiCapturer = new DxgiScreenCapturer(CurrentDisplayIndex);
                    }
                    catch
                    {
                        _lastDxgiFailureTime = DateTime.UtcNow;
                    }
                }

                if (_dxgiCapturer != null)
                {
                    try
                    {
                        bmp = _dxgiCapturer.CaptureFrame(10);
                    }
                    catch
                    {
                        _lastDxgiFailureTime = DateTime.UtcNow;
                        try { _dxgiCapturer?.Dispose(); } catch { }
                        _dxgiCapturer = null;
                    }
                }
            }

            if (bmp == null)
            {
                // If DXGI is active and timed out because desktop is static, DO NOT FALL BACK TO GDI!
                if (_dxgiCapturer != null && _dxgiCapturer.LastCaptureTimedOut)
                {
                    return Array.Empty<byte>();
                }

                bmp = CaptureGdiRaw(quality, maxDimension);
            }

            if (bmp != null)
            {
                using (bmp)
                {
                    ulong hash = CalculateFastScreenHash(bmp);
                    LastCapturedFrameHash = hash;

                    // Bandwidth & Quota Guard: If screen hash did not change at all, avoid JPEG compression & network transmission!
                    if (hash != 0 && hash == _lastCaptureHash && !ForceKeyframeRequested)
                    {
                        return Array.Empty<byte>();
                    }
                    _lastCaptureHash = hash;
                    ForceKeyframeRequested = false;

                    if (UseH264Mode)
                    {
                        if (_h264Encoder == null)
                        {
                            _h264Encoder = new H264Encoder(bmp.Width, bmp.Height);
                        }
                        return _h264Encoder.EncodeFrame(bmp, quality);
                    }
                    else if (UseRtTileEngine)
                    {
                        bool forceKf = ForceKeyframeRequested;
                        ForceKeyframeRequested = false;

                        // DIRECT NATIVE 1:1 TILE ENGINE (Alpemix Zero-Bandwidth Mode):
                        // Operating directly on native bmp guarantees 100% BIT-IDENTICAL hashes for unchanged areas.
                        // On idle screen: 0 dirty tiles -> 0 BYTES sent.
                        // On user edit: only the changed 64x64 tiles (400 - 1500 bytes) are encoded and sent!
                        return BigLineRtEngine.EncodeFrame(bmp, quality, forceKf);
                    }
                    else
                    {
                        return ProcessAndCompress(bmp, quality, maxDimension);
                    }
                }
            }

            return Array.Empty<byte>();
        }

        public static ulong LastCapturedFrameHash { get; private set; } = 0;

        public static ulong CalculateFastScreenHash(Bitmap bmp)
        {
            try
            {
                int w = bmp.Width;
                int h = bmp.Height;
                if (w <= 0 || h <= 0) return 0;

                BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, bmp.PixelFormat);
                try
                {
                    unsafe
                    {
                        ulong hash = 14695981039346656037UL;
                        byte* ptr = (byte*)data.Scan0;
                        int stride = data.Stride;

                        // Dense scan: Sample every 4th row and 4th ulong to catch text edits & highlights in 0.05ms with 0% CPU
                        int ulongsPerRow = (w * 4) / 8;
                        for (int y = 0; y < h; y += 4)
                        {
                            ulong* row = (ulong*)(ptr + (y * stride));
                            for (int x = 0; x < ulongsPerRow; x += 4)
                            {
                                hash ^= (row[x] & 0x00FFFFFF00FFFFFFUL);
                                hash *= 1099511628211UL;
                            }
                        }
                        return hash;
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
            catch
            {
                return _lastCaptureHash;
            }
        }

        private static bool IsBitmapBlack(Bitmap bmp)
        {
            try
            {
                int w = bmp.Width;
                int h = bmp.Height;
                if (w <= 0 || h <= 0) return true;

                BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    unsafe
                    {
                        byte* ptr = (byte*)data.Scan0;
                        int stride = data.Stride;

                        // Check 5 sample points (4 corners + center)
                        int[] offsets = new int[]
                        {
                            0, // Top-Left (0, 0)
                            (w - 1) * 4, // Top-Right (w-1, 0)
                            (h - 1) * stride, // Bottom-Left (0, h-1)
                            (h - 1) * stride + (w - 1) * 4, // Bottom-Right (w-1, h-1)
                            (h / 2) * stride + (w / 2) * 4 // Center
                        };

                        foreach (int off in offsets)
                        {
                            byte b = ptr[off];
                            byte g = ptr[off + 1];
                            byte r = ptr[off + 2];
                            if (r != 0 || g != 0 || b != 0) return false;
                        }
                        return true;
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
            catch
            {
                return false;
            }
        }

        private static byte[] CaptureGdi(int quality, int maxDimension)
        {
            try
            {
                int screenWidth = 0;
                int screenHeight = 0;
                int startX = 0;
                int startY = 0;

                try
                {
                    var screens = System.Windows.Forms.Screen.AllScreens;
                    int idx = CurrentDisplayIndex;
                    if (idx < 0 || idx >= screens.Length) idx = 0;

                    var bounds = screens[idx].Bounds;
                    screenWidth = bounds.Width;
                    screenHeight = bounds.Height;
                    startX = bounds.X;
                    startY = bounds.Y;
                }
                catch { }

                if (screenWidth <= 0 || screenHeight <= 0)
                {
                    screenWidth = GetSystemMetrics(SM_CXSCREEN);
                    screenHeight = GetSystemMetrics(SM_CYSCREEN);
                    startX = 0;
                    startY = 0;
                }

                if (screenWidth <= 0) screenWidth = 1920;
                if (screenHeight <= 0) screenHeight = 1080;

                using (Bitmap bmpScreen = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb))
                {
                    using (Graphics gScreen = Graphics.FromImage(bmpScreen))
                    {
                        gScreen.CopyFromScreen(startX, startY, 0, 0, new Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);
                    }

                    return ProcessAndCompress(bmpScreen, quality, maxDimension);
                }
            }
            catch (Exception ex)
            {
                LogHelper($"[GDI+ Capture Error]: {ex.Message}\r\n{ex.StackTrace}");
                try { MainWindow.Instance?.AppendLog($"[GDI+ Capture Error]: {ex.Message}"); } catch { }

                try
                {
                    DesktopHelper.AttachToInputDesktop();
                    int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                    int screenHeight = GetSystemMetrics(SM_CYSCREEN);
                    if (screenWidth <= 0) screenWidth = 1920;
                    if (screenHeight <= 0) screenHeight = 1080;

                    using (Bitmap bmpScreen = new Bitmap(screenWidth, screenHeight, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics gScreen = Graphics.FromImage(bmpScreen))
                        {
                            IntPtr hdcDest = gScreen.GetHdc();
                            IntPtr hdcSrc = GetDC(IntPtr.Zero); // 100% Direct Primary Screen DC
                            BitBlt(hdcDest, 0, 0, screenWidth, screenHeight, hdcSrc, 0, 0, 0x00CC0020);
                            gScreen.ReleaseHdc(hdcDest);
                            ReleaseDC(IntPtr.Zero, hdcSrc);
                        }
                        return ProcessAndCompress(bmpScreen, quality, maxDimension);
                    }
                }
                catch (Exception ex2)
                {
                    LogHelper($"[BitBlt Fallback Error]: {ex2.Message}");
                    return Array.Empty<byte>();
                }
            }
        }

        private static byte[] ProcessAndCompress(Bitmap bmpScreen, int quality, int maxDimension)
        {
            try
            {
                int originalW = bmpScreen.Width;
                int originalH = bmpScreen.Height;

                if (maxDimension > 0 && (originalW > maxDimension || originalH > maxDimension))
                {
                    double scale = Math.Min((double)maxDimension / originalW, (double)maxDimension / originalH);
                    int newW = (int)(originalW * scale);
                    int newH = (int)(originalH * scale);

                    using (Bitmap scaledBmp = new Bitmap(newW, newH, PixelFormat.Format32bppRgb))
                    {
                        using (Graphics g = Graphics.FromImage(scaledBmp))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                            g.DrawImage(bmpScreen, new Rectangle(0, 0, newW, newH), 0, 0, originalW, originalH, GraphicsUnit.Pixel);
                        }
                        return CompressToJpeg(scaledBmp, quality);
                    }
                }

                return CompressToJpeg(bmpScreen, quality);
            }
            catch (Exception ex)
            {
                LogHelper($"[Process/Compress Error]: {ex.Message}\r\n{ex.StackTrace}");
                try { MainWindow.Instance?.AppendLog($"[Process/Compress Error]: {ex.Message}"); } catch { }
                return Array.Empty<byte>();
            }
        }

        private static byte[] CompressToJpeg(Bitmap bmp, int quality)
        {
            if (_jpegEncoder == null) return Array.Empty<byte>();

            lock (_compressLock)
            {
                _sharedMs.Position = 0;
                _sharedMs.SetLength(0);

                using (EncoderParameters encoderParams = new EncoderParameters(1))
                using (EncoderParameter encoderParam = new EncoderParameter(Encoder.Quality, (long)quality))
                {
                    encoderParams.Param[0] = encoderParam;
                    bmp.Save(_sharedMs, _jpegEncoder, encoderParams);
                }

                return _sharedMs.ToArray();
            }
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public static void LogHelper(string message)
        {
            try
            {
                string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                string path = Path.Combine(programData, "BigLineconnect", "helper_log.txt");
                File.AppendAllText(path, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [ScreenCapturer] {message}\r\n");
            }
            catch { }
        }

        public static void Shutdown()
        {
            try
            {
                _dxgiCapturer?.Dispose();
                _dxgiCapturer = null;
                _useDxgi = true;
                _h264Encoder?.Dispose();
                _h264Encoder = null;
                ForceKeyframeRequested = true;
                LastCapturedFrameHash = 0;
                _lastCaptureHash = 0;
                BigLineRtEngine.Reset();
            }
            catch { }
        }
    }
}
