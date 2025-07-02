using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace uhigh.StdLib
{
    /// <summary>
    /// File system utilities and operations
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Read all text from a file with encoding detection
        /// </summary>
        public static string ReadText(string path)
        {
            return File.ReadAllText(path);
        }

        /// <summary>
        /// Write text to a file with UTF-8 encoding
        /// </summary>
        public static void WriteText(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        /// <summary>
        /// Append text to a file
        /// </summary>
        public static void AppendText(string path, string content)
        {
            File.AppendAllText(path, content, Encoding.UTF8);
        }

        /// <summary>
        /// Read all lines from a file
        /// </summary>
        public static List<string> ReadLines(string path)
        {
            return File.ReadAllLines(path).ToList();
        }

        /// <summary>
        /// Write lines to a file
        /// </summary>
        public static void WriteLines(string path, IEnumerable<string> lines)
        {
            File.WriteAllLines(path, lines, Encoding.UTF8);
        }

        /// <summary>
        /// Read file as bytes
        /// </summary>
        public static byte[] ReadBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// Write bytes to a file
        /// </summary>
        public static void WriteBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        /// <summary>
        /// Copy a file to another location
        /// </summary>
        public static void CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
        {
            File.Copy(sourcePath, destinationPath, overwrite);
        }

        /// <summary>
        /// Move/rename a file
        /// </summary>
        public static void MoveFile(string sourcePath, string destinationPath)
        {
            File.Move(sourcePath, destinationPath);
        }

        /// <summary>
        /// Delete a file if it exists
        /// </summary>
        public static bool DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if a file exists
        /// </summary>
        public static bool FileExists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Get file information
        /// </summary>
        public static FileInfo GetFileInfo(string path)
        {
            return new FileInfo(path);
        }

        /// <summary>
        /// Get file size in bytes
        /// </summary>
        public static long GetFileSize(string path)
        {
            return new FileInfo(path).Length;
        }

        /// <summary>
        /// Get file size in human-readable format
        /// </summary>
        public static string GetFileSizeFormatted(string path)
        {
            long bytes = GetFileSize(path);
            return FormatBytes(bytes);
        }

        /// <summary>
        /// Format bytes into human-readable string
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }

        /// <summary>
        /// Get file extension without the dot
        /// </summary>
        public static string GetExtension(string path)
        {
            return Path.GetExtension(path).TrimStart('.');
        }

        /// <summary>
        /// Get filename without extension
        /// </summary>
        public static string GetFileNameWithoutExtension(string path)
        {
            return Path.GetFileNameWithoutExtension(path);
        }

        /// <summary>
        /// Join path segments
        /// </summary>
        public static string JoinPath(params string[] paths)
        {
            return Path.Combine(paths);
        }

        /// <summary>
        /// Get absolute path
        /// </summary>
        public static string GetAbsolutePath(string path)
        {
            return Path.GetFullPath(path);
        }

        /// <summary>
        /// Get relative path from one path to another
        /// </summary>
        public static string GetRelativePath(string relativeTo, string path)
        {
            return Path.GetRelativePath(relativeTo, path);
        }

        /// <summary>
        /// Read JSON from file and deserialize to object
        /// </summary>
        public static T ReadJson<T>(string path)
        {
            var json = ReadText(path);
            return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Failed to deserialize JSON");
        }

        /// <summary>
        /// Serialize object to JSON and write to file
        /// </summary>
        public static void WriteJson<T>(string path, T obj, bool prettyPrint = true)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = prettyPrint,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            var json = JsonSerializer.Serialize(obj, options);
            WriteText(path, json);
        }

        /// <summary>
        /// Create a backup of a file
        /// </summary>
        public static string BackupFile(string path)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var directory = Path.GetDirectoryName(path) ?? "";
            var fileNameWithoutExt = GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            
            var backupPath = Path.Combine(directory, $"{fileNameWithoutExt}_{timestamp}{extension}");
            CopyFile(path, backupPath);
            
            return backupPath;
        }

        /// <summary>
        /// Find files matching a pattern
        /// </summary>
        public static List<string> FindFiles(string directory, string pattern = "*", bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(directory, pattern, searchOption).ToList();
        }

        /// <summary>
        /// Watch a file for changes
        /// </summary>
        public static FileSystemWatcher WatchFile(string path, Action<string> onChanged)
        {
            var directory = Path.GetDirectoryName(path) ?? "";
            var fileName = Path.GetFileName(path);
            
            var watcher = new FileSystemWatcher(directory, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            
            watcher.Changed += (sender, e) => onChanged(e.FullPath);
            
            return watcher;
        }
    }

    /// <summary>
    /// Directory utilities and operations
    /// </summary>
    public static class DirectoryUtils
    {
        /// <summary>
        /// Create directory if it doesn't exist
        /// </summary>
        public static void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <summary>
        /// Delete directory and all contents
        /// </summary>
        public static void DeleteDirectory(string path, bool recursive = true)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive);
            }
        }

        /// <summary>
        /// Check if directory exists
        /// </summary>
        public static bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Get all files in directory
        /// </summary>
        public static List<string> GetFiles(string path, string pattern = "*", bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetFiles(path, pattern, searchOption).ToList();
        }

        /// <summary>
        /// Get all subdirectories
        /// </summary>
        public static List<string> GetDirectories(string path, bool recursive = false)
        {
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            return Directory.GetDirectories(path, "*", searchOption).ToList();
        }

        /// <summary>
        /// Copy directory recursively
        /// </summary>
        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            CreateDirectory(destinationPath);
            
            // Copy files
            foreach (var file in GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destinationPath, fileName);
                FileUtils.CopyFile(file, destFile);
            }
            
            // Copy subdirectories
            foreach (var dir in GetDirectories(sourcePath))
            {
                var dirName = Path.GetFileName(dir);
                var destDir = Path.Combine(destinationPath, dirName);
                CopyDirectory(dir, destDir);
            }
        }

        /// <summary>
        /// Get directory size in bytes
        /// </summary>
        public static long GetDirectorySize(string path)
        {
            return GetFiles(path, "*", true).Sum(file => FileUtils.GetFileSize(file));
        }

        /// <summary>
        /// Get directory size in human-readable format
        /// </summary>
        public static string GetDirectorySizeFormatted(string path)
        {
            return FileUtils.FormatBytes(GetDirectorySize(path));
        }

        /// <summary>
        /// Get current working directory
        /// </summary>
        public static string GetCurrentDirectory()
        {
            return Directory.GetCurrentDirectory();
        }

        /// <summary>
        /// Set current working directory
        /// </summary>
        public static void SetCurrentDirectory(string path)
        {
            Directory.SetCurrentDirectory(path);
        }

        /// <summary>
        /// Get temporary directory path
        /// </summary>
        public static string GetTempDirectory()
        {
            return Path.GetTempPath();
        }

        /// <summary>
        /// Create a temporary directory
        /// </summary>
        public static string CreateTempDirectory()
        {
            var tempPath = Path.Combine(GetTempDirectory(), Path.GetRandomFileName());
            CreateDirectory(tempPath);
            return tempPath;
        }

        /// <summary>
        /// Clean directory (remove all contents but keep the directory)
        /// </summary>
        public static void CleanDirectory(string path)
        {
            if (!DirectoryExists(path)) return;
            
            foreach (var file in GetFiles(path))
            {
                FileUtils.DeleteFile(file);
            }
            
            foreach (var dir in GetDirectories(path))
            {
                DeleteDirectory(dir);
            }
        }
    }

    /// <summary>
    /// Path utilities
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Normalize path separators for current OS
        /// </summary>
        public static string NormalizePath(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Check if path is absolute
        /// </summary>
        public static bool IsAbsolutePath(string path)
        {
            return Path.IsPathRooted(path);
        }

        /// <summary>
        /// Get user home directory
        /// </summary>
        public static string GetHomeDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        /// <summary>
        /// Get desktop directory
        /// </summary>
        public static string GetDesktopDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        /// <summary>
        /// Get documents directory
        /// </summary>
        public static string GetDocumentsDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Get downloads directory (approximation)
        /// </summary>
        public static string GetDownloadsDirectory()
        {
            return Path.Combine(GetHomeDirectory(), "Downloads");
        }

        /// <summary>
        /// Expand environment variables in path
        /// </summary>
        public static string ExpandPath(string path)
        {
            return Environment.ExpandEnvironmentVariables(path);
        }

        /// <summary>
        /// Generate a safe filename by removing invalid characters
        /// </summary>
        public static string MakeSafeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Generate a unique filename if file already exists
        /// </summary>
        public static string GetUniqueFileName(string path)
        {
            if (!FileUtils.FileExists(path)) return path;
            
            var directory = Path.GetDirectoryName(path) ?? "";
            var fileName = FileUtils.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            
            int counter = 1;
            string newPath;
            
            do
            {
                newPath = Path.Combine(directory, $"{fileName} ({counter}){extension}");
                counter++;
            } while (FileUtils.FileExists(newPath));
            
            return newPath;
        }
    }
}
