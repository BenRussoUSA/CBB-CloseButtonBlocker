namespace CBB;

public sealed class WindowMenu : Form
{
    private readonly WindowController controller;
    private readonly ListBox windows = new() { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true, DisplayMember = nameof(WindowEntry.Title) };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(500, 0), Padding = new Padding(0, 6, 0, 6) };
    private readonly CheckBox startup = new() { Text = "Launch at startup", AutoSize = true, AutoCheck = false };
    private readonly ContextMenuStrip choices = new();

    public WindowMenu(WindowController controller, Action settings, Action toggleStartup, Func<Task> updates, Action uninstall, Action exit)
    {
        this.controller = controller;
        Text = "CBB-CloseButtonBlocker · 0.8.0alpha"; ShowInTaskbar = false;
        ClientSize = new Size(520, 530); MinimumSize = new Size(470, 430); MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(Math.Max(area.Left, area.Right - Width - 12), Math.Max(area.Top, area.Bottom - Height - 12));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "Open windows · right-click a title to change its Close button", AutoSize = true, Padding = new Padding(0, 0, 0, 10) });
        layout.Controls.Add(windows); layout.Controls.Add(status);
        layout.Controls.Add(startup); startup.Click += (_, _) => toggleStartup();
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true };
        void Add(string title, Action handler) { var button = new Button { Text = title, AutoSize = true }; button.Click += (_, _) => handler(); actions.Controls.Add(button); }
        Add("Settings", settings);
        var update = new Button { Text = "Check for updates", AutoSize = true }; update.Click += async (_, _) => await updates(); actions.Controls.Add(update);
        Add("About Close Button Blocker", () => AppInfo.OpenUrl(AppInfo.Repository + "#readme"));
        Add("Exit CBB", exit); Add("Uninstall", uninstall);
        layout.Controls.Add(actions); Controls.Add(layout);
        windows.SelectedIndexChanged += (_, _) => ShowStatus();
        windows.MouseUp += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            int index = windows.IndexFromPoint(e.Location);
            if (index < 0) return;
            windows.SelectedIndex = index; ShowChoices(e.Location);
        };
        windows.KeyDown += (_, e) => { if (e.KeyCode == Keys.Apps || (e.Shift && e.KeyCode == Keys.F10)) { ShowChoices(new Point(30, 30)); e.Handled = true; } };
        KeyPreview = true; KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        RefreshWindows();
    }

    public void RefreshWindows()
    {
        var entries = controller.Windows.OrderBy(w => w.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        if (!entries.SequenceEqual(windows.Items.Cast<WindowEntry>()))
        {
            var selected = (windows.SelectedItem as WindowEntry)?.Handle;
            windows.BeginUpdate(); windows.Items.Clear(); windows.Items.AddRange(entries);
            windows.SelectedIndex = Array.FindIndex(entries, w => w.Handle == selected); windows.EndUpdate();
        }
        startup.Checked = AppInfo.StartupEnabled(); ShowStatus();
    }

    private void ShowStatus()
    {
        if (windows.SelectedItem is not WindowEntry window) { status.Text = windows.Items.Count == 0 ? "No eligible windows are currently open." : "Select a window. Manual choices last for that window's lifetime."; return; }
        if (controller.Errors.TryGetValue(window.Handle, out var error)) { status.Text = error; return; }
        uint state = NativeWindows.CloseState(window.Handle);
        status.Text = state == NativeWindows.MissingMenuItem ? "No standard Close command. This window is unsupported." :
            (state & 3) == 0 ? "Close Button ON · normal operation" : "Close Button OFF · Windows Close command disabled";
    }

    private void ShowChoices(Point point)
    {
        if (windows.SelectedItem is not WindowEntry window) return;
        foreach (ToolStripItem item in choices.Items) item.Font?.Dispose();
        choices.Items.Clear();
        uint state = NativeWindows.CloseState(window.Handle);
        foreach (bool on in new[] { true, false })
        {
            bool active = state != NativeWindows.MissingMenuItem && ((state & 3) == 0) == on;
            var item = new ToolStripMenuItem((active ? "◆ " : "    ") + "Close Button " + (on ? "ON" : "OFF"))
            { Font = new Font(SystemFonts.MenuFont!, active ? FontStyle.Bold : FontStyle.Regular), Enabled = state != NativeWindows.MissingMenuItem };
            item.Click += (_, _) =>
            {
                if (controller.Change(window, on) is string error) MessageBox.Show(this, error, "CBB could not change this window");
                ShowStatus();
            };
            choices.Items.Add(item);
        }
        choices.Show(windows, point);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { foreach (ToolStripItem item in choices.Items) item.Font?.Dispose(); choices.Dispose(); }
        base.Dispose(disposing);
    }
}
