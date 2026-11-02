using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CBB;

public static class Updates
{
    public static (Version Version, int Stage, int Number)? ParseVersion(string text)
    {
        var m = Regex.Match(text, @"^v?(\d+\.\d+\.\d+)(?:-?(alpha|beta|rc)(?:[.-]?(\d+))?)?$", RegexOptions.IgnoreCase);
        if (!m.Success || !Version.TryParse(m.Groups[1].Value, out var version)) return null;
        int stage = m.Groups[2].Value.ToLowerInvariant() switch { "alpha" => 0, "beta" => 1, "rc" => 2, _ => 3 };
        return (version, stage, int.TryParse(m.Groups[3].Value, out int number) ? number : 0);
    }

    public static bool IsNewer(string candidate, string current)
    {
        var a = ParseVersion(candidate); var b = ParseVersion(current);
        return a.HasValue && b.HasValue && a.Value.CompareTo(b.Value) > 0;
    }

    public static async Task<(string Message, string? Url)> CheckAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("CBB-CloseButtonBlocker/0.8.0");
        using var response = await http.GetAsync("https://api.github.com/repos/BenRussoUSA/CBB-CloseButtonBlocker/releases?per_page=30");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return ("This is a private alpha. Sign into GitHub with repository access to check for updates manually.", null);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var newer = doc.RootElement.EnumerateArray()
            .Where(r => !r.GetProperty("draft").GetBoolean() && IsNewer(r.GetProperty("tag_name").GetString()!, AppInfo.Version))
            .OrderByDescending(r => ParseVersion(r.GetProperty("tag_name").GetString()!)).ToArray();
        if (newer.Length == 0) return ("No newer release was found.", null);
        string? url = newer[0].GetProperty("html_url").GetString();
        if (url == null || !url.StartsWith(AppInfo.Repository + "/releases/tag/", StringComparison.Ordinal)) return ("An invalid release link was ignored.", null);
        return ($"Version {newer[0].GetProperty("tag_name").GetString()} is available. Click to open its GitHub release page.", url);
    }
}
