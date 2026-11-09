// Author: Thomas Andersen
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace QuickSlots;

internal static class Program
{
    [STAThread]
    private static void Main() { ApplicationConfiguration.Initialize(); Application.Run(new MainForm()); }
}

internal sealed class MainForm : Form
{
    private readonly string?[] slots = new string?[9];
    private readonly SlotOverlay overlay = new();
    private readonly Label status;
    private readonly KeyboardHook hook;
    private readonly string dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "QuickSlots", "slots.json");
    private int pendingCopy = -1;
    private uint clipboardBeforeCopy;
    private readonly System.Windows.Forms.Timer copyTimer = new() { Interval = 100 };
    private int copyAttempts;

    public MainForm()
    {
        Text = "QuickSlots — Version 0.12"; MinimumSize = new Size(420, 300); Size = new Size(480, 350);
        StartPosition = FormStartPosition.CenterScreen; Font = new Font("Segoe UI", 10); BackColor = Color.FromArgb(248, 249, 252);
        var heading = new Label { Text = "QuickSlots  ·  Version 0.12", Font = new Font("Segoe UI", 22, FontStyle.Bold), AutoSize = true, Location = new Point(24, 22) };
        var description = new Label { Text = "Your nine text slots are ready. Keep this app open to use the global shortcuts.", AutoSize = false, Size = new Size(410, 42), Location = new Point(26, 68), ForeColor = Color.FromArgb(83, 91, 105) };
        status = new Label { Text = "Starting keyboard shortcuts…", AutoSize = true, Location = new Point(26, 126), ForeColor = Color.FromArgb(29, 117, 83) };
        var shortcuts = new Label { Text = "HOLD  Right Alt     Show slots at bottom left\r\nSAVE   Right Alt + 1–9     Copy selected text into a slot\r\nPASTE  Left Alt + 1–9       Paste a saved slot", AutoSize = false, Size = new Size(410, 100), Location = new Point(26, 165), BackColor = Color.White, Padding = new Padding(14), ForeColor = Color.FromArgb(39, 47, 61) };
        Controls.AddRange([heading, description, status, shortcuts]);
        LoadSlots(); overlay.SetSlots(slots); copyTimer.Tick += FinishCopy;
        hook = new KeyboardHook(OnKey); hook.Install(); status.Text = "Shortcuts active";
        FormClosed += (_, _) => { hook.Dispose(); overlay.Close(); copyTimer.Dispose(); };
        overlay.Show(); overlay.Hide();
    }

    private void OnKey(int key, bool down, bool injected)
    {
        if (injected) return;
        if (key == Native.VK_RMENU) { if (down) overlay.Show(); else overlay.Hide(); return; }
        if (!down || key < Native.VK_1 || key > Native.VK_9) return;
        int slot = key - Native.VK_1;
        if (Native.IsKeyDown(Native.VK_RMENU))
        {
            pendingCopy = slot; copyAttempts = 0; clipboardBeforeCopy = Native.GetClipboardSequenceNumber();
            Native.SendCtrlKey('C', Native.VK_RMENU); copyTimer.Stop(); copyTimer.Start(); return;
        }
        if (Native.IsKeyDown(Native.VK_LMENU) && slots[slot] is { } value)
        {
            try { Clipboard.SetText(value); Native.SendCtrlKey('V', Native.VK_LMENU); status.Text = $"Pasted slot {slot + 1}"; }
            catch (ExternalException) { status.Text = "Could not access the clipboard. Try again."; }
        }
    }

    private void FinishCopy(object? sender, EventArgs e)
    {
        copyTimer.Stop(); if (pendingCopy < 0) return;
        try
        {
            if (Native.GetClipboardSequenceNumber() != clipboardBeforeCopy && Clipboard.ContainsText())
            {
                slots[pendingCopy] = Clipboard.GetText(); overlay.SetSlots(slots); SaveSlots();
                status.Text = $"Saved selected text to slot {pendingCopy + 1}"; pendingCopy = -1; return;
            }
        }
        catch (ExternalException) { }
        if (++copyAttempts < 10) copyTimer.Start();
        else { status.Text = "No selected text was copied."; pendingCopy = -1; }
    }

    private void LoadSlots()
    {
        try { var saved = JsonSerializer.Deserialize<string?[]>(File.ReadAllText(dataPath)); if (saved is not null) Array.Copy(saved, slots, Math.Min(saved.Length, slots.Length)); }
        catch (IOException) { } catch (JsonException) { }
    }

    private void SaveSlots()
    {
        try { Directory.CreateDirectory(Path.GetDirectoryName(dataPath)!); File.WriteAllText(dataPath, JsonSerializer.Serialize(slots)); }
        catch (IOException) { status.Text = "Slot saved for this session; could not save it to disk."; }
    }
}

internal sealed class SlotOverlay : Form
{
    private readonly Label[] rows = new Label[9];
    // The overlay must never take focus away from the app whose selection is being copied.
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
            return parameters;
        }
    }

    public SlotOverlay()
    {
        FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true; BackColor = Color.FromArgb(24, 28, 37); ForeColor = Color.White; Padding = new Padding(14); Size = new Size(350, 330);
        var title = new Label { Text = "QUICKSLOTS     RIGHT ALT + 1–9 TO SAVE", AutoSize = false, Height = 28, Dock = DockStyle.Top, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(172, 188, 211) };
        Controls.Add(title);
        var list = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 9, BackColor = Color.Transparent };
        list.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36)); list.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 9; i++)
        {
            list.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 9));
            list.Controls.Add(new Label { Text = (i + 1).ToString(), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(112, 196, 255) }, 0, i);
            rows[i] = new Label { Text = "(empty)", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoEllipsis = true, Font = new Font("Segoe UI", 10), ForeColor = Color.White };
            list.Controls.Add(rows[i], 1, i);
        }
        Controls.Add(list); list.BringToFront(); Shown += (_, _) => PlaceAtBottomLeft();
    }
    public void SetSlots(string?[] slots)
    {
        for (var i = 0; i < rows.Length; i++) rows[i].Text = string.IsNullOrEmpty(slots[i]) ? "(empty)" : string.Join(" ", slots[i]!.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
    private void PlaceAtBottomLeft() { var area = Screen.FromPoint(Cursor.Position).WorkingArea; Location = new Point(area.Left + 18, area.Bottom - Height - 18); }
}

internal sealed class KeyboardHook : IDisposable
{
    private readonly Action<int, bool, bool> callback; private readonly Native.LowLevelKeyboardProc procedure; private IntPtr handle;
    private bool rightAltDown, leftAltDown;
    public KeyboardHook(Action<int, bool, bool> callback) { this.callback = callback; procedure = Hook; }
    public void Install()
    {
        handle = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, procedure, Native.GetModuleHandle(null), 0);
        if (handle == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not start the global keyboard shortcut listener.");
    }
    private IntPtr Hook(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            var info = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(data);
            bool down = message == (IntPtr)Native.WM_KEYDOWN || message == (IntPtr)Native.WM_SYSKEYDOWN;
            bool up = message == (IntPtr)Native.WM_KEYUP || message == (IntPtr)Native.WM_SYSKEYUP;
            if (down || up)
            {
                bool injected = (info.flags & Native.LLKHF_INJECTED) != 0;
                if (!injected)
                {
                    if (info.vkCode == Native.VK_RMENU) rightAltDown = down;
                    if (info.vkCode == Native.VK_LMENU) leftAltDown = down;
                }
                callback((int)info.vkCode, down, injected);
                if (!injected && (down || up) && info.vkCode is >= Native.VK_1 and <= Native.VK_9 && (rightAltDown || leftAltDown)) return (IntPtr)1;
            }
        }
        return Native.CallNextHookEx(handle, code, message, data);
    }
    public void Dispose() { if (handle != IntPtr.Zero) { Native.UnhookWindowsHookEx(handle); handle = IntPtr.Zero; } }
}

internal static class Native
{
    internal const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
    internal const int VK_1 = 0x31, VK_9 = 0x39, VK_LMENU = 0xA4, VK_RMENU = 0xA5, LLKHF_INJECTED = 0x10;
    [StructLayout(LayoutKind.Sequential)] internal struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public UIntPtr dwExtraInfo; }
    internal delegate IntPtr LowLevelKeyboardProc(int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr module, uint threadId);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] internal static extern IntPtr GetModuleHandle(string? moduleName);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern uint GetClipboardSequenceNumber();
    internal static bool IsKeyDown(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
    internal static void SendCtrlKey(int key, int altKey)
    {
        var inputs = new[] { Input.Key(altKey, true), Input.Key(0x11, false), Input.Key(key, false), Input.Key(key, true), Input.Key(0x11, true), Input.Key(altKey, false) };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }
    [StructLayout(LayoutKind.Explicit, Size = 40)] private struct Input { [FieldOffset(0)] public uint type; [FieldOffset(8)] public InputUnion union; public static Input Key(int key, bool up) => new() { type = 1, union = new InputUnion { keyboard = new KeyboardInput { virtualKey = (ushort)key, flags = (up ? 2u : 0u) | (key == VK_RMENU ? 1u : 0u) } } }; }
    [StructLayout(LayoutKind.Explicit, Size = 32)] private struct InputUnion { [FieldOffset(0)] public KeyboardInput keyboard; }
    [StructLayout(LayoutKind.Sequential)] private struct KeyboardInput { public ushort virtualKey, scanCode; public uint flags, time; public UIntPtr extraInfo; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, Input[] inputs, int size);
}
