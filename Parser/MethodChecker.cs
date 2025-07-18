using uhigh.Net.Diagnostics;
using uhigh.Net.Lexer;

namespace uhigh.Net.Parser
{
    /// <summary>
    /// The method signature class
    /// </summary>
    public class MethodSignature
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the parameters
        /// </summary>
        public List<Parameter> Parameters { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the return type
        /// </summary>
        public string? ReturnType { get; set; }
        /// <summary>
        /// Gets or sets the value of the is built in
        /// </summary>
        public bool IsBuiltIn { get; set; }
        /// <summary>
        /// Gets or sets the value of the is static
        /// </summary>
        public bool IsStatic { get; set; }
        /// <summary>
        /// Gets or sets the value of the class name
        /// </summary>
        public string? ClassName { get; set; }
        /// <summary>
        /// Gets or sets the value of the declaration location
        /// </summary>
        public SourceLocation? DeclarationLocation { get; set; }

        /// <summary>
        /// Gets the full signature
        /// </summary>
        /// <returns>The string</returns>
        public string GetFullSignature()
        {
            var paramTypes = Parameters.Select(p => p.Type ?? "object").ToList();
            return $"{Name}({string.Join(", ", paramTypes)})";
        }        /// <summary>
                 /// Matcheses the call using the specified name
                 /// </summary>
                 /// <param name="name">The name</param>
                 /// <param name="arguments">The arguments</param>
                 /// <returns>The bool</returns>
        public bool MatchesCall(string name, List<Expression> arguments)
        {
            if (Name != name) return false;

            // For built-in methods, use reflection for type checking
            if (IsBuiltIn)
            {
                return Parameters.Count == arguments.Count;
            }

            // For user-defined methods in μHigh, be more lenient
            // Check parameter count and allow for optional parameters
            if (Parameters.Count == arguments.Count)
            {
                return true;
            }

            // Check if we have fewer arguments but remaining parameters have defaults
            if (arguments.Count < Parameters.Count)
            {
                var requiredParams = Parameters.Take(arguments.Count).Count();
                var optionalParams = Parameters.Skip(arguments.Count).Count(HasDefaultValue);
                return requiredParams + optionalParams == Parameters.Count;
            }

            return false;
        }

        /// <summary>
        /// Hases the default value using the specified parameter
        /// </summary>
        /// <param name="parameter">The parameter</param>
        /// <returns>The bool</returns>
        private bool HasDefaultValue(Parameter parameter)
        {
            // Check if parameter has a default value
            // For now, we'll consider parameters with nullable types as having defaults
            return parameter.Type != null && parameter.Type.EndsWith("?");
        }        /// <summary>
                 /// Infers the argument type using the specified argument
                 /// </summary>
                 /// <param name="argument">The argument</param>
                 /// <returns>The string</returns>
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

        /// <summary>
        /// Infers the literal type using the specified literal
        /// </summary>
        /// <param name="literal">The literal</param>
        /// <returns>The string</returns>
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
        }/// <summary>
         /// Ises the type compatible using the specified param type
         /// </summary>
         /// <param name="paramType">The param type</param>
         /// <param name="argType">The arg type</param>
         /// <returns>The bool</returns>
        private bool IsTypeCompatible(string? paramType, string argType)
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

        /// <summary>
        /// Ises the arithmetic operator using the specified op
        /// </summary>
        /// <param name="op">The op</param>
        /// <returns>The bool</returns>
        private bool IsArithmeticOperator(TokenType op)
        {
            return op is TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide or TokenType.Modulo;
        }

        /// <summary>
        /// Ises the comparison operator using the specified op
        /// </summary>
        /// <param name="op">The op</param>
        /// <returns>The bool</returns>
        private bool IsComparisonOperator(TokenType op)
        {
            return op is TokenType.Equal or TokenType.NotEqual or TokenType.Less or TokenType.Greater
                or TokenType.LessEqual or TokenType.GreaterEqual;
        }
    }

    /// <summary>
    /// The class info class
    /// </summary>
    public class ClassInfo
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the base class
        /// </summary>
        public string? BaseClass { get; set; }
        /// <summary>
        /// Gets or sets the value of the fields
        /// </summary>
        public List<PropertyDeclaration> Fields { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the properties
        /// </summary>
        public List<PropertyDeclaration> Properties { get; set; } = new(); // Separate properties from fields
        /// <summary>
        /// Gets or sets the value of the methods
        /// </summary>
        public List<MethodDeclaration> Methods { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the constructors
        /// </summary>
        public List<MethodDeclaration> Constructors { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the is public
        /// </summary>
        public bool IsPublic { get; set; }
        /// <summary>
        /// Gets or sets the value of the declaration location
        /// </summary>
        public SourceLocation? DeclarationLocation { get; set; }

        /// <summary>
        /// Hases the field using the specified field name
        /// </summary>
        /// <param name="fieldName">The field name</param>
        /// <returns>The bool</returns>
        public bool HasField(string fieldName)
        {
            return Fields.Any(f => f.Name == fieldName) || Properties.Any(p => p.Name == fieldName);
        }

        /// <summary>
        /// Hases the method using the specified method name
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>The bool</returns>
        public bool HasMethod(string methodName)
        {
            return Methods.Any(m => m.Name == methodName);
        }

        /// <summary>
        /// Gets the method using the specified method name
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>The method declaration</returns>
        public MethodDeclaration? GetMethod(string methodName)
        {
            return Methods.FirstOrDefault(m => m.Name == methodName);
        }
    }

    /// <summary>
    /// The method checker class
    /// </summary>
    public class MethodChecker
    {
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly DiagnosticsReporter _diagnostics;
        /// <summary>
        /// The reflection resolver
        /// </summary>
        private readonly ReflectionMethodResolver _reflectionResolver;
        /// <summary>
        /// The type resolver
        /// </summary>
        private readonly ReflectionTypeResolver _typeResolver;
        /// <summary>
        /// The methods
        /// </summary>
        private readonly Dictionary<string, List<MethodSignature>> _methods = new();
        /// <summary>
        /// The class methods
        /// </summary>
        private readonly Dictionary<string, List<MethodSignature>> _classMethods = new();
        /// <summary>
        /// The classes
        /// </summary>
        private readonly Dictionary<string, ClassInfo> _classes = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodChecker"/> class
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        public MethodChecker(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            _reflectionResolver = new ReflectionMethodResolver(diagnostics);
            _typeResolver = new ReflectionTypeResolver(diagnostics);
            RegisterBuiltInMethods();
        }

        /// <summary>
        /// Registers the built in methods
        /// </summary>
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

        /// <summary>
        /// Registers the built in using the specified name
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="paramTypes">The param types</param>
        /// <param name="returnType">The return type</param>
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

        /// <summary>
        /// Registers the method using the specified func
        /// </summary>
        /// <param name="func">The func</param>
        /// <param name="location">The location</param>
        public void RegisterMethod(FunctionDeclaration func, SourceLocation? location = null)
        {
            // Validate attributes first
            ValidateAttributes(func.Attributes, "function", location);

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

        /// <summary>
        /// Registers the method using the specified method
        /// </summary>
        /// <param name="method">The method</param>
        /// <param name="className">The class name</param>
        /// <param name="location">The location</param>
        public void RegisterMethod(MethodDeclaration method, string className, SourceLocation? location = null)
        {
            // Validate attributes first
            ValidateAttributes(method.Attributes, "method", location);

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

        /// <summary>
        /// Registers the class decl
        /// </summary>
        /// <param name="classDecl">The class decl</param>
        /// <param name="location">The location</param>
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

            // Always add the class, even if it has no members
            _classes[classDecl.Name] = classInfo;
            _diagnostics.ReportInfo($"Registered class: {classDecl.Name} with {classInfo.Fields.Count} fields, {classInfo.Methods.Count} methods, and {classInfo.Constructors.Count} constructors");
        }

        /// <summary>
        /// Validates the constructor call using the specified class name
        /// </summary>
        /// <param name="className">The class name</param>
        /// <param name="arguments">The arguments</param>
        /// <param name="callToken">The call token</param>
        /// <returns>The bool</returns>
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
        /// <summary>
        /// Ises the external using the specified class name
        /// </summary>
        /// <param name="className">The class name</param>
        /// <returns>The bool</returns>
        private bool IsExternalClass(string className)
        {
            // Check if this class name appears in any import mappings or known external classes
            // This could be expanded to check against a registry of external/imported classes
            return false; // For now, return false - can be enhanced later
        }

        /// <summary>
        /// Validates the member access using the specified class name
        /// </summary>
        /// <param name="className">The class name</param>
        /// <param name="memberName">The member name</param>
        /// <param name="accessToken">The access token</param>
        /// <returns>The bool</returns>
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
        }        /// <summary>
                 /// Validates the call using the specified function name
                 /// </summary>
                 /// <param name="functionName">The function name</param>
                 /// <param name="arguments">The arguments</param>
                 /// <param name="location">The location</param>
                 /// <returns>The bool</returns>
        public bool ValidateCall(string functionName, List<Expression> arguments, Token location)
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



        /// <summary>
        /// Suggests the similar methods using the specified method name
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="callToken">The call token</param>
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

        // Add method to validate attributes
        /// <summary>
        /// Validates the attributes using the specified attributes
        /// </summary>
        /// <param name="attributes">The attributes</param>
        /// <param name="targetType">The target type</param>
        /// <param name="location">The location</param>
        private void ValidateAttributes(List<AttributeDeclaration>? attributes, string targetType, SourceLocation? location)
        {
            if (attributes == null || attributes.Count == 0)
                return;

            var attributeResolver = _typeResolver.GetAttributeResolver();
            var target = attributeResolver.ConvertToAttributeTarget(targetType);

            foreach (var attribute in attributes)
            {
                // Be more lenient with attribute validation for now
                if (!attributeResolver.TryResolveAttribute(attribute.Name, out var attributeInfos))
                {
                    _diagnostics.ReportWarning($"Unknown attribute: {attribute.Name}. Allowing for now.",
                        location?.Line ?? 0, location?.Column ?? 0, "UH402");
                }
                else
                {
                    attributeResolver.ValidateAttribute(attribute, target, location);
                }
            }
        }

        // Add method to get attribute resolver for other components
        /// <summary>
        /// Gets the attribute resolver
        /// </summary>
        /// <returns>The reflection attribute resolver</returns>
        public ReflectionAttributeResolver GetAttributeResolver()
        {
            return _typeResolver.GetAttributeResolver();
        }

        // Add method to get type resolver for other components
        /// <summary>
        /// Gets the type resolver
        /// </summary>
        /// <returns>The type resolver</returns>
        public ReflectionTypeResolver GetTypeResolver()
        {
            return _typeResolver;
        }

        /// <summary>
        /// Prints the method summary
        /// </summary>
        public void PrintMethodSummary()
        {
            _diagnostics.ReportInfo($"Registered {_classes.Count} user-defined classes");
            _diagnostics.ReportInfo($"Registered {_methods.Values.Sum(list => list.Count)} user-defined global methods");
            _diagnostics.ReportInfo($"Registered {_classMethods.Values.Sum(list => list.Count)} user-defined class methods");
            _diagnostics.ReportInfo($"Discovered {_typeResolver.GetAllTypeNames().Count()} types via reflection");
            _diagnostics.ReportInfo($"Discovered {_typeResolver.GetAllMethodNames().Count()} methods via reflection");
        }

        /// <summary>
        /// Gets the class info using the specified class name
        /// </summary>
        /// <param name="className">The class name</param>
        /// <returns>The class info</returns>
        public ClassInfo? GetClassInfo(string className)
        {
            return _classes.GetValueOrDefault(className);
        }

        /// <summary>
        /// Gets the all class names
        /// </summary>
        /// <returns>A list of string</returns>
        public List<string> GetAllClassNames()
        {
            return _classes.Keys.ToList();
        }

        /// <summary>
        /// Gets the all method names
        /// </summary>
        /// <returns>A list of string</returns>
        public List<string> GetAllMethodNames()
        {
            return _methods.Keys.Concat(_classMethods.Keys).Distinct().ToList();
        }

        /// <summary>
        /// Gets the method signature using the specified method name
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>The method signature</returns>
        public MethodSignature? GetMethodSignature(string methodName)
        {
            if (_methods.ContainsKey(methodName))
                return _methods[methodName].FirstOrDefault();

            if (_classMethods.ContainsKey(methodName))
                return _classMethods[methodName].FirstOrDefault();

            return null;
        }

        /// <summary>
        /// Gets all user-defined class names
        /// </summary>
        /// <returns>A list of string</returns>
        public List<string> GetUserDefinedClassNames()
        {
            return _classes.Keys.ToList();
        }

        /// <summary>
        /// Checks if a type name is a user-defined class
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The bool</returns>
        public bool IsUserDefinedType(string typeName)
        {
            // Check exact match first
            if (_classes.ContainsKey(typeName))
                return true;

            // Check if any registered class ends with this type name (for namespace.class scenario)
            return _classes.Keys.Any(key => key.EndsWith($".{typeName}") || key == typeName);
        }

        /// <summary>
        /// Infers the argument type using the specified argument
        /// </summary>
        /// <param name="argument">The argument</param>
        /// <returns>The string</returns>
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

        /// <summary>
        /// Infers the literal type using the specified literal
        /// </summary>
        /// <param name="literal">The literal</param>
        /// <returns>The string</returns>
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

        /// <summary>
        /// Ises the arithmetic operator using the specified op
        /// </summary>
        /// <param name="op">The op</param>
        /// <returns>The bool</returns>
        private bool IsArithmeticOperator(TokenType op)
        {
            return op is TokenType.Plus or TokenType.Minus or TokenType.Multiply or TokenType.Divide or TokenType.Modulo;
        }

        /// <summary>
        /// Ises the comparison operator using the specified op
        /// </summary>
        /// <param name="op">The op</param>
        /// <returns>The bool</returns>
        private bool IsComparisonOperator(TokenType op)
        {
            return op is TokenType.Equal or TokenType.NotEqual or TokenType.Less or TokenType.Greater
                or TokenType.LessEqual or TokenType.GreaterEqual;
        }

        /// <summary>
        /// Levenshteins the distance using the specified s
        /// </summary>
        /// <param name="s">The </param>
        /// <param name="t">The </param>
        /// <returns>The int</returns>
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

        /// <summary>
        /// Validates the type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="token">The token</param>
        /// <returns>The bool</returns>
        internal bool ValidateType(string typeName, Token token)
        {
            // Check user-defined classes first (both exact and qualified matches)
            if (IsUserDefinedType(typeName))
            {
                return true;
            }

            if (_typeResolver.TryResolveType(typeName, out var type))
            {
                return true;
            }

            _diagnostics.ReportError(
                $"Unknown type: {typeName}",
                token.Line, token.Column, "UH202"
            );

            // Suggest similar types (include user-defined classes - both simple and qualified names)
            var userDefinedTypes = _classes.Keys.Concat(_classes.Keys.Select(k => k.Contains('.') ? k.Split('.').Last() : k)).Distinct();
            var suggestions = _typeResolver.GetAllTypeNames()
                .Concat(userDefinedTypes)
                .Where(name => LevenshteinDistance(typeName, name) <= 2)
                .Take(3)
                .ToList();

            if (suggestions.Any())
            {
                _diagnostics.ReportWarning(
                    $"Did you mean: {string.Join(", ", suggestions)}?",
                    token.Line, token.Column, "UH203");
            }

            return false;
        }
    }
}
