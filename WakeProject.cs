using System.Xml.Serialization;

namespace Wake.Net
{
    [XmlRoot("Project")]
    public class WakeProject
    {
        [XmlElement("Name")]
        public string Name { get; set; } = "";

        [XmlElement("Version")]
        public string Version { get; set; } = "1.0.0";

        [XmlElement("Description")]
        public string? Description { get; set; }

        [XmlElement("Author")]
        public string? Author { get; set; }

        [XmlElement("Target")]
        public string Target { get; set; } = "net9.0";

        [XmlElement("OutputType")]
        public string OutputType { get; set; } = "Exe"; // Exe, Library

        [XmlArray("SourceFiles")]
        [XmlArrayItem("File")]
        public List<string> SourceFiles { get; set; } = new();

        [XmlArray("Dependencies")]
        [XmlArrayItem("Package")]
        public List<PackageReference> Dependencies { get; set; } = new();

        [XmlArray("Properties")]
        [XmlArrayItem("Property")]
        public List<ProjectProperty> Properties { get; set; } = new();

        [XmlElement("RootNamespace")]
        public string? RootNamespace { get; set; }

        [XmlElement("Nullable")]
        public bool Nullable { get; set; } = true;

        public static WakeProject CreateDefault(string projectName)
        {
            return new WakeProject
            {
                Name = projectName,
                Version = "1.0.0",
                Target = "net9.0",
                OutputType = "Exe",
                SourceFiles = new List<string> { "main.wake" },
                RootNamespace = projectName,
                Nullable = true
            };
        }
    }

    public class PackageReference
    {
        [XmlAttribute("Include")]
        public string Name { get; set; } = "";

        [XmlAttribute("Version")]
        public string Version { get; set; } = "";
    }

    public class ProjectProperty
    {
        [XmlAttribute("Name")]
        public string Name { get; set; } = "";

        [XmlAttribute("Value")]
        public string Value { get; set; } = "";
    }
}
