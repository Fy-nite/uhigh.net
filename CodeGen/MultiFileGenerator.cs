using uhigh.Net.Parser;
using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;
using System.Text;

namespace uhigh.Net.CodeGen
{
    public class MultiFileGenerator
    {
        private readonly DiagnosticsReporter _diagnostics;
        private readonly Dictionary<string, StringBuilder> _files = new();
        private readonly HashSet<string> _globalUsings = new();
        private readonly Dictionary<string, string> _importMappings = new();

        public MultiFileGenerator(DiagnosticsReporter? diagnostics = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
        }

        public Dictionary<string, string> Generate(Program program, string outputDirectory = "output")
        {
            _files.Clear();
            _globalUsings.Clear();
            _importMappings.Clear();

            _diagnostics.ReportInfo("Starting multi-file C# code generation");

            // Process imports and collect global usings
            ProcessImports(program);

            // Generate files for each namespace/class/module
            GenerateFiles(program);

            // Convert to dictionary of filename -> content
            var result = new Dictionary<string, string>();
            foreach (var kvp in _files)
            {
                result[kvp.Key] = kvp.Value.ToString();
            }

            _diagnostics.ReportInfo($"Generated {result.Count} C# files");
            return result;
        }

        private void ProcessImports(Program program)
        {
            if (program.Statements == null) return;

            foreach (var statement in program.Statements.OfType<ImportStatement>())
            {
                ProcessImportStatement(statement);
            }

            // Add default usings
            _globalUsings.Add("System");
            _globalUsings.Add("System.Collections.Generic");
            _globalUsings.Add("System.Linq");
        }

        private void ProcessImportStatement(ImportStatement import)
        {
            if (import.AssemblyName.EndsWith(".dll"))
            {
                _importMappings[import.ClassName] = import.AssemblyName;
            }
            else
            {
                var lastDotIndex = import.ClassName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var namespaceToAdd = import.ClassName.Substring(0, lastDotIndex);
                    _globalUsings.Add(namespaceToAdd);
                }
            }
        }

        private void GenerateFiles(Program program)
        {
            if (program.Statements == null) return;

            // Group statements by their logical file
            var fileGroups = GroupStatementsByFile(program.Statements);

            foreach (var group in fileGroups)
            {
                GenerateFile(group.Key, group.Value);
            }
        }

        private Dictionary<string, List<Statement>> GroupStatementsByFile(List<Statement> statements)
        {
            var groups = new Dictionary<string, List<Statement>>();

            foreach (var statement in statements)
            {
                string fileName = GetFileNameForStatement(statement);
                
                if (!groups.ContainsKey(fileName))
                {
                    groups[fileName] = new List<Statement>();
                }
                
                groups[fileName].Add(statement);
            }

            return groups;
        }

        private string GetFileNameForStatement(Statement statement)
        {
            return statement switch
            {
                NamespaceDeclaration nsDecl => $"{nsDecl.Name}.cs",
                ClassDeclaration classDecl => $"{classDecl.Name}.cs", 
                FunctionDeclaration funcDecl => "Functions.cs",
                TypeAliasDeclaration typeAlias => "TypeAliases.cs",
                ImportStatement => "Program.cs", // Imports go to main file
                _ => "Program.cs" // Default file for loose statements
            };
        }

        private void GenerateFile(string fileName, List<Statement> statements)
        {
            var output = new StringBuilder();
            
            // Add using statements
            foreach (var usingDirective in _globalUsings.OrderBy(u => u))
            {
                output.AppendLine($"using {usingDirective};");
            }
            output.AppendLine();

            // Generate type aliases first
            var typeAliases = statements.OfType<TypeAliasDeclaration>().ToList();
            foreach (var typeAlias in typeAliases)
            {
                GenerateTypeAlias(output, typeAlias);
            }
            if (typeAliases.Any()) output.AppendLine();

            // Generate namespace or class content
            var hasNamespace = statements.Any(s => s is NamespaceDeclaration);
            
            if (hasNamespace)
            {
                // Generate namespace structure
                foreach (var nsDecl in statements.OfType<NamespaceDeclaration>())
                {
                    GenerateNamespace(output, nsDecl);
                }
            }
            else
            {
                // Group by classes or create default Program class
                var classes = statements.OfType<ClassDeclaration>().ToList();
                var functions = statements.OfType<FunctionDeclaration>().ToList();
                var otherStatements = statements.Where(s => !(s is ClassDeclaration) && 
                                                           !(s is FunctionDeclaration) && 
                                                           !(s is TypeAliasDeclaration) &&
                                                           !(s is ImportStatement)).ToList();

                if (classes.Any())
                {
                    // Generate each class
                    foreach (var classDecl in classes)
                    {
                        GenerateClass(output, classDecl, 0);
                    }
                }

                if (functions.Any() || otherStatements.Any())
                {
                    // Create a Program class for functions and loose statements
                    output.AppendLine("public class Program");
                    output.AppendLine("{");
                    
                    // Generate built-in functions
                    GenerateBuiltInFunctions(output, 1);

                    // Generate functions
                    foreach (var funcDecl in functions)
                    {
                        if (funcDecl.Name == "main")
                        {
                            GenerateMainFunction(output, funcDecl, 1);
                        }
                        else
                        {
                            GenerateFunction(output, funcDecl, 1);
                        }
                    }

                    // Generate main method if no main function exists
                    if (!functions.Any(f => f.Name == "main") && otherStatements.Any())
                    {
                        GenerateMainMethod(output, otherStatements, 1);
                    }

                    output.AppendLine("}");
                }
            }

            _files[fileName] = output;
        }

        private void GenerateNamespace(StringBuilder output, NamespaceDeclaration nsDecl)
        {
            output.AppendLine($"namespace {nsDecl.Name}");
            output.AppendLine("{");

            foreach (var member in nsDecl.Members)
            {
                GenerateStatement(output, member, 1);
            }

            output.AppendLine("}");
            output.AppendLine();
        }

        private void GenerateClass(StringBuilder output, ClassDeclaration classDecl, int indentLevel)
        {
            Indent(output, indentLevel);
            
            if (classDecl.Modifiers.Count > 0)
            {
                output.Append(string.Join(" ", classDecl.Modifiers) + " ");
            }
            else
            {
                output.Append("public ");
            }
            
            output.Append("class ");
            output.Append(classDecl.Name);
            
            if (classDecl.BaseClass != null)
            {
                output.Append($" : {ConvertType(classDecl.BaseClass)}");
            }
            
            output.AppendLine();
            Indent(output, indentLevel);
            output.AppendLine("{");

            foreach (var member in classDecl.Members)
            {
                GenerateStatement(output, member, indentLevel + 1);
            }

            Indent(output, indentLevel);
            output.AppendLine("}");
            output.AppendLine();
        }

        private void GenerateStatement(StringBuilder output, Statement statement, int indentLevel)
        {
            switch (statement)
            {
                case ClassDeclaration classDecl:
                    GenerateClass(output, classDecl, indentLevel);
                    break;
                case MethodDeclaration methodDecl:
                    GenerateMethod(output, methodDecl, indentLevel);
                    break;
                case FieldDeclaration fieldDecl:
                    GenerateField(output, fieldDecl, indentLevel);
                    break;
                case PropertyDeclaration propDecl:
                    GenerateProperty(output, propDecl, indentLevel);
                    break;
                case FunctionDeclaration funcDecl:
                    GenerateFunction(output, funcDecl, indentLevel);
                    break;
                // Add other statement types as needed
                default:
                    Indent(output, indentLevel);
                    output.AppendLine($"// TODO: Generate {statement.GetType().Name}");
                    break;
            }
        }

        private void GenerateMethod(StringBuilder output, MethodDeclaration methodDecl, int indentLevel)
        {
            if (methodDecl.Attributes.Any(attr => attr.IsExternal || attr.IsDotNetFunc))
            {
                return; // Skip external methods
            }

            Indent(output, indentLevel);
            
            if (methodDecl.Modifiers.Count > 0)
            {
                output.Append(string.Join(" ", methodDecl.Modifiers) + " ");
            }
            else
            {
                output.Append("public ");
            }
            
            var returnType = methodDecl.ReturnType != null ? ConvertType(methodDecl.ReturnType) : "void";
            output.AppendLine($"{returnType} {methodDecl.Name}()");
            
            Indent(output, indentLevel);
            output.AppendLine("{");
            Indent(output, indentLevel + 1);
            output.AppendLine("// Method body");
            Indent(output, indentLevel);
            output.AppendLine("}");
            output.AppendLine();
        }

        private void GenerateField(StringBuilder output, FieldDeclaration fieldDecl, int indentLevel)
        {
            Indent(output, indentLevel);
            
            if (fieldDecl.Modifiers.Count > 0)
            {
                output.Append(string.Join(" ", fieldDecl.Modifiers) + " ");
            }
            else
            {
                output.Append("private ");
            }
            
            var fieldType = fieldDecl.Type != null ? ConvertType(fieldDecl.Type) : "object";
            output.AppendLine($"{fieldType} {fieldDecl.Name};");
        }

        private void GenerateProperty(StringBuilder output, PropertyDeclaration propDecl, int indentLevel)
        {
            Indent(output, indentLevel);
            var propType = propDecl.Type != null ? ConvertType(propDecl.Type) : "object";
            output.AppendLine($"public {propType} {propDecl.Name} {{ get; set; }}");
        }

        private void GenerateFunction(StringBuilder output, FunctionDeclaration funcDecl, int indentLevel)
        {
            if (funcDecl.Attributes.Any(attr => attr.IsExternal || attr.IsDotNetFunc))
            {
                return; // Skip external functions
            }

            Indent(output, indentLevel);
            
            if (funcDecl.Modifiers.Count > 0)
            {
                output.Append(string.Join(" ", funcDecl.Modifiers) + " ");
            }
            else
            {
                output.Append("public static ");
            }
            
            var returnType = funcDecl.ReturnType != null ? ConvertType(funcDecl.ReturnType) : "void";
            output.AppendLine($"{returnType} {funcDecl.Name}()");
            
            Indent(output, indentLevel);
            output.AppendLine("{");
            Indent(output, indentLevel + 1);
            output.AppendLine("// Function body");
            Indent(output, indentLevel);
            output.AppendLine("}");
            output.AppendLine();
        }

        private void GenerateMainFunction(StringBuilder output, FunctionDeclaration mainFunc, int indentLevel)
        {
            Indent(output, indentLevel);
            output.AppendLine("public static void Main(string[] args)");
            Indent(output, indentLevel);
            output.AppendLine("{");
            Indent(output, indentLevel + 1);
            output.AppendLine("// Main function body");
            Indent(output, indentLevel);
            output.AppendLine("}");
            output.AppendLine();
        }

        private void GenerateMainMethod(StringBuilder output, List<Statement> statements, int indentLevel)
        {
            Indent(output, indentLevel);
            output.AppendLine("public static void Main(string[] args)");
            Indent(output, indentLevel);
            output.AppendLine("{");
            Indent(output, indentLevel + 1);
            output.AppendLine("// Generated main method");
            Indent(output, indentLevel);
            output.AppendLine("}");
            output.AppendLine();
        }

        private void GenerateBuiltInFunctions(StringBuilder output, int indentLevel)
        {
            string[] builtIns = {
                "public static void print(object value) => Console.WriteLine(value);",
                "public static string input() => Console.ReadLine() ?? \"\";",
                "public static int @int(string s) => int.Parse(s);",
                "public static double @float(string s) => double.Parse(s);"
            };

            foreach (var builtIn in builtIns)
            {
                Indent(output, indentLevel);
                output.AppendLine(builtIn);
            }
            output.AppendLine();
        }

        private void GenerateTypeAlias(StringBuilder output, TypeAliasDeclaration alias)
        {
            output.Append("using ");
            if (alias.Modifiers.Count > 0)
                output.Append(string.Join(" ", alias.Modifiers) + " ");
            output.Append($"{alias.Name} = ");
            output.Append(ConvertTypeAnnotation(alias.Type));
            output.AppendLine(";");
        }

        private string ConvertTypeAnnotation(TypeAnnotation typeAnn)
        {
            // Handle array types first
            if (typeAnn.Name.EndsWith("[]"))
            {
                return typeAnn.Name; // Already in correct format
            }
            
            if (typeAnn.Name == "array" && typeAnn.TypeArguments.Count == 1)
            {
                return $"{ConvertTypeAnnotation(typeAnn.TypeArguments[0])}[]";
            }
            if (typeAnn.TypeArguments.Count > 0)
            {
                return $"{ConvertType(typeAnn.Name)}<{string.Join(", ", typeAnn.TypeArguments.Select(ConvertTypeAnnotation))}>";
            }
            return ConvertType(typeAnn.Name);
        }

        private string ConvertType(string type)
        {
            // Handle array types first
            if (type.EndsWith("[]"))
            {
                return type; // Already in C# format
            }
            
            // Handle generic types
            if (type.Contains('<') && type.Contains('>'))
            {
                var genericMatch = System.Text.RegularExpressions.Regex.Match(type, @"^(\w+)<(.+)>$");
                if (genericMatch.Success)
                {
                    var baseType = genericMatch.Groups[1].Value;
                    var typeArgs = genericMatch.Groups[2].Value;
                    var typeArgsList = typeArgs.Split(',').Select(t => ConvertType(t.Trim())).ToList();
                    
                    return baseType switch
                    {
                        "array" => $"{typeArgsList[0]}[]",
                        "list" => $"List<{string.Join(", ", typeArgsList)}>",
                        "map" => typeArgsList.Count >= 2 ? $"Dictionary<{typeArgsList[0]}, {typeArgsList[1]}>" : "Dictionary<object, object>",
                        _ => $"{ConvertType(baseType)}<{string.Join(", ", typeArgsList)}>"
                    };
                }
            }
            
            return type switch
            {
                "int" => "int",
                "float" => "double", 
                "string" => "string",
                "bool" => "bool",
                "void" => "void",
                "Command" => "Command", // Keep custom types as-is
                _ => "object"
            };
        }

        private void Indent(StringBuilder output, int level)
        {
            output.Append(new string('\t', level));
        }
    }
}
