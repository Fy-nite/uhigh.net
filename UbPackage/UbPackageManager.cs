using System.IO.Compression;
using System.Text;
using uhigh.Net.Diagnostics;
using System.Formats.Tar; // Add this for TAR support

namespace uhigh.Net.UbPackage
{
    /// <summary>
    /// Manages .ub package operations
    /// </summary>
    public class UbPackageManager
    {
        private readonly DiagnosticsReporter? _diagnostics;
        
        /// <summary>
        /// Package manifest file name within .ub archives
        /// </summary>
        public const string ManifestFileName = "package.json";

        public UbPackageManager(DiagnosticsReporter? diagnostics = null)
        {
            _diagnostics = diagnostics;
        }

        /// <summary>
        /// Creates a .ub package from a μHigh project using tar.gz compression
        /// </summary>
        /// <param name="projectPath">Path to the .uhighproj file</param>
        /// <param name="outputPath">Path where to create the .ub file</param>
        /// <returns>True if successful</returns>
        public async Task<bool> PackAsync(string projectPath, string outputPath)
        {
            try
            {
                _diagnostics?.ReportInfo($"Packing project: {projectPath}");

                // Load the project
                var project = await ProjectFile.LoadAsync(projectPath, _diagnostics);
                if (project == null)
                {
                    _diagnostics?.ReportError("Failed to load project file");
                    return false;
                }

                // Create manifest from project
                var manifest = PackageManifest.FromProject(project);
                
                // Validate manifest
                if (!manifest.IsValid())
                {
                    _diagnostics?.ReportError("Invalid package manifest");
                    return false;
                }

                var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? "";
                
                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // Create tar archive in memory
                using var tarStream = new MemoryStream();
                using (var tarWriter = new TarWriter(tarStream, leaveOpen: true))
                {
                    // Add manifest
                    var manifestBytes = Encoding.UTF8.GetBytes(manifest.ToJson());
                    using (var manifestStream = new MemoryStream(manifestBytes))
                    {
                        var manifestEntry = new PaxTarEntry(TarEntryType.RegularFile, ManifestFileName)
                        {
                            DataStream = manifestStream,
                            ModificationTime = DateTimeOffset.Now
                        };
                        tarWriter.WriteEntry(manifestEntry);
                    }

                    // Add source files
                    foreach (var sourceFile in manifest.SourceFiles)
                    {
                        var fullSourcePath = Path.IsPathRooted(sourceFile) 
                            ? sourceFile 
                            : Path.Combine(projectDir, sourceFile);

                        if (!File.Exists(fullSourcePath))
                        {
                            _diagnostics?.ReportWarning($"Source file not found: {fullSourcePath}");
                            continue;
                        }

                        // Use relative path in archive
                        var archivePath = sourceFile.Replace('\\', '/');
                        tarWriter.WriteEntry(archivePath, fullSourcePath);

                        _diagnostics?.ReportInfo($"Added source file: {archivePath}");
                    }
                }

                // Compress tar to gzip
                tarStream.Position = 0;
                using var outStream = File.Create(outputPath);
                using var gzipStream = new GZipStream(outStream, CompressionLevel.Optimal);
                await tarStream.CopyToAsync(gzipStream);

                _diagnostics?.ReportInfo($"Package created successfully: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportError($"Failed to create package: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extracts a .ub package (tar.gz) to a directory
        /// </summary>
        /// <param name="packagePath">Path to the .ub file</param>
        /// <param name="extractPath">Directory to extract to</param>
        /// <returns>True if successful</returns>
        public async Task<bool> UnpackAsync(string packagePath, string extractPath)
        {
            try
            {
                _diagnostics?.ReportInfo($"Unpacking package: {packagePath}");

                if (!File.Exists(packagePath))
                {
                    _diagnostics?.ReportError($"Package file not found: {packagePath}");
                    return false;
                }

                if (!Directory.Exists(extractPath))
                {
                    Directory.CreateDirectory(extractPath);
                }

                using var fileStream = File.OpenRead(packagePath);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var tarReader = new TarReader(gzipStream);

                TarEntry entry;
                while ((entry = tarReader.GetNextEntry()) != null)
                {
                    var destPath = Path.Combine(extractPath, entry.Name);
                    var destDir = Path.GetDirectoryName(destPath);

                    if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    using var entryStream = entry.DataStream;
                    using var destStream = File.Create(destPath);
                    await entryStream.CopyToAsync(destStream);

                    _diagnostics?.ReportInfo($"Extracted: {entry.Name}");
                }

                _diagnostics?.ReportInfo($"Package extracted successfully to: {extractPath}");
                return true;
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportError($"Failed to extract package: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Reads the manifest from a .ub package (tar.gz)
        /// </summary>
        /// <param name="packagePath">Path to the .ub file</param>
        /// <returns>Package manifest or null if not found/invalid</returns>
        public async Task<PackageManifest?> ReadManifestAsync(string packagePath)
        {
            try
            {
                if (!File.Exists(packagePath))
                {
                    return null;
                }

                using var fileStream = File.OpenRead(packagePath);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var tarReader = new TarReader(gzipStream);

                TarEntry entry;
                while ((entry = tarReader.GetNextEntry()) != null)
                {
                    if (entry.Name == ManifestFileName)
                    {
                        using var manifestStream = entry.DataStream;
                        using var reader = new StreamReader(manifestStream, Encoding.UTF8);
                        var manifestJson = await reader.ReadToEndAsync();
                        return PackageManifest.FromJson(manifestJson);
                    }
                }

                _diagnostics?.ReportError($"Package manifest not found in: {packagePath}");
                return null;
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportError($"Failed to read manifest from {packagePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Installs a .ub package as a dependency in a project
        /// </summary>
        /// <param name="packagePath">Path to the .ub file</param>
        /// <param name="projectPath">Path to the target project</param>
        /// <param name="packageCachePath">Directory to store installed packages</param>
        /// <returns>True if successful</returns>
        public async Task<bool> InstallAsync(string packagePath, string projectPath, string? packageCachePath = null)
        {
            try
            {
                _diagnostics?.ReportInfo($"Installing package {packagePath} to project {projectPath}");

                // Read package manifest
                var manifest = await ReadManifestAsync(packagePath);
                if (manifest == null)
                {
                    _diagnostics?.ReportError("Failed to read package manifest");
                    return false;
                }

                // Load target project
                var project = await ProjectFile.LoadAsync(projectPath, _diagnostics);
                if (project == null)
                {
                    _diagnostics?.ReportError("Failed to load target project");
                    return false;
                }

                // Set up package cache directory
                var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? "";
                var cacheDir = packageCachePath ?? Path.Combine(projectDir, ".ub-packages");
                
                if (!Directory.Exists(cacheDir))
                {
                    Directory.CreateDirectory(cacheDir);
                }

                // Create package-specific directory
                var packageDir = Path.Combine(cacheDir, $"{manifest.Name}-{manifest.Version}");
                
                // Extract package to cache
                if (!await UnpackAsync(packagePath, packageDir))
                {
                    return false;
                }

                // Add .ub package reference to project (store as property for now)
                var packageRef = $"{manifest.Name}:{manifest.Version}:{packageDir}";
                var existingProp = project.Properties.FirstOrDefault(p => p.Name == "UbPackageReferences");
                
                if (existingProp != null)
                {
                    // Append to existing references
                    var refs = existingProp.Value.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                    if (!refs.Any(r => r.StartsWith($"{manifest.Name}:")))
                    {
                        refs.Add(packageRef);
                        existingProp.Value = string.Join(";", refs);
                    }
                }
                else
                {
                    // Create new property
                    project.Properties.Add(new ProjectProperty
                    {
                        Name = "UbPackageReferences",
                        Value = packageRef,
                        Category = "Package"
                    });
                }

                // Save updated project
                if (!await ProjectFile.SaveAsync(project, projectPath, _diagnostics))
                {
                    return false;
                }

                _diagnostics?.ReportInfo($"Package {manifest.Name} v{manifest.Version} installed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportError($"Failed to install package: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lists all .ub packages installed in a project
        /// </summary>
        /// <param name="projectPath">Path to the project file</param>
        /// <returns>List of installed package information</returns>
        public async Task<List<(string Name, string Version, string Path)>> ListInstalledAsync(string projectPath)
        {
            var result = new List<(string Name, string Version, string Path)>();

            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, _diagnostics);
                if (project == null)
                {
                    return result;
                }

                var packageRefsProp = project.Properties.FirstOrDefault(p => p.Name == "UbPackageReferences");
                if (packageRefsProp == null)
                {
                    return result;
                }

                var refs = packageRefsProp.Value.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var refString in refs)
                {
                    var parts = refString.Split(':');
                    if (parts.Length == 3)
                    {
                        result.Add((parts[0], parts[1], parts[2]));
                    }
                }
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportError($"Failed to list installed packages: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Builds a project from a .ub package
        /// </summary>
        /// <param name="packagePath">Path to the .ub file</param>
        /// <param name="outputPath">Output path for the built executable</param>
        /// <returns>True if successful</returns>
        public async Task<bool> BuildFromPackageAsync(string packagePath, string? outputPath = null)
        {
            try
            {
                _diagnostics?.ReportInfo($"Building from package: {packagePath}");

                // Create temporary directory for extraction
                var tempDir = Path.Combine(Path.GetTempPath(), $"ub-build-{Guid.NewGuid()}");
                
                try
                {
                    // Extract package
                    if (!await UnpackAsync(packagePath, tempDir))
                    {
                        return false;
                    }

                    // Read manifest to create project
                    var manifestPath = Path.Combine(tempDir, ManifestFileName);
                    if (!File.Exists(manifestPath))
                    {
                        _diagnostics?.ReportError("Package manifest not found");
                        return false;
                    }

                    var manifestJson = await File.ReadAllTextAsync(manifestPath);
                    var manifest = PackageManifest.FromJson(manifestJson);
                    if (manifest == null)
                    {
                        _diagnostics?.ReportError("Invalid package manifest");
                        return false;
                    }

                    // Create temporary project file
                    var project = manifest.ToProject();
                    var tempProjectPath = Path.Combine(tempDir, $"{manifest.Name}.uhighproj");
                    
                    if (!await ProjectFile.SaveAsync(project, tempProjectPath, _diagnostics))
                    {
                        return false;
                    }

                    // Build the project
                    var compiler = new Compiler(true, null); // verbose mode
                    
                    // If no output path specified, place output in same directory as the .ub file
                    var finalOutputPath = outputPath;
                    if (string.IsNullOrEmpty(finalOutputPath))
                    {
                        var packageDir = Path.GetDirectoryName(Path.GetFullPath(packagePath)) ?? "";
                        var extension = manifest.OutputType.Equals("Library", StringComparison.OrdinalIgnoreCase) ? ".dll" : ".exe";
                        finalOutputPath = Path.Combine(packageDir, manifest.Name + extension);
                    }
                    
                    var success = await compiler.CompileProject(tempProjectPath, finalOutputPath);
                    
                    // If we defaulted the output path and compilation succeeded, copy from build directory to intended location
                    if (success && string.IsNullOrEmpty(outputPath))
                    {
                        var buildDir = Path.Combine(Path.GetDirectoryName(finalOutputPath)!, "build");
                        var extension = manifest.OutputType.Equals("Library", StringComparison.OrdinalIgnoreCase) ? ".dll" : ".exe";
                        var sourceFile = Path.Combine(buildDir, manifest.Name + extension);
                        
                        if (File.Exists(sourceFile) && !string.IsNullOrEmpty(finalOutputPath))
                        {
                            File.Copy(sourceFile, finalOutputPath, overwrite: true);
                            
                            // Also copy the runtime config file for executables
                            if (manifest.OutputType.Equals("Exe", StringComparison.OrdinalIgnoreCase))
                            {
                                var sourceRuntimeConfig = Path.Combine(buildDir, manifest.Name + ".runtimeconfig.json");
                                var targetRuntimeConfig = Path.ChangeExtension(finalOutputPath, ".runtimeconfig.json");
                                if (File.Exists(sourceRuntimeConfig))
                                {
                                    File.Copy(sourceRuntimeConfig, targetRuntimeConfig, overwrite: true);
                                }
                            }
                            
                            Console.WriteLine($"Output copied to: {finalOutputPath}");
                        }
                    }

                    return success;
                }
                finally
                {
                    // Clean up temporary directory
                    if (Directory.Exists(tempDir))
                    {
                        Directory.Delete(tempDir, true);
                    }
                }
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportError($"Failed to build from package: {ex.Message}");
                return false;
            }
        }
    }
}