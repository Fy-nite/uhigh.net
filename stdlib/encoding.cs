using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace StdLib
{
    /// <summary>
    /// Encoding and decoding utilities
    /// </summary>
    public static class EncodingUtils
    {
        /// <summary>
        /// Encode string to Base64
        /// </summary>
        public static string ToBase64(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Decode Base64 string
        /// </summary>
        public static string FromBase64(string base64)
        {
            var bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// URL encode string
        /// </summary>
        public static string UrlEncode(string input)
        {
            return HttpUtility.UrlEncode(input);
        }

        /// <summary>
        /// URL decode string
        /// </summary>
        public static string UrlDecode(string input)
        {
            return HttpUtility.UrlDecode(input);
        }

        /// <summary>
        /// HTML encode string
        /// </summary>
        public static string HtmlEncode(string input)
        {
            return HttpUtility.HtmlEncode(input);
        }

        /// <summary>
        /// HTML decode string
        /// </summary>
        public static string HtmlDecode(string input)
        {
            return HttpUtility.HtmlDecode(input);
        }

        /// <summary>
        /// Convert string to MD5 hash
        /// </summary>
        public static string ToMd5(string input)
        {
            using var md5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = md5.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>
        /// Convert string to SHA256 hash
        /// </summary>
        public static string ToSha256(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLower();
        }

        /// <summary>
        /// Convert bytes to hex string
        /// </summary>
        public static string ToHex(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToLower();
        }

        /// <summary>
        /// Convert hex string to bytes
        /// </summary>
        public static byte[] FromHex(string hex)
        {
            return Convert.FromHexString(hex);
        }

        /// <summary>
        /// Escape special characters for regex
        /// </summary>
        public static string EscapeRegex(string input)
        {
            return Regex.Escape(input);
        }

        /// <summary>
        /// Escape special characters for JSON
        /// </summary>
        public static string EscapeJson(string input)
        {
            return input.Replace("\\", "\\\\")
                       .Replace("\"", "\\\"")
                       .Replace("\n", "\\n")
                       .Replace("\r", "\\r")
                       .Replace("\t", "\\t");
        }

        /// <summary>
        /// Convert string to slug (URL-friendly)
        /// </summary>
        public static string ToSlug(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            // Convert to lowercase and replace spaces with hyphens
            input = input.ToLower().Replace(" ", "-");

            // Remove special characters
            input = Regex.Replace(input, @"[^a-z0-9\-]", "");

            // Remove consecutive hyphens
            input = Regex.Replace(input, @"-+", "-");

            // Trim hyphens from start and end
            return input.Trim('-');
        }
    }
}
