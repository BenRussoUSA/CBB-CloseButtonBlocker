using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;

namespace CBB;

[DataContract]
public sealed class WindowRule
{
    [DataMember(IsRequired = true)] public string Pattern { get; private set; }
    [DataMember] public bool IsRegex { get; private set; }
    private Regex? expression;
    public bool TimedOut { get; private set; }
    public WindowRule(string pattern, bool isRegex = false) { Pattern = pattern; IsRegex = isRegex; }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Pattern)) return "Enter a window title or pattern.";
        if (Pattern.Length > 2048) return "Limit the pattern to 2,048 characters.";
        if (!IsRegex) return null;
        try { expression ??= new Regex(Pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(50)); return null; }
        catch (ArgumentException ex) { return ex.Message; }
    }

    public bool Matches(string title)
    {
        if (TimedOut) return false;
        if (!IsRegex) return string.Equals(Pattern, title, StringComparison.Ordinal);
        if (Validate() != null) return false;
        try { return expression!.IsMatch(title); }
        catch (RegexMatchTimeoutException) { TimedOut = true; return false; }
    }

    public override string ToString() => $"{(IsRegex ? "Regex" : "Exact title")}: {Pattern}";
}

[DataContract]
public sealed class AppSettings
{
    [DataMember(IsRequired = true)] public int SchemaVersion { get; set; } = 1;
    [DataMember] public bool LaunchAtStartup { get; set; }
    [DataMember(IsRequired = true)] public List<WindowRule> Rules { get; set; } = [];
}

// Bound the bytes actually read, including a response/file that grows during the read.
public static class BoundedJson
{
    public static T Read<T>(Stream input, int maxBytes)
    {
        using var data = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (data.Length + count > maxBytes) throw new SerializationException("JSON exceeds the allowed size.");
            data.Write(buffer, 0, count);
        }
        data.Position = 0;
        return (T)(new DataContractJsonSerializer(typeof(T)).ReadObject(data) ?? throw new SerializationException("Empty JSON."));
    }
}

public sealed class SettingsStore(string path)
{
    public const int MaxBytes = 1024 * 1024;
    public const int MaxRules = 100;
    public static string DefaultPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CBB-CloseButtonBlocker", "settings.json");
    public string? LoadWarning { get; private set; }

    public static void Validate(AppSettings settings)
    {
        if (settings.SchemaVersion != 1) throw new SerializationException("Settings were created by an incompatible CBB version.");
        if (settings.Rules == null || settings.Rules.Count > MaxRules)
            throw new SerializationException($"Settings may contain at most {MaxRules} rules.");
        if (settings.Rules.Any(r => r == null || r.Validate() != null)) throw new SerializationException("A saved rule is invalid.");
    }

    public AppSettings Load()
    {
        LoadWarning = null;
        if (!File.Exists(path)) return new();
        try
        {
            using var input = File.OpenRead(path);
            var settings = BoundedJson.Read<AppSettings>(input, MaxBytes);
            Validate(settings);
            return settings;
        }
        catch (Exception ex) when (ex is IOException or SerializationException or UnauthorizedAccessException or System.Xml.XmlException)
        {
            LoadWarning = $"Saved settings could not be loaded. The existing file has been retained.\n{ex.Message}\n\n{path}";
            return new();
        }
    }

    public void Save(AppSettings settings)
    {
        Validate(settings);
        using var data = new MemoryStream();
        new DataContractJsonSerializer(typeof(AppSettings)).WriteObject(data, settings);
        if (data.Length > MaxBytes) throw new SerializationException("Settings exceed the 1 MiB limit.");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + ".tmp";
        using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            data.Position = 0;
            data.CopyTo(output);
            output.Flush(true);
        }
        if (File.Exists(path)) File.Replace(temp, path, path + ".bak", true);
        else File.Move(temp, path);
    }
}
