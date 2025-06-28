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
        private readonly Dictionary<string, List<MethodSignature>> _methods = new();
        private readonly Dictionary<string, List<MethodSignature>> _classMethods = new();
        private readonly Dictionary<string, ClassInfo> _classes = new();

        public MethodChecker(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            _reflectionResolver = new ReflectionMethodResolver(diagnostics);
            RegisterBuiltInMethods();
        }

        private void RegisterBuiltInMethods()
        {
            // Math functions
            RegisterBuiltIn("abs", new[] { "double" }, "double");
            RegisterBuiltIn("sqrt", new[] { "double" }, "double");
            RegisterBuiltIn("pow", new[] { "double", "double" }, "double");
            RegisterBuiltIn("min", new[] { "double", "double" }, "double");
            RegisterBuiltIn("max", new[] { "double", "double" }, "double");
            RegisterBuiltIn("random", new string[0], "double");

            // String functions
            RegisterBuiltIn("len", new[] { "string" }, "int");
            RegisterBuiltIn("len", new[] { "object[]" }, "int");
            RegisterBuiltIn("uppercase", new[] { "string" }, "string");
            RegisterBuiltIn("lowercase", new[] { "string" }, "string");
            RegisterBuiltIn("substring", new[] { "string", "int", "int" }, "string");

            // Array functions
            RegisterBuiltIn("append", new[] { "object", "object" }, "void");
            RegisterBuiltIn("pop", new[] { "object", "int" }, "object");
            RegisterBuiltIn("sort", new[] { "object" }, "void");
            RegisterBuiltIn("reverse", new[] { "object" }, "void");

            // Type conversion functions
            RegisterBuiltIn("int", new[] { "string" }, "int");
            RegisterBuiltIn("int", new[] { "double" }, "int");
            RegisterBuiltIn("float", new[] { "string" }, "double");
            RegisterBuiltIn("float", new[] { "int" }, "double");
            RegisterBuiltIn("string", new[] { "object" }, "string");
            RegisterBuiltIn("bool", new[] { "object" }, "bool");

            // Range function
            RegisterBuiltIn("range", new[] { "int" }, "IEnumerable<int>");
            RegisterBuiltIn("range", new[] { "int", "int" }, "IEnumerable<int>");

            // IO functions
            RegisterBuiltIn("print", new[] { "object" }, "void");
            RegisterBuiltIn("input", new string[0], "string");
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
                _diagnostics.ReportInfo($"Skipping constructor validation for qualified name: {className}");
                return true; // Assume qualified names are valid external references
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

            // Skip validation for qualified names that might be external references
            if (functionName.Contains('.'))
            {
                var parts = functionName.Split('.');
                if (parts.Length == 2 && char.IsLower(parts[1][0]))
                {
                    // This looks like namespace.function or class.method - allow it
                    _diagnostics.ReportInfo($"Allowing qualified method call: {functionName}");
                    return true;
                }
            }

            _diagnostics.ReportInfo($"Validating call to '{functionName}' with {arguments.Count} arguments");

            // First check user-defined functions
            if (_methods.ContainsKey(functionName))
            {
                var signatures = _methods[functionName];
                _diagnostics.ReportInfo($"Found {signatures.Count} signature(s) for '{functionName}'");
                
                // foreach (var sig in signatures)
                // {
                //     _diagnostics.ReportInfo($"  Signature: {sig.GetFullSignature()}");
                // }

                var matchingSignature = signatures.FirstOrDefault(s => s.MatchesCall(functionName, arguments));

                if (matchingSignature != null)
                {
                    _diagnostics.ReportInfo($"Successfully validated call to '{functionName}' with {arguments.Count} arguments");
                    return true;
                }                // Report parameter count mismatch with better error message
                var expectedCounts = signatures.Select(s => s.Parameters.Count).Distinct().ToList();
                
                // Provide detailed parameter information for debugging
                var sig = signatures.First();
                var paramDetails = sig.Parameters.Select((p, i) => $"param {i+1}: {p.Name}:{p.Type ?? "any"}").ToList();
                var argDetails = arguments.Select((a, i) => $"arg {i+1}: {InferArgumentType(a)}").ToList();
                
                _diagnostics.ReportInfo($"Function '{functionName}' signature: ({string.Join(", ", paramDetails)})");
                _diagnostics.ReportInfo($"Call arguments: ({string.Join(", ", argDetails)})");
                
                if (expectedCounts.Count == 1 && expectedCounts[0] == arguments.Count)
                {
                    // Same parameter count but type mismatch - report different error
                    _diagnostics.ReportError(
                        $"Method '{functionName}' parameter types do not match the provided arguments",
                        location.Line, location.Column, "UH200");
                }
                else
                {
                    var expectedCountsStr = expectedCounts.Count == 1 ? 
                        expectedCounts[0].ToString() : 
                        string.Join(" or ", expectedCounts);
                    _diagnostics.ReportError(
                        $"Method '{functionName}' expects {expectedCountsStr} parameter(s), but {arguments.Count} were provided",
                        location.Line, location.Column, "UH200");
                }
                return false;
            }

            // Then check .NET methods using reflection
            if (_reflectionResolver.TryResolveStaticMethod(functionName, arguments, out var method))
            {
                _diagnostics.ReportInfo($"Resolved .NET method: {functionName}");
                return true;
            }

            // Check if it's a known type method
            var dotIndex = functionName.LastIndexOf('.');
            if (dotIndex > 0)
            {
                var typeName = functionName.Substring(0, dotIndex);
                var methodName = functionName.Substring(dotIndex + 1);

                if (_reflectionResolver.TryResolveMethod(typeName, methodName, arguments, out method))
                {
                    _diagnostics.ReportInfo($"Resolved .NET method: {typeName}.{methodName}");
                    return true;
                }
            }

            _diagnostics.ReportError($"Unknown function: {functionName}", location.Line, location.Column, "UH201");
            SuggestSimilarMethods(functionName, location);
            return false;
        }

        private bool ValidateQualifiedCall(string qualifiedName, List<Expression> arguments, Token callToken)
        {
            var parts = qualifiedName.Split('.');
            if (parts.Length < 2)
            {
                return ValidateCall(qualifiedName, arguments, callToken);
            }

            var className = string.Join(".", parts.Take(parts.Length - 1));
            var methodName = parts.Last();

            // Check if this is a known .NET method (like Console.WriteLine)
            if (IsKnownDotNetMethod(qualifiedName))
            {
                _diagnostics.ReportInfo($"Validated .NET method call: {qualifiedName}");
                return true;
            }

            // Check class methods
            return ValidateMethodCall(className, methodName, arguments, callToken);
        }

        private bool IsKnownDotNetMethod(string qualifiedName)
        {
            // List of known .NET methods that we want to allow
            var knownMethods = new HashSet<string>
            {
                "Console.WriteLine",
                "Console.Write", 
                "Console.ReadLine",
                "Math.Abs",
                "Math.Sqrt",
                "Math.Pow",
                "Math.Min",
                "Math.Max",
                "String.IsNullOrEmpty",
                "String.IsNullOrWhiteSpace",
                "Convert.ToInt32",
                "Convert.ToDouble",
                "Convert.ToString"
            };

            return knownMethods.Contains(qualifiedName);
        }

        public bool ValidateMethodCall(string className, string methodName, List<Expression> arguments, Token callToken)
        {
            var key = $"{className}.{methodName}";
            
            if (_classMethods.ContainsKey(key))
            {
                var signatures = _classMethods[key];
                var matchingSignature = signatures.FirstOrDefault(s => s.MatchesCall(methodName, arguments));
                
                if (matchingSignature != null)
                    return true;

                var expectedCounts = signatures.Select(s => s.Parameters.Count).Distinct();
                _diagnostics.ReportError(
                    $"Method '{className}.{methodName}' expects {string.Join(" or ", expectedCounts)} parameter(s), but {arguments.Count} were provided",
                    callToken.Line, callToken.Column, "UH202");
                return false;
            }

            _diagnostics.ReportError(
                $"Method '{methodName}' is not defined in class '{className}'",
                callToken.Line, callToken.Column, "UH203");
            return false;
        }

        private void SuggestSimilarMethods(string methodName, Token callToken)
        {
            var allMethods = _methods.Keys.Concat(_classMethods.Keys).ToList();
            var suggestions = allMethods
                .Where(name => LevenshteinDistance(methodName, name) <= 2)
                .Take(3)
                .ToList();

            if (suggestions.Any())
            {
                _diagnostics.ReportWarning(
                    $"Did you mean: {string.Join(", ", suggestions)}?",
                    callToken.Line, callToken.Column, "UH204");
            }
        }

        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
            if (string.IsNullOrEmpty(b)) return a.Length;

            var matrix = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                matrix[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                matrix[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    matrix[i, j] = Math.Min(Math.Min(
                        matrix[i - 1, j] + 1,
                        matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }

            return matrix[a.Length, b.Length];
        }

        public void PrintMethodSummary()
        {
            _diagnostics.ReportInfo($"Registered {_classes.Count} classes");
            _diagnostics.ReportInfo($"Registered {_methods.Values.Sum(list => list.Count)} global methods");
            _diagnostics.ReportInfo($"Registered {_classMethods.Values.Sum(list => list.Count)} class methods");
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
    }
}
