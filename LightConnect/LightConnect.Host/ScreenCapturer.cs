using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace LightConnect.Host
{
    public static class ScreenCapturer
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int SRCCOPY = 0x00CC0020;

        private static ImageCodecInfo? _jpegEncoder;
        private static EncoderParameters? _encoderParams;

        static ScreenCapturer()
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    _jpegEncoder = codec;
                    break;
                }
            }
            _encoderParams = new EncoderParameters(1);
            _encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 45L);
        }

        public static byte[] CaptureScreenJpeg(int targetWidth = 1280, int quality = 45)
        {
            IntPtr hDesk = GetDesktopWindow();
            IntPtr hSrcDC = GetDC(hDesk);
            if (hSrcDC == IntPtr.Zero) return Array.Empty<byte>();

            IntPtr hDestDC = CreateCompatibleDC(hSrcDC);
            int screenWidth = SystemInformation.VirtualScreen.Width;
            int screenHeight = SystemInformation.VirtualScreen.Height;
            int screenLeft = SystemInformation.VirtualScreen.Left;
            int screenTop = SystemInformation.VirtualScreen.Top;

            if (screenWidth <= 0 || screenHeight <= 0)
            {
                screenWidth = 1920;
                screenHeight = 1080;
            }

            IntPtr hBitmap = CreateCompatibleBitmap(hSrcDC, screenWidth, screenHeight);
            IntPtr hOldBmp = SelectObject(hDestDC, hBitmap);

            BitBlt(hDestDC, 0, 0, screenWidth, screenHeight, hSrcDC, screenLeft, screenTop, SRCCOPY);

            SelectObject(hDestDC, hOldBmp);

            using (Bitmap rawBmp = Image.FromHbitmap(hBitmap))
            {
                DeleteObject(hBitmap);
                DeleteDC(hDestDC);
                ReleaseDC(hDesk, hSrcDC);

                // Resize if needed for high-speed streaming
                int scaledWidth = targetWidth;
                int scaledHeight = (int)((double)screenHeight / screenWidth * scaledWidth);

                using (Bitmap scaledBmp = new Bitmap(scaledWidth, scaledHeight, PixelFormat.Format24bppRgb))
                {
                    using (Graphics g = Graphics.FromImage(scaledBmp))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                        g.DrawImage(rawBmp, 0, 0, scaledWidth, scaledHeight);
                    }

                    using (MemoryStream ms = new MemoryStream())
                    {
                        if (_jpegEncoder != null)
                        {
                            var encParams = new EncoderParameters(1);
                            encParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)quality);
                            scaledBmp.Save(ms, _jpegEncoder, encParams);
                        }
                        else
                        {
                            scaledBmp.Save(ms, ImageFormat.Jpeg);
                        }
                        return ms.ToArray();
                    }
                }
            }
        }
    }
}
