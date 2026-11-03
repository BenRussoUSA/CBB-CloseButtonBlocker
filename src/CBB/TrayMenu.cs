namespace CBB;

public sealed class TrayMenu : ContextMenuStrip
{
    public TrayMenu(Action about, Func<Task> updates, Action exit)
    {
        Items.Add("About CBB-CloseButtonBlocker", null, (_, _) => about());
        Items.Add("Check for updates", null, async (_, _) => await updates());
        Items.Add(new ToolStripSeparator());
        Items.Add("Exit CBB", null, (_, _) => exit());
    }
}
