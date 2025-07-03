using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StdLib
{
    /// <summary>
    /// Configuration management utilities
    /// </summary>
    public static class Config
    {
        private static readonly Dictionary<string, object> _config = new();
        private static string? _configFilePath;

        /// <summary>
        /// Load configuration from JSON file
        /// </summary>
        public static void LoadFromFile(string filePath)
        {
            _configFilePath = filePath;
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                
                if (config != null)
                {
                    foreach (var kvp in config)
                    {
                        _config[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Save current configuration to file
        /// </summary>
        public static void SaveToFile(string? filePath = null)
        {
            var targetPath = filePath ?? _configFilePath;
            if (targetPath == null) throw new InvalidOperationException("No config file path specified");
            
            var json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(targetPath, json);
        }

        /// <summary>
        /// Get configuration value
        /// </summary>
        public static T? Get<T>(string key, T? defaultValue = default)
        {
            if (!_config.TryGetValue(key, out var value))
                return defaultValue;
            
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }
            
            return (T?)value;
        }

        /// <summary>
        /// Set configuration value
        /// </summary>
        public static void Set<T>(string key, T value)
        {
            _config[key] = value!;
        }

        /// <summary>
        /// Check if configuration key exists
        /// </summary>
        public static bool Has(string key)
        {
            return _config.ContainsKey(key);
        }

        /// <summary>
        /// Remove configuration key
        /// </summary>
        public static void Remove(string key)
        {
            _config.Remove(key);
        }

        /// <summary>
        /// Get all configuration keys
        /// </summary>
        public static IEnumerable<string> GetKeys()
        {
            return _config.Keys;
        }

        /// <summary>
        /// Clear all configuration
        /// </summary>
        public static void Clear()
        {
            _config.Clear();
        }

        /// <summary>
        /// Get environment variable with fallback
        /// </summary>
        public static string GetEnv(string key, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(key) ?? defaultValue;
        }

        /// <summary>
        /// Set environment variable
        /// </summary>
        public static void SetEnv(string key, string value)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    /// <summary>
    /// Typed configuration section
    /// </summary>
    public class ConfigSection<T> where T : class, new()
    {
        private readonly string _sectionName;
        private T _data;

        public ConfigSection(string sectionName)
        {
            _sectionName = sectionName;
            _data = Config.Get<T>(sectionName) ?? new T();
        }

        public T Data => _data;

        public void Save()
        {
            Config.Set(_sectionName, _data);
        }

        public void Reload()
        {
            _data = Config.Get<T>(_sectionName) ?? new T();
        }
    }
}
