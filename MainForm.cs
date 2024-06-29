using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace F_Key_Sender
{
    public partial class MainForm : Form
    {

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        // Dictionary to store virtual key codes
        private static readonly Dictionary<string, byte> virtualKeyCodes = new Dictionary<string, byte>
        {
            {"F13", 0x7C},
            {"F14", 0x7D},
            {"F15", 0x7E},
            {"F16", 0x7F},
            {"F17", 0x80},
            {"F18", 0x81},
            {"F19", 0x82},
            {"F20", 0x83},
            {"F21", 0x84},
            {"F22", 0x85},
            {"F23", 0x86},
            {"F24", 0x87},
            {"X", 0x58},
        };

        private static readonly Dictionary<string, ushort> wscanCodes = new Dictionary<string, ushort>
        {
            {"F13", 100},
            {"F14", 101},
            {"F15", 102},
            {"F16", 103},
            {"F17", 104},
            {"F18", 105},
            {"F19", 106},
            {"F20", 107},
            {"F21", 108},
            {"F22", 109},
            {"F23", 110},
            {"F24", 118},
            {"LSHIFT", 42},
            {"LCTRL", 29},
            {"LALT", 56},
            {"X", 45 }
        };

        private static readonly Dictionary<string, ushort> scanCodes = new Dictionary<string, ushort>
        {
            {"F13", 0x64},
            {"F14", 0x65},
            {"F15", 0x66},
            {"F16", 0x67},
            {"F17", 0x68},
            {"F18", 0x69},
            {"F19", 0x6A},
            {"F20", 0x6B},
            {"F21", 0x6C},
            {"F22", 0x6D},
            {"F23", 0x6E},
            {"F24", 0x76},
            {"LCTRL", 0x1D},
            {"LSHIFT", 0x2A},
            {"LALT", 0x38},
            {"X", 0x2D}
        };

        private static readonly Dictionary<string, (ushort vk, ushort scan)> keyCodes = new Dictionary<string, (ushort, ushort)>
        {
            {"F13", (0x7C, 100)},
            {"F14", (0x7D, 101)},
            {"F15", (0x7E, 102)},
            {"F16", (0x7F, 103)},
            {"F17", (0x80, 104)},
            {"F18", (0x81, 105)},
            {"F19", (0x82, 106)},
            {"F20", (0x83, 107)},
            {"F21", (0x84, 108)},
            {"F22", (0x85, 109)},
            {"F23", (0x86, 110)},
            {"F24", (0x87, 118)},
            {"LCTRL", (0x11, 29)},
            {"LSHIFT", (0x10, 42)},
            {"LALT", (0x12, 56)},
            {"X", (0x58, 45)}
        };

        // Flags for keybd_event
        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYDOWN = 0x0000;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_SCANCODE = 0x0008;

        private readonly Stopwatch stopwatch = new Stopwatch();
        private const int KEY_PRESS_DURATION = 50; // Duration in milliseconds

        public MainForm()
        {
            InitializeComponent();
            this.TopMost = true;
            stopwatch.Start();
            dropdownMethod.SelectedIndex = 0;
        }

        private static readonly AutoResetEvent waitHandle = new AutoResetEvent(false);

        private void SendKeyCombo(string key)
        {
            bool ctrl = checkBoxCtrl.Checked;
            bool shift = checkBoxShift.Checked;
            bool alt = checkBoxAlt.Checked;

            if (dropdownMethod.SelectedIndex == 0) // SendInput
            {
                SendKey_Method_SendInput(key, ctrl, shift, alt);
            }
            else if (dropdownMethod.SelectedIndex == 1) // keybd_event
            {
                SendKey_keybd_event(key, ctrl, shift, alt);
            }
        }

        private void SendKey_keybd_event(string key, bool ctrl, bool shift, bool alt)
        {
            byte virtualKeyCode = virtualKeyCodes[key];

            // Delay before sending keys
            waitHandle.WaitOne((int)nudDelay.Value * 1000);

            // Press modifier keys
            if (ctrl) keybd_event(0x11, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            if (alt) keybd_event(0x12, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

            // Press F-key
            keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

            // Hold the key for a short duration
            waitHandle.WaitOne(50);

            // Release F-key
            keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Release modifier keys
            if (alt) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (ctrl) keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        private void SendKey_Method_SendInput(string key, bool ctrl, bool shift, bool alt)
        {
            if (!keyCodes.TryGetValue(key.ToUpper(), out var codes))
            {
                return; // Invalid key
            }

            // Helper function to create and send a single input
            void SendSingleInput(ushort vk, ushort scan, bool isKeyUp = false)
            {
                INPUT[] inputs = new INPUT[1];
                inputs[0] = new INPUT
                {
                    type = INPUT_KEYBOARD,
                    u = new InputUnion
                    {
                        ki = new KEYBDINPUT
                        {
                            wVk = vk,
                            wScan = scan,
                            dwFlags = isKeyUp ? KEYEVENTF_KEYUP : 0,
                            time = (uint)stopwatch.ElapsedMilliseconds,
                            dwExtraInfo = IntPtr.Zero
                        }
                    }
                };

                SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
            }

            void SendKeyWithDuration(ushort vk, ushort scan)
            {
                SendSingleInput(vk, scan); // Key down
                Thread.Sleep(KEY_PRESS_DURATION);
                SendSingleInput(vk, scan, true); // Key up
            }

            // Delay before sending keys
            waitHandle.WaitOne((int)nudDelay.Value * 1000);

            // Press modifier keys
            if (ctrl) SendKeyWithDuration(keyCodes["LCTRL"].vk, keyCodes["LCTRL"].scan);
            if (shift) SendKeyWithDuration(keyCodes["LSHIFT"].vk, keyCodes["LSHIFT"].scan);
            if (alt) SendKeyWithDuration(keyCodes["LALT"].vk, keyCodes["LALT"].scan);

            // Press the main key
            SendKeyWithDuration(codes.vk, codes.scan);

            // Note: We don't need to release modifier keys separately as they're already released in SendKeyWithDuration
        }


        private void chkAlwaysOnTop_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = chkAlwaysOnTop.Checked;
        }

        // Define button clicks to send F-key presses based on the button clicked
        private void btnF13_Click(object sender, EventArgs e)
        {
            // Send F13 key press to function, it will handle which combos to send
            SendKeyCombo("F13");
        }

        private void btnF14_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F14");
        }

        private void btnF15_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F15");
        }

        private void btnF16_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F16");
        }

        private void btnF17_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F17");
        }

        private void btnF18_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F18");
        }

        private void btnF19_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F19");
        }

        private void btnF20_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F20");
        }

        private void btnF21_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F21");
        }

        private void btnF22_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F22");
        }

        private void btnF23_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F23");
        }

        private void btnF24_Click(object sender, EventArgs e)
        {
            SendKeyCombo("F24");
        }

        private void dropdownMethod_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnTestX_Click(object sender, EventArgs e)
        {
            SendKeyCombo("X");
        }
    }
}