using uhigh.Net.Diagnostics;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// Factory for Java code generator
    /// </summary>
    public class JavaGeneratorFactory : ICodeGeneratorFactory
    {
        public string TargetName => "java";

        public CodeGeneratorInfo GeneratorInfo => new()
        {
            Name = "Java Code Generator",
            Description = "Generates Java code from μHigh programs",
            Version = "1.0.0",
            SupportedFeatures = new() { "classes", "methods", "types", "imports" },
            RequiredDependencies = new() { "java.base" }
        };

        public ICodeGenerator CreateGenerator() => new JavaGenerator();

        public bool CanHandle(CodeGeneratorConfig config) => true;
    }
}
