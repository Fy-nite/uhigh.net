using System.Diagnostics;
using System.Text.Json;
using uhigh.Net.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace uhigh.Net.NuGet
{
    /// <summary>
    /// The operation timer class for tracking timing information
    /// </summary>
    public class OperationTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly DiagnosticsReporter _diagnostics;
        private readonly Stopwatch _stopwatch;
        private readonly bool _verboseMode;

        public OperationTimer(string operationName, DiagnosticsReporter diagnostics, bool verboseMode = false)
        {
            _operationName = operationName;
            _diagnostics = diagnostics;
            _verboseMode = verboseMode;
            _stopwatch = Stopwatch.StartNew();
            if (_verboseMode)
            {
                _diagnostics.ReportInfo($"Starting {_operationName}");
            }
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var elapsed = _stopwatch.Elapsed;
            if (_verboseMode)
            {
                _diagnostics.ReportInfo($"Completed {_operationName} in {FormatDuration(elapsed)}");
            }
            else
            {
                _diagnostics.ReportInfo($"{_operationName} ({FormatDuration(elapsed)})");
            }
        }

        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMilliseconds < 1000)
                return $"{duration.TotalMilliseconds:F0}ms";
            if (duration.TotalSeconds < 60)
                return $"{duration.TotalSeconds:F1}s";
            return $"{duration.TotalMinutes:F1}m";
        }
    }

    /// <summary>
    /// The nu get manager class
    /// </summary>
    public class NuGetManager
    {
        private readonly DiagnosticsReporter _diagnostics;
        private readonly string _globalPackagesPath;
        private readonly string[] _defaultSources = {
            "https://api.nuget.org/v3-flatcontainer/",
            "https://api.nuget.org/v3/index.json"
        };

        public NuGetManager(DiagnosticsReporter? diagnostics = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _globalPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        }

        public async Task<bool> RestorePackagesAsync(uhighProject project, string projectDir, bool force = false)
        {
            using var timer = new OperationTimer("NuGet restore", _diagnostics, true);
            try
            {
                if (project.Dependencies.Count == 0)
                {
                    _diagnostics.ReportInfo("No packages to restore");
                    return true;
                }

                _diagnostics.ReportInfo($"Restoring {project.Dependencies.Count} package(s) for {project.Name}");

                var success = true;
                var packageTasks = new List<Task<(string name, string version, bool success, TimeSpan duration)>>();

                foreach (var package in project.Dependencies)
                {
                    packageTasks.Add(RestorePackageWithTimingAsync(package, projectDir, force));
                }

                var results = await Task.WhenAll(packageTasks);

                foreach (var (name, version, packageSuccess, duration) in results)
                {
                    if (!packageSuccess)
                    {
                        success = false;
                        _diagnostics.ReportError($"Failed to restore package: {name} v{version}");
                    }
                    else
                    {
                        _diagnostics.ReportInfo($"  -> {name} v{version} ({OperationTimer.FormatDuration(duration)})");
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Package restoration failed: {ex.Message}");
                return false;
            }
        }

        private async Task<(string name, string version, bool success, TimeSpan duration)> RestorePackageWithTimingAsync(PackageReference package, string projectDir, bool force)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var success = await RestorePackageAsync(package, projectDir, force);
                stopwatch.Stop();
                return (package.Name, package.Version, success, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _diagnostics.ReportError($"Failed to restore {package.Name}: {ex.Message}");
                return (package.Name, package.Version, false, stopwatch.Elapsed);
            }
        }

        private async Task<bool> RestorePackageAsync(PackageReference package, string projectDir, bool force)
        {
            try
            {
                var packageDir = Path.Combine(_globalPackagesPath, package.Name.ToLowerInvariant(), package.Version.ToLowerInvariant());

                if (Directory.Exists(packageDir) && !force)
                {
                    _diagnostics.ReportInfo($"Package {package.Name} v{package.Version} already exists");
                    return true;
                }

                using var dotnetTimer = new OperationTimer($"dotnet restore for {package.Name}", _diagnostics, false);
                if (await TryDotNetRestoreAsync(package, projectDir))
                {
                    return true;
                }

                using var downloadTimer = new OperationTimer($"direct download for {package.Name}", _diagnostics, false);
                return await DownloadPackageDirectlyAsync(package);
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Failed to restore {package.Name}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TryDotNetRestoreAsync(PackageReference package, string projectDir)
        {
            try
            {
                var tempProjPath = Path.Combine(Path.GetTempPath(), $"uhigh-restore-{Guid.NewGuid()}.csproj");
                var tempProjContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""{package.Name}"" Version=""{package.Version}"" />
  </ItemGroup>
</Project>";

                await File.WriteAllTextAsync(tempProjPath, tempProjContent);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"restore \"{tempProjPath}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                try { File.Delete(tempProjPath); } catch { }

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> DownloadPackageDirectlyAsync(PackageReference package)
        {
            try
            {
                var packageName = package.Name.ToLowerInvariant();
                var packageVersion = package.Version.ToLowerInvariant();

                using var httpClient = new HttpClient();

                var downloadUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName}/{packageVersion}/{packageName}.{packageVersion}.nupkg";

                _diagnostics.ReportInfo($"Downloading {package.Name} from {downloadUrl}");

                var response = await httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _diagnostics.ReportError($"Failed to download {package.Name}: HTTP {response.StatusCode}");
                    return false;
                }

                var packageBytes = await response.Content.ReadAsByteArrayAsync();

                var packageDir = Path.Combine(_globalPackagesPath, packageName, packageVersion);
                Directory.CreateDirectory(packageDir);

                using var packageStream = new MemoryStream(packageBytes);
                using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);

                archive.ExtractToDirectory(packageDir, overwriteFiles: true);

                _diagnostics.ReportInfo($"Extracted {package.Name} to {packageDir}");
                return true;
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Direct download failed for {package.Name}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetPackageAssembliesAsync(PackageReference package, string targetFramework = "net8.0")
        {
            var assemblies = new List<string>();

            try
            {
                var packageDir = Path.Combine(_globalPackagesPath, package.Name.ToLowerInvariant(), package.Version.ToLowerInvariant());

                if (!Directory.Exists(packageDir))
                {
                    _diagnostics.ReportWarning($"Package directory not found: {packageDir}");
                    return assemblies;
                }

                var libDir = Path.Combine(packageDir, "lib");
                if (Directory.Exists(libDir))
                {
                    var frameworkDirs = Directory.GetDirectories(libDir)
                        .Where(dir => IsCompatibleFramework(Path.GetFileName(dir), targetFramework))
                        .OrderByDescending(dir => GetFrameworkPriority(Path.GetFileName(dir)))
                        .ToList();

                    foreach (var frameworkDir in frameworkDirs)
                    {
                        var dlls = Directory.GetFiles(frameworkDir, "*.dll", SearchOption.TopDirectoryOnly);
                        assemblies.AddRange(dlls);

                        if (assemblies.Count > 0) break;
                    }
                }

                var refDir = Path.Combine(packageDir, "ref");
                if (Directory.Exists(refDir) && assemblies.Count == 0)
                {
                    var frameworkDirs = Directory.GetDirectories(refDir)
                        .Where(dir => IsCompatibleFramework(Path.GetFileName(dir), targetFramework))
                        .OrderByDescending(dir => GetFrameworkPriority(Path.GetFileName(dir)))
                        .ToList();

                    foreach (var frameworkDir in frameworkDirs)
                    {
                        var dlls = Directory.GetFiles(frameworkDir, "*.dll", SearchOption.TopDirectoryOnly);
                        assemblies.AddRange(dlls);

                        if (assemblies.Count > 0) break;
                    }
                }

                if (assemblies.Count == 0)
                {
                    var allDlls = Directory.GetFiles(packageDir, "*.dll", SearchOption.AllDirectories)
                        .Where(dll => !dll.Contains("runtimes") || dll.Contains("native"))
                        .ToList();
                    assemblies.AddRange(allDlls);
                }

                _diagnostics.ReportInfo($"Found {assemblies.Count} assemblies for {package.Name}");
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Failed to get assemblies for {package.Name}: {ex.Message}");
            }

            return assemblies;
        }

        private bool IsCompatibleFramework(string packageFramework, string targetFramework)
        {
            var targetParts = ParseFramework(targetFramework);
            var packageParts = ParseFramework(packageFramework);

            if (targetParts.name != packageParts.name) return false;

            return packageParts.version <= targetParts.version;
        }

        private (string name, Version version) ParseFramework(string framework)
        {
            var match = Regex.Match(framework, @"^(net|netstandard|netcoreapp)(\d+\.?\d*)");
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                if (Version.TryParse(match.Groups[2].Value, out var version))
                {
                    return (name, version);
                }
            }

            return ("unknown", new Version(0, 0));
        }

        private int GetFrameworkPriority(string framework)
        {
            if (framework.StartsWith("net8")) return 80;
            if (framework.StartsWith("net7")) return 70;
            if (framework.StartsWith("net6")) return 60;
            if (framework.StartsWith("net5")) return 50;
            if (framework.StartsWith("netcoreapp3")) return 40;
            if (framework.StartsWith("netstandard2")) return 30;
            if (framework.StartsWith("netstandard1")) return 20;
            if (framework.StartsWith("net4")) return 10;
            return 0;
        }

        public async Task<List<PackageSearchResult>> SearchPackagesAsync(string searchTerm, int take = 10)
        {
            try
            {
                using var httpClient = new HttpClient();
                var searchUrl = $"https://azuresearch-usnc.nuget.org/query?q={Uri.EscapeDataString(searchTerm)}&take={take}";

                _diagnostics.ReportInfo($"Searching for packages: {searchTerm}");

                var response = await httpClient.GetStringAsync(searchUrl);
                var searchResult = JsonSerializer.Deserialize<NuGetSearchResponse>(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var packages = new List<PackageSearchResult>();

                if (searchResult?.Data != null)
                {
                    foreach (var package in searchResult.Data)
                    {
                        packages.Add(new PackageSearchResult
                        {
                            Id = package.Id ?? "",
                            Version = package.Version ?? "",
                            Description = package.Description ?? "",
                            Authors = package.Authors?.ToList() ?? new List<string>(),
                            Tags = package.Tags?.ToList() ?? new List<string>(),
                            TotalDownloads = package.TotalDownloads ?? 0,
                            ProjectUrl = package.ProjectUrl
                        });
                    }
                }

                return packages;
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Package search failed: {ex.Message}");
                return new List<PackageSearchResult>();
            }
        }
    }

    public class PackageSearchResult
    {
        public string Id { get; set; } = "";
        public string Version { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Authors { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public long TotalDownloads { get; set; }
        public string? ProjectUrl { get; set; }
    }

    public class NuGetSearchResponse
    {
        public int TotalHits { get; set; }
        public List<NuGetPackage>? Data { get; set; }
    }

    public class NuGetPackage
    {
        public string? Id { get; set; }
        public string? Version { get; set; }
        public string? Description { get; set; }
        public string[]? Authors { get; set; }
        public string[]? Tags { get; set; }
        public long? TotalDownloads { get; set; }
        public string? ProjectUrl { get; set; }
    }
}
