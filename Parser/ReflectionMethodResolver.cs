using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Parser
{
    /// <summary>
    /// The reflection method resolver class
    /// </summary>
    public class ReflectionMethodResolver
    {
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly DiagnosticsReporter _diagnostics;
        /// <summary>
        /// The known types
        /// </summary>
        private readonly Dictionary<string, Type> _knownTypes = new();
        /// <summary>
        /// The loaded assemblies
        /// </summary>
        private readonly HashSet<Assembly> _loadedAssemblies = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionMethodResolver"/> class
        /// </summary>
        /// <param name="diagnostics">The diagnostics</param>
        public ReflectionMethodResolver(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            LoadSystemTypes();
        }

        /// <summary>
        /// Loads the system types
        /// </summary>
        private void LoadSystemTypes()
        {
            // Load common system types
            var systemTypes = new[]
            {
                typeof(Console),
                typeof(Math),
                typeof(string),
                typeof(int),
                typeof(double),
                typeof(DateTime),
                typeof(List<>),
                typeof(Array),
                typeof(Enumerable),
                typeof(Dictionary<,>),
                typeof(Exception),
                typeof(ArgumentException),
                typeof(InvalidOperationException),
                typeof(System.IO.File),
                typeof(System.IO.Directory),
                typeof(System.IO.Path),
                typeof(System.Text.StringBuilder),
                typeof(System.Text.RegularExpressions.Regex),
                typeof(System.Linq.Enumerable),
                typeof(System.Linq.Queryable),
                typeof(System.Threading.Tasks.Task),
                typeof(System.Threading.Tasks.Task<>),
                typeof(System.Threading.Thread),
                typeof(System.Reflection.Assembly),
                typeof(System.Threading.CancellationToken)
            };

            foreach (var type in systemTypes)
            {
                _knownTypes[type.Name] = type;
                if (type.Namespace != null)
                {
                    _knownTypes[$"{type.Namespace}.{type.Name}"] = type;
                }
            }

            // Load current assembly types
            LoadAssembly(Assembly.GetExecutingAssembly());
            
            // Try to load uhigh.StdLib assembly
            try 
            {
                var stdLibAssembly = Assembly.LoadFrom("uhigh.StdLib.dll");
                LoadAssembly(stdLibAssembly);
            }
            catch
            {
                // Try to find it in current app domain
                var stdLibAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name?.Contains("uhigh.StdLib") == true);
                if (stdLibAssembly != null)
                {
                    LoadAssembly(stdLibAssembly);
                }
            }
        }

        /// <summary>
        /// Loads the assembly using the specified assembly
        /// </summary>
        /// <param name="assembly">The assembly</param>
        public void LoadAssembly(Assembly assembly)
        {
            if (_loadedAssemblies.Contains(assembly)) return;
            
            _loadedAssemblies.Add(assembly);
            
            try
            {
                foreach (var type in assembly.GetTypes().Where(t => t.IsPublic))
                {
                    _knownTypes[type.Name] = type;
                    if (type.Namespace != null)
                    {
                        _knownTypes[$"{type.Namespace}.{type.Name}"] = type;
                    }
                }
                _diagnostics.ReportInfo($"Loaded {assembly.GetTypes().Length} types from {assembly.GetName().Name}");
            }
            catch (ReflectionTypeLoadException ex)
            {
                _diagnostics.ReportWarning($"Could not load some types from {assembly.GetName().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Tries the resolve method using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="methodName">The method name</param>
        /// <param name="arguments">The arguments</param>
        /// <param name="method">The method</param>
        /// <returns>The bool</returns>
        public bool TryResolveMethod(string typeName, string methodName, List<Expression> arguments, out MethodInfo? method)
        {
            method = null;
            
            if (!_knownTypes.TryGetValue(typeName, out var type))
            {
                return false;
            }

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                              .Where(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
                              .ToArray();

            if (methods.Length == 0)
            {
                return false;
            }

            // Enhanced overload resolution with type matching
            method = FindBestMethodMatch(methods, arguments);
            return method != null;
        }

        /// <summary>
        /// Finds the best method match using the specified methods
        /// </summary>
        /// <param name="methods">The methods</param>
        /// <param name="arguments">The arguments</param>
        /// <returns>The method info</returns>
        private MethodInfo? FindBestMethodMatch(MethodInfo[] methods, List<Expression> arguments)
        {
            var candidates = new List<(MethodInfo method, int score)>();

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();
                var score = CalculateMatchScore(parameters, arguments);
                
                if (score >= 0) // Valid match
                {
                    candidates.Add((method, score));
                }
            }

            // Return the best match (highest score)
            return candidates.OrderByDescending(c => c.score).FirstOrDefault().method;
        }

        /// <summary>
        /// Calculates the match score using the specified parameters
        /// </summary>
        /// <param name="parameters">The parameters</param>
        /// <param name="arguments">The arguments</param>
        /// <returns>The score</returns>
        private int CalculateMatchScore(ParameterInfo[] parameters, List<Expression> arguments)
        {
            if (parameters.Length != arguments.Count)
            {
                // Check for params array or optional parameters
                if (parameters.Length > 0 && parameters.Last().GetCustomAttribute<ParamArrayAttribute>() != null)
                {
                    return arguments.Count >= parameters.Length - 1 ? 50 : -1; // Lower score for params
                }
                
                // Check for optional parameters
                var requiredParams = parameters.Count(p => !p.HasDefaultValue);
                if (arguments.Count < requiredParams || arguments.Count > parameters.Length)
                {
                    return -1; // Invalid match
                }
                
                return 75; // Lower score for optional parameters
            }

            int score = 100; // Perfect parameter count match

            for (int i = 0; i < Math.Min(parameters.Length, arguments.Count); i++)
            {
                var paramType = parameters[i].ParameterType;
                var argType = InferExpressionType(arguments[i]);

                if (TypesMatch(paramType, argType))
                {
                    score += 10; // Exact type match
                }
                else if (IsImplicitlyConvertible(argType, paramType))
                {
                    score += 5; // Implicit conversion available
                }
                else if (paramType == typeof(object))
                {
                    score += 1; // Can accept any type
                }
                else
                {
                    score -= 20; // Type mismatch penalty
                }
            }

            return score;
        }

        /// <summary>
        /// Infers the expression type using the specified expression
        /// </summary>
        /// <param name="expression">The expression</param>
        /// <returns>The type</returns>
        private Type InferExpressionType(Expression expression)
        {
            return expression switch
            {
                LiteralExpression lit => InferLiteralType(lit),
                IdentifierExpression => typeof(object), // Unknown at compile time
                BinaryExpression => typeof(object), // Would need more analysis
                CallExpression => typeof(object), // Would need return type analysis
                _ => typeof(object)
            };
        }

        /// <summary>
        /// Infers the literal type using the specified literal
        /// </summary>
        /// <param name="literal">The literal</param>
        /// <returns>The type</returns>
        private Type InferLiteralType(LiteralExpression literal)
        {
            return literal.Value?.GetType() ?? typeof(object);
        }

        /// <summary>
        /// Typeses the match using the specified param type
        /// </summary>
        /// <param name="paramType">The param type</param>
        /// <param name="argType">The arg type</param>
        /// <returns>The bool</returns>
        private bool TypesMatch(Type paramType, Type argType)
        {
            if (paramType == argType) return true;
            
            // Handle nullable types
            if (IsNullableType(paramType))
            {
                var underlyingType = Nullable.GetUnderlyingType(paramType);
                return TypesMatch(underlyingType!, argType);
            }

            return false;
        }

        /// <summary>
        /// Ises the implicitly convertible using the specified from
        /// </summary>
        /// <param name="from">The from</param>
        /// <param name="to">The to</param>
        /// <returns>The bool</returns>
        private bool IsImplicitlyConvertible(Type from, Type to)
        {
            if (from == to) return true;
            
            // Check for built-in implicit conversions
            var conversions = new Dictionary<Type, Type[]>
            {
                [typeof(int)] = new[] { typeof(long), typeof(float), typeof(double), typeof(decimal) },
                [typeof(float)] = new[] { typeof(double) },
                [typeof(long)] = new[] { typeof(float), typeof(double), typeof(decimal) },
                [typeof(char)] = new[] { typeof(string) },
                [typeof(string)] = new[] { typeof(object) }
            };

            if (conversions.ContainsKey(from))
            {
                return conversions[from].Contains(to);
            }

            // Check if 'to' is assignable from 'from' (inheritance/interface)
            return to.IsAssignableFrom(from);
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
        /// Gets the known types
        /// </summary>
        /// <returns>An enumerable of type</returns>
        public IEnumerable<Type> GetKnownTypes()
        {
            return _knownTypes.Values.Distinct();
        }

        /// <summary>
        /// Tries the get type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <param name="type">The type</param>
        /// <returns>The bool</returns>
        public bool TryGetType(string typeName, out Type? type)
        {
            return _knownTypes.TryGetValue(typeName, out type);
        }

        /// <summary>
        /// Tries the resolve static method using the specified qualified name
        /// </summary>
        /// <param name="qualifiedName">The qualified name</param>
        /// <param name="arguments">The arguments</param>
        /// <param name="method">The method</param>
        /// <returns>The bool</returns>
        public bool TryResolveStaticMethod(string qualifiedName, List<Expression> arguments, out MethodInfo? method)
        {
            method = null;
            
            var lastDot = qualifiedName.LastIndexOf('.');
            if (lastDot == -1) return false;
            
            var typeName = qualifiedName.Substring(0, lastDot);
            var methodName = qualifiedName.Substring(lastDot + 1);
            
            return TryResolveMethod(typeName, methodName, arguments, out method);
        }

        /// <summary>
        /// Gets the available methods using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>An enumerable of string</returns>
        public IEnumerable<string> GetAvailableMethods(string typeName)
        {
            if (!_knownTypes.TryGetValue(typeName, out var type))
            {
                return Enumerable.Empty<string>();
            }

            return type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                      .Select(m => m.Name)
                      .Distinct();
        }

        /// <summary>
        /// Ises the known type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The bool</returns>
        public bool IsKnownType(string typeName)
        {
            return _knownTypes.ContainsKey(typeName);
        }
    }
}
