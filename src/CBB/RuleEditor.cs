namespace CBB;

public sealed class RuleEditor : Form
{
    private readonly TextBox pattern = new() { Dock = DockStyle.Fill, MaxLength = 2048 };
    private readonly ComboBox kind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox sample = new() { Dock = DockStyle.Fill, MaxLength = 1024 };
    private readonly Label result = new() { AutoSize = true, MaximumSize = new Size(520, 0) };
    private readonly ListBox matches = new() { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true, AccessibleName = "Matching open windows" };
    private readonly System.Windows.Forms.Timer previewDelay = new() { Interval = 200 };
    public WindowRule? Rule { get; private set; }

    public RuleEditor(WindowRule? existing = null, Func<IEnumerable<WindowEntry>>? openWindows = null)
    {
        Font = new Font("Segoe UI", 9); AutoScaleMode = AutoScaleMode.Dpi;
        Text = existing == null ? "Add automatic blocking rule" : "Edit automatic blocking rule";
        ClientSize = new Size(600, 470); MinimumSize = new Size(560, 480);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 10 };
        for (int i = 0; i < 10; i++) layout.RowStyles.Add(new RowStyle(i == 8 ? SizeType.Percent : SizeType.AutoSize, i == 8 ? 100 : 0));
        kind.Items.AddRange(["Exact window title (case-sensitive)", "Regular expression (case-sensitive unless (?i) is used)"]);
        kind.SelectedIndex = existing?.IsRegex == true ? 1 : 0;
        pattern.Text = existing?.Pattern ?? "";
        layout.Controls.Add(new Label { Text = "Match type", AutoSize = true });
        layout.Controls.Add(kind);
        layout.Controls.Add(new Label { Text = "Window title or regular expression", AutoSize = true });
        layout.Controls.Add(pattern);
        layout.Controls.Add(new Label { Text = "Test against a window title (optional)", AutoSize = true });
        layout.Controls.Add(sample);
        layout.Controls.Add(result);
        layout.Controls.Add(new Label { Text = "Matching windows currently open (preview)", AutoSize = true, Margin = new Padding(0, 12, 0, 6) });
        layout.Controls.Add(matches);
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        var save = new Button { Text = "Save", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var help = new Button { Text = "Help", AutoSize = true };
        buttons.Controls.AddRange([save, cancel, help]); layout.Controls.Add(buttons); Controls.Add(layout);
        AcceptButton = save; CancelButton = cancel;
        void Preview(object? sender, EventArgs e)
        {
            previewDelay.Stop();
            matches.Items.Clear();
            var rule = new WindowRule(pattern.Text, kind.SelectedIndex == 1);
            if (rule.Validate() is string error) { result.Text = error; return; }
            result.Text = sample.Text.Length == 0 ? "" : rule.Matches(sample.Text) ? "The test title matches." : "The test title does not match.";
            var budget = System.Diagnostics.Stopwatch.StartNew();
            foreach (var window in openWindows?.Invoke() ?? Enumerable.Empty<WindowEntry>())
            {
                if (rule.Matches(window.Title)) matches.Items.Add(window.Title);
                if (rule.TimedOut || budget.ElapsedMilliseconds >= 50)
                { result.Text = "Preview stopped at its time limit. Simplify the expression for a complete preview."; break; }
            }
            if (rule.TimedOut) result.Text = "The expression exceeded its 50 ms limit. Simplify it before saving.";
            if (matches.Items.Count == 0) matches.Items.Add("No matching open windows in this preview.");
        }
        void SchedulePreview(object? sender, EventArgs e) { previewDelay.Stop(); previewDelay.Start(); }
        pattern.TextChanged += SchedulePreview; sample.TextChanged += SchedulePreview; kind.SelectedIndexChanged += SchedulePreview;
        previewDelay.Tick += Preview; Shown += Preview;
        help.Click += (_, _) => ShowHelp(this);
        save.Click += (_, _) =>
        {
            var rule = new WindowRule(pattern.Text, kind.SelectedIndex == 1);
            if (rule.Validate() is string error) { MessageBox.Show(this, error, "Invalid rule"); return; }
            Rule = rule; DialogResult = DialogResult.OK;
        };
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) previewDelay.Dispose();
        base.Dispose(disposing);
    }

    public static void ShowHelp(IWin32Window owner)
    {
        using var help = new Form { Text = "Window rule help", ClientSize = new Size(650, 460), StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false };
        help.Controls.Add(new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10), BackColor = SystemColors.Window,
            Text = "EXACT WINDOW TITLE\r\nMatches the entire title, including capitalization and spaces.\r\n\r\nREGULAR EXPRESSIONS\r\nA regex matches anywhere in the title unless you add anchors.\r\n\r\nNotepad           Contains Notepad\r\n^Notepad$         Exactly Notepad\r\n.* - Notepad$     Ends with ' - Notepad'\r\n(?i)notepad       Contains notepad, ignoring capitalization\r\nReport \\.txt$      Ends with 'Report .txt' (a literal dot)\r\n^(Word|Excel)$    Exactly Word or Excel\r\n\r\n^ = beginning, $ = end, . = any character, * = zero or more.\r\nEscape punctuation such as a literal dot with a backslash: \\.\r\nUse the test field before saving. Each regex has a 50 ms time limit. A rule that times out is paused until edited or CBB restarts. Up to 100 rules can be saved.\r\n\r\nWHEN RULES APPLY\r\nRules apply when CBB first detects a window, including windows already open when CBB starts. Saving rules also checks open windows that have no manual override.\r\n\r\nTurning Block Close Button Off overrides rules for that window until it closes or CBB exits. A new matching window is blocked again. Rules are saved across reboots and upgrades.\r\n\r\nRemoving a rule does not immediately unblock an already blocked window. Turn Block Close Button Off for that window."
        });
        help.ShowDialog(owner);
    }
}
