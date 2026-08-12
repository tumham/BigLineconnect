using System;
using System.Drawing;
using System.Drawing.Imaging;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace BigLineconnect
{
    public class DxgiScreenCapturer : IDisposable
    {
        private Device? _device;
        private OutputDuplication? _deskDupl;
        private Texture2D? _stagingTexture;
        private int _width;
        private int _height;
        private bool _isInitialized;
        private readonly int _outputIndex;

        public DxgiScreenCapturer(int outputIndex = 0)
        {
            _outputIndex = outputIndex;
            try
            {
                Init();
            }
            catch
            {
                _isInitialized = false;
            }
        }

        private void Init()
        {
            using (var factory = new Factory1())
            {
                using (var adapter = factory.GetAdapter1(0))
                {
                    _device = new Device(adapter);
                    using (var output = adapter.GetOutput(_outputIndex))
                    {
                        using (var output1 = output.QueryInterface<Output1>())
                        {
                            var desc = output.Description;
                            _width = desc.DesktopBounds.Right - desc.DesktopBounds.Left;
                            _height = desc.DesktopBounds.Bottom - desc.DesktopBounds.Top;

                            _deskDupl = output1.DuplicateOutput(_device);
                        }
                    }
                }
            }

            var textureDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = _width,
                Height = _height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging
            };

            _stagingTexture = new Texture2D(_device, textureDesc);
            _isInitialized = true;
        }

        public Bitmap? CaptureFrame(int timeoutMs = 10)
        {
            if (!_isInitialized)
            {
                try
                {
                    Init();
                }
                catch
                {
                    return null;
                }
            }

            if (_deskDupl == null || _device == null || _stagingTexture == null) return null;

            try
            {
                SharpDX.DXGI.Resource desktopResource;
                OutputDuplicateFrameInformation frameInfo;

                var result = _deskDupl.TryAcquireNextFrame(timeoutMs, out frameInfo, out desktopResource);
                if (result.Failure) return null;

                using (desktopResource)
                {
                    using (var tempTexture = desktopResource.QueryInterface<Texture2D>())
                    {
                        _device.ImmediateContext.CopyResource(tempTexture, _stagingTexture);
                    }
                }

                // Map staging texture FIRST to force GPU-CPU synchronization before releasing the desktop frame
                var dataBox = _device.ImmediateContext.MapSubresource(_stagingTexture, 0, MapMode.Read, MapFlags.None);
                
                _deskDupl.ReleaseFrame();

                var bitmap = new Bitmap(_width, _height, PixelFormat.Format32bppArgb);
                var boundsRect = new Rectangle(0, 0, _width, _height);
                
                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);
                
                var sourcePtr = dataBox.DataPointer;
                var destPtr = mapDest.Scan0;

                for (int y = 0; y < _height; y++)
                {
                    Utilities.CopyMemory(destPtr, sourcePtr, _width * 4);
                    sourcePtr = IntPtr.Add(sourcePtr, dataBox.RowPitch);
                    destPtr = IntPtr.Add(destPtr, mapDest.Stride);
                }

                bitmap.UnlockBits(mapDest);
                _device.ImmediateContext.UnmapSubresource(_stagingTexture, 0);

                return bitmap;
            }
            catch (SharpDXException ex)
            {
                if (ex.ResultCode == SharpDX.DXGI.ResultCode.AccessLost || 
                    ex.ResultCode == SharpDX.DXGI.ResultCode.AccessDenied ||
                    ex.ResultCode == SharpDX.DXGI.ResultCode.SessionDisconnected)
                {
                    Dispose();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _isInitialized = false;
            try { _stagingTexture?.Dispose(); } catch { }
            try { _deskDupl?.Dispose(); } catch { }
            try { _device?.Dispose(); } catch { }
            _stagingTexture = null;
            _deskDupl = null;
            _device = null;
        }
    }
}
