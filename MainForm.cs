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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F_Key_Sender
{
    public partial class MainForm : Form
    {

        // Dictionary to store virtual key codes and scan codes. Will want to use wscan codes for SendInput
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

        public MainForm()
        {
            InitializeComponent();
            this.TopMost = true;
            stopwatch.Start();
            dropdownMethod.SelectedIndex = 0;
        }

        // Using WaitHandle instead of Thread.Sleep to avoid wasting system resources and also more accurate
        private static readonly AutoResetEvent waitHandle = new AutoResetEvent(false);

        private CancellationTokenSource _cts;

        private async void SendKeyCombo(string key)
        {
            bool ctrl = checkBoxCtrl.Checked;
            bool shift = checkBoxShift.Checked;
            bool alt = checkBoxAlt.Checked;

            _cts = new CancellationTokenSource();

            try
            {
                if (dropdownMethod.SelectedIndex == 0) // SendInput
                {
                    await SendKey_Method_SendInputAsync(key, ctrl, shift, alt, _cts.Token);
                }
                else if (dropdownMethod.SelectedIndex == 1) // keybd_event
                {
                    await SendKey_keybd_eventAsync(key, ctrl, shift, alt, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                labelToolstripStatus.Text = "Status: Operation Cancelled";
                labelToolstripStatus.ForeColor = Color.Orange;
            }
            finally
            {
                All_Buttons_Enabler();
                _cts.Dispose();
                _cts = null;
            }
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private async Task SendKey_keybd_eventAsync(string key, bool ctrl, bool shift, bool alt, CancellationToken ct)
        {
            byte virtualKeyCode = BitConverter.GetBytes(keyCodes[key].vk)[0];

            await Task.Run(async () =>
            {
                // Start delay, disable buttons, and update status text
                this.Invoke((MethodInvoker)delegate
                {
                    All_Buttons_Disabler();
                    labelToolstripStatus.Text = "Status: Waiting Before Sending...";
                    labelToolstripStatus.ForeColor = Color.Purple;
                });

                await Task.Delay((int)nudDelay.Value * 1000, ct);

                ct.ThrowIfCancellationRequested();

                // Press modifier keys
                if (ctrl) keybd_event(0x11, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                if (alt) keybd_event(0x12, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                // Press F-key
                keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                // Hold the key for specified duration, update status text
                this.Invoke((MethodInvoker)delegate
                {
                    labelToolstripStatus.Text = "Status: Holding Key...";
                    labelToolstripStatus.ForeColor = Color.Red;
                });

                await Task.Delay((int)nudDuration.Value, ct);

                ct.ThrowIfCancellationRequested();

                // Release F-key
                keybd_event(virtualKeyCode, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Release modifier keys
                if (alt) keybd_event(0x12, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (shift) keybd_event(0x10, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                if (ctrl) keybd_event(0x11, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

                // Re-enable all buttons after keys are released
                this.Invoke((MethodInvoker)delegate
                {
                    All_Buttons_Enabler();
                    labelToolstripStatus.Text = "Status: Ready";
                    labelToolstripStatus.ForeColor = Color.Black;
                });
            }, ct);
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

        // Bypassing SendKeys and directly using SendInput due to limitations of F17 through F24 in .NET's SendKeys
        private async Task SendKey_Method_SendInputAsync(string key, bool ctrl, bool shift, bool alt, CancellationToken ct)
        {
            if (!keyCodes.TryGetValue(key.ToUpper(), out var codes))
            {
                return; // Invalid key
            }

            await Task.Run(async () =>
            {
                // Helper function to create a single input
                INPUT CreateInput(ushort vk, ushort scan, bool isKeyUp = false)
                {
                    return new INPUT
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
                }

                // Delay before sending keys. Disable all buttons while keys are virtually held down
                this.Invoke((MethodInvoker)delegate
                {
                    All_Buttons_Disabler();
                    labelToolstripStatus.Text = "Status: Waiting Before Sending...";
                    labelToolstripStatus.ForeColor = Color.Purple;
                });

                await Task.Delay((int)nudDelay.Value * 1000, ct);

                ct.ThrowIfCancellationRequested();

                // Create lists to hold key down and key up inputs
                List<INPUT> keyDownInputs = new List<INPUT>();
                List<INPUT> keyUpInputs = new List<INPUT>();

                // Add key down events for modifiers
                if (ctrl) keyDownInputs.Add(CreateInput(keyCodes["LCTRL"].vk, keyCodes["LCTRL"].scan));
                if (shift) keyDownInputs.Add(CreateInput(keyCodes["LSHIFT"].vk, keyCodes["LSHIFT"].scan));
                if (alt) keyDownInputs.Add(CreateInput(keyCodes["LALT"].vk, keyCodes["LALT"].scan));

                // Add key down event for the main key
                keyDownInputs.Add(CreateInput(codes.vk, codes.scan));

                // Add key up events in reverse order
                keyUpInputs.Add(CreateInput(codes.vk, codes.scan, true));
                if (alt) keyUpInputs.Add(CreateInput(keyCodes["LALT"].vk, keyCodes["LALT"].scan, true));
                if (shift) keyUpInputs.Add(CreateInput(keyCodes["LSHIFT"].vk, keyCodes["LSHIFT"].scan, true));
                if (ctrl) keyUpInputs.Add(CreateInput(keyCodes["LCTRL"].vk, keyCodes["LCTRL"].scan, true));

                // Send key down inputs
                SendInput((uint)keyDownInputs.Count, keyDownInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));

                // Wait for the key press duration using Task.Delay
                this.Invoke((MethodInvoker)delegate
                {
                    labelToolstripStatus.Text = "Status: Holding Key...";
                    labelToolstripStatus.ForeColor = Color.Red;
                });

                await Task.Delay((int)nudDuration.Value, ct);

                ct.ThrowIfCancellationRequested();

                // Send key up inputs
                SendInput((uint)keyUpInputs.Count, keyUpInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));

                // Re-enable all buttons after keys are released
                this.Invoke((MethodInvoker)delegate
                {
                    All_Buttons_Enabler();
                    labelToolstripStatus.Text = "Status: Ready";
                    labelToolstripStatus.ForeColor = Color.Black;
                });
            }, ct);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            base.OnFormClosing(e);
        }

        private void All_Buttons_Disabler()
        {
            // Disable all buttons
            btnF13.Enabled = false;
            btnF14.Enabled = false;
            btnF15.Enabled = false;
            btnF16.Enabled = false;
            btnF17.Enabled = false;
            btnF18.Enabled = false;
            btnF19.Enabled = false;
            btnF20.Enabled = false;
            btnF21.Enabled = false;
            btnF22.Enabled = false;
            btnF23.Enabled = false;
            btnF24.Enabled = false;
            btnTestX.Enabled = false;

            // Disable checkboxes
            checkBoxAlt.Enabled = false;
            checkBoxCtrl.Enabled = false;
            checkBoxShift.Enabled = false;
        }

        private void All_Buttons_Enabler()
        {
            // Enable all buttons
            btnF13.Enabled = true;
            btnF14.Enabled = true;
            btnF15.Enabled = true;
            btnF16.Enabled = true;
            btnF17.Enabled = true;
            btnF18.Enabled = true;
            btnF19.Enabled = true;
            btnF20.Enabled = true;
            btnF21.Enabled = true;
            btnF22.Enabled = true;
            btnF23.Enabled = true;
            btnF24.Enabled = true;
            btnTestX.Enabled = true;

            // Enable checkboxes
            checkBoxAlt.Enabled = true;
            checkBoxCtrl.Enabled = true;
            checkBoxShift.Enabled = true;
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

        private void btnTestX_Click(object sender, EventArgs e)
        {
            SendKeyCombo("X");
        }
    }


}