using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace BigLineconnect
{
    /// <summary>
    /// BigLine-RT: 64x64 Tile-Based Differential Screen Capture and Assembly Engine (AnyDesk DeskRT equivalent)
    /// </summary>
    public static class BigLineRtEngine
    {
        public const int TILE_SIZE = 64; // 64x64 pixel tiles for optimal compression & throughput

        public const byte FLAG_KEYFRAME = 0x01;
        public const byte FLAG_TILES = 0x02;
        private static ulong[]? _lastTileHashes;
        private static int _lastScreenWidth = 0;
        private static int _lastScreenHeight = 0;
        private static ushort _tileCols = 0;
        private static ushort _tileRows = 0;
        private static int _frameCount = 0;

        private static ImageCodecInfo? _jpegEncoder;

        static BigLineRtEngine()
        {
            _jpegEncoder = GetEncoder(ImageFormat.Jpeg);
        }

        public static void Reset()
        {
            _lastTileHashes = null;
            _lastScreenWidth = 0;
            _lastScreenHeight = 0;
            _frameCount = 0;
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }
            return null;
        }

        /// <summary>
        /// Compresses screen bitmap into BigLine-RT differential tile packet or keyframe packet.
        /// </summary>
        public static byte[] EncodeFrame(Bitmap bmpScreen, int quality, bool forceKeyframe = false)
        {
            int width = bmpScreen.Width;
            int height = bmpScreen.Height;

            ushort cols = (ushort)((width + TILE_SIZE - 1) / TILE_SIZE);
            ushort rows = (ushort)((height + TILE_SIZE - 1) / TILE_SIZE);
            int totalTiles = cols * rows;

            _frameCount++;
            if (_frameCount == 1 || _frameCount % 30 == 0 || _lastTileHashes == null)
            {
                forceKeyframe = true;
            }

            bool isDimensionChanged = width != _lastScreenWidth || height != _lastScreenHeight || _lastTileHashes == null || _lastTileHashes.Length != totalTiles;
            if (isDimensionChanged)
            {
                _lastScreenWidth = width;
                _lastScreenHeight = height;
                _tileCols = cols;
                _tileRows = rows;
                _lastTileHashes = new ulong[totalTiles];
                forceKeyframe = true;
            }

            // Lock bitmap bits for fast SIMD / span comparison
            BitmapData bd = bmpScreen.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            
            try
            {
                var dirtyTileIndices = new List<ushort>();
                var currentHashes = new ulong[totalTiles];

                unsafe
                {
                    byte* pBase = (byte*)bd.Scan0;
                    int stride = bd.Stride;

                    for (ushort r = 0; r < rows; r++)
                    {
                        for (ushort c = 0; c < cols; c++)
                        {
                            int tileIdx = r * cols + c;
                            int tileX = c * TILE_SIZE;
                            int tileY = r * TILE_SIZE;
                            int tileW = Math.Min(TILE_SIZE, width - tileX);
                            int tileH = Math.Min(TILE_SIZE, height - tileY);

                            ulong hash = ComputeTileHash(pBase, stride, tileX, tileY, tileW, tileH);
                            currentHashes[tileIdx] = hash;

                            if (forceKeyframe || _lastTileHashes == null || _lastTileHashes[tileIdx] != hash)
                            {
                                dirtyTileIndices.Add((ushort)tileIdx);
                            }
                        }
                    }
                }

                // Update hash cache
                _lastTileHashes = currentHashes;

                // If no tiles changed and not keyframe, return empty array to save bandwidth completely
                if (dirtyTileIndices.Count == 0 && !forceKeyframe)
                {
                    return Array.Empty<byte>();
                }

                // If more than 60% of tiles changed, issue full Keyframe for efficiency
                if (forceKeyframe || dirtyTileIndices.Count > (totalTiles * 0.6))
                {
                    return BuildKeyframePacket(bmpScreen, width, height, quality);
                }

                // Otherwise build differential tiles packet
                return BuildTilesPacket(bmpScreen, dirtyTileIndices, cols, width, height, quality);
            }
            finally
            {
                bmpScreen.UnlockBits(bd);
            }
        }


        private static unsafe ulong ComputeTileHash(byte* pBase, int stride, int x, int y, int w, int h)
        {
            ulong hash = 14695981039346656037UL;
            for (int r = 0; r < h; r++)
            {
                uint* rowPtr = (uint*)(pBase + (y + r) * stride + x * 4);
                for (int c = 0; c < w; c++)
                {
                    hash = (hash ^ rowPtr[c]) * 1099511628211UL;
                }
            }
            return hash;
        }

        private static byte[] BuildKeyframePacket(Bitmap bmpScreen, int width, int height, int quality)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(MAGIC_HEADER);
                writer.Write(FLAG_KEYFRAME);
                writer.Write((ushort)width);
                writer.Write((ushort)height);

                // Compress full image to JPEG
                using (var imgMs = new MemoryStream())
                {
                    using (EncoderParameters encoderParams = new EncoderParameters(1))
                    using (EncoderParameter encoderParam = new EncoderParameter(Encoder.Quality, (long)quality))
                    {
                        encoderParams.Param[0] = encoderParam;
                        bmpScreen.Save(imgMs, _jpegEncoder ?? ImageCodecInfo.GetImageEncoders()[0], encoderParams);
                    }
                    byte[] jpegData = imgMs.ToArray();
                    writer.Write(jpegData.Length);
                    writer.Write(jpegData);
                }

                return ms.ToArray();
            }
        }

        private static byte[] BuildTilesPacket(Bitmap bmpScreen, List<ushort> dirtyIndices, ushort cols, int width, int height, int quality)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(MAGIC_HEADER);
                writer.Write(FLAG_TILES);
                writer.Write((ushort)width);
                writer.Write((ushort)height);
                writer.Write((ushort)dirtyIndices.Count);

                using (EncoderParameters encoderParams = new EncoderParameters(1))
                using (EncoderParameter encoderParam = new EncoderParameter(Encoder.Quality, (long)quality))
                {
                    encoderParams.Param[0] = encoderParam;
                    var encoder = _jpegEncoder ?? ImageCodecInfo.GetImageEncoders()[0];

                    foreach (ushort tileIdx in dirtyIndices)
                    {
                        int c = tileIdx % cols;
                        int r = tileIdx / cols;
                        int x = c * TILE_SIZE;
                        int y = r * TILE_SIZE;
                        int w = Math.Min(TILE_SIZE, width - x);
                        int h = Math.Min(TILE_SIZE, height - y);

                        writer.Write((ushort)c);
                        writer.Write((ushort)r);

                        // Crop tile and compress
                        using (Bitmap tileBmp = bmpScreen.Clone(new Rectangle(x, y, w, h), PixelFormat.Format32bppArgb))
                        using (MemoryStream tileMs = new MemoryStream())
                        {
                            tileBmp.Save(tileMs, encoder, encoderParams);
                            byte[] tileJpeg = tileMs.ToArray();
                            writer.Write(tileJpeg.Length);
                            writer.Write(tileJpeg);
                        }
                    }
                }

                return ms.ToArray();
            }
        }

        public const uint MAGIC_HEADER = 0x45545242; // 'B','R','T','E' in little-endian

        public static bool IsBigLineRtPacket(byte[] data)
        {
            if (data == null || data.Length < 5) return false;
            uint header = BitConverter.ToUInt32(data, 0);
            return header == MAGIC_HEADER;
        }

        private static readonly object _canvasLock = new object();

        public static Bitmap? ProcessRtPacket(byte[] packet, ref Bitmap? currentCanvas)
        {
            if (!IsBigLineRtPacket(packet)) return null;

            lock (_canvasLock)
            {
                using (var ms = new MemoryStream(packet))
                using (var reader = new BinaryReader(ms))
                {
                    uint magic = reader.ReadUInt32();
                    byte flag = reader.ReadByte();
                    ushort width = reader.ReadUInt16();
                    ushort height = reader.ReadUInt16();

                    if (flag == FLAG_KEYFRAME)
                    {
                        int length = reader.ReadInt32();
                        byte[] jpegData = reader.ReadBytes(length);

                        using (var jpegMs = new MemoryStream(jpegData))
                        {
                            var fullBmp = new Bitmap(jpegMs);
                            currentCanvas?.Dispose();
                            currentCanvas = new Bitmap(fullBmp);
                        }
                    }
                    else if (flag == FLAG_TILES)
                    {
                        ushort tileCount = reader.ReadUInt16();
                        if (currentCanvas == null || currentCanvas.Width != width || currentCanvas.Height != height)
                        {
                            currentCanvas?.Dispose();
                            currentCanvas = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                        }

                        using (Graphics g = Graphics.FromImage(currentCanvas))
                        {
                            g.CompositingMode = CompositingMode.SourceCopy;
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;

                            for (int i = 0; i < tileCount; i++)
                            {
                                ushort col = reader.ReadUInt16();
                                ushort row = reader.ReadUInt16();
                                int dataLen = reader.ReadInt32();
                                byte[] tileJpeg = reader.ReadBytes(dataLen);

                                int x = col * TILE_SIZE;
                                int y = row * TILE_SIZE;

                                using (var tileMs = new MemoryStream(tileJpeg))
                                using (var tileBmp = new Bitmap(tileMs))
                                {
                                    g.DrawImage(tileBmp, x, y);
                                }
                            }
                        }
                    }

                    if (currentCanvas != null)
                    {
                        return (Bitmap)currentCanvas.Clone();
                    }
                    return null;
                }
            }
        }
    }
}
