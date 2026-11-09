# QuickSlots

QuickSlots is a small Windows desktop app for nine reusable text snippets. Author: Thomas Andersen. Current version: 0.12.

Open `QuickSlots.exe`. The app window confirms the shortcuts are active; keep it open while using them. Slot contents are saved under `%LOCALAPPDATA%\\QuickSlots\\slots.json`.

- Hold **Right Alt** to show the slot panel in the bottom-left corner.
- Press **Right Alt + 1–9** while text is selected to copy it into that slot.
- Press **Left Alt + 1–9** to paste a saved slot into the active app.

On Windows with the .NET 9 SDK installed, run `publish.bat`. The standalone app is written to `publish-v0.12\\QuickSlots.exe` and does not require a separate .NET install.
