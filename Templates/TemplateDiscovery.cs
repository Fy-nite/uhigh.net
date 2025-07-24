using System.Reflection;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Templates
{
    /// <summary>
    /// Service for discovering and loading project templates
    /// </summary>
    public class TemplateDiscovery
    {
        private readonly Dictionary<string, IProjectTemplate> _templates = new();
        private readonly DiagnosticsReporter? _diagnostics;
        
        /// <summary>
        /// Initializes a new instance of the TemplateDiscovery class
        /// </summary>
        /// <param name="diagnostics">Diagnostics reporter</param>
        public TemplateDiscovery(DiagnosticsReporter? diagnostics = null)
        {
            _diagnostics = diagnostics;
        }
        
        /// <summary>
        /// Gets all available templates
        /// </summary>
        public IReadOnlyDictionary<string, IProjectTemplate> Templates => _templates;
        
        /// <summary>
        /// Discovers and loads all templates from built-in and addon sources
        /// </summary>
        /// <param name="addonPaths">Additional paths to search for templates</param>
        public async Task DiscoverTemplatesAsync(params string[] addonPaths)
        {
            _templates.Clear();
            
            // Load built-in templates
            LoadBuiltInTemplates();
            
            // Load templates from addon directories
            foreach (var addonPath in addonPaths)
            {
                await LoadTemplatesFromDirectoryAsync(addonPath);
            }
            
            _diagnostics?.ReportInfo($"Discovered {_templates.Count} project templates");
        }
        
        /// <summary>
        /// Gets a template by name
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <returns>Template instance or null if not found</returns>
        public IProjectTemplate? GetTemplate(string templateName)
        {
            _templates.TryGetValue(templateName.ToLowerInvariant(), out var template);
            return template;
        }
        
        /// <summary>
        /// Lists all available template names
        /// </summary>
        /// <returns>List of template names</returns>
        public List<string> ListTemplateNames()
        {
            return _templates.Keys.OrderBy(k => k).ToList();
        }
        
        /// <summary>
        /// Loads built-in templates
        /// </summary>
        private void LoadBuiltInTemplates()
        {
            try
            {
                // Load all built-in template types from this assembly
                var templateTypes = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IProjectTemplate).IsAssignableFrom(t))
                    .Where(t => t.Namespace?.StartsWith("uhigh.Net.Templates.BuiltIn") == true);
                
                foreach (var templateType in templateTypes)
                {
                    try
                    {
                        if (Activator.CreateInstance(templateType) is IProjectTemplate template)
                        {
                            var key = template.Name.ToLowerInvariant();
                            _templates[key] = template;
                            _diagnostics?.ReportInfo($"Loaded built-in template: {template.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _diagnostics?.ReportWarning($"Failed to load built-in template {templateType.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportWarning($"Failed to load built-in templates: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads templates from a directory containing compiled assemblies
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        private async Task LoadTemplatesFromDirectoryAsync(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    _diagnostics?.ReportInfo($"Template directory not found: {directoryPath}");
                    return;
                }
                
                // Look for .dll files in the directory
                var dllFiles = Directory.GetFiles(directoryPath, "*.dll", SearchOption.TopDirectoryOnly);
                
                foreach (var dllFile in dllFiles)
                {
                    await LoadTemplatesFromAssemblyAsync(dllFile);
                }
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportWarning($"Failed to load templates from directory {directoryPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Loads templates from a specific assembly file
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly file</param>
        private async Task LoadTemplatesFromAssemblyAsync(string assemblyPath)
        {
            try
            {
                // Load the assembly
                var assembly = Assembly.LoadFrom(assemblyPath);
                
                // Find all types that implement IProjectTemplate
                var templateTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IProjectTemplate).IsAssignableFrom(t));
                
                foreach (var templateType in templateTypes)
                {
                    try
                    {
                        if (Activator.CreateInstance(templateType) is IProjectTemplate template)
                        {
                            var key = template.Name.ToLowerInvariant();
                            
                            // Check for conflicts with existing templates
                            if (_templates.ContainsKey(key))
                            {
                                _diagnostics?.ReportWarning($"Template name conflict: '{template.Name}' already exists. Skipping addon template from {assemblyPath}");
                                continue;
                            }
                            
                            _templates[key] = template;
                            _diagnostics?.ReportInfo($"Loaded addon template: {template.Name} from {assemblyPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _diagnostics?.ReportWarning($"Failed to instantiate template {templateType.Name} from {assemblyPath}: {ex.Message}");
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var loaderExceptions = string.Join("; ", ex.LoaderExceptions.Select(e => e.Message));
                _diagnostics?.ReportWarning($"Failed to load types from assembly {assemblyPath}: {loaderExceptions}");
            }
            catch (FileLoadException ex)
            {
                _diagnostics?.ReportWarning($"Failed to load assembly file {assemblyPath}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _diagnostics?.ReportWarning($"Unexpected error while loading assembly {assemblyPath}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets template information for display
        /// </summary>
        /// <param name="templateName">Name of the template</param>
        /// <returns>Template information string</returns>
        public string GetTemplateInfo(string templateName)
        {
            var template = GetTemplate(templateName);
            if (template == null)
            {
                return $"Template '{templateName}' not found";
            }
            
            var info = $"Template: {template.Name}\n";
            info += $"Description: {template.Description}\n";
            info += $"Output Type: {template.OutputType}\n";
            info += $"Version: {template.Version}\n";
            info += $"Author: {template.Author}\n";
            
            var parameters = template.GetParameters();
            if (parameters.Any())
            {
                info += "Parameters:\n";
                foreach (var param in parameters)
                {
                    info += $"  --{param.Key}: {param.Value}\n";
                }
            }
            
            return info;
        }
    }
}