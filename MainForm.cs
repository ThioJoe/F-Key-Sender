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

        // Flags for KEYBDINPUT structure used in API calls
        // Reference: https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-keybdinput
        const uint INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYDOWN = 0x0000;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_SCANCODE = 0x0008;
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        const uint KEYEVENTF_UNICODE = 0x0004;

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

        private async void SendKeyCombo(string key, bool customVK = false, bool customSC = false, bool customUnicode = false)
        {
            bool ctrl = checkBoxCtrl.Checked;
            bool shift = checkBoxShift.Checked;
            bool alt = checkBoxAlt.Checked;

            // Enable cancel button even though not visible yet
            btnCancel.Enabled = true;

            _cts = new CancellationTokenSource();

            try
            {
                if (dropdownMethod.SelectedIndex == 0) // SendInput
                {
                    await SendKey_Method_SendInputAsync(key, ctrl, shift, alt, customVK, customSC, customUnicode, _cts.Token);
                }
                else if (dropdownMethod.SelectedIndex == 1) // keybd_event
                {
                    await SendKey_keybd_eventAsync(key, ctrl, shift, alt, customVK, customSC, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Handle cancellation
                labelToolstripStatus.Text = "Status: Ready (Operation Cancelled)";
                labelToolstripStatus.ForeColor = Color.SaddleBrown;
            }
            finally
            {
                All_Buttons_Enabler();
                btnCancel.Visible = false;
                _cts.Dispose();
                _cts = null;
            }
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private async Task SendKey_keybd_eventAsync(string key, bool ctrl, bool shift, bool alt, bool customVK, bool customSC, CancellationToken ct)
        {
            bool isExtended = false;
            ushort keyHex = 0;
            byte virtualKeyCode = 0;
            byte scanCode = 0;
            uint keydwFlagsDown = 0;
            uint keydwFlagsUp = KEYEVENTF_KEYUP;

            if (customVK)
            {
                (keyHex, isExtended) = StringToUShort(key);
                virtualKeyCode = BitConverter.GetBytes(keyHex)[0];

                // If the key is extended, set the extended key flag
                if (isExtended)
                {
                    keydwFlagsDown |= KEYEVENTF_EXTENDEDKEY;
                    keydwFlagsUp |= KEYEVENTF_EXTENDEDKEY;
                }
            }
            else if (customSC)
            {
                (keyHex, isExtended) = StringToUShort(key);
                scanCode = BitConverter.GetBytes(keyHex)[0];

                // If the key is extended, set the extended key flag
                if (isExtended)
                {
                    keydwFlagsDown |= KEYEVENTF_EXTENDEDKEY;
                    keydwFlagsUp |= KEYEVENTF_EXTENDEDKEY;
                }
            }
            else
            {
                virtualKeyCode = BitConverter.GetBytes(keyCodes[key].vk)[0];
                scanCode = BitConverter.GetBytes(keyCodes[key].scan)[0];
            }

            await Task.Run(async () =>
            {
                // Start delay, disable buttons, and update status text
                this.Invoke((MethodInvoker)delegate
                {
                    All_Buttons_Disabler();
                    labelToolstripStatus.Text = "Status: Waiting Before Sending...";
                    labelToolstripStatus.ForeColor = Color.Purple;
                    btnCancel.Visible = true;
                });

                await Task.Delay((int)nudDelay.Value * 1000, ct);

                ct.ThrowIfCancellationRequested();

                try
                {
                    // Press modifier keys
                    if (ctrl) keybd_event(0x11, 29, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    if (shift) keybd_event(0x10, 42, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    if (alt) keybd_event(0x12, 56, KEYEVENTF_KEYDOWN, UIntPtr.Zero);

                    // Send the down key event for main key
                    // keybd_event: First parameter is the virtual key code, second is the scan code, third is the flags, fourth is the extra info
                    keybd_event(virtualKeyCode, scanCode, keydwFlagsDown, UIntPtr.Zero);

                    // Hold the key for specified duration, update status text
                    this.Invoke((MethodInvoker)delegate
                    {
                        labelToolstripStatus.Text = "Status: Holding Key...";
                        labelToolstripStatus.ForeColor = Color.Green;
                    });

                    await Task.Delay((int)nudDuration.Value, ct);

                    ct.ThrowIfCancellationRequested();
                }
                finally
                {
                    // Send key up event for main key
                    keybd_event(virtualKeyCode, scanCode, keydwFlagsUp, UIntPtr.Zero);

                    // Release modifier keys
                    if (alt) keybd_event(0x12, 56, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (shift) keybd_event(0x10, 42, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    if (ctrl) keybd_event(0x11, 29, KEYEVENTF_KEYUP, UIntPtr.Zero);

                    // Re-enable all buttons after keys are released
                    this.Invoke((MethodInvoker)delegate
                    {
                        All_Buttons_Enabler();
                        labelToolstripStatus.Text = "Status: Ready";
                        labelToolstripStatus.ForeColor = Color.Black;
                        btnCancel.Visible = false;
                    });
                }
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
        private async Task SendKey_Method_SendInputAsync(string key, bool ctrl, bool shift, bool alt, bool customVK, bool customSC, bool customUnicode, CancellationToken ct)
        {
            ushort keyHex = 0;
            bool isExtended = false;
            bool scanOnly = false; // Set to true if for the VK code to be ignored (Note, if the VK value needs to be explicitly 0, do not use this)
            ushort[] unicodeCodesArray = null; // Array to hold the UTF-16 code points if customUnicode is true. May contain one ushort, or multiple if a surrogate pair

            // Here the 'codes dictionary is created either way, but values are only assigned if the key exists in keyCodes
            // Otherwise the values will be set later based on the custom flags
            if (keyCodes.TryGetValue(key.ToUpper(), out var codes))
            {
                // If the key exists in keyCodes, use its vk and scan codes
                // No further action is needed here as 'codes' now contains the correct values
            }

            // Deal with custom key codes
            if (customUnicode)
            {
                // If customUnicode is true, we use the UTF-16 code point of the character as the scan code
                // If it's a surrogate pair, we need to send both codes separately
                unicodeCodesArray = UnicodeToUShortArray(key);
            }
            else if (customVK)
            {
                (keyHex, isExtended) = StringToUShort(key);
                // If customVK is true, create the codes dictionary with a custom virtual key code
                codes = (keyHex, 0); // Only provide vk, set scan code to 0
            }
            else if (customSC)
            {
                (keyHex, isExtended) = StringToUShort(key);
                // If customSC is true, create the codes dictionary with a custom scan code
                codes = (0, keyHex); // Only provide scan code, set vk to 0
                scanOnly = true;

                // DEBUG display message with the scan code
                //MessageBox.Show($"Scan Code: {codes.scan}", "Scan Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            await Task.Run(async () =>
            {
                // Helper function to create a single input
                INPUT CreateInput(ushort vk, ushort scan, bool isKeyUp = false, bool extended = false, bool scanFlag = false, bool unicodeFlag = false)
                {
                    uint dwFlags = 0;

                    if (isKeyUp)
                        dwFlags |= KEYEVENTF_KEYUP;

                    if (unicodeFlag)
                    {
                        dwFlags |= KEYEVENTF_UNICODE;
                        // Note: Be sure that vk is 0 when using KEYEVENTF_UNICODE
                    }
                    else // KEYEVENTF_UNICODE can only be combined with KEYEVENTF_KEYUP, so only check for the rest of the flags if unicode is false
                    {
                        if (extended)
                            dwFlags |= KEYEVENTF_EXTENDEDKEY;

                        if (scanFlag)
                            dwFlags |= KEYEVENTF_SCANCODE;
                    }

                    return new INPUT
                    {
                        type = INPUT_KEYBOARD,
                        u = new InputUnion
                        {
                            ki = new KEYBDINPUT
                            {
                                wVk = vk,
                                wScan = scan,
                                dwFlags = dwFlags,
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
                    btnCancel.Visible = true;
                });

                await Task.Delay((int)nudDelay.Value * 1000, ct);

                ct.ThrowIfCancellationRequested();

                // Create lists to hold key down and key up inputs before sending all together
                List<INPUT> keyDownInputs = new List<INPUT>();
                List<INPUT> keyUpInputs = new List<INPUT>();

                // -------- Add Key Down For Modifiers --------
                if (ctrl) keyDownInputs.Add(CreateInput(vk:keyCodes["LCTRL"].vk, scan:keyCodes["LCTRL"].scan, isKeyUp:false, extended:false));
                if (shift) keyDownInputs.Add(CreateInput(vk:keyCodes["LSHIFT"].vk, scan:keyCodes["LSHIFT"].scan, isKeyUp:false, extended:false));
                if (alt) keyDownInputs.Add(CreateInput(vk:keyCodes["LALT"].vk, scan:keyCodes["LALT"].scan, isKeyUp:false, extended:false));
                //---------------------------------------------

                // -------- Add Key Down and Up Events For Main Key --------               
                if (!customUnicode)
                {
                    keyDownInputs.Add(CreateInput(vk: codes.vk, scan: codes.scan, isKeyUp: false, extended: isExtended, scanFlag: scanOnly, unicodeFlag: customUnicode));
                    keyUpInputs.Add(CreateInput(vk: codes.vk, scan: codes.scan, isKeyUp: true, extended: isExtended, scanFlag: scanOnly, unicodeFlag: customUnicode));
                }
                // If customUnicode is true, use loops to add as many surrogate pairs as necessary to the key down and key up lists
                else
                {
                    // Key Down
                    foreach (ushort unicodeCode in unicodeCodesArray)
                    {
                        keyDownInputs.Add(CreateInput(vk: 0, scan: unicodeCode, isKeyUp: false, extended: false, scanFlag: false, unicodeFlag: true));
                        
                    }
                    // Key Up
                    foreach (ushort unicodeCode in unicodeCodesArray)
                    {
                        keyUpInputs.Add(CreateInput(vk: 0, scan: unicodeCode, isKeyUp: true, extended: false, scanFlag: false, unicodeFlag: true));
                    }
                }
                //-------------------------------------------------

                // -------- Add Key Up For Modifiers --------
                if (alt) keyUpInputs.Add(CreateInput(vk:keyCodes["LALT"].vk, scan: keyCodes["LALT"].scan, isKeyUp:true, extended:false));
                if (shift) keyUpInputs.Add(CreateInput(vk:keyCodes["LSHIFT"].vk, scan:keyCodes["LSHIFT"].scan, isKeyUp:true, extended:false));
                if (ctrl) keyUpInputs.Add(CreateInput(vk:keyCodes["LCTRL"].vk, scan:keyCodes["LCTRL"].scan, isKeyUp:true, extended:false));
                //------------------------------------------

                try
                {
                    // Send key down inputs all at once
                    // SendInput: First parameter is the number of INPUT structures, second is the array of inputs, third is the size of the INPUT struct
                    SendInput((uint)keyDownInputs.Count, keyDownInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));

                    // Wait for the key press duration using Task.Delay
                    this.Invoke((MethodInvoker)delegate
                    {
                        labelToolstripStatus.Text = "Status: Holding Key...";
                        labelToolstripStatus.ForeColor = Color.Green;
                    });

                    await Task.Delay((int)nudDuration.Value, ct);

                    ct.ThrowIfCancellationRequested();
                }
                finally
                {
                    // Send key up inputs all at once
                    SendInput((uint)keyUpInputs.Count, keyUpInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));

                    // Re-enable all buttons after keys are released
                    this.Invoke((MethodInvoker)delegate
                    {
                        All_Buttons_Enabler();
                        labelToolstripStatus.Text = "Status: Ready";
                        labelToolstripStatus.ForeColor = Color.Black;
                        btnCancel.Visible = false;
                    });
                }
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

            // Disable numeric updowns
            nudDelay.Enabled = false;
            nudDuration.Enabled = false;

            // Disable dropdown
            dropdownMethod.Enabled = false;
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

            // Enable numeric updowns
            nudDelay.Enabled = true;
            nudDuration.Enabled = true;

            // Enable dropdown
            dropdownMethod.Enabled = true;
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

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                labelToolstripStatus.Text = "Status: Cancelling...";
                labelToolstripStatus.ForeColor = Color.Orange;
                btnCancel.Enabled = false;
            }
        }

        // Radio button to choose to use custom SC (scan) code in textBoxCustomCode
        private void radioButtonSC_CheckedChanged(object sender, EventArgs e)
        {
            updateHexLabel();
        }

        // Radio button to choose to use custom VK (virtual key) code in textBoxCustomCode
        private void radioButtonVK_CheckedChanged(object sender, EventArgs e)
        {
            updateHexLabel();
        }

        // Radio button to choose to use a unicode code in textBoxCustomCode
        private void radioButtonUnicode_CheckedChanged(object sender, EventArgs e)
        {
            updateHexLabel();
        }

        private void textBoxCustomCode_TextChanged(object sender, EventArgs e)
        {

        }

        private void updateHexLabel()
        {
            if (radioButtonVK.Checked)
            {
                labelHexPrefix.Text = "0x";
            }
            else if (radioButtonSC.Checked)
            {
                labelHexPrefix.Text = "0x";
            }
            else if (radioButtonUnicode.Checked)
            {
                labelHexPrefix.Text = "U+";
            }
        }

        private void buttonSendCustomKey_Click(object sender, EventArgs e)
        {
            string inputString = textBoxCustomCode.Text;

            // ---------- Handle invalid inputs ----------

            // If it's empty then display error
            if (string.IsNullOrEmpty(inputString))
            {
                MessageBox.Show("Please enter a custom key code.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // If no radio buttons are checked then display error
            if (!radioButtonVK.Checked && !radioButtonSC.Checked && !radioButtonUnicode.Checked)
            {
                MessageBox.Show("To send a custom key, you must select a type of code.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // If Unicode radio button is checked, and keybd_event is selected, display error
            if (radioButtonUnicode.Checked && dropdownMethod.SelectedIndex == 1)
            {
                MessageBox.Show("Unicode key codes are not supported with keybd_event method. Use SendInput instead.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // -------------------------------------------
            // Trim any leading or trailing whitespace
            inputString = inputString.Trim();
            // Check if the input starts with "0x" or "U+" and remove it if true
            if (inputString.ToLower().StartsWith("0x"))
            {
                inputString = inputString.Substring(2);
            }
            else if (inputString.ToLower().StartsWith("u+"))
            {
                inputString = inputString.Substring(2);
            }

            // If it contains any non-hexadecimal characters except a space or starting with 0x U+ then display error
            if (!System.Text.RegularExpressions.Regex.IsMatch(inputString, @"^[0-9A-Fa-f\s]+$"))
            {
                MessageBox.Show("Please enter a valid hexadecimal key code.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }


            if (radioButtonVK.Checked)
            {
                SendKeyCombo(inputString, customVK:true, customSC:false);
            }
            else if (radioButtonSC.Checked)
            {
                SendKeyCombo(inputString, customVK: false, customSC:true);
            }
            else if (radioButtonUnicode.Checked)
            {
                SendKeyCombo(inputString, customVK: false, customSC: false, customUnicode: true);
            }
        }

        // Convert string to ushort for both VK and SC
        private (ushort, bool) StringToUShort(string input) { 
            // Determine if it's an extended key starting with E0
            input = input.ToUpper();
            bool isExtended = false;

            if (input.StartsWith("E0") && input.Length > 2)
            {
                isExtended = true;
            }

            if (isExtended)
            {
                // Remove E0 and convert to ushort
                return (ushort.Parse(input.Substring(2), System.Globalization.NumberStyles.HexNumber), true);
            }
            else
            {
                // Convert to ushort
                return (ushort.Parse(input, System.Globalization.NumberStyles.HexNumber), false);
            }
        }

        // Function for Unicode specifically, handles both UTF-16 and UTF-32 code points to return surrogate pairs if necessary
        // Returns array of ushort values
        private ushort[] UnicodeToUShortArray(string input)
        {
            // Parse the hexadecimal string to an integer
            if (!int.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
            {
                throw new ArgumentException("Invalid Unicode code point");
            }

            // Convert the code point to UTF-16
            string utf16String = char.ConvertFromUtf32(codePoint);

            // Convert each UTF-16 character to ushort
            return utf16String.Select(c => (ushort)c).ToArray();
        }

        // Display message box with info about using custom key codes
        private void buttonCustomInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                " ----- Advanced Optional Custom Codes -----\n\n" +
                "Allows you to specify a custom hex value to send as a\n" +
                "Virtual Key (VK), Scan Code (SC), or Unicode Codepoint\n\n" +

                "Modifier checkboxes will still be applied.\n\n\n" +

                "For VK and SC:\n" +
                "    This should only be 1 byte (2 characters), except\n" +
                "    in the case of \"extended\" scan codes which are\n" +
                "    two bytes, starting with E0.\n\n" +

                "For Unicode:\n" +
                "    This should be a 2-byte UTF-16 code point.\n\n\n" +

                "Valid Scan Code & Virtual Key Examples:\n" +
                "    0x3B\n" +
                "    0xE05D\n" +
                "    0x003B  (First 00 will be removed)\n\n" + 

                "Valid Unicode Examples:\n" +
                "    U+0041\n" +
                "    U+03A9\n" +
                "    U+1F600\n\n"
                ,
                "Custom Key Code Info",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
    }


}