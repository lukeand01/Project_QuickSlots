# QuickSlots

QuickSlots is a small Windows desktop app for nine reusable text snippets. Author: Thomas Andersen. Current version: 0.12.

**[Download QuickSlots for Windows (standalone .exe)](https://github.com/lukeand01/Project_QuickSlots/releases/download/v0.12/QuickSlots.exe)**

Download and run the executable. It is a self-contained Windows x64 build and needs no installer or separate .NET runtime. Keep the app open while using its shortcuts. Slot contents are saved under `%LOCALAPPDATA%\QuickSlots\slots.json`.

- Hold **Right Alt** to show the slot panel in the bottom-left corner.
- Press **Right Alt + 1–9** while text is selected to copy it into that slot.
- Press **Left Alt + 1–9** to paste a saved slot into the active app.

To build from source on Windows with the .NET 9 SDK installed, run `publish.bat`. The standalone app is written to `publish-v0.12\QuickSlots.exe`.
