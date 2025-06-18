using uhigh.Net.Parser;
using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;
using System.Text;

namespace uhigh.Net.CodeGen
{
    public class CSharpGenerator
    {
        private readonly StringBuilder _output = new();
        private int _indentLevel = 0;
        private readonly HashSet<string> _usings = new();
        private readonly Dictionary<string, string> _importMappings = new();
        private DiagnosticsReporter _diagnostics = new();
        private string _rootNamespace = "Generated";
        private string _className = "Program";

        public string Generate(Program program, DiagnosticsReporter? diagnostics = null, string? rootNamespace = null, string? className = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _output.Clear();
            _indentLevel = 0;
            _usings.Clear();
            _importMappings.Clear();
            _rootNamespace = rootNamespace ?? "Generated";
            _className = className ?? "Program";

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

        private void GenerateBuiltInFunctions()
        {
            // Reduce built-in functions since we'll rely more on reflection
            // Keep only the most essential ones that need special handling
            
            // Type conversion functions that need special naming
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
            
            // Special functions that don't map directly to .NET
            Indent();
            _output.AppendLine("public static void print(object value) => Console.WriteLine(value);");
            Indent();
            _output.AppendLine("public static string input() => Console.ReadLine() ?? \"\";");
            
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

        private void GenerateClassDeclaration(ClassDeclaration classDecl)
        {
            // Check for external attribute on class
            var hasExternalAttribute = classDecl.Members.OfType<AttributeDeclaration>()
                .Any(attr => attr.IsExternal);

            if (hasExternalAttribute)
            {
                _diagnostics.ReportInfo($"Skipping code generation for external class: {classDecl.Name}");
                return;
            }

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
                case SharpBlock sharpBlock:
                    GenerateSharpBlock(sharpBlock);
                    break;
                case ExpressionStatement exprStmt:
                    Indent();
                    GenerateExpression(exprStmt.Expression);
                    _output.AppendLine(";");
                    break;
                case MatchStatement matchStmt:
                    Indent();
                    GenerateExpression(matchStmt.Value);
                    _output.Append(" switch {");
                    bool isFirst = true;
                    foreach (var arm in matchStmt.Arms)
                    {
                        if (!isFirst) _output.Append(",");
                        _output.AppendLine();
                        var originalIndent = _indentLevel;
                        _indentLevel = originalIndent + 1;
                        Indent();
                        if (arm.IsDefault)
                        {
                            _output.Append("_");
                        }
                        else
                        {
                            for (int i = 0; i < arm.Patterns.Count; i++)
                            {
                                if (i > 0) _output.Append(" or ");
                                GenerateExpression(arm.Patterns[i]);
                            }
                        }
                        _output.Append(" => ");
                        GenerateExpression(arm.Result);
                        _indentLevel = originalIndent;
                        isFirst = false;
                    }
                    _output.AppendLine();
                    Indent();
                    _output.AppendLine("};");
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown statement type: {statement.GetType().Name}");
                    break;
            }
        }

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
                    // Add "new" keyword for constructor calls in C#
                    _output.Append($"new {constructorExpr.ClassName}(");
                    for (int i = 0; i < constructorExpr.Arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(constructorExpr.Arguments[i]);
                    }
                    _output.Append(")");
                    break;
                case CallExpression callExpr:
                    if (callExpr.Function is IdentifierExpression funcIdExpr)
                    {
                        var functionName = funcIdExpr.Name;
                        
                        // If this is a capitalized name, it should have been converted to ConstructorCallExpression
                        // If we reach here, treat it as a regular function call
                        GenerateFunctionCall(functionName, callExpr.Arguments);
                    }
                    else if (callExpr.Function is QualifiedIdentifierExpression qualifiedFuncExpr)
                    {
                        var functionName = qualifiedFuncExpr.Name;
                        GenerateFunctionCall(functionName, callExpr.Arguments);
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
                case MatchExpression matchExpr:
                    GenerateMatchExpression(matchExpr);
                    break;
            }
        }

        private void GenerateMatchExpression(MatchExpression matchExpr)
        {
            GenerateExpression(matchExpr.Value);
            _output.Append(" switch {");
            
            bool isFirst = true;
            foreach (var arm in matchExpr.Arms)
            {
                if (!isFirst)
                {
                    _output.Append(",");
                }
                _output.AppendLine();
                
                // Increase indent for arms
                var originalIndent = _indentLevel;
                _indentLevel = originalIndent + 1;
                Indent();
                
                if (arm.IsDefault)
                {
                    _output.Append("_");
                }
                else
                {
                    // Handle multiple patterns (e.g., 1 or 2 or 3)
                    for (int i = 0; i < arm.Patterns.Count; i++)
                    {
                        if (i > 0) _output.Append(" or ");
                        GenerateExpression(arm.Patterns[i]);
                    }
                }
                
                _output.Append(" => ");
                GenerateExpression(arm.Result);
                
                _indentLevel = originalIndent;
                isFirst = false;
            }
            
            _output.AppendLine();
            Indent();
            _output.Append("}");
        }

        private void GenerateFunctionCall(string functionName, List<Expression> arguments)
        {
            // Handle special function names that are C# keywords
            if (functionName == "println")
            {
                functionName = "Console.WriteLine"; // Map println to Console.WriteLine
            }
            
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
                "void" => "void",
                "object" => "object",
                "any" => "object",
                "number" => "double",
                "integer" => "int",
                "double" => "double",
                "char" => "char",
                "list" => "List<object>",
                "array" => "object[]",
                "StringArray" => "string[]",
                "map" => "Dictionary<object, object>",
                "function" => "Func<object[], object>", // Generic function type
                "promise" => "Task<object>", // Async promise type
                "set" => "HashSet<object>",
                "tuple" => "Tuple<object, object>", // Simple tuple type
                "date" => "DateTime",
                "json" => "string", // JSON is typically represented as a string
                "stream" => "Stream", // Stream type for file operations
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
