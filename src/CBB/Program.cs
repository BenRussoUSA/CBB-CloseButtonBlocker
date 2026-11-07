namespace CBB;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        if (args.Contains("--prepare-uninstall"))
        {
            StopRunningInstance();
            AppInfo.SetStartup(false);
            var store = new SettingsStore(SettingsStore.DefaultPath);
            var settings = store.Load();
            if (store.LoadWarning == null) { settings.LaunchAtStartup = false; store.Save(settings); }
            return;
        }
        if (args.Contains("--stop"))
        {
            StopRunningInstance();
            return;
        }
        using var mutex = new Mutex(true, AppInfo.MutexName, out bool first);
        if (!first) { MessageBox.Show("CBB is already running. Look for its icon in the Windows notification area (including hidden icons).", AppInfo.Name); return; }
        using var context = new TrayContext();
        Application.Run(context);
    }

    private static void StopRunningInstance()
    {
        try { using var stop = EventWaitHandle.OpenExisting(AppInfo.StopEventName); stop.Set(); } catch (WaitHandleCannotBeOpenedException) { return; }
        try
        {
            using var mutex = Mutex.OpenExisting(AppInfo.MutexName);
            bool acquired;
            try { acquired = mutex.WaitOne(TimeSpan.FromSeconds(15)); }
            catch (AbandonedMutexException) { acquired = true; }
            if (acquired) mutex.ReleaseMutex(); else Environment.ExitCode = 1;
        }
        catch (WaitHandleCannotBeOpenedException) { }
    }
}
