using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Parser
{
    public class AttributeInfo
    {
        public string Name { get; set; } = "";
        public Type AttributeType { get; set; } = null!;
        public List<ParameterInfo> Parameters { get; set; } = new();
        public AttributeTargets ValidTargets { get; set; }
        public bool AllowMultiple { get; set; }
        public bool Inherited { get; set; }
        public string? Description { get; set; }
        
        public bool CanApplyTo(AttributeTargets target)
        {
            return ValidTargets.HasFlag(target);
        }
    }

    public class ReflectionAttributeResolver
    {
        private readonly DiagnosticsReporter _diagnostics;
        private readonly Dictionary<string, List<AttributeInfo>> _discoveredAttributes = new();
        private readonly HashSet<Assembly> _scannedAssemblies = new();
        private readonly ReflectionTypeResolver _typeResolver;

        public ReflectionAttributeResolver(DiagnosticsReporter diagnostics, ReflectionTypeResolver typeResolver)
        {
            _diagnostics = diagnostics;
            _typeResolver = typeResolver;
            ScanDefaultAssemblies();
        }

        private void ScanDefaultAssemblies()
        {
            // Scan core .NET assemblies for attributes
            ScanAssembly(typeof(object).Assembly);           // System.Private.CoreLib
            ScanAssembly(typeof(System.ComponentModel.DataAnnotations.RequiredAttribute).Assembly); // DataAnnotations
            ScanAssembly(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute).Assembly); // System.Text.Json
            ScanAssembly(typeof(System.Runtime.Serialization.DataMemberAttribute).Assembly); // System.Runtime.Serialization
            
            // Try to scan common attribute assemblies
            try 
            {
                var newtonsoft = Assembly.LoadFrom("Newtonsoft.Json.dll");
                ScanAssembly(newtonsoft);
            }
            catch { /* Ignore if not available */ }

            // Scan current assembly for custom attributes
            ScanAssembly(Assembly.GetExecutingAssembly());

            _diagnostics.ReportInfo($"Discovered {_discoveredAttributes.Count} attribute types via reflection");
        }

        public void ScanAssembly(Assembly assembly)
        {
            if (_scannedAssemblies.Contains(assembly))
                return;

            _scannedAssemblies.Add(assembly);

            try
            {
                var attributeTypes = assembly.GetExportedTypes()
                    .Where(t => t.IsSubclassOf(typeof(Attribute)) && !t.IsAbstract)
                    .ToArray();

                foreach (var attributeType in attributeTypes)
                {
                    RegisterAttribute(attributeType);
                }

                _diagnostics.ReportInfo($"Scanned assembly {assembly.GetName().Name} - found {attributeTypes.Length} attribute types");
            }
            catch (Exception ex)
            {
                _diagnostics.ReportWarning($"Failed to scan assembly {assembly.FullName} for attributes: {ex.Message}", 0, 0, "UH401");
            }
        }

        private void RegisterAttribute(Type attributeType)
        {
            var name = attributeType.Name;
            if (name.EndsWith("Attribute"))
            {
                name = name.Substring(0, name.Length - 9); // Remove "Attribute" suffix
            }

            var attributeInfo = new AttributeInfo
            {
                Name = name,
                AttributeType = attributeType
            };

            // Get AttributeUsage information
            var usageAttr = attributeType.GetCustomAttribute<AttributeUsageAttribute>();
            if (usageAttr != null)
            {
                attributeInfo.ValidTargets = usageAttr.ValidOn;
                attributeInfo.AllowMultiple = usageAttr.AllowMultiple;
                attributeInfo.Inherited = usageAttr.Inherited;
            }
            else
            {
                // Default to allowing all targets if no AttributeUsage is specified
                attributeInfo.ValidTargets = AttributeTargets.All;
            }

            // Get constructor parameters
            var constructors = attributeType.GetConstructors();
            if (constructors.Length > 0)
            {
                // Use the constructor with the most parameters as the primary one
                var primaryConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
                attributeInfo.Parameters = primaryConstructor.GetParameters().ToList();
            }

            // Register by simple name
            if (!_discoveredAttributes.ContainsKey(name))
            {
                _discoveredAttributes[name] = new List<AttributeInfo>();
            }
            _discoveredAttributes[name].Add(attributeInfo);

            // Also register by full name
            var fullName = attributeType.FullName;
            if (fullName != null && !_discoveredAttributes.ContainsKey(fullName))
            {
                _discoveredAttributes[fullName] = new List<AttributeInfo> { attributeInfo };
            }

            // Register common .NET attribute aliases
            RegisterAttributeAliases(name, attributeInfo);
        }

        private void RegisterAttributeAliases(string name, AttributeInfo attributeInfo)
        {
            var aliases = name.ToLower() switch
            {
                "required" => new[] { "Required" },
                "range" => new[] { "Range" },
                "jsonproperty" => new[] { "JsonProperty", "JsonPropertyName" },
                "obsolete" => new[] { "Obsolete", "Deprecated" },
                "serializable" => new[] { "Serializable" },
                "datamember" => new[] { "DataMember" },
                "httpget" => new[] { "HttpGet", "Get" },
                "httppost" => new[] { "HttpPost", "Post" },
                "httpput" => new[] { "HttpPut", "Put" },
                "httpdelete" => new[] { "HttpDelete", "Delete" },
                _ => new string[0]
            };

            foreach (var alias in aliases)
            {
                if (!_discoveredAttributes.ContainsKey(alias))
                {
                    _discoveredAttributes[alias] = new List<AttributeInfo>();
                }
                _discoveredAttributes[alias].Add(attributeInfo);
            }
        }

        public bool TryResolveAttribute(string attributeName, out List<AttributeInfo> attributeInfos)
        {
            attributeInfos = new List<AttributeInfo>();

            if (_discoveredAttributes.TryGetValue(attributeName, out var found))
            {
                attributeInfos = found;
                return true;
            }

            // Try case-insensitive lookup
            var match = _discoveredAttributes.FirstOrDefault(kvp => 
                string.Equals(kvp.Key, attributeName, StringComparison.OrdinalIgnoreCase));
            
            if (!match.Equals(default(KeyValuePair<string, List<AttributeInfo>>)))
            {
                attributeInfos = match.Value;
                return true;
            }

            // Try with "Attribute" suffix
            if (!attributeName.EndsWith("Attribute"))
            {
                if (TryResolveAttribute(attributeName + "Attribute", out attributeInfos))
                {
                    return true;
                }
            }

            // For now, be lenient and allow unknown attributes (they might be custom ones)
            // Create a placeholder attribute info
            var placeholderInfo = new AttributeInfo
            {
                Name = attributeName,
                AttributeType = typeof(Attribute), // Use base Attribute type as placeholder
                ValidTargets = AttributeTargets.All,
                AllowMultiple = true,
                Inherited = true
            };
            
            attributeInfos = new List<AttributeInfo> { placeholderInfo };
            return true; // Allow unknown attributes for now
        }

        public bool ValidateAttribute(AttributeDeclaration attribute, AttributeTargets target, SourceLocation? location = null)
        {
            if (!TryResolveAttribute(attribute.Name, out var attributeInfos))
            {
                var errorLocation = location ?? new SourceLocation(0, 0);
                _diagnostics.ReportError($"Unknown attribute: {attribute.Name}", 
                    errorLocation.Line, errorLocation.Column, "UH402");
                
                // Suggest similar attributes
                var suggestions = GetSimilarAttributes(attribute.Name);
                if (suggestions.Any())
                {
                    _diagnostics.ReportWarning($"Did you mean: {string.Join(", ", suggestions)}?", 
                        errorLocation.Line, errorLocation.Column, "UH403");
                }
                return false;
            }

            // Find the best matching attribute info
            var attributeInfo = attributeInfos.FirstOrDefault(ai => ai.CanApplyTo(target));
            if (attributeInfo == null)
            {
                var errorLocation = location ?? new SourceLocation(0, 0);
                _diagnostics.ReportError($"Attribute '{attribute.Name}' cannot be applied to {target}", 
                    errorLocation.Line, errorLocation.Column, "UH404");
                return false;
            }

            // Validate argument count and types
            return ValidateAttributeArguments(attribute, attributeInfo, location);
        }

        private bool ValidateAttributeArguments(AttributeDeclaration attribute, AttributeInfo attributeInfo, SourceLocation? location)
        {
            var errorLocation = location ?? new SourceLocation(0, 0);
            
            // Check if we have the right number of arguments
            var requiredParams = attributeInfo.Parameters.Count(p => !p.HasDefaultValue);
            var providedArgs = attribute.Arguments.Count;

            if (providedArgs < requiredParams)
            {
                _diagnostics.ReportError(
                    $"Attribute '{attribute.Name}' requires at least {requiredParams} argument(s), but {providedArgs} were provided",
                    errorLocation.Line, errorLocation.Column, "UH405");
                return false;
            }

            if (providedArgs > attributeInfo.Parameters.Count)
            {
                _diagnostics.ReportError(
                    $"Attribute '{attribute.Name}' accepts at most {attributeInfo.Parameters.Count} argument(s), but {providedArgs} were provided",
                    errorLocation.Line, errorLocation.Column, "UH406");
                return false;
            }

            // Validate argument types (basic validation)
            for (int i = 0; i < Math.Min(providedArgs, attributeInfo.Parameters.Count); i++)
            {
                var param = attributeInfo.Parameters[i];
                var arg = attribute.Arguments[i];
                
                if (!IsArgumentCompatible(arg, param.ParameterType))
                {
                    _diagnostics.ReportWarning(
                        $"Argument {i + 1} for attribute '{attribute.Name}' may not be compatible with parameter type {param.ParameterType.Name}",
                        errorLocation.Line, errorLocation.Column, "UH407");
                }
            }

            return true;
        }

        private bool IsArgumentCompatible(Expression argument, Type parameterType)
        {
            // Basic type compatibility checking
            return argument switch
            {
                LiteralExpression lit => IsLiteralCompatible(lit, parameterType),
                IdentifierExpression => true, // Can't validate at compile time
                _ => true // Allow other expressions for now
            };
        }

        private bool IsLiteralCompatible(LiteralExpression literal, Type parameterType)
        {
            return literal.Value switch
            {
                string when parameterType == typeof(string) => true,
                int when parameterType == typeof(int) || parameterType == typeof(long) => true,
                double when parameterType == typeof(double) || parameterType == typeof(float) => true,
                bool when parameterType == typeof(bool) => true,
                null when !parameterType.IsValueType || IsNullableType(parameterType) => true,
                _ => parameterType == typeof(object)
            };
        }

        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public List<string> GetSimilarAttributes(string attributeName)
        {
            return _discoveredAttributes.Keys
                .Where(name => LevenshteinDistance(attributeName, name) <= 2)
                .Take(5)
                .ToList();
        }

        public IEnumerable<string> GetAllAttributeNames()
        {
            return _discoveredAttributes.Keys;
        }

        public IEnumerable<AttributeInfo> GetAttributesForTarget(AttributeTargets target)
        {
            return _discoveredAttributes.Values
                .SelectMany(list => list)
                .Where(attr => attr.CanApplyTo(target))
                .Distinct();
        }

        public AttributeTargets ConvertToAttributeTarget(string targetType)
        {
            return targetType.ToLower() switch
            {
                "class" => AttributeTargets.Class,
                "method" => AttributeTargets.Method,
                "function" => AttributeTargets.Method,
                "property" => AttributeTargets.Property,
                "field" => AttributeTargets.Field,
                "parameter" => AttributeTargets.Parameter,
                "enum" => AttributeTargets.Enum,
                "interface" => AttributeTargets.Interface,
                "assembly" => AttributeTargets.Assembly,
                "module" => AttributeTargets.Module,
                _ => AttributeTargets.All
            };
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

    public class SourceLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }

        public SourceLocation(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }
}
