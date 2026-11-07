using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

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
    [DllImport("user32.dll")] public static extern nint GetAncestor(nint hwnd, uint flags);
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

    private static readonly int OwnProcessId = Process.GetCurrentProcess().Id;

    public static WindowEntry? Read(nint hwnd)
    {
        if (!IsWindow(hwnd) || !IsWindowVisible(hwnd) || GetAncestor(hwnd, 2) != hwnd || GetWindow(hwnd, 4) != 0) return null;
        GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == OwnProcessId) return null;
        if (DwmGetWindowAttribute(hwnd, 14, out int cloaked, sizeof(int)) == 0 && cloaked != 0) return null;
        var title = new StringBuilder(1024);
        GetWindowText(hwnd, title, title.Capacity);
        return title.Length == 0 ? null : new WindowEntry(hwnd, pid, title.ToString());
    }

    public static List<WindowEntry> List()
    {
        var result = new List<WindowEntry>();
        EnumWindows((hwnd, _) =>
        {
            var entry = Read(hwnd);
            if (entry != null) result.Add(entry);
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
        public bool RuleEvaluated;
    }
    private readonly Dictionary<nint, State> tracked = [];
    private readonly NativeWindows.WinEventProc callback;
    private readonly List<nint> hooks = [];
    private readonly HashSet<nint> dirty = [];
    private readonly HashSet<nint> destroyed = [];
    private readonly System.Windows.Forms.Timer pending = new() { Interval = 50 };
    private IReadOnlyList<WindowRule> rules = [];
    private sealed class RuleWork(WindowEntry window, State state)
    {
        public WindowEntry Window = window;
        public State State = state;
        public int Index;
    }
    private readonly Queue<RuleWork> ruleWork = new();
    private bool rescanRequired;
    private bool disposed;
    public List<WindowEntry> Windows { get; private set; } = [];
    public Dictionary<nint, string> Errors { get; } = [];
    public bool LifecycleHookAvailable => hooks.Count == 5 && hooks.All(h => h != 0);
    public int FullScanCount { get; private set; }
    public event EventHandler? WindowsChanged;
    public event EventHandler? RuleTimedOut;

    public WindowController()
    {
        // Out-of-context hooks run on the message-pumping thread that registers them.
        // The callback only queues work; it never modifies another application's window.
        callback = (_, evt, hwnd, obj, child, _, _) =>
        {
            if (disposed || hwnd == 0 || obj != 0 || child != 0) return;
            if (evt != 3 && evt != 0x8000 && evt != 0x8001 && evt != 0x8002 && evt != 0x8003 &&
                evt != 0x800A && evt != 0x800C && evt != 0x8017 && evt != 0x8018) return;
            if (evt == 0x8001)
            {
                if (!tracked.ContainsKey(hwnd) && !dirty.Contains(hwnd)) return;
                // Forget the lifetime immediately, before Windows can reuse this handle.
                tracked.Remove(hwnd); Errors.Remove(hwnd); destroyed.Add(hwnd);
            }
            else if (NativeWindows.GetAncestor(hwnd, 2) != hwnd) return;
            if (dirty.Count < 4096) dirty.Add(hwnd); else rescanRequired = true;
            if (!pending.Enabled) pending.Start();
        };
        const uint skipOwnProcess = 2;
        // Separate ranges avoid receiving unrelated focus, location, selection and reorder events.
        hooks.Add(NativeWindows.SetWinEventHook(0x8000, 0x8003, 0, callback, 0, 0, skipOwnProcess));
        hooks.Add(NativeWindows.SetWinEventHook(0x800A, 0x800A, 0, callback, 0, 0, skipOwnProcess));
        hooks.Add(NativeWindows.SetWinEventHook(0x800C, 0x800C, 0, callback, 0, 0, skipOwnProcess));
        hooks.Add(NativeWindows.SetWinEventHook(3, 3, 0, callback, 0, 0, skipOwnProcess));
        hooks.Add(NativeWindows.SetWinEventHook(0x8017, 0x8018, 0, callback, 0, 0, skipOwnProcess));
        pending.Tick += (_, _) => ProcessPending();
    }

    public void Scan(IReadOnlyList<WindowRule> rules, bool reconsider = false)
    {
        this.rules = rules;
        pending.Stop(); dirty.Clear(); destroyed.Clear(); ruleWork.Clear(); rescanRequired = false;
        FullScanCount++;
        foreach (var handle in tracked.Keys.Where(h => !NativeWindows.IsWindow(h)).ToArray())
        { tracked.Remove(handle); Errors.Remove(handle); }
        Windows = NativeWindows.List();
        foreach (var window in Windows)
            ConsiderRules(window, reconsider);
        ProcessRules();
        WindowsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ConsiderRules(WindowEntry window, bool reconsider)
    {
        bool fresh = !tracked.TryGetValue(window.Handle, out var state) || state.ProcessId != window.ProcessId;
        if (fresh) tracked[window.Handle] = state = new(window.ProcessId);
        if ((fresh || reconsider || !state!.RuleEvaluated) && !state!.Manual && !state.RuleApplied && rules.Count != 0 &&
            !ruleWork.Any(j => ReferenceEquals(j.State, state)))
            ruleWork.Enqueue(new(window, state));
    }

    private void ProcessPending()
    {
        pending.Stop();
        if (disposed) return;
        if (rescanRequired) { Scan(rules); return; }
        foreach (var handle in destroyed) Windows.RemoveAll(w => w.Handle == handle);
        destroyed.Clear();
        var changes = dirty.ToArray(); dirty.Clear();
        foreach (var handle in changes)
        {
            var window = NativeWindows.Read(handle);
            int index = Windows.FindIndex(w => w.Handle == handle);
            if (window == null)
            {
                if (index >= 0) Windows.RemoveAt(index);
                continue;
            }
            if (index >= 0) Windows[index] = window; else Windows.Add(window);
            ConsiderRules(window, false);
        }
        ProcessRules();
        if (changes.Length != 0 || ruleWork.Count != 0) WindowsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ProcessRules()
    {
        // Yield between matches; at worst one 50 ms regex can extend this 16 ms batch.
        var budget = Stopwatch.StartNew();
        bool changed = false;
        while (ruleWork.Count != 0 && budget.ElapsedMilliseconds < 16)
        {
            var job = ruleWork.Peek();
            if (!tracked.TryGetValue(job.Window.Handle, out var state) || !ReferenceEquals(state, job.State) ||
                state.Manual || state.RuleApplied)
            { ruleWork.Dequeue(); continue; }
            if (job.Index >= rules.Count) { state.RuleEvaluated = true; ruleWork.Dequeue(); continue; }
            var rule = rules[job.Index++];
            bool wasTimedOut = rule.TimedOut;
            if (rule.Matches(job.Window.Title))
            {
                state.RuleApplied = true;
                Change(job.Window, false, false);
                changed = true;
                ruleWork.Dequeue();
            }
            if (!wasTimedOut && rule.TimedOut) RuleTimedOut?.Invoke(this, EventArgs.Empty);
        }
        if (ruleWork.Count != 0 && !pending.Enabled) pending.Start();
        if (changed) WindowsChanged?.Invoke(this, EventArgs.Empty);
    }

    public string? Change(WindowEntry window, bool closeOn, bool manual = true)
    {
        if (!NativeWindows.IsWindow(window.Handle)) return "This window has closed.";
        NativeWindows.GetWindowThreadProcessId(window.Handle, out var pid);
        if (pid != window.ProcessId) return "This window has been replaced. Refresh the list.";
        if (!tracked.TryGetValue(window.Handle, out var state) || state.ProcessId != pid) tracked[window.Handle] = state = new(pid);
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
        foreach (var pair in tracked)
        {
            var handle = pair.Key; var state = pair.Value;
            NativeWindows.GetWindowThreadProcessId(handle, out var pid);
            if (state.Original is uint original && pid == state.ProcessId && NativeWindows.IsWindow(handle))
                NativeWindows.SetCloseState(handle, original);
        }
        tracked.Clear(); ruleWork.Clear();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true; pending.Dispose();
        foreach (var hook in hooks) if (hook != 0) NativeWindows.UnhookWinEvent(hook);
        RestoreAll(); GC.KeepAlive(callback);
    }
}
