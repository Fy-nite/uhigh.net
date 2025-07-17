using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;

namespace StdLib
{
    /// <summary>
    /// Serialization format enumeration
    /// </summary>
    public enum SerializationFormat
    {
        Json,
        Xml,
        Csv,
        Yaml,
        Binary
    }

    /// <summary>
    /// Serialization options for controlling output format
    /// </summary>
    public class SerializationOptions
    {
        /// <summary>
        /// Gets or sets whether to indent the output for readability
        /// </summary>
        public bool Indent { get; set; } = true;

        /// <summary>
        /// Gets or sets the property naming policy for JSON
        /// </summary>
        public JsonNamingPolicy? PropertyNamingPolicy { get; set; } = JsonNamingPolicy.CamelCase;

        /// <summary>
        /// Gets or sets whether to include null values in JSON output
        /// </summary>
        public bool IncludeNullValues { get; set; } = false;

        /// <summary>
        /// Gets or sets the encoding for text-based formats
        /// </summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>
        /// Gets or sets the CSV delimiter
        /// </summary>
        public string CsvDelimiter { get; set; } = ",";

        /// <summary>
        /// Gets or sets whether to include headers in CSV output
        /// </summary>
        public bool CsvIncludeHeaders { get; set; } = true;
    }

    /// <summary>
    /// Comprehensive serialization utility class
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Default JSON serializer options
        /// </summary>
        private static readonly JsonSerializerOptions DefaultJsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            AllowTrailingCommas = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Serialize an object to the specified format
        /// </summary>
        /// <typeparam name="T">The type of object to serialize</typeparam>
        /// <param name="obj">The object to serialize</param>
        /// <param name="format">The serialization format</param>
        /// <param name="options">Serialization options</param>
        /// <returns>The serialized string</returns>
        public static string Serialize<T>(T obj, SerializationFormat format = SerializationFormat.Json, SerializationOptions? options = null)
        {
            options ??= new SerializationOptions();

            return format switch
            {
                SerializationFormat.Json => SerializeToJson(obj, options),
                SerializationFormat.Xml => SerializeToXml(obj, options),
                SerializationFormat.Csv => SerializeToCsv(obj, options),
                SerializationFormat.Yaml => SerializeToYaml(obj, options),
                SerializationFormat.Binary => throw new NotSupportedException("Binary serialization requires byte[] return type"),
                _ => throw new ArgumentException($"Unsupported serialization format: {format}")
            };
        }

        /// <summary>
        /// Deserialize a string to the specified type
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="data">The serialized data</param>
        /// <param name="format">The serialization format</param>
        /// <param name="options">Serialization options</param>
        /// <returns>The deserialized object</returns>
        public static T? Deserialize<T>(string data, SerializationFormat format = SerializationFormat.Json, SerializationOptions? options = null)
        {
            options ??= new SerializationOptions();

            return format switch
            {
                SerializationFormat.Json => DeserializeFromJson<T>(data, options),
                SerializationFormat.Xml => DeserializeFromXml<T>(data, options),
                SerializationFormat.Csv => DeserializeFromCsv<T>(data, options),
                SerializationFormat.Yaml => DeserializeFromYaml<T>(data, options),
                SerializationFormat.Binary => throw new NotSupportedException("Binary deserialization requires byte[] input"),
                _ => throw new ArgumentException($"Unsupported serialization format: {format}")
            };
        }

        /// <summary>
        /// Serialize to JSON format
        /// </summary>
        private static string SerializeToJson<T>(T obj, SerializationOptions options)
        {
            var jsonOptions = new JsonSerializerOptions(DefaultJsonOptions)
            {
                WriteIndented = options.Indent,
                PropertyNamingPolicy = options.PropertyNamingPolicy
            };

            if (!options.IncludeNullValues)
            {
                jsonOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            }

            return JsonSerializer.Serialize(obj, jsonOptions);
        }

        /// <summary>
        /// Deserialize from JSON format
        /// </summary>
        private static T? DeserializeFromJson<T>(string json, SerializationOptions options)
        {
            var jsonOptions = new JsonSerializerOptions(DefaultJsonOptions)
            {
                PropertyNamingPolicy = options.PropertyNamingPolicy
            };

            return JsonSerializer.Deserialize<T>(json, jsonOptions);
        }

        /// <summary>
        /// Serialize to XML format
        /// </summary>
        private static string SerializeToXml<T>(T obj, SerializationOptions options)
        {
            var serializer = new XmlSerializer(typeof(T));
            var settings = new XmlWriterSettings
            {
                Indent = options.Indent,
                IndentChars = "  ",
                Encoding = options.Encoding,
                OmitXmlDeclaration = false
            };

            using var stringWriter = new StringWriter();
            using var xmlWriter = XmlWriter.Create(stringWriter, settings);

            serializer.Serialize(xmlWriter, obj);
            return stringWriter.ToString();
        }

        /// <summary>
        /// Deserialize from XML format
        /// </summary>
        private static T? DeserializeFromXml<T>(string xml, SerializationOptions options)
        {
            var serializer = new XmlSerializer(typeof(T));
            using var stringReader = new StringReader(xml);
            return (T?)serializer.Deserialize(stringReader);
        }

        /// <summary>
        /// Serialize to CSV format (for collections)
        /// </summary>
        private static string SerializeToCsv<T>(T obj, SerializationOptions options)
        {
            if (obj is not System.Collections.IEnumerable enumerable)
            {
                throw new ArgumentException("CSV serialization requires an enumerable object");
            }

            var sb = new StringBuilder();
            var items = enumerable.Cast<object>().ToList();

            if (items.Count == 0)
                return string.Empty;

            var firstItem = items.First();
            var properties = firstItem.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // Write headers
            if (options.CsvIncludeHeaders)
            {
                sb.AppendLine(string.Join(options.CsvDelimiter, properties.Select(p => p.Name)));
            }

            // Write data rows
            foreach (var item in items)
            {
                var values = properties.Select(p =>
                {
                    var value = p.GetValue(item)?.ToString() ?? string.Empty;
                    // Escape CSV values containing delimiter or quotes
                    if (value.Contains(options.CsvDelimiter) || value.Contains("\"") || value.Contains("\n"))
                    {
                        value = "\"" + value.Replace("\"", "\"\"") + "\"";
                    }
                    return value;
                });

                sb.AppendLine(string.Join(options.CsvDelimiter, values));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Deserialize from CSV format
        /// </summary>
        private static T? DeserializeFromCsv<T>(string csv, SerializationOptions options)
        {
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
                return default;

            // Get the type we're deserializing to
            var targetType = typeof(T);

            // If T is a collection type, get the element type
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Handle List<T> deserialization
                var elementType = targetType.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(elementType);
                var list = Activator.CreateInstance(listType);
                // TODO: implement CSV to List<T> deserialization
                return (T?)list;
            }
            else if (targetType.IsArray)
            {
                // Handle T[] deserialization
                var elementType = targetType.GetElementType();
                // TODO: implement CSV to T[] deserialization
                return default;
            }
            else
            {
                // Handle single object deserialization
                // TODO: implement CSV to single object deserialization
                return default;
            }
        }

        /// <summary>
        /// Parse a CSV line handling quoted values
        /// </summary>
        private static string[] ParseCsvLine(string line, string delimiter)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        // Escaped quote
                        current.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (!inQuotes && line.Substring(i).StartsWith(delimiter))
                {
                    result.Add(current.ToString());
                    current.Clear();
                    i += delimiter.Length - 1; // Skip delimiter
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Convert string value to target type
        /// </summary>
        private static object? ConvertValue(string value, Type targetType)
        {
            if (string.IsNullOrEmpty(value))
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType)!;
            }

            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int))
                return int.TryParse(value, out var intVal) ? intVal : 0;

            if (targetType == typeof(double))
                return double.TryParse(value, out var doubleVal) ? doubleVal : 0.0;

            if (targetType == typeof(bool))
                return bool.TryParse(value, out var boolVal) ? boolVal : false;

            if (targetType == typeof(DateTime))
                return DateTime.TryParse(value, out var dateVal) ? dateVal : DateTime.MinValue;

            // Use Convert.ChangeType for other types
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        /// <summary>
        /// Basic YAML serialization (simplified implementation)
        /// </summary>
        private static string SerializeToYaml<T>(T obj, SerializationOptions options)
        {
            // This is a simplified YAML serializer
            // For production use, consider using a proper YAML library like YamlDotNet
            var json = SerializeToJson(obj, options);
            return ConvertJsonToYaml(json);
        }

        /// <summary>
        /// Basic YAML deserialization (simplified implementation)
        /// </summary>
        private static T? DeserializeFromYaml<T>(string yaml, SerializationOptions options)
        {
            // This is a simplified YAML deserializer
            // For production use, consider using a proper YAML library like YamlDotNet
            var json = ConvertYamlToJson(yaml);
            return DeserializeFromJson<T>(json, options);
        }

        /// <summary>
        /// Convert JSON to basic YAML format
        /// </summary>
        private static string ConvertJsonToYaml(string json)
        {
            // Simplified JSON to YAML conversion
            // This handles basic cases - for complex scenarios use a proper YAML library
            var lines = json.Split('\n');
            var result = new StringBuilder();
            var indentLevel = 0;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed == "{" || trimmed == "}")
                    continue;

                if (trimmed == "[" || trimmed == "]")
                    continue;

                if (trimmed.EndsWith("{"))
                {
                    var key = trimmed.Replace("{", "").Replace("\"", "").Replace(":", "");
                    result.AppendLine($"{new string(' ', indentLevel * 2)}{key}:");
                    indentLevel++;
                }
                else if (trimmed.EndsWith("},") || trimmed.EndsWith("}"))
                {
                    indentLevel = Math.Max(0, indentLevel - 1);
                }
                else
                {
                    var cleanLine = trimmed.Replace("\"", "").TrimEnd(',');
                    if (cleanLine.Contains(':'))
                    {
                        result.AppendLine($"{new string(' ', indentLevel * 2)}{cleanLine}");
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert basic YAML to JSON format
        /// </summary>
        private static string ConvertYamlToJson(string yaml)
        {
            // Simplified YAML to JSON conversion
            // This handles basic cases - for complex scenarios use a proper YAML library
            var lines = yaml.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var result = new StringBuilder();
            result.AppendLine("{");

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                if (trimmed.Contains(':'))
                {
                    var parts = trimmed.Split(':', 2);
                    var key = parts[0].Trim();
                    var value = parts.Length > 1 ? parts[1].Trim() : "";

                    if (string.IsNullOrEmpty(value))
                    {
                        // Object start
                        result.AppendLine($"  \"{key}\": {{");
                    }
                    else
                    {
                        // Simple value
                        var jsonValue = value;
                        if (!value.StartsWith("\"") && !bool.TryParse(value, out _) && !double.TryParse(value, out _))
                        {
                            jsonValue = $"\"{value}\"";
                        }
                        result.AppendLine($"  \"{key}\": {jsonValue},");
                    }
                }
            }

            result.AppendLine("}");
            return result.ToString().Replace(",\n}", "\n}");
        }

        /// <summary>
        /// Save object to file
        /// </summary>
        /// <typeparam name="T">The type of object to save</typeparam>
        /// <param name="obj">The object to save</param>
        /// <param name="filePath">The file path</param>
        /// <param name="format">The serialization format</param>
        /// <param name="options">Serialization options</param>
        public static async Task SaveToFileAsync<T>(T obj, string filePath, SerializationFormat? format = null, SerializationOptions? options = null)
        {
            format ??= GetFormatFromExtension(filePath);
            var serialized = Serialize(obj, format.Value, options);

            var encoding = options?.Encoding ?? Encoding.UTF8;
            await File.WriteAllTextAsync(filePath, serialized, encoding);
        }

        /// <summary>
        /// Load object from file
        /// </summary>
        /// <typeparam name="T">The type of object to load</typeparam>
        /// <param name="filePath">The file path</param>
        /// <param name="format">The serialization format</param>
        /// <param name="options">Serialization options</param>
        /// <returns>The loaded object</returns>
        public static async Task<T?> LoadFromFileAsync<T>(string filePath, SerializationFormat? format = null, SerializationOptions? options = null)
        {
            format ??= GetFormatFromExtension(filePath);

            var encoding = options?.Encoding ?? Encoding.UTF8;
            var content = await File.ReadAllTextAsync(filePath, encoding);

            return Deserialize<T>(content, format.Value, options);
        }

        /// <summary>
        /// Get serialization format from file extension
        /// </summary>
        private static SerializationFormat GetFormatFromExtension(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".json" => SerializationFormat.Json,
                ".xml" => SerializationFormat.Xml,
                ".csv" => SerializationFormat.Csv,
                ".yaml" or ".yml" => SerializationFormat.Yaml,
                _ => SerializationFormat.Json
            };
        }

        /// <summary>
        /// Create a deep clone of an object using serialization
        /// </summary>
        /// <typeparam name="T">The type of object to clone</typeparam>
        /// <param name="obj">The object to clone</param>
        /// <returns>A deep copy of the object</returns>
        public static T? DeepClone<T>(T obj)
        {
            var json = SerializeToJson(obj, new SerializationOptions());
            return DeserializeFromJson<T>(json, new SerializationOptions());
        }
    }

    /// <summary>
    /// Extension methods for easier serialization
    /// </summary>
    public static class SerializationExtensions
    {
        /// <summary>
        /// Convert object to JSON string
        /// </summary>
        public static string ToJson<T>(this T obj, bool indent = true)
        {
            return Serializer.Serialize(obj, SerializationFormat.Json, new SerializationOptions { Indent = indent });
        }

        /// <summary>
        /// Convert object to XML string
        /// </summary>
        public static string ToXml<T>(this T obj, bool indent = true)
        {
            return Serializer.Serialize(obj, SerializationFormat.Xml, new SerializationOptions { Indent = indent });
        }

        /// <summary>
        /// Convert collection to CSV string
        /// </summary>
        public static string ToCsv<T>(this T obj, bool includeHeaders = true, string delimiter = ",")
        {
            return Serializer.Serialize(obj, SerializationFormat.Csv, new SerializationOptions
            {
                CsvIncludeHeaders = includeHeaders,
                CsvDelimiter = delimiter
            });
        }

        /// <summary>
        /// Parse JSON string to object
        /// </summary>
        public static T? FromJson<T>(this string json)
        {
            return Serializer.Deserialize<T>(json, SerializationFormat.Json);
        }

        /// <summary>
        /// Parse XML string to object
        /// </summary>
        public static T? FromXml<T>(this string xml)
        {
            return Serializer.Deserialize<T>(xml, SerializationFormat.Xml);
        }
    }
}
