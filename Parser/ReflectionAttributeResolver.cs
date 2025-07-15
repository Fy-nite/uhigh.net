using System.Reflection;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Parser
{
    /// <summary>
    /// The attribute info class
    /// </summary>
    public class AttributeInfo
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the attribute type
        /// </summary>
        public Type AttributeType { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the parameters
        /// </summary>
        public List<ParameterInfo> Parameters { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the valid targets
        /// </summary>
        public AttributeTargets ValidTargets { get; set; }
        /// <summary>
        /// Gets or sets the value of the allow multiple
        /// </summary>
        public bool AllowMultiple { get; set; }
        /// <summary>
        /// Gets or sets the value of the inherited
        /// </summary>
        public bool Inherited { get; set; }
        /// <summary>
        /// Gets or sets the value of the description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Cans the apply to using the specified target
        /// </summary>
        /// <param name="target">The target</param>
        /// <returns>The bool</returns>
        public bool CanApplyTo(AttributeTargets target)
        {
            return ValidTargets.HasFlag(target);
        }
    }

    /// <summary>
    /// The reflection attribute resolver class
    /// </summary>
    public class ReflectionAttributeResolver
    {
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly DiagnosticsReporter _diagnostics;
        /// <summary>
        /// The discovered attributes
        /// </summary>
        private readonly Dictionary<string, List<AttributeInfo>> _discoveredAttributes = new();
        /// <summary>
        /// The scanned assemblies
        /// </summary>
        private readonly HashSet<Assembly> _scannedAssemblies = new();
        /// <summary>
        /// The type resolver
        /// </summary>
        private readonly ReflectionTypeResolver _typeResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionAttributeResolver"/> class
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="typeResolver">The type resolver</param>
        public ReflectionAttributeResolver(DiagnosticsReporter diagnostics, ReflectionTypeResolver typeResolver)
        {
            _diagnostics = diagnostics;
            _typeResolver = typeResolver;
            ScanDefaultAssemblies();
        }

        /// <summary>
        /// Scans the default assemblies
        /// </summary>
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

        /// <summary>
        /// Registers the attribute using the specified attribute type
        /// </summary>
        /// <param name="attributeType">The attribute type</param>
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

        /// <summary>
        /// Registers the attribute aliases using the specified name
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="attributeInfo">The attribute info</param>
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

        /// <summary>
        /// Tries the resolve attribute using the specified attribute name
        /// </summary>
        /// <param name="attributeName">The attribute name</param>
        /// <param name="attributeInfos">The attribute infos</param>
        /// <returns>The bool</returns>
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

        /// <summary>
        /// Validates the attribute using the specified attribute
        /// </summary>
        /// <param name="attribute">The attribute</param>
        /// <param name="target">The target</param>
        /// <param name="location">The location</param>
        /// <returns>The bool</returns>
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

        /// <summary>
        /// Validates the attribute arguments using the specified attribute
        /// </summary>
        /// <param name="attribute">The attribute</param>
        /// <param name="attributeInfo">The attribute info</param>
        /// <param name="location">The location</param>
        /// <returns>The bool</returns>
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

        /// <summary>
        /// Ises the argument compatible using the specified argument
        /// </summary>
        /// <param name="argument">The argument</param>
        /// <param name="parameterType">The parameter type</param>
        /// <returns>The bool</returns>
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

        /// <summary>
        /// Ises the literal compatible using the specified literal
        /// </summary>
        /// <param name="literal">The literal</param>
        /// <param name="parameterType">The parameter type</param>
        /// <returns>The bool</returns>
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

        /// <summary>
        /// Ises the nullable type using the specified type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The bool</returns>
        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        /// <summary>
        /// Gets the similar attributes using the specified attribute name
        /// </summary>
        /// <param name="attributeName">The attribute name</param>
        /// <returns>A list of string</returns>
        public List<string> GetSimilarAttributes(string attributeName)
        {
            return _discoveredAttributes.Keys
                .Where(name => LevenshteinDistance(attributeName, name) <= 2)
                .Take(5)
                .ToList();
        }

        /// <summary>
        /// Gets the all attribute names
        /// </summary>
        /// <returns>An enumerable of string</returns>
        public IEnumerable<string> GetAllAttributeNames()
        {
            return _discoveredAttributes.Keys;
        }

        /// <summary>
        /// Gets the attributes for target using the specified target
        /// </summary>
        /// <param name="target">The target</param>
        /// <returns>An enumerable of attribute info</returns>
        public IEnumerable<AttributeInfo> GetAttributesForTarget(AttributeTargets target)
        {
            return _discoveredAttributes.Values
                .SelectMany(list => list)
                .Where(attr => attr.CanApplyTo(target))
                .Distinct();
        }

        /// <summary>
        /// Converts the to attribute target using the specified target type
        /// </summary>
        /// <param name="targetType">The target type</param>
        /// <returns>The attribute targets</returns>
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
    }

    /// <summary>
    /// The source location class
    /// </summary>
    public class SourceLocation
    {
        /// <summary>
        /// Gets or sets the value of the line
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// Gets or sets the value of the column
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceLocation"/> class
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        public SourceLocation(int line, int column)
        {
            Line = line;
            Column = column;
        }
    }
}
