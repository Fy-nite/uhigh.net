using uhigh.Net.Diagnostics;
using uhigh.Net.Parser;

namespace uhigh.Net.ProjectSystem
{
    public class ProjectManager
    {
        private readonly bool _verboseMode;
        private readonly string? _stdLibPath;

        public ProjectManager(bool verboseMode = false, string? stdLibPath = null)
        {
            _verboseMode = verboseMode;
            _stdLibPath = stdLibPath;
        }

        public async Task<bool> CreateProject(string projectName, string? projectDir = null, string? description = null, string? author = null, string outputType = "Exe", string target = "net9.0")
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode);

            try
            {
                projectDir ??= Environment.CurrentDirectory;
                var fullProjectDir = Path.Combine(projectDir, projectName);

                var project = new uhighProject
                {
                    Name = projectName,
                    Version = "1.0.0",
                    Description = description,
                    Author = author,
                    Target = target,
                    OutputType = outputType,
                    SourceFiles = new List<string> { "main.uh" },
                    RootNamespace = projectName,
                    Nullable = true
                };

                var success = await ProjectFile.CreateAsync(projectName, fullProjectDir, diagnostics);

                if (success)
                {
                    Console.WriteLine($"Created project '{projectName}' in '{fullProjectDir}'");
                    Console.WriteLine($"Project file: {Path.Combine(fullProjectDir, $"{projectName}.uhighproj")}");
                    Console.WriteLine($"Main file: {Path.Combine(fullProjectDir, "main.uh")}");
                    Console.WriteLine($"Output type: {outputType}");
                }

                diagnostics.PrintSummary();
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to create project: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        public async Task<bool> ListProjectInfo(string projectPath)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);

            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                Console.WriteLine($"Project: {project.Name}");
                Console.WriteLine($"Version: {project.Version}");
                Console.WriteLine($"Target: {project.Target}");
                Console.WriteLine($"Output Type: {project.OutputType}");

                if (!string.IsNullOrEmpty(project.Description))
                    Console.WriteLine($"Description: {project.Description}");

                if (!string.IsNullOrEmpty(project.Author))
                    Console.WriteLine($"Author: {project.Author}");

                if (!string.IsNullOrEmpty(project.RootNamespace))
                    Console.WriteLine($"Root Namespace: {project.RootNamespace}");

                Console.WriteLine($"Nullable: {project.Nullable}");

                Console.WriteLine("\nSource Files:");
                foreach (var file in project.SourceFiles)
                {
                    var exists = File.Exists(file) ? "✓" : "✗";
                    Console.WriteLine($"  {exists} {file}");
                }

                if (project.Dependencies.Count > 0)
                {
                    Console.WriteLine("\nDependencies:");
                    foreach (var dep in project.Dependencies)
                    {
                        Console.WriteLine($"  {dep.Name} v{dep.Version}");
                    }
                }

                if (project.Properties.Count > 0)
                {
                    Console.WriteLine("\nProperties:");
                    foreach (var prop in project.Properties)
                    {
                        Console.WriteLine($"  {prop.Name} = {prop.Value}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to read project info: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        public async Task<bool> AddSourceFileToProject(string projectPath, string sourceFile, bool createFile = false)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);

            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Make path relative to project directory
                var projectDir = Path.GetDirectoryName(projectPath) ?? "";
                var relativePath = Path.GetRelativePath(projectDir, sourceFile);

                if (project.SourceFiles.Contains(relativePath))
                {
                    Console.WriteLine($"Source file '{relativePath}' is already in the project");
                    return true;
                }

                // Create file if requested and it doesn't exist
                if (createFile && !File.Exists(sourceFile))
                {
                    var fileName = Path.GetFileNameWithoutExtension(sourceFile);
                    var defaultContent = $@"// {fileName}.uhigh - Generated by uhigh.Net

// Add your uhigh.Net code here
";
                    await File.WriteAllTextAsync(sourceFile, defaultContent);
                    Console.WriteLine($"Created source file: {sourceFile}");
                }

                project.SourceFiles.Add(relativePath);
                var success = await ProjectFile.SaveAsync(project, projectPath, diagnostics);

                if (success)
                {
                    Console.WriteLine($"Added source file '{relativePath}' to project");
                }

                diagnostics.PrintSummary();
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to add source file: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }

        public async Task<bool> AddPackageToProject(string projectPath, string packageName, string version)
        {
            var diagnostics = new DiagnosticsReporter(_verboseMode, projectPath);

            try
            {
                var project = await ProjectFile.LoadAsync(projectPath, diagnostics);
                if (project == null)
                {
                    diagnostics.PrintSummary();
                    return false;
                }

                // Check if package already exists
                var existingPackage = project.Dependencies.FirstOrDefault(d => d.Name == packageName);
                if (existingPackage != null)
                {
                    Console.WriteLine($"Package '{packageName}' already exists with version {existingPackage.Version}");
                    Console.WriteLine($"Updating to version {version}");
                    existingPackage.Version = version;
                }
                else
                {
                    project.Dependencies.Add(new PackageReference
                    {
                        Name = packageName,
                        Version = version
                    });
                }

                var success = await ProjectFile.SaveAsync(project, projectPath, diagnostics);

                if (success)
                {
                    Console.WriteLine($"Added package '{packageName}' v{version} to project");
                }

                diagnostics.PrintSummary();
                return success;
            }
            catch (Exception ex)
            {
                diagnostics.ReportFatal($"Failed to add package: {ex.Message}", exception: ex);
                diagnostics.PrintSummary();
                return false;
            }
        }
    }
}
