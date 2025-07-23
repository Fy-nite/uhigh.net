using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace uhigh.Net.CommandLine.Setup
{
    /// <summary>
    /// Interface for update sources (e.g., GitHub, NuGet, custom server)
    /// </summary>
    public interface IUpdateSource
    {
        Task<string?> GetLatestVersionAsync(bool includePrerelease = false);
        Task<bool> DownloadUpdateAsync(string version, string destinationPath, bool includePrerelease = false);
    }

    /// <summary>
    /// Updater command for checking and applying updates
    /// </summary>
    public class Updater : Command
    {
        public Updater() : base("update", "Check for and apply updates to μHigh")
        {
            var prereleaseOption = new Option<bool>("--prerelease", "Include prerelease (beta) versions");
            this.AddOption(prereleaseOption);

            this.SetHandler(async (bool prerelease) =>
            {
                await RunUpdaterAsync(prerelease);
            }, prereleaseOption);
        }

        private async Task RunUpdaterAsync(bool includePrerelease)
        {
            // 1. Get current version
            var currentVersion = GetCurrentVersion();
            Console.WriteLine($"Current version: {currentVersion}");

            // 2. Get latest version from update source (GitHub)
            IUpdateSource updateSource = new GitHubUpdateSource("fy-nite", "uhigh.net");
            var latestVersion = await updateSource.GetLatestVersionAsync(includePrerelease);
            Console.WriteLine($"Latest version: {latestVersion}");

            // 3. Compare and offer update
            if (latestVersion != null && latestVersion != currentVersion)
            {
                Console.WriteLine("Update available. Downloading...");
                // 4. Download and apply update
                var success = await updateSource.DownloadUpdateAsync(latestVersion, "uhigh-latest.zip", includePrerelease);
                if (success)
                {
                    Console.WriteLine("Update downloaded. (Apply logic here)");
                    // TODO: Extract and replace binaries, restart, etc.
                }
                else
                {
                    Console.WriteLine("Failed to download update.");
                }
            }
            else
            {
                Console.WriteLine("You are already up to date.");
            }
        }

        private string GetCurrentVersion()
        {
            // TODO: Read from assembly or version file
            return "0.1.0";
        }
    }

    /// <summary>
    /// GitHub update source for real release fetching
    /// </summary>
    public class GitHubUpdateSource : IUpdateSource
    {
        private readonly string _owner;
        private readonly string _repo;
        private static readonly HttpClient _http = new();

        public GitHubUpdateSource(string owner, string repo)
        {
            _owner = owner;
            _repo = repo;
        }

        public async Task<string?> GetLatestVersionAsync(bool includePrerelease = false)
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("uhigh-updater");

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<GitHubRelease[]>(json);

            var release = releases?
                .Where(r => includePrerelease || !r.prerelease)
                .OrderByDescending(r => r.published_at)
                .FirstOrDefault();

            return release?.tag_name?.TrimStart('v');
        }

        public async Task<bool> DownloadUpdateAsync(string version, string destinationPath, bool includePrerelease = false)
        {
            var url = $"https://api.github.com/repos/{_owner}/{_repo}/releases";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.UserAgent.ParseAdd("uhigh-updater");

            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return false;

            var json = await resp.Content.ReadAsStringAsync();
            var releases = JsonSerializer.Deserialize<GitHubRelease[]>(json);

            GitHubRelease? release = null;
            if (version == "latest")
            {
                release = releases?
                    .Where(r => includePrerelease || !r.prerelease)
                    .OrderByDescending(r => r.published_at)
                    .FirstOrDefault();
            }
            else
            {
                release = releases?.FirstOrDefault(r => r.tag_name?.TrimStart('v') == version);
            }

            if (release == null) return false;

            var asset = release.assets?.FirstOrDefault(a => a.name.EndsWith(".zip"));
            if (asset == null) return false;

            var assetReq = new HttpRequestMessage(HttpMethod.Get, asset.browser_download_url);
            assetReq.Headers.UserAgent.ParseAdd("uhigh-updater");
            var assetResp = await _http.SendAsync(assetReq);
            if (!assetResp.IsSuccessStatusCode) return false;

            var bytes = await assetResp.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(destinationPath, bytes);
            return true;
        }

        private class GitHubRelease
        {
            public string? tag_name { get; set; }
            public bool prerelease { get; set; }
            public DateTime published_at { get; set; }
            public GitHubAsset[]? assets { get; set; }
        }

        private class GitHubAsset
        {
            public string name { get; set; } = "";
            public string browser_download_url { get; set; } = "";
        }
    }

    /// <summary>
    /// Dummy update source for demonstration (replace with real implementation at somepoint lol)
    /// </summary>
    public class DummyUpdateSource : IUpdateSource
    {
        public Task<string?> GetLatestVersionAsync(bool includePrerelease = false)
        {
            // TODO: Query GitHub releases, NuGet, or custom endpoint
            return Task.FromResult<string?>("0.2.0");
        }

        public Task<bool> DownloadUpdateAsync(string version, string destinationPath, bool includePrerelease = false)
        {
            // TODO: Download the update package
            Console.WriteLine($"Pretending to download version {version} to {destinationPath}");
            return Task.FromResult(true);
        }
    }
}