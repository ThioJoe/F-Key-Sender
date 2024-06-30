# F13-F24 Key Sender Tool
 Send keystrokes for F13 to F24 keys via buttons, including with modifiers (Shift, Ctrl, Alt).

## Features:
- Selectable delay before keys are sent (to give you time to move focus to application you want to send the keystrokes to)
- Selectable key hold duration
- Choice between two virtual keypress methods:
  - `SendInput` (Default): Modern, more reliable method for simulating keystrokes. 
  - `keybd_event`: Older method for generating keyboard input in case SendInput doesn't work.
- Release Exe signed with EV code signing certificate (No pop up from Windows about untrusted software)

## Screenshot:
![Window Screenshot](https://github.com/ThioJoe/F-Key-Sender/assets/12518330/07053027-a58f-4454-b017-d803925fca43)
