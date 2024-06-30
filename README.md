# F13-F24 Key Sender Tool
 Send keystrokes for F13 to F24 keys via buttons, including with modifiers (Shift, Ctrl, Alt).

## Features:
- Selectable delay before keys are sent (to give you time to move focus to application you want to send the keystrokes to)
- Selectable key hold duration
- Choice between two virtual keypress methods:
  - `SendInput` (Default): Modern, more reliable method for simulating keystrokes. 
  - `keybd_event`: Older method for generating keyboard input in case SendInput doesn't work.
- Release Exe signed with EV code signing certificate (No pop up from Windows about untrusted software)

## Why?

Most modern keyboards only include function keys up to F12, but did you know that Windows supports function keys all the way up to F24? These additional function keys (F13-F24) can be incredibly useful, especially for power users, programmers, and gamers who want to maximize their productivity and customize their workflow.

Purpose of this tool:

- **Extend Your Keyboard's Capabilities**: Many keyboards, especially gaming keyboards or specialized macro pads, come with extra programmable keys. This tool allows you to map these physical keys to the often-overlooked F13-F24 virtual keys.
- **Unlock New Shortcuts**: Applications that support custom shortcuts can often utilize these extended function keys, giving you a whole new range of shortcut possibilities without conflicting with existing F1-F12 shortcuts.
- **Initial Key Assignment**: When setting up macro keys, you typically need to press the key combination you want to assign. This tool helps in that initial setup phase by allowing you to "press" F13-F24, which would otherwise be impossible on standard keyboards. Once assigned, your macro key will function as the corresponding F13-F24 key without needing this tool for regular use.

By providing a simple interface to send these virtual keystrokes, this tool opens up new possibilities for customization and efficiency in your daily computer use, whether for work, gaming, or any other purpose where extra programmable keys could be beneficial.

## Screenshot:
<img src="https://github.com/ThioJoe/F-Key-Sender/assets/12518330/167e6a9c-fdec-4e95-93ff-7bfa3555ad9f" width=50% height=50%>


