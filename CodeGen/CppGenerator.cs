using uhigh.Net.Diagnostics;
using uhigh.Net.Parser;
using uhigh.Net.Lexer;
using System.Text;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// C++ code generator for μHigh programs
    /// </summary>
    public class CppGenerator : ICodeGenerator
    {
        private readonly StringBuilder _output = new();
        private int _indentLevel = 0;
        private DiagnosticsReporter _diagnostics = new();

        public CodeGeneratorInfo Info => new()
        {
            Name = "C++ Code Generator",
            Description = "Generates C++ code from μHigh programs",
            Version = "1.0.0",
            SupportedFeatures = new() { "classes", "functions" },
            RequiredDependencies = new() { "C++17+" }
        };

        public string TargetName => "cpp";
        public string FileExtension => ".cpp";

        public void Initialize(CodeGeneratorConfig config, DiagnosticsReporter diagnostics)
        {
            diagnostics.ReportInfo("Initialized C++ generator");
        }

        public bool CanGenerate(Program program, DiagnosticsReporter diagnostics)
        {
            // Accept all for now
            return true;
        }

        public string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;

            _output.AppendLine("// μHigh C++ code generator");
            _output.AppendLine("#include <iostream>");
            _output.AppendLine("#include <vector>");
            _output.AppendLine("#include <string>");
            _output.AppendLine("#include <map>");
            _output.AppendLine("using namespace std;");
            _output.AppendLine();

            // Collect all classes and functions from the program
            var classes = new List<ClassDeclaration>();
            var functions = new List<FunctionDeclaration>();
            CollectDeclarations(program.Statements, classes, functions);

            // Generate all classes first
            foreach (var cls in classes)
            {
                GenerateClass(cls);
            }

            // Generate all functions (except main)
            foreach (var func in functions.Where(f => f.Name != "main"))
            {
                GenerateFunction(func);
            }

            // Generate main function last
            var mainFunc = functions.FirstOrDefault(f => f.Name == "main");
            if (mainFunc != null)
            {
                GenerateMainFunction(mainFunc);
            }
            else
            {
                // If no main function found, check for top-level statements and create a main
                var topLevelStatements = program.Statements.Where(s => 
                    !(s is ClassDeclaration) && 
                    !(s is FunctionDeclaration) && 
                    !(s is NamespaceDeclaration) &&
                    !(s is ImportStatement) &&
                    !(s is IncludeStatement)).ToList();

                if (topLevelStatements.Any())
                {
                    GenerateMainFromTopLevel(topLevelStatements);
                }
            }

            return _output.ToString();
        }

        private void CollectDeclarations(List<Statement> statements, List<ClassDeclaration> classes, List<FunctionDeclaration> functions)
        {
            foreach (var stmt in statements)
            {
                switch (stmt)
                {
                    case ClassDeclaration cls:
                        classes.Add(cls);
                        break;
                    case FunctionDeclaration func:
                        functions.Add(func);
                        break;
                    case NamespaceDeclaration ns:
                        // Recursively collect from namespace members
                        CollectDeclarations(ns.Members, classes, functions);
                        break;
                    case ModuleDeclaration module:
                        // Recursively collect from module members
                        CollectDeclarations(module.Members, classes, functions);
                        break;
                }
            }
        }

        private void GenerateMainFromTopLevel(List<Statement> statements)
        {
            Indent();
            _output.AppendLine("int main() {");
            _indentLevel++;

            foreach (var stmt in statements)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("    return 0;");
            _output.AppendLine("}");
        }

        private void GenerateFunction(FunctionDeclaration func)
        {
            var retType = ConvertType(func.ReturnType ?? "void");
            
            Indent();
            _output.Append($"{retType} {func.Name}(");
            for (int i = 0; i < func.Parameters.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                var param = func.Parameters[i];
                var paramType = ConvertType(param.Type ?? "auto");
                _output.Append($"{paramType} {param.Name}");
            }
            _output.AppendLine(") {");
            _indentLevel++;

            foreach (var stmt in func.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("}");
            _output.AppendLine();
        }

        private void GenerateClass(ClassDeclaration cls)
        {
            Indent();
            _output.AppendLine($"class {cls.Name} {{");
            _output.AppendLine("public:");
            _indentLevel++;

            foreach (var member in cls.Members)
            {
                GenerateStatement(member);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("};");
            _output.AppendLine();
        }

        private void GenerateMainFunction(FunctionDeclaration mainFunc)
        {
            Indent();
            _output.AppendLine("int main() {");
            _indentLevel++;

            foreach (var stmt in mainFunc.Body)
            {
                GenerateStatement(stmt);
            }

            _indentLevel--;
            Indent();
            _output.AppendLine("    return 0;");
            _output.AppendLine("}");
        }

        private void GenerateStatement(ASTNode stmt)
        {
            switch (stmt)
            {
                case IfStatement ifStmt:
                    Indent();
                    _output.Append("if (");
                    GenerateExpression(ifStmt.Condition);
                    _output.AppendLine(") {");
                    _indentLevel++;
                    foreach (var s in ifStmt.ThenBranch)
                        GenerateStatement(s);
                    _indentLevel--;
                    Indent();
                    _output.AppendLine("}");
                    if (ifStmt.ElseBranch != null && ifStmt.ElseBranch.Any())
                    {
                        Indent();
                        _output.AppendLine("else {");
                        _indentLevel++;
                        foreach (var s in ifStmt.ElseBranch)
                            GenerateStatement(s);
                        _indentLevel--;
                        Indent();
                        _output.AppendLine("}");
                    }
                    break;
                case WhileStatement whileStmt:
                    Indent();
                    _output.Append("while (");
                    GenerateExpression(whileStmt.Condition);
                    _output.AppendLine(") {");
                    _indentLevel++;
                    foreach (var s in whileStmt.Body)
                        GenerateStatement(s);
                    _indentLevel--;
                    Indent();
                    _output.AppendLine("}");
                    break;
                case ForStatement forStmt:
                    Indent();
                    if (forStmt.IsForInLoop)
                    {
                        // Range-based for loop: for (auto item : container)
                        _output.Append($"for (auto {forStmt.IteratorVariable} : ");
                        GenerateExpression(forStmt.IterableExpression!);
                        _output.AppendLine(") {");
                    }
                    else
                    {
                        // Traditional for loop
                        _output.Append("for (");
                        if (forStmt.Initializer != null)
                        {
                            if (forStmt.Initializer is VariableDeclaration varDecl)
                            {
                                _output.Append($"auto {varDecl.Name}");
                                if (varDecl.Initializer != null)
                                {
                                    _output.Append(" = ");
                                    GenerateExpression(varDecl.Initializer);
                                }
                            }
                        }
                        _output.Append("; ");
                        if (forStmt.Condition != null)
                            GenerateExpression(forStmt.Condition);
                        _output.Append("; ");
                        if (forStmt.Increment is ExpressionStatement exprStmt)
                            GenerateExpression(exprStmt.Expression);
                        _output.AppendLine(") {");
                    }
                    _indentLevel++;
                    foreach (var s in forStmt.Body)
                        GenerateStatement(s);
                    _indentLevel--;
                    Indent();
                    _output.AppendLine("}");
                    break;
                case ExpressionStatement exprStatement:
                    Indent();
                    GenerateExpression(exprStatement.Expression);
                    _output.AppendLine(";");
                    break;
                case VariableDeclaration varDeclaration:
                    Indent();
                    var varType = ConvertType(varDeclaration.Type ?? "auto");
                    _output.Append($"{varType} {varDeclaration.Name}");
                    if (varDeclaration.Initializer != null)
                    {
                        _output.Append(" = ");
                        GenerateExpression(varDeclaration.Initializer);
                    }
                    _output.AppendLine(";");
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
                case FieldDeclaration fieldDecl:
                    Indent();
                    var fieldType = ConvertType(fieldDecl.Type ?? "auto");
                    _output.Append($"{fieldType} {fieldDecl.Name}");
                    if (fieldDecl.Initializer != null)
                    {
                        _output.Append(" = ");
                        GenerateExpression(fieldDecl.Initializer);
                    }
                    _output.AppendLine(";");
                    break;
                case MethodDeclaration methodDecl:
                    GenerateMethod(methodDecl);
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown statement type for C++: {stmt.GetType().Name}");
                    break;
            }
        }

        private void GenerateMethod(MethodDeclaration methodDecl)
        {
            Indent();
            var retType = ConvertType(methodDecl.ReturnType ?? "void");
            
            if (methodDecl.IsConstructor)
            {
                _output.Append($"{GetCurrentClassName()}(");
            }
            else
            {
                _output.Append($"{retType} {methodDecl.Name}(");
            }

            for (int i = 0; i < methodDecl.Parameters.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                var param = methodDecl.Parameters[i];
                var paramType = ConvertType(param.Type ?? "auto");
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
        }
        private void GenerateLiteral(LiteralExpression litExpr)
        {
            if (litExpr.Value == null)
            {
                _output.Append("nullptr");
                return;
            }

            switch (litExpr.Value)
            {
                case int i:
                    _output.Append(i.ToString());
                    break;
                case double d:
                    _output.Append(d.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case float f:
                    _output.Append(f.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    break;
                case bool b:
                    _output.Append(b ? "true" : "false");
                    break;
                case string s:
                    _output.Append($"\"{s.Replace("\"", "\\\"")}\"");
                    break;
                default:
                    _output.Append(litExpr.Value.ToString());
                    break;
            }
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
                case CallExpression callExpr:
                    if (callExpr.Function is IdentifierExpression funcIdExpr)
                    {
                        var functionName = funcIdExpr.Name switch
                        {
                            "println" => "cout",
                            "print" => "cout",
                            _ => funcIdExpr.Name
                        };

                        if (functionName == "cout")
                        {
                            _output.Append("cout");
                            foreach (var arg in callExpr.Arguments)
                            {
                                _output.Append(" << ");
                                GenerateExpression(arg);
                            }
                            if (funcIdExpr.Name == "println")
                                _output.Append(" << endl");
                        }
                        else
                        {
                            _output.Append(functionName);
                            _output.Append("(");
                            for (int i = 0; i < callExpr.Arguments.Count; i++)
                            {
                                if (i > 0) _output.Append(", ");
                                GenerateExpression(callExpr.Arguments[i]);
                            }
                            _output.Append(")");
                        }
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
                case MemberAccessExpression memberExpr:
                    GenerateExpression(memberExpr.Object);
                    _output.Append($".{memberExpr.MemberName}");
                    break;
                case IndexExpression indexExpr:
                    GenerateExpression(indexExpr.Object);
                    _output.Append("[");
                    GenerateExpression(indexExpr.Index);
                    _output.Append("]");
                    break;
                case ConstructorCallExpression ctorExpr:
                    GenerateConstructorCall(ctorExpr);
                    break;
                case QualifiedIdentifierExpression qualifiedExpr:
                    GenerateQualifiedIdentifier(qualifiedExpr);
                    break;
                case ArrayExpression arrayExpr:
                    _output.Append("{ ");
                    for (int i = 0; i < arrayExpr.Elements.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(arrayExpr.Elements[i]);
                    }
                    _output.Append(" }");
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown expression type for C++: {expression.GetType().Name}");
                    _output.Append("/* unknown expression */");
                    break;
            }
        }

        private void GenerateConstructorCall(ConstructorCallExpression ctorExpr)
        {
            // Convert class name to C++ equivalent
            var className = ConvertClassName(ctorExpr.ClassName);
            
            // Generate constructor call: ClassName(args...)
            _output.Append($"{className}(");
            for (int i = 0; i < ctorExpr.Arguments.Count; i++)
            {
                if (i > 0) _output.Append(", ");
                GenerateExpression(ctorExpr.Arguments[i]);
            }
            _output.Append(")");
        }

        private void GenerateQualifiedIdentifier(QualifiedIdentifierExpression qualifiedExpr)
        {
            // Handle common qualified identifiers
            var qualifiedName = qualifiedExpr.Name;
            
            // Convert common .NET qualified names to C++ equivalents
            var cppName = qualifiedName switch
            {
                "System.Console.WriteLine" => "cout",
                "System.Console.Write" => "cout",
                "System.Math.Abs" => "abs",
                "System.Math.Sqrt" => "sqrt",
                "System.Math.Pow" => "pow",
                "std.cout" => "cout",
                "std.endl" => "endl",
                _ => qualifiedName.Replace('.', ':')  // Convert to C++ scope resolution
            };

            _output.Append(cppName);
        }

        private string ConvertClassName(string className)
        {
            // Handle generic types and common conversions
            return className switch
            {
                var name when name.StartsWith("List<") => "vector" + name.Substring(4),
                var name when name.StartsWith("Dictionary<") => "map" + name.Substring(10),
                var name when name.StartsWith("Observable<") => "observable" + name.Substring(10),
                "String" => "string",
                "Object" => "void*",
                _ => className
            };
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
                "bool" => "bool",
                "string" => "string",
                "void" => "void",
                _ => "auto"
            };
        }

        private string GetCurrentClassName()
        {
            return "UnknownClass"; // This should be tracked properly in a real implementation
        }

        private void Indent()
        {
            _output.Append(new string(' ', _indentLevel * 4));
        }

        public string GenerateCombined(List<Program> programs, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            throw new NotImplementedException();
        }

        public string GenerateWithoutUsings(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            throw new NotImplementedException();
        }

        public HashSet<string> GetCollectedUsings()
        {
            throw new NotImplementedException();
        }
    }
}
