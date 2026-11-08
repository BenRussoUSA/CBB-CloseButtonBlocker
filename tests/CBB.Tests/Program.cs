using CBB;
using System.Diagnostics;
using System.Drawing.Imaging;

internal static class Program
{
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ShowWindow(nint hwnd, int command);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern nint SendMessage(nint hwnd, uint message, nint wparam, nint lparam);
    private static int checks;
    private static void Check(bool pass, string message)
    {
        if (!pass) throw new Exception("FAIL: " + message);
        checks++; Console.WriteLine("PASS: " + message);
    }

    [STAThread]
    private static int Main(string[] args)
    {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        if (args.Contains("--fixture")) { Fixture(); return 0; }
        var output = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.Combine(Path.GetTempPath(), "CBB-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(output);
        try
        {
            Rules(); Persistence(output); Versions(); Lifecycle(); Snapshots(output);
            Console.WriteLine($"SUCCESS: {checks} checks passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static void Rules()
    {
        Check(new WindowRule("Notepad").Matches("Notepad"), "exact match");
        Check(!new WindowRule("Notepad").Matches("notes - Notepad"), "exact match does not match substring");
        Check(!new WindowRule("Notepad").Matches("notepad"), "exact case sensitivity");
        Check(new WindowRule(".* - Notepad$", true).Matches("Report - Notepad"), "regex suffix");
        Check(new WindowRule("(?i)notepad", true).Matches("NOTEPAD"), "regex case option");
        Check(new WindowRule("[", true).Validate() != null, "invalid regex rejected");
        Check(new WindowRule(" ").Validate() != null, "empty rule rejected");
        Check(!new WindowRule("(a+)+$", true).Matches(new string('a', 10000) + "!"), "pathological regex times out safely");
        var expensive = new WindowRule("(a+)+$", true);
        expensive.Matches(new string('a', 10000) + "!");
        var elapsed = Stopwatch.StartNew();
        for (int i = 0; i < 100; i++) expensive.Matches(new string('a', 10000) + "!");
        Check(expensive.TimedOut && elapsed.ElapsedMilliseconds < 50, "timed-out regex is quarantined instead of repeatedly consuming CPU");
    }

    private static void Persistence(string output)
    {
        var file = Path.Combine(output, "settings-test.json");
        var store = new SettingsStore(file);
        store.Save(new() { LaunchAtStartup = true, Rules = [new("Unicode Ω 窗口"), new("(?i)notes$", true)] });
        var loaded = new SettingsStore(file).Load();
        Check(loaded.LaunchAtStartup && loaded.Rules.Count == 2 && loaded.Rules[0].Pattern == "Unicode Ω 窗口", "settings survive reload with Unicode");
        store.Save(new() { Rules = loaded.Rules });
        Check(File.Exists(file + ".bak"), "atomic save retains previous settings backup");
        Check(!new SettingsStore(file).Load().LaunchAtStartup, "updated startup preference persists");
        File.WriteAllText(file, "{broken");
        var corrupt = new SettingsStore(file); corrupt.Load();
        Check(corrupt.LoadWarning != null && File.ReadAllText(file) == "{broken", "corrupt settings retained with warning");
        File.WriteAllText(file, "{\"SchemaVersion\":999}");
        var future = new SettingsStore(file); future.Load();
        Check(future.LoadWarning != null, "future schema is not silently loaded");
        File.WriteAllText(file, "{\"SchemaVersion\":1,\"LaunchAtStartup\":true,\"Rules\":[{\"Pattern\":\"(?i)notes$\",\"IsRegex\":true}]}");
        var legacy = new SettingsStore(file);
        var migrated = legacy.Load();
        Check(legacy.LoadWarning == null && migrated.LaunchAtStartup && migrated.Rules.Single().Matches("NOTES"), "0.9.0 JSON loads with the Framework serializer");
        File.WriteAllText(file, new string(' ', SettingsStore.MaxBytes + 1));
        var oversized = new SettingsStore(file); oversized.Load();
        Check(oversized.LoadWarning != null && new FileInfo(file).Length > SettingsStore.MaxBytes, "oversized settings rejected without altering the file");
        File.WriteAllText(file, "{\"SchemaVersion\":1,\"Rules\":[" + string.Join(",", Enumerable.Repeat("{\"Pattern\":\"example\"}", SettingsStore.MaxRules + 1)) + "]}");
        var tooMany = new SettingsStore(file); tooMany.Load();
        Check(tooMany.LoadWarning != null, "excessive rule count rejected on load");
        Check(new WindowRule(new string('x', 2049)).Validate() != null, "excessive pattern length rejected");
    }

    private static void Versions()
    {
        Check(Updates.IsNewer("v0.8.1alpha", "0.8.0alpha"), "alpha update comparison");
        Check(Updates.IsNewer("v0.8.0-beta.1", "0.8.0alpha"), "beta follows alpha");
        Check(Updates.IsNewer("0.8.0", "0.8.0beta"), "stable follows beta");
        Check(!Updates.IsNewer("0.8.0alpha", "0.8.0alpha"), "same release is not an update");
        Check(!Updates.IsNewer("0.7.9", "0.8.0alpha"), "older stable does not replace newer alpha");
        Check(!Updates.IsNewer("bad", "0.8.0alpha"), "invalid release tag ignored");
        Check(Updates.IsNewer("v0.9.0beta", "0.8.3"), "public beta is newer than the private test release");
        Check(!Updates.IsNewer("v0.9.0beta", "0.9.0beta"), "current public beta does not update to itself");
        Check(Updates.IsNewer("v0.9.4beta", "0.9.0beta"), "0.9.4beta upgrades 0.9.0beta");
        Check(!Updates.IsNewer(new string('9', 10000), AppInfo.Version), "oversized release tag ignored");
        (string Message, string? Url) Response(string json)
        {
            using var data = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
            return Updates.ReadResponse(data);
        }
        Check(Response("[{\"tag_name\":\"v1.0.0\",\"html_url\":\"https://example.com/update\",\"draft\":false}]").Url == null, "update links outside the project are rejected");
        Check(Response("[{\"tag_name\":\"v1.0.0\",\"html_url\":\"" + AppInfo.Repository + "/releases/tag/v1.0.0\",\"draft\":false}]").Url != null, "public project release link accepted");
        Check(Response("[{\"tag_name\":\"v1.0.0\",\"html_url\":\"" + AppInfo.Repository + "/releases/tag/v1.0.0\",\"draft\":true}]").Url == null, "draft releases are ignored");
    }

    private static void Pump() { for (int i = 0; i < 12; i++) { Application.DoEvents(); Thread.Sleep(20); } }

    private static string ReadFixtureReply(Process child)
    {
        // Keep this STA responsive while the other process changes a window.
        // Accessibility/event delivery can require messages to be processed here.
        var reply = child.StandardOutput.ReadLineAsync();
        var deadline = Stopwatch.StartNew();
        while (!reply.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(10))
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
        if (!reply.IsCompleted) throw new TimeoutException("The fixture did not reply within 10 seconds.");
        return reply.GetAwaiter().GetResult() ?? throw new IOException("The fixture closed its output unexpectedly.");
    }

    private static void Lifecycle()
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "CBB.Tests.exe"), "--fixture")
        { RedirectStandardInput = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        using var child = Process.Start(start)!;
        try
        {
            nint handle = (nint)long.Parse(ReadFixtureReply(child));
            Pump();
            Console.WriteLine($"Fixture: hwnd={handle}, visible={NativeWindows.IsWindowVisible(handle)}, owner={NativeWindows.GetWindow(handle, 4)}, child={child.Id}");
            using var controller = new WindowController();
            Check(controller.LifecycleHookAvailable, "Windows lifecycle hook registered");
            var rules = new[] { new WindowRule("CBB UAT Fixture") };
            controller.Scan(rules);
            var window = controller.Windows.Single(w => w.Handle == handle);
            Check((NativeWindows.CloseState(handle) & 3) != 0, "existing matching window blocked on first scan");
            Check(controller.Change(window, true) == null && (NativeWindows.CloseState(handle) & 3) == 0, "manual ON restores close");
            int scans = controller.FullScanCount;
            Pump(); Pump(); Pump(); Pump();
            Check(controller.FullScanCount == scans, "idle message pumping does not trigger recurring full scans");
            Check((NativeWindows.CloseState(handle) & 3) == 0, "event handling respects manual override");
            child.StandardInput.WriteLine("rename"); child.StandardInput.Flush(); ReadFixtureReply(child); Pump();
            Check(controller.Windows.Single(w => w.Handle == handle).Title == "Renamed fixture" && controller.FullScanCount == scans, "title notification updates the list without a scan");
            controller.Scan(rules, true);
            Check((NativeWindows.CloseState(handle) & 3) == 0, "title change and rule save preserve manual override");
            child.StandardInput.WriteLine("hide"); child.StandardInput.Flush(); ReadFixtureReply(child); Pump();
            Check(controller.Windows.All(w => w.Handle != handle), "hide notification removes a window from the visible list");
            child.StandardInput.WriteLine("show"); child.StandardInput.Flush(); ReadFixtureReply(child); Pump();
            Check(controller.Windows.Any(w => w.Handle == handle) && (NativeWindows.CloseState(handle) & 3) == 0, "show notification retains the window's manual override");
            child.StandardInput.WriteLine("replace"); child.StandardInput.Flush();
            scans = controller.FullScanCount;
            handle = (nint)long.Parse(ReadFixtureReply(child)); Pump();
            window = controller.Windows.Single(w => w.Handle == handle);
            Check((NativeWindows.CloseState(handle) & 3) != 0 && controller.FullScanCount == scans, "create/destroy notifications block a replacement window without scanning");
            Check(controller.Change(window, true) == null, "replacement window can be unblocked");
            Check(controller.Change(window, false) == null, "manual OFF disables close");
            controller.RestoreAll();
            Check((NativeWindows.CloseState(handle) & 3) == 0, "CBB exit restores modified close command");
            controller.Scan(rules);
            Check((NativeWindows.CloseState(handle) & 3) != 0, "fresh session reapplies saved rule");
            controller.RestoreAll();
            controller.Scan([]);
            using (var menu = new WindowMenu(controller))
            {
                menu.Show(); Pump();
                var grid = menu.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<DataGridView>().Single();
                var row = grid.Rows.Cast<DataGridViewRow>().Single(r => ((WindowEntry)r.Tag!).Handle == handle);
                grid.CurrentCell = row.Cells[0]; grid.Focus();
                row.Cells[0].AccessibilityObject.DoDefaultAction(); Pump();
                Check((NativeWindows.CloseState(handle) & 3) != 0 && row.Cells[0].Value is true,
                    "grid toggle blocks native Close and shows On");
                SendMessage(grid.Handle, 0x100, (nint)Keys.Space, 0);
                SendMessage(grid.Handle, 0x101, (nint)Keys.Space, 0); Pump();
                Check((NativeWindows.CloseState(handle) & 3) == 0 && row.Cells[0].Value is false,
                    "Space on grid toggle restores native Close and shows Off");
                var bounds = grid.GetCellDisplayRectangle(0, row.Index, false);
                nint point = (nint)((bounds.Left + bounds.Width / 2) | ((bounds.Top + bounds.Height / 2) << 16));
                void ClickToggle()
                {
                    SendMessage(grid.Handle, 0x201, 1, point);
                    SendMessage(grid.Handle, 0x202, 0, point); Pump();
                }
                ClickToggle();
                Check((NativeWindows.CloseState(handle) & 3) != 0 && row.Cells[0].Value is true,
                    "single mouse click on toggle blocks native Close");
                ClickToggle();
                Check((NativeWindows.CloseState(handle) & 3) == 0 && row.Cells[0].Value is false,
                    "single mouse click on toggle restores native Close");
                controller.Scan(rules); menu.RefreshWindows();
                Check((NativeWindows.CloseState(handle) & 3) == 0,
                    "grid manual override survives automatic rule scan");
                var filters = menu.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<FlowLayoutPanel>().Single();
                var search = filters.Controls.OfType<TextBox>().Single();
                search.Text = "CBB UAT Fixture"; Pump();
                Check(grid.Rows.Count == 1 && ReferenceEquals(grid.Rows[0], row), "search retains the existing matching grid row");
                var blockedOnly = filters.Controls.OfType<CheckBox>().Single();
                blockedOnly.Checked = true; Pump();
                Check(grid.Rows.Count == 0, "blocked-only filter excludes an unblocked window");
                controller.Change(window, false); menu.RefreshWindows(); Pump();
                Check(grid.Rows.Count == 1, "blocked-only filter includes a newly blocked window");
                grid.CurrentCell = grid.Rows[0].Cells[0];
                grid.Rows[0].Cells[0].AccessibilityObject.DoDefaultAction(); Pump();
                Check(grid.Rows.Count == 0 && (NativeWindows.CloseState(handle) & 3) == 0, "turning protection off safely removes a row from the blocked-only view");
                using (var editor = new RuleEditor(new WindowRule(window.Title), () => new[] { window }))
                {
                    editor.Show(menu); Pump();
                    var matches = editor.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<ListBox>().Single();
                    Check(matches.Items.Count == 1 && Equals(matches.Items[0], window.Title), "rule editor previews the selected window title before saving");
                    editor.Close();
                }
                menu.Close();
            }
            controller.RestoreAll();
        }
        finally
        {
            if (!child.HasExited) { child.StandardInput.WriteLine("exit"); child.StandardInput.Flush(); if (!child.WaitForExit(3000)) child.Kill(); }
        }
    }

    private static void Fixture()
    {
        Form NewWindow() => new() { Text = "CBB UAT Fixture", ShowInTaskbar = true, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), Size = new Size(300, 200) };
        var window = NewWindow();
        var anchor = new Control(); anchor.CreateControl();
        window.Show(); ShowWindow(window.Handle, 4); Console.WriteLine(window.Handle.ToInt64()); Console.Out.Flush();
        _ = Task.Run(() =>
        {
            while (Console.ReadLine() is string command)
            {
                anchor.BeginInvoke((Action)(() =>
                {
                    if (command == "exit") { window.Close(); Application.ExitThread(); return; }
                    if (command == "rename") { window.Text = "Renamed fixture"; Console.WriteLine("renamed"); }
                    if (command == "hide") { window.Hide(); Console.WriteLine("hidden"); }
                    if (command == "show") { window.Show(); ShowWindow(window.Handle, 4); Console.WriteLine("shown"); }
                    if (command == "replace") { window.Dispose(); window = NewWindow(); window.Show(); ShowWindow(window.Handle, 4); Console.WriteLine(window.Handle.ToInt64()); }
                    Console.Out.Flush();
                }));
            }
        });
        Application.Run(); anchor.Dispose(); window.Dispose();
    }

    private static void Snapshots(string output)
    {
        void Save(Form form, string name)
        {
            using (form)
            {
                form.Show(); Application.DoEvents();
                using var bitmap = new Bitmap(form.Width, form.Height); form.DrawToBitmap(bitmap, form.ClientRectangle with { Width = form.Width, Height = form.Height });
                bitmap.Save(Path.Combine(output, name + ".png"), ImageFormat.Png); form.Close();
            }
        }
        Save(new SettingsForm(new() { Rules = [new(".* - Notepad$", true), new("Important Application")] }), "settings");
        Save(new RuleEditor(new(".* - Notepad$", true)), "rule-editor");
        using var onWindow = new Form { Text = "Example document - Close Button ON" };
        using var offWindow = new Form { Text = "Protected document - Close Button OFF" };
        _ = onWindow.Handle; _ = offWindow.Handle;
        NativeWindows.SetCloseState(offWindow.Handle, 3);
        using var previewController = new WindowController();
        previewController.Windows.AddRange([
            new(onWindow.Handle, (uint)Process.GetCurrentProcess().Id, onWindow.Text),
            new(offWindow.Handle, (uint)Process.GetCurrentProcess().Id, offWindow.Text),
            new(0, 0, "Unsupported window - no standard Close command")
        ]);
        using (var preview = new WindowMenu(previewController))
        {
            var grid = preview.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<DataGridView>().Single();
            Check(grid.Rows.Cast<DataGridViewRow>().Single(r => ((WindowEntry)r.Tag!).Handle == 0).Cells[0].ReadOnly,
                "unsupported window toggle is read-only");
            Save(preview, "window-list");
        }
        using (var trayMenu = new TrayMenu(() => { }, () => { }, () => Task.CompletedTask, () => { }))
        {
            trayMenu.Show(new Point(100, 100)); Application.DoEvents();
            using var bitmap = new Bitmap(trayMenu.Width, trayMenu.Height);
            trayMenu.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, "tray-menu.png"), ImageFormat.Png); trayMenu.Close();
        }
        using var icon = TrayContext.CreateIcon(); using var iconBitmap = icon.ToBitmap(); iconBitmap.Save(Path.Combine(output, "tray-icon.png"));
        using var file = File.Create(Path.Combine(output, "app.ico")); icon.Save(file);
        Check(true, "settings UI and tray icon rendered for inspection");
    }
}
