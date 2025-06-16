using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Parser
{
    public class ReflectionMethodResolver
    {
        private readonly DiagnosticsReporter _diagnostics;
        private readonly Dictionary<string, Type> _knownTypes = new();
        private readonly HashSet<Assembly> _loadedAssemblies = new();

        public ReflectionMethodResolver(DiagnosticsReporter diagnostics)
        {
            _diagnostics = diagnostics;
            LoadSystemTypes();
        }

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
        }

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

        private Type InferLiteralType(LiteralExpression literal)
        {
            return literal.Value?.GetType() ?? typeof(object);
        }

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

        private bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public IEnumerable<Type> GetKnownTypes()
        {
            return _knownTypes.Values.Distinct();
        }

        public bool TryGetType(string typeName, out Type? type)
        {
            return _knownTypes.TryGetValue(typeName, out type);
        }

        public bool TryResolveStaticMethod(string qualifiedName, List<Expression> arguments, out MethodInfo? method)
        {
            method = null;
            
            var lastDot = qualifiedName.LastIndexOf('.');
            if (lastDot == -1) return false;
            
            var typeName = qualifiedName.Substring(0, lastDot);
            var methodName = qualifiedName.Substring(lastDot + 1);
            
            return TryResolveMethod(typeName, methodName, arguments, out method);
        }

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

        public bool IsKnownType(string typeName)
        {
            return _knownTypes.ContainsKey(typeName);
        }
    }
}
