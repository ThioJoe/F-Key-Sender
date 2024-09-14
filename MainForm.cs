using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace F_Key_Sender
{
    public partial class MainForm : Form
    {
        const string VERSION = "1.1.1";

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

        // UI Constants
        const int statusBarHeightDefault = 22;

        private readonly Stopwatch stopwatch = new Stopwatch();

        public MainForm()
        {
            InitializeComponent();
            this.TopMost = true;
            stopwatch.Start();
            dropdownMethod.SelectedIndex = 0;

            VerifyAssemblyVersion();

            // Set Version Label
            labelVersion.Text = $"Version: {VERSION}";
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
                statusStrip1.Height = statusBarHeightDefault;
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
                    statusStrip1.Height = statusBarHeightDefault;
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
                        statusStrip1.Height = statusBarHeightDefault;
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
                        statusStrip1.Height = statusBarHeightDefault;
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

            // Status-Related flags for later use
            bool warnDuplicateUnicode = false; // If true, display a warning about duplicate Unicode code points
            

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

                // Check for duplicate Unicode code points besides Zero-Width Joiners, to tell user only one can be printed at a time
                warnDuplicateUnicode = CheckDuplicateUnicodeCodepoints(unicodeCodesArray);
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
                    statusStrip1.Height = statusBarHeightDefault;
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
                    keyDownInputs.Add(CreateInput(vk: codes.vk, scan: codes.scan, isKeyUp: false, extended: isExtended, scanFlag: scanOnly, unicodeFlag: false));
                    keyUpInputs.Add(CreateInput(vk: codes.vk, scan: codes.scan, isKeyUp: true, extended: isExtended, scanFlag: scanOnly, unicodeFlag: false));
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

                // Count the number of key down and key up inputs that were sent vs how many expected to be sent
                uint upToSend = (uint)keyUpInputs.Count;
                uint downToSend = (uint)keyDownInputs.Count;
                uint upSentCount = 0;
                uint downSentCount = 0;

                try
                {
                    // Send key down inputs all at once
                    // SendInput: First parameter is the number of INPUT structures, second is the array of inputs, third is the size of the INPUT struct
                    downSentCount = SendInput((uint)keyDownInputs.Count, keyDownInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));

                    // Wait for the key press duration using Task.Delay
                    this.Invoke((MethodInvoker)delegate
                    {
                        labelToolstripStatus.Text = "Status: Holding Key...";
                        labelToolstripStatus.ForeColor = Color.Green;
                        statusStrip1.Height = statusBarHeightDefault;
                    });

                    await Task.Delay((int)nudDuration.Value, ct);

                    ct.ThrowIfCancellationRequested();
                }
                finally
                {
                    // Send key up inputs all at once
                    upSentCount = SendInput((uint)keyUpInputs.Count, keyUpInputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));

                    // Re-enable all buttons after keys are released
                    this.Invoke((MethodInvoker)delegate
                    {
                        All_Buttons_Enabler();
                        btnCancel.Visible = false;

                        bool downSendFail = false;
                        bool upSendFail = false;

                        // Check if the number of key down and key up inputs sent match the number expected to be sent
                        if (downSentCount != downToSend || upSentCount != upToSend)
                        {
                            downSendFail = downSentCount != downToSend;
                            upSendFail = upSentCount != upToSend;

                            // Construct message based on which side had the issue or both
                            string message = "Warning: Not all keys sent successfully. Unfortunately Windows does not provide specifics on exactly which keys failed.\n";
                            if (downSendFail)
                            {
                                message += $"Problem sending down inputs.\nExpected: {downToSend}, Sent: {downSentCount}\n\n";
                            }
                            if (upSendFail)
                            {
                                message += $"Error sending key up inputs.\nExpected: {upToSend}, Sent: {upSentCount}\n\n";
                            }

                            // Use GetLastError to get the error code
                            int error = Marshal.GetLastWin32Error();

                            message += $"Error Code: {error}";

                            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        // Update status bar text
                        if (warnDuplicateUnicode)
                        {
                            string warningStatus = "Warning: Duplicate Unicode Codepoints detected.\n" +
                                                   "                 Only one of each can be sent at a time.";
                            labelToolstripStatus.Text = warningStatus;
                            labelToolstripStatus.ForeColor = Color.SaddleBrown;
                            // Increase height of toolstrip to accommodate the warning message
                            // Count number of newlines in the warning message to determine how much to increase the height
                            int newLines = warningStatus.Count(c => c == '\n');
                            statusStrip1.Height = (newLines + 1) * statusBarHeightDefault;
                        }
                        else if (!downSendFail && !upSendFail)
                        {
                            labelToolstripStatus.Text = "Status: Success";
                            labelToolstripStatus.ForeColor = Color.Black;
                            // Reset height of toolstrip
                            statusStrip1.Height = statusBarHeightDefault;
                        }
                        else
                        {
                            labelToolstripStatus.Text = "Status: Ready - Problems Occurred Last Run";
                            labelToolstripStatus.ForeColor = Color.SaddleBrown;
                            // Reset height of toolstrip
                            statusStrip1.Height = statusBarHeightDefault;
                        }
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
                statusStrip1.Height = statusBarHeightDefault;
                btnCancel.Enabled = false;
            }
        }

        // Radio button to choose to use custom SC (scan) code in textBoxCustomCode
        private void radioButtonSC_CheckedChanged(object sender, EventArgs e)
        {
            updateHexLabel();
            NormalizeTextBoxText();
        }

        // Radio button to choose to use custom VK (virtual key) code in textBoxCustomCode
        private void radioButtonVK_CheckedChanged(object sender, EventArgs e)
        {
            updateHexLabel();
            NormalizeTextBoxText();
        }

        // Radio button to choose to use a unicode code in textBoxCustomCode
        private void radioButtonUnicode_CheckedChanged(object sender, EventArgs e)
        {
            updateHexLabel();
            NormalizeTextBoxText();
        }

        private void textBoxCustomCode_TextChanged(object sender, EventArgs e)
        {
            // If the text box is empty, disable the send button
            if (string.IsNullOrEmpty(textBoxCustomCode.Text))
            {
                buttonSendCustomKey.Enabled = false;
                // Return early to avoid further processing
                return;
            }
            else
            {
                buttonSendCustomKey.Enabled = true;
            }

            NormalizeTextBoxText();
        }

        private void NormalizeTextBoxText()
        {
            // Set string to process
            string inputString = textBoxCustomCode.Text.ToUpper();

            // Clean up formatting for SC and VK codes
            if (radioButtonVK.Checked || radioButtonSC.Checked)
            {
                // Clean up formatting - Set to upper case, and remove 0x or U+
                if (inputString.StartsWith("0X"))
                {
                    inputString = inputString.Substring(2);
                }
            }

            // Don't process Unicode codes yet, we might need the full string to more easily identify zero-width joiners

            // Update the text box with the cleaned up string
            textBoxCustomCode.Text = inputString;
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
            if (inputString.ToUpper().StartsWith("0X"))
            {
                inputString = inputString.Substring(2);
            }
            else if (inputString.ToUpper().StartsWith("U+"))
            {
                inputString = inputString.Substring(2);
            }

            // Special processing for unicode code points
            if (radioButtonUnicode.Checked)
            {
                inputString = PrepareUnicodeString(inputString);
                if (inputString == null)
                {
                    return;
                }
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

        private string PrepareUnicodeString(string rawInput)
        {
            string input = rawInput.Trim();
            string inputNoSpaces = input.Replace(" ", "");

            // Replace multiple sequential spaces with a single space
            input = System.Text.RegularExpressions.Regex.Replace(input, @"\s+", " ");

            string finalString = null;
            // If the input excluding spaces is 5 characters or less, remove the spaces and return
            if (inputNoSpaces.Length <= 5)
            {
                return inputNoSpaces;
            }
            // Or if it's 6 characters and starts with a zero, remove the zero and return
            else if (inputNoSpaces.Length == 6 && inputNoSpaces.StartsWith("0"))
            {
                return inputNoSpaces.Substring(1);
            }

            // If input is longer than 5 characters, we can assume it's a zero-width joiner, so we must find a way to split each glyph and convert each codepoint to 5 characters
            // Check if "U+" is present, because the beginning would have already been stripped, so if it's still present we can use it to split the string
            if (input.Contains("U+"))
            {
                // Split the string by "U+" and remove any empty entries
                string[] codePoints = input.Split(new string[] { "U+" }, StringSplitOptions.RemoveEmptyEntries);

                finalString = ParseEachPossibleCodepoint(codePoints);

            }

            // If U+ is not present or splitting on it failed, we can try to split on spaces
            if (finalString == null && input.Contains(" "))
            {
                // Split the string by spaces and remove any empty entries
                string[] codePoints = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                finalString = ParseEachPossibleCodepoint(codePoints);
            }

            // At this point if the string length is divisible by 5, we will assume it's valid and return it
            if (finalString == null && input.Length % 5 == 0)
            {
                finalString = input;
            }
            // If it's divisible by 4, we will assume it's valid and add a leading zero to each codepoint before returning
            else if (finalString == null && input.Length % 4 == 0)
            {
                List<string> fixedCodepoints = new List<string>();  // List to hold the reformatted code points

                // Split the string into 4 character chunks
                for (int i = 0; i < input.Length; i += 4)
                {
                    string codePoint = input.Substring(i, 4);
                    // Add a leading zero to each codepoint
                    fixedCodepoints.Add("0" + codePoint);
                }

                // Return the fixed code points as a single string no spaces
                finalString = string.Join("", fixedCodepoints);
            }


            // If finalString is still null, display error
            if (finalString == null)
            {
                MessageBox.Show("Could not parse Unicode codepoints. Click the (?) info button for details on valid formats.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }

            return finalString;
        }

        private string ParseEachPossibleCodepoint(string[] codePoints)
        {
            List<string> fixedCodepoints = new List<string>();  // List to hold the reformatted code points

            // Check if the code points are valid
            foreach (string codePoint in codePoints)
            {
                string sanitizedCodepoint = codePoint.Trim();
                // If it's 6 characters but starts with a zero, remove the zero
                if (sanitizedCodepoint.Length == 6 && sanitizedCodepoint.StartsWith("0"))
                {
                    sanitizedCodepoint = sanitizedCodepoint.Substring(1);
                }
                else if (sanitizedCodepoint.Length != 4 && sanitizedCodepoint.Length != 5)
                {
                    return null;
                }

                // If any are 4 characters, add a leading zero to make all a consistant 5 characters
                if (sanitizedCodepoint.Length == 4)
                {
                    sanitizedCodepoint = "0" + sanitizedCodepoint;
                }

                // Add the fixed code point to the fixedCodePoints list
                fixedCodepoints.Add(sanitizedCodepoint);
            }

            // Return the fixed code points as a single string no spaces
            return string.Join("", fixedCodepoints);
        }

        // Function for Unicode specifically, handles both UTF-16 and UTF-32 code points to return surrogate pairs if necessary
        // Returns array of ushort values
        private ushort[] UnicodeToUShortArray(string input)
        {
            List<ushort> result = new List<ushort>();

            int chunkSize = input.Length > 4 ? 5 : 4;

            // Process the input in chunks of 4 or 5 characters (4 for BMP, 5 for higher planes)
            for (int i = 0; i < input.Length; i += chunkSize)
            {
                string codePointHex = input.Substring(i, Math.Min(chunkSize, input.Length - i));

                // Parse the hexadecimal string to an integer
                if (!int.TryParse(codePointHex, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
                {
                    throw new ArgumentException($"Invalid Unicode code point: {codePointHex}");
                }

                // Convert the code point to UTF-16
                string utf16String = char.ConvertFromUtf32(codePoint);

                // Add each UTF-16 character to the result
                result.AddRange(utf16String.Select(c => (ushort)c));
            }

            ushort[] finalArray = result.ToArray();

            return finalArray;
        }

        // Check if there are any duplicate Unicode code points in the array that are not zero-width joiners
        // Since the the keydown events are sent together, if any are sent twice, it will not print it twice
        private bool CheckDuplicateUnicodeCodepoints(ushort[] inputArray)
        {
            foreach (ushort codePoint in inputArray)
            {
                if (codePoint != 0x200D && inputArray.Count(c => c == codePoint) > 1)
                {
                    return true;
                }
            }
            return false;
        }

        // Display message box with info about using custom key codes
        private void buttonCustomInfo_Click(object sender, EventArgs e)
        {
            _ = MessageBox.Show(
                "-------- Advanced Optional Custom Codes --------\n\n" +
                "Allows you to specify a custom hex value to send as a\n" +
                "Virtual Key (VK), Scan Code (SC), or Unicode Codepoint\n\n" +

                "Modifier checkboxes will still be applied.\n\n" +

                "-------------------- Notes --------------------\n\n" +

                "For VK and SC:\n" +
                "    This should only be 1 byte (2 characters), except\n" +
                "    in the case of \"extended\" scan codes which are\n" +
                "    two bytes, starting with E0.\n\n" +

                "For Unicode:\n" +
                "    This should be a 4 or 5 character codepoint. If sending\n" +
                "     a glyph that uses a zero-width joiner like some emojis,\n" +
                "     all codepoints must be 5 characters or split by spaces or U+.\n\n" + 

                "-------------------- Examples --------------------\n\n" +

                "Valid Scan Code & Virtual Key Examples:\n" +
                "    0x3B\n" +
                "    0xE05D\n" +
                "    0x003B  (First 00 will be removed)\n\n" +

                "Valid Unicode Examples:\n" +
                "    U+0041\n" +
                "    U+03A9\n" +
                "    U+1F600\n\n" +

                "Unicode Zero-Width-Joiner Valid Examples:\n" +
                "    1F468 0200D 1F33E  (Split by spaces)\n" +
                "    1F4680200D1F33E    (Combined but all are 5 characters)\n" +
                "    U+2764 U+FE0F U+200D U+1F525  (Split by U+)"
                ,
                "Custom Key Code Info",
                MessageBoxButtons.OK,
                MessageBoxIcon.None
            );
        }

        // If in debug mode and the assembly version does not match the file version, display a warning message
        private void VerifyAssemblyVersion()
        {
#if DEBUG
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyVersion = assembly.GetName().Version.ToString();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            var fileVersion = new Version(fileVersionInfo.FileVersion).ToString();

            // Ignore the last digit of the version number if it's 0 and the file version doesn't use it by counting periods in the file version
            if (assemblyVersion.EndsWith(".0") && VERSION.Count(c => c == '.') < 3)
            {
                assemblyVersion = assemblyVersion.Substring(0, assemblyVersion.Length - 2);
            }
            if (fileVersion.EndsWith(".0") && VERSION.Count(c => c == '.') < 3)
            {
                fileVersion = fileVersion.Substring(0, fileVersion.Length - 2);
            }

            if (assemblyVersion != VERSION || fileVersion != VERSION)
            {
                string warningMessage = $"WARNING: Version mismatch detected!\n" +
                                        $"Expected version: {VERSION}\n" +
                                        $"Assembly version: {assemblyVersion}\n" +
                                        $"File version: {fileVersion}";

                Debug.WriteLine(warningMessage);
                MessageBox.Show(warningMessage, "Version Mismatch",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                Debug.WriteLine($"Version check passed. Current version: {VERSION}");
            }
#endif
        }
    }
}