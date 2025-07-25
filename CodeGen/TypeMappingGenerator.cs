using uhigh.Net.Parser;
using System.Text;

namespace uhigh.Net.CodeGen
{
    /// <summary>
    /// Utility class to generate type mappings for different target languages
    /// </summary>
    public class TypeMappingGenerator
    {
        /// <summary>
        /// Generates a mapping dictionary for a target language
        /// </summary>
        /// <param name="targetLanguage">The target language name</param>
        /// <returns>Dictionary of μHigh types to target language types</returns>
        public static Dictionary<string, string> GenerateTypeMappings(string targetLanguage)
        {
            return targetLanguage.ToLowerInvariant() switch
            {
                "csharp" => GenerateCSharpTypeMappings(),
                "js" or "javascript" => GenerateJavaScriptTypeMappings(),
                "cpp" or "c++" => GenerateCppTypeMappings(),
                "python" => GeneratePythonTypeMappings(),
                "rust" => GenerateRustTypeMappings(),
                "llvm" => GenerateLLVMTypeMappings(),
                _ => new Dictionary<string, string>()
            };
        }

        private static Dictionary<string, string> GenerateCSharpTypeMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "int", "int" },
                { "long", "long" },
                { "float", "float" },
                { "double", "double" },
                { "decimal", "decimal" },
                { "byte", "byte" },
                { "sbyte", "sbyte" },
                { "short", "short" },
                { "ushort", "ushort" },
                { "uint", "uint" },
                { "ulong", "ulong" },
                { "bool", "bool" },
                { "string", "string" },
                { "char", "char" },
                { "Guid", "Guid" },
                { "object", "object" },
                { "array", "List<object>" },
                { "array<T>", "List<T>" },
                { "Dictionary<K,V>", "Dictionary<K,V>" },
                { "Set<T>", "HashSet<T>" },
                { "Tuple<T1,T2>", "Tuple<T1,T2>" },
                { "enum", "enum" },
                { "void", "void" },
                { "DateTime", "DateTime" },
                { "any", "object" },
                { "Func<T>", "Func<T>" },
                { "Lambda<T>", "Func<T>" },
                { "Observable<T>", "Observable<T>" }
            };
        }

        private static Dictionary<string, string> GenerateJavaScriptTypeMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "int", "number" },
                { "long", "BigInt" },
                { "float", "number" },
                { "double", "number" },
                { "decimal", "number" },
                { "byte", "number" },
                { "sbyte", "number" },
                { "short", "number" },
                { "ushort", "number" },
                { "uint", "number" },
                { "ulong", "BigInt" },
                { "bool", "boolean" },
                { "string", "string" },
                { "char", "string" },
                { "Guid", "string" },
                { "object", "object" },
                { "array", "Array" },
                { "array<T>", "Array" },
                { "Dictionary<K,V>", "Map" },
                { "Set<T>", "Set" },
                { "Tuple<T1,T2>", "[T1,T2]" },
                { "enum", "object" },
                { "void", "void" },
                { "DateTime", "Date" },
                { "any", "any" },
                { "Func<T>", "Function" },
                { "Lambda<T>", "Function" },
                { "Observable<T>", "Observable" }
            };
        }

        private static Dictionary<string, string> GenerateCppTypeMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "int", "int" },
                { "long", "long long" },
                { "float", "float" },
                { "double", "double" },
                { "decimal", "double" },
                { "byte", "uint8_t" },
                { "sbyte", "int8_t" },
                { "short", "short" },
                { "ushort", "unsigned short" },
                { "uint", "unsigned int" },
                { "ulong", "unsigned long long" },
                { "bool", "bool" },
                { "string", "std::string" },
                { "char", "char" },
                { "Guid", "std::string" },
                { "object", "void*" },
                { "array", "std::vector<void*>" },
                { "array<T>", "std::vector<T>" },
                { "Dictionary<K,V>", "std::map<K,V>" },
                { "Set<T>", "std::set<T>" },
                { "Tuple<T1,T2>", "std::tuple<T1,T2>" },
                { "enum", "enum" },
                { "void", "void" },
                { "null", "nullptr" },
                { "DateTime", "std::chrono::system_clock::time_point" },
                { "any", "auto" },
                { "Func<T>", "std::function<T>" },
                { "Lambda<T>", "std::function<T>" },
                { "Observable<T>", "Observable<T>" }
            };
        }

        // Placeholder for future target languages
        private static Dictionary<string, string> GeneratePythonTypeMappings() => new();
        private static Dictionary<string, string> GenerateRustTypeMappings() => new();
        private static Dictionary<string, string> GenerateLLVMTypeMappings() => new();

        /// <summary>
        /// Generates method mappings for a target language
        /// </summary>
        /// <param name="targetLanguage">The target language name</param>
        /// <returns>Dictionary of μHigh methods to target language methods</returns>
        public static Dictionary<string, string> GenerateMethodMappings(string targetLanguage)
        {
            return targetLanguage.ToLowerInvariant() switch
            {
                "csharp" => GenerateCSharpMethodMappings(),
                "js" or "javascript" => GenerateJavaScriptMethodMappings(),
                "cpp" or "c++" => GenerateCppMethodMappings(),
                _ => new Dictionary<string, string>()
            };
        }

        private static Dictionary<string, string> GenerateCSharpMethodMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Add_to_array", "Add" },
                { "Add_to_set", "Add" },
                { "Add_to_dict", "Add" },
                { "Remove_from_array", "Remove" },
                { "Remove_from_set", "Remove" },
                { "Remove_from_dict", "Remove" },
                { "Length_of_array", "Count" },
                { "Length_of_set", "Count" },
                { "Length_of_dict", "Count" },
                { "Length_of_string", "Length" },
                { "Index_of_array", "[]" },
                { "Index_of_dict", "[]" },
                { "Substring_of", "Substring" },
                { "ToUpper", "ToUpper" },
                { "ToLower", "ToLower" },
                { "Contains_in_array", "Contains" },
                { "Contains_in_set", "Contains" },
                { "Contains_in_dict", "ContainsKey" },
                { "Contains_in_string", "Contains" },
                { "IndexOf_in_array", "IndexOf" },
                { "IndexOf_in_string", "IndexOf" },
                { "Join", "string.Join" },
                { "ToString_of", "ToString" }
            };
        }

        private static Dictionary<string, string> GenerateJavaScriptMethodMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Add_to_array", "push" },
                { "Add_to_set", "add" },
                { "Add_to_dict", "set" },
                { "Remove_from_array", "splice" },
                { "Remove_from_set", "delete" },
                { "Remove_from_dict", "delete" },
                { "Length_of_array", "length" },
                { "Length_of_set", "size" },
                { "Length_of_dict", "size" },
                { "Length_of_string", "length" },
                { "Index_of_array", "[]" },
                { "Index_of_dict", "[]" },
                { "Substring_of", "substring" },
                { "ToUpper", "toUpperCase" },
                { "ToLower", "toLowerCase" },
                { "Contains_in_array", "includes" },
                { "Contains_in_set", "has" },
                { "Contains_in_dict", "has" },
                { "Contains_in_string", "includes" },
                { "IndexOf_in_array", "indexOf" },
                { "IndexOf_in_string", "indexOf" },
                { "Join", "join" },
                { "ToString_of", "toString" }
            };
        }

        private static Dictionary<string, string> GenerateCppMethodMappings()
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Add_to_array", "push_back" },
                { "Add_to_set", "insert" },
                { "Add_to_dict", "insert" },
                { "Remove_from_array", "erase" },
                { "Remove_from_set", "erase" },
                { "Remove_from_dict", "erase" },
                { "Length_of_array", "size" },
                { "Length_of_set", "size" },
                { "Length_of_dict", "size" },
                { "Length_of_string", "length" },
                { "Index_of_array", "[]" },
                { "Index_of_dict", "[]" },
                { "Substring_of", "substr" },
                { "Contains_in_array", "find" },
                { "Contains_in_set", "find" },
                { "Contains_in_dict", "find" },
                { "Contains_in_string", "find" }
            };
        }
    }
}
