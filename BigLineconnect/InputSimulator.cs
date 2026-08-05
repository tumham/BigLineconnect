using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BigLineconnect
{
    public static class InputSimulator
    {
        // P/Invoke for SendInput
        [DllImport("user32.dll", EntryPoint = "SendInput", SetLastError = true)]
        private static extern uint NativeSendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private static uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize)
        {
            return NativeSendInput(nInputs, pInputs, cbSize);
        }

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
            [FieldOffset(0)] public HARDWAREINPUT hi;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        private const uint KEYEVENTF_KEYDOWN = 0x0000;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        private static readonly Dictionary<string, ushort> SpecialKeyMap = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
        {
            { "enter", 0x0D },      // VK_RETURN
            { "backspace", 0x08 },  // VK_BACK
            { "tab", 0x09 },        // VK_TAB
            { "escape", 0x1B },     // VK_ESCAPE
            { "space", 0x20 },      // VK_SPACE
            { "control", 0x11 },    // VK_CONTROL
            { "shift", 0x10 },      // VK_SHIFT
            { "alt", 0x12 },        // VK_MENU
            { "arrowleft", 0x25 },  // VK_LEFT
            { "arrowup", 0x26 },    // VK_UP
            { "arrowright", 0x27 }, // VK_RIGHT
            { "arrowdown", 0x28 },  // VK_DOWN
            { "delete", 0x2E },     // VK_DELETE
            { "insert", 0x2D },     // VK_INSERT
            { "home", 0x24 },       // VK_HOME
            { "end", 0x23 },        // VK_END
            { "pageup", 0x21 },     // VK_PRIOR
            { "pagedown", 0x22 },   // VK_NEXT
            { "f1", 0x70 },         // VK_F1
            { "f2", 0x71 },         // VK_F2
            { "f3", 0x72 },         // VK_F3
            { "f4", 0x73 },         // VK_F4
            { "f5", 0x74 },         // VK_F5
            { "f6", 0x75 },         // VK_F6
            { "f7", 0x76 },         // VK_F7
            { "f8", 0x77 },         // VK_F8
            { "f9", 0x78 },         // VK_F9
            { "f10", 0x79 },        // VK_F10
            { "f11", 0x7A },        // VK_F11
            { "f12", 0x7B }         // VK_F12
        };

        public static void SimulateMouseMove(double xPercent, double yPercent, int displayIndex = 0)
        {
            try
            {
                var screens = System.Windows.Forms.Screen.AllScreens;
                if (displayIndex < 0 || displayIndex >= screens.Length) displayIndex = 0;

                var bounds = screens[displayIndex].Bounds;
                int actualX = bounds.X + (int)(xPercent * bounds.Width);
                int actualY = bounds.Y + (int)(yPercent * bounds.Height);

                SetCursorPos(actualX, actualY);

                int normX = Math.Max(0, Math.Min(65535, (int)(xPercent * 65535)));
                int normY = Math.Max(0, Math.Min(65535, (int)(yPercent * 65535)));

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
                            mouseData = 0,
                            dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE,
                            time = 0,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };
                SendInput(1, inputs, Marshal.SizeOf<INPUT>());
            }
            catch { }
        }

        public static void SimulateMouseButton(string button, string action, double? xPercent = null, double? yPercent = null, int displayIndex = 0)
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
                var mi = new MOUSEINPUT
                {
                    mouseData = 0,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                };

                if (xPercent.HasValue && yPercent.HasValue)
                {
                    var screens = System.Windows.Forms.Screen.AllScreens;
                    if (displayIndex < 0 || displayIndex >= screens.Length) displayIndex = 0;
                    var bounds = screens[displayIndex].Bounds;
                    int actualX = bounds.X + (int)(xPercent.Value * bounds.Width);
                    int actualY = bounds.Y + (int)(yPercent.Value * bounds.Height);
                    SetCursorPos(actualX, actualY);

                    mi.dx = Math.Max(0, Math.Min(65535, (int)(xPercent.Value * 65535)));
                    mi.dy = Math.Max(0, Math.Min(65535, (int)(yPercent.Value * 65535)));
                    mi.dwFlags |= MOUSEEVENTF_ABSOLUTE;
                }

                inputs[0] = new INPUT
                {
                    type = INPUT_MOUSE,
                    U = new InputUnion { mi = mi }
                };

                SendInput(1, inputs, Marshal.SizeOf<INPUT>());
            }
            catch { }
        }

        public static void SimulateMouseScroll(int deltaY)
        {
            DesktopHelper.AttachToInputDesktop();
            // deltaY is usually +120 or -120 per scroll click
            INPUT[] inputs = new INPUT[1];
            inputs[0] = new INPUT
            {
                type = INPUT_MOUSE,
                U = new InputUnion
                {
                    mi = new MOUSEINPUT
                    {
                        dx = 0,
                        dy = 0,
                        dwFlags = MOUSEEVENTF_WHEEL,
                        mouseData = (uint)deltaY,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }

        public static void SimulateMouseDoubleClick(string button)
        {
            try
            {
                DesktopHelper.AttachToInputDesktop();
                SimulateMouseButton(button, "down");
                SimulateMouseButton(button, "up");
                System.Threading.Thread.Sleep(40);
                SimulateMouseButton(button, "down");
                SimulateMouseButton(button, "up");
            }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static void ReleaseAllModifiers()
        {
            try
            {
                SimulateMouseButton("left", "up");
                SimulateMouseButton("right", "up");
                SimulateMouseButton("middle", "up");
            }
            catch { }

            ushort[] modifiers = new ushort[] { 0x10, 0x11, 0x12, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C };
            foreach (var vk in modifiers)
            {
                try
                {
                    uint scanCode = MapVirtualKey(vk, 0);
                    uint flags = KEYEVENTF_KEYUP;
                    if (vk == 0xA3 || vk == 0xA5 || vk == 0x5B || vk == 0x5C)
                    {
                        flags |= KEYEVENTF_EXTENDEDKEY;
                    }

                    INPUT[] inputs = new INPUT[1];
                    inputs[0] = new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        U = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = vk,
                                wScan = (ushort)scanCode,
                                dwFlags = flags,
                                time = 0,
                                dwExtraInfo = IntPtr.Zero
                            }
                        }
                    };
                    SendInput(1, inputs, Marshal.SizeOf<INPUT>());
                }
                catch { }
            }
        }

        public static void SimulateKey(string key, string action)
        {
            if (key.Equals("release_all", StringComparison.OrdinalIgnoreCase) || 
                key.Equals("release_modifiers", StringComparison.OrdinalIgnoreCase))
            {
                ReleaseAllModifiers();
                return;
            }

            ushort vkCode = 0;

            if (SpecialKeyMap.TryGetValue(key, out ushort mappedVk))
            {
                vkCode = mappedVk;
            }
            else if (key.Length == 1)
            {
                // Translate character to virtual key code
                short scan = VkKeyScan(key[0]);
                if (scan != -1)
                {
                    vkCode = (ushort)(scan & 0xFF);
                }
            }

            if (vkCode == 0) return;

            uint scanCode = MapVirtualKey(vkCode, 0);
            uint flags = action.Equals("up", StringComparison.OrdinalIgnoreCase) ? KEYEVENTF_KEYUP : 0;

            if (vkCode == 0x25 || vkCode == 0x26 || vkCode == 0x27 || vkCode == 0x28 || 
                vkCode == 0x2E || vkCode == 0x2D || vkCode == 0x24 || vkCode == 0x23 || 
                vkCode == 0x21 || vkCode == 0x22 || vkCode == 0xA3 || vkCode == 0xA5 || 
                vkCode == 0x5B || vkCode == 0x5C)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }

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
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }

        public static void SimulateChar(char ch)
        {
            DesktopHelper.AttachToInputDesktop();
            INPUT[] inputs = new INPUT[2];
            
            // Key Down
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)ch,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            
            // Key Up
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)ch,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };

            SendInput(2, inputs, Marshal.SizeOf<INPUT>());
        }
    }
}
