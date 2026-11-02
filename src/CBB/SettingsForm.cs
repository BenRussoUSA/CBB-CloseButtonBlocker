namespace CBB;

public sealed class SettingsForm : Form
{
    private readonly ListBox rules = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true, IntegralHeight = false };
    private readonly CheckBox startup = new() { Text = "Launch at startup (when I sign into Windows)", AutoSize = true };
    public AppSettings? Result { get; private set; }

    public SettingsForm(AppSettings settings)
    {
        Text = "CBB Settings"; ClientSize = new Size(650, 420); MinimumSize = new Size(550, 380);
        StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 5 };
        layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        layout.RowStyles.Add(new(SizeType.Percent, 100)); layout.RowStyles.Add(new(SizeType.AutoSize)); layout.RowStyles.Add(new(SizeType.AutoSize));
        startup.Checked = AppInfo.StartupEnabled();
        layout.Controls.Add(startup);
        layout.Controls.Add(new Label { Text = "Automatically block matching windows", AutoSize = true, Margin = new Padding(0, 14, 0, 8) });
        foreach (var rule in settings.Rules) rules.Items.Add(rule);
        layout.Controls.Add(rules);
        var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        var add = new Button { Text = "Add", AutoSize = true };
        var edit = new Button { Text = "Edit", AutoSize = true };
        var remove = new Button { Text = "Remove", AutoSize = true };
        var help = new Button { Text = "Help", AutoSize = true };
        actions.Controls.AddRange([add, edit, remove, help]); layout.Controls.Add(actions);
        var buttons = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = new Button { Text = "Save", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        buttons.Controls.AddRange([save, cancel]); layout.Controls.Add(buttons); Controls.Add(layout);
        AcceptButton = save; CancelButton = cancel;
        void EditRule(bool adding)
        {
            if (!adding && rules.SelectedIndex < 0) return;
            using var editor = new RuleEditor(adding ? null : (WindowRule)rules.SelectedItem!);
            if (editor.ShowDialog(this) != DialogResult.OK || editor.Rule == null) return;
            if (adding) rules.Items.Add(editor.Rule); else rules.Items[rules.SelectedIndex] = editor.Rule;
        }
        add.Click += (_, _) => EditRule(true); edit.Click += (_, _) => EditRule(false); rules.DoubleClick += (_, _) => EditRule(false);
        remove.Click += (_, _) => { if (rules.SelectedIndex >= 0) rules.Items.RemoveAt(rules.SelectedIndex); };
        help.Click += (_, _) => RuleEditor.ShowHelp(this);
        save.Click += (_, _) => { Result = new() { LaunchAtStartup = startup.Checked, Rules = rules.Items.Cast<WindowRule>().ToList() }; DialogResult = DialogResult.OK; };
    }
}
