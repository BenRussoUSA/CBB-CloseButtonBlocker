using System.Runtime.InteropServices;
using System.Text;

namespace CBB;

public static class NativeWindows
{
    public const uint CloseCommand = 0xF060;
    public const uint MissingMenuItem = uint.MaxValue;
    public delegate bool EnumProc(nint hwnd, nint param);
    public delegate void WinEventProc(nint hook, uint evt, nint hwnd, int obj, int child, uint thread, uint time);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, nint param);
    [DllImport("user32.dll")] public static extern bool IsWindow(nint hwnd);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(nint hwnd);
    [DllImport("user32.dll")] public static extern nint GetWindow(nint hwnd, uint command);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(nint hwnd, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(nint hwnd, out uint processId);
    [DllImport("user32.dll")] public static extern nint GetSystemMenu(nint hwnd, bool revert);
    [DllImport("user32.dll")] public static extern uint GetMenuState(nint menu, uint id, uint flags);
    [DllImport("user32.dll")] public static extern uint EnableMenuItem(nint menu, uint id, uint flags);
    [DllImport("user32.dll")] public static extern bool DrawMenuBar(nint hwnd);
    [DllImport("user32.dll")] public static extern nint SetWinEventHook(uint min, uint max, nint module, WinEventProc callback, uint process, uint thread, uint flags);
    [DllImport("user32.dll")] public static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll")] public static extern bool DestroyIcon(nint icon);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint hwnd, uint attr, out int value, int size);

    public static uint CloseState(nint hwnd)
    {
        var menu = GetSystemMenu(hwnd, false);
        return menu == 0 ? MissingMenuItem : GetMenuState(menu, CloseCommand, 0);
    }

    public static bool SetCloseState(nint hwnd, uint state)
    {
        var menu = GetSystemMenu(hwnd, false);
        if (menu == 0 || EnableMenuItem(menu, CloseCommand, state & 3) == MissingMenuItem) return false;
        DrawMenuBar(hwnd);
        var result = CloseState(hwnd);
        return result != MissingMenuItem && (result & 3) == (state & 3);
    }

    public static List<WindowEntry> List()
    {
        var result = new List<WindowEntry>();
        EnumWindows((hwnd, _) =>
        {
            if (!IsWindowVisible(hwnd) || GetWindow(hwnd, 4) != 0) return true;
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == Environment.ProcessId) return true;
            if (DwmGetWindowAttribute(hwnd, 14, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
            var title = new StringBuilder(1024);
            GetWindowText(hwnd, title, title.Capacity);
            if (title.Length > 0) result.Add(new(hwnd, pid, title.ToString()));
            return true;
        }, 0);
        return result;
    }
}

public sealed record WindowEntry(nint Handle, uint ProcessId, string Title);

public sealed class WindowController : IDisposable
{
    private sealed class State(uint pid)
    {
        public uint ProcessId = pid;
        public uint? Original;
        public bool Manual;
        public bool RuleApplied;
    }
    private readonly Dictionary<nint, State> tracked = [];
    private readonly NativeWindows.WinEventProc callback;
    private readonly nint hook;
    private readonly Queue<nint> destroyed = new();
    public List<WindowEntry> Windows { get; private set; } = [];
    public Dictionary<nint, string> Errors { get; } = [];
    public bool LifecycleHookAvailable => hook != 0;

    public WindowController()
    {
        // Out-of-context events are delivered on this UI thread. Never call UI from the callback.
        callback = (_, evt, hwnd, obj, child, _, _) =>
        {
            if (obj == 0 && child == 0 && hwnd != 0) destroyed.Enqueue(hwnd);
        };
        hook = NativeWindows.SetWinEventHook(0x8001, 0x8001, 0, callback, 0, 0, 0);
    }

    public void Scan(IReadOnlyList<WindowRule> rules, bool reconsider = false)
    {
        while (destroyed.TryDequeue(out var handle)) { tracked.Remove(handle); Errors.Remove(handle); }
        foreach (var handle in tracked.Keys.Where(h => !NativeWindows.IsWindow(h)).ToArray())
        { tracked.Remove(handle); Errors.Remove(handle); }
        Windows = NativeWindows.List();
        foreach (var window in Windows)
        {
            bool fresh = !tracked.TryGetValue(window.Handle, out var state) || state.ProcessId != window.ProcessId;
            if (fresh) tracked[window.Handle] = state = new(window.ProcessId);
            if ((fresh || reconsider) && !state!.Manual && !state.RuleApplied && rules.Any(r => r.Matches(window.Title)))
            {
                state.RuleApplied = true;
                Change(window, false, false);
            }
        }
    }

    public string? Change(WindowEntry window, bool closeOn, bool manual = true)
    {
        if (!NativeWindows.IsWindow(window.Handle)) return "This window has closed.";
        NativeWindows.GetWindowThreadProcessId(window.Handle, out var pid);
        if (pid != window.ProcessId) return "This window has been replaced. Refresh the list.";
        if (!tracked.TryGetValue(window.Handle, out var state)) tracked[window.Handle] = state = new(pid);
        if (manual) state.Manual = true;
        uint current = NativeWindows.CloseState(window.Handle);
        string? error = null;
        if (current == NativeWindows.MissingMenuItem) error = "This window does not expose a standard Windows Close command.";
        else if (!closeOn && (current & 3) != 0 && state.Original == null)
            error = "This application's Close command is already disabled. CBB will not take ownership of it.";
        else if (closeOn && state.Original == null && (current & 3) != 0)
            error = "CBB did not disable this button. Its original state will be preserved.";
        else
        {
            uint desired = closeOn ? state.Original ?? 0 : 3u;
            if (!NativeWindows.SetCloseState(window.Handle, desired)) error = "Windows did not accept the change. The application may require elevated access or use a custom title bar.";
            else if (closeOn) state.Original = null;
            else state.Original ??= current & 3;
        }
        if (error != null) Errors[window.Handle] = error; else Errors.Remove(window.Handle);
        return error;
    }

    public void RestoreAll()
    {
        foreach (var (handle, state) in tracked)
        {
            NativeWindows.GetWindowThreadProcessId(handle, out var pid);
            if (state.Original is uint original && pid == state.ProcessId && NativeWindows.IsWindow(handle))
                NativeWindows.SetCloseState(handle, original);
        }
        tracked.Clear();
    }

    public void Dispose() { RestoreAll(); if (hook != 0) NativeWindows.UnhookWinEvent(hook); GC.KeepAlive(callback); }
}
