using System.Drawing.Drawing2D;

namespace CBB;

public sealed class WindowMenu : Form
{
    private readonly WindowController controller;
    private readonly ListBox windows = new() { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true, DisplayMember = nameof(WindowEntry.Title), DrawMode = DrawMode.OwnerDrawFixed };
    private readonly Label status = new() { AutoSize = true, MaximumSize = new Size(500, 0), Padding = new Padding(0, 6, 0, 6) };
    private readonly ContextMenuStrip choices = new();
    private readonly Icon blockedIcon = TrayContext.CreateIcon();
    private readonly Dictionary<nint, uint> closeStates = [];

    public WindowMenu(WindowController controller, Action settings, Action uninstall)
    {
        this.controller = controller;
        Text = $"{AppInfo.Name} · {AppInfo.Version}"; ShowInTaskbar = false;
        ClientSize = new Size(520, 530); MinimumSize = new Size(470, 430); MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(Math.Max(area.Left, area.Right - Width - 12), Math.Max(area.Top, area.Bottom - Height - 12));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "List of windows   (right click one to change close button state)", AutoSize = true, Padding = new Padding(0, 0, 0, 10) });
        layout.Controls.Add(windows); layout.Controls.Add(status);
        var footer = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        footer.ColumnStyles.Add(new(SizeType.Percent, 100)); footer.ColumnStyles.Add(new(SizeType.AutoSize));
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = Padding.Empty };
        var settingsButton = new Button { Text = "Settings", AutoSize = true };
        var uninstallButton = new Button { Text = "Uninstall", AutoSize = true };
        settingsButton.Click += (_, _) => settings(); uninstallButton.Click += (_, _) => uninstall();
        actions.Controls.AddRange([settingsButton, uninstallButton]);
        var author = new LinkLabel
        {
            Text = "by BenRussoUSA @ GitHub", AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Margin = new Padding(8, 0, 0, 7),
            LinkBehavior = LinkBehavior.AlwaysUnderline, LinkColor = Color.Blue,
            ActiveLinkColor = Color.DarkBlue, VisitedLinkColor = Color.Blue
        };
        author.LinkClicked += (_, _) => AppInfo.OpenUrl("https://github.com/BenRussoUSA");
        footer.Controls.Add(actions, 0, 0); footer.Controls.Add(author, 1, 0);
        layout.Controls.Add(footer); Controls.Add(layout);
        windows.DrawItem += DrawWindow;
        windows.FontChanged += (_, _) => ResizeRows();
        DpiChanged += (_, _) => ResizeRows();
        ResizeRows();
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

    private int ScalePixels(int value) => (int)Math.Round(value * DeviceDpi / 96f);

    private void ResizeRows()
    {
        windows.ItemHeight = Math.Max(ScalePixels(23), windows.Font.Height + ScalePixels(6));
        windows.HorizontalExtent = windows.Items.Cast<WindowEntry>()
            .Select(w => TextRenderer.MeasureText(w.Title, windows.Font).Width + ScalePixels(46)).DefaultIfEmpty(0).Max();
        windows.Invalidate();
    }

    private void DrawWindow(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= windows.Items.Count) return;
        e.DrawBackground();
        var window = (WindowEntry)windows.Items[e.Index];
        uint state = closeStates.GetValueOrDefault(window.Handle, NativeWindows.MissingMenuItem);
        var marker = new Rectangle(e.Bounds.Left + ScalePixels(12), e.Bounds.Top + (e.Bounds.Height - ScalePixels(16)) / 2, ScalePixels(16), ScalePixels(16));
        if (state != NativeWindows.MissingMenuItem && (state & 3) != 0)
            e.Graphics.DrawIcon(blockedIcon, marker);
        else
        {
            var graphicsState = e.Graphics.Save();
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var ball = new Rectangle(marker.Left + ScalePixels(3), marker.Top + ScalePixels(3), ScalePixels(10), ScalePixels(10));
            if (state == NativeWindows.MissingMenuItem)
            {
                // Do not display a verified ON indicator for an unsupported window.
                using var pen = new Pen(SystemColors.GrayText, Math.Max(1, ScalePixels(1)));
                e.Graphics.DrawEllipse(pen, ball);
            }
            else
            {
                using var brush = new SolidBrush(Color.FromArgb(30, 160, 65));
                e.Graphics.FillEllipse(brush, ball);
            }
            e.Graphics.Restore(graphicsState);
        }
        var textBounds = new Rectangle(e.Bounds.Left + ScalePixels(38), e.Bounds.Top,
            Math.Max(0, e.Bounds.Width - ScalePixels(40)), e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, window.Title, windows.Font, textBounds, e.ForeColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);
        e.DrawFocusRectangle();
    }

    public void RefreshWindows()
    {
        var entries = controller.Windows.OrderBy(w => w.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        var nextStates = entries.ToDictionary(w => w.Handle, w => NativeWindows.CloseState(w.Handle));
        bool changed = !entries.SequenceEqual(windows.Items.Cast<WindowEntry>()) ||
            nextStates.Count != closeStates.Count || nextStates.Any(kv => !closeStates.TryGetValue(kv.Key, out uint old) || old != kv.Value);
        closeStates.Clear(); foreach (var pair in nextStates) closeStates[pair.Key] = pair.Value;
        if (!entries.SequenceEqual(windows.Items.Cast<WindowEntry>()))
        {
            var selected = (windows.SelectedItem as WindowEntry)?.Handle;
            int top = windows.TopIndex;
            windows.BeginUpdate(); windows.Items.Clear(); windows.Items.AddRange(entries);
            windows.SelectedIndex = Array.FindIndex(entries, w => w.Handle == selected);
            if (windows.Items.Count > 0) windows.TopIndex = Math.Min(top, windows.Items.Count - 1);
            windows.EndUpdate(); ResizeRows();
        }
        if (changed) windows.Invalidate();
        ShowStatus();
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
                RefreshWindows();
            };
            choices.Items.Add(item);
        }
        choices.Show(windows, point);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { foreach (ToolStripItem item in choices.Items) item.Font?.Dispose(); choices.Dispose(); blockedIcon.Dispose(); }
        base.Dispose(disposing);
    }
}
