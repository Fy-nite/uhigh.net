using System.Text;
using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;
using uhigh.Net.Parser;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// JavaScript code generator for μHigh programs
    /// </summary>
    public class JavaScriptGenerator : ICodeGenerator
    {
        private readonly StringBuilder _output = new();
        private int _indentLevel = 0;
        private DiagnosticsReporter _diagnostics = new();
        private CodeGeneratorConfig? _config;
        private readonly HashSet<string> _imports = new();

        public CodeGeneratorInfo Info => new()
        {
            Name = "JavaScript Code Generator",
            Description = "Generates modern JavaScript (ES6+) code from μHigh programs",
            Version = "1.0.0",
            SupportedFeatures = new() { "functions", "classes", "arrow-functions", "async", "modules" },
            RequiredDependencies = new() { "Node.js 16+" }
        };

        public string TargetName => "javascript";
        public string FileExtension => ".js";

        public void Initialize(CodeGeneratorConfig config, DiagnosticsReporter diagnostics)
        {
            _config = config;
            _diagnostics = diagnostics;
            diagnostics.ReportInfo("Initialized JavaScript generator");
        }

        public bool CanGenerate(Program program, DiagnosticsReporter diagnostics)
        {
            // Check for features not supported in JavaScript
            var unsupportedFeatures = new List<string>();

            // Check for static typing (JavaScript is dynamically typed)
            foreach (var stmt in program.Statements)
            {
                if (stmt is ClassDeclaration classDecl)
                {
                    // Check for properties with explicit types
                    foreach (var member in classDecl.Members)
                    {
                        if (member is PropertyDeclaration prop && prop.Type != null)
                        {
                            diagnostics.ReportWarning($"Explicit type annotations are ignored in JavaScript: {prop.Name}");
                        }
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
            _imports.Clear();

            _diagnostics.ReportInfo("Starting JavaScript code generation");

            // Generate imports
            ProcessImports(program);

            // Generate program content
            GenerateProgramContent(program);

            _diagnostics.ReportInfo($"JavaScript code generation completed. Generated {_output.ToString().Split('\n').Length} lines");
            return _output.ToString();
        }

        public string GenerateCombined(List<Program> programs, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;
            _imports.Clear();

            _diagnostics.ReportInfo("Starting combined JavaScript code generation");

            // Process all imports
            foreach (var program in programs)
            {
                ProcessImports(program);
            }

            // Generate imports
            foreach (var import in _imports.OrderBy(i => i))
            {
                _output.AppendLine(import);
            }
            if (_imports.Any()) _output.AppendLine();

            // Generate content from all programs
            foreach (var program in programs)
            {
                GenerateProgramContent(program);
            }

            return _output.ToString();
        }

        public string GenerateWithoutUsings(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            // JavaScript doesn't have "using" statements like C#, so just generate normally
            return Generate(program, diagnostics, rootNamespace, className);
        }

        public HashSet<string> GetCollectedUsings()
        {
            return new HashSet<string>(_imports);
        }

        private void ProcessImports(Program program)
        {
            foreach (var stmt in program.Statements.OfType<ImportStatement>())
            {
                if (stmt.AssemblyName.EndsWith(".js") || stmt.AssemblyName.EndsWith(".mjs"))
                {
                    _imports.Add($"import {{ {stmt.ClassName} }} from '{stmt.AssemblyName}';");
                }
                else
                {
                    // Handle Node.js built-in modules
                    _imports.Add($"const {stmt.ClassName} = require('{stmt.AssemblyName}');");
                }
            }
        }

        private void GenerateProgramContent(Program program)
        {
            foreach (var statement in program.Statements.Where(s => !(s is ImportStatement)))
            {
                GenerateStatement(statement);
            }
        }

        private void GenerateStatement(ASTNode statement)
        {
            switch (statement)
            {
                case FunctionDeclaration funcDecl:
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
                case NamespaceDeclaration nsDecl:
                    // JavaScript does not support namespaces; emit members at top level
                    foreach (var member in nsDecl.Members)
                    {
                        GenerateStatement(member);
                    }
                    break;
                case FieldDeclaration fieldDecl:
                    GenerateFieldDeclaration(fieldDecl);
                    break;
                case MethodDeclaration methodDecl:
                    GenerateMethodDeclaration(methodDecl);
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown statement type for JavaScript: {statement.GetType().Name}");
                    break;
            }
        }

        // Add support for field declarations in classes
        private void GenerateFieldDeclaration(FieldDeclaration fieldDecl)
        {
            // In JavaScript, fields are typically initialized in the constructor or as class fields (ES2022+)
            // We'll emit as a class field if inside a class, otherwise as a variable
            Indent();
            _output.Append(fieldDecl.IsStatic ? "static " : "");
            _output.Append(fieldDecl.Name);
            if (fieldDecl.Initializer != null)
            {
                _output.Append(" = ");
                GenerateExpression(fieldDecl.Initializer);
            }
            _output.AppendLine(";");
        }

        // Add support for method declarations in classes
        private void GenerateMethodDeclaration(MethodDeclaration methodDecl)
        {
            Indent();
            // Static methods
            if (methodDecl.IsStatic)
                _output.Append("static ");
            // Constructors
            if (methodDecl.IsConstructor)
            {
                _output.Append("constructor(");
            }
            else
            {
                _output.Append($"{methodDecl.Name}(");
            }
            for (int i = 0; i < methodDecl.Parameters.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                _output.Append(methodDecl.Parameters[i].Name);
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
        }

        private void GenerateFunctionDeclaration(FunctionDeclaration funcDecl)
        {
            Indent();
            // Emit type info as comment for custom types
            if (funcDecl.ReturnType != null || funcDecl.Parameters.Any(p => p.Type != null))
            {
                var paramTypes = string.Join(", ", funcDecl.Parameters.Select(p => $"{p.Name}: {p.Type ?? "any"}"));
                var retType = funcDecl.ReturnType ?? "any";
                _output.AppendLine($"// function {funcDecl.Name}({paramTypes}): {retType}");
            }
            
            if (funcDecl.Name == "main")
            {
                // Generate main function as immediately invoked
                _output.Append("(function main() {");
                _output.AppendLine();
                _indentLevel++;
                
                foreach (var stmt in funcDecl.Body)
                {
                    GenerateStatement(stmt);
                }
                
                _indentLevel--;
                Indent();
                _output.AppendLine("})();");
            }
            else
            {
                _output.Append($"function {funcDecl.Name}(");
                
                for (int i = 0; i < funcDecl.Parameters.Count; i++)
                {
                    if (i > 0) _output.Append(", ");
                    _output.Append(funcDecl.Parameters[i].Name);
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
            }
            _output.AppendLine();
        }

        private void GenerateClassDeclaration(ClassDeclaration classDecl)
        {
            Indent();
            // Emit generic parameters as comment (JS does not support generics)
            if (classDecl.GenericParameters != null && classDecl.GenericParameters.Count > 0)
            {
                _output.AppendLine($"// Generic class: {classDecl.Name}<{string.Join(", ", classDecl.GenericParameters)}>");
            }
            _output.AppendLine($"class {classDecl.Name} {{");
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

        private void GenerateVariableDeclaration(VariableDeclaration varDecl)
        {
            Indent();
            _output.Append(varDecl.IsConstant ? "const " : "let ");
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
                _output.Append($"for (const {forStmt.IteratorVariable} of ");
                GenerateExpression(forStmt.IterableExpression!);
                _output.AppendLine(") {");
            }
            else
            {
                _output.Append("for (");
                if (forStmt.Initializer != null)
                {
                    // Generate initializer inline
                    var initStr = "";
                    if (forStmt.Initializer is VariableDeclaration initVar)
                    {
                        initStr = $"let {initVar.Name} = ";
                        if (initVar.Initializer != null)
                        {
                            var prevOutput = _output.ToString();
                            var prevLength = _output.Length;
                            GenerateExpression(initVar.Initializer);
                            initStr += _output.ToString().Substring(prevLength);
                            _output.Length = prevLength;
                        }
                    }
                    _output.Append(initStr);
                }
                _output.Append("; ");

                if (forStmt.Condition != null)
                    GenerateExpression(forStmt.Condition);
                _output.Append("; ");

                if (forStmt.Increment != null)
                {
                    // Generate increment expression
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
                        // Map μHigh built-ins to JavaScript equivalents
                        var functionName = funcIdExpr.Name switch
                        {
                            "print" => "console.log",
                            "println" => "console.log",
                            _ => funcIdExpr.Name
                        };
                        _output.Append(functionName);
                    }
                    else
                    {
                        GenerateExpression(callExpr.Function);
                    }
                    
                    _output.Append("(");
                    for (int i = 0; i < callExpr.Arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(callExpr.Arguments[i]);
                    }
                    _output.Append(")");
                    break;
                case MemberAccessExpression memberExpr:
                    GenerateExpression(memberExpr.Object);
                    _output.Append($".{memberExpr.MemberName}");
                    break;
                case AssignmentExpression assignExpr:
                    GenerateExpression(assignExpr.Target);
                    _output.Append($" {ConvertOperator(assignExpr.Operator)} ");
                    GenerateExpression(assignExpr.Value);
                    break;
                case ConstructorCallExpression ctorExpr:
                    // new ClassName(arg1, arg2, ...)
                    _output.Append("new ");
                    _output.Append(ctorExpr.ClassName);
                    _output.Append("(");
                    for (int i = 0; i < ctorExpr.Arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(ctorExpr.Arguments[i]);
                    }
                    _output.Append(")");
                    break;
                case QualifiedIdentifierExpression qidExpr:
                    // Just output the qualified name (e.g., Namespace.Name)
                    _output.Append(qidExpr.Name);
                    break;
                case LambdaExpression lambdaExpr:
                    // Arrow function: (params) => { body }
                    _output.Append("(");
                    for (int i = 0; i < lambdaExpr.Parameters.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        _output.Append(lambdaExpr.Parameters[i].Name);
                    }
                    _output.Append(") => ");
                    if (lambdaExpr.IsExpressionLambda && lambdaExpr.Body != null)
                    {
                        GenerateExpression(lambdaExpr.Body);
                    }
                    else if (lambdaExpr.IsBlockLambda)
                    {
                        _output.Append("{");
                        _output.AppendLine();
                        _indentLevel++;
                        foreach (var stmt in lambdaExpr.Statements)
                        {
                            GenerateStatement(stmt);
                        }
                        _indentLevel--;
                        Indent();
                        _output.Append("}");
                    }
                    else
                    {
                        _output.Append("{}");
                    }
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown expression type for JavaScript: {expression.GetType().Name}");
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
                TokenType.Equal => "===", // Use strict equality in JavaScript
                TokenType.NotEqual => "!==",
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

        private void Indent()
        {
            _output.Append(new string(' ', _indentLevel * 2));
        }
    }

}
