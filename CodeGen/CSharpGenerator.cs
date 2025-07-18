using System.Text;
using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;
using uhigh.Net.Parser;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// The sharp generator class
    /// </summary>
    public class CSharpGenerator: ICodeGenerator
    {
        /// <summary>
        /// The output
        /// </summary>
        private readonly StringBuilder _output = new();
        /// <summary>
        /// The indent level
        /// </summary>
        private int _indentLevel = 0;
        /// <summary>
        /// The usings
        /// </summary>
        private readonly HashSet<string> _usings = new();
        /// <summary>
        /// The import mappings
        /// </summary>
        private readonly Dictionary<string, string> _importMappings = new();
        /// <summary>
        /// The diagnostics
        /// </summary>
        private DiagnosticsReporter _diagnostics = new();
        /// <summary>
        /// The root namespace
        /// </summary>
        private string _rootNamespace = "Generated";
        /// <summary>
        /// The class name
        /// </summary>
        private string _className = "Program";
        /// <summary>
        /// The suppress usings
        /// </summary>
        private bool _suppressUsings = false;
        /// <summary>
        /// The type resolver
        /// </summary>
        private ReflectionTypeResolver _typeResolver; // Add this field

        /// <summary>
        /// The config
        /// </summary>
        private CodeGeneratorConfig? _config;
        private string? _currentClassName;

        public CodeGeneratorInfo Info => new()
        {
            Name = "C# Code Generator",
            Description = "Generates C# code from μHigh programs with full feature support",
            Version = "2.0.0",
            SupportedFeatures = new() { "classes", "functions", "generics", "match", "lambdas", "async", "attributes" },
            RequiredDependencies = new() { ".NET 8.0+", "Microsoft.CodeAnalysis" }
        };

        public string TargetName => "csharp";

        public string FileExtension => ".cs";

        /// <summary>
        /// Initializes the generator with configuration
        /// </summary>
        /// <param name="config">Generator configuration</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        public void Initialize(CodeGeneratorConfig config, DiagnosticsReporter diagnostics)
        {
            _config = config;
            _rootNamespace = config.RootNamespace ?? "Generated";
            _className = config.ClassName ?? "Program";
            
            diagnostics.ReportInfo($"Initialized C# generator with namespace: {_rootNamespace}, class: {_className}");
        }

        /// <summary>
        /// Validates if the generator can handle the given program
        /// </summary>
        /// <param name="program">Program to validate</param>
        /// <param name="diagnostics">Diagnostics reporter</param>
        /// <returns>True if the program can be generated</returns>
        public bool CanGenerate(Program program, DiagnosticsReporter diagnostics)
        {
            // Check for unsupported features
            var unsupportedFeatures = new List<string>();

            // Add validation logic here
            // For now, C# generator supports most features
            
            if (unsupportedFeatures.Any())
            {
                foreach (var feature in unsupportedFeatures)
                {
                    diagnostics.ReportError($"Unsupported feature for C# target: {feature}");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Generates the program
        /// </summary>
        /// <param name="program">The program</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <returns>The string</returns>
        public string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _typeResolver = new ReflectionTypeResolver(_diagnostics); // Initialize type resolver
            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();
            _importMappings.Clear();
            _rootNamespace = rootNamespace ?? "Generated";
            _className = className ?? "Program";
            _suppressUsings = false; // Reset flag

            _diagnostics.ReportInfo("Starting C# code generation");

            // Process imports first
            ProcessImports(program);

            // Add using statements only if not suppressed
            if (!_suppressUsings)
            {
                foreach (var usingDirective in _usings.OrderBy(u => u))
                {
                    _output.AppendLine($"using {usingDirective};");
                }
                _output.AppendLine();
            }

            // Generate type aliases as C# using statements
            foreach (var typeAlias in program.Statements.OfType<TypeAliasDeclaration>())
            {
                GenerateTypeAlias(typeAlias);
            }

            // Generate program content
            GenerateProgramContent(program);

            _diagnostics.ReportInfo($"C# code generation completed. Generated {_output.ToString().Split('\n').Length} lines");
            return _output.ToString();
        }

        // Add method to generate code without using statements (for combining multiple files)
        /// <summary>
        /// Generates the without usings using the specified program
        /// </summary>
        /// <param name="program">The program</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <returns>The string</returns>
        public string GenerateWithoutUsings(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _suppressUsings = true;
            return Generate(program, diagnostics, rootNamespace, className);
        }

        // Add method to get collected using statements
        /// <summary>
        /// Gets the collected usings
        /// </summary>
        /// <returns>A hash set of string</returns>
        public HashSet<string> GetCollectedUsings()
        {
            return new HashSet<string>(_usings);
        }

        // Add method to generate combined code from multiple programs
        /// <summary>
        /// Generates the combined using the specified programs
        /// </summary>
        /// <param name="programs">The programs</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="rootNamespace">The root namespace</param>
        /// <param name="className">The class name</param>
        /// <returns>The string</returns>
        public string GenerateCombined(List<Program> programs, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();
            _importMappings.Clear();
            _rootNamespace = rootNamespace ?? "Generated";
            _className = className ?? "Program";

            _diagnostics.ReportInfo("Starting combined C# code generation");

            // Collect all using statements from all programs first
            foreach (var program in programs)
            {
                ProcessImports(program);
            }

            // Generate using statements once at the top
            foreach (var usingDirective in _usings.OrderBy(u => u))
            {
                _output.AppendLine($"using {usingDirective};");
            }
            _output.AppendLine();

            // Collect all statements from all programs and organize them by type
            var allNamespaces = new Dictionary<string, List<Statement>>();
            var allClasses = new Dictionary<string, List<Statement>>();
            var allFunctions = new List<FunctionDeclaration>();
            var allStatements = new List<Statement>();
            var mainFunction = (FunctionDeclaration?)null;

            foreach (var program in programs)
            {
                if (program.Statements == null) continue;

                foreach (var statement in program.Statements.Where(s => !(s is ImportStatement)))
                {
                    switch (statement)
                    {
                        case NamespaceDeclaration nsDecl:
                            if (!allNamespaces.ContainsKey(nsDecl.Name))
                                allNamespaces[nsDecl.Name] = new List<Statement>();
                            allNamespaces[nsDecl.Name].AddRange(nsDecl.Members);
                            break;
                        case ClassDeclaration classDecl:
                            if (!allClasses.ContainsKey(classDecl.Name))
                                allClasses[classDecl.Name] = new List<Statement>();
                            allClasses[classDecl.Name].AddRange(classDecl.Members);
                            break;
                        case FunctionDeclaration funcDecl:
                            if (funcDecl.Name == "main")
                                mainFunction = funcDecl;
                            else
                                allFunctions.Add(funcDecl);
                            break;
                        default:
                            allStatements.Add(statement);
                            break;
                    }
                }
            }

            // Generate merged content
            if (allNamespaces.Count > 0)
            {
                // Generate merged namespaces
                foreach (var ns in allNamespaces)
                {
                    _output.AppendLine($"namespace {ns.Key}");
                    _output.AppendLine("{");
                    _indentLevel++;

                    foreach (var member in ns.Value)
                    {
                        GenerateStatement(member);
                    }

                    _indentLevel--;
                    _output.AppendLine("}");
                    _output.AppendLine();
                }
            }

            if (allClasses.Count > 0)
            {
                // Generate merged classes
                foreach (var cls in allClasses)
                {
                    _output.AppendLine($"public class {cls.Key}");
                    _output.AppendLine("{");
                    _indentLevel++;

                    foreach (var member in cls.Value)
                    {
                        GenerateStatement(member);
                    }

                    _indentLevel--;
                    _output.AppendLine("}");
                    _output.AppendLine();
                }
            }

            // If we have loose functions or statements, wrap them in the default namespace/class
            if (allFunctions.Count > 0 || allStatements.Count > 0 || mainFunction != null)
            {
                _output.AppendLine($"namespace {_rootNamespace}");
                _output.AppendLine("{");
                _indentLevel++;

                _output.AppendLine($"public class {_className}");
                _output.AppendLine("{");
                _indentLevel++;

                GenerateBuiltInFunctions();

                // Generate all loose statements first
                foreach (var statement in allStatements)
                {
                    GenerateStatement(statement);
                }

                // Generate all functions
                foreach (var function in allFunctions)
                {
                    GenerateFunctionDeclaration(function);
                }

                // Generate main function or method
                if (mainFunction != null)
                {
                    GenerateMainFunction(mainFunction);
                }
                else if (allStatements.Where(s => !(s is FunctionDeclaration)).Any())
                {
                    // Generate main method for loose statements
                    GenerateMainMethod(allStatements.Where(s => !(s is FunctionDeclaration)).Cast<ASTNode>().ToList());
                }

                _indentLevel--;
                _output.AppendLine("}");
                _indentLevel--;
                _output.AppendLine("}");
            }

            _diagnostics.ReportInfo($"Combined C# code generation completed. Generated {_output.ToString().Split('\n').Length} lines");
            return _output.ToString();
        }

        // Helper method to generate program content without using statements
        /// <summary>
        /// Generates the program content without usings using the specified program
        /// </summary>
        /// <param name="program">The program</param>
        private void GenerateProgramContentWithoutUsings(Program program)
        {
            if (program.Statements == null) return;

            var hasNamespace = program.Statements.Any(s => s is NamespaceDeclaration);
            var hasClass = program.Statements.Any(s => s is ClassDeclaration);

            // Generate the source structure directly without wrapping or using statements
            foreach (var statement in program.Statements.Where(s => !(s is ImportStatement)))
            {
                GenerateStatement(statement);
            }
        }

        /// <summary>
        /// Processes the imports using the specified program
        /// </summary>
        /// <param name="program">The program</param>
        private void ProcessImports(Program program)
        {
            if (program.Statements == null) return;

            foreach (var statement in program.Statements.OfType<ImportStatement>())
            {
                ProcessImportStatement(statement);
            }

            // Add default usings
            _usings.Add("System");
            _usings.Add("System.Collections.Generic");
            _usings.Add("System.Linq");
            _usings.Add("System.Threading.Tasks");
        }

        /// <summary>
        /// Processes the import statement using the specified import
        /// </summary>
        /// <param name="import">The import</param>
        private void ProcessImportStatement(ImportStatement import)
        {
            if (import.AssemblyName.EndsWith(".dll"))
            {
                // Custom assembly - add to mappings for later reference loading
                _importMappings[import.ClassName] = import.AssemblyName;
            }
            else
            {
                // System namespace or any namespace: add the full namespace
                if (!string.IsNullOrWhiteSpace(import.ClassName))
                {
                    _usings.Add(import.ClassName);
                }
            }
        }

        /// <summary>
        /// Generates the program content using the specified program
        /// </summary>
        /// <param name="program">The program</param>
        private void GenerateProgramContent(Program program)
        {
            if (program.Statements == null) return;

            var hasNamespace = program.Statements.Any(s => s is NamespaceDeclaration);
            var hasClass = program.Statements.Any(s => s is ClassDeclaration);
            var hasMainFunction = program.Statements.OfType<FunctionDeclaration>().Any(f => f.Name == "main");

            // If source has its own namespace/class structure, use it as-is
            if (hasNamespace || hasClass)
            {
                // Generate the source structure directly without wrapping
                foreach (var statement in program.Statements.Where(s => !(s is ImportStatement)))
                {
                    GenerateStatement(statement);
                }
            }
            else
            {
                // Generate default wrapper namespace and class for loose statements/functions
                _output.AppendLine($"namespace {_rootNamespace}");
                _output.AppendLine("{");
                _indentLevel++;
                GenerateDefaultProgram(program);
                _indentLevel--;
                _output.AppendLine("}");
            }
        }

        /// <summary>
        /// Generates the default program using the specified program
        /// </summary>
        /// <param name="program">The program</param>
        private void GenerateDefaultProgram(Program program)
        {
            Indent();
            _output.AppendLine($"public class {_className}");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            GenerateBuiltInFunctions();

            var statements = program.Statements?.Where(s => !(s is ImportStatement) && !(s is NamespaceDeclaration)).ToList();
            var mainFunction = statements?.OfType<FunctionDeclaration>().FirstOrDefault(f => f.Name == "main");
            var hasMainFunction = mainFunction != null;

            if (statements != null)
            {
                foreach (var statement in statements)
                {
                    if (statement is FunctionDeclaration funcDecl)
                    {
                        if (funcDecl.Name == "main")
                        {
                            GenerateMainFunction(funcDecl);
                        }
                        else
                        {
                            GenerateFunctionDeclaration(funcDecl);
                        }
                    }
                    else
                    {
                        GenerateStatement(statement);
                    }
                }
            }

            if (!hasMainFunction)
            {
                GenerateMainMethod(statements?.Where(s => !(s is FunctionDeclaration)).Cast<ASTNode>().ToList());
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
        }

        /// <summary>
        /// Generates the built in functions
        /// </summary>
        private void GenerateBuiltInFunctions()
        {
            // don't generate built-in functions since we have the standard library
            _output.AppendLine("// Built-in functions are now part of the standard library");
            _output.AppendLine("// You can use them directly without generating here\n");
        }

        /// <summary>
        /// Generates the main method using the specified statements
        /// </summary>
        /// <param name="statements">The statements</param>
        private void GenerateMainMethod(List<ASTNode>? statements)
        {
            Indent();
            _output.AppendLine("public static void Main(string[] args)");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            if (statements != null)
            {
                foreach (var statement in statements)
                {
                    GenerateStatement(statement);
                }
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();
        }

        /// <summary>
        /// Generates the main function using the specified main func
        /// </summary>
        /// <param name="mainFunc">The main func</param>
        private void GenerateMainFunction(FunctionDeclaration mainFunc)
        {
            Indent();
            _output.AppendLine("public static void Main(string[] args)");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var stmt in mainFunc.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();
        }

        /// <summary>
        /// Generates the class declaration using the specified class decl
        /// </summary>
        /// <param name="classDecl">The class decl</param>
        private void GenerateClassDeclaration(ClassDeclaration classDecl)
        {
            // Check for external attribute on class
            var hasExternalAttribute = classDecl.Attributes.Any(attr => attr.IsExternal);

            if (hasExternalAttribute)
            {
                _diagnostics.ReportInfo($"Skipping code generation for external class: {classDecl.Name}");
                return;
            }

            // Store current class name for constructor generation
            var previousClassName = _currentClassName;
            _currentClassName = classDecl.Name;

            // Generate attributes for the class
            GenerateAttributes(classDecl.Attributes);

            Indent();

            // Generate modifiers
            if (classDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", classDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("public "); // Default to public
            }

            _output.Append("class ");
            _output.Append(classDecl.Name);

            // Emit generic parameters if present
            if (classDecl.GenericParameters != null && classDecl.GenericParameters.Count > 0)
            {
                _output.Append("<");
                _output.Append(string.Join(", ", classDecl.GenericParameters));
                _output.Append(">");
            }

            if (classDecl.BaseClass != null)
            {
                _output.Append($" : {ConvertType(classDecl.BaseClass)}");
            }

            _output.AppendLine();
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var member in classDecl.Members)
            {
                GenerateStatement(member);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();

            // Restore previous class name
            _currentClassName = previousClassName;
        }

        /// <summary>
        /// Generates the attributes using the specified attributes
        /// </summary>
        /// <param name="attributes">The attributes</param>
        private void GenerateAttributes(List<AttributeDeclaration> attributes)
        {
            foreach (var attribute in attributes)
            {
                // Skip μHigh-specific attributes that don't map to C#
                if (attribute.IsExternal || attribute.IsDotNetFunc)
                    continue;

                Indent();
                _output.Append($"[{attribute.Name}");

                if (attribute.Arguments.Count > 0)
                {
                    _output.Append("(");
                    for (int i = 0; i < attribute.Arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(attribute.Arguments[i]);
                    }
                    _output.Append(")");
                }

                _output.AppendLine("]");
            }
        }

        /// <summary>
        /// Generates the statement using the specified statement
        /// </summary>
        /// <param name="statement">The statement</param>
        private void GenerateStatement(ASTNode statement)
        {
            switch (statement)
            {
                case ImportStatement:
                    // Already processed
                    break;
                case TypeAliasDeclaration typeAlias:
                    GenerateTypeAlias(typeAlias);
                    break;
                case NamespaceDeclaration nsDecl:
                    GenerateNamespaceDeclaration(nsDecl);
                    break;

                case ClassDeclaration classDecl:
                    GenerateClassDeclaration(classDecl);
                    break;
                case MethodDeclaration methodDecl:
                    GenerateMethodDeclaration(methodDecl);
                    break;
                case FieldDeclaration fieldDecl:
                    GenerateFieldDeclaration(fieldDecl);
                    break;
                case PropertyDeclaration propDecl:
                    GeneratePropertyDeclaration(propDecl);
                    break;
                case VariableDeclaration varDecl:
                    GenerateVariableDeclaration(varDecl);
                    break;
                case FunctionDeclaration funcDecl:
                    GenerateFunctionDeclaration(funcDecl);
                    break;
                case IfStatement ifStmt:
                    GenerateIfStatement(ifStmt);
                    break;
                case WhileStatement whileStmt:
                    GenerateWhileStatement(whileStmt);
                    break;
                case ForStatement forStmt:
                    GenerateForStatement(forStmt);
                    break;
                case ReturnStatement returnStmt:
                    Indent();
                    _output.Append("return");
                    if (returnStmt.Value != null)
                    {
                        _output.Append(" ");
                        GenerateExpression(returnStmt.Value);
                    }
                    _output.AppendLine(";");
                    break;
                case BreakStatement:
                    Indent();
                    _output.AppendLine("break;");
                    break;
                case ContinueStatement:
                    Indent();
                    _output.AppendLine("continue;");
                    break;
                case SharpBlock sharpBlock:
                    GenerateSharpBlock(sharpBlock);
                    break;
                case MatchStatement matchStmt:
                    GenerateMatchStatement(matchStmt);
                    break;
                case ExpressionStatement exprStmt:
                    Indent();
                    // check if it's a using statement

                    GenerateExpression(exprStmt.Expression);
                    _output.AppendLine(";");
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown statement type: {statement.GetType().Name}");
                    break;
            }
        }

        /// <summary>
        /// Generates the sharp block using the specified sharp block
        /// </summary>
        /// <param name="sharpBlock">The sharp block</param>
        private void GenerateSharpBlock(SharpBlock sharpBlock)
        {
            if (string.IsNullOrWhiteSpace(sharpBlock.Code))
            {
                _diagnostics.ReportCodeGenWarning("Empty sharp block found");
                return;
            }

            // Split the code into lines and indent each line properly
            var lines = sharpBlock.Code.Split('\n');
            foreach (var line in lines)
            {
                Indent();
                _output.AppendLine(line.TrimEnd());
            }
        }

        /// <summary>
        /// Generates the namespace declaration using the specified ns decl
        /// </summary>
        /// <param name="nsDecl">The ns decl</param>
        private void GenerateNamespaceDeclaration(NamespaceDeclaration nsDecl)
        {
            Indent();
            // Use the namespace name as-is, don't prefix with root namespace
            _output.AppendLine($"namespace {nsDecl.Name}");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var member in nsDecl.Members)
            {
                GenerateStatement(member);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();
        }

        /// <summary>
        /// Generates the method declaration using the specified method decl
        /// </summary>
        /// <param name="methodDecl">The method decl</param>
        private void GenerateMethodDeclaration(MethodDeclaration methodDecl)
        {
            // Check if method has external or dotnetfunc attribute - don't generate implementation
            var hasExternalAttribute = methodDecl.Attributes.Any(attr => attr.IsExternal);
            var hasDotNetFuncAttribute = methodDecl.Attributes.Any(attr => attr.IsDotNetFunc);

            if (hasDotNetFuncAttribute || hasExternalAttribute)
            {
                _diagnostics.ReportInfo($"Skipping code generation for external method: {methodDecl.Name}");
                return;
            }

            // Emit attributes for methods
            GenerateAttributes(methodDecl.Attributes);

            Indent();

            // Generate modifiers
            if (methodDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", methodDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("public "); // Default to public
            }

            if (methodDecl.IsConstructor)
            {
                // For constructors, use the current class name
                var className = _currentClassName ?? "GeneratedClass";
                _output.Append($"{className}(");
            }
            else
            {
                var returnType = methodDecl.ReturnType != null ? ConvertType(methodDecl.ReturnType) : "void";
                _output.Append($"{returnType} {methodDecl.Name}");

                // Emit generic parameters for methods
                if (methodDecl.GenericParameters != null && methodDecl.GenericParameters.Count > 0)
                {
                    _output.Append("<");
                    _output.Append(string.Join(", ", methodDecl.GenericParameters));
                    _output.Append(">");
                }
                _output.Append("(");
            }

            // Parameters
            for (int i = 0; i < methodDecl.Parameters.Count; i++)
            {
                var param = methodDecl.Parameters[i];
                if (i > 0) _output.Append(", ");

                var paramType = param.Type != null ? ConvertType(param.Type) : "object";
                _output.Append($"{paramType} {param.Name}");
            }

            _output.AppendLine(")");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var stmt in methodDecl.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();
        }

        private string GetCurrentClassName()
        {
            return _currentClassName ?? "GeneratedClass";
        }

        /// <summary>
        /// Generates the property declaration using the specified prop decl
        /// </summary>
        /// <param name="propDecl">The prop decl</param>
        private void GeneratePropertyDeclaration(PropertyDeclaration propDecl)
        {
            Indent();
            _output.Append("public ");

            var propType = propDecl.Type != null ? ConvertType(propDecl.Type) : "object";
            _output.Append($"{propType} {propDecl.Name}");

            if (propDecl.Accessors.Count > 0)
            {
                _output.AppendLine();
                Indent();
                _output.AppendLine("{");
                _indentLevel++;

                foreach (var accessor in propDecl.Accessors)
                {
                    Indent();
                    _output.Append(accessor.Type);

                    if (accessor.Body != null)
                    {
                        // Expression-bodied accessor: get => expression;
                        _output.Append(" => ");
                        GenerateExpression(accessor.Body);
                        _output.AppendLine(";");
                    }
                    else if (accessor.Statements.Count > 0)
                    {
                        // Block-bodied accessor: get { ... }
                        _output.AppendLine();
                        Indent();
                        _output.AppendLine("{");
                        _indentLevel++;

                        foreach (var stmt in accessor.Statements)
                        {
                            GenerateStatement(stmt);
                        }

                        _indentLevel--;
                        Indent();
                        _output.AppendLine("}");
                    }
                    else
                    {
                        // Auto-implemented accessor
                        _output.AppendLine(";");
                    }
                }

                _indentLevel--;
                Indent();
                _output.AppendLine("}");
            }
            else if (propDecl.Initializer != null)
            {
                // Property with initializer
                _output.Append(" = ");
                GenerateExpression(propDecl.Initializer);
                _output.AppendLine(";");
            }
            else
            {
                // Simple auto property
                _output.AppendLine(" { get; set; }");
            }
        }

        /// <summary>
        /// Generates the field declaration using the specified field decl
        /// </summary>
        /// <param name="fieldDecl">The field decl</param>
        private void GenerateFieldDeclaration(FieldDeclaration fieldDecl)
        {
            // Emit attributes for fields
            GenerateAttributes(fieldDecl.Attributes);

            Indent();

            // Generate modifiers
            if (fieldDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", fieldDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("private "); // Default to private for fields
            }

            var fieldType = fieldDecl.Type != null ? ConvertType(fieldDecl.Type) : "object";
            _output.Append($"{fieldType} {fieldDecl.Name}");

            if (fieldDecl.Initializer != null)
            {
                _output.Append(" = ");
                GenerateExpression(fieldDecl.Initializer);
            }

            _output.AppendLine(";");
        }



        /// <summary>
        /// Generates the variable declaration using the specified var decl
        /// </summary>
        /// <param name="varDecl">The var decl</param>
        private void GenerateVariableDeclaration(VariableDeclaration varDecl)
        {
            Indent();

            if (varDecl.IsConstant)
            {
                _output.Append("const ");
            }
            else
            {
                _output.Append("var ");
            }

            _output.Append(varDecl.Name);

            if (varDecl.Initializer != null)
            {
                _output.Append(" = ");
                GenerateExpression(varDecl.Initializer);
            }

            _output.AppendLine(";");
        }

        /// <summary>
        /// Generates the function declaration using the specified func decl
        /// </summary>
        /// <param name="funcDecl">The func decl</param>
        private void GenerateFunctionDeclaration(FunctionDeclaration funcDecl)
        {
            // Check if function has external or dotnetfunc attribute - completely skip generation
            var hasExternalAttribute = funcDecl.Attributes.Any(attr => attr.IsExternal);
            var hasDotNetFuncAttribute = funcDecl.Attributes.Any(attr => attr.IsDotNetFunc);

            if (hasDotNetFuncAttribute || hasExternalAttribute)
            {
                _diagnostics.ReportInfo($"Skipping code generation for external function: {funcDecl.Name}");
                return; // Completely skip generating this function
            }

            // Handle qualified function names - these should use [dotnetfunc] attribute
            if (funcDecl.Name.Contains('.'))
            {
                _diagnostics.ReportWarning($"Qualified function name '{funcDecl.Name}' should use [dotnetfunc] attribute for .NET methods");
                return; // Skip generating this as well
            }

            // Emit attributes for functions
            GenerateAttributes(funcDecl.Attributes);

            Indent();

            // Generate modifiers
            if (funcDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", funcDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("public static "); // Default to public static for functions
            }

            // Return type
            if (funcDecl.ReturnType != null)
            {
                _output.Append(ConvertType(funcDecl.ReturnType));
            }
            else
            {
                _output.Append("void");
            }

            _output.Append($" {funcDecl.Name}");
            
            // Emit generic parameters for functions
            if (funcDecl.GenericParameters != null && funcDecl.GenericParameters.Count > 0)
            {
                _output.Append("<");
                _output.Append(string.Join(", ", funcDecl.GenericParameters));
                _output.Append(">");
            }

            _output.Append("(");

            // Parameters
            for (int i = 0; i < funcDecl.Parameters.Count; i++)
            {
                var param = funcDecl.Parameters[i];
                if (i > 0) _output.Append(", ");

                var paramType = param.Type != null ? ConvertType(param.Type) : "object";
                _output.Append($"{paramType} {param.Name}");
            }

            _output.AppendLine(")");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var stmt in funcDecl.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();
        }

        /// <summary>
        /// Generates the if statement using the specified if stmt
        /// </summary>
        /// <param name="ifStmt">The if stmt</param>
        private void GenerateIfStatement(IfStatement ifStmt)
        {
            Indent();
            _output.Append("if (");
            GenerateExpression(ifStmt.Condition);
            _output.AppendLine(")");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var stmt in ifStmt.ThenBranch)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");

            if (ifStmt.ElseBranch != null)
            {
                Indent();
                _output.AppendLine("else");
                Indent();
                _output.AppendLine("{");
                _indentLevel++;

                foreach (var stmt in ifStmt.ElseBranch)
                {
                    GenerateStatement(stmt);
                }

                _indentLevel--;
                Indent();
                _output.AppendLine("}");
            }
        }

        /// <summary>
        /// Generates the while statement using the specified while stmt
        /// </summary>
        /// <param name="whileStmt">The while stmt</param>
        private void GenerateWhileStatement(WhileStatement whileStmt)
        {
            Indent();
            _output.Append("while (");
            GenerateExpression(whileStmt.Condition);
            _output.AppendLine(")");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var stmt in whileStmt.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
        }

        /// <summary>
        /// Generates the for statement using the specified for stmt
        /// </summary>
        /// <param name="forStmt">The for stmt</param>
        private void GenerateForStatement(ForStatement forStmt)
        {
            // Detect μHigh for-in loop (for var i in expr { ... })
            if (!string.IsNullOrEmpty(forStmt.IteratorVariable) && forStmt.IterableExpression != null)
            {
                Indent();
                _output.Append("foreach (var ");
                _output.Append(forStmt.IteratorVariable);
                _output.Append(" in ");
                GenerateExpression(forStmt.IterableExpression);
                _output.AppendLine(")");
                Indent();
                _output.AppendLine("{");
                _indentLevel++;

                foreach (var stmt in forStmt.Body)
                {
                    GenerateStatement(stmt);
                }

                _indentLevel--;
                Indent();
                _output.AppendLine("}");
                return;
            }

            Indent();
            _output.Append("for (");

            // Initializer
            if (forStmt.Initializer is VariableDeclaration varDecl)
            {
                _output.Append("var ");
                _output.Append(varDecl.Name);
                if (varDecl.Initializer != null)
                {
                    _output.Append(" = ");
                    GenerateExpression(varDecl.Initializer);
                }
            }
            else if (forStmt.Initializer != null)
            {
                GenerateStatement(forStmt.Initializer);
                if (_output.Length > 0 && _output[_output.Length - 1] == ';')
                    _output.Length--;
            }

            _output.Append("; ");

            // Condition
            if (forStmt.Condition != null)
            {
                GenerateExpression(forStmt.Condition);
            }

            _output.Append("; ");

            // Increment
            if (forStmt.Increment is ExpressionStatement exprStmt)
            {
                GenerateExpression(exprStmt.Expression);
            }
            else if (forStmt.Increment != null)
            {
                GenerateStatement(forStmt.Increment);
                if (_output.Length > 0 && _output[_output.Length - 1] == ';')
                    _output.Length--;
            }

            _output.AppendLine(")");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var stmt in forStmt.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
        }

        /// <summary>
        /// Generates the range iterable using the specified range expr
        /// </summary>
        /// <param name="rangeExpr">The range expr</param>
        private void GenerateRangeIterable(RangeExpression rangeExpr)
        {
            if (rangeExpr.IsSimpleRange && rangeExpr.End != null)
            {
                // Generate Enumerable.Range(0, n)
                _output.Append("Enumerable.Range(0, ");
                GenerateExpression(rangeExpr.End);
                _output.Append(")");
            }
            else if (rangeExpr.Start != null && rangeExpr.End != null)
            {
                // Generate Enumerable.Range(start, end - start)
                _output.Append("Enumerable.Range(");
                GenerateExpression(rangeExpr.Start);
                _output.Append(", ");
                GenerateExpression(rangeExpr.End);
                _output.Append(" - ");
                GenerateExpression(rangeExpr.Start);
                _output.Append(")");
            }
            else
            {
                _output.Append("new int[0]"); // Empty range fallback
            }
        }

        /// <summary>
        /// Generates the expression using the specified expression
        /// </summary>
        /// <param name="expression">The expression</param>
        private void GenerateExpression(ASTNode expression)
        {
            switch (expression)
            {
                case ThisExpression:
                    _output.Append("this");
                    break;
                case MemberAccessExpression memberExpr:
                    GenerateExpression(memberExpr.Object);
                    _output.Append($".{memberExpr.MemberName}");
                    break;
                case QualifiedIdentifierExpression qualifiedExpr:
                    _output.Append(qualifiedExpr.Name);
                    break;
                case BinaryExpression binExpr:
                    GenerateExpression(binExpr.Left);
                    _output.Append($" {ConvertOperator(binExpr.Operator)} ");
                    GenerateExpression(binExpr.Right);
                    break;
                case UnaryExpression unaryExpr:
                    _output.Append(ConvertOperator(unaryExpr.Operator));
                    GenerateExpression(unaryExpr.Operand);
                    break;
                case LiteralExpression litExpr:
                    GenerateLiteral(litExpr);
                    break;
                case IdentifierExpression identifierExpr:
                    _output.Append(identifierExpr.Name);
                    break;
                case AssignmentExpression assignExpr:
                    GenerateExpression(assignExpr.Target);
                    _output.Append($" {ConvertOperator(assignExpr.Operator)} ");
                    GenerateExpression(assignExpr.Value);
                    break;
                case ConstructorCallExpression constructorExpr:
                    _output.Append($"new {constructorExpr.ClassName}");
                    
                    // Handle generic type arguments if present
                    if (constructorExpr.ClassName.Contains('<'))
                    {
                        // Generic constructor call - type already included in ClassName
                    }
                    
                    _output.Append("(");
                    for (int i = 0; i < constructorExpr.Arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(constructorExpr.Arguments[i]);
                    }
                    _output.Append(")");
                    break;
                case RangeExpression rangeExpr:
                    GenerateRangeIterable(rangeExpr);
                    break;
                case CallExpression callExpr:
                    if (callExpr.Function is IdentifierExpression funcIdExpr)
                    {
                        var functionName = funcIdExpr.Name;
                        GenerateFunctionCall(functionName, callExpr.Arguments);
                    }
                    else if (callExpr.Function is QualifiedIdentifierExpression qualifiedFuncExpr)
                    {
                        var functionName = qualifiedFuncExpr.Name;
                        GenerateFunctionCall(functionName, callExpr.Arguments);
                    }
                    else if (callExpr.Function is MemberAccessExpression memberAccessExpr)
                    {
                        // Handle method calls like object.Method()
                        GenerateExpression(memberAccessExpr.Object);
                        _output.Append($".{memberAccessExpr.MemberName}(");
                        for (int i = 0; i < callExpr.Arguments.Count; i++)
                        {
                            if (i > 0) _output.Append(", ");
                            GenerateExpression(callExpr.Arguments[i]);
                        }
                        _output.Append(")");
                    }
                    else
                    {
                        GenerateExpression(callExpr.Function);
                        _output.Append("(");
                        for (int i = 0; i < callExpr.Arguments.Count; i++)
                        {
                            if (i > 0) _output.Append(", ");
                            GenerateExpression(callExpr.Arguments[i]);
                        }
                        _output.Append(")");
                    }
                    break;

                case ArrayExpression arrayExpr:
                    GenerateArrayExpression(arrayExpr);
                    break;

                case LambdaExpression lambdaExpr:
                    GenerateLambdaExpression(lambdaExpr);
                    break;

                case BlockExpression blockExpr:
                    GenerateBlockExpression(blockExpr);
                    break;
                case MatchExpression matchExpr:
                    GenerateMatchExpression(matchExpr);
                    break;
                case IndexExpression indexExpr:
                    GenerateExpression(indexExpr.Object);
                    _output.Append("[");
                    GenerateExpression(indexExpr.Index);
                    _output.Append("]");
                    break;
            }
        }

        /// <summary>
        /// Generates the lambda expression using the specified lambda expr
        /// </summary>
        /// <param name="lambdaExpr">The lambda expr</param>
        private void GenerateLambdaExpression(LambdaExpression lambdaExpr)
        {
            // Generate parameter list
            if (lambdaExpr.Parameters.Count == 1 && lambdaExpr.Parameters[0].Type == null)
            {
                // Single parameter without type: x => expression
                _output.Append(lambdaExpr.Parameters[0].Name);
            }
            else
            {
                // Multiple parameters or typed parameters: (x, y) => expression
                _output.Append("(");
                for (int i = 0; i < lambdaExpr.Parameters.Count; i++)
                {
                    if (i > 0) _output.Append(", ");

                    var param = lambdaExpr.Parameters[i];
                    if (param.Type != null)
                    {
                        _output.Append($"{ConvertType(param.Type)} {param.Name}");
                    }
                    else
                    {
                        _output.Append(param.Name);
                    }
                }
                _output.Append(")");
            }

            _output.Append(" => ");

            // Generate body
            if (lambdaExpr.IsExpressionLambda && lambdaExpr.Body != null)
            {
                // Expression lambda: x => x + 1
                GenerateExpression(lambdaExpr.Body);
            }
            else if (lambdaExpr.IsBlockLambda)
            {
                // Block lambda: x => { return x + 1; }
                _output.AppendLine();
                Indent();
                _output.AppendLine("{");
                _indentLevel++;

                foreach (var stmt in lambdaExpr.Statements)
                {
                    GenerateStatement(stmt);
                }

                _indentLevel--;
                Indent();
                _output.Append("}");
            }
        }

        /// <summary>
        /// Generates the block expression using the specified block expr
        /// </summary>
        /// <param name="blockExpr">The block expr</param>
        private void GenerateBlockExpression(BlockExpression blockExpr)
        {
            _output.AppendLine();
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var stmt in blockExpr.Statements)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.Append("}");
        }

        /// <summary>
        /// Generates the match statement using the specified match stmt
        /// </summary>
        /// <param name="matchStmt">The match stmt</param>
        private void GenerateMatchStatement(MatchStatement matchStmt)
        {
            Indent();
            _output.Append("switch (");
            GenerateExpression(matchStmt.Value);
            _output.AppendLine(")");
            Indent();
            _output.AppendLine("{");
            _indentLevel++;

            foreach (var arm in matchStmt.Arms)
            {
                if (arm.IsDefault)
                {
                    Indent();
                    _output.AppendLine("default:");
                }
                else
                {
                    // Handle multiple patterns as separate case labels
                    foreach (var pattern in arm.Patterns)
                    {
                        if (pattern is IdentifierExpression idExpr && idExpr.Name == "_")
                        {
                            // add underscore patterns as default case
                            Indent();
                            _output.AppendLine("default:");
                            continue;
                        }
                        Indent();
                        _output.Append("case ");
                        GenerateExpression(pattern);
                        _output.AppendLine(":");
                    }
                }

                _indentLevel++;

                // Handle both expression and block forms
                if (arm.Result is BlockExpression blockExpr)
                {
                    // Generate block statements directly
                    foreach (var stmt in blockExpr.Statements)
                    {
                        GenerateStatement(stmt);
                    }
                }
                else
                {
                    // Generate single expression
                    Indent();
                    GenerateExpression(arm.Result);
                    _output.AppendLine(";");
                }

                Indent();
                _output.AppendLine("break;");
                _indentLevel--;
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
        }

        /// <summary>
        /// Generates the array expression using the specified array expr
        /// </summary>
        /// <param name="arrayExpr">The array expr</param>
        private void GenerateArrayExpression(ArrayExpression arrayExpr)
        {
            // Check if we have an explicit array type
            if (!string.IsNullOrEmpty(arrayExpr.ArrayType))
            {
                _output.Append($"new {arrayExpr.ArrayType} {{ ");
            }
            else if (!string.IsNullOrEmpty(arrayExpr.ElementType))
            {
                _output.Append($"new {ConvertType(arrayExpr.ElementType)}[] {{ ");
            }
            else
            {
                _output.Append("new[] { ");
            }

            for (int i = 0; i < arrayExpr.Elements.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                GenerateExpression(arrayExpr.Elements[i]);
            }
            _output.Append(" }");
        }

        /// <summary>
        /// Generates the match expression using the specified match expr
        /// </summary>
        /// <param name="matchExpr">The match expr</param>
        private void GenerateMatchExpression(MatchExpression matchExpr)
        {
            // Check if any arm uses block form - if so, we need different handling
            var hasBlockArms = matchExpr.Arms.Any(arm => arm.Result is BlockExpression);

            if (hasBlockArms)
            {
                // Convert to immediately invoked function expression for blocks
                _output.Append("((Func<object>)(() => ");
                _output.AppendLine();
                Indent();
                _output.Append("switch (");
                GenerateExpression(matchExpr.Value);
                _output.AppendLine(")");
                Indent();
                _output.AppendLine("{");
                _indentLevel++;

                foreach (var arm in matchExpr.Arms)
                {
                    if (arm.IsDefault)
                    {
                        Indent();
                        _output.AppendLine("default:");
                    }
                    else
                    {
                        foreach (var pattern in arm.Patterns)
                        {
                            if (pattern is IdentifierExpression idExpr && idExpr.Name == "_")
                            {
                                Indent();
                                _output.AppendLine("default:");
                                continue;
                            }
                            Indent();
                            _output.Append("case ");
                            GenerateExpression(pattern);
                            _output.AppendLine(":");
                        }
                    }

                    _indentLevel++;

                    if (arm.Result is BlockExpression blockExpr)
                    {
                        // Generate block with return
                        foreach (var stmt in blockExpr.Statements)
                        {
                            GenerateStatement(stmt);
                        }
                        // Ensure we have a return for the last statement if it's an expression
                        if (blockExpr.Statements.Count > 0 &&
                            blockExpr.Statements.Last() is ExpressionStatement lastExpr)
                        {
                            Indent();
                            _output.Append("return ");
                            GenerateExpression(lastExpr.Expression);
                            _output.AppendLine(";");
                        }
                        else
                        {
                            // If no expression, just return null
                            Indent();
                            _output.AppendLine("return null;");
                        }
                    }
                    else
                    {
                        Indent();
                        _output.Append("return ");
                        GenerateExpression(arm.Result);
                        _output.AppendLine(";");
                    }

                    _indentLevel--;
                }

                _indentLevel--;
                Indent();
                _output.AppendLine("}");
                _output.Append("))()");
            }
            else
            {
                // Use C# 8.0+ switch expression syntax for expression-only arms
                _output.Append("(");
                GenerateExpression(matchExpr.Value);
                _output.Append(" switch");
                _output.AppendLine();
                Indent();
                _output.AppendLine("{");
                _indentLevel++;

                foreach (var arm in matchExpr.Arms)
                {
                    Indent();

                    if (arm.IsDefault)
                    {
                        _output.Append("_ => ");
                        GenerateExpression(arm.Result);
                        _output.AppendLine(",");
                    }
                    else
                    {
                        // Handle multiple patterns with 'or' syntax
                        if (arm.Patterns.Count > 1)
                        {
                            for (int i = 0; i < arm.Patterns.Count; i++)
                            {
                                if (i > 0) _output.Append(" or ");
                                GenerateExpression(arm.Patterns[i]);
                            }
                        }
                        else if (arm.Patterns.Count == 1)
                        {
                            GenerateExpression(arm.Patterns[0]);
                        }

                        _output.Append(" => ");
                        GenerateExpression(arm.Result);
                        _output.AppendLine(",");
                    }
                }

                _indentLevel--;
                Indent();
                _output.Append("})");
            }
        }

        /// <summary>
        /// Generates the function call using the specified function name
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="arguments">The arguments</param>
        private void GenerateFunctionCall(string functionName, List<Expression> arguments)
        {
            // Handle special function names that are C# keywords

            // this is now a function inside the standard libarary
            // if (functionName == "println")
            // {
            //     functionName = "Console.WriteLine"; // Map println to Console.WriteLine
            // }

            // Handle qualified method calls (e.g., Console.WriteLine)
            if (functionName.Contains('.'))
            {
                _output.Append(functionName);
            }
            else
            {
                _output.Append(functionName);
            }

            _output.Append("(");
            for (int i = 0; i < arguments.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                GenerateExpression(arguments[i]);
            }
            _output.Append(")");
        }

        /// <summary>
        /// Generates the literal using the specified literal
        /// </summary>
        /// <param name="literal">The literal</param>
        private void GenerateLiteral(LiteralExpression literal)
        {
            switch (literal.Value)
            {
                case string s:
                    _output.Append($"\"{s}\"");
                    break;
                case bool b:
                    _output.Append(b.ToString().ToLower());
                    break;
                default:
                    _output.Append(literal.Value);
                    break;
            }
        }

        /// <summary>
        /// Converts the operator using the specified op
        /// </summary>
        /// <param name="op">The op</param>
        /// <returns>The string</returns>
        private string ConvertOperator(TokenType op)
        {
            return op switch
            {
                TokenType.Plus => "+",
                TokenType.Minus => "-",
                TokenType.Multiply => "*",
                TokenType.Divide => "/",
                TokenType.Modulo => "%",
                TokenType.Assign => "=",
                TokenType.PlusAssign => "+=",
                TokenType.MinusAssign => "-=",
                TokenType.MultiplyAssign => "*=",
                TokenType.DivideAssign => "/=",
                TokenType.Equal => "==",
                TokenType.NotEqual => "!=",
                TokenType.Less => "<",
                TokenType.Greater => ">",
                TokenType.LessEqual => "<=",
                TokenType.GreaterEqual => ">=",
                TokenType.And => "&&",
                TokenType.Or => "||",
                TokenType.Not => "!",
                TokenType.Increment => "++",
                TokenType.Decrement => "--",
                _ => op.ToString()
            };
        }

        /// <summary>
        /// Generates the type alias using the specified alias
        /// </summary>
        /// <param name="alias">The alias</param>
        private void GenerateTypeAlias(TypeAliasDeclaration alias)
        {
            Indent();
            _output.Append("using ");
            if (alias.Modifiers.Count > 0)
                _output.Append(string.Join(" ", alias.Modifiers) + " ");
            _output.Append($"{alias.Name} = ");
            _output.Append(ConvertTypeAnnotation(alias.Type));
            _output.AppendLine(";");
        }

        // Convert TypeAnnotation to C# type string
        /// <summary>
        /// Converts the type annotation using the specified type ann
        /// </summary>
        /// <param name="typeAnn">The type ann</param>
        /// <returns>The string</returns>
        private string ConvertTypeAnnotation(TypeAnnotation typeAnn)
        {
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

        /// <summary>
        /// Converts the type using the specified type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The string</returns>
        private string ConvertType(string type)
        {
            // Handle array types first
            if (type.EndsWith("[]"))
            {
                var elementType = type.Substring(0, type.Length - 2);
                return $"{ConvertType(elementType)}[]";
            }

            // First try reflection to see if it's a known .NET type
            if (_typeResolver?.TryResolveType(type, out var reflectedType) == true)
            {
                // Use the actual .NET type name
                return GetCSharpTypeName(reflectedType);
            }

            // Handle generic types with reflection
            if (_typeResolver?.IsGenericType(type) == true)
            {
                if (_typeResolver.TryResolveGenericType(type, out var genericType))
                {
                    return GetCSharpTypeName(genericType);
                }
            }

            // Handle generic types manually if reflection fails
            if (type.Contains('<') && type.Contains('>'))
            {
                var genericMatch = System.Text.RegularExpressions.Regex.Match(type, @"^(\w+)<(.+)>$");
                if (genericMatch.Success)
                {
                    var baseType = genericMatch.Groups[1].Value;
                    var typeArgs = genericMatch.Groups[2].Value;
                    var typeArgsList = typeArgs.Split(',').Select(t => ConvertType(t.Trim())).ToList();

                    // Fallback to manual mapping for common types
                    var convertedBaseType = baseType switch
                    {
                        "array" => $"{typeArgsList[0]}[]",
                        "list" => $"List<{string.Join(", ", typeArgsList)}>",
                        "map" => typeArgsList.Count >= 2 ? $"Dictionary<{typeArgsList[0]}, {typeArgsList[1]}>" : "Dictionary<object, object>",
                        "set" => $"HashSet<{typeArgsList[0]}>",
                        _ => $"{baseType}<{string.Join(", ", typeArgsList)}>"
                    };

                    return convertedBaseType;
                }
            }

            // Emit generic type parameters as-is (e.g., T, U, V)
            if (type.Length == 1 && char.IsUpper(type[0]))
                return type;
            if (type.StartsWith("T") && type.Length <= 10 && char.IsUpper(type[0]))
                return type;

            // Try to resolve as a simple type through reflection
            if (_typeResolver?.TryResolveType(type, out var simpleType) == true)
            {
                return GetCSharpTypeName(simpleType);
            }
            // Fallback to manual mapping for μHigh-specific types
            return type switch
            {
                "int" => "int",
                "float" => "double",
                "string" => "string",
                "bool" => "bool",
                "void" => "void",
                // ...existing code...
                _ => type // <-- emit custom type names as-is
            };
        }

        /// <summary>
        /// Gets the c sharp type name using the specified type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The string</returns>
        private string GetCSharpTypeName(Type type)
        {
            // Handle special C# type names
            if (type == typeof(int)) return "int";
            if (type == typeof(double)) return "double";
            if (type == typeof(float)) return "float";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(void)) return "void";
            if (type == typeof(object)) return "object";

            // Handle generic types
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var typeArgs = type.GetGenericArguments();

                if (genericTypeDef == typeof(List<>))
                {
                    return $"List<{GetCSharpTypeName(typeArgs[0])}>";
                }
                if (genericTypeDef == typeof(Dictionary<,>))
                {
                    return $"Dictionary<{GetCSharpTypeName(typeArgs[0])}, {GetCSharpTypeName(typeArgs[1])}>";
                }

                // Generic type with multiple arguments
                var argNames = typeArgs.Select(GetCSharpTypeName);
                return $"{type.Name.Split('`')[0]}<{string.Join(", ", argNames)}>";
            }

            // Array types
            if (type.IsArray)
            {
                return GetCSharpTypeName(type.GetElementType()) + "[]";
            }

            // Use full name for other types
            return type.FullName ?? type.Name;
        }

        /// <summary>
        /// Indents this instance
        /// </summary>
        private void Indent()
        {
            _output.Append(new string('\t', _indentLevel));
        }
    }
}
