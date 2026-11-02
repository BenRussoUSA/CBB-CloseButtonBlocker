using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace CBB;

public sealed class TrayContext : ApplicationContext
{
    private readonly SettingsStore store = new(SettingsStore.DefaultPath);
    private AppSettings settings;
    private readonly WindowController controller = new();
    private readonly NotifyIcon tray;
    private readonly Icon icon;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 700 };
    private readonly EventWaitHandle stop = new(false, EventResetMode.AutoReset, AppInfo.StopEventName);
    private readonly Control lifetime = new();
    private WindowMenu? menu;
    private DateTime nextCheck = DateTime.UtcNow;
    private bool checking;
    private string? updateUrl;

    public TrayContext()
    {
        _ = lifetime.Handle;
        settings = store.Load();
        // Respect Windows Startup Apps settings; do not overwrite the registry merely by launching.
        settings.LaunchAtStartup = AppInfo.StartupEnabled();
        icon = CreateIcon();
        tray = new NotifyIcon { Icon = icon, Text = "CBB-CloseButtonBlocker 0.8.0alpha", Visible = true };
        tray.MouseClick += (_, e) => { if (e.Button is MouseButtons.Left or MouseButtons.Right) OpenMenu(); };
        tray.BalloonTipClicked += (_, _) => { if (updateUrl != null) AppInfo.OpenUrl(updateUrl); };
        timer.Tick += async (_, _) =>
        {
            if (stop.WaitOne(0)) { ExitThread(); return; }
            controller.Scan(settings.Rules);
            menu?.RefreshWindows();
            if (DateTime.UtcNow >= nextCheck && !checking) await CheckUpdates(false);
        };
        timer.Start();
        Microsoft.Win32.SystemEvents.SessionEnding += OnSessionEnding;
        if (store.LoadWarning != null) MessageBox.Show(store.LoadWarning, "CBB settings recovery");
        if (!controller.LifecycleHookAvailable) MessageBox.Show("Windows lifecycle monitoring could not start. Close CBB and retry before using automatic rules.", AppInfo.Name);
    }

    private void OnSessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
    {
        if (!lifetime.IsDisposed) lifetime.Invoke((Action)controller.RestoreAll);
    }

    public static Icon CreateIcon()
    {
        using var bitmap = new Bitmap(64, 64);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Black);
            using var x = new Pen(Color.FromArgb(235, 235, 235), 3);
            g.DrawLine(x, 17, 17, 47, 47); g.DrawLine(x, 47, 17, 17, 47);
            using var no = new Pen(Color.FromArgb(242, 45, 55), 6);
            g.DrawEllipse(no, 7, 7, 50, 50); g.DrawLine(no, 14, 14, 50, 50);
        }
        nint h = bitmap.GetHicon();
        try { using var borrowed = Icon.FromHandle(h); return (Icon)borrowed.Clone(); }
        finally { NativeWindows.DestroyIcon(h); }
    }

    private void OpenMenu()
    {
        if (menu is { IsDisposed: false }) { menu.Activate(); return; }
        controller.Scan(settings.Rules);
        menu = new WindowMenu(controller, () => OpenSettings(), () => ToggleStartup(), () => CheckUpdates(true), () => Uninstall(), () => ExitThread());
        menu.FormClosed += (_, _) => menu = null;
        menu.Show();
    }

    private void OpenSettings()
    {
        using var dialog = new SettingsForm(settings);
        if (dialog.ShowDialog(menu) != DialogResult.OK || dialog.Result == null) return;
        SaveSettings(dialog.Result);
    }

    private void SaveSettings(AppSettings proposed)
    {
        bool previousStartup = AppInfo.StartupEnabled();
        try
        {
            AppInfo.SetStartup(proposed.LaunchAtStartup);
            store.Save(proposed);
            settings = proposed;
            controller.Scan(settings.Rules, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            try { AppInfo.SetStartup(previousStartup); } catch { /* Report the save failure without crashing. */ }
            MessageBox.Show(menu, "Settings could not be saved.\n" + ex.Message, AppInfo.Name);
        }
        menu?.RefreshWindows();
    }

    private void ToggleStartup() => SaveSettings(new() { LaunchAtStartup = !AppInfo.StartupEnabled(), Rules = settings.Rules.ToList() });

    private async Task CheckUpdates(bool interactive)
    {
        if (checking) return;
        checking = true; nextCheck = DateTime.UtcNow.AddDays(1);
        try
        {
            var result = await Updates.CheckAsync();
            if (result.Url != null)
            {
                updateUrl = result.Url;
                tray.ShowBalloonTip(10000, "CBB update available", result.Message, ToolTipIcon.Info);
                if (interactive && MessageBox.Show(menu, result.Message + "\n\nOpen the release page?", "CBB updates", MessageBoxButtons.YesNo) == DialogResult.Yes) AppInfo.OpenUrl(result.Url);
            }
            else if (interactive)
            {
                if (MessageBox.Show(menu, result.Message + "\n\nOpen GitHub releases?", "CBB updates", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    AppInfo.OpenUrl(AppInfo.Repository + "/releases");
            }
        }
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException)
        { if (interactive) MessageBox.Show(menu, "Could not check for updates. Try again later.\n" + ex.Message, "CBB updates"); }
        finally { checking = false; }
    }

    private void Uninstall()
    {
        using var dialog = new Form { Text = "Uninstall CBB-CloseButtonBlocker", ClientSize = new Size(500, 230), StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false, FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false };
        var layout = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20), FlowDirection = FlowDirection.TopDown, WrapContents = false };
        var feedback = new Button { Text = "Please leave feedback via GitHub", Width = 440, Height = 36 };
        var info = new Label { Text = "Feedback is optional and opens in your browser.\nGitHub sign-in and access to this private alpha are required.", AutoSize = true, Margin = new Padding(0, 10, 0, 10) };
        var uninstall = new Button { Text = "SKIP feedback, Uninstall", Width = 440, Height = 36 };
        var cancel = new Button { Text = "Cancel", Width = 100, DialogResult = DialogResult.Cancel };
        feedback.Click += (_, _) =>
        {
            AppInfo.OpenUrl(AppInfo.Repository + "/issues/new?title=" + Uri.EscapeDataString("Uninstall feedback (0.8.0alpha)") + "&body=" + Uri.EscapeDataString("Why I am uninstalling CBB:\n\n"));
            uninstall.Text = "Uninstall now";
        };
        uninstall.Click += (_, _) => dialog.DialogResult = DialogResult.OK;
        layout.Controls.AddRange([feedback, info, uninstall, cancel]); dialog.Controls.Add(layout); dialog.CancelButton = cancel;
        if (dialog.ShowDialog(menu) != DialogResult.OK) return;
        string? product = AppInfo.InstalledProduct();
        if (product == null) { MessageBox.Show(menu, "This copy is not registered as an MSI installation. Exit CBB and remove its extracted folder to remove a portable copy.", AppInfo.Name); return; }
        try
        {
            Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "msiexec.exe"), "/x " + product) { UseShellExecute = true });
            ExitThread();
        }
        catch (Exception ex) { MessageBox.Show(menu, ex.Message, "Could not start uninstall"); }
    }

    protected override void ExitThreadCore()
    {
        timer.Stop(); controller.RestoreAll(); tray.Visible = false; menu?.Close(); base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Microsoft.Win32.SystemEvents.SessionEnding -= OnSessionEnding;
            timer.Dispose(); controller.Dispose(); tray.Dispose(); icon.Dispose(); stop.Dispose(); lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
