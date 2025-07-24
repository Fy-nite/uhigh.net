using System.Text;
using uhigh.Net.Diagnostics;
using uhigh.Net.Parser;
using uhigh.Net.Lexer;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// LLVM IR code generator for μHigh programs
    /// </summary>
    public class LLVMGenerator : ICodeGenerator
    {
        private readonly StringBuilder _output = new();
        private int _indentLevel = 0;
        private DiagnosticsReporter _diagnostics = new();

        // μHigh type to LLVM IR type mapping
        private static readonly Dictionary<string, string> TypeMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "int", "i32" },
            { "long", "i64" },
            { "float", "float" },
            { "double", "double" },
            { "bool", "i1" },
            { "char", "i8" },
            { "void", "void" },
            { "string", "%String*" }, // Custom string type
            { "array", "%Array*" },   // Custom array type
            { "object", "%Object*" }, // Base object type
            // Add more type mappings as needed
        };

        public CodeGeneratorInfo Info => new()
        {
            Name = "LLVM IR Generator",
            Description = "Generates LLVM IR from μHigh programs for optimized native code compilation",
            Version = "1.0.0",
            SupportedFeatures = new() { "classes", "functions", "generics", "performance", "native" },
            RequiredDependencies = new() { "LLVM 14.0+", "clang" }
        };

        public string TargetName => "llvm";
        public string FileExtension => ".ll";

        public void Initialize(CodeGeneratorConfig config, DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            diagnostics.ReportInfo("Initialized LLVM IR generator");
        }

        public bool CanGenerate(Program program, DiagnosticsReporter diagnostics)
        {
            // Basic validation for LLVM compatibility
            // For now, we'll accept all programs but report warnings for unsupported features
            bool canGenerate = true;
            
            // Check for advanced features that might not be fully supported yet
            var advancedFeatures = DetectAdvancedFeatures(program);
            foreach (var feature in advancedFeatures)
            {
                diagnostics.ReportWarning($"The feature '{feature}' may have limited support in LLVM target");
            }
            
            return canGenerate;
        }

        private List<string> DetectAdvancedFeatures(Program program)
        {
            var features = new List<string>();
            
            // Detect complex language features
            // This is a placeholder - actual implementation would be more sophisticated
            
            return features;
        }

        public string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;

            // Add LLVM module header
            _output.AppendLine("; LLVM IR code generated from μHigh");
            _output.AppendLine("; Target triple: x86_64-pc-linux-gnu");
            _output.AppendLine();
            
            // Define target triple
            _output.AppendLine("target triple = \"x86_64-pc-linux-gnu\"");
            _output.AppendLine();
            
            // Declare runtime functions
            GenerateRuntimeDeclarations();

            // Declare custom types
            GenerateCustomTypes();
            
            // Generate program declarations
            GenerateDeclarations(program);
            
            // Generate function implementations
            GenerateFunctions(program);
            
            return _output.ToString();
        }
        
        private void GenerateRuntimeDeclarations()
        {
            _output.AppendLine("; Runtime function declarations");
            _output.AppendLine("declare i32 @printf(i8* nocapture, ...) nounwind");
            _output.AppendLine("declare i8* @malloc(i64) nounwind");
            _output.AppendLine("declare void @free(i8*) nounwind");
            _output.AppendLine("declare i64 @strlen(i8*) nounwind");
            _output.AppendLine("declare i8* @strcpy(i8*, i8*) nounwind");
            _output.AppendLine();
        }
        
        private void GenerateCustomTypes()
        {
            _output.AppendLine("; Custom type definitions");
            _output.AppendLine("%String = type { i32, i8* }");
            _output.AppendLine("%Array = type { i32, i32, i8* }");
            _output.AppendLine("%Object = type { i32 }");
            _output.AppendLine();
            
            // Helper functions for string handling
            _output.AppendLine("define %String* @createString(i8* %value) {");
            _output.AppendLine("  %len = call i64 @strlen(i8* %value)");
            _output.AppendLine("  %size = add i64 %len, 1");
            _output.AppendLine("  %buffer = call i8* @malloc(i64 %size)");
            _output.AppendLine("  call i8* @strcpy(i8* %buffer, i8* %value)");
            _output.AppendLine("  %str = call %String* @malloc(i64 16)"); // Sizeof String struct
            _output.AppendLine("  %lenPtr = getelementptr %String, %String* %str, i32 0, i32 0");
            _output.AppendLine("  store i32 %len, i32* %lenPtr");
            _output.AppendLine("  %valuePtr = getelementptr %String, %String* %str, i32 0, i32 1");
            _output.AppendLine("  store i8* %buffer, i8** %valuePtr");
            _output.AppendLine("  ret %String* %str");
            _output.AppendLine("}");
            _output.AppendLine();
        }
        
        private void GenerateDeclarations(Program program)
        {
            _output.AppendLine("; Function declarations");
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration funcDecl)
                {
                    GenerateFunctionDeclaration(funcDecl);
                }
                else if (statement is ClassDeclaration classDecl)
                {
                    GenerateClassDeclaration(classDecl);
                }
            }
            _output.AppendLine();
        }
        
        private void GenerateFunctionDeclaration(FunctionDeclaration funcDecl)
        {
            var returnType = ConvertType(funcDecl.ReturnType ?? "void");
            var name = funcDecl.Name;
            
            _output.Append($"declare {returnType} @{name}(");
            
            for (int i = 0; i < funcDecl.Parameters.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                var paramType = ConvertType(funcDecl.Parameters[i].Type ?? "object");
                _output.Append($"{paramType}");
            }
            
            _output.AppendLine(")");
        }
        
        private void GenerateClassDeclaration(ClassDeclaration classDecl)
        {
            var name = classDecl.Name;
            _output.AppendLine($"%{name} = type {{");
            _indentLevel++;
            
            // Generate fields
            foreach (var member in classDecl.Members)
            {
                if (member is FieldDeclaration field)
                {
                    Indent();
                    var fieldType = ConvertType(field.Type ?? "object");
                    _output.AppendLine($"{fieldType}, ; {field.Name}");
                }
            }
            
            _indentLevel--;
            _output.AppendLine("}");
        }
        
        private void GenerateFunctions(Program program)
        {
            _output.AppendLine("; Function implementations");
            foreach (var statement in program.Statements)
            {
                if (statement is FunctionDeclaration funcDecl)
                {
                    GenerateFunctionImplementation(funcDecl);
                }
            }
        }
        
        private void GenerateFunctionImplementation(FunctionDeclaration funcDecl)
        {
            var returnType = ConvertType(funcDecl.ReturnType ?? "void");
            var name = funcDecl.Name;
            
            _output.Append($"define {returnType} @{name}(");
            
            for (int i = 0; i < funcDecl.Parameters.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                var param = funcDecl.Parameters[i];
                var paramType = ConvertType(param.Type ?? "object");
                _output.Append($"{paramType} %{param.Name}");
            }
            
            _output.AppendLine(") {");
            _indentLevel++;
            
            Indent();
            _output.AppendLine("entry:");
            _indentLevel++;
            
            // Function body would be generated here
            // This is a placeholder - function bodies require sophisticated LLVM IR generation
            Indent();
            _output.AppendLine("; Function body would be generated here");
            
            if (returnType == "void")
            {
                Indent();
                _output.AppendLine("ret void");
            }
            else
            {
                Indent();
                _output.AppendLine($"ret {returnType} undef");
            }
            
            _indentLevel--;
            _indentLevel--;
            _output.AppendLine("}");
            _output.AppendLine();
        }

        private string ConvertType(string typeName)
        {
            // Handle generic type parameters - preserve as specified type
            if (IsGenericTypeParameter(typeName))
            {
                return "%generic*"; // Generic types as opaque pointers
            }
            
            // First check built-in type mappings
            if (TypeMappings.TryGetValue(typeName, out var llvmType))
            {
                return llvmType;
            }
            
            // Handle array types
            if (typeName.EndsWith("[]"))
            {
                return "%Array*"; // Generic array type
            }
            
            // Handle custom types as named struct pointers
            return $"%{typeName}*";
        }
        
        private bool IsGenericTypeParameter(string typeName)
        {
            return (typeName.Length == 1 && char.IsUpper(typeName[0])) ||
                   (typeName.StartsWith("T") && typeName.Length <= 15 && char.IsUpper(typeName[0]));
        }
        
        private void Indent()
        {
            _output.Append(new string(' ', _indentLevel * 2));
        }

        public string GenerateCombined(List<Program> programs, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            // Combine all programs into one
            var combinedProgram = new Program();
            combinedProgram.Statements = programs.SelectMany(p => p.Statements).ToList();
            
            return Generate(combinedProgram, diagnostics, rootNamespace, className);
        }

        public string GenerateWithoutUsings(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            // LLVM IR doesn't have using statements, so this is equivalent to standard generation
            return Generate(program, diagnostics, rootNamespace, className);
        }

        public HashSet<string> GetCollectedUsings()
        {
            // LLVM IR doesn't have using statements
            return new HashSet<string>();
        }

        private void GenerateExpression(ASTNode expression)
        {
            switch (expression)
            {
                // ...existing code...
                case ArrayExpression arrayExpr:
                    // LLVM IR: arrays need to be constructed via memory allocation, so emit a comment for now
                    _output.Append("; array literal not directly supported in LLVM IR");
                    break;
                // ...existing code...
            }
        }
    }
}
