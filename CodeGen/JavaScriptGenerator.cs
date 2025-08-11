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

            // Add main() call if a top-level main function exists
            if (HasTopLevelMainFunction(program))
            {
                _output.AppendLine();
                _output.AppendLine("main();");
            }

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

            // Add main() call if any program has a top-level main function
            if (programs.Any(HasTopLevelMainFunction))
            {
                _output.AppendLine();
                _output.AppendLine("main();");
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
                case UsingStatement usingStmt:
                    GenerateUsingStatement(usingStmt);
                    break;
                case MatchStatement matchStmt:
                    GenerateMatchStatement(matchStmt);
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
                // For Main method, use lowercase 'main' in JavaScript
                var methodName = methodDecl.Name == "Main" ? "main" : methodDecl.Name;
                _output.Append($"{methodName}(");
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
                var paramTypes = string.Join(", ", funcDecl.Parameters.Select(p => $"{p.Name}: {ConvertTypeForComment(p.Type ?? "any")}"));
                var retType = ConvertTypeForComment(funcDecl.ReturnType ?? "any");
                _output.AppendLine($"// function {funcDecl.Name}({paramTypes}): {retType}");
            }

            // Emit generic parameters as comment if present
            if (funcDecl.GenericParameters != null && funcDecl.GenericParameters.Count > 0)
            {
                _output.AppendLine($"// Generic function: {funcDecl.Name}<{string.Join(", ", funcDecl.GenericParameters)}>");
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


        private static string GetLanguageMethodName(string methodName, string objectType)
        {
            // Simple mapping for arrays and maps
            if (objectType == "array" || objectType == "Array")
            {
                return methodName switch
                {
                    "Add" => "push",
                    "Remove" => "splice", // Needs index
                    "Clear" => "length = 0",
                    "Count" => "length",
                    _ => methodName
                };
            }
            if (objectType == "map" || objectType == "Map")
            {
                return methodName switch
                {
                    "Add" => "set",
                    "Remove" => "delete",
                    "Clear" => "clear",
                    "Count" => "size",
                    _ => methodName
                };
            }

            return methodName;
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
                        var functionName = funcIdExpr.Name;
                        
                        // Handle μHigh built-in method mappings
                        if (IsBuiltInMethod(functionName) && callExpr.Arguments.Count > 0)
                        {
                            GenerateMappedMethodCall(functionName, callExpr.Arguments);
                        }
                        else
                        {
                            // Map μHigh built-ins to JavaScript equivalents
                            var mappedName = funcIdExpr.Name switch
                            {
                                "print" => "console.log",
                                "println" => "console.log",
                                _ => funcIdExpr.Name
                            };
                            
                            _output.Append(mappedName);
                            _output.Append("(");
                            for (int i = 0; i < callExpr.Arguments.Count; i++)
                            {
                                if (i > 0) _output.Append(", ");
                                GenerateExpression(callExpr.Arguments[i]);
                            }
                            _output.Append(")");
                        }
                    }
                    else if (callExpr.Function is MemberAccessExpression memberAccessExpr)
                    {
                        // Handle method calls like object.Method()
                        if (IsBuiltInMethod(memberAccessExpr.MemberName))
                        {
                            // For utility methods like obj.Add_to(item), map to appropriate JS method
                            GenerateExpression(memberAccessExpr.Object);
                            
                            switch (memberAccessExpr.MemberName)
                            {
                                case "Add_to":
                                    _output.Append(".push(");
                                    break;
                                case "Remove_from":
                                    _output.Append(".splice(");
                                    if (callExpr.Arguments.Count > 0)
                                    {
                                        GenerateExpression(callExpr.Arguments[0]);
                                        _output.Append(", 1");
                                    }
                                    _output.Append(")");
                                    return;
                                case "Length_of":
                                    _output.Append(".length");
                                    return;
                                case "ToUpper":
                                    _output.Append(".toUpperCase(");
                                    break;
                                case "ToLower":
                                    _output.Append(".toLowerCase(");
                                    break;
                                case "Index_of":
                                    _output.Append("[");
                                    if (callExpr.Arguments.Count > 0)
                                        GenerateExpression(callExpr.Arguments[0]);
                                    _output.Append("]");
                                    return;
                                case "Substring_of":
                                    _output.Append(".substring(");
                                    break;
                                default:
                                    _output.Append($".{memberAccessExpr.MemberName}(");
                                    break;
                            }
                            
                            // For methods that need arguments
                            for (int i = 0; i < callExpr.Arguments.Count; i++)
                            {
                                if (i > 0) _output.Append(", ");
                                GenerateExpression(callExpr.Arguments[i]);
                            }
                            _output.Append(")");
                        }
                        else
                        {
                            GenerateExpression(memberAccessExpr.Object);
                            _output.Append($".{memberAccessExpr.MemberName}(");
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
                        // Map μHigh built-ins to JavaScript equivalents
                        var functionName = callExpr.Function is IdentifierExpression innerFuncIdExpr ? innerFuncIdExpr.Name switch
                        {
                            "print" => "console.log",
                            "println" => "console.log",
                            _ => innerFuncIdExpr.Name
                        } : null;
                        
                        if (functionName != null)
                        {
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
                    }
                    break;
                case IndexExpression indexExpr:
                    // Handle array or map indexing
                    GenerateExpression(indexExpr.Object);
                    _output.Append("[");
                    GenerateExpression(indexExpr.Index);
                    _output.Append("]");
                    break;
                case AssignmentExpression assignExpr:
                    if (assignExpr.Value == null) throw new Exception("assignExpr.Value is null");
                    if (assignExpr.Target == null) throw new Exception("assignExpr.Target is null");
                    GenerateExpression(assignExpr.Target);
                    _output.Append($" {ConvertOperator(assignExpr.Operator)} ");
                    GenerateExpression(assignExpr.Value);
                    break;
                case ConstructorCallExpression constructorExpr:
                    // Apply constructor name mapping for special types
                    var mappedClassName = MapConstructorName(constructorExpr.ClassName);
                    
                    // Handle collection initializer syntax if present
                    if (IsCollectionType(mappedClassName) && constructorExpr.Arguments.Count == 0)
                    {
                        // For empty collections, use appropriate JS initializer
                        switch (mappedClassName)
                        {
                            case "Array":
                            case var name when name.StartsWith("Array<"):
                                _output.Append("[]");
                                break;
                            case "Map":
                            case var name when name.StartsWith("Map<"):
                                _output.Append("new Map()");
                                break;
                            case "Set":
                            case var name when name.StartsWith("Set<"):
                                _output.Append("new Set()");
                                break;
                            default:
                                _output.Append($"new {mappedClassName}()");
                                break;
                        }
                    }
                    else
                    {
                        _output.Append($"new {mappedClassName}(");
                        for (int i = 0; i < constructorExpr.Arguments.Count; i++)
                        {
                            if (i > 0) _output.Append(", ");
                            GenerateExpression(constructorExpr.Arguments[i]);
                        }
                        _output.Append(")");
                    }
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
                case UnaryExpression unaryExpr:
                    if (unaryExpr.Operand == null) { throw new Exception("unaryExpr.Operand is null"); }
                    if (unaryExpr.Operator == null) { throw new Exception("unaryExpr.Operator is null"); }
                    TokenType _operator = (TokenType)unaryExpr.Operator;
                    if (unaryExpr.Operator == TokenType.Increment || unaryExpr.Operator == TokenType.Decrement)
                    {
                        // Prefix increment/decrement
                        _output.Append(ConvertOperator(_operator));
                        GenerateExpression(unaryExpr.Operand);
                    }
                    else
                    {
                        // Other unary operators (not, minus)
                        _output.Append(ConvertOperator(_operator));
                        if (NeedsParentheses(unaryExpr.Operand))
                        {
                            _output.Append("(");
                            GenerateExpression(unaryExpr.Operand);
                            _output.Append(")");
                        }
                        else
                        {
                            GenerateExpression(unaryExpr.Operand);
                        }
                    }
                    break;
                case ArrayExpression arrayExpr:
                    // Emit JS array literal: [] or [a, b, c]
                    _output.Append("[");
                    for (int i = 0; i < arrayExpr.Elements.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(arrayExpr.Elements[i]);
                    }
                    _output.Append("]");
                    break;
                default:
                    _diagnostics.ReportCodeGenWarning($"Unknown expression type for JavaScript: {expression.GetType().Name}");
                    break;
            }
        }

        private bool NeedsParentheses(Expression expr)
        {
            return expr is BinaryExpression || expr is MatchExpression;
        }

        private bool IsBuiltInMethod(string methodName)
        {
            return methodName.StartsWith("Add_to") || 
                   methodName.StartsWith("Remove_from") || 
                   methodName.StartsWith("Length_of") || 
                   methodName.StartsWith("Index_of") || 
                   methodName.StartsWith("Substring_of") ||
                   methodName == "ToUpper" || 
                   methodName == "ToLower";
        }

        private void GenerateMappedMethodCall(string methodName, List<Expression> arguments)
        {
            if (arguments.Count == 0) return;
    
            var target = arguments[0];

            switch (methodName)
            {
                case "Add_to":
                    if (arguments.Count >= 2)
                    {
                        GenerateExpression(target);
                        _output.Append(".push(");
                        GenerateExpression(arguments[1]);
                        _output.Append(")");
                    }
                    break;
                    
                case "Remove_from":
                    if (arguments.Count >= 2)
                    {
                        // For arrays: splice(index, 1)
                        GenerateExpression(target);
                        _output.Append(".splice(");
                        GenerateExpression(arguments[1]);
                        _output.Append(", 1)");
                    }
                    break;
                    
                case "Length_of":
                    GenerateExpression(target);
                    _output.Append(".length");
                    break;
                    
                case "Index_of":
                    if (arguments.Count >= 2)
                    {
                        GenerateExpression(target);
                        _output.Append("[");
                        GenerateExpression(arguments[1]);
                        _output.Append("]");
                    }
                    break;
                    
                case "Substring_of":
                    if (arguments.Count >= 3)
                    {
                        GenerateExpression(target);
                        _output.Append(".substring(");
                        GenerateExpression(arguments[1]);
                        _output.Append(", ");
                        GenerateExpression(arguments[1]);
                        _output.Append(" + ");
                        GenerateExpression(arguments[2]);
                        _output.Append(")");
                    }
                    break;
                    
                case "ToUpper":
                    GenerateExpression(target);
                    _output.Append(".toUpperCase()");
                    break;
                    
                case "ToLower":
                    GenerateExpression(target);
                    _output.Append(".toLowerCase()");
                    break;
                    
                default:
                    // Fallback to regular function call
                    _output.Append(methodName);
                    _output.Append("(");
                    for (int i = 0; i < arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(arguments[i]);
                    }
                    _output.Append(")");
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

        /// <summary>
        /// Converts type to comment format, preserving generic parameters
        /// </summary>
        private string ConvertTypeForComment(string type)
        {
            // Preserve generic type parameters as-is in comments
            if (IsGenericTypeParameter(type))
            {
                return type;
            }
            
            // Convert other types normally
            return type switch
            {
                "int" => "number",
                "float" => "number", 
                "double" => "number",
                "string" => "string",
                "bool" => "boolean",
                "void" => "void",
                _ => type
            };
        }

        private bool IsGenericTypeParameter(string typeName)
        {
            return (typeName.Length == 1 && char.IsUpper(typeName[0])) ||
                   (typeName.StartsWith("T") && typeName.Length <= 15 && char.IsUpper(typeName[0]));
        }

        // Add these helper methods for JavaScript-specific mappings
        private string MapConstructorName(string className)
        {
            // Special mapping for common collection types
            return className switch
            {
                "List" => "Array",
                "List<string>" => "Array",
                "List<int>" => "Array",
                "List<float>" => "Array",
                "List<bool>" => "Array",
                "Dictionary" => "Map",
                "HashSet" => "Set",
                // Handle common generic patterns
                var name when name.StartsWith("List<") => name.Replace("List<", "Array<"),
                var name when name.StartsWith("Dictionary<") => name.Replace("Dictionary<", "Map<"),
                var name when name.StartsWith("HashSet<") => name.Replace("HashSet<", "Set<"),
                // Keep Observable as is (our custom class)
                var name when name.StartsWith("Observable<") => name,
                // For all other types, leave as is
                _ => className
            };
        }

        private bool IsCollectionType(string className)
        {
            return className == "Array" || 
                   className == "Map" || 
                   className == "Set" ||
                   className.StartsWith("Array<") ||
                   className.StartsWith("Map<") ||
                   className.StartsWith("Set<");
        }

        /// <summary>
        /// Generates a using statement as try-finally in JavaScript
        /// </summary>
        /// <param name="usingStmt">The using statement</param>
        private void GenerateUsingStatement(UsingStatement usingStmt)
        {
            // JavaScript doesn't have using statements, so convert to try-finally with explicit disposal
            
            string? resourceVariable = null;
            
            if (usingStmt.ResourceDeclaration != null)
            {
                // Declare the resource variable
                Indent();
                _output.Append($"let {usingStmt.ResourceDeclaration.Name}");
                if (usingStmt.ResourceDeclaration.Initializer != null)
                {
                    _output.Append(" = ");
                    GenerateExpression(usingStmt.ResourceDeclaration.Initializer);
                }
                _output.AppendLine(";");
                resourceVariable = usingStmt.ResourceDeclaration.Name;
            }
            
            Indent();
            _output.AppendLine("try {");
            _indentLevel++;
            
            // If we have a resource expression (not declaration), evaluate it first
            if (usingStmt.ResourceExpression != null && resourceVariable == null)
            {
                Indent();
                _output.Append("let _resource = ");
                GenerateExpression(usingStmt.ResourceExpression);
                _output.AppendLine(";");
                resourceVariable = "_resource";
            }
            
            foreach (var stmt in usingStmt.Body)
            {
                GenerateStatement(stmt);
            }
            
            _indentLevel--;
            Indent();
            _output.AppendLine("} finally {");
            _indentLevel++;
            
            if (resourceVariable != null)
            {
                Indent();
                _output.AppendLine($"if ({resourceVariable} && typeof {resourceVariable}.dispose === 'function') {{");
                _indentLevel++;
                Indent();
                _output.AppendLine($"{resourceVariable}.dispose();");
                _indentLevel--;
                Indent();
                _output.AppendLine("}");
            }
            
            _indentLevel--;
            Indent();
            _output.AppendLine("}");
        }

        // Helper to check for top-level main function
        private bool HasTopLevelMainFunction(Program program)
        {
            return program.Statements.OfType<FunctionDeclaration>().Any(f => f.Name == "main");
        }

        // Add this method to generate match statements as switch in JS
        private void GenerateMatchStatement(MatchStatement matchStmt)
        {
            Indent();
            _output.Append("switch (");
            GenerateExpression(matchStmt.Value);
            _output.AppendLine(") {");
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
                    foreach (var pattern in arm.Patterns)
                    {
                        Indent();
                        _output.Append("case ");
                        GenerateExpression(pattern);
                        _output.AppendLine(":");
                    }
                }

                _indentLevel++;
                // If the result is a block, emit its statements
                if (arm.Result is BlockExpression blockExpr)
                {
                    foreach (var stmt in blockExpr.Statements)
                    {
                        GenerateStatement(stmt);
                    }
                }
                else
                {
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
    }

}
