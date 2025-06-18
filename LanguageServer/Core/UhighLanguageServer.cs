using System.Text.Json;
using System.Text.Json.Serialization;
using uhigh.Net.LanguageServer.Protocol;
using uhigh.Net.LanguageServer.Core;
using uhigh.Net.LanguageServer.Services;

namespace uhigh.Net.LanguageServer.Core
{
    public class UhighLanguageServer
    {
        private readonly DocumentManager _documentManager;
        private readonly LanguageService _languageService;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _initialized = false;
        private StreamWriter? _outputWriter;
        
        public UhighLanguageServer()
        {
            _documentManager = new DocumentManager();
            _languageService = new LanguageService(_documentManager);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            // Subscribe to document events
            _documentManager.DocumentOpened += OnDocumentOpened;
            _documentManager.DocumentChanged += OnDocumentChanged;
            _documentManager.DocumentClosed += OnDocumentClosed;
        }
        
        public void SetOutputStream(StreamWriter writer)
        {
            _outputWriter = writer;
        }
        
        public async Task<string> ProcessRequest(string requestJson)
        {
            try
            {
                // Try to parse as a request first
                using var doc = JsonDocument.Parse(requestJson);
                var root = doc.RootElement;
                
                if (root.TryGetProperty("id", out _))
                {
                    // This is a request that expects a response
                    var request = JsonSerializer.Deserialize<JsonRpcRequest>(requestJson, _jsonOptions);
                    if (request == null)
                    {
                        var errorResponse = CreateErrorResponse(null, -32700, "Parse error");
                        return JsonSerializer.Serialize(errorResponse, _jsonOptions);
                    }
                    
                    var response = await HandleRequest(request);
                    return JsonSerializer.Serialize(response, _jsonOptions);
                }
                else
                {
                    // This is a notification - process but don't return anything
                    var notification = JsonSerializer.Deserialize<JsonRpcNotification>(requestJson, _jsonOptions);
                    if (notification != null)
                    {
                        await HandleNotificationAsync(notification);
                    }
                    return string.Empty; // No response for notifications
                }
            }
            catch (JsonException ex)
            {
                var errorResponse = CreateErrorResponse(null, -32700, "Parse error");
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }
            catch (Exception ex)
            {
                var errorResponse = CreateErrorResponse(null, -32603, $"Internal error: {ex.Message}");
                return JsonSerializer.Serialize(errorResponse, _jsonOptions);
            }
        }
        
        private async Task<JsonRpcResponse> HandleRequest(JsonRpcRequest request)
        {
            return request.Method switch
            {
                "initialize" => await HandleInitialize(request),
                "textDocument/completion" => await HandleCompletion(request),
                "textDocument/hover" => await HandleHover(request),
                "textDocument/documentSymbol" => await HandleDocumentSymbol(request),
                "shutdown" => HandleShutdown(request),
                _ => CreateErrorResponse(request.Id, -32601, $"Method not found: {request.Method}")
            };
        }

        private async Task HandleNotificationAsync(JsonRpcNotification notification)
        {
            switch (notification.Method)
            {
                case "initialized":
                    await HandleInitializedAsync();
                    break;
                case "textDocument/didOpen":
                    await HandleDidOpenAsync(notification);
                    break;
                case "textDocument/didChange":
                    await HandleDidChangeAsync(notification);
                    break;
                case "textDocument/didClose":
                    await HandleDidCloseAsync(notification);
                    break;
                case "exit":
                    await HandleExitAsync();
                    break;
            }
        }
        
        private async Task<JsonRpcResponse> HandleInitialize(JsonRpcRequest request)
        {
            var result = new InitializeResult
            {
                Capabilities = new ServerCapabilities
                {
                    TextDocumentSync = TextDocumentSyncKind.Full,
                    CompletionProvider = new CompletionOptions
                    {
                        ResolveProvider = false,
                        TriggerCharacters = new[] { ".", ":" }
                    },
                    HoverProvider = true,
                    DocumentSymbolProvider = true
                },
                ServerInfo = new ServerInfo
                {
                    Name = "Uhigh Language Server",
                    Version = "1.0.0"
                }
            };
            
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = result
            };
        }
        
        private async Task HandleInitializedAsync()
        {
            _initialized = true;
        }
        
        private async Task<JsonRpcResponse> HandleCompletion(JsonRpcRequest request)
        {
            if (!_initialized)
                return CreateErrorResponse(request.Id, -32002, "Server not initialized");
            
            try
            {
                var completionParams = JsonSerializer.Deserialize<CompletionParams>(
                    JsonSerializer.Serialize(request.Params, _jsonOptions), _jsonOptions);
                
                if (completionParams == null)
                    return CreateErrorResponse(request.Id, -32602, "Invalid params");
                
                var result = _languageService.GetCompletions(
                    completionParams.TextDocument.Uri, 
                    completionParams.Position);
                
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.Id, -32603, $"Internal error: {ex.Message}");
            }
        }
        
        private async Task<JsonRpcResponse> HandleHover(JsonRpcRequest request)
        {
            if (!_initialized)
                return CreateErrorResponse(request.Id, -32002, "Server not initialized");
            
            try
            {
                var hoverParams = JsonSerializer.Deserialize<HoverParams>(
                    JsonSerializer.Serialize(request.Params, _jsonOptions), _jsonOptions);
                
                if (hoverParams == null)
                    return CreateErrorResponse(request.Id, -32602, "Invalid params");
                
                var result = _languageService.GetHover(
                    hoverParams.TextDocument.Uri, 
                    hoverParams.Position);
                
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.Id, -32603, $"Internal error: {ex.Message}");
            }
        }
        
        private async Task<JsonRpcResponse> HandleDocumentSymbol(JsonRpcRequest request)
        {
            if (!_initialized)
                return CreateErrorResponse(request.Id, -32002, "Server not initialized");
            
            try
            {
                var symbolParams = JsonSerializer.Deserialize<DocumentSymbolParams>(
                    JsonSerializer.Serialize(request.Params, _jsonOptions), _jsonOptions);
                
                if (symbolParams == null)
                    return CreateErrorResponse(request.Id, -32602, "Invalid params");
                
                var result = _languageService.GetDocumentSymbols(symbolParams.TextDocument.Uri);
                
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.Id, -32603, $"Internal error: {ex.Message}");
            }
        }
        
        private JsonRpcResponse HandleShutdown(JsonRpcRequest request)
        {
            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = null
            };
        }
        
        private async Task HandleDidOpenAsync(JsonRpcNotification notification)
        {
            try
            {
                var didOpenParams = JsonSerializer.Deserialize<DidOpenTextDocumentParams>(
                    JsonSerializer.Serialize(notification.Params, _jsonOptions), _jsonOptions);
                
                if (didOpenParams != null)
                {
                    _documentManager.OpenDocument(didOpenParams.TextDocument);
                }
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }
        
        private async Task HandleDidChangeAsync(JsonRpcNotification notification)
        {
            try
            {
                var didChangeParams = JsonSerializer.Deserialize<DidChangeTextDocumentParams>(
                    JsonSerializer.Serialize(notification.Params, _jsonOptions), _jsonOptions);
                
                if (didChangeParams != null)
                {
                    _documentManager.ChangeDocument(didChangeParams);
                }
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }
        
        private async Task HandleDidCloseAsync(JsonRpcNotification notification)
        {
            try
            {
                var didCloseParams = JsonSerializer.Deserialize<DidCloseTextDocumentParams>(
                    JsonSerializer.Serialize(notification.Params, _jsonOptions), _jsonOptions);
                
                if (didCloseParams != null)
                {
                    _documentManager.CloseDocument(didCloseParams);
                }
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }
        
        private async Task HandleExitAsync()
        {
            Environment.Exit(0);
        }
        
        private void OnDocumentOpened(TextDocument document)
        {
            PublishDiagnostics(document.Uri);
        }
        
        private void OnDocumentChanged(TextDocument document)
        {
            PublishDiagnostics(document.Uri);
        }
        
        private void OnDocumentClosed(string uri)
        {
            // Clear diagnostics for closed document
            PublishDiagnostics(uri, Array.Empty<Protocol.Diagnostic>());
        }
        
        private async void PublishDiagnostics(string uri, Protocol.Diagnostic[]? diagnostics = null)
        {
            if (_outputWriter == null) return;
            
            try
            {
                diagnostics ??= _languageService.GetDiagnostics(uri);
                
                var publishParams = new PublishDiagnosticsParams
                {
                    Uri = uri,
                    Diagnostics = diagnostics
                };
                
                var notification = new JsonRpcNotification
                {
                    Method = "textDocument/publishDiagnostics",
                    Params = publishParams
                };
                
                var notificationJson = JsonSerializer.Serialize(notification, _jsonOptions);
                await SendNotification(_outputWriter, notificationJson);
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }
        
        private async Task SendNotification(StreamWriter writer, string notificationJson)
        {
            try
            {
                var contentBytes = System.Text.Encoding.UTF8.GetBytes(notificationJson);
                var header = $"Content-Length: {contentBytes.Length}\r\n\r\n";
                
                await writer.WriteAsync(header);
                await writer.WriteAsync(notificationJson);
                await writer.FlushAsync();
            }
            catch (Exception ex)
            {
                // Silent error handling
            }
        }
        
        private JsonRpcResponse CreateErrorResponse(object? id, int code, string message)
        {
            return new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message
                }
            };
        }
    }
}