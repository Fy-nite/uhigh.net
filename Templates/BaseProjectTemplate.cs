using uhigh.Net.Diagnostics;
using System.Text.Json;

namespace uhigh.Net.Templates
{
    /// <summary>
    /// Base class for project templates providing common functionality
    /// </summary>
    public abstract class BaseProjectTemplate : IProjectTemplate
    {
        /// <summary>
        /// Gets the name of the template
        /// </summary>
        public abstract string Name { get; }
        
        /// <summary>
        /// Gets the description of the template
        /// </summary>
        public abstract string Description { get; }
        
        /// <summary>
        /// Gets the output type this template creates (Exe, Library, etc.)
        /// </summary>
        public abstract string OutputType { get; }
        
        /// <summary>
        /// Gets the version of the template
        /// </summary>
        public virtual string Version => "1.0.0";
        
        /// <summary>
        /// Gets the author of the template
        /// </summary>
        public virtual string Author => "μHigh Template";
        
        /// <summary>
        /// Creates a new project using this template
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <param name="projectPath">Path where the project should be created</param>
        /// <param name="parameters">Additional parameters for the template</param>
        /// <param name="diagnostics">Diagnostics reporter for errors/warnings</param>
        /// <returns>True if the project was created successfully</returns>
        public virtual async Task<bool> CreateProjectAsync(string projectName, string projectPath, Dictionary<string, object>? parameters = null, DiagnosticsReporter? diagnostics = null)
        {
            try
            {
                // Validate parameters
                if (!ValidateParameters(parameters))
                {
                    diagnostics?.ReportError("Invalid template parameters");
                    return false;
                }
                
                // Create project directory
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                    diagnostics?.ReportInfo($"Created project directory: {projectPath}");
                }
                
                // Create project file
                var success = await CreateProjectFileAsync(projectName, projectPath, parameters, diagnostics);
                if (!success)
                {
                    return false;
                }
                
                // Create source files
                success = await CreateSourceFilesAsync(projectName, projectPath, parameters, diagnostics);
                if (!success)
                {
                    return false;
                }
                
                // Create additional files (README, etc.)
                await CreateAdditionalFilesAsync(projectName, projectPath, parameters, diagnostics);
                
                diagnostics?.ReportInfo($"Project '{projectName}' created successfully using template '{Name}'");
                return true;
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to create project using template '{Name}': {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Gets the parameters that this template accepts
        /// </summary>
        /// <returns>Dictionary of parameter names and their descriptions</returns>
        public virtual Dictionary<string, string> GetParameters()
        {
            return new Dictionary<string, string>
            {
                { "description", "Project description" },
                { "author", "Project author" },
                { "targetFramework", "Target framework (default: net8.0)" }
            };
        }
        
        /// <summary>
        /// Validates whether the provided parameters are valid for this template
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>True if parameters are valid</returns>
        public virtual bool ValidateParameters(Dictionary<string, object>? parameters)
        {
            // Base validation - can be overridden by derived classes
            return true;
        }
        
        /// <summary>
        /// Creates the project file for the template
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <param name="projectPath">Path where the project should be created</param>
        /// <param name="parameters">Additional parameters for the template</param>
        /// <param name="diagnostics">Diagnostics reporter for errors/warnings</param>
        /// <returns>True if successful</returns>
        protected abstract Task<bool> CreateProjectFileAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics);
        
        /// <summary>
        /// Creates the source files for the template
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <param name="projectPath">Path where the project should be created</param>
        /// <param name="parameters">Additional parameters for the template</param>
        /// <param name="diagnostics">Diagnostics reporter for errors/warnings</param>
        /// <returns>True if successful</returns>
        protected abstract Task<bool> CreateSourceFilesAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics);
        
        /// <summary>
        /// Creates additional files for the template (README, etc.)
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <param name="projectPath">Path where the project should be created</param>
        /// <param name="parameters">Additional parameters for the template</param>
        /// <param name="diagnostics">Diagnostics reporter for errors/warnings</param>
        /// <returns>Task</returns>
        protected virtual Task CreateAdditionalFilesAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics)
        {
            // Default implementation does nothing
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Gets a parameter value with a default fallback
        /// </summary>
        /// <param name="parameters">Parameters dictionary</param>
        /// <param name="key">Parameter key</param>
        /// <param name="defaultValue">Default value if not found</param>
        /// <returns>Parameter value or default</returns>
        protected T GetParameter<T>(Dictionary<string, object>? parameters, string key, T defaultValue)
        {
            if (parameters?.TryGetValue(key, out var value) == true && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
    }
}