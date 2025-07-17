using System.Reflection;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Parser
{
    /// <summary>
    /// The reflection type resolver class
    /// </summary>
    public class ReflectionTypeResolver
    {
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly DiagnosticsReporter _diagnostics;
        /// <summary>
        /// The discovered types
        /// </summary>
        private readonly Dictionary<string, Type> _discoveredTypes = new();
        /// <summary>
        /// The discovered methods
        /// </summary>
        private readonly Dictionary<string, List<MethodInfo>> _discoveredMethods = new();
        /// <summary>
        /// The scanned assemblies
        /// </summary>
        private readonly HashSet<Assembly> _scannedAssemblies = new();
        /// <summary>
        /// The generic type definitions
        /// </summary>
        private readonly Dictionary<string, Type> _genericTypeDefinitions = new();
        /// <summary>
        /// The attribute resolver
        /// </summary>
        private ReflectionAttributeResolver? _attributeResolver; // Add this

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionTypeResolver"/> class
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        public ReflectionTypeResolver(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            ScanDefaultAssemblies();
            InitializeAttributeResolver(); // Add this
        }

        /// <summary>
        /// Initializes the attribute resolver
        /// </summary>
        private void InitializeAttributeResolver()
        {
            _attributeResolver = new ReflectionAttributeResolver(_diagnostics, this);
        }

        /// <summary>
        /// Gets the attribute resolver
        /// </summary>
        /// <exception cref="InvalidOperationException">Attribute resolver not initialized</exception>
        /// <returns>The reflection attribute resolver</returns>
        public ReflectionAttributeResolver GetAttributeResolver()
        {
            return _attributeResolver ?? throw new InvalidOperationException("Attribute resolver not initialized");
        }

        /// <summary>
        /// Scans the default assemblies
        /// </summary>
        private void ScanDefaultAssemblies()
        {
            // Scan core .NET assemblies
            ScanAssembly(typeof(object).Assembly);           // mscorlib/System.Private.CoreLib
            ScanAssembly(typeof(Console).Assembly);          // System.Console
            ScanAssembly(typeof(List<>).Assembly);           // System.Collections
            ScanAssembly(typeof(Enumerable).Assembly);       // System.Linq
            ScanAssembly(typeof(System.IO.File).Assembly);   // System.IO
            ScanAssembly(typeof(System.Text.StringBuilder).Assembly); // System.Text
            ScanAssembly(typeof(System.Threading.Tasks.Task).Assembly); // System.Threading.Tasks
            ScanAssembly(typeof(System.Collections.Generic.Dictionary<,>).Assembly); // System.Collections.Generic
            ScanAssembly(typeof(System.Collections.IEnumerable).Assembly); // System.Collections
            ScanAssembly(typeof(System.Collections.Generic.List<>).Assembly); // System.Collections.Generic.List
            ScanAssembly(typeof(System.Collections.Generic.HashSet<>).Assembly); // System.Collections.Generic.HashSet

            // Try to scan uhigh.StdLib assembly more reliably
            try
            {
                var stdLibAssembly = Assembly.LoadFrom("uhigh.StdLib.dll");
                ScanAssembly(stdLibAssembly);
            }
            catch
            {
                // Try to find it in current app domain
                var stdLibAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name?.Contains("uhigh.StdLib") == true);
                if (stdLibAssembly != null)
                {
                    ScanAssembly(stdLibAssembly);
                }
            }

            // Scan current assembly for custom types
            ScanAssembly(Assembly.GetExecutingAssembly());

            _diagnostics.ReportInfo($"Discovered {_discoveredTypes.Count} types and {_discoveredMethods.Values.Sum(m => m.Count)} methods via reflection");
        }

        /// <summary>
        /// Scans the assembly using the specified assembly
        /// </summary>
        /// <param name="assembly">The assembly</param>
        public void ScanAssembly(Assembly assembly)
        {
            if (_scannedAssemblies.Contains(assembly))
                return;

            _scannedAssemblies.Add(assembly);

            try
            {
                var types = assembly.GetExportedTypes();
                foreach (var type in types)
                {
                    RegisterType(type);
                }
                _diagnostics.ReportInfo($"Scanned assembly {assembly.GetName().Name} - found {types.Length} types");

                // Also scan for attributes
                _attributeResolver?.ScanAssembly(assembly);
            }
            catch (Exception ex)
            {
                _diagnostics.ReportWarning($"Failed to scan assembly {assembly.FullName}: {ex.Message}", 0, 0, "UH301");
            }
        }

        /// <summary>
        /// Registers the type using the specified type
        /// </summary>
        /// <param name="type">The type</param>
        private void RegisterType(Type type)
        {
            // Register type by simple name
            if (!_discoveredTypes.ContainsKey(type.Name))
            {
                _discoveredTypes[type.Name] = type;
            }

            // Register type by full name
            if (!_discoveredTypes.ContainsKey(type.FullName!))
            {
                _discoveredTypes[type.FullName!] = type;
            }

            // Register shortened namespace.type for common ones
            if (type.Namespace?.StartsWith("System") == true)
            {
                var shortName = type.FullName!.Replace("System.", "");
                if (!_discoveredTypes.ContainsKey(shortName))
                {
                    _discoveredTypes[shortName] = type;
                }
            }

            // Register generic type definitions
            if (type.IsGenericTypeDefinition)
            {
                var genericName = type.Name.Split('`')[0]; // Remove `1, `2, etc.
                if (!_genericTypeDefinitions.ContainsKey(genericName))
                {
                    _genericTypeDefinitions[genericName] = type;
                }

                // Also register with full namespace
                if (!string.IsNullOrEmpty(type.Namespace))
                {
                    var fullGenericName = $"{type.Namespace}.{genericName}";
                    if (!_genericTypeDefinitions.ContainsKey(fullGenericName))
                    {
                        _genericTypeDefinitions[fullGenericName] = type;
                    }
                }
            }

            // Discover methods for this type
            DiscoverMethods(type);
        }

        /// <summary>
        /// Discovers the methods using the specified type
        /// </summary>
        /// <param name="type">The type</param>
        private void DiscoverMethods(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

            foreach (var method in methods)
            {
                // Register by simple method name
                var methodName = method.Name;
                if (!_discoveredMethods.ContainsKey(methodName))
                {
                    _discoveredMethods[methodName] = new List<MethodInfo>();
                }
                _discoveredMethods[methodName].Add(method);

                // Register by qualified name (Type.Method)
                var qualifiedName = $"{type.Name}.{method.Name}";
                if (!_discoveredMethods.ContainsKey(qualifiedName))
                {
                    _discoveredMethods[qualifiedName] = new List<MethodInfo>();
                }
                _discoveredMethods[qualifiedName].Add(method);

                // Register by full qualified name (Namespace.Type.Method)
                if (!string.IsNullOrEmpty(type.FullName))
                {
                    var fullQualifiedName = $"{type.FullName}.{method.Name}";
                    if (!_discoveredMethods.ContainsKey(fullQualifiedName))
                    {
                        _discoveredMethods[fullQualifiedName] = new List<MethodInfo>();
                    }
                    _discoveredMethods[fullQualifiedName].Add(method);
                }
            }
        }

        /// <summary>
        /// Tries the resolve type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="type">The type</param>
        /// <returns>The bool</returns>
        public bool TryResolveType(string typeName, out Type type)
        {
            // Check user-defined types first (if provided)
            if (UserTypeResolver != null)
            {
                var userType = UserTypeResolver(typeName);
                if (userType != null)
                {
                    type = userType;
                    return true;
                }
            }

            // Handle array syntax first (e.g., string[], int[])
            if (typeName.EndsWith("[]"))
            {
                var elementTypeName = typeName.Substring(0, typeName.Length - 2);
                if (TryResolveType(elementTypeName, out var elementType))
                {
                    type = elementType.MakeArrayType();
                    return true;
                }
            }

            // Handle type parameters (T, U, etc.) - allow them through
            if (IsTypeParameter(typeName))
            {
                type = typeof(object); // Placeholder for type parameters
                return true;
            }

            // Try to resolve generic type first
            if (TryResolveGenericType(typeName, out type))
            {
                return true;
            }

            if (_discoveredTypes.TryGetValue(typeName, out type!))
            {
                return true;
            }

            // Try case-insensitive lookup
            var match = _discoveredTypes.FirstOrDefault(kvp =>
                string.Equals(kvp.Key, typeName, StringComparison.OrdinalIgnoreCase));

            if (!match.Equals(default(KeyValuePair<string, Type>)))
            {
                type = match.Value;
                return true;
            }


            // base generic types like System.Int32, System.String, etc.
            switch (typeName.ToLowerInvariant())
            {
                case "int":
                case "int32":
                    type = typeof(int);
                    return true;
                case "string":
                case "str":
                    type = typeof(string);
                    return true;
                case "bool":
                case "boolean":
                    type = typeof(bool);
                    return true;
                case "double":
                case "float":
                    type = typeof(double);
                    return true;
                case "decimal":
                    type = typeof(decimal);
                    return true;
                case "object":
                    type = typeof(object);
                    return true;
                case "void":
                    type = typeof(void);
                    return true;
            }

            // partial matching is broken for now.

            // // Try partial matching for common types if nothing else worked
            // // This is useful for cases like "list" or "dictionary"
            // var partialMatch = _discoveredTypes.FirstOrDefault(kvp =>
            //     kvp.Key.EndsWith(typeName, StringComparison.OrdinalIgnoreCase));

            // if (!partialMatch.Equals(default(KeyValuePair<string, Type>)))
            // {
            //     type = partialMatch.Value;
            //     return true;
            // }

            type = null!;
            return false;
        }

        // Add method to check if a type name is a type parameter
        /// <summary>
        /// Ises the type parameter using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The bool</returns>
        private bool IsTypeParameter(string typeName)
        {
            // Type parameters are typically single uppercase letters (T, U, V, etc.)
            // or start with T (TKey, TValue, etc.)
            return typeName.Length == 1 && char.IsUpper(typeName[0]) ||
                   typeName.StartsWith("T") && typeName.Length <= 10 && char.IsUpper(typeName[0]);
        }

        // Add new method for generic type resolution
        /// <summary>
        /// Tries the resolve generic type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="type">The type</param>
        /// <returns>The bool</returns>
        public bool TryResolveGenericType(string typeName, out Type type)
        {
            type = null!;

            // Check if it's a generic type syntax like "List<string>" or "Dictionary<string, int>"
            if (!typeName.Contains('<') || !typeName.Contains('>'))
            {
                return false;
            }

            try
            {
                var genericMatch = System.Text.RegularExpressions.Regex.Match(typeName, @"^([^<]+)<(.+)>$");
                if (!genericMatch.Success)
                {
                    return false;
                }

                var baseTypeName = genericMatch.Groups[1].Value.Trim();
                var typeArgsString = genericMatch.Groups[2].Value.Trim();

                // Find the generic type definition
                if (!_genericTypeDefinitions.TryGetValue(baseTypeName, out var genericTypeDef))
                {
                    // Try with common aliases
                    var aliasedType = TryResolveTypeAlias(baseTypeName);
                    if (aliasedType != null && _genericTypeDefinitions.ContainsKey(aliasedType))
                    {
                        genericTypeDef = _genericTypeDefinitions[aliasedType];
                    }
                    else
                    {
                        // Try case-insensitive lookup for generic types
                        var genericMatch2 = _genericTypeDefinitions.FirstOrDefault(kvp =>
                            string.Equals(kvp.Key, baseTypeName, StringComparison.OrdinalIgnoreCase));

                        if (!genericMatch2.Equals(default(KeyValuePair<string, Type>)))
                        {
                            genericTypeDef = genericMatch2.Value;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                // Parse type arguments
                var typeArgNames = ParseGenericTypeArguments(typeArgsString);
                var typeArgs = new Type[typeArgNames.Count];

                for (int i = 0; i < typeArgNames.Count; i++)
                {
                    // Handle type parameters in generic arguments
                    if (IsTypeParameter(typeArgNames[i]))
                    {
                        typeArgs[i] = typeof(object); // Placeholder for type parameters
                    }
                    else if (!TryResolveType(typeArgNames[i], out var argType))
                    {
                        return false;
                    }
                    else
                    {
                        typeArgs[i] = argType;
                    }
                }

                // Validate type argument count
                if (typeArgs.Length != genericTypeDef.GetGenericArguments().Length)
                {
                    _diagnostics.ReportWarning($"Generic type {baseTypeName} expects {genericTypeDef.GetGenericArguments().Length} type arguments, but {typeArgs.Length} were provided");
                    return false;
                }

                // Create the generic type
                type = genericTypeDef.MakeGenericType(typeArgs);

                // Cache the resolved type for future use
                _discoveredTypes[typeName] = type;

                return true;
            }
            catch (Exception ex)
            {
                _diagnostics.ReportWarning($"Failed to resolve generic type '{typeName}': {ex.Message}");
                return false;
            }
        }

        // Add helper method to parse generic type arguments
        /// <summary>
        /// Parses the generic type arguments using the specified type args string
        /// </summary>
        /// <param name="typeArgsString">The type args string</param>
        /// <returns>The type args</returns>
        private List<string> ParseGenericTypeArguments(string typeArgsString)
        {
            var typeArgs = new List<string>();
            var current = "";
            var depth = 0;

            for (int i = 0; i < typeArgsString.Length; i++)
            {
                var c = typeArgsString[i];

                if (c == '<')
                {
                    depth++;
                    current += c;
                }
                else if (c == '>')
                {
                    depth--;
                    current += c;
                }
                else if (c == ',' && depth == 0)
                {
                    if (!string.IsNullOrWhiteSpace(current))
                    {
                        typeArgs.Add(current.Trim());
                    }
                    current = "";
                }
                else if (!char.IsWhiteSpace(c) || depth > 0)
                {
                    current += c;
                }
            }

            if (!string.IsNullOrWhiteSpace(current))
            {
                typeArgs.Add(current.Trim());
            }

            return typeArgs;
        }

        // Add method to resolve common type aliases
        /// <summary>
        /// Tries the resolve type alias using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The string</returns>
        private string TryResolveTypeAlias(string typeName)
        {
            return typeName switch
            {
                "list" => "List",
                "dictionary" => "Dictionary",
                "map" => "Dictionary",
                "set" => "HashSet",
                "queue" => "Queue",
                "stack" => "Stack",
                "array" => "Array",
                _ => null!
            };
        }

        // Add method to get generic type information
        /// <summary>
        /// Ises the generic type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The bool</returns>
        public bool IsGenericType(string typeName)
        {
            return typeName.Contains('<') && typeName.Contains('>');
        }

        // Add method to get generic type definition
        /// <summary>
        /// Tries the get generic type definition using the specified base type name
        /// </summary>
        /// <param name="baseTypeName">The base type name</param>
        /// <param name="genericTypeDef">The generic type def</param>
        /// <returns>The bool</returns>
        public bool TryGetGenericTypeDefinition(string baseTypeName, out Type? genericTypeDef)
        {
            if (_genericTypeDefinitions.TryGetValue(baseTypeName, out genericTypeDef))
            {
                return true;
            }

            // Try to find the generic type definition
            var candidates = new[]
            {
                $"System.Collections.Generic.{baseTypeName}`1",
                $"System.Collections.Generic.{baseTypeName}`2",
                $"System.{baseTypeName}`1",
                $"System.{baseTypeName}`2"
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    genericTypeDef = Type.GetType(candidate);
                    if (genericTypeDef != null && genericTypeDef.IsGenericTypeDefinition)
                    {
                        _genericTypeDefinitions[baseTypeName] = genericTypeDef;
                        return true;
                    }
                }
                catch
                {
                    // Continue searching
                }
            }

            genericTypeDef = null;
            return false;
        }

        // Add method to get all generic type definitions
        /// <summary>
        /// Gets the all generic type names
        /// </summary>
        /// <returns>An enumerable of string</returns>
        public IEnumerable<string> GetAllGenericTypeNames()
        {
            return _genericTypeDefinitions.Keys;
        }

        /// <summary>
        /// Tries the resolve method using the specified method name
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <param name="arguments">The arguments</param>
        /// <param name="method">The method</param>
        /// <returns>The bool</returns>
        public bool TryResolveMethod(string methodName, List<Expression> arguments, out MethodInfo method)
        {
            method = null!;

            if (!_discoveredMethods.TryGetValue(methodName, out var candidates))
            {
                return false;
            }

            // Find best matching method based on parameter count and types
            foreach (var candidate in candidates)
            {
                if (IsMethodMatch(candidate, arguments))
                {
                    method = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Ises the method match using the specified method
        /// </summary>
        /// <param name="method">The method</param>
        /// <param name="arguments">The arguments</param>
        /// <returns>The bool</returns>
        private bool IsMethodMatch(MethodInfo method, List<Expression> arguments)
        {
            var parameters = method.GetParameters();

            // Check parameter count (allowing for params arrays)
            if (parameters.Length != arguments.Count)
            {
                // Check if last parameter is params array
                if (parameters.Length > 0 &&
                    parameters.Last().GetCustomAttribute<ParamArrayAttribute>() != null &&
                    arguments.Count >= parameters.Length - 1)
                {
                    return true; // Params array can handle variable arguments
                }
                return false;
            }

            // For now, just check parameter count
            // Could add more sophisticated type checking here
            return true;
        }

        /// <summary>
        /// Ises the valid type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The bool</returns>
        public bool IsValidType(string typeName)
        {
            return TryResolveType(typeName, out _);
        }

        /// <summary>
        /// Gets the similar types using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>A list of string</returns>
        public List<string> GetSimilarTypes(string typeName)
        {
            // Prioritize types that start with the input, then others by Levenshtein distance
            var startsWith = _discoveredTypes.Keys
                .Where(name => name.StartsWith(typeName, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            if (startsWith.Count > 0)
                return startsWith;

            return _discoveredTypes.Keys
                .OrderBy(name => LevenshteinDistance(typeName, name))
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// Gets the similar methods using the specified method name
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>A list of string</returns>
        public List<string> GetSimilarMethods(string methodName)
        {
            // Prioritize methods that start with the input, then others by Levenshtein distance
            var startsWith = _discoveredMethods.Keys
                .Where(name => name.StartsWith(methodName, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            if (startsWith.Count > 0)
                return startsWith;

            return _discoveredMethods.Keys
                .OrderBy(name => LevenshteinDistance(methodName, name))
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// Loads the assembly from file using the specified assembly path
        /// </summary>
        /// <param name="assemblyPath">The assembly path</param>
        public void LoadAssemblyFromFile(string assemblyPath)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                ScanAssembly(assembly);
                _diagnostics.ReportInfo($"Loaded and scanned assembly: {assemblyPath}");
            }
            catch (Exception ex)
            {
                _diagnostics.ReportError($"Failed to load assembly {assemblyPath}: {ex.Message}", 0, 0, "UH302");
            }
        }

        /// <summary>
        /// Gets the all type names
        /// </summary>
        /// <returns>An enumerable of string</returns>
        public IEnumerable<string> GetAllTypeNames()
        {
            return _discoveredTypes.Keys;
        }

        /// <summary>
        /// Gets the all method names
        /// </summary>
        /// <returns>An enumerable of string</returns>
        public IEnumerable<string> GetAllMethodNames()
        {
            return _discoveredMethods.Keys;
        }

        /// <summary>
        /// Levenshteins the distance using the specified a
        /// </summary>
        /// <param name="a">The </param>
        /// <param name="b">The </param>
        /// <returns>The int</returns>
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

        /// <summary>
        /// Optional callback to check for user-defined types
        /// </summary>
        public Func<string, Type?>? UserTypeResolver { get; set; }
    }
}
