using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
            {"F24", 0x87}
        };

        private static readonly Dictionary<string, int> wscanCodes = new Dictionary<string, int>
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
        };

        // Flags for keybd_event
        const uint KEYEVENTF_KEYDOWN = 0x0000;
        const uint KEYEVENTF_KEYUP = 0x0002;
        public MainForm()
        {
            InitializeComponent();
            this.TopMost = true;
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

        private void SendKey_Method_SendInput(string key, bool ctrl, bool shift, bool alt)
        {
            string command = "";
            if (ctrl) command += "^";
            if (shift) command += "+";
            if (alt) command += "%";
            command += $"{{{key}}}";

            SendKeys.Send(command);
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
    }
}