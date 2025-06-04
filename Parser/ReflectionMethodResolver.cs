using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wake.Net.Diagnostics;

namespace Wake.Net.Parser
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
                typeof(Enumerable)
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

            // Simple overload resolution - find best match by parameter count
            method = methods.FirstOrDefault(m => m.GetParameters().Length == arguments.Count) 
                     ?? methods.FirstOrDefault();

            return method != null;
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
