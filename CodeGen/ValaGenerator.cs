using System.Text;
using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;
using uhigh.Net.Parser;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// Vala code generator for μHigh programs
    /// </summary>
    public class ValaGenerator : ICodeGenerator
    {
        private readonly StringBuilder _output = new();
        private int _indentLevel = 0;
        private DiagnosticsReporter _diagnostics = new();
        private CodeGeneratorConfig? _config;
        private readonly HashSet<string> _usings = new();

        public CodeGeneratorInfo Info => new()
        {
            Name = "Vala Code Generator",
            Description = "Generates Vala code from μHigh programs with GObject support",
            Version = "1.0.0",
            SupportedFeatures = new() { "classes", "functions", "interfaces", "generics", "async", "properties" },
            RequiredDependencies = new() { "vala >= 0.48", "glib-2.0", "gobject-2.0" }
        };

        public string TargetName => "vala";
        public string FileExtension => ".vala";

        public void Initialize(CodeGeneratorConfig config, DiagnosticsReporter diagnostics)
        {
            _config = config;
            _diagnostics = diagnostics;
            diagnostics.ReportInfo("Initialized Vala generator");
        }

        public bool CanGenerate(Program program, DiagnosticsReporter diagnostics)
        {
            // Check for features not well supported in Vala
            var unsupportedFeatures = new List<string>();

            foreach (var stmt in program.Statements)
            {
                if (stmt is ClassDeclaration classDecl)
                {
                    // Check for complex generic constraints
                    if (classDecl.GenericParameters?.Count > 2)
                    {
                        diagnostics.ReportWarning($"Complex generics may not be fully supported in Vala: {classDecl.Name}");
                    }
                }
            }

            return unsupportedFeatures.Count == 0;
        }

        public string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();

            _diagnostics.ReportInfo("Starting Vala code generation");

            // Process imports and generate using statements
            ProcessImports(program);

            // Generate program content
            GenerateProgramContent(program);

            _diagnostics.ReportInfo($"Vala code generation completed. Generated {_output.ToString().Split('\n').Length} lines");
            return _output.ToString();
        }

        public string GenerateCombined(List<Program> programs, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();

            _diagnostics.ReportInfo("Starting combined Vala code generation");

            // Process all imports
            foreach (var program in programs)
            {
                ProcessImports(program);
            }

            // Generate using statements
            foreach (var usingDirective in _usings.OrderBy(u => u))
            {
                _output.AppendLine($"using {usingDirective};");
            }
            if (_usings.Any()) _output.AppendLine();

            // Generate content from all programs
            foreach (var program in programs)
            {
                GenerateProgramContentWithoutUsings(program);
            }

            return _output.ToString();
        }

        public string GenerateWithoutUsings(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;

            GenerateProgramContentWithoutUsings(program);
            return _output.ToString();
        }

        public HashSet<string> GetCollectedUsings()
        {
            return new HashSet<string>(_usings);
        }

        private void ProcessImports(Program program)
        {
            foreach (var stmt in program.Statements.OfType<ImportStatement>())
            {
                if (stmt.AssemblyName.EndsWith(".vapi") || stmt.AssemblyName.EndsWith(".gir"))
                {
                    _usings.Add(stmt.ClassName);
                }
                else
                {
                    // Map common .NET namespaces to Vala equivalents
                    var valaNamespace = stmt.ClassName switch
                    {
                        "System" => "GLib",
                        "System.Collections.Generic" => "Gee",
                        "System.IO" => "GLib",
                        "System.Threading.Tasks" => "GLib",
                        _ => stmt.ClassName
                    };
                    _usings.Add(valaNamespace);
                }
            }

            // Add default Vala namespaces
            _usings.Add("GLib");
        }

        private void GenerateProgramContent(Program program)
        {
            // Generate using statements
            foreach (var usingDirective in _usings.OrderBy(u => u))
            {
                _output.AppendLine($"using {usingDirective};");
            }
            if (_usings.Any()) _output.AppendLine();

            GenerateProgramContentWithoutUsings(program);
        }

        private void GenerateProgramContentWithoutUsings(Program program)
        {
            var hasMainFunction = program.Statements.OfType<FunctionDeclaration>().Any(f => f.Name == "main");

            foreach (var statement in program.Statements.Where(s => !(s is ImportStatement)))
            {
                GenerateStatement(statement);
            }

            // Generate main function if needed
            if (hasMainFunction)
            {
                var mainFunc = program.Statements.OfType<FunctionDeclaration>().First(f => f.Name == "main");
                GenerateMainFunction(mainFunc);
            }
        }

        private void GenerateStatement(ASTNode statement)
        {
            switch (statement)
            {
                case FunctionDeclaration funcDecl when funcDecl.Name != "main":
                    GenerateFunctionDeclaration(funcDecl);
                    break;
                case ClassDeclaration classDecl:
                    GenerateClassDeclaration(classDecl);
                    break;
                case VariableDeclaration varDecl:
                    GenerateVariableDeclaration(varDecl);
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
                case ExpressionStatement exprStmt:
                    Indent();
                    GenerateExpression(exprStmt.Expression);
                    _output.AppendLine(";");
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
                case NamespaceDeclaration nsDecl:
                    Indent();
                    _output.AppendLine($"namespace {nsDecl.Name} {{");
                    _indentLevel++;
                    foreach (var member in nsDecl.Members)
                    {
                        GenerateStatement(member);
                    }
                    _indentLevel--;
                    Indent();
                    _output.AppendLine("}");
                    _output.AppendLine();
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown statement type for Vala: {statement.GetType().Name}");
                    break;
            }
        }

        private void GenerateFunctionDeclaration(FunctionDeclaration funcDecl)
        {
            Indent();
            
            // Generate modifiers
            if (funcDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", funcDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("public ");
            }

            // Return type
            var returnType = funcDecl.ReturnType != null ? ConvertType(funcDecl.ReturnType) : "void";
            _output.Append($"{returnType} {funcDecl.Name}");

            // Generic parameters
            if (funcDecl.GenericParameters != null && funcDecl.GenericParameters.Count > 0)
            {
                _output.Append($"<{string.Join(", ", funcDecl.GenericParameters)}>");
            }

            _output.Append("(");
            
            // Parameters
            for (int i = 0; i < funcDecl.Parameters.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                var param = funcDecl.Parameters[i];
                var paramType = param.Type != null ? ConvertType(param.Type) : "Object";
                _output.Append($"{paramType} {param.Name}");
            }
            
            _output.AppendLine(") {");
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

        private void GenerateMainFunction(FunctionDeclaration mainFunc)
        {
            _output.AppendLine("int main(string[] args) {");
            _indentLevel++;

            foreach (var stmt in mainFunc.Body)
            {
                GenerateStatement(stmt);
            }

            Indent();
            _output.AppendLine("return 0;");
            _indentLevel--;
            _output.AppendLine("}");
        }

        private void GenerateClassDeclaration(ClassDeclaration classDecl)
        {
            Indent();

            // Generate modifiers
            if (classDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", classDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("public ");
            }

            _output.Append($"class {classDecl.Name}");

            // Generic parameters
            if (classDecl.GenericParameters != null && classDecl.GenericParameters.Count > 0)
            {
                _output.Append($"<{string.Join(", ", classDecl.GenericParameters)}>");
            }

            // Base class
            if (classDecl.BaseClass != null)
            {
                _output.Append($" : {ConvertType(classDecl.BaseClass)}");
            }
            else
            {
                _output.Append(" : Object"); // All Vala classes inherit from Object
            }

            _output.AppendLine(" {");
            _indentLevel++;

            foreach (var member in classDecl.Members)
            {
                GenerateStatement(member);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();
        }

        private void GenerateMethodDeclaration(MethodDeclaration methodDecl)
        {
            Indent();

            // Generate modifiers
            if (methodDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", methodDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("public ");
            }

            if (methodDecl.IsConstructor)
            {
                _output.Append("construct");
            }
            else
            {
                var returnType = methodDecl.ReturnType != null ? ConvertType(methodDecl.ReturnType) : "void";
                _output.Append($"{returnType} {methodDecl.Name}");
            }

            _output.Append("(");

            // Parameters
            for (int i = 0; i < methodDecl.Parameters.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                var param = methodDecl.Parameters[i];
                var paramType = param.Type != null ? ConvertType(param.Type) : "Object";
                _output.Append($"{paramType} {param.Name}");
            }

            _output.AppendLine(") {");
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

        private void GenerateFieldDeclaration(FieldDeclaration fieldDecl)
        {
            Indent();

            // Generate modifiers
            if (fieldDecl.Modifiers.Count > 0)
            {
                _output.Append(string.Join(" ", fieldDecl.Modifiers) + " ");
            }
            else
            {
                _output.Append("private ");
            }

            var fieldType = fieldDecl.Type != null ? ConvertType(fieldDecl.Type) : "Object";
            _output.Append($"{fieldType} {fieldDecl.Name}");

            if (fieldDecl.Initializer != null)
            {
                _output.Append(" = ");
                GenerateExpression(fieldDecl.Initializer);
            }

            _output.AppendLine(";");
        }

        private void GeneratePropertyDeclaration(PropertyDeclaration propDecl)
        {
            Indent();
            _output.Append("public ");

            var propType = propDecl.Type != null ? ConvertType(propDecl.Type) : "Object";
            _output.Append($"{propType} {propDecl.Name}");

            if (propDecl.Accessors.Count > 0)
            {
                _output.AppendLine(" {");
                _indentLevel++;

                foreach (var accessor in propDecl.Accessors)
                {
                    Indent();
                    _output.Append(accessor.Type.ToLower());

                    if (accessor.Body != null || accessor.Statements.Count > 0)
                    {
                        _output.AppendLine(" {");
                        _indentLevel++;

                        if (accessor.Body != null)
                        {
                            Indent();
                            _output.Append("return ");
                            GenerateExpression(accessor.Body);
                            _output.AppendLine(";");
                        }
                        else
                        {
                            foreach (var stmt in accessor.Statements)
                            {
                                GenerateStatement(stmt);
                            }
                        }

                        _indentLevel--;
                        Indent();
                        _output.AppendLine("}");
                    }
                    else
                    {
                        _output.AppendLine(";");
                    }
                }

                _indentLevel--;
                Indent();
                _output.AppendLine("}");
            }
            else
            {
                _output.AppendLine(" { get; set; }");
            }
        }

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

        private void GenerateIfStatement(IfStatement ifStmt)
        {
            Indent();
            _output.Append("if (");
            GenerateExpression(ifStmt.Condition);
            _output.AppendLine(") {");
            _indentLevel++;

            foreach (var stmt in ifStmt.ThenBranch)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.Append("}");

            if (ifStmt.ElseBranch != null && ifStmt.ElseBranch.Any())
            {
                _output.AppendLine(" else {");
                _indentLevel++;

                foreach (var stmt in ifStmt.ElseBranch)
                {
                    GenerateStatement(stmt);
                }

                _indentLevel--;
                Indent();
                _output.AppendLine("}");
            }
            else
            {
                _output.AppendLine();
            }
        }

        private void GenerateWhileStatement(WhileStatement whileStmt)
        {
            Indent();
            _output.Append("while (");
            GenerateExpression(whileStmt.Condition);
            _output.AppendLine(") {");
            _indentLevel++;

            foreach (var stmt in whileStmt.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
        }

        private void GenerateForStatement(ForStatement forStmt)
        {
            Indent();

            if (forStmt.IsForInLoop)
            {
                _output.Append($"foreach (var {forStmt.IteratorVariable} in ");
                GenerateExpression(forStmt.IterableExpression!);
                _output.AppendLine(") {");
            }
            else
            {
                _output.Append("for (");
                
                if (forStmt.Initializer != null)
                {
                    if (forStmt.Initializer is VariableDeclaration initVar)
                    {
                        _output.Append($"var {initVar.Name}");
                        if (initVar.Initializer != null)
                        {
                            _output.Append(" = ");
                            GenerateExpression(initVar.Initializer);
                        }
                    }
                }
                _output.Append("; ");

                if (forStmt.Condition != null)
                    GenerateExpression(forStmt.Condition);
                _output.Append("; ");

                if (forStmt.Increment != null)
                {
                    if (forStmt.Increment is ExpressionStatement exprStmt)
                    {
                        GenerateExpression(exprStmt.Expression);
                    }
                }

                _output.AppendLine(") {");
            }

            _indentLevel++;

            foreach (var stmt in forStmt.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
        }

        private void GenerateExpression(ASTNode expression)
        {
            switch (expression)
            {
                case BinaryExpression binExpr:
                    GenerateExpression(binExpr.Left);
                    _output.Append($" {ConvertOperator(binExpr.Operator)} ");
                    GenerateExpression(binExpr.Right);
                    break;
                case LiteralExpression litExpr:
                    GenerateLiteral(litExpr);
                    break;
                case IdentifierExpression identifierExpr:
                    _output.Append(identifierExpr.Name);
                    break;
                case CallExpression callExpr:
                    if (callExpr.Function is IdentifierExpression funcIdExpr)
                    {
                        var functionName = MapBuiltInFunction(funcIdExpr.Name);
                        _output.Append(functionName);
                        _output.Append("(");
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
                case ConstructorCallExpression constructorExpr:
                    _output.Append($"new {ConvertType(constructorExpr.ClassName)}(");
                    for (int i = 0; i < constructorExpr.Arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(constructorExpr.Arguments[i]);
                    }
                    _output.Append(")");
                    break;
                case AssignmentExpression assignExpr:
                    GenerateExpression(assignExpr.Target);
                    _output.Append($" {ConvertOperator(assignExpr.Operator)} ");
                    GenerateExpression(assignExpr.Value);
                    break;
                case MemberAccessExpression memberExpr:
                    GenerateExpression(memberExpr.Object);
                    _output.Append($".{memberExpr.MemberName}");
                    break;
                case ArrayExpression arrayExpr:
                    _output.Append("{");
                    for (int i = 0; i < arrayExpr.Elements.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(arrayExpr.Elements[i]);
                    }
                    _output.Append("}");
                    break;
                case QualifiedIdentifierExpression qualifiedIdExpr:
                    // Output the qualified name, e.g., Namespace.Class.Member
                    _output.Append(string.Join(".", qualifiedIdExpr.GetParts()));
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown expression type for Vala: {expression.GetType().Name}");
                    break;
            }
        }

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
                case null:
                    _output.Append("null");
                    break;
                default:
                    _output.Append(literal.Value);
                    break;
            }
        }

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

        private string ConvertType(string type)
        {
            return type switch
            {
                "int" => "int",
                "float" => "double",
                "double" => "double",
                "string" => "string",
                "bool" => "bool",
                "void" => "void",
                "object" => "Object",
                "any" => "Object",
                // Generic collections
                "List" => "Gee.ArrayList",
                "Dictionary" => "Gee.HashMap",
                "Array" => "GenericArray",
                var name when name.StartsWith("List<") => name.Replace("List<", "Gee.ArrayList<").Replace(">", ">"),
                var name when name.StartsWith("Dictionary<") => name.Replace("Dictionary<", "Gee.HashMap<"),
                _ => type
            };
        }

        private string MapBuiltInFunction(string functionName)
        {
            return functionName switch
            {
                "print" => "print",
                "println" => "print",
                _ => functionName
            };
        }

        private void Indent()
        {
            _output.Append(new string(' ', _indentLevel * 4));
        }
    }
}
