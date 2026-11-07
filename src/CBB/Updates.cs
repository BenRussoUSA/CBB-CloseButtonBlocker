using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace CBB;

public static class Updates
{
    [DataContract]
    private sealed class Release
    {
        [DataMember(Name = "tag_name")] public string? Tag { get; set; }
        [DataMember(Name = "html_url")] public string? Url { get; set; }
        [DataMember(Name = "draft")] public bool Draft { get; set; }
    }

    public static (Version Version, int Stage, int Number)? ParseVersion(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length > 128) return null;
        var m = Regex.Match(text, @"^v?(\d+\.\d+\.\d+)(?:-?(alpha|beta|rc)(?:[.-]?(\d+))?)?$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
        if (!m.Success || !Version.TryParse(m.Groups[1].Value, out var version)) return null;
        int stage = m.Groups[2].Value.ToLowerInvariant() switch { "alpha" => 0, "beta" => 1, "rc" => 2, _ => 3 };
        return (version, stage, int.TryParse(m.Groups[3].Value, out int number) ? number : 0);
    }

    public static bool IsNewer(string candidate, string current)
    {
        var a = ParseVersion(candidate); var b = ParseVersion(current);
        return a.HasValue && b.HasValue && a.Value.CompareTo(b.Value) > 0;
    }

    public static (string Message, string? Url) ReadResponse(Stream data)
    {
        var releases = BoundedJson.Read<List<Release>>(data, 1024 * 1024);
        var newer = releases.Where(r => r != null && !r.Draft && r.Tag != null && IsNewer(r.Tag, AppInfo.Version))
            .OrderByDescending(r => ParseVersion(r.Tag!)).FirstOrDefault();
        if (newer == null) return ("No newer release was found.", null);
        string? url = newer.Url;
        if (url == null || !url.StartsWith(AppInfo.Repository + "/releases/tag/", StringComparison.Ordinal) ||
            !Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "github.com" ||
            uri.UserInfo.Length != 0 || !uri.IsDefaultPort)
            return ("An invalid release link was ignored.", null);
        return ($"Version {newer.Tag} is available. Click to open its GitHub release page.", url);
    }

    public static async Task<(string Message, string? Url)> CheckAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15), MaxResponseContentBufferSize = 1024 * 1024 };
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"CBB-CloseButtonBlocker/{AppInfo.Version}");
        using var response = await http.GetAsync("https://api.github.com/repos/BenRussoUSA/CBB-CloseButtonBlocker/releases?per_page=30");
        if (response.StatusCode == HttpStatusCode.NotFound)
            return ("GitHub releases are currently unavailable. Try again later or open the release page in your browser.", null);
        response.EnsureSuccessStatusCode();
        using var data = await response.Content.ReadAsStreamAsync();
        return ReadResponse(data);
    }
}
