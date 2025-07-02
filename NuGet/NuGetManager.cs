using System.Diagnostics;
using System.Text.Json;
using uhigh.Net.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace uhigh.Net.NuGet
{
    /// <summary>
    /// The nu get manager class
    /// </summary>
    public class NuGetManager
    {
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly DiagnosticsReporter _diagnostics;
        /// <summary>
        /// The global packages path
        /// </summary>
        private readonly string _globalPackagesPath;
        /// <summary>
        /// The default sources
        /// </summary>
        private readonly string[] _defaultSources = {
            "https://api.nuget.org/v3-flatcontainer/",
            "https://api.nuget.org/v3/index.json"
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="NuGetManager"/> class
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        public NuGetManager(DiagnosticsReporter? diagnostics = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            
            // Use NuGet's global packages folder
            _globalPackagesPath = Environment.GetEnvironmentVariable("NUGET_PACKAGES") 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        }

        /// <summary>
        /// Restores the packages using the specified project
        /// </summary>
        /// <param name="project">The project</param>
        /// <param name="projectDir">The project dir</param>
        /// <param name="force">The force</param>
        /// <returns>A task containing the bool</returns>
        public async Task<bool> RestorePackagesAsync(uhighProject project, string projectDir, bool force = false)
        {
            try
            {
                _diagnostics.ReportInfo($"Restoring NuGet packages for {project.Name}");
                
                if (project.Dependencies.Count == 0)
                {
                    _diagnostics.ReportInfo("No packages to restore");
                    return true;
                }

                var success = true;
                foreach (var package in project.Dependencies)
                {
                    var packageResult = await RestorePackageAsync(package, projectDir, force);
                    if (!packageResult)
                    {
                        success = false;
                        _diagnostics.ReportError($"Failed to restore package: {package.Name} v{package.Version}");
                    }
                    else
                    {
                        _diagnostics.ReportInfo($"Restored: {package.Name} v{package.Version}");
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

        /// <summary>
        /// Restores the package using the specified package
        /// </summary>
        /// <param name="package">The package</param>
        /// <param name="projectDir">The project dir</param>
        /// <param name="force">The force</param>
        /// <returns>A task containing the bool</returns>
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

                // Download package using dotnet restore if available, otherwise use direct download
                if (await TryDotNetRestoreAsync(package, projectDir))
                {
                    return true;
                }

                // Fallback to direct download
                return await DownloadPackageDirectlyAsync(package);
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Failed to restore {package.Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tries the dot net restore using the specified package
        /// </summary>
        /// <param name="package">The package</param>
        /// <param name="projectDir">The project dir</param>
        /// <returns>A task containing the bool</returns>
        private async Task<bool> TryDotNetRestoreAsync(PackageReference package, string projectDir)
        {
            try
            {
                // Create a temporary project file to restore the package
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

                // Clean up
                try { File.Delete(tempProjPath); } catch { }

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Downloads the package directly using the specified package
        /// </summary>
        /// <param name="package">The package</param>
        /// <returns>A task containing the bool</returns>
        private async Task<bool> DownloadPackageDirectlyAsync(PackageReference package)
        {
            try
            {
                var packageName = package.Name.ToLowerInvariant();
                var packageVersion = package.Version.ToLowerInvariant();
                
                using var httpClient = new HttpClient();
                
                // Try to download from NuGet API
                var downloadUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName}/{packageVersion}/{packageName}.{packageVersion}.nupkg";
                
                _diagnostics.ReportInfo($"Downloading {package.Name} from {downloadUrl}");
                
                var response = await httpClient.GetAsync(downloadUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _diagnostics.ReportError($"Failed to download {package.Name}: HTTP {response.StatusCode}");
                    return false;
                }

                var packageBytes = await response.Content.ReadAsByteArrayAsync();
                
                // Extract package to global packages folder
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

        /// <summary>
        /// Gets the package assemblies using the specified package
        /// </summary>
        /// <param name="package">The package</param>
        /// <param name="targetFramework">The target framework</param>
        /// <returns>The assemblies</returns>
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

                // Look for assemblies in lib folder with target framework compatibility
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
                        
                        if (assemblies.Count > 0) break; // Use the best matching framework
                    }
                }

                // Also check ref folder for reference assemblies
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

                _diagnostics.ReportInfo($"Found {assemblies.Count} assemblies for {package.Name}");
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Failed to get assemblies for {package.Name}: {ex.Message}");
            }

            return assemblies;
        }

        /// <summary>
        /// Ises the compatible framework using the specified package framework
        /// </summary>
        /// <param name="packageFramework">The package framework</param>
        /// <param name="targetFramework">The target framework</param>
        /// <returns>The bool</returns>
        private bool IsCompatibleFramework(string packageFramework, string targetFramework)
        {
            // Simplified framework compatibility check
            // In a real implementation, you'd want more sophisticated logic
            
            var targetParts = ParseFramework(targetFramework);
            var packageParts = ParseFramework(packageFramework);
            
            if (targetParts.name != packageParts.name) return false;
            
            return packageParts.version <= targetParts.version;
        }

        /// <summary>
        /// Parses the framework using the specified framework
        /// </summary>
        /// <param name="framework">The framework</param>
        /// <returns>The string name version version</returns>
        private (string name, Version version) ParseFramework(string framework)
        {
            // Parse frameworks like "net8.0", "netstandard2.0", "net48", etc.
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

        /// <summary>
        /// Gets the framework priority using the specified framework
        /// </summary>
        /// <param name="framework">The framework</param>
        /// <returns>The int</returns>
        private int GetFrameworkPriority(string framework)
        {
            // Higher number = higher priority
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

        /// <summary>
        /// Searches the packages using the specified search term
        /// </summary>
        /// <param name="searchTerm">The search term</param>
        /// <param name="take">The take</param>
        /// <returns>A task containing a list of package search result</returns>
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

    /// <summary>
    /// The package search result class
    /// </summary>
    public class PackageSearchResult
    {
        /// <summary>
        /// Gets or sets the value of the id
        /// </summary>
        public string Id { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the version
        /// </summary>
        public string Version { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the description
        /// </summary>
        public string Description { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the authors
        /// </summary>
        public List<string> Authors { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the tags
        /// </summary>
        public List<string> Tags { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the total downloads
        /// </summary>
        public long TotalDownloads { get; set; }
        /// <summary>
        /// Gets or sets the value of the project url
        /// </summary>
        public string? ProjectUrl { get; set; }
    }

    // JSON response models for NuGet API
    /// <summary>
    /// The nu get search response class
    /// </summary>
    public class NuGetSearchResponse
    {
        /// <summary>
        /// Gets or sets the value of the total hits
        /// </summary>
        public int TotalHits { get; set; }
        /// <summary>
        /// Gets or sets the value of the data
        /// </summary>
        public List<NuGetPackage>? Data { get; set; }
    }

    /// <summary>
    /// The nu get package class
    /// </summary>
    public class NuGetPackage
    {
        /// <summary>
        /// Gets or sets the value of the id
        /// </summary>
        public string? Id { get; set; }
        /// <summary>
        /// Gets or sets the value of the version
        /// </summary>
        public string? Version { get; set; }
        /// <summary>
        /// Gets or sets the value of the description
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Gets or sets the value of the authors
        /// </summary>
        public string[]? Authors { get; set; }
        /// <summary>
        /// Gets or sets the value of the tags
        /// </summary>
        public string[]? Tags { get; set; }
        /// <summary>
        /// Gets or sets the value of the total downloads
        /// </summary>
        public long? TotalDownloads { get; set; }
        /// <summary>
        /// Gets or sets the value of the project url
        /// </summary>
        public string? ProjectUrl { get; set; }
    }
}
