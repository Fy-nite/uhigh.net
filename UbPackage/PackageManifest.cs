using System.Text.Json;
using System.Text.Json.Serialization;

namespace uhigh.Net.UbPackage
{
    /// <summary>
    /// Represents the manifest file for a .ub package
    /// </summary>
    public class PackageManifest
    {
        /// <summary>
        /// The package format version for compatibility
        /// </summary>
        [JsonPropertyName("packageFormatVersion")]
        public string PackageFormatVersion { get; set; } = "1.0";

        /// <summary>
        /// The name of the package
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// The version of the package
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Package description
        /// </summary>
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Package author
        /// </summary>
        [JsonPropertyName("author")]
        public string? Author { get; set; }

        /// <summary>
        /// Target framework
        /// </summary>
        [JsonPropertyName("targetFramework")]
        public string TargetFramework { get; set; } = "net8.0";

        /// <summary>
        /// Output type (Exe, Library)
        /// </summary>
        [JsonPropertyName("outputType")]
        public string OutputType { get; set; } = "Library";

        /// <summary>
        /// Root namespace for the package
        /// </summary>
        [JsonPropertyName("rootNamespace")]
        public string? RootNamespace { get; set; }

        /// <summary>
        /// List of source files included in the package
        /// </summary>
        [JsonPropertyName("sourceFiles")]
        public List<string> SourceFiles { get; set; } = new();

        /// <summary>
        /// Dependencies on other .ub packages
        /// </summary>
        [JsonPropertyName("ubDependencies")]
        public Dictionary<string, string> UbDependencies { get; set; } = new();

        /// <summary>
        /// Dependencies on NuGet packages
        /// </summary>
        [JsonPropertyName("nugetDependencies")]
        public Dictionary<string, string> NugetDependencies { get; set; } = new();

        /// <summary>
        /// Package creation timestamp
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Keywords for package discovery
        /// </summary>
        [JsonPropertyName("keywords")]
        public List<string> Keywords { get; set; } = new();

        /// <summary>
        /// Minimum μHigh version required
        /// </summary>
        [JsonPropertyName("uhighVersion")]
        public string? UhighVersion { get; set; }

        /// <summary>
        /// Creates a manifest from a μHigh project
        /// </summary>
        /// <param name="project">The project to create manifest from</param>
        /// <returns>Package manifest</returns>
        public static PackageManifest FromProject(uhighProject project)
        {
            var manifest = new PackageManifest
            {
                Name = project.Name,
                Version = project.Version,
                Description = project.Description,
                Author = project.Author,
                TargetFramework = project.Target,
                OutputType = project.OutputType,
                RootNamespace = project.RootNamespace,
                SourceFiles = new List<string>(project.SourceFiles)
            };

            // Convert project dependencies to NuGet dependencies
            foreach (var dep in project.Dependencies)
            {
                manifest.NugetDependencies[dep.Name] = dep.Version;
            }

            return manifest;
        }

        /// <summary>
        /// Converts this manifest to a μHigh project
        /// </summary>
        /// <returns>μHigh project</returns>
        public uhighProject ToProject()
        {
            var project = new uhighProject
            {
                Name = Name,
                Version = Version,
                Description = Description,
                Author = Author,
                Target = TargetFramework,
                OutputType = OutputType,
                RootNamespace = RootNamespace,
                SourceFiles = new List<string>(SourceFiles)
            };

            // Convert NuGet dependencies to project dependencies
            foreach (var dep in NugetDependencies)
            {
                project.Dependencies.Add(new PackageReference
                {
                    Name = dep.Key,
                    Version = dep.Value
                });
            }

            return project;
        }

        /// <summary>
        /// Serializes the manifest to JSON
        /// </summary>
        /// <returns>JSON string</returns>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Serialize(this, options);
        }

        /// <summary>
        /// Deserializes a manifest from JSON
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Package manifest or null if invalid</returns>
        public static PackageManifest? FromJson(string json)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                return JsonSerializer.Deserialize<PackageManifest>(json, options);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validates the manifest for required fields
        /// </summary>
        /// <returns>True if valid</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Name) &&
                   !string.IsNullOrWhiteSpace(Version) &&
                   SourceFiles.Count > 0;
        }
    }
}