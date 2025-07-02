using System.Xml.Serialization;
using System.Collections.Generic; // Add this using directive

namespace uhigh.Net
{
    /// <summary>
    /// The uhigh project class
    /// </summary>
    [XmlRoot("Project")]
    public class uhighProject
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        [XmlElement("Name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the version
        /// </summary>
        [XmlElement("Version")]
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the value of the description
        /// </summary>
        [XmlElement("Description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the value of the author
        /// </summary>
        [XmlElement("Author")]
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the value of the target
        /// </summary>
        [XmlElement("Target")]
        public string Target { get; set; } = "net9.0";

        /// <summary>
        /// Gets or sets the value of the output type
        /// </summary>
        [XmlElement("OutputType")]
        public string OutputType { get; set; } = "Exe"; // Exe, Library

        /// <summary>
        /// Gets or sets the value of the source files
        /// </summary>
        [XmlArray("SourceFiles")]
        [XmlArrayItem("File")]
        public List<string> SourceFiles { get; set; } = new();

        /// <summary>
        /// Gets or sets the value of the dependencies
        /// </summary>
        [XmlArray("Dependencies")]
        [XmlArrayItem("Package")]
        public List<PackageReference> Dependencies { get; set; } = new();

        /// <summary>
        /// Gets or sets the value of the properties
        /// </summary>
        [XmlArray("Properties")]
        [XmlArrayItem("Property")]
        public List<ProjectProperty> Properties { get; set; } = new();

        /// <summary>
        /// Gets or sets the value of the root namespace
        /// </summary>
        [XmlElement("RootNamespace")]
        public string? RootNamespace { get; set; }

        /// <summary>
        /// Gets or sets the value of the class name
        /// </summary>
        [XmlElement("ClassName")]
        public string? ClassName { get; set; }

        /// <summary>
        /// Gets or sets the value of the nullable
        /// </summary>
        [XmlElement("Nullable")]
        public bool Nullable { get; set; } = true;

        /// <summary>
        /// Creates the default using the specified project name
        /// </summary>
        /// <param name="projectName">The project name</param>
        /// <returns>The uhigh project</returns>
        public static uhighProject CreateDefault(string projectName)
        {
            return new uhighProject
            {
                Name = projectName,
                Version = "1.0.0",
                Target = "net9.0",
                OutputType = "Exe",
                SourceFiles = new List<string> { "main.uh" }, // Update file extension to .uh
                RootNamespace = projectName,
                ClassName = "Program",
                Nullable = true
            };
        }
    }

    /// <summary>
    /// The package reference class
    /// </summary>
    public class PackageReference
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        [XmlAttribute("Include")]
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the version
        /// </summary>
        [XmlAttribute("Version")]
        public string Version { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the value of the required for
        /// </summary>
        [XmlAttribute("RequiredFor")]
        public string? RequiredFor { get; set; } // New: track what feature requires this package
        
        /// <summary>
        /// Gets or sets the value of the source
        /// </summary>
        [XmlAttribute("Source")]
        public string? Source { get; set; } // New: NuGet source URL
        
        /// <summary>
        /// Gets or sets the value of the include assets
        /// </summary>
        [XmlAttribute("IncludeAssets")]
        public string? IncludeAssets { get; set; } // New: What assets to include (compile, runtime, build, etc.)
        
        /// <summary>
        /// Gets or sets the value of the exclude assets
        /// </summary>
        [XmlAttribute("ExcludeAssets")]
        public string? ExcludeAssets { get; set; } // New: What assets to exclude
        
        /// <summary>
        /// Gets or sets the value of the private assets
        /// </summary>
        [XmlAttribute("PrivateAssets")]
        public string? PrivateAssets { get; set; } // New: Private assets
        
        /// <summary>
        /// Gets or sets the value of the compile only
        /// </summary>
        [XmlAttribute("CompileOnly")]
        public bool CompileOnly { get; set; } = false; // New: Whether this is compile-time only
    }

    /// <summary>
    /// The project property class
    /// </summary>
    public class ProjectProperty
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        [XmlAttribute("Name")]
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        [XmlAttribute("Value")]
        public string Value { get; set; } = "";
        
        /// <summary>
        /// Gets or sets the value of the category
        /// </summary>
        [XmlAttribute("Category")]
        public string? Category { get; set; } // New: categorize properties
    }
}
