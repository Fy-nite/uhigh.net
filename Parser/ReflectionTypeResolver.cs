using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Parser
{
    public class ReflectionTypeResolver
    {
        private readonly DiagnosticsReporter _diagnostics;
        private readonly Dictionary<string, Type> _discoveredTypes = new();
        private readonly Dictionary<string, List<MethodInfo>> _discoveredMethods = new();
        private readonly HashSet<Assembly> _scannedAssemblies = new();
        private readonly Dictionary<string, Type> _genericTypeDefinitions = new(); // Add this

        public ReflectionTypeResolver(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            ScanDefaultAssemblies();
        }

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
            }
            catch (Exception ex)
            {
                _diagnostics.ReportWarning($"Failed to scan assembly {assembly.FullName}: {ex.Message}", 0, 0, "UH301");
            }
        }

        private void RegisterType(Type type)
        {
            // Register type by simple name
            if (!_discoveredTypes.ContainsKey(type.Name))
            {
                _discoveredTypes[type.Name] = type;
            }

            // Register type by full name
            if (!_discoveredTypes.ContainsKey(type.FullName))
            {
                _discoveredTypes[type.FullName] = type;
            }

            // Register shortened namespace.type for common ones
            if (type.Namespace?.StartsWith("System") == true)
            {
                var shortName = type.FullName.Replace("System.", "");
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

        public bool TryResolveType(string typeName, out Type type)
        {
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

            if (_discoveredTypes.TryGetValue(typeName, out type))
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

            // Try partial matching for common types
            var partialMatch = _discoveredTypes.FirstOrDefault(kvp =>
                kvp.Key.EndsWith(typeName, StringComparison.OrdinalIgnoreCase));

            if (!partialMatch.Equals(default(KeyValuePair<string, Type>)))
            {
                type = partialMatch.Value;
                return true;
            }

            type = null;
            return false;
        }

        // Add method to check if a type name is a type parameter
        private bool IsTypeParameter(string typeName)
        {
            // Type parameters are typically single uppercase letters (T, U, V, etc.)
            // or start with T (TKey, TValue, etc.)
            return typeName.Length == 1 && char.IsUpper(typeName[0]) ||
                   typeName.StartsWith("T") && typeName.Length <= 10 && char.IsUpper(typeName[0]);
        }

        // Add new method for generic type resolution
        public bool TryResolveGenericType(string typeName, out Type type)
        {
            type = null;

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
                _ => null
            };
        }

        // Add method to get generic type information
        public bool IsGenericType(string typeName)
        {
            return typeName.Contains('<') && typeName.Contains('>');
        }

        // Add method to get generic type definition
        public bool TryGetGenericTypeDefinition(string baseTypeName, out Type genericTypeDef)
        {
            return _genericTypeDefinitions.TryGetValue(baseTypeName, out genericTypeDef);
        }

        // Add method to get all generic type definitions
        public IEnumerable<string> GetAllGenericTypeNames()
        {
            return _genericTypeDefinitions.Keys;
        }

        public bool TryResolveMethod(string methodName, List<Expression> arguments, out MethodInfo method)
        {
            method = null;

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

        public bool IsValidType(string typeName)
        {
            return TryResolveType(typeName, out _);
        }

        public List<string> GetSimilarTypes(string typeName)
        {
            return _discoveredTypes.Keys
                .Where(name => LevenshteinDistance(typeName, name) <= 2)
                .Take(5)
                .ToList();
        }

        public List<string> GetSimilarMethods(string methodName)
        {
            return _discoveredMethods.Keys
                .Where(name => LevenshteinDistance(methodName, name) <= 2)
                .Take(5)
                .ToList();
        }

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

        public IEnumerable<string> GetAllTypeNames()
        {
            return _discoveredTypes.Keys;
        }

        public IEnumerable<string> GetAllMethodNames()
        {
            return _discoveredMethods.Keys;
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
    }
}
