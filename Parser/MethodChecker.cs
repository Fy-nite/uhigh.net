using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;

namespace uhigh.Net.Parser
{
    public class MethodSignature
    {
        public string Name { get; set; } = "";
        public List<Parameter> Parameters { get; set; } = new();
        public string? ReturnType { get; set; }
        public bool IsBuiltIn { get; set; }
        public bool IsStatic { get; set; }
        public string? ClassName { get; set; }        
        public SourceLocation? DeclarationLocation { get; set; }

        public string GetFullSignature()
        {
            var paramTypes = Parameters.Select(p => p.Type ?? "object").ToList();
            return $"{Name}({string.Join(", ", paramTypes)})";
        }        public bool MatchesCall(string name, List<Expression> arguments)
        {
            if (Name != name) return false;
            
            // For built-in methods, use reflection for type checking
            if (IsBuiltIn)
            {
                return Parameters.Count == arguments.Count;
            }
            
            // For user-defined methods in μHigh, be more lenient
            // Just check parameter count for now
            return Parameters.Count == arguments.Count;
        }

        private bool HasDefaultValue(Parameter parameter)
        {
            // Check if parameter has a default value
            // For now, we'll consider parameters with nullable types as having defaults
            return parameter.Type != null && parameter.Type.EndsWith("?");
        }        private string InferArgumentType(Expression argument)
        {
            return argument switch
            {
                LiteralExpression lit => InferLiteralType(lit),
                IdentifierExpression => "number", // Assume numeric variables for μHigh
                CallExpression => "object", // Would need more sophisticated analysis
                BinaryExpression binExpr when IsArithmeticOperator(binExpr.Operator) => "number",
                BinaryExpression binExpr when IsComparisonOperator(binExpr.Operator) => "bool",
                _ => "object"
            };
        }

        private string InferLiteralType(LiteralExpression literal)
        {
            return literal.Value switch
            {
                string => "string",
                int => "number",
                long => "number", 
                float => "number",
                double => "number",
                bool => "bool",
                null => "null",
                _ => "object"
            };
        }private bool IsTypeCompatible(string? paramType, string argType)
        {
            if (paramType == null || paramType == "object") return true;
            if (paramType == argType) return true;
            
            // Handle nullable types
            if (paramType.EndsWith("?"))
            {
                var baseType = paramType.TrimEnd('?');
                return baseType == argType || argType == "null";
            }
            
            // Handle numeric conversions (more permissive for μHigh)
            var numericTypes = new[] { "int", "long", "float", "double", "number" };
            if (numericTypes.Contains(paramType) && numericTypes.Contains(argType))
            {
                return true;
            }
            
            // Handle string conversions
            if (paramType == "string" && argType != "null")
            {
                return true; // Most types can be converted to string
            }
            
            // Handle object parameters (can accept anything)
            if (paramType == "object")
            {
                return true;
            }
            
            return false;
        }

        private bool IsArithmeticOperator(TokenType op)
        {
            return op is TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide or TokenType.Modulo;
        }

        private bool IsComparisonOperator(TokenType op)
        {
            return op is TokenType.Equal or TokenType.NotEqual or TokenType.Less or TokenType.Greater 
                or TokenType.LessEqual or TokenType.GreaterEqual;
        }
    }

    public class ClassInfo
    {
        public string Name { get; set; } = "";
        public string? BaseClass { get; set; }
        public List<PropertyDeclaration> Fields { get; set; } = new();
        public List<PropertyDeclaration> Properties { get; set; } = new(); // Separate properties from fields
        public List<MethodDeclaration> Methods { get; set; } = new();
        public List<MethodDeclaration> Constructors { get; set; } = new();
        public bool IsPublic { get; set; }
        public SourceLocation? DeclarationLocation { get; set; }

        public bool HasField(string fieldName)
        {
            return Fields.Any(f => f.Name == fieldName) || Properties.Any(p => p.Name == fieldName);
        }

        public bool HasMethod(string methodName)
        {
            return Methods.Any(m => m.Name == methodName);
        }

        public MethodDeclaration? GetMethod(string methodName)
        {
            return Methods.FirstOrDefault(m => m.Name == methodName);
        }
    }

    public class MethodChecker
    {
        private readonly DiagnosticsReporter _diagnostics;
        private readonly ReflectionMethodResolver _reflectionResolver;
        private readonly ReflectionTypeResolver _typeResolver; // Add this
        private readonly Dictionary<string, List<MethodSignature>> _methods = new();
        private readonly Dictionary<string, List<MethodSignature>> _classMethods = new();
        private readonly Dictionary<string, ClassInfo> _classes = new();

        public MethodChecker(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            _reflectionResolver = new ReflectionMethodResolver(diagnostics);
            _typeResolver = new ReflectionTypeResolver(diagnostics); // Add this
            RegisterBuiltInMethods();
        }

        private void RegisterBuiltInMethods()
        {
            // Only register essential built-ins that don't map directly to .NET
            // Let reflection handle the rest

            // Special μHigh functions that need custom handling
            RegisterBuiltIn("print", new[] { "object" }, "void");
            RegisterBuiltIn("input", new string[0], "string");
            
            // Type conversion functions with special naming
            RegisterBuiltIn("int", new[] { "string" }, "int");
            RegisterBuiltIn("int", new[] { "double" }, "int");
            RegisterBuiltIn("float", new[] { "string" }, "double");
            RegisterBuiltIn("float", new[] { "int" }, "double");
            RegisterBuiltIn("string", new[] { "object" }, "string");
            RegisterBuiltIn("bool", new[] { "object" }, "bool");

            // Range function (common in μHigh)
            RegisterBuiltIn("range", new[] { "int" }, "IEnumerable<int>");
            RegisterBuiltIn("range", new[] { "int", "int" }, "IEnumerable<int>");
            RegisterBuiltIn("range", new[] { "int", "int", "int" }, "IEnumerable<int>");
        }

        private void RegisterBuiltIn(string name, string[] paramTypes, string returnType)
        {
            var parameters = paramTypes.Select((type, index) => new Parameter 
            { 
                Name = $"param{index}", 
                Type = type 
            }).ToList();

            var signature = new MethodSignature
            {
                Name = name,
                Parameters = parameters,
                ReturnType = returnType,
                IsBuiltIn = true,
                IsStatic = true
            };

            if (!_methods.ContainsKey(name))
                _methods[name] = new List<MethodSignature>();
            
            _methods[name].Add(signature);
        }

        public void RegisterMethod(FunctionDeclaration func, SourceLocation? location = null)
        {
            // Check if function has external or dotnetfunc attribute - skip validation for these
            var hasExternalAttribute = func.Attributes.Any(attr => attr.IsExternal);
            var hasDotNetFuncAttribute = func.Attributes.Any(attr => attr.IsDotNetFunc);

            if (hasDotNetFuncAttribute || hasExternalAttribute)
            {
                _diagnostics.ReportInfo($"Skipping method validation for external function: {func.Name}");
                return;
            }

            var signature = new MethodSignature
            {
                Name = func.Name,
                Parameters = func.Parameters,
                ReturnType = func.ReturnType,
                IsBuiltIn = false,
                IsStatic = true,
                DeclarationLocation = location
            };

            if (!_methods.ContainsKey(func.Name))
                _methods[func.Name] = new List<MethodSignature>();

            _methods[func.Name].Add(signature);
        }

        public void RegisterMethod(MethodDeclaration method, string className, SourceLocation? location = null)
        {
            // Check if method has external or dotnetfunc attribute - skip validation for these
            var hasExternalAttribute = method.Attributes?.Any(attr => attr.IsExternal) ?? false;
            var hasDotNetFuncAttribute = method.Attributes?.Any(attr => attr.IsDotNetFunc) ?? false;

            if (hasDotNetFuncAttribute || hasExternalAttribute)
            {
                _diagnostics.ReportInfo($"Skipping method validation for external method: {className}.{method.Name}");
                return;
            }

            var signature = new MethodSignature
            {
                Name = method.Name,
                Parameters = method.Parameters,
                ReturnType = method.ReturnType,
                IsBuiltIn = false,
                IsStatic = method.IsStatic,
                ClassName = className,
                DeclarationLocation = location
            };

            var key = method.IsStatic ? method.Name : $"{className}.{method.Name}";
            
            if (!_classMethods.ContainsKey(key))
                _classMethods[key] = new List<MethodSignature>();

            _classMethods[key].Add(signature);
        }

        public void RegisterClass(ClassDeclaration classDecl, SourceLocation? location = null)
        {
            var classInfo = new ClassInfo
            {
                Name = classDecl.Name,
                BaseClass = classDecl.BaseClass,
                IsPublic = classDecl.IsPublic,
                DeclarationLocation = location
            };

            // Register fields and methods
            foreach (var member in classDecl.Members)
            {
                switch (member)
                {
                    case PropertyDeclaration prop:
                        classInfo.Fields.Add(prop);
                        break;
                    case MethodDeclaration method:
                        if (method.IsConstructor)
                            classInfo.Constructors.Add(method);
                        else
                            classInfo.Methods.Add(method);
                        RegisterMethod(method, classDecl.Name, location);
                        break;
                }
            }

            _classes[classDecl.Name] = classInfo;
            _diagnostics.ReportInfo($"Registered class: {classDecl.Name} with {classInfo.Fields.Count} fields, {classInfo.Methods.Count} methods, and {classInfo.Constructors.Count} constructors");
        }

        public bool ValidateConstructorCall(string className, List<Expression> arguments, Token callToken)
        {
            // Don't validate qualified names like "microshell.shell" as constructor calls
            if (className.Contains('.'))
            {
                _diagnostics.ReportInfo($"Allowing qualified constructor call (assuming external): {className}");
                return true; // Assume qualified names are valid external references
            }

            // Check if this is a known external class from imports
            if (IsExternalClass(className))
            {
                _diagnostics.ReportInfo($"Allowing external class constructor: {className}");
                return true;
            }

            if (!_classes.ContainsKey(className))
            {
                // More detailed logging for debugging
                _diagnostics.ReportInfo($"Available classes: {string.Join(", ", _classes.Keys)}");
                _diagnostics.ReportError(
                    $"Class '{className}' is not defined",
                    callToken.Line, callToken.Column, "UH205");
                return false;
            }

            var classInfo = _classes[className];
            
            // If no explicit constructors, allow parameterless constructor
            if (!classInfo.Constructors.Any())
            {
                if (arguments.Count == 0)
                {
                    _diagnostics.ReportInfo($"Allowing default constructor for class '{className}'");
                    return true;
                }
                
                _diagnostics.ReportError(
                    $"Class '{className}' has no constructor that takes {arguments.Count} parameter(s)",
                    callToken.Line, callToken.Column, "UH206");
                return false;
            }

            // Check if any constructor matches
            foreach (var constructor in classInfo.Constructors)
            {
                if (constructor.Parameters.Count == arguments.Count)
                {
                    _diagnostics.ReportInfo($"Found matching constructor for '{className}' with {arguments.Count} parameters");
                    return true;
                }
            }

            var expectedCounts = classInfo.Constructors.Select(c => c.Parameters.Count).Distinct();
            _diagnostics.ReportError(
                $"Class '{className}' constructor expects {string.Join(" or ", expectedCounts)} parameter(s), but {arguments.Count} were provided",
                callToken.Line, callToken.Column, "UH207");
            return false;
        }

        // Add helper method to check if a class is external
        private bool IsExternalClass(string className)
        {
            // Check if this class name appears in any import mappings or known external classes
            // This could be expanded to check against a registry of external/imported classes
            return false; // For now, return false - can be enhanced later
        }

        public bool ValidateMemberAccess(string className, string memberName, Token accessToken)
        {
            if (!_classes.ContainsKey(className))
            {
                _diagnostics.ReportError(
                    $"Class '{className}' is not defined",
                    accessToken.Line, accessToken.Column, "UH208");
                return false;
            }

            var classInfo = _classes[className];
            
            if (classInfo.HasField(memberName) || classInfo.HasMethod(memberName))
                return true;

            _diagnostics.ReportError(
                $"Class '{className}' does not have a member named '{memberName}'",
                accessToken.Line, accessToken.Column, "UH209");
            
            // Suggest similar members
            var allMembers = classInfo.Fields.Select(f => f.Name)
                .Concat(classInfo.Methods.Select(m => m.Name))
                .ToList();
            
            var suggestions = allMembers
                .Where(name => LevenshteinDistance(memberName, name) <= 2)
                .Take(3)
                .ToList();

            if (suggestions.Any())
            {
                _diagnostics.ReportWarning(
                    $"Did you mean: {string.Join(", ", suggestions)}?",
                    accessToken.Line, accessToken.Column, "UH210");
            }

            return false;
        }        public bool ValidateCall(string functionName, List<Expression> arguments, Token location)
        {
            // Check if this is actually a constructor call (capitalized name without dots)
            if (char.IsUpper(functionName[0]) && !functionName.Contains('.'))
            {
                return ValidateConstructorCall(functionName, arguments, location);
            }

            _diagnostics.ReportInfo($"Validating call to '{functionName}' with {arguments.Count} arguments");

            // First check user-defined functions
            if (_methods.ContainsKey(functionName))
            {
                var signatures = _methods[functionName];
                var matchingSignature = signatures.FirstOrDefault(s => s.MatchesCall(functionName, arguments));

                if (matchingSignature != null)
                {
                    _diagnostics.ReportInfo($"Successfully validated user-defined call to '{functionName}'");
                    return true;
                }
            }

            // Then check reflection for .NET methods
            if (_typeResolver.TryResolveMethod(functionName, arguments, out var method))
            {
                _diagnostics.ReportInfo($"Successfully validated .NET method call to '{functionName}'");
                return true;
            }

            // Check if it's a known type method using the old resolver as fallback
            if (_reflectionResolver.TryResolveStaticMethod(functionName, arguments, out method))
            {
                _diagnostics.ReportInfo($"Successfully validated static .NET method call to '{functionName}'");
                return true;
            }

            // Check qualified calls
            var dotIndex = functionName.LastIndexOf('.');
            if (dotIndex > 0)
            {
                var typeName = functionName.Substring(0, dotIndex);
                var methodName = functionName.Substring(dotIndex + 1);

                if (_typeResolver.TryResolveType(typeName, out var type))
                {
                    var qualifiedMethodName = $"{type.FullName}.{methodName}";
                    if (_typeResolver.TryResolveMethod(qualifiedMethodName, arguments, out method))
                    {
                        _diagnostics.ReportInfo($"Successfully validated qualified method call to '{functionName}'");
                        return true;
                    }
                }
            }

            _diagnostics.ReportError($"Unknown function: {functionName}", location.Line, location.Column, "UH201");
            SuggestSimilarMethods(functionName, location);
            return false;
        }

       

        private void SuggestSimilarMethods(string methodName, Token callToken)
        {
            // Get suggestions from both user-defined and reflected methods
            var userMethods = _methods.Keys.Concat(_classMethods.Keys).ToList();
            var reflectedMethods = _typeResolver.GetSimilarMethods(methodName);
            
            var allSuggestions = userMethods.Concat(reflectedMethods)
                .Where(name => LevenshteinDistance(methodName, name) <= 2)
                .Distinct()
                .Take(5)
                .ToList();

            if (allSuggestions.Any())
            {
                _diagnostics.ReportWarning(
                    $"Did you mean: {string.Join(", ", allSuggestions)}?",
                    callToken.Line, callToken.Column, "UH204");
            }
        }

        // Add method to validate types
        public bool ValidateType(string typeName, Token location)
        {
            // Check if it's a user-defined type
            if (_classes.ContainsKey(typeName))
            {
                return true;
            }

            // Check if it's a .NET type via reflection
            if (_typeResolver.IsValidType(typeName))
            {
                return true;
            }

            // Check built-in type aliases
            var builtInTypes = new[] { "int", "float", "string", "bool", "void", "array", "object" };
            if (builtInTypes.Contains(typeName))
            {
                return true;
            }

            _diagnostics.ReportError($"Unknown type: {typeName}", location.Line, location.Column, "UH301");
            
            // Suggest similar types
            var suggestions = _typeResolver.GetSimilarTypes(typeName);
            if (suggestions.Any())
            {
                _diagnostics.ReportWarning(
                    $"Did you mean: {string.Join(", ", suggestions)}?",
                    location.Line, location.Column, "UH302");
            }

            return false;
        }

        // Add method to get type resolver for other components
        public ReflectionTypeResolver GetTypeResolver()
        {
            return _typeResolver;
        }

        public void PrintMethodSummary()
        {
            _diagnostics.ReportInfo($"Registered {_classes.Count} user-defined classes");
            _diagnostics.ReportInfo($"Registered {_methods.Values.Sum(list => list.Count)} user-defined global methods");
            _diagnostics.ReportInfo($"Registered {_classMethods.Values.Sum(list => list.Count)} user-defined class methods");
            _diagnostics.ReportInfo($"Discovered {_typeResolver.GetAllTypeNames().Count()} types via reflection");
            _diagnostics.ReportInfo($"Discovered {_typeResolver.GetAllMethodNames().Count()} methods via reflection");
        }

        public ClassInfo? GetClassInfo(string className)
        {
            return _classes.GetValueOrDefault(className);
        }

        public List<string> GetAllClassNames()
        {
            return _classes.Keys.ToList();
        }

        public List<string> GetAllMethodNames()
        {
            return _methods.Keys.Concat(_classMethods.Keys).Distinct().ToList();
        }

        public MethodSignature? GetMethodSignature(string methodName)
        {
            if (_methods.ContainsKey(methodName))
                return _methods[methodName].FirstOrDefault();
            
            if (_classMethods.ContainsKey(methodName))
                return _classMethods[methodName].FirstOrDefault();
                
            return null;
        }
        
        private string InferArgumentType(Expression argument)
        {
            return argument switch
            {
                LiteralExpression lit => InferLiteralType(lit),
                IdentifierExpression => "number", // Assume numeric variables for μHigh
                CallExpression => "object", // Would need more sophisticated analysis
                BinaryExpression binExpr when IsArithmeticOperator(binExpr.Operator) => "number",
                BinaryExpression binExpr when IsComparisonOperator(binExpr.Operator) => "bool",
                _ => "object"
            };
        }

        private string InferLiteralType(LiteralExpression literal)
        {
            return literal.Value switch
            {
                string => "string",
                int => "number",
                long => "number", 
                float => "number",
                double => "number",
                bool => "bool",
                null => "null",
                _ => "object"
            };
        }
        
        private bool IsArithmeticOperator(TokenType op)
        {
            return op is TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide or TokenType.Modulo;
        }

        private bool IsComparisonOperator(TokenType op)
        {
            return op is TokenType.Equal or TokenType.NotEqual or TokenType.Less or TokenType.Greater 
                or TokenType.LessEqual or TokenType.GreaterEqual;
        }
        
        private int LevenshteinDistance(string s, string t)
        {
            // Levenshtein distance algorithm to find the edit distance between two strings
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }
    }
}
 