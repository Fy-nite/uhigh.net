using System.Text;
using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;
using uhigh.Net.Parser;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// ObjectIR v2 TextIR code generator for μHigh programs
    /// Generates human-readable ObjectIR code conforming to the ObjectIR v2 specification
    /// </summary>
    public class ObjectIRGenerator : ICodeGenerator
    {
        private readonly StringBuilder _output = new();
        private int _indentLevel = 0;
        private DiagnosticsReporter _diagnostics = new();
        private CodeGeneratorConfig? _config;
        private readonly HashSet<string> _usings = new();
        private string _moduleName = "UhighModule";
        private string _moduleVersion = "1.0.0";
        private int _labelCounter = 0;
        private readonly Stack<string> _loopLabels = new();
        private readonly Dictionary<string, int> _localVariables = new();
        private int _localVariableCounter = 0;

        public CodeGeneratorInfo Info => new()
        {
            Name = "ObjectIR v2 Code Generator",
            Description = "Generates ObjectIR v2 TextIR code from μHigh programs",
            Version = "1.0.0",
            SupportedFeatures = new() { "classes", "interfaces", "methods", "fields", "control-flow" },
            RequiredDependencies = new() { "ObjectIR.Core" }
        };

        public string TargetName => "objectir";
        public string FileExtension => ".oir";

        public void Initialize(CodeGeneratorConfig config, DiagnosticsReporter diagnostics)
        {
            _config = config;
            _diagnostics = diagnostics;
            _moduleName = config.RootNamespace ?? "UhighModule";
            diagnostics.ReportInfo($"Initialized ObjectIR generator for module: {_moduleName}");
        }

        public bool CanGenerate(Program program, DiagnosticsReporter diagnostics)
        {
            // ObjectIR supports most OOP features
            return true;
        }

        public string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;
            _labelCounter = 0;
            _loopLabels.Clear();

            if (rootNamespace != null) _moduleName = rootNamespace;

            _diagnostics.ReportInfo("Starting ObjectIR TextIR generation");

            // Generate module header
            GenerateModuleHeader();

            // Process imports
            ProcessImports(program);

            // Generate type declarations
            GenerateTypeDeclarations(program);

            _diagnostics.ReportInfo("ObjectIR TextIR generation completed");
            return _output.ToString();
        }

        public string GenerateCombined(List<Program> programs, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;

            if (rootNamespace != null) _moduleName = rootNamespace;

            GenerateModuleHeader();

            foreach (var program in programs)
            {
                ProcessImports(program);
                GenerateTypeDeclarations(program);
            }

            return _output.ToString();
        }

        public string GenerateWithoutUsings(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            return Generate(program, diagnostics, rootNamespace, className);
        }

        public HashSet<string> GetCollectedUsings()
        {
            return new HashSet<string>(_usings);
        }

        private void GenerateModuleHeader()
        {
            _output.AppendLine($"// ObjectIR v2 TextIR - Generated from μHigh");
            _output.AppendLine($"// Generated at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _output.AppendLine();
            _output.AppendLine($"module {_moduleName} version {_moduleVersion}");
            _output.AppendLine();
        }

        private void ProcessImports(Program program)
        {
            foreach (var stmt in program.Statements.OfType<ImportStatement>())
            {
                _usings.Add(stmt.ClassName);
            }
        }

        private void GenerateTypeDeclarations(Program program)
        {
            var hasProgramClass = false;

            foreach (var stmt in program.Statements)
            {
                switch (stmt)
                {
                    case ClassDeclaration classDecl:
                        GenerateClassDeclaration(classDecl);
                        break;
                    case InterfaceDeclaration interfaceDecl:
                        GenerateInterfaceDeclaration(interfaceDecl);
                        break;
                    case EnumDeclaration enumDecl:
                        GenerateEnumDeclaration(enumDecl);
                        break;
                    case NamespaceDeclaration namespaceDecl:
                        // Process members inside the namespace
                        foreach (var member in namespaceDecl.Members)
                        {
                            if (member is ClassDeclaration classDecl)
                                GenerateClassDeclaration(classDecl);
                            else if (member is InterfaceDeclaration interfaceDecl)
                                GenerateInterfaceDeclaration(interfaceDecl);
                            else if (member is EnumDeclaration enumDecl)
                                GenerateEnumDeclaration(enumDecl);
                            else if (member is FunctionDeclaration funcDecl)
                            {
                                if (!hasProgramClass)
                                {
                                    _output.AppendLine("public class Program {");
                                    _indentLevel++;
                                    hasProgramClass = true;
                                }
                                GenerateFunctionAsMethod(funcDecl);
                            }
                        }
                        break;
                    case FunctionDeclaration funcDecl:
                        // Top-level functions become static methods in a Program class
                        if (!hasProgramClass)
                        {
                            _output.AppendLine("public class Program {");
                            _indentLevel++;
                            hasProgramClass = true;
                        }
                        GenerateFunctionAsMethod(funcDecl);
                        break;
                }
            }

            // Close the Program class if we opened it
            if (hasProgramClass)
            {
                _indentLevel--;
                _output.AppendLine("}");
                _output.AppendLine();
            }
        }

        private void GenerateClassDeclaration(ClassDeclaration classDecl)
        {
            var accessMod = classDecl.IsPublic ? "public" : "private";

            _output.Append($"{accessMod} class {classDecl.Name}");

            if (!string.IsNullOrEmpty(classDecl.BaseClass))
            {
                _output.Append($" : {classDecl.BaseClass}");
            }

            _output.AppendLine(" {");
            _indentLevel++;

            // Generate fields
            foreach (var member in classDecl.Members.OfType<FieldDeclaration>())
            {
                GenerateFieldDeclaration(member);
            }

            // Generate constructors and methods
            foreach (var member in classDecl.Members)
            {
                if (member is MethodDeclaration method)
                {
                    if (method.IsConstructor)
                    {
                        GenerateConstructorDeclaration(method);
                    }
                    else
                    {
                        GenerateMethodDeclaration(method);
                    }
                }
                else if (member is FunctionDeclaration func)
                {
                    GenerateFunctionDeclarationAsMember(func);
                }
            }

            _indentLevel--;
            _output.AppendLine("}");
            _output.AppendLine();
        }

        private void GenerateInterfaceDeclaration(InterfaceDeclaration interfaceDecl)
        {
            _output.AppendLine($"public interface {interfaceDecl.Name} {{");
            _indentLevel++;

            foreach (var member in interfaceDecl.Members.OfType<MethodDeclaration>())
            {
                WriteLine($"method {member.Name}({GenerateParameters(member.Parameters)}) -> {MapType(member.ReturnType)}");
            }

            _indentLevel--;
            _output.AppendLine("}");
            _output.AppendLine();
        }

        private void GenerateEnumDeclaration(EnumDeclaration enumDecl)
        {
            _output.AppendLine($"public enum {enumDecl.Name} {{");
            _indentLevel++;

            foreach (var member in enumDecl.Members)
            {
                WriteLine($"field {member.Name}: int");
            }

            _indentLevel--;
            _output.AppendLine("}");
            _output.AppendLine();
        }

        private void GenerateFieldDeclaration(FieldDeclaration field)
        {
            var staticMod = field.IsStatic ? "static " : "";
            var fieldType = MapType(field.Type);
            WriteLine($"{staticMod}field {field.Name}: {fieldType}");
        }

        private void GenerateConstructorDeclaration(MethodDeclaration ctor)
        {
            var parameters = GenerateParameters(ctor.Parameters);
            WriteLine($"constructor({parameters}) {{");
            _indentLevel++;

            GenerateMethodBody(ctor.Body);

            _indentLevel--;
            WriteLine("}");
            _output.AppendLine();
        }

        private void GenerateMethodDeclaration(MethodDeclaration method)
        {
            var staticMod = method.IsStatic ? "static " : "";
            var virtualMod = method.Modifiers.Contains("virtual") ? "virtual " : "";
            var overrideMod = method.Modifiers.Contains("override") ? "override " : "";
            var parameters = GenerateParameters(method.Parameters);
            var returnType = MapType(method.ReturnType);

            WriteLine($"{staticMod}{virtualMod}{overrideMod}method {method.Name}({parameters}) -> {returnType} {{");
            _indentLevel++;

            var locals = CollectLocalVariables(method.Body);
            foreach (var (name, type) in locals)
            {
                WriteLine($"local {name}: {MapType(type)}");
            }

            GenerateMethodBody(method.Body);

            _indentLevel--;
            WriteLine("}");
            _output.AppendLine();
        }

        private void GenerateFunctionDeclarationAsMember(FunctionDeclaration func)
        {
            var staticMod = func.Modifiers.Contains("static") ? "static " : "";
            var virtualMod = func.Modifiers.Contains("virtual") ? "virtual " : "";
            var overrideMod = func.Modifiers.Contains("override") ? "override " : "";
            var parameters = GenerateParameters(func.Parameters);
            var returnType = MapType(func.ReturnType);

            WriteLine($"{staticMod}{virtualMod}{overrideMod}method {func.Name}({parameters}) -> {returnType} {{");
            _indentLevel++;

            var locals = CollectLocalVariables(func.Body);
            foreach (var (name, type) in locals)
            {
                WriteLine($"local {name}: {MapType(type)}");
            }

            GenerateMethodBody(func.Body);

            _indentLevel--;
            WriteLine("}");
            _output.AppendLine();
        }

        private void GenerateFunctionAsMethod(FunctionDeclaration func)
        {
            var parameters = GenerateParameters(func.Parameters);
            var returnType = MapType(func.ReturnType);

            WriteLine($"static method {func.Name}({parameters}) -> {returnType} {{");
            _indentLevel++;

            var locals = CollectLocalVariables(func.Body);
            foreach (var (name, type) in locals)
            {
                WriteLine($"local {name}: {MapType(type)}");
            }

            GenerateMethodBody(func.Body);

            _indentLevel--;
            WriteLine("}");
            _output.AppendLine();
        }

        private string GenerateParameters(List<Parameter> parameters)
        {
            return string.Join(", ", parameters.Select(p => $"{p.Name}: {MapType(p.Type)}"));
        }

        private void GenerateMethodBody(List<Statement> body)
        {
            _localVariables.Clear();
            _localVariableCounter = 0;

            // Register local variables
            var locals = CollectLocalVariables(body);
            foreach (var (name, _) in locals)
            {
                _localVariables[name] = _localVariableCounter++;
            }

            foreach (var stmt in body)
            {
                GenerateStatement(stmt);
            }

            // Ensure methods end with ret
            if (body.Count == 0 || body.Last() is not ReturnStatement)
            {
                WriteLine("ret");
            }
        }

        private void GenerateStatement(Statement stmt)
        {
            switch (stmt)
            {
                case ExpressionStatement exprStmt:
                    GenerateExpression(exprStmt.Expression);
                    if (ShouldPopResult(exprStmt.Expression))
                    {
                        WriteLine("pop");
                    }
                    break;

                case VariableDeclaration varDecl:
                    if (varDecl.Initializer != null)
                    {
                        GenerateExpression(varDecl.Initializer);
                        var localIndex = GetLocalIndex(varDecl.Name);
                        WriteLine($"stloc {localIndex}");
                    }
                    break;

                case ReturnStatement returnStmt:
                    if (returnStmt.Value != null)
                    {
                        GenerateExpression(returnStmt.Value);
                    }
                    WriteLine("ret");
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

                case BreakStatement:
                    WriteLine("break");
                    break;

                case ContinueStatement:
                    WriteLine("continue");
                    break;

                default:
                    WriteLine($"// TODO: Unsupported statement type: {stmt.GetType().Name}");
                    break;
            }
        }

        private void GenerateExpression(Expression expr)
        {
            switch (expr)
            {
                case LiteralExpression literal:
                    GenerateLiteral(literal);
                    break;

                case IdentifierExpression identifier:
                    GenerateIdentifier(identifier);
                    break;

                case BinaryExpression binary:
                    GenerateBinaryExpression(binary);
                    break;

                case UnaryExpression unary:
                    GenerateUnaryExpression(unary);
                    break;

                case CallExpression call:
                    GenerateCallExpression(call);
                    break;

                case MemberAccessExpression memberAccess:
                    GenerateMemberAccess(memberAccess);
                    break;

                case IndexExpression indexExpr:
                    GenerateArrayAccess(indexExpr);
                    break;

                case ConstructorCallExpression newExpr:
                    GenerateNewExpression(newExpr);
                    break;

                case ArrayExpression arrayExpr:
                    GenerateArrayCreation(arrayExpr);
                    break;

                case AssignmentExpression assignment:
                    GenerateAssignmentExpression(assignment);
                    break;

                default:
                    WriteLine($"ldnull  // TODO: Unsupported expression: {expr.GetType().Name}");
                    break;
            }
        }

        private void GenerateLiteral(LiteralExpression literal)
        {
            switch (literal.Value)
            {
                case null:
                    WriteLine("ldnull");
                    break;
                case int i:
                    WriteLine($"ldc.i4 {i}");
                    break;
                case long l:
                    WriteLine($"ldc.i8 {l}");
                    break;
                case float f:
                    WriteLine($"ldc.r4 {f}");
                    break;
                case double d:
                    WriteLine($"ldc.r8 {d}");
                    break;
                case bool b:
                    WriteLine($"ldc.i4 {(b ? 1 : 0)}");
                    break;
                case string s:
                    WriteLine($"ldstr \"{EscapeString(s)}\"");
                    break;
                default:
                    WriteLine($"ldstr \"{literal.Value}\"");
                    break;
            }
        }

        private void GenerateIdentifier(IdentifierExpression identifier)
        {
            var localIndex = GetLocalIndex(identifier.Name);
            if (localIndex >= 0)
            {
                WriteLine($"ldloc {localIndex}");
            }
            else
            {
                WriteLine($"ldarg {identifier.Name}");
            }
        }

        private void GenerateBinaryExpression(BinaryExpression binary)
        {
            GenerateExpression(binary.Left);
            GenerateExpression(binary.Right);

            var opcode = binary.Operator switch
            {
                TokenType.Plus => "add",
                TokenType.Minus => "sub",
                TokenType.Multiply => "mul",
                TokenType.Divide => "div",
                TokenType.Modulo => "rem",
                TokenType.Equal => "ceq",
                TokenType.NotEqual => "cne",
                TokenType.Less => "clt",
                TokenType.LessEqual => "cle",
                TokenType.Greater => "cgt",
                TokenType.GreaterEqual => "cge",
                TokenType.And => "and",
                TokenType.Or => "or",
                _ => "nop"
            };

            WriteLine(opcode);
        }

        private void GenerateUnaryExpression(UnaryExpression unary)
        {
            GenerateExpression(unary.Operand);

            switch (unary.Operator)
            {
                case TokenType.Minus:
                    WriteLine("neg");
                    break;
                case TokenType.Not:
                    WriteLine("not");
                    break;
            }
        }

        private void GenerateCallExpression(CallExpression call)
        {
            // Generate arguments first
            foreach (var arg in call.Arguments)
            {
                GenerateExpression(arg);
            }

            // Build the full qualified method name
            var methodName = BuildFullMethodName(call.Function);
            
            // Infer parameter types from arguments
            var paramTypes = call.Arguments.Select(InferExpressionType).ToArray();
            var paramTypeList = string.Join(", ", paramTypes);
            
            var signature = $"{methodName}({paramTypeList})";

            WriteLine($"call {signature}");
        }

        private string BuildFullMethodName(Expression expr)
        {
            switch (expr)
            {
                case QualifiedIdentifierExpression qualified:
                    // Qualified identifier like "Console.WriteLine" or "System.Console.WriteLine"
                    return qualified.Name;
                
                case MemberAccessExpression member:
                    // Recursively build the full path (e.g., Console.WriteLine)
                    var objName = BuildFullMethodName(member.Object);
                    return string.IsNullOrEmpty(objName) ? member.MemberName : $"{objName}.{member.MemberName}";
                
                case IdentifierExpression identifier:
                    return identifier.Name;
                
                default:
                    return "Unknown";
            }
        }

        private string InferExpressionType(Expression expr)
        {
            switch (expr)
            {
                case LiteralExpression literal:
                    return literal.Value switch
                    {
                        string => "string",
                        int => "int32",
                        long => "int64",
                        float => "float32",
                        double => "float64",
                        bool => "bool",
                        null => "object",
                        _ => "object"
                    };
                
                case IdentifierExpression:
                case MemberAccessExpression:
                case CallExpression:
                case BinaryExpression:
                case UnaryExpression:
                    // For complex expressions, we'd need type analysis
                    // For now, default to object
                    return "object";
                
                case ArrayExpression:
                    return "array";
                
                default:
                    return "object";
            }
        }

        private void GenerateMemberAccess(MemberAccessExpression memberAccess)
        {
            GenerateExpression(memberAccess.Object);
            WriteLine($"ldfld {memberAccess.MemberName}");
        }

        private void GenerateArrayAccess(IndexExpression indexExpr)
        {
            GenerateExpression(indexExpr.Object);
            GenerateExpression(indexExpr.Index);
            WriteLine("ldelem");
        }

        private void GenerateNewExpression(ConstructorCallExpression newExpr)
        {
            foreach (var arg in newExpr.Arguments)
            {
                GenerateExpression(arg);
            }

            WriteLine($"newobj {newExpr.ClassName}");
        }

        private void GenerateArrayCreation(ArrayExpression arrayExpr)
        {
            WriteLine($"ldc.i4 {arrayExpr.Elements.Count}");
            WriteLine("newarr");

            for (int i = 0; i < arrayExpr.Elements.Count; i++)
            {
                WriteLine("dup");
                WriteLine($"ldc.i4 {i}");
                GenerateExpression(arrayExpr.Elements[i]);
                WriteLine("stelem");
            }
        }

        private void GenerateAssignmentExpression(AssignmentExpression assignment)
        {
            if (assignment.Target is IdentifierExpression identifier)
            {
                GenerateExpression(assignment.Value);
                var localIndex = GetLocalIndex(identifier.Name);
                if (localIndex >= 0)
                {
                    WriteLine($"stloc {localIndex}");
                }
                else
                {
                    WriteLine($"starg {identifier.Name}");
                }
            }
            else if (assignment.Target is MemberAccessExpression memberAccess)
            {
                GenerateExpression(memberAccess.Object);
                GenerateExpression(assignment.Value);
                WriteLine($"stfld {memberAccess.MemberName}");
            }
            else if (assignment.Target is IndexExpression indexExpr)
            {
                GenerateExpression(indexExpr.Object);
                GenerateExpression(indexExpr.Index);
                GenerateExpression(assignment.Value);
                WriteLine("stelem");
            }
        }

        private void GenerateIfStatement(IfStatement ifStmt)
        {
            // Generate condition with readable format: if (stack)
            GenerateExpression(ifStmt.Condition);
            WriteLine($"if (stack) {{");
            _indentLevel++;

            foreach (var stmt in ifStmt.ThenBranch)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;

            if (ifStmt.ElseBranch != null && ifStmt.ElseBranch.Count > 0)
            {
                WriteLine($"}} else {{");
                _indentLevel++;
                foreach (var stmt in ifStmt.ElseBranch)
                {
                    GenerateStatement(stmt);
                }
                _indentLevel--;
            }

            WriteLine("}");
        }

        private void GenerateWhileStatement(WhileStatement whileStmt)
        {
            var loopEnd = NextLabel("while_end");
            _loopLabels.Push(loopEnd);

            // Readable format: while (expression)
            WriteLine($"while (expression) {{");
            _indentLevel++;

            GenerateExpression(whileStmt.Condition);

            WriteLine("} do {");

            foreach (var stmt in whileStmt.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            WriteLine("}");

            _loopLabels.Pop();
        }

        private void GenerateForStatement(ForStatement forStmt)
        {
            // Initialize
            if (forStmt.Initializer != null)
            {
                GenerateStatement(forStmt.Initializer);
            }

            var loopEnd = NextLabel("for_end");
            _loopLabels.Push(loopEnd);

            WriteLine($"while (expression) {{");
            _indentLevel++;

            // Condition
            if (forStmt.Condition != null)
            {
                GenerateExpression(forStmt.Condition);
            }
            else
            {
                WriteLine("ldc.i4 1  // true");
            }

            WriteLine("} do {");

            // Body
            foreach (var stmt in forStmt.Body)
            {
                GenerateStatement(stmt);
            }

            // Increment
            if (forStmt.Increment != null)
            {
                GenerateStatement(forStmt.Increment);
            }

            _indentLevel--;
            WriteLine("}");

            _loopLabels.Pop();
        }

        private string MapType(string? type)
        {
            if (string.IsNullOrEmpty(type)) return "void";

            return type switch
            {
                "int" => "int32",
                "long" => "int64",
                "float" => "float32",
                "double" => "float64",
                "bool" => "bool",
                "string" => "string",
                "void" => "void",
                _ => type
            };
        }

        private List<(string name, string type)> CollectLocalVariables(List<Statement> body)
        {
            var locals = new List<(string, string)>();
            var localSet = new HashSet<string>();

            void CollectFromStatement(Statement stmt)
            {
                if (stmt is VariableDeclaration varDecl)
                {
                    if (localSet.Add(varDecl.Name))
                    {
                        locals.Add((varDecl.Name, varDecl.Type ?? "object"));
                    }
                }
                else if (stmt is IfStatement ifStmt)
                {
                    foreach (var s in ifStmt.ThenBranch)
                        CollectFromStatement(s);
                    if (ifStmt.ElseBranch != null)
                    {
                        foreach (var s in ifStmt.ElseBranch)
                            CollectFromStatement(s);
                    }
                }
                else if (stmt is WhileStatement whileStmt)
                {
                    foreach (var s in whileStmt.Body)
                        CollectFromStatement(s);
                }
                else if (stmt is ForStatement forStmt)
                {
                    if (forStmt.Initializer != null)
                        CollectFromStatement(forStmt.Initializer);
                    foreach (var s in forStmt.Body)
                        CollectFromStatement(s);
                }
            }

            foreach (var stmt in body)
            {
                CollectFromStatement(stmt);
            }

            return locals;
        }

        private bool ShouldPopResult(Expression expr)
        {
            return expr is CallExpression or BinaryExpression or UnaryExpression;
        }

        private int GetLocalIndex(string name)
        {
            if (_localVariables.TryGetValue(name, out var index))
            {
                return index;
            }
            return -1;
        }

        private string NextLabel(string prefix)
        {
            return $"{prefix}_{_labelCounter++}";
        }

        private string EscapeString(string str)
        {
            return str
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
        }

        private void WriteLine(string line)
        {
            _output.AppendLine(new string(' ', _indentLevel * 4) + line);
        }
    }
}
