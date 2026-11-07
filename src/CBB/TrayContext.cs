using System.Drawing.Drawing2D;

namespace CBB;

public sealed class TrayContext : ApplicationContext
{
    private readonly SettingsStore store = new(SettingsStore.DefaultPath);
    private AppSettings settings;
    private readonly WindowController controller = new();
    private readonly NotifyIcon tray;
    private readonly Icon icon;
    private readonly TrayMenu trayMenu;
    private readonly System.Windows.Forms.Timer updates = new() { Interval = 24 * 60 * 60 * 1000 };
    private readonly EventWaitHandle stop = new(false, EventResetMode.AutoReset, AppInfo.StopEventName);
    private readonly Control lifetime = new();
    private WindowMenu? menu;
    private SettingsForm? settingsDialog;
    private readonly RegisteredWaitHandle stopRegistration;
    private bool exiting;
    private bool checking;
    private string? updateUrl;

    public TrayContext()
    {
        _ = lifetime.Handle;
        settings = store.Load();
        // Respect Windows Startup Apps settings; do not overwrite the registry merely by launching.
        settings.LaunchAtStartup = AppInfo.StartupEnabled();
        icon = CreateIcon();
        trayMenu = new TrayMenu(OpenSettings, () => AppInfo.OpenUrl(AppInfo.Repository + "#readme"), () => CheckUpdates(true), () => ExitThread());
        tray = new NotifyIcon { Icon = icon, Text = $"{AppInfo.Name} {AppInfo.Version}", ContextMenuStrip = trayMenu, Visible = true };
        tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) OpenMenu(); };
        tray.BalloonTipClicked += (_, _) => { if (updateUrl != null) AppInfo.OpenUrl(updateUrl); };
        controller.WindowsChanged += (_, _) => menu?.RefreshWindows();
        controller.RuleTimedOut += (_, _) => tray.ShowBalloonTip(10000, "CBB rule paused",
            "A regular expression exceeded its 50 ms limit. It is paused until edited or CBB restarts. Review your rules in Settings.", ToolTipIcon.Warning);
        stopRegistration = ThreadPool.RegisterWaitForSingleObject(stop, (_, _) =>
        {
            try { lifetime.BeginInvoke((Action)(() => { if (!exiting) ExitThread(); })); }
            catch (InvalidOperationException) { /* The UI is already shutting down. */ }
        }, null, Timeout.Infinite, true);
        updates.Tick += async (_, _) => await CheckUpdates(false);
        updates.Start();
        controller.Scan(settings.Rules);
        lifetime.BeginInvoke((Action)(async () => await CheckUpdates(false)));
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
        menu = new WindowMenu(controller, CreateRule);
        menu.FormClosed += (_, _) => menu = null;
        menu.Show();
    }

    private void OpenSettings()
    {
        if (settingsDialog is { IsDisposed: false }) { settingsDialog.Activate(); return; }
        using var dialog = new SettingsForm(settings, () => controller.Windows);
        dialog.StartPosition = menu is { Visible: true } ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
        settingsDialog = dialog;
        try
        {
            if (dialog.ShowDialog(menu) != DialogResult.OK || dialog.Result == null) return;
            SaveSettings(dialog.Result);
        }
        finally { settingsDialog = null; }
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
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or System.Runtime.Serialization.SerializationException)
        {
            try { AppInfo.SetStartup(previousStartup); } catch { /* Report the save failure without crashing. */ }
            MessageBox.Show(menu, "Settings could not be saved.\n" + ex.Message, AppInfo.Name);
        }
        menu?.RefreshWindows();
    }

    private void CreateRule(WindowEntry window)
    {
        if (settings.Rules.Count >= SettingsStore.MaxRules)
        { MessageBox.Show(menu, $"You can save up to {SettingsStore.MaxRules} rules. Remove a rule in Settings first.", AppInfo.Name); return; }
        using var editor = new RuleEditor(new WindowRule(window.Title), () => controller.Windows);
        editor.Text = "Create automatic blocking rule";
        if (editor.ShowDialog(menu) != DialogResult.OK || editor.Rule == null) return;
        SaveSettings(new AppSettings { LaunchAtStartup = AppInfo.StartupEnabled(), Rules = settings.Rules.Concat(new[] { editor.Rule }).ToList() });
    }

    private async Task CheckUpdates(bool interactive)
    {
        if (checking) return;
        checking = true; updates.Stop(); updates.Start();
        try
        {
            var result = await Updates.CheckAsync();
            if (exiting) return;
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
        catch (Exception ex) when (ex is System.Net.Http.HttpRequestException or TaskCanceledException or System.Runtime.Serialization.SerializationException or System.Xml.XmlException or InvalidOperationException or IOException)
        { if (interactive && !exiting) MessageBox.Show(menu, "Could not check for updates. Try again later.\n" + ex.Message, "CBB updates"); }
        finally { checking = false; }
    }

    protected override void ExitThreadCore()
    {
        exiting = true; updates.Stop(); stopRegistration.Unregister(null);
        controller.RestoreAll(); tray.Visible = false; settingsDialog?.Close(); menu?.Close(); base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Microsoft.Win32.SystemEvents.SessionEnding -= OnSessionEnding;
            exiting = true; stopRegistration.Unregister(null);
            updates.Dispose(); controller.Dispose(); tray.Dispose(); trayMenu.Dispose(); icon.Dispose(); stop.Dispose(); lifetime.Dispose();
        }
        base.Dispose(disposing);
    }
}
