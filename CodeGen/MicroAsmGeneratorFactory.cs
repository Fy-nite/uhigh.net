using uhigh.Net.Diagnostics;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// Factory for MicroASM code generator
    /// </summary>
    public class MicroAsmGeneratorFactory : ICodeGeneratorFactory
    {
        public string TargetName => "microasm";

        public CodeGeneratorInfo GeneratorInfo => new()
        {
            Name = "MicroASM Code Generator",
            Description = "Generates MicroASM assembly code from μHigh programs",
            Version = "1.0.0",
            SupportedFeatures = new() { "functions", "variables", "control-flow", "arithmetic", "io" },
            RequiredDependencies = new() { "MicroASM Runtime", "jmasm" }
        };

        public ICodeGenerator CreateGenerator() => new MicroAsmGenerator();

        public bool CanHandle(CodeGeneratorConfig config) => true;
    }
}
