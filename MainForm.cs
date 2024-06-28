using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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

        // Flags for keybd_event
        const uint KEYEVENTF_KEYDOWN = 0x0000;
        const uint KEYEVENTF_KEYUP = 0x0002;
        public MainForm()
        {
            InitializeComponent();
            this.TopMost = true;
        }

        private async void SendKeyCombo(string key)
        {
            // Get Byte value of the key
            byte virtualKeyCode = virtualKeyCodes[key];
            bool ctrl = checkBoxCtrl.Checked;
            bool shift = checkBoxShift.Checked;
            bool alt = checkBoxAlt.Checked;

            await Task.Delay((int)nudDelay.Value * 1000);

            // Simulate modifier keys (Ctrl, Shift, Alt) if checked
            if (ctrl) keybd_event(0x11, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Ctrl down
            if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Shift down
            if (alt) keybd_event(0x12, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Alt down

            // Simulate F-key press
            keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
            keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            // Simulate modifier key releases
            if (alt) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Alt up
            if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Shift up
            if (ctrl) keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); // Ctrl up
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
    }
}