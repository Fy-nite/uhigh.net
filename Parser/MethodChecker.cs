using Wake.Net.Diagnostics;
using Wake.Net.Lexer;

namespace Wake.Net.Parser
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
        }

        public bool MatchesCall(string name, List<Expression> arguments)
        {
            if (Name != name) return false;
            
            // Check parameter count first
            if (Parameters.Count != arguments.Count) return false;
            
            // For built-in methods with overloads, we need stricter matching
            if (IsBuiltIn)
            {
                // Built-in methods already have proper type checking in RegisterBuiltIn
                return true;
            }
            
            // For user-defined methods, accept parameter count match for now
            // In the future, we could add type checking based on argument expressions
            return true;
        }
    }

    public class MethodChecker
    {
        private readonly Dictionary<string, List<MethodSignature>> _methods = new();
        private readonly Dictionary<string, List<MethodSignature>> _classMethods = new();
        private readonly DiagnosticsReporter _diagnostics;

        public MethodChecker(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
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

        public bool ValidateCall(string methodName, List<Expression> arguments, Token callToken)
        {
            // Check global methods first
            if (_methods.ContainsKey(methodName))
            {
                var signatures = _methods[methodName];
                var matchingSignature = signatures.FirstOrDefault(s => s.MatchesCall(methodName, arguments));
                
                if (matchingSignature != null)
                    return true;

                // Report parameter count mismatch
                var expectedCounts = signatures.Select(s => s.Parameters.Count).Distinct();
                _diagnostics.ReportError(
                    $"Method '{methodName}' expects {string.Join(" or ", expectedCounts)} parameter(s), but {arguments.Count} were provided",
                    callToken.Line, callToken.Column, "UH200");
                return false;
            }

            // Method not found
            _diagnostics.ReportError(
                $"Method '{methodName}' is not defined",
                callToken.Line, callToken.Column, "UH201");
            
            // Suggest similar method names
            SuggestSimilarMethods(methodName, callToken);
            return false;
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
            _diagnostics.ReportInfo($"Registered {_methods.Values.Sum(list => list.Count)} global methods");
            _diagnostics.ReportInfo($"Registered {_classMethods.Values.Sum(list => list.Count)} class methods");
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
    }
}
