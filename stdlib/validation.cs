using System.Globalization;
using System.Text.RegularExpressions;

namespace StdLib
{
    /// <summary>
    /// Data validation utilities
    /// </summary>
    public static class Validator
    {
        /// <summary>
        /// Check if string is null or empty
        /// </summary>
        public static bool IsEmpty(string? value)
        {
            return string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Check if string is null, empty, or whitespace
        /// </summary>
        public static bool IsBlank(string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Check if string is a valid email address
        /// </summary>
        public static bool IsEmail(string email)
        {
            if (IsBlank(email)) return false;

            var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
            return Regex.IsMatch(email, pattern);
        }

        /// <summary>
        /// Check if string is a valid URL
        /// </summary>
        public static bool IsUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        /// <summary>
        /// Check if string contains only letters
        /// </summary>
        public static bool IsAlpha(string value)
        {
            return !IsBlank(value) && value.All(char.IsLetter);
        }

        /// <summary>
        /// Check if string contains only letters and numbers
        /// </summary>
        public static bool IsAlphanumeric(string value)
        {
            return !IsBlank(value) && value.All(char.IsLetterOrDigit);
        }

        /// <summary>
        /// Check if string contains only digits
        /// </summary>
        public static bool IsNumeric(string value)
        {
            return !IsBlank(value) && value.All(char.IsDigit);
        }

        /// <summary>
        /// Check if string is a valid integer
        /// </summary>
        public static bool IsInteger(string value)
        {
            return int.TryParse(value, out _);
        }

        /// <summary>
        /// Check if string is a valid decimal number
        /// </summary>
        public static bool IsDecimal(string value)
        {
            return decimal.TryParse(value, out _);
        }

        /// <summary>
        /// Check if value is within range (inclusive)
        /// </summary>
        public static bool InRange<T>(T value, T min, T max) where T : IComparable<T>
        {
            return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
        }

        /// <summary>
        /// Check if string length is within range
        /// </summary>
        public static bool LengthInRange(string value, int min, int max)
        {
            if (value == null) return false;
            return InRange(value.Length, min, max);
        }

        /// <summary>
        /// Check if string matches pattern
        /// </summary>
        public static bool MatchesPattern(string value, string pattern)
        {
            return !IsBlank(value) && Regex.IsMatch(value, pattern);
        }

        /// <summary>
        /// Check if string is a valid phone number (basic check)
        /// </summary>
        public static bool IsPhoneNumber(string phone)
        {
            if (IsBlank(phone)) return false;

            // Remove common formatting characters
            var cleaned = Regex.Replace(phone, @"[\s\-\(\)\+\.]", "");

            // Check if it's between 7 and 15 digits (international standard)
            return IsNumeric(cleaned) && InRange(cleaned.Length, 7, 15);
        }

        /// <summary>
        /// Check if string is a valid credit card number (Luhn algorithm)
        /// </summary>
        public static bool IsCreditCard(string cardNumber)
        {
            if (IsBlank(cardNumber)) return false;

            // Remove spaces and dashes
            var cleaned = cardNumber.Replace(" ", "").Replace("-", "");

            if (!IsNumeric(cleaned) || cleaned.Length < 13 || cleaned.Length > 19)
                return false;

            // Luhn algorithm
            int sum = 0;
            bool alternate = false;

            for (int i = cleaned.Length - 1; i >= 0; i--)
            {
                int digit = int.Parse(cleaned[i].ToString());

                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }

                sum += digit;
                alternate = !alternate;
            }

            return sum % 10 == 0;
        }

        /// <summary>
        /// Check if string is a valid IPv4 address
        /// </summary>
        public static bool IsIPv4(string ip)
        {
            if (IsBlank(ip)) return false;

            var parts = ip.Split('.');
            if (parts.Length != 4) return false;

            return parts.All(part => int.TryParse(part, out int num) && InRange(num, 0, 255));
        }

        /// <summary>
        /// Check if string is a valid IPv6 address
        /// </summary>
        public static bool IsIPv6(string ip)
        {
            return System.Net.IPAddress.TryParse(ip, out var address) &&
                   address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6;
        }

        /// <summary>
        /// Check if string is a valid MAC address
        /// </summary>
        public static bool IsMacAddress(string mac)
        {
            if (IsBlank(mac)) return false;

            var pattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";
            return Regex.IsMatch(mac, pattern);
        }

        /// <summary>
        /// Check if string is a valid hexadecimal color
        /// </summary>
        public static bool IsHexColor(string color)
        {
            if (IsBlank(color)) return false;

            var pattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            return Regex.IsMatch(color, pattern);
        }

        /// <summary>
        /// Check if date is valid
        /// </summary>
        public static bool IsValidDate(string date)
        {
            return DateTime.TryParse(date, out _);
        }

        /// <summary>
        /// Check if date is valid with specific format
        /// </summary>
        public static bool IsValidDate(string date, string format)
        {
            return DateTime.TryParseExact(date, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        /// <summary>
        /// Check if string contains only ASCII characters
        /// </summary>
        public static bool IsAscii(string value)
        {
            return !IsBlank(value) && value.All(c => c <= 127);
        }

        /// <summary>
        /// Check if string is a valid Base64 string
        /// </summary>
        public static bool IsStrongPassword(string password, int minLength = 8)
        {
            if (IsBlank(password) || password.Length < minLength) return false;

            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(c => "!@#$%^&*()_+-=[]{}|;:,.<>?".Contains(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        /// <summary>
        /// Check if string is a valid GUID
        /// </summary>
        public static bool IsGuid(string value)
        {
            return Guid.TryParse(value, out _);
        }

        /// <summary>
        /// Check if string is a valid JSON
        /// </summary>
        public static bool IsJson(string value)
        {
            if (IsBlank(value)) return false;

            try
            {
                System.Text.Json.JsonDocument.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if string is a valid XML
        /// </summary>
        public static bool IsXml(string value)
        {
            if (IsBlank(value)) return false;

            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if string is a valid Social Security Number (US format)
        /// </summary>
        public static bool IsSocialSecurityNumber(string ssn)
        {
            if (IsBlank(ssn)) return false;

            var pattern = @"^\d{3}-?\d{2}-?\d{4}$";
            return Regex.IsMatch(ssn, pattern);
        }

        /// <summary>
        /// Check if string is a valid license plate (basic format)
        /// </summary>
        public static bool IsLicensePlate(string plate)
        {
            if (IsBlank(plate)) return false;

            // Basic US license plate pattern - 3 letters followed by 3-4 numbers
            var pattern = @"^[A-Z]{3}[-\s]?\d{3,4}$";
            return Regex.IsMatch(plate.ToUpper(), pattern);
        }

        /// <summary>
        /// Check if string is a valid postal code (US ZIP code)
        /// </summary>
        public static bool IsPostalCode(string postalCode, string countryCode = "US")
        {
            if (IsBlank(postalCode)) return false;

            return countryCode.ToUpper() switch
            {
                "US" => Regex.IsMatch(postalCode, @"^\d{5}(-\d{4})?$"),
                "CA" => Regex.IsMatch(postalCode, @"^[A-Z]\d[A-Z]\s?\d[A-Z]\d$"),
                "UK" => Regex.IsMatch(postalCode, @"^[A-Z]{1,2}\d[A-Z\d]?\s?\d[A-Z]{2}$"),
                _ => false
            };
        }

        /// <summary>
        /// Check if age is within valid range
        /// </summary>
        public static bool IsValidAge(int age, int minAge = 0, int maxAge = 150)
        {
            return InRange(age, minAge, maxAge);
        }

        /// <summary>
        /// Check if string contains only whitespace characters
        /// </summary>
        public static bool IsWhitespace(string value)
        {
            return !IsEmpty(value) && value.All(char.IsWhiteSpace);
        }

        /// <summary>
        /// Check if string is a valid time format (HH:mm or HH:mm:ss)
        /// </summary>
        public static bool IsTime(string time)
        {
            if (IsBlank(time)) return false;

            return TimeSpan.TryParse(time, out _);
        }

        /// <summary>
        /// Check if string is a valid latitude coordinate
        /// </summary>
        public static bool IsLatitude(string latitude)
        {
            if (!IsDecimal(latitude)) return false;

            var lat = decimal.Parse(latitude);
            return InRange(lat, -90, 90);
        }

        /// <summary>
        /// Check if string is a valid longitude coordinate
        /// </summary>
        public static bool IsLongitude(string longitude)
        {
            if (!IsDecimal(longitude)) return false;

            var lng = decimal.Parse(longitude);
            return InRange(lng, -180, 180);
        }
    }

    /// <summary>
    /// String formatting utilities
    /// </summary>
    public static class Formatter
    {
        /// <summary>
        /// Format phone number with standard formatting
        /// </summary>
        public static string FormatPhoneNumber(string phone)
        {
            if (Validator.IsBlank(phone)) return "";

            var digits = Regex.Replace(phone, @"[^\d]", "");

            return digits.Length switch
            {
                7 => $"{digits.Substring(0, 3)}-{digits.Substring(3)}",
                10 => $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}",
                11 when digits.StartsWith("1") => $"+1 ({digits.Substring(1, 3)}) {digits.Substring(4, 3)}-{digits.Substring(7)}",
                _ => phone
            };
        }

        /// <summary>
        /// Format credit card number with spaces
        /// </summary>
        public static string FormatCreditCard(string cardNumber)
        {
            if (Validator.IsBlank(cardNumber)) return "";

            var digits = Regex.Replace(cardNumber, @"[^\d]", "");

            // Add spaces every 4 digits
            var formatted = "";
            for (int i = 0; i < digits.Length; i += 4)
            {
                if (i > 0) formatted += " ";
                formatted += digits.Substring(i, Math.Min(4, digits.Length - i));
            }

            return formatted;
        }

        /// <summary>
        /// Format currency with locale
        /// </summary>
        public static string FormatCurrency(decimal amount, string currencyCode = "USD")
        {
            var culture = currencyCode switch
            {
                "USD" => new CultureInfo("en-US"),
                "EUR" => new CultureInfo("de-DE"),
                "GBP" => new CultureInfo("en-GB"),
                "JPY" => new CultureInfo("ja-JP"),
                _ => CultureInfo.CurrentCulture
            };

            return amount.ToString("C", culture);
        }

        /// <summary>
        /// Format percentage
        /// </summary>
        public static string FormatPercentage(double value, int decimals = 2)
        {
            return value.ToString($"P{decimals}");
        }

        /// <summary>
        /// Format file size in human readable format
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            return FileUtils.FormatBytes(bytes);
        }

        /// <summary>
        /// Format time duration
        /// </summary>
        public static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration.Hours}h {duration.Minutes}m";
            if (duration.TotalHours >= 1)
                return $"{duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
            if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}m {duration.Seconds}s";

            return $"{duration.Seconds}s";
        }

        /// <summary>
        /// Format number with thousand separators
        /// </summary>
        public static string FormatNumber(long number)
        {
            return number.ToString("N0");
        }

        /// <summary>
        /// Format decimal number with specified decimal places
        /// </summary>
        public static string FormatNumber(double number, int decimals = 2)
        {
            return number.ToString($"F{decimals}");
        }

        /// <summary>
        /// Convert string to title case
        /// </summary>
        public static string ToTitleCase(string text)
        {
            if (Validator.IsBlank(text)) return "";

            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.ToLower());
        }

        /// <summary>
        /// Convert string to camelCase
        /// </summary>
        public static string ToCamelCase(string text)
        {
            if (Validator.IsBlank(text)) return "";

            var words = text.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return "";

            var result = words[0].ToLower();
            for (int i = 1; i < words.Length; i++)
            {
                result += char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }

            return result;
        }

        /// <summary>
        /// Convert string to snake_case
        /// </summary>
        public static string ToSnakeCase(string text)
        {
            if (Validator.IsBlank(text)) return "";

            return Regex.Replace(text, @"(?<!^)(?=[A-Z])", "_").ToLower()
                       .Replace(" ", "_")
                       .Replace("-", "_");
        }

        /// <summary>
        /// Convert string to kebab-case
        /// </summary>
        public static string ToKebabCase(string text)
        {
            return ToSnakeCase(text).Replace("_", "-");
        }

        /// <summary>
        /// Truncate string with ellipsis
        /// </summary>
        public static string Truncate(string text, int maxLength, string ellipsis = "...")
        {
            if (Validator.IsBlank(text) || text.Length <= maxLength) return text ?? "";

            return text.Substring(0, maxLength - ellipsis.Length) + ellipsis;
        }

        /// <summary>
        /// Mask sensitive data (e.g., credit card, SSN)
        /// </summary>
        public static string Mask(string text, char maskChar = '*', int visibleStart = 0, int visibleEnd = 4)
        {
            if (Validator.IsBlank(text)) return "";
            if (text.Length <= visibleStart + visibleEnd) return text;

            var start = text.Substring(0, visibleStart);
            var end = text.Substring(text.Length - visibleEnd);
            var mask = new string(maskChar, text.Length - visibleStart - visibleEnd);

            return start + mask + end;
        }
    }
}
