using System.Text.Json;
using System.Text.RegularExpressions;

namespace CBB;

public sealed record WindowRule(string Pattern, bool IsRegex = false)
{
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Pattern)) return "Enter a window title or pattern.";
        if (Pattern.Length > 2048) return "Limit the pattern to 2,048 characters.";
        if (!IsRegex) return null;
        try { _ = new Regex(Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50)); return null; }
        catch (ArgumentException ex) { return ex.Message; }
    }

    public bool Matches(string title)
    {
        if (!IsRegex) return string.Equals(Pattern, title, StringComparison.Ordinal);
        try { return Regex.IsMatch(title, Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50)); }
        catch (ArgumentException) { return false; }
        catch (RegexMatchTimeoutException) { return false; }
    }

    public override string ToString() => $"{(IsRegex ? "Regex" : "Exact title")}: {Pattern}";
}

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 1;
    public bool LaunchAtStartup { get; set; }
    public List<WindowRule> Rules { get; set; } = [];
}

public sealed class SettingsStore(string path)
{
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CBB-CloseButtonBlocker", "settings.json");
    public string? LoadWarning { get; private set; }
    public AppSettings Load()
    {
        if (!File.Exists(path)) return new();
        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? throw new JsonException("Empty settings.");
            if (settings.SchemaVersion != 1) throw new JsonException("Settings were created by an incompatible CBB version.");
            if (settings.Rules == null || settings.Rules.Any(r => r == null || r.Validate() != null)) throw new JsonException("A saved rule is invalid.");
            return settings;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            LoadWarning = $"Saved settings could not be loaded. The existing file has been retained.\n{ex.Message}\n\n{path}";
            return new();
        }
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temp = path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true);
        else File.Move(temp, path);
    }
}
