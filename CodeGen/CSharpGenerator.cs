using Wake.Net.Parser;
using Wake.Net.Lexer;
using Wake.Net.Diagnostics;
using System.Text;

namespace Wake.Net.CodeGen
{
    public class CSharpGenerator
    {
        private readonly StringBuilder _output = new();
        private int _indentLevel = 0;
        private readonly HashSet<string> _usings = new();
        private readonly Dictionary<string, string> _importMappings = new();
        private DiagnosticsReporter _diagnostics = new();
        private string _rootNamespace = "Generated";

        public string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();
            _importMappings.Clear();
            _rootNamespace = rootNamespace ?? "Generated";

            _diagnostics.ReportInfo("Starting C# code generation");

            // Process imports first
            ProcessImports(program);

            // Add using statements
            foreach (var usingDirective in _usings.OrderBy(u => u))
            {
                _output.AppendLine($"using {usingDirective};");
            }
            _output.AppendLine();

            // Generate program content
            GenerateProgramContent(program);

            _diagnostics.ReportInfo($"C# code generation completed. Generated {_output.ToString().Split('\n').Length} lines");
            return _output.ToString();
        }

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
        }

        private void ProcessImportStatement(ImportStatement import)
        {
            if (import.AssemblyName.EndsWith(".dll"))
            {
                // Custom assembly - add to mappings for later reference loading
                _importMappings[import.ClassName] = import.AssemblyName;
            }
            else
            {
                // System namespace
                var lastDotIndex = import.ClassName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    var namespaceToAdd = import.ClassName.Substring(0, lastDotIndex);
                    _usings.Add(namespaceToAdd);
                }
            }
        }

        private void GenerateProgramContent(Program program)
        {
            if (program.Statements == null) return;

            var hasNamespace = program.Statements.Any(s => s is NamespaceDeclaration);
            
            if (!hasNamespace)
            {
                // Generate namespace using project root namespace
                _output.AppendLine($"namespace {_rootNamespace}");
                _output.AppendLine("{");
                _indentLevel++;
                GenerateDefaultProgram(program);
                _indentLevel--;
                _output.AppendLine("}");
            }
            else
            {
                // Generate namespaces and top-level statements
                foreach (var statement in program.Statements.Where(s => !(s is ImportStatement)))
                {
                    GenerateStatement(statement);
                }
            }
        }

        private void GenerateDefaultProgram(Program program)
        {
            Indent();
            _output.AppendLine("public class Program");
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

        private void GenerateBuiltInFunctions()
        {
            // Math functions
            Indent();
            _output.AppendLine("public static double abs(double x) => Math.Abs(x);");
            Indent();
            _output.AppendLine("public static double sqrt(double x) => Math.Sqrt(x);");
            Indent();
            _output.AppendLine("public static double pow(double x, double y) => Math.Pow(x, y);");
            Indent();
            _output.AppendLine("public static double min(double x, double y) => Math.Min(x, y);");
            Indent();
            _output.AppendLine("public static double max(double x, double y) => Math.Max(x, y);");
            Indent();
            _output.AppendLine("private static Random _random = new Random();");
            Indent();
            _output.AppendLine("public static double random() => _random.NextDouble();");
            
            // String functions
            Indent();
            _output.AppendLine("public static int len(string s) => s.Length;");
            Indent();
            _output.AppendLine("public static int len<T>(T[] array) => array.Length;");
            Indent();
            _output.AppendLine("public static int len<T>(List<T> list) => list.Count;");
            Indent();
            _output.AppendLine("public static string uppercase(string s) => s.ToUpper();");
            Indent();
            _output.AppendLine("public static string lowercase(string s) => s.ToLower();");
            Indent();
            _output.AppendLine("public static string substring(string s, int start, int length) => s.Substring(start, length);");
            
            // Array functions
            Indent();
            _output.AppendLine("public static void append<T>(List<T> list, T item) => list.Add(item);");
            Indent();
            _output.AppendLine("public static T pop<T>(List<T> list, int index) { var item = list[index]; list.RemoveAt(index); return item; }");
            Indent();
            _output.AppendLine("public static void sort<T>(List<T> list) where T : IComparable<T> => list.Sort();");
            Indent();
            _output.AppendLine("public static void reverse<T>(List<T> list) => list.Reverse();");
            
            // Type conversion functions
            Indent();
            _output.AppendLine("public static int @int(string s) => int.Parse(s);");
            Indent();
            _output.AppendLine("public static int @int(double d) => (int)d;");
            Indent();
            _output.AppendLine("public static double @float(string s) => double.Parse(s);");
            Indent();
            _output.AppendLine("public static double @float(int i) => (double)i;");
            Indent();
            _output.AppendLine("public static string @string(object obj) => obj.ToString() ?? \"\";");
            Indent();
            _output.AppendLine("public static bool @bool(object obj) => obj != null && !obj.Equals(0) && !obj.Equals(\"\");");
            
            // Range function
            Indent();
            _output.AppendLine("public static IEnumerable<int> range(int count) => Enumerable.Range(0, count);");
            Indent();
            _output.AppendLine("public static IEnumerable<int> range(int start, int count) => Enumerable.Range(start, count);");
            
            // IO functions
            Indent();
            _output.AppendLine("public static void print(object value) => Console.WriteLine(value);");
            Indent();
            _output.AppendLine("public static string input() => Console.ReadLine() ?? \"\";");
            Indent();
            _output.AppendLine("public static void input(out string value) => value = Console.ReadLine() ?? \"\";");
            
            _output.AppendLine();
        }

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

        private void GenerateStatement(ASTNode statement)
        {
            switch (statement)
            {
                case ImportStatement:
                    // Already processed
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
                    GenerateReturnStatement(returnStmt);
                    break;
                case BreakStatement:
                    Indent();
                    _output.AppendLine("break;");
                    break;
                case ContinueStatement:
                    Indent();
                    _output.AppendLine("continue;");
                    break;
                case ExpressionStatement exprStmt:
                    Indent();
                    GenerateExpression(exprStmt.Expression);
                    _output.AppendLine(";");
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown statement type: {statement.GetType().Name}");
                    break;
            }
        }

        private void GenerateNamespaceDeclaration(NamespaceDeclaration nsDecl)
        {
            Indent();
            // Use fully qualified namespace with root namespace prefix
            var fullNamespace = $"{_rootNamespace}.{nsDecl.Name}";
            _output.AppendLine($"namespace {fullNamespace}");
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

        private void GenerateClassDeclaration(ClassDeclaration classDecl)
        {
            Indent();
            _output.Append("public class ");
            _output.Append(classDecl.Name);
            
            if (classDecl.BaseClass != null)
            {
                _output.Append($" : {classDecl.BaseClass}");
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
        }

        private void GenerateMethodDeclaration(MethodDeclaration methodDecl)
        {
            Indent();
            _output.Append("public ");
            
            if (methodDecl.IsConstructor)
            {
                // Constructor doesn't have return type
                _output.Append($"{GetCurrentClassName()}(");
            }
            else
            {
                var returnType = methodDecl.ReturnType != null ? ConvertType(methodDecl.ReturnType) : "void";
                _output.Append($"{returnType} {methodDecl.Name}(");
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

        private void GeneratePropertyDeclaration(PropertyDeclaration propDecl)
        {
            Indent();
            _output.Append("public ");
            
            var propType = propDecl.Type != null ? ConvertType(propDecl.Type) : "object";
            _output.Append($"{propType} {propDecl.Name}");
            
            if (propDecl.Initializer != null)
            {
                _output.Append(" = ");
                GenerateExpression(propDecl.Initializer);
            }
            
            _output.AppendLine(";");
        }

        private string GetCurrentClassName()
        {
            // This would need to track current class context
            // For simplicity, return a default name
            return "GeneratedClass";
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

        private void GenerateFunctionDeclaration(FunctionDeclaration funcDecl)
        {
            Indent();
            _output.Append("public static ");
            
            // Return type
            if (funcDecl.ReturnType != null)
            {
                _output.Append(ConvertType(funcDecl.ReturnType));
            }
            else
            {
                _output.Append("void");
            }
            
            _output.Append($" {funcDecl.Name}(");
            
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

        private void GenerateForStatement(ForStatement forStmt)
        {
            Indent();
            _output.Append("for (");
            
            if (forStmt.Initializer != null)
                GenerateStatement(forStmt.Initializer);
            _output.Append("; ");
            
            if (forStmt.Condition != null)
                GenerateExpression(forStmt.Condition);
            _output.Append("; ");
            
            if (forStmt.Increment != null)
                GenerateStatement(forStmt.Increment);
            
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

        private void GenerateReturnStatement(ReturnStatement returnStmt)
        {
            Indent();
            _output.Append("return");
            
            if (returnStmt.Value != null)
            {
                _output.Append(" ");
                GenerateExpression(returnStmt.Value);
            }
            
            _output.AppendLine(";");
        }

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
                        // Handle special function names that are C# keywords
                        var functionName = funcIdExpr.Name;
                        if (functionName == "println")
                        {
                            functionName = "print"; // Map println to our built-in print function
                        }
                        
                        // Handle constructor calls
                        if (char.IsUpper(functionName[0]))
                        {
                            _output.Append($"new {functionName}");
                        }
                        else
                        {
                            _output.Append(functionName);
                        }
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
                case ArrayExpression arrayExpr:
                    _output.Append("new object[] { ");
                    for (int i = 0; i < arrayExpr.Elements.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(arrayExpr.Elements[i]);
                    }
                    _output.Append(" }");
                    break;
                case IndexExpression indexExpr:
                    GenerateExpression(indexExpr.Object);
                    _output.Append("[");
                    GenerateExpression(indexExpr.Index);
                    _output.Append("]");
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
            var converted = type switch
            {
                "int" => "int",
                "float" => "double",
                "string" => "string",
                "bool" => "bool",
                _ => "object"
            };

            if (converted == "object" && type != "object")
            {
                _diagnostics.ReportCodeGenWarning($"Unknown type '{type}' converted to 'object'", type);
            }

            return converted;
        }

        private void Indent()
        {
            _output.Append(new string('\t', _indentLevel));
        }
    }
}
