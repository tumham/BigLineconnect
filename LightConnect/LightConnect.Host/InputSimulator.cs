using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LightConnect.Host
{
    public static class InputSimulator
    {
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll", EntryPoint = "SendInput", SetLastError = true)]
        private static extern uint NativeSendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        private static readonly Dictionary<string, ushort> KeyMap = new(StringComparer.OrdinalIgnoreCase)
        {
            { "enter", 0x0D },
            { "backspace", 0x08 },
            { "tab", 0x09 },
            { "escape", 0x1B },
            { "space", 0x20 },
            { "control", 0x11 },
            { "shift", 0x10 },
            { "alt", 0x12 },
            { "arrowleft", 0x25 },
            { "arrowup", 0x26 },
            { "arrowright", 0x27 },
            { "arrowdown", 0x28 },
            { "delete", 0x2E },
            { "home", 0x24 },
            { "end", 0x23 },
            { "pageup", 0x21 },
            { "pagedown", 0x22 }
        };

        public static void SimulateMouseMove(double xRatio, double yRatio)
        {
            try
            {
                var bounds = SystemInformation.VirtualScreen;
                int actualX = bounds.Left + (int)(xRatio * bounds.Width);
                int actualY = bounds.Top + (int)(yRatio * bounds.Height);

                SetCursorPos(actualX, actualY);

                int normX = Math.Max(0, Math.Min(65535, (int)(xRatio * 65535)));
                int normY = Math.Max(0, Math.Min(65535, (int)(yRatio * 65535)));

                INPUT[] inputs = new INPUT[1];
                inputs[0] = new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            dx = normX,
                            dy = normY,
                            dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
                        }
                    }
                };
                NativeSendInput(1, inputs, Marshal.SizeOf<INPUT>());
            }
            catch { }
        }

        public static void SimulateMouseButton(string button, string action, double? xRatio = null, double? yRatio = null)
        {
            try
            {
                uint flags = 0;
                if (button.Equals("left", StringComparison.OrdinalIgnoreCase))
                {
                    flags = action.Equals("down", StringComparison.OrdinalIgnoreCase) ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
                }
                else if (button.Equals("right", StringComparison.OrdinalIgnoreCase))
                {
                    flags = action.Equals("down", StringComparison.OrdinalIgnoreCase) ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
                }
                else if (button.Equals("middle", StringComparison.OrdinalIgnoreCase))
                {
                    flags = action.Equals("down", StringComparison.OrdinalIgnoreCase) ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
                }

                if (flags == 0) return;

                INPUT[] inputs = new INPUT[1];
                var mi = new MOUSEINPUT { dwFlags = flags };

                if (xRatio.HasValue && yRatio.HasValue)
                {
                    var bounds = SystemInformation.VirtualScreen;
                    int actualX = bounds.Left + (int)(xRatio.Value * bounds.Width);
                    int actualY = bounds.Top + (int)(yRatio.Value * bounds.Height);
                    SetCursorPos(actualX, actualY);

                    mi.dx = Math.Max(0, Math.Min(65535, (int)(xRatio.Value * 65535)));
                    mi.dy = Math.Max(0, Math.Min(65535, (int)(yRatio.Value * 65535)));
                    mi.dwFlags |= MOUSEEVENTF_ABSOLUTE;
                }

                inputs[0] = new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = mi } };
                NativeSendInput(1, inputs, Marshal.SizeOf<INPUT>());
            }
            catch { }
        }

        public static void SimulateMouseScroll(int deltaY)
        {
            try
            {
                INPUT[] inputs = new INPUT[1];
                inputs[0] = new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion
                    {
                        mi = new MOUSEINPUT
                        {
                            mouseData = (uint)deltaY,
                            dwFlags = MOUSEEVENTF_WHEEL
                        }
                    }
                };
                NativeSendInput(1, inputs, Marshal.SizeOf<INPUT>());
            }
            catch { }
        }

        public static void SimulateKey(string key, string action)
        {
            try
            {
                ushort vkCode = 0;
                if (KeyMap.TryGetValue(key, out var mappedVk))
                {
                    vkCode = mappedVk;
                }
                else if (key.Length == 1)
                {
                    short scan = VkKeyScan(key[0]);
                    if (scan != -1) vkCode = (ushort)(scan & 0xFF);
                }

                if (vkCode == 0) return;

                uint scanCode = MapVirtualKey(vkCode, 0);
                uint flags = action.Equals("up", StringComparison.OrdinalIgnoreCase) ? KEYEVENTF_KEYUP : 0;

                INPUT[] inputs = new INPUT[1];
                inputs[0] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vkCode,
                            wScan = (ushort)scanCode,
                            dwFlags = flags
                        }
                    }
                };
                NativeSendInput(1, inputs, Marshal.SizeOf<INPUT>());
            }
            catch { }
        }
    }
}
