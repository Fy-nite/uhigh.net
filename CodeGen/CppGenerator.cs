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
        private HashSet<string> _usings = new();

        // μHigh type to C++ type mapping table
        private static readonly Dictionary<string, string> TypeMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "int", "int" },
            { "long", "long long" },
            { "float", "float" },
            { "double", "double" },
            { "decimal", "double" },
            { "byte", "uint8_t" },
            { "sbyte", "int8_t" },
            { "short", "short" },
            { "ushort", "unsigned short" },
            { "uint", "unsigned int" },
            { "ulong", "unsigned long long" },
            { "bool", "bool" },
            { "string", "std::string" },
            { "char", "char" },
            { "Guid", "std::string" },
            { "object", "void*" },
            { "array", "std::vector<any>" },
            { "Dictionary", "std::map<any, any>" },
            { "Set", "std::set<any>" },
            { "Tuple", "std::tuple" },
            { "enum", "enum" },
            { "void", "void" },
            { "null", "nullptr" },
            { "DateTime", "std::chrono::system_clock::time_point" },
            { "any", "auto" },
            { "Func", "std::function" },
    
        };

        // μHigh method to C++ method mapping table (partial, for demo)
        private static readonly Dictionary<string, string> MethodMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            { "Add_to_array", "push_back" },
            { "Add_to_set", "insert" },
            { "Add_to_dict", "insert" },
            { "Remove_from", "erase" },
            { "Length_of", "size" },
            { "Count_of", "size" },
            { "Clear", "clear" },
            { "Contains", "find" }, // For dicts, use find to check existence
            { "ToString", "to_string" },
            { "ToString_of", "to_string" },
            { "ToArray", "to_array" },
            { "ToList", "to_vector" },
            { "ToSet", "to_set" },
            { "ToDictionary", "to_map" },
            { "ToTuple", "to_tuple" },
            { "ToStringArray", "to_string_array" },
            { "ToStringList", "to_string_vector" },
            { "ToStringSet", "to_string_set" },
            { "ToStringDictionary", "to_string_map" },
            { "Length_of_string", "length" },
            { "Index_of_array", "[]" },
            { "Index_of_dict", "[]" },
            { "Substring_of", "substr" },
            { "ToUpper", "toupper_string" }, // Will need a helper function
            { "ToLower", "tolower_string" }, // Will need a helper function
            { "Contains_in_string", "find != string::npos" }, // Special handling needed
            { "IndexOf_in_string", "find" },
            { "Sort_array", "sort" },
            { "Reverse_array", "reverse" },
            { "Join_strings", "join_strings" }, // Will need a helper function
            { "Map_array", "transform" },
            { "Filter_array", "copy_if" },
            { "Reduce_array", "accumulate" }
        };

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
            _usings.Clear();

            _output.AppendLine("// μHigh C++ code generator");
            _output.AppendLine("#include <iostream>");
            _output.AppendLine("#include <vector>");
            _output.AppendLine("#include <string>");
            _output.AppendLine("#include <map>");
            _output.AppendLine("using namespace std;");
            _output.AppendLine(@"// custom T type mappings

class T
{
public:
    T() = default;
    T(const T&) = default;
    T(T&&) = default;
    T& operator=(const T&) = default;
    T& operator=(T&&) = default;
    ~T() = default;

};
using List = std::vector<T>;
template<typename K, typename V>
using Dictionary = std::map<K, V>;
template<typename T>
using Set = std::set<T>;
");

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

            // Emit using directives for any STL containers used
            foreach (var usingDirective in _usings)
            {
                _output.Insert(0, $"#include {usingDirective}\n");
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
            // Emit template if generic parameters exist
            if (cls.GenericParameters != null && cls.GenericParameters.Count > 0)
            {
                Indent();
                _output.Append("template<");
                for (int i = 0; i < cls.GenericParameters.Count; i++)
                {
                    if (i > 0) _output.Append(", ");
                    _output.Append($"typename {cls.GenericParameters[i]}");
                }
                _output.AppendLine(">");
            }
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

        private void GenerateStatement(ASTNode stmt
        )
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
                    _diagnostics.ReportCodeGenWarning($"Unknown statement type for C++: {stmt.GetType().Name}");
                    break;
            }
        }

        private void GenerateMethod(MethodDeclaration methodDecl)
        {
            // Emit template if generic parameters exist
            if (methodDecl.GenericParameters != null && methodDecl.GenericParameters.Count > 0)
            {
                Indent();
                _output.Append("template<");
                for (int i = 0; i < methodDecl.GenericParameters.Count; i++)
                {
                    if (i > 0) _output.Append(", ");
                    _output.Append($"typename {methodDecl.GenericParameters[i]}");
                }
                _output.AppendLine(">");
            }
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
                        var functionName = funcIdExpr.Name;
                        
                        // Handle μHigh built-in method mappings
                        if (IsBuiltInMethod(functionName) && callExpr.Arguments.Count > 0)
                        {
                            GenerateMappedMethodCall(functionName, callExpr.Arguments.Cast<ASTNode>().ToList());
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
                    }
                    else if (callExpr.Function is MemberAccessExpression memberAccessExpr)
                    {
                        // Handle method calls like object.Method()
                        if (IsBuiltInMethod(memberAccessExpr.MemberName))
                        {
                            // For utility methods like obj.Add_to(item), map to appropriate C++ method
                            GenerateExpression(memberAccessExpr.Object);
                            
                            switch (memberAccessExpr.MemberName)
                            {
                                case "Add_to":
                                    _output.Append(".push_back(");
                                    break;
                                case "Remove_from":
                                    _output.Append(".erase(");
                                    break;
                                case "Length_of":
                                    _output.Append(".size()");
                                    return;
                                case "ToUpper":
                                    // C++ needs transform for strings
                                    _output.Append("; // Convert to uppercase");
                                    _output.AppendLine();
                                    Indent();
                                    _output.Append("std::transform(");
                                    GenerateExpression(memberAccessExpr.Object);
                                    _output.Append(".begin(), ");
                                    GenerateExpression(memberAccessExpr.Object);
                                    _output.Append(".end(), ");
                                    GenerateExpression(memberAccessExpr.Object);
                                    _output.Append(".begin(), ::toupper");
                                    _output.Append(")");
                                    return;
                                case "ToLower":
                                    // C++ needs transform for strings
                                    _output.Append("; // Convert to lowercase");
                                    _output.AppendLine();
                                    Indent();
                                    _output.Append("std::transform(");
                                    GenerateExpression(memberAccessExpr.Object);
                                    _output.Append(".begin(), ");
                                    GenerateExpression(memberAccessExpr.Object);
                                    _output.Append(".end(), ");
                                    GenerateExpression(memberAccessExpr.Object);
                                    _output.Append(".begin(), ::tolower");
                                    _output.Append(")");
                                    return;
                                case "Index_of":
                                    _output.Append("[");
                                    if (callExpr.Arguments.Count > 0)
                                        GenerateExpression(callExpr.Arguments[0]);
                                    _output.Append("]");
                                    return;
                                case "Substring_of":
                                    _output.Append(".substr(");
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

                case IndexExpression indexExpr:
                    GenerateExpression(indexExpr.Object);
                    _output.Append("[");
                    GenerateExpression(indexExpr.Index);
                    _output.Append("]");
                    break;
                case ConstructorCallExpression constructorExpr:
                    // Apply constructor name mapping for special types
                    var mappedClassName = MapConstructorName(constructorExpr.ClassName);
                    
                    // For vector/map types, might need to include required headers
                    if (IsStlContainerType(mappedClassName))
                    {
                        _usings.Add(GetHeaderForContainer(mappedClassName));
                    }
                    
                    _output.Append($"{mappedClassName}(");
                    for (int i = 0; i < constructorExpr.Arguments.Count; i++)
                    {
                        if (i > 0) _output.Append(", ");
                        GenerateExpression(constructorExpr.Arguments[i]);
                    }
                    _output.Append(")");
                    break;
                case ArrayExpression arrayExpr:
                    // Emit C++ initializer list: { ... }
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
            // Handle generic type parameters - preserve as typename
            if (IsGenericTypeParameter(type))
            {
                return type; // Keep T, U, V, etc. as-is
            }

            // Handle array and generic types
            if (type.StartsWith("array<") && type.EndsWith(">"))
            {
                var elementType = type[6..^1];
                return $"std::vector<{ConvertType(elementType)}>";
            }
            if (type.StartsWith("List<") && type.EndsWith(">"))
            {
                var elementType = type[5..^1];
                return $"std::vector<{ConvertType(elementType)}>";
            }
            if (type.StartsWith("Dictionary<") && type.EndsWith(">"))
            {
                var genericArgs = type[10..^1].Split(',');
                if (genericArgs.Length == 2)
                    return $"std::map<{ConvertType(genericArgs[0])}, {ConvertType(genericArgs[1])}>";
            }
            if (type.StartsWith("Set<") && type.EndsWith(">"))
            {
                var elementType = type[4..^1];
                return $"std::set<{ConvertType(elementType)}>";
            }
            if (type.StartsWith("Tuple<") && type.EndsWith(">"))
            {
                var genericArgs = type[6..^1].Split(',');
                return $"std::tuple<{string.Join(", ", genericArgs.Select(ConvertType))}>";
            }

            // Use mapping table for simple types
            if (TypeMappings.TryGetValue(type, out var mapped))
                return mapped;

            // Fallback to auto for unknown types
            return type switch
            {
                "var" => "auto",
                _ => "auto"
            };
        }

        private bool IsGenericTypeParameter(string typeName)
        {
            return (typeName.Length == 1 && char.IsUpper(typeName[0])) ||
                   (typeName.StartsWith("T") && typeName.Length <= 15 && char.IsUpper(typeName[0]));
        }


        private string MapMethod(string methodName, string targetType)
        {
            var key = $"{methodName}_of_{targetType}".ToLowerInvariant();
            if (MethodMappings.TryGetValue(key, out var mapped))
                return mapped;
            return methodName;
        }

        private bool IsBuiltInMethod(string functionName)
        {
            return MethodMappings.ContainsKey(functionName) ||
                   MethodMappings.Keys.Any(k => k.StartsWith(functionName + "_of_", StringComparison.OrdinalIgnoreCase));
        }

        private string GetCurrentClassName()
        {
            return "UnknownClass";
        }

        // Handles mapped method calls for built-in methods
        private void GenerateMappedMethodCall(string functionName, List<ASTNode> arguments)
        {
            // Try to find a mapping for the method
            string mappedMethod = MethodMappings.ContainsKey(functionName)
                ? MethodMappings[functionName]
                : functionName;

            // For methods like push_back, insert, erase, etc., assume first argument is the target object
            if (arguments.Count > 0)
            {
                // The first argument is the target object, the rest are method arguments
                GenerateExpression(arguments[0]);
                _output.Append($".{mappedMethod}(");
                for (int i = 1; i < arguments.Count; i++)
                {
                    if (i > 1) _output.Append(", ");
                    GenerateExpression(arguments[i]);
                }
                _output.Append(")");
            }
            else
            {
                // No arguments, just emit the method name
                _output.Append(mappedMethod + "()");
            }
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

        private static string GetLanguageMethodName(string methodName, string objectType)
        {
            // Simple mapping for common container methods
            if (objectType.StartsWith("vector") || objectType == "std::vector" || objectType == "vector")
            {
                return methodName switch
                {
                    "Add" => "push_back",
                    "Remove" => "erase",
                    "Clear" => "clear",
                    "Count" => "size",
                    _ => methodName
                };
            }
            if (objectType.StartsWith("map") || objectType == "std::map" || objectType == "map")
            {
                return methodName switch
                {
                    "Add" => "insert",
                    "Remove" => "erase",
                    "Clear" => "clear",
                    "Count" => "size",
                    _ => methodName
                };
            }
            if (objectType.StartsWith("set") || objectType == "std::set" || objectType == "set")
            {
                return methodName switch
                {
                    "Add" => "insert",
                    "Remove" => "erase",
                    "Clear" => "clear",
                    "Count" => "size",
                    _ => methodName
                };
            }
            return methodName;
        }

        private string MapConstructorName(string className)
        {
            // Remove "new" keyword since C++ doesn't use it the same way
            // Special mapping for common collection types
            return className switch
            {
                "List" => "std::vector<object>",
                "Dictionary" => "std::map<object, object>",
                "Set" => "std::set<object>",
                "string" => "std::string",
                // Handle common generic patterns
                var name when name.StartsWith("List<") => name.Replace("List<", "std::vector<"),
                var name when name.StartsWith("Dictionary<") => name.Replace("Dictionary<", "std::map<"),
                var name when name.StartsWith("Set<") => name.Replace("Set<", "std::set<"),
                // For all other types, apply standard type conversion
                _ => className
            };
        }

        private bool IsStlContainerType(string typeName)
        {
            return typeName.StartsWith("std::vector") || 
                   typeName.StartsWith("std::map") || 
                   typeName.StartsWith("std::set") ||
                   typeName.StartsWith("std::string");
        }

        private string GetHeaderForContainer(string typeName)
        {
            if (typeName.StartsWith("std::vector")) return "<vector>";
            if (typeName.StartsWith("std::map")) return "<map>";
            if (typeName.StartsWith("std::set")) return "<set>";
            if (typeName.StartsWith("std::string")) return "<string>";
            return "";
        }
    }
}
