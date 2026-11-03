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
        ApplicationConfiguration.Initialize();
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
    }

    private static void Versions()
    {
        Check(Updates.IsNewer("v0.8.1alpha", "0.8.0alpha"), "alpha update comparison");
        Check(Updates.IsNewer("v0.8.0-beta.1", "0.8.0alpha"), "beta follows alpha");
        Check(Updates.IsNewer("0.8.0", "0.8.0beta"), "stable follows beta");
        Check(!Updates.IsNewer("0.8.0alpha", "0.8.0alpha"), "same release is not an update");
        Check(!Updates.IsNewer("0.7.9", "0.8.0alpha"), "older stable does not replace newer alpha");
        Check(!Updates.IsNewer("bad", "0.8.0alpha"), "invalid release tag ignored");
    }

    private static void Pump() { for (int i = 0; i < 12; i++) { Application.DoEvents(); Thread.Sleep(20); } }

    private static void Lifecycle()
    {
        var start = new ProcessStartInfo(Path.Combine(AppContext.BaseDirectory, "CBB.Tests.exe"), "--fixture")
        { RedirectStandardInput = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, WindowStyle = ProcessWindowStyle.Hidden };
        using var child = Process.Start(start)!;
        try
        {
            nint handle = (nint)long.Parse(child.StandardOutput.ReadLine()!);
            Pump();
            Console.WriteLine($"Fixture: hwnd={handle}, visible={NativeWindows.IsWindowVisible(handle)}, owner={NativeWindows.GetWindow(handle, 4)}, child={child.Id}");
            using var controller = new WindowController();
            Check(controller.LifecycleHookAvailable, "Windows lifecycle hook registered");
            var rules = new[] { new WindowRule("CBB UAT Fixture") };
            controller.Scan(rules);
            var window = controller.Windows.Single(w => w.Handle == handle);
            Check((NativeWindows.CloseState(handle) & 3) != 0, "existing matching window blocked on first scan");
            Check(controller.Change(window, true) == null && (NativeWindows.CloseState(handle) & 3) == 0, "manual ON restores close");
            controller.Scan(rules);
            Check((NativeWindows.CloseState(handle) & 3) == 0, "polling respects manual override");
            child.StandardInput.WriteLine("rename"); child.StandardInput.Flush(); child.StandardOutput.ReadLine(); Pump();
            controller.Scan(rules, true);
            Check((NativeWindows.CloseState(handle) & 3) == 0, "title change and rule save preserve manual override");
            child.StandardInput.WriteLine("replace"); child.StandardInput.Flush();
            handle = (nint)long.Parse(child.StandardOutput.ReadLine()!); Pump(); controller.Scan(rules);
            window = controller.Windows.Single(w => w.Handle == handle);
            Check((NativeWindows.CloseState(handle) & 3) != 0, "new matching window blocked after old instance closes");
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
                anchor.BeginInvoke(() =>
                {
                    if (command == "exit") { window.Close(); Application.ExitThread(); return; }
                    if (command == "rename") { window.Text = "Renamed fixture"; Console.WriteLine("renamed"); }
                    if (command == "replace") { window.Dispose(); window = NewWindow(); window.Show(); ShowWindow(window.Handle, 4); Console.WriteLine(window.Handle.ToInt64()); }
                    Console.Out.Flush();
                });
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
            new(onWindow.Handle, (uint)Environment.ProcessId, onWindow.Text),
            new(offWindow.Handle, (uint)Environment.ProcessId, offWindow.Text),
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
