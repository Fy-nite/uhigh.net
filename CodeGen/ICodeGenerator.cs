using uhigh.Net.Diagnostics;
using uhigh.Net.Parser;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// Configuration for code generators
    /// </summary>
    public class CodeGeneratorConfig
    {
        public string? RootNamespace { get; set; }
        public string? ClassName { get; set; }
        public string OutputType { get; set; } = "Exe";
        public bool VerboseMode { get; set; }
        public string? StdLibPath { get; set; }
        public List<string> AdditionalAssemblies { get; set; } = new();
        public Dictionary<string, object> TargetSpecificOptions { get; set; } = new();
    }

    /// <summary>
    /// Metadata about a code generator
    /// </summary>
    public class CodeGeneratorInfo
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public List<string> SupportedFeatures { get; set; } = new();
        public List<string> RequiredDependencies { get; set; } = new();
    }

    /// <summary>
    /// Interface for code generators that can target different languages/platforms
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// Gets information about this code generator
        /// </summary>
        CodeGeneratorInfo Info { get; }

        /// <summary>
        /// Gets the target language/platform name
        /// </summary>
        string TargetName { get; }

        /// <summary>
        /// Gets the file extension for generated files
        /// </summary>
        string FileExtension { get; }

        /// <summary>
        /// Initializes the generator with configuration
        /// </summary>
        /// <param name="config">Generator configuration</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        void Initialize(CodeGeneratorConfig config, DiagnosticsReporter diagnostics);

        /// <summary>
        /// Validates if the generator can handle the given program
        /// </summary>
        /// <param name="program">Program to validate</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        /// <returns>True if the program can be generated</returns>
        bool CanGenerate(Program program, DiagnosticsReporter diagnostics);

        /// <summary>
        /// Generates code from a single program AST
        /// </summary>
        /// <param name="program">The program AST</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        /// <param name="rootNamespace">Root namespace</param>
        /// <param name="className">Class name</param>
        /// <returns>Generated code</returns>
        string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null);

        /// <summary>
        /// Generates code from multiple program ASTs
        /// </summary>
        /// <param name="programs">List of program ASTs</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        /// <param name="rootNamespace">Root namespace</param>
        /// <param name="className">Class name</param>
        /// <returns>Generated code</returns>
        string GenerateCombined(List<Program> programs, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null);

        /// <summary>
        /// Generates code without using statements (for combining multiple files)
        /// </summary>
        /// <param name="program">The program AST</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        /// <param name="rootNamespace">Root namespace</param>
        /// <param name="className">Class name</param>
        /// <returns>Generated code without using statements</returns>
        string GenerateWithoutUsings(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null);

        /// <summary>
        /// Gets collected using statements/imports
        /// </summary>
        /// <returns>Collection of using statements</returns>
        HashSet<string> GetCollectedUsings();
    }

    /// <summary>
    /// Interface for code generators that can compile generated code
    /// </summary>
    public interface ICompilableCodeGenerator : ICodeGenerator
    {
        /// <summary>
        /// Compiles generated code to executable bytes
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="rootNamespace">Root namespace</param>
        /// <param name="className">Class name</param>
        /// <param name="outputType">Output type (Exe, Library)</param>
        /// <param name="additionalAssemblies">Additional assemblies</param>
        /// <returns>Compiled bytes or null if compilation failed</returns>
        byte[]? CompileToBytes(string generatedCode, string? rootNamespace = null, string? className = null, string outputType = "Exe", List<string>? additionalAssemblies = null);

        /// <summary>
        /// Compiles and runs generated code
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="outputPath">Optional output path</param>
        /// <param name="rootNamespace">Root namespace</param>
        /// <param name="className">Class name</param>
        /// <param name="outputType">Output type</param>
        /// <param name="additionalAssemblies">Additional assemblies</param>
        /// <returns>True if compilation and execution succeeded</returns>
        Task<bool> CompileAndRun(string generatedCode, string? outputPath = null, string? rootNamespace = null, string? className = null, string outputType = "Exe", List<string>? additionalAssemblies = null);

        /// <summary>
        /// Compiles to executable file
        /// </summary>
        /// <param name="generatedCode">The generated code</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="rootNamespace">Root namespace</param>
        /// <param name="className">Class name</param>
        /// <param name="outputType">Output type</param>
        /// <param name="additionalAssemblies">Additional assemblies</param>
        /// <returns>True if compilation succeeded</returns>
        Task<bool> CompileToExecutable(string generatedCode, string outputPath, string? rootNamespace = null, string? className = null, string outputType = "Exe", List<string>? additionalAssemblies = null);
    }

    /// <summary>
    /// Factory interface for creating code generators
    /// </summary>
    public interface ICodeGeneratorFactory
    {
        /// <summary>
        /// Gets the target name this factory supports
        /// </summary>
        string TargetName { get; }

        /// <summary>
        /// Gets information about the generator this factory creates
        /// </summary>
        CodeGeneratorInfo GeneratorInfo { get; }

        /// <summary>
        /// Creates a new instance of the code generator
        /// </summary>
        /// <returns>New code generator instance</returns>
        ICodeGenerator CreateGenerator();

        /// <summary>
        /// Validates if this factory can create a generator for the given configuration
        /// </summary>
        /// <param name="config">Configuration to validate</param>
        /// <returns>True if the factory can handle this configuration</returns>
        bool CanHandle(CodeGeneratorConfig config);
    }

    /// <summary>
    /// Registry for code generators with enhanced plugin support
    /// </summary>
    public static class CodeGeneratorRegistry
    {
        private static readonly Dictionary<string, ICodeGeneratorFactory> _factories = new();
        private static readonly List<string> _loadedPlugins = new();

        static CodeGeneratorRegistry()
        {
            // Register built-in generators
            Register(new CSharpGeneratorFactory());
            Register(new JavaScriptGeneratorFactory());
        }

        /// <summary>
        /// Registers a code generator factory
        /// </summary>
        /// <param name="factory">Factory to register</param>
        public static void Register(ICodeGeneratorFactory factory)
        {
            _factories[factory.TargetName.ToLowerInvariant()] = factory;
        }

        /// <summary>
        /// Registers a code generator for a target
        /// </summary>
        /// <param name="target">Target name</param>
        /// <param name="factory">Factory function</param>
        public static void Register(string target, Func<ICodeGenerator> factory)
        {
            _factories[target.ToLowerInvariant()] = new LambdaCodeGeneratorFactory(target, factory);
        }

        /// <summary>
        /// Gets a code generator for a target
        /// </summary>
        /// <param name="target">Target name</param>
        /// <returns>Code generator or null if not found</returns>
        public static ICodeGenerator? GetGenerator(string target)
        {
            return _factories.TryGetValue(target.ToLowerInvariant(), out var factory) ? factory.CreateGenerator() : null;
        }

        /// <summary>
        /// Gets a code generator factory for a target
        /// </summary>
        /// <param name="target">Target name</param>
        /// <returns>Code generator factory or null if not found</returns>
        public static ICodeGeneratorFactory? GetFactory(string target)
        {
            return _factories.TryGetValue(target.ToLowerInvariant(), out var factory) ? factory : null;
        }

        /// <summary>
        /// Gets all available targets
        /// </summary>
        /// <returns>Available target names</returns>
        public static IEnumerable<string> GetAvailableTargets()
        {
            return _factories.Keys;
        }

        /// <summary>
        /// Gets information about all registered generators
        /// </summary>
        /// <returns>Generator information</returns>
        public static IEnumerable<CodeGeneratorInfo> GetGeneratorInfo()
        {
            return _factories.Values.Select(f => f.GeneratorInfo);
        }

        /// <summary>
        /// Loads code generator plugins from a directory
        /// </summary>
        /// <param name="pluginDirectory">Directory containing plugins</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        public static void LoadPlugins(string pluginDirectory, DiagnosticsReporter diagnostics)
        {
            if (!Directory.Exists(pluginDirectory))
            {
                diagnostics.ReportInfo($"Plugin directory not found: {pluginDirectory}");
                return;
            }

            var pluginFiles = Directory.GetFiles(pluginDirectory, "*.dll", SearchOption.AllDirectories);
            
            foreach (var pluginFile in pluginFiles)
            {
                if (_loadedPlugins.Contains(pluginFile)) continue;

                try
                {
                    var assembly = System.Reflection.Assembly.LoadFrom(pluginFile);
                    
                    // Look for types implementing ICodeGeneratorFactory
                    var factoryTypes = assembly.GetTypes()
                        .Where(t => typeof(ICodeGeneratorFactory).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

                    foreach (var factoryType in factoryTypes)
                    {
                        var factory = (ICodeGeneratorFactory)Activator.CreateInstance(factoryType)!;
                        Register(factory);
                        diagnostics.ReportInfo($"Loaded code generator plugin: {factory.TargetName} from {Path.GetFileName(pluginFile)}");
                    }

                    _loadedPlugins.Add(pluginFile);
                }
                catch (Exception ex)
                {
                    diagnostics.ReportWarning($"Failed to load plugin {Path.GetFileName(pluginFile)}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Lambda-based factory for simple generators
    /// </summary>
    internal class LambdaCodeGeneratorFactory : ICodeGeneratorFactory
    {
        private readonly string _targetName;
        private readonly Func<ICodeGenerator> _factory;

        public LambdaCodeGeneratorFactory(string targetName, Func<ICodeGenerator> factory)
        {
            _targetName = targetName;
            _factory = factory;
        }

        public string TargetName => _targetName;

        public CodeGeneratorInfo GeneratorInfo => new()
        {
            Name = _targetName,
            Description = "Lambda-based code generator",
            Version = "1.0.0"
        };

        public ICodeGenerator CreateGenerator() => _factory();

        public bool CanHandle(CodeGeneratorConfig config) => true;
    }

    /// <summary>
    /// Factory for C# code generator
    /// </summary>
    public class CSharpGeneratorFactory : ICodeGeneratorFactory
    {
        public string TargetName => "csharp";

        public CodeGeneratorInfo GeneratorInfo => new()
        {
            Name = "C# Code Generator",
            Description = "Generates C# code from μHigh programs",
            Version = "2.0.0",
            SupportedFeatures = new() { "classes", "functions", "generics", "match", "lambdas" },
            RequiredDependencies = new() { ".NET 8.0+" }
        };

        public ICodeGenerator CreateGenerator() => new CSharpGenerator();

        public bool CanHandle(CodeGeneratorConfig config) => true;
    }

    /// <summary>
    /// Factory for JavaScript code generator
    /// </summary>
    public class JavaScriptGeneratorFactory : ICodeGeneratorFactory
    {
        public string TargetName => "javascript";

        public CodeGeneratorInfo GeneratorInfo => new()
        {
            Name = "JavaScript Code Generator",
            Description = "Generates JavaScript code from μHigh programs",
            Version = "2.0.0",
            SupportedFeatures = new() { "classes", "functions", "generics", "match", "lambdas" },
            RequiredDependencies = new() { "Node.js" }
        };

        public ICodeGenerator CreateGenerator() => new JavaScriptGenerator();

        public bool CanHandle(CodeGeneratorConfig config) => true;
    }
}
