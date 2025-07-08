using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;
using uhigh.Net.Diagnostics;

namespace uhigh.Net
{
    /// <summary>
    /// The project file class
    /// </summary>
    public class ProjectFile
    {
        /// <summary>
        /// The camel case
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// The uhigh project
        /// </summary>
        private static readonly XmlSerializer Serializer = new(typeof(uhighProject));

        /// <summary>
        /// Loads the project path
        /// </summary>
        /// <param name="projectPath">The project path</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <returns>A task containing the uhigh project</returns>
        public static async Task<uhighProject?> LoadAsync(string projectPath, DiagnosticsReporter? diagnostics = null)
        {
            try
            {
                if (!File.Exists(projectPath))
                {
                    diagnostics?.ReportError($"Project file not found: {projectPath}");
                    return null;
                }

                // Check if this is actually a project file by extension
                if (!projectPath.EndsWith(".uhighproj", StringComparison.OrdinalIgnoreCase))
                {
                    diagnostics?.ReportError($"Invalid project file extension. Expected .uhighproj, got: {Path.GetExtension(projectPath)}");
                    return null;
                }

                using var stream = new FileStream(projectPath, FileMode.Open, FileAccess.Read);
                var project = (uhighProject?)Serializer.Deserialize(stream);
                
                if (project == null)
                {
                    diagnostics?.ReportError($"Failed to parse project file: {projectPath}");
                    return null;
                }

                // Resolve relative paths relative to the project directory
                var projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? "";
                diagnostics?.ReportInfo($"Project directory: {projectDir}");
                
                // Keep source files as relative paths for now - they'll be resolved during compilation
                // This allows the project file to remain portable
                for (int i = 0; i < project.SourceFiles.Count; i++)
                {
                    var relativePath = project.SourceFiles[i];
                    var fullPath = Path.IsPathRooted(relativePath) 
                        ? relativePath 
                        : Path.Combine(projectDir, relativePath);
                    
                    var exists = File.Exists(fullPath);
                    diagnostics?.ReportInfo($"Source file: {relativePath} (exists: {exists})");
                    
                    if (!exists)
                    {
                        diagnostics?.ReportWarning($"Source file not found: {fullPath}");
                    }
                }

                diagnostics?.ReportInfo($"Loaded project: {project.Name} v{project.Version} with {project.SourceFiles.Count} source files");
                
                return project;
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to load project file {projectPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Saves the project
        /// </summary>
        /// <param name="project">The project</param>
        /// <param name="projectPath">The project path</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <returns>A task containing the bool</returns>
        public static async Task<bool> SaveAsync(uhighProject project, string projectPath, DiagnosticsReporter? diagnostics = null)
        {
            try
            {
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\n",
                    Async = true
                };

                using var writer = XmlWriter.Create(projectPath, settings);
                Serializer.Serialize(writer, project);
                await writer.FlushAsync();
                
                diagnostics?.ReportInfo($"Saved project file: {projectPath}");
                return true;
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to save project file {projectPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates the project name
        /// </summary>
        /// <param name="projectName">The project name</param>
        /// <param name="projectDir">The project dir</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <returns>A task containing the bool</returns>
        public static async Task<bool> CreateAsync(string projectName, string projectDir, DiagnosticsReporter? diagnostics = null)
        {
            try
            {
                var project = uhighProject.CreateDefault(projectName);
                var projectPath = Path.Combine(projectDir, $"{projectName}.uhighproj");

                // Create directory if it doesn't exist
                if (!Directory.Exists(projectDir))
                {
                    Directory.CreateDirectory(projectDir);
                }

                // Create a default main.uh file
                var mainuhighPath = Path.Combine(projectDir, "main.uh");
                if (!File.Exists(mainuhighPath))
                {
                    var defaultCode = $@"// {projectName} - Generated by the μHigh.Net project file creator
using System
using StdLib
namespace {projectName}
{{
    public class Program
    {{
        /// <summary>
        /// Main entry point for the μHigh program
        /// </summary>
        /// <param name=""args"">Command line arguments</param>
        /// <returns>void</returns>
    
        public static func Main(args: string[]) : void
        {{
            IO.Print(""Hello, μHigh! from {projectName}."");
        }}
    }}
}}
";
                    await File.WriteAllTextAsync(mainuhighPath, defaultCode);
                }

                return await SaveAsync(project, projectPath, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to create project: {ex.Message}");
                return false;
            }
        }
    }
}
