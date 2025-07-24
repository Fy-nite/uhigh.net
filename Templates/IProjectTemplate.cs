using uhigh.Net.Diagnostics;

namespace uhigh.Net.Templates
{
    /// <summary>
    /// Interface for project templates that can be used to create new projects
    /// </summary>
    public interface IProjectTemplate
    {
        /// <summary>
        /// Gets the name of the template
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Gets the description of the template
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Gets the output type this template creates (Exe, Library, etc.)
        /// </summary>
        string OutputType { get; }
        
        /// <summary>
        /// Gets the version of the template
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Gets the author of the template
        /// </summary>
        string Author { get; }
        
        /// <summary>
        /// Creates a new project using this template
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <param name="projectPath">Path where the project should be created</param>
        /// <param name="parameters">Additional parameters for the template</param>
        /// <param name="diagnostics">Diagnostics reporter for errors/warnings</param>
        /// <returns>True if the project was created successfully</returns>
        Task<bool> CreateProjectAsync(string projectName, string projectPath, Dictionary<string, object>? parameters = null, DiagnosticsReporter? diagnostics = null);
        
        /// <summary>
        /// Gets the parameters that this template accepts
        /// </summary>
        /// <returns>Dictionary of parameter names and their descriptions</returns>
        Dictionary<string, string> GetParameters();
        
        /// <summary>
        /// Validates whether the provided parameters are valid for this template
        /// </summary>
        /// <param name="parameters">Parameters to validate</param>
        /// <returns>True if parameters are valid</returns>
        bool ValidateParameters(Dictionary<string, object>? parameters);
    }
}