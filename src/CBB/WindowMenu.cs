using System.Drawing.Drawing2D;

namespace CBB;

public sealed class WindowMenu : Form
{
    private readonly WindowController controller;
    private readonly DataGridView windows = new()
    {
        Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false, RowHeadersVisible = false, MultiSelect = false,
        AutoGenerateColumns = false, BackgroundColor = SystemColors.Window,
        BorderStyle = BorderStyle.FixedSingle, CellBorderStyle = DataGridViewCellBorderStyle.Single,
        ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect, EditMode = DataGridViewEditMode.EditOnEnter,
        AccessibleName = "Open windows", AccessibleDescription = "Turn Hide Close Button on to block closing a window."
    };
    private readonly Label status = new() { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 6) };
    private WindowEntry[] entries = [];
    private bool refreshing;

    public WindowMenu(WindowController controller)
    {
        this.controller = controller;
        Text = $"{AppInfo.Name} · {AppInfo.Version}"; ShowInTaskbar = false;
        ClientSize = new Size(700, 530); MinimumSize = new Size(540, 430); MaximizeBox = false;
        StartPosition = FormStartPosition.Manual;
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(Math.Max(area.Left, area.Right - Width - 12), Math.Max(area.Top, area.Bottom - Height - 12));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(14), ColumnCount = 1, RowCount = 4 };
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.Percent, 100));
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "List of windows", AutoSize = true, Padding = new Padding(0, 0, 0, 10) });
        windows.Columns.Add(new DataGridViewColumn(new CloseToggleCell())
        {
            Name = "HideCloseButton", HeaderText = "Hide Close Button", SortMode = DataGridViewColumnSortMode.NotSortable,
            Resizable = DataGridViewTriState.False
        });
        windows.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "WindowTitle", HeaderText = "Window Title", ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = new DataGridViewCellStyle { Padding = new Padding(8, 0, 8, 0) }
        });
        windows.DefaultCellStyle.SelectionBackColor = SystemColors.ControlLight;
        windows.DefaultCellStyle.SelectionForeColor = SystemColors.ControlText;
        layout.Controls.Add(windows); layout.Controls.Add(status);
        var author = new LinkLabel
        {
            Text = "by BenRussoUSA @ GitHub", AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom, Margin = new Padding(8, 0, 0, 7),
            LinkBehavior = LinkBehavior.AlwaysUnderline, LinkColor = Color.Blue,
            ActiveLinkColor = Color.DarkBlue, VisitedLinkColor = Color.Blue
        };
        author.LinkClicked += (_, _) => AppInfo.OpenUrl("https://github.com/BenRussoUSA");
        layout.Controls.Add(author); Controls.Add(layout);
        windows.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (!refreshing && windows.IsCurrentCellDirty) windows.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };
        windows.CellValueChanged += (_, e) =>
        {
            if (refreshing || e.RowIndex < 0 || e.ColumnIndex != 0) return;
            var row = windows.Rows[e.RowIndex];
            if (row.Tag is not WindowEntry window || row.Cells[0].ReadOnly) return;
            bool blocked = row.Cells[0].Value is true;
            var error = controller.Change(window, closeOn: !blocked);
            RefreshWindows();
            if (error != null) MessageBox.Show(this, error, "CBB could not change this window");
        };
        windows.SelectionChanged += (_, _) => { if (!refreshing) ShowStatus(); };
        windows.FontChanged += (_, _) => ResizeRows();
        DpiChanged += (_, _) => ResizeRows();
        ResizeRows();
        KeyPreview = true; KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        RefreshWindows();
    }

    private int ScalePixels(int value) => (int)Math.Round(value * DeviceDpi / 96f);

    private void ResizeRows()
    {
        windows.Columns[0].Width = Math.Max(ScalePixels(145), TextRenderer.MeasureText("Hide Close Button", windows.Font).Width + ScalePixels(24));
        windows.Columns[1].MinimumWidth = ScalePixels(180);
        windows.ColumnHeadersHeight = Math.Max(ScalePixels(34), windows.Font.Height + ScalePixels(16));
        windows.RowTemplate.Height = Math.Max(ScalePixels(38), windows.Font.Height + ScalePixels(18));
        foreach (DataGridViewRow row in windows.Rows) row.Height = windows.RowTemplate.Height;
        windows.Invalidate();
    }

    public void RefreshWindows()
    {
        if (IsDisposed) return;
        var nextEntries = controller.Windows.OrderBy(w => w.Title, StringComparer.CurrentCultureIgnoreCase).ToArray();
        refreshing = true;
        try
        {
            if (!nextEntries.SequenceEqual(entries))
            {
                var selected = (windows.CurrentRow?.Tag as WindowEntry)?.Handle;
                int column = windows.CurrentCell?.ColumnIndex ?? 0;
                int top = windows.FirstDisplayedScrollingRowIndex;
                windows.Rows.Clear();
                entries = nextEntries;
                foreach (var window in entries)
                {
                    int index = windows.Rows.Add(false, window.Title);
                    windows.Rows[index].Tag = window;
                }
                int selectedIndex = Array.FindIndex(entries, w => w.Handle == selected);
                if (selectedIndex >= 0) windows.CurrentCell = windows.Rows[selectedIndex].Cells[column];
                if (top >= 0 && windows.Rows.Count > 0) windows.FirstDisplayedScrollingRowIndex = Math.Min(top, windows.Rows.Count - 1);
            }
            foreach (DataGridViewRow row in windows.Rows)
            {
                var window = (WindowEntry)row.Tag!;
                uint state = NativeWindows.CloseState(window.Handle);
                bool supported = state != NativeWindows.MissingMenuItem;
                bool blocked = supported && (state & 3) != 0;
                var cell = row.Cells[0];
                cell.ReadOnly = !supported;
                if (!Equals(cell.Value, blocked)) cell.Value = blocked;
                cell.ToolTipText = !supported ? "Unavailable: this window has no standard Windows Close command." :
                    blocked ? "On: Close is blocked. Click or press Space to restore it." :
                    "Off: normal closing is allowed. Click or press Space to block it.";
                row.Cells[1].ToolTipText = window.Title;
            }
        }
        finally { refreshing = false; }
        windows.InvalidateColumn(0);
        ShowStatus();
    }

    private void ShowStatus()
    {
        if (windows.CurrentRow?.Tag is not WindowEntry window)
        {
            status.Text = entries.Length == 0 ? "No eligible windows are currently open." : "Turn a toggle on to block that window's Close button.";
            return;
        }
        if (controller.Errors.TryGetValue(window.Handle, out var error)) { status.Text = error; return; }
        uint state = NativeWindows.CloseState(window.Handle);
        status.Text = state == NativeWindows.MissingMenuItem ? "Unavailable: this window has no standard Windows Close command." :
            (state & 3) == 0 ? "Off: normal closing is allowed. Turn on to block the Close button." :
            "On: the Windows Close command is disabled. The button may remain visible.";
    }
}

// Keep native checkbox input and accessibility; only replace its appearance with a slider.
internal sealed class CloseToggleCell : DataGridViewCheckBoxCell
{
    public CloseToggleCell() : base(false) { }

    protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cellBounds, int rowIndex,
        DataGridViewElementStates cellState, object? value, object? formattedValue, string? errorText,
        DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
    {
        base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
            cellStyle, advancedBorderStyle, paintParts & ~DataGridViewPaintParts.ContentForeground);
        if ((paintParts & DataGridViewPaintParts.ContentForeground) == 0) return;
        float scale = (DataGridView?.DeviceDpi ?? 96) / 96f;
        int Px(int pixels) => (int)Math.Round(pixels * scale);
        bool blocked = value is true;
        bool available = !ReadOnly;
        var track = new Rectangle(cellBounds.Left + (cellBounds.Width - Px(88)) / 2,
            cellBounds.Top + (cellBounds.Height - Px(22)) / 2, Px(42), Px(22));
        var saved = graphics.Save();
        graphics.SetClip(Rectangle.Intersect(clipBounds, cellBounds));
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color trackColor = !available ? SystemColors.ControlDark : blocked ? Color.FromArgb(0, 105, 180) : Color.FromArgb(105, 105, 105);
        using var path = new GraphicsPath();
        path.AddArc(track.Left, track.Top, track.Height, track.Height, 90, 180);
        path.AddArc(track.Right - track.Height, track.Top, track.Height, track.Height, 270, 180);
        path.CloseFigure();
        using var brush = new SolidBrush(trackColor);
        graphics.FillPath(brush, path);
        using var knob = new SolidBrush(available ? Color.White : SystemColors.ControlLight);
        int size = track.Height - Px(6);
        graphics.FillEllipse(knob, blocked && available ? track.Right - size - Px(3) : track.Left + Px(3), track.Top + Px(3), size, size);
        graphics.Restore(saved);
        var text = new Rectangle(track.Right + Px(7), cellBounds.Top, Px(36), cellBounds.Height);
        Color textColor = !available ? SystemColors.GrayText : cellState.HasFlag(DataGridViewElementStates.Selected) ? cellStyle.SelectionForeColor : cellStyle.ForeColor;
        TextRenderer.DrawText(graphics, !available ? "N/A" : blocked ? "On" : "Off", cellStyle.Font, text, textColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
    }

    protected override Rectangle GetContentBounds(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex)
    {
        var size = GetSize(rowIndex);
        return new Rectangle(0, 0, size.Width, size.Height);
    }
}
