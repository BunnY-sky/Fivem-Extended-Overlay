using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FiveMOverlay;

/// <summary>Replaces configured slash shortcuts in the FiveM chat without submitting the chat line.</summary>
internal sealed class BrChatShortcut : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_SPACE = 0x20;
    private const int VK_BACK = 0x08;
    private const int VK_SHIFT = 0x10;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;
    private const int VK_OEM_2 = 0xBF;
    private const int VK_7 = 0x37;
    private const int VK_OEM_MINUS = 0xBD;
    private const int LLKHF_INJECTED = 0x10;
    private const string CursorToken = "{cursor}";

    private readonly LowLevelKeyboardProc _proc;
    private readonly Func<IReadOnlyList<ChatCommandSetting>> _commandsProvider;
    private IntPtr _hook;
    private readonly List<char> _recent = new();
    private bool _shiftDown;
    private DateTime _lastKeyTime = DateTime.MinValue;

    public BrChatShortcut(Func<IReadOnlyList<ChatCommandSetting>> commandsProvider)
    {
        _commandsProvider = commandsProvider;
        _proc = HookCallback;
        _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
        {
            var released = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            int releasedVk = (int)released.vkCode;
            if (releasedVk == VK_SHIFT || releasedVk == VK_LSHIFT || releasedVk == VK_RSHIFT) _shiftDown = false;
        }

        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if ((data.flags & LLKHF_INJECTED) == 0)
            {
                int vk = (int)data.vkCode;
                if (vk == VK_SHIFT || vk == VK_LSHIFT || vk == VK_RSHIFT) _shiftDown = true;
                else if (vk == VK_SPACE && IsFiveMForeground())
                {
                    var match = FindMatchingCommand();
                    if (match != null)
                    {
                        _recent.Clear();
                        ReplaceInput(match);
                        return (IntPtr)1; // swallow Space; do not submit or extend the original shortcut
                    }
                    _recent.Clear();
                }
                else if (IsFiveMForeground())
                {
                    if (vk == VK_BACK)
                    {
                        _recent.Clear();
                    }
                    else
                    {
                        char? ch = TranslateShortcutCharacter(vk, _shiftDown);
                        if (ch.HasValue)
                        {
                            var now = DateTime.UtcNow;
                            if ((now - _lastKeyTime).TotalSeconds > 4) _recent.Clear();

                            // Start a fresh candidate whenever a slash is typed. This is important
                            // because opening FiveM chat with T also reaches the global keyboard hook;
                            // without resetting here, the buffer could contain "t/br" instead of "/br".
                            if (ch.Value == '/') _recent.Clear();

                            _recent.Add(char.ToLowerInvariant(ch.Value));
                            int maxTriggerLength = 3;
                            try
                            {
                                maxTriggerLength = Math.Max(3, _commandsProvider()
                                    .Where(c => !string.IsNullOrWhiteSpace(c.Trigger))
                                    .Select(c => c.Trigger.Trim().Length)
                                    .DefaultIfEmpty(3).Max());
                            }
                            catch { }
                            while (_recent.Count > maxTriggerLength) _recent.RemoveAt(0);
                            _lastKeyTime = now;
                        }
                        else
                        {
                            _recent.Clear();
                        }
                    }
                }
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private ChatCommandSetting? FindMatchingCommand()
    {
        if (_recent.Count == 0 || (DateTime.UtcNow - _lastKeyTime).TotalSeconds > 4) return null;
        string typed = new(_recent.ToArray());
        try
        {
            // Prefer the longest trigger if users configured overlapping shortcuts.
            return _commandsProvider()
                .Where(c => !string.IsNullOrWhiteSpace(c.Trigger))
                .OrderByDescending(c => c.Trigger.Length)
                .FirstOrDefault(c => string.Equals(typed, c.Trigger.Trim(), StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    private static char? TranslateShortcutCharacter(int vk, bool shift)
    {
        if (vk == VK_OEM_2 || (vk == VK_7 && shift)) return '/';
        if (vk >= 0x41 && vk <= 0x5A) return (char)(shift ? vk : vk + 32); // A-Z, normally lowercase command input
        if (vk >= 0x30 && vk <= 0x39 && !shift) return (char)vk;
        if (vk == VK_OEM_MINUS) return shift ? '_' : '-';
        if (vk == 0xBC && !shift) return ',';
        if (vk == 0xBE && !shift) return '.';
        return null;
    }

    private static bool IsFiveMForeground()
    {
        GetWindowThreadProcessId(GetForegroundWindow(), out uint pid);
        if (pid == 0) return false;
        try
        {
            string name = Process.GetProcessById((int)pid).ProcessName;
            return name.StartsWith("FiveM_GTAProcess", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("FiveM", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private static void ReplaceInput(ChatCommandSetting command)
    {
        string trigger = command.Trigger.Trim();
        string replacement = command.Replacement ?? string.Empty;
        int cursorIndex = replacement.IndexOf(CursorToken, StringComparison.OrdinalIgnoreCase);
        string output = cursorIndex >= 0 ? replacement.Remove(cursorIndex, CursorToken.Length) : replacement;
        int charsAfterCursor = cursorIndex >= 0 ? replacement.Length - cursorIndex - CursorToken.Length : 0;

        // Space was swallowed, so only the trigger itself is currently in the chat field.
        for (int i = 0; i < trigger.Length; i++) SendKey(VK_BACK);
        foreach (char c in output) SendUnicode(c);
        for (int i = 0; i < charsAfterCursor; i++) SendKey(0x25, extended: true); // Left arrow to {cursor}
    }

    private static void SendKey(ushort vk, bool extended = false)
    {
        uint flags = extended ? 0x0001u : 0u;
        var inputs = new INPUT[2];
        inputs[0] = new INPUT { type = 1, U = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } } };
        inputs[1] = new INPUT { type = 1, U = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags | 0x0002u } } };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void SendUnicode(char c)
    {
        var inputs = new INPUT[2];
        inputs[0] = new INPUT { type = 1, U = new INPUTUNION { ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = 0x0004 } } };
        inputs[1] = new INPUT { type = 1, U = new INPUTUNION { ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = 0x0004 | 0x0002 } } };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero) { UnhookWindowsHookEx(_hook); _hook = IntPtr.Zero; }
        GC.KeepAlive(_proc);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)] private struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public INPUTUNION U; }
    [StructLayout(LayoutKind.Explicit)] private struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }
    [StructLayout(LayoutKind.Sequential)] private struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }
    [StructLayout(LayoutKind.Sequential)] private struct HARDWAREINPUT { public uint uMsg; public ushort wParamL, wParamH; }
    [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string? lpModuleName);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll", SetLastError = true)] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);
}
