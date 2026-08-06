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

        private static ImageCodecInfo? _jpegEncoder;
        private static DxgiScreenCapturer? _dxgiCapturer;
        private static bool _useDxgi = true; // Default to DXGI for ultra-fast 60 FPS DirectX hardware capture with automatic GDI+ fallback
        private static int _consecutiveBlackFrames = 0;
        public static int CurrentDisplayIndex { get; set; } = 0;
        private static readonly MemoryStream _sharedMs = new MemoryStream(1024 * 1024 * 4);
        private static readonly object _compressLock = new object();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        private const int SPI_GETDESKWALLPAPER = 0x0073;
        private const int SPI_SETDESKWALLPAPER = 0x0014;
        private const int SPIF_SENDCHANGE = 0x02;

        private static string? _originalWallpaper = null;

        public static void SuppressWallpaper(bool suppress)
        {
            try
            {
                if (suppress)
                {
                    if (_originalWallpaper == null)
                    {
                        var sb = new System.Text.StringBuilder(260);
                        SystemParametersInfo(SPI_GETDESKWALLPAPER, sb.Capacity, sb.ToString(), 0);
                        _originalWallpaper = sb.ToString();
                    }
                    SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, "", SPIF_SENDCHANGE);
                }
                else
                {
                    if (_originalWallpaper != null && !string.IsNullOrEmpty(_originalWallpaper))
                    {
                        SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, _originalWallpaper, SPIF_SENDCHANGE);
                        _originalWallpaper = null;
                    }
                }
            }
            catch { }
        }

        static ScreenCapturer()
        {
            // Initialize JPEG encoder once for performance
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        }

        public static byte[] Capture(int quality = 50, int maxDimension = 1366)
        {
            if (_useDxgi)
            {
                if (_dxgiCapturer == null)
                {
                    try
                    {
                        _dxgiCapturer = new DxgiScreenCapturer(CurrentDisplayIndex);
                        LogHelper($"DXGI Screen Capturer initialized for display {CurrentDisplayIndex}.");
                    }
                    catch (Exception ex)
                    {
                        LogHelper($"Failed to initialize DXGI Screen Capturer: {ex.Message}\r\n{ex.StackTrace}");
                        _useDxgi = false;
                    }
                }

                if (_useDxgi && _dxgiCapturer != null)
                {
                    Bitmap? bmp = null;
                    try
                    {
                        bmp = _dxgiCapturer.CaptureFrame();
                    }
                    catch (Exception ex)
                    {
                        LogHelper($"DXGI CaptureFrame failed: {ex.Message}\r\n{ex.StackTrace}");
                        try { _dxgiCapturer.Dispose(); } catch { }
                        _dxgiCapturer = null;
                        _useDxgi = false;
                    }

                    if (bmp != null)
                    {
                        using (bmp)
                        {
                            if (IsBitmapBlack(bmp))
                            {
                                LogHelper("DXGI: Black frame detected, disabling DXGI and falling back to GDI+.");
                                _useDxgi = false;
                                try { _dxgiCapturer?.Dispose(); } catch { }
                                _dxgiCapturer = null;
                            }
                            else
                            {
                                return ProcessAndCompress(bmp, quality, maxDimension);
                            }
                        }
                    }
                }
            }

            // Fallback to GDI+
            return CaptureGdi(quality, maxDimension);
        }

        private static bool IsBitmapBlack(Bitmap bmp)
        {
            try
            {
                // Check 5 points: 4 corners and the center
                Color c1 = bmp.GetPixel(0, 0);
                Color c2 = bmp.GetPixel(bmp.Width - 1, 0);
                Color c3 = bmp.GetPixel(0, bmp.Height - 1);
                Color c4 = bmp.GetPixel(bmp.Width - 1, bmp.Height - 1);
                Color c5 = bmp.GetPixel(bmp.Width / 2, bmp.Height / 2);

                return c1.R == 0 && c1.G == 0 && c1.B == 0 &&
                       c2.R == 0 && c2.G == 0 && c2.B == 0 &&
                       c3.R == 0 && c3.G == 0 && c3.B == 0 &&
                       c4.R == 0 && c4.G == 0 && c4.B == 0 &&
                       c5.R == 0 && c5.G == 0 && c5.B == 0;
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
                int screenWidth = bmpScreen.Width;
                int screenHeight = bmpScreen.Height;

                double scale = 1.0;
                if (screenWidth > maxDimension || screenHeight > maxDimension)
                {
                    scale = (double)maxDimension / Math.Max(screenWidth, screenHeight);
                }

                int targetWidth = (int)(screenWidth * scale);
                int targetHeight = (int)(screenHeight * scale);

                if (scale < 1.0)
                {
                    using (Bitmap bmpResized = new Bitmap(targetWidth, targetHeight, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics gResized = Graphics.FromImage(bmpResized))
                        {
                            gResized.InterpolationMode = InterpolationMode.Bilinear; // Fast & Crisp
                            gResized.CompositingQuality = CompositingQuality.HighSpeed;
                            gResized.SmoothingMode = SmoothingMode.HighSpeed;
                            gResized.DrawImage(bmpScreen, 0, 0, targetWidth, targetHeight);
                        }

                        return CompressToJpeg(bmpResized, quality);
                    }
                }

                return CompressToJpeg(bmpScreen, quality);
            }
            catch (Exception ex)
            {
                LogHelper($"[Process/Compress Error]: {ex.Message}\r\n{ex.StackTrace}");
                try { MainWindow.Instance.AppendLog($"[Process/Compress Error]: {ex.Message}"); } catch { }
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
            }
            catch { }
        }
    }
}
