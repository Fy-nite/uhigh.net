using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StdLib
{
    /// <summary>
    /// HTTP client utilities for web requests
    /// </summary>
    public static class HttpUtils
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Perform a GET request and return the response as string
        /// </summary>
        public static async Task<string> GetAsync(string url, Dictionary<string, string>? headers = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeaders(request, headers);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Perform a POST request with JSON data
        /// </summary>
        public static async Task<string> PostJsonAsync<T>(string url, T data, Dictionary<string, string>? headers = null)
        {
            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            AddHeaders(request, headers);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Perform a POST request with form data
        /// </summary>
        public static async Task<string> PostFormAsync(string url, Dictionary<string, string> formData, Dictionary<string, string>? headers = null)
        {
            var content = new FormUrlEncodedContent(formData);
            
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            AddHeaders(request, headers);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Download a file from URL
        /// </summary>
        public static async Task DownloadFileAsync(string url, string filePath, Dictionary<string, string>? headers = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            AddHeaders(request, headers);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            using var fileStream = new FileStream(filePath, FileMode.Create);
            await response.Content.CopyToAsync(fileStream);
        }

        /// <summary>
        /// Upload a file via POST
        /// </summary>
        public static async Task<string> UploadFileAsync(string url, string filePath, string fieldName = "file", Dictionary<string, string>? headers = null)
        {
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(await File.ReadAllBytesAsync(filePath));
            
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, fieldName, Path.GetFileName(filePath));
            
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
            AddHeaders(request, headers);
            
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            
            return await response.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Check if URL is reachable
        /// </summary>
        public static async Task<bool> IsUrlReachableAsync(string url, int timeoutSeconds = 10)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };
                var response = await client.GetAsync(url);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static void AddHeaders(HttpRequestMessage request, Dictionary<string, string>? headers)
        {
            if (headers == null) return;
            
            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// API client builder for easier HTTP operations
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly Dictionary<string, string> _defaultHeaders;

        public ApiClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _httpClient = new HttpClient();
            _defaultHeaders = new Dictionary<string, string>();
        }

        public ApiClient AddHeader(string key, string value)
        {
            _defaultHeaders[key] = value;
            return this;
        }

        public ApiClient SetBearerToken(string token)
        {
            _defaultHeaders["Authorization"] = $"Bearer {token}";
            return this;
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var response = await HttpUtils.GetAsync($"{_baseUrl}/{endpoint.TrimStart('/')}", _defaultHeaders);
            return JsonSerializer.Deserialize<T>(response)!;
        }

        public async Task<T> PostAsync<T, TRequest>(string endpoint, TRequest data)
        {
            var response = await HttpUtils.PostJsonAsync($"{_baseUrl}/{endpoint.TrimStart('/')}", data, _defaultHeaders);
            return JsonSerializer.Deserialize<T>(response)!;
        }
    }
}
