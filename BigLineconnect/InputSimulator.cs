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
            { "f12", 0x7B },        // VK_F12
            { "meta", 0x5B },       // VK_LWIN
            { "win", 0x5B },        // VK_LWIN
            { "cmd", 0x5B },        // VK_LWIN
            { "capslock", 0x14 }    // VK_CAPITAL
        };

        public static void SimulateMouseMove(double xPercent, double yPercent, int displayIndex = 0)
        {
            try
            {
                DesktopHelper.AttachToInputDesktop();
                var screens = System.Windows.Forms.Screen.AllScreens;
                if (displayIndex < 0 || displayIndex >= screens.Length) displayIndex = 0;

                var bounds = screens[displayIndex].Bounds;
                int actualX = bounds.X + Math.Min(bounds.Width - 1, (int)(xPercent * bounds.Width));
                int actualY = bounds.Y + Math.Min(bounds.Height - 1, (int)(yPercent * bounds.Height));

                SetCursorPos(actualX, actualY);
                mouse_event(MOUSEEVENTF_MOVE, 0, 0, 0, (UIntPtr)0); // Force Windows mouse subsystem to dispatch input
            }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private static bool _isLeftMouseDown = false;
        private static bool _isRightMouseDown = false;
        private static bool _isMiddleMouseDown = false;

        public static void SimulateMouseButton(string button, string action, double? xPercent = null, double? yPercent = null, int displayIndex = 0)
        {
            try
            {
                DesktopHelper.AttachToInputDesktop();
                uint flags = 0;
                bool isDown = action.Equals("down", StringComparison.OrdinalIgnoreCase);

                if (button.Equals("left", StringComparison.OrdinalIgnoreCase))
                {
                    flags = isDown ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;
                    _isLeftMouseDown = isDown;
                }
                else if (button.Equals("right", StringComparison.OrdinalIgnoreCase))
                {
                    flags = isDown ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
                    _isRightMouseDown = isDown;
                }
                else if (button.Equals("middle", StringComparison.OrdinalIgnoreCase))
                {
                    flags = isDown ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
                    _isMiddleMouseDown = isDown;
                }

                if (flags == 0) return;

                var screens = System.Windows.Forms.Screen.AllScreens;
                if (displayIndex < 0 || displayIndex >= screens.Length) displayIndex = 0;
                var bounds = screens[displayIndex].Bounds;

                if (xPercent.HasValue && yPercent.HasValue && isDown)
                {
                    int actualX = bounds.X + Math.Min(bounds.Width - 1, (int)(xPercent.Value * bounds.Width));
                    int actualY = bounds.Y + Math.Min(bounds.Height - 1, (int)(yPercent.Value * bounds.Height));
                    SetCursorPos(actualX, actualY);
                }

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
                            dwFlags = flags,
                            mouseData = 0,
                            time = 0,
                            dwExtraInfo = (IntPtr)0
                        }
                    }
                };

                uint res = SendInput(1, inputs, Marshal.SizeOf<INPUT>());
                if (res == 0)
                {
                    mouse_event(flags, 0, 0, 0, (UIntPtr)0);
                }

                Program.TriggerInstantCapture();
            }
            catch { }
        }
        public static void SimulateMouseScroll(int deltaY)
        {
            try
            {
                DesktopHelper.AttachToInputDesktop();
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
                            dwExtraInfo = (IntPtr)0x42494755
                        }
                    }
                };

                SendInput(1, inputs, Marshal.SizeOf<INPUT>());
                Program.TriggerInstantCapture();
            }
            catch { }
        }

        public static void SimulateMouseDoubleClick(string button, double? xPercent = null, double? yPercent = null, int displayIndex = 0)
        {
            try
            {
                DesktopHelper.AttachToInputDesktop();
                SimulateMouseButton(button, "down", xPercent, yPercent, displayIndex);
                SimulateMouseButton(button, "up", xPercent, yPercent, displayIndex);
                System.Threading.Thread.Sleep(30);
                SimulateMouseButton(button, "down", xPercent, yPercent, displayIndex);
                SimulateMouseButton(button, "up", xPercent, yPercent, displayIndex);
                Program.TriggerInstantCapture();
            }
            catch { }
        }

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        public static void ReleaseAllModifiers()
        {
            try
            {
                if (_isLeftMouseDown)
                {
                    SimulateMouseButton("left", "up");
                    _isLeftMouseDown = false;
                }
                if (_isRightMouseDown)
                {
                    SimulateMouseButton("right", "up");
                    _isRightMouseDown = false;
                }
                if (_isMiddleMouseDown)
                {
                    SimulateMouseButton("middle", "up");
                    _isMiddleMouseDown = false;
                }
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

        public static void SimulateKeyStroke(string key, bool shift = false, bool ctrl = false, bool alt = false)
        {
            DesktopHelper.AttachToInputDesktop();

            ushort vkCode = 0;
            if (SpecialKeyMap.TryGetValue(key, out ushort mappedVk))
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
            uint extFlag = 0;
            if (vkCode == 0x25 || vkCode == 0x26 || vkCode == 0x27 || vkCode == 0x28 || 
                vkCode == 0x2E || vkCode == 0x2D || vkCode == 0x24 || vkCode == 0x23 || 
                vkCode == 0x21 || vkCode == 0x22 || vkCode == 0xA3 || vkCode == 0xA5 || 
                vkCode == 0x5B || vkCode == 0x5C)
            {
                extFlag = KEYEVENTF_EXTENDEDKEY;
            }

            var inputs = new List<INPUT>();

            if (ctrl) inputs.Add(CreateKeyInput(0x11, 0, 0));
            if (alt) inputs.Add(CreateKeyInput(0x12, 0, 0));
            if (shift) inputs.Add(CreateKeyInput(0x10, 0, 0));

            inputs.Add(CreateKeyInput(vkCode, (ushort)scanCode, extFlag));
            inputs.Add(CreateKeyInput(vkCode, (ushort)scanCode, extFlag | KEYEVENTF_KEYUP));

            if (shift) inputs.Add(CreateKeyInput(0x10, 0, KEYEVENTF_KEYUP));
            if (alt) inputs.Add(CreateKeyInput(0x12, 0, KEYEVENTF_KEYUP));
            if (ctrl) inputs.Add(CreateKeyInput(0x11, 0, KEYEVENTF_KEYUP));

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            Program.TriggerInstantCapture();
        }

        public static void SimulateKeyRepeat(string key, int count = 1, bool shift = false, bool ctrl = false, bool alt = false)
        {
            if (count <= 0) return;
            if (count == 1)
            {
                SimulateKeyStroke(key, shift, ctrl, alt);
                return;
            }

            DesktopHelper.AttachToInputDesktop();

            ushort vkCode = 0;
            if (SpecialKeyMap.TryGetValue(key, out ushort mappedVk))
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
            uint extFlag = 0;
            if (vkCode == 0x25 || vkCode == 0x26 || vkCode == 0x27 || vkCode == 0x28 || 
                vkCode == 0x2E || vkCode == 0x2D || vkCode == 0x24 || vkCode == 0x23 || 
                vkCode == 0x21 || vkCode == 0x22 || vkCode == 0xA3 || vkCode == 0xA5 || 
                vkCode == 0x5B || vkCode == 0x5C)
            {
                extFlag = KEYEVENTF_EXTENDEDKEY;
            }

            var inputs = new List<INPUT>();

            if (ctrl) inputs.Add(CreateKeyInput(0x11, 0, 0));
            if (alt) inputs.Add(CreateKeyInput(0x12, 0, 0));
            if (shift) inputs.Add(CreateKeyInput(0x10, 0, 0));

            for (int i = 0; i < count; i++)
            {
                inputs.Add(CreateKeyInput(vkCode, (ushort)scanCode, extFlag));
                inputs.Add(CreateKeyInput(vkCode, (ushort)scanCode, extFlag | KEYEVENTF_KEYUP));
            }

            if (shift) inputs.Add(CreateKeyInput(0x10, 0, KEYEVENTF_KEYUP));
            if (alt) inputs.Add(CreateKeyInput(0x12, 0, KEYEVENTF_KEYUP));
            if (ctrl) inputs.Add(CreateKeyInput(0x11, 0, KEYEVENTF_KEYUP));

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            Program.TriggerInstantCapture();
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

            if (vkCode == 0)
            {
                if (key.Length == 1 && action.Equals("down", StringComparison.OrdinalIgnoreCase))
                {
                    SimulateChar(key[0]);
                }
                return;
            }

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

        private static INPUT CreateKeyInput(ushort wVk, ushort wScan, uint dwFlags)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = wVk,
                        wScan = wScan,
                        dwFlags = dwFlags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
        }

        public static void SimulateChar(char ch)
        {
            DesktopHelper.AttachToInputDesktop();

            short scan = VkKeyScan(ch);
            if (scan != -1)
            {
                byte vk = (byte)(scan & 0xFF);
                byte shiftState = (byte)((scan >> 8) & 0xFF);
                bool needShift = (shiftState & 1) != 0;
                bool needCtrlAlt = (shiftState & 6) == 6; // AltGr key combination

                uint scanCode = MapVirtualKey(vk, 0);

                var inputs = new List<INPUT>();

                if (needCtrlAlt)
                {
                    inputs.Add(CreateKeyInput(0x11, 0, 0)); // Ctrl Down
                    inputs.Add(CreateKeyInput(0x12, 0, 0)); // Alt Down
                }
                else if (needShift)
                {
                    inputs.Add(CreateKeyInput(0x10, 0, 0)); // Shift Down
                }

                inputs.Add(CreateKeyInput(vk, (ushort)scanCode, 0)); // VK Down
                inputs.Add(CreateKeyInput(vk, (ushort)scanCode, KEYEVENTF_KEYUP)); // VK Up

                if (needCtrlAlt)
                {
                    inputs.Add(CreateKeyInput(0x12, 0, KEYEVENTF_KEYUP));
                    inputs.Add(CreateKeyInput(0x11, 0, KEYEVENTF_KEYUP));
                }
                else if (needShift)
                {
                    inputs.Add(CreateKeyInput(0x10, 0, KEYEVENTF_KEYUP));
                }

                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            }
            else
            {
                // Fallback for exotic unicode characters
                INPUT[] inputs = new INPUT[2];
                inputs[0] = CreateKeyInput(0, (ushort)ch, KEYEVENTF_UNICODE);
                inputs[1] = CreateKeyInput(0, (ushort)ch, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP);
                SendInput(2, inputs, Marshal.SizeOf<INPUT>());
            }

            Program.TriggerInstantCapture(2);
        }

        public static void SimulateBinaryInput(ReadOnlySpan<byte> packet, int displayIndex = 0)
        {
            if (packet.Length < 9 || packet[0] != BinaryInputProtocol.MAGIC_BYTE) return;

            byte cmd = packet[1];

            if (cmd == BinaryInputProtocol.CMD_KEY_CHAR)
            {
                ushort charCode = (ushort)(packet[2] | (packet[3] << 8));
                SimulateChar((char)charCode);
            }
            else if (cmd == BinaryInputProtocol.CMD_KEY_STROKE || cmd == BinaryInputProtocol.CMD_KEY_DOWN)
            {
                ushort vkCode = (ushort)(packet[2] | (packet[3] << 8));
                byte mods = packet[4];

                bool shift = (mods & BinaryInputProtocol.MOD_SHIFT) != 0;
                bool ctrl = (mods & BinaryInputProtocol.MOD_CTRL) != 0;
                bool alt = (mods & BinaryInputProtocol.MOD_ALT) != 0;

                SimulateVkCode(vkCode, shift, ctrl, alt, isUp: false, isStroke: (cmd == BinaryInputProtocol.CMD_KEY_STROKE));
            }
            else if (cmd == BinaryInputProtocol.CMD_KEY_UP)
            {
                ushort vkCode = (ushort)(packet[2] | (packet[3] << 8));
                SimulateVkCode(vkCode, false, false, false, isUp: true, isStroke: false);
            }
            else if (cmd == BinaryInputProtocol.CMD_MOUSE_MOVE)
            {
                ushort normX = (ushort)(packet[5] | (packet[6] << 8));
                ushort normY = (ushort)(packet[7] | (packet[8] << 8));

                double xPercent = normX / 65535.0;
                double yPercent = normY / 65535.0;

                SimulateMouseMove(xPercent, yPercent, displayIndex);
            }
            else if (cmd == BinaryInputProtocol.CMD_MOUSE_BUTTON || cmd == BinaryInputProtocol.CMD_MOUSE_DBLCLICK)
            {
                byte btn = packet[2];
                byte act = packet[3];

                ushort normX = (ushort)(packet[5] | (packet[6] << 8));
                ushort normY = (ushort)(packet[7] | (packet[8] << 8));

                double xPercent = normX / 65535.0;
                double yPercent = normY / 65535.0;

                string buttonStr = btn == BinaryInputProtocol.MOUSE_BTN_RIGHT ? "right" :
                                  (btn == BinaryInputProtocol.MOUSE_BTN_MIDDLE ? "middle" : "left");
                string actionStr = act == BinaryInputProtocol.MOUSE_ACT_UP ? "up" :
                                  (act == BinaryInputProtocol.MOUSE_ACT_CLICK ? "click" : "down");

                if (cmd == BinaryInputProtocol.CMD_MOUSE_DBLCLICK)
                {
                    SimulateMouseDoubleClick("left", xPercent, yPercent, displayIndex);
                }
                else
                {
                    SimulateMouseButton(buttonStr, actionStr, xPercent, yPercent, displayIndex);
                }
            }
            else if (cmd == BinaryInputProtocol.CMD_MOUSE_SCROLL)
            {
                short deltaY = (short)(packet[2] | (packet[3] << 8));
                SimulateMouseScroll(deltaY);
            }
        }

        public static void SimulateVkCode(ushort vkCode, bool shift, bool ctrl, bool alt, bool isUp, bool isStroke)
        {
            if (vkCode == 0) return;
            DesktopHelper.AttachToInputDesktop();

            uint scanCode = MapVirtualKey(vkCode, 0);
            uint extFlag = 0;
            if (vkCode == 0x25 || vkCode == 0x26 || vkCode == 0x27 || vkCode == 0x28 || 
                vkCode == 0x2E || vkCode == 0x2D || vkCode == 0x24 || vkCode == 0x23 || 
                vkCode == 0x21 || vkCode == 0x22 || vkCode == 0xA3 || vkCode == 0xA5 || 
                vkCode == 0x5B || vkCode == 0x5C)
            {
                extFlag = KEYEVENTF_EXTENDEDKEY;
            }

            var inputs = new List<INPUT>();

            if (!isUp)
            {
                if (ctrl) inputs.Add(CreateKeyInput(0x11, 0, 0));
                if (alt) inputs.Add(CreateKeyInput(0x12, 0, 0));
                if (shift) inputs.Add(CreateKeyInput(0x10, 0, 0));

                inputs.Add(CreateKeyInput(vkCode, (ushort)scanCode, extFlag));

                if (isStroke)
                {
                    inputs.Add(CreateKeyInput(vkCode, (ushort)scanCode, extFlag | KEYEVENTF_KEYUP));
                    if (shift) inputs.Add(CreateKeyInput(0x10, 0, KEYEVENTF_KEYUP));
                    if (alt) inputs.Add(CreateKeyInput(0x12, 0, KEYEVENTF_KEYUP));
                    if (ctrl) inputs.Add(CreateKeyInput(0x11, 0, KEYEVENTF_KEYUP));
                }
            }
            else
            {
                inputs.Add(CreateKeyInput(vkCode, (ushort)scanCode, extFlag | KEYEVENTF_KEYUP));
            }

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
        }
    }
}
