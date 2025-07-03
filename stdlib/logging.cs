using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace StdLib
{
    /// <summary>
    /// Log levels enumeration
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }

    /// <summary>
    /// Simple logging utility
    /// </summary>
    public static class Logger
    {
        private static LogLevel _minLevel = LogLevel.Info;
        private static readonly List<ILogOutput> _outputs = new();
        private static readonly object _lock = new object();

        static Logger()
        {
            // Default to console output
            AddOutput(new ConsoleLogOutput());
        }

        /// <summary>
        /// Set minimum log level
        /// </summary>
        public static void SetLevel(LogLevel level)
        {
            _minLevel = level;
        }

        /// <summary>
        /// Add log output destination
        /// </summary>
        public static void AddOutput(ILogOutput output)
        {
            _outputs.Add(output);
        }

        /// <summary>
        /// Clear all outputs
        /// </summary>
        public static void ClearOutputs()
        {
            _outputs.Clear();
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public static void Debug(string message, params object[] args)
        {
            Log(LogLevel.Debug, message, args);
        }

        /// <summary>
        /// Log info message
        /// </summary>
        public static void Info(string message, params object[] args)
        {
            Log(LogLevel.Info, message, args);
        }

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void Warning(string message, params object[] args)
        {
            Log(LogLevel.Warning, message, args);
        }

        /// <summary>
        /// Log error message
        /// </summary>
        public static void Error(string message, params object[] args)
        {
            Log(LogLevel.Error, message, args);
        }

        /// <summary>
        /// Log fatal message
        /// </summary>
        public static void Fatal(string message, params object[] args)
        {
            Log(LogLevel.Fatal, message, args);
        }

        /// <summary>
        /// Log exception
        /// </summary>
        public static void Exception(Exception ex, string? message = null)
        {
            var logMessage = message != null ? $"{message}: {ex}" : ex.ToString();
            Log(LogLevel.Error, logMessage);
        }

        private static void Log(LogLevel level, string message, params object[] args)
        {
            if (level < _minLevel) return;

            lock (_lock)
            {
                var formattedMessage = args.Length > 0 ? string.Format(message, args) : message;
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = level,
                    Message = formattedMessage
                };

                foreach (var output in _outputs)
                {
                    output.Write(logEntry);
                }
            }
        }
    }

    /// <summary>
    /// Log entry structure
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Log output interface
    /// </summary>
    public interface ILogOutput
    {
        void Write(LogEntry entry);
    }

    /// <summary>
    /// Console log output
    /// </summary>
    public class ConsoleLogOutput : ILogOutput
    {
        public void Write(LogEntry entry)
        {
            var color = entry.Level switch
            {
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Fatal => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}");
            Console.ForegroundColor = originalColor;
        }
    }

    /// <summary>
    /// File log output
    /// </summary>
    public class FileLogOutput : ILogOutput
    {
        private readonly string _filePath;

        public FileLogOutput(string filePath)
        {
            _filePath = filePath;
        }

        public void Write(LogEntry entry)
        {
            var logLine = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] {entry.Message}";
            File.AppendAllText(_filePath, logLine + Environment.NewLine);
        }
    }
}
