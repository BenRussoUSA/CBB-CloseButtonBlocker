namespace CBB;

public sealed class RuleEditor : Form
{
    private readonly TextBox pattern = new() { Dock = DockStyle.Fill };
    private readonly ComboBox kind = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly TextBox sample = new() { Dock = DockStyle.Fill, PlaceholderText = "Paste a window title to test" };
    private readonly Label result = new() { AutoSize = true, MaximumSize = new Size(520, 0) };
    public WindowRule? Rule { get; private set; }

    public RuleEditor(WindowRule? existing = null)
    {
        Text = existing == null ? "Add automatic blocking rule" : "Edit automatic blocking rule";
        ClientSize = new Size(560, 310); MinimumSize = new Size(520, 350);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 8 };
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
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        var save = new Button { Text = "Save", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        var help = new Button { Text = "Help", AutoSize = true };
        buttons.Controls.AddRange([save, cancel, help]); layout.Controls.Add(buttons); Controls.Add(layout);
        AcceptButton = save; CancelButton = cancel;
        void Preview(object? sender, EventArgs e)
        {
            var rule = new WindowRule(pattern.Text, kind.SelectedIndex == 1);
            result.Text = rule.Validate() is string error ? error : sample.Text.Length == 0 ? "" : rule.Matches(sample.Text) ? "Matches: this window would be blocked." : "Does not match (or pattern exceeded the time limit).";
        }
        pattern.TextChanged += Preview; sample.TextChanged += Preview; kind.SelectedIndexChanged += Preview;
        help.Click += (_, _) => ShowHelp(this);
        save.Click += (_, _) =>
        {
            var rule = new WindowRule(pattern.Text, kind.SelectedIndex == 1);
            if (rule.Validate() is string error) { MessageBox.Show(this, error, "Invalid rule"); return; }
            Rule = rule; DialogResult = DialogResult.OK;
        };
    }

    public static void ShowHelp(IWin32Window owner)
    {
        using var help = new Form { Text = "Window rule help", ClientSize = new Size(650, 460), StartPosition = FormStartPosition.CenterParent, ShowInTaskbar = false };
        help.Controls.Add(new TextBox
        {
            Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10), BackColor = SystemColors.Window,
            Text = "EXACT WINDOW TITLE\r\nMatches the entire title, including capitalization and spaces.\r\n\r\nREGULAR EXPRESSIONS\r\nA regex matches anywhere in the title unless you add anchors.\r\n\r\nNotepad           Contains Notepad\r\n^Notepad$         Exactly Notepad\r\n.* - Notepad$     Ends with ' - Notepad'\r\n(?i)notepad       Contains notepad, ignoring capitalization\r\nReport \\.txt$      Ends with 'Report .txt' (a literal dot)\r\n^(Word|Excel)$    Exactly Word or Excel\r\n\r\n^ = beginning, $ = end, . = any character, * = zero or more.\r\nEscape punctuation such as a literal dot with a backslash: \\.\r\nUse the test field before saving. Matching has a 50 ms time limit.\r\n\r\nWHEN RULES APPLY\r\nRules apply when CBB first detects a window, including windows already open when CBB starts. Saving rules also checks open windows that have no manual override.\r\n\r\nChoosing Close Button ON overrides rules for that window until it closes or CBB exits. A new matching window is blocked again. Rules are saved across reboots and upgrades.\r\n\r\nRemoving a rule does not immediately unblock an already blocked window. Use Close Button ON for that window."
        });
        help.ShowDialog(owner);
    }
}
