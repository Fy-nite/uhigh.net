using System.Text.Json.Serialization;

namespace uhigh.Net.LanguageServer.Protocol
{
    // JSON-RPC base types
    public class JsonRpcMessage
    {
        [JsonPropertyName("jsonrpc")]
        [JsonPropertyOrder(0)]
        public string JsonRpc { get; set; } = "2.0";
    }

    public class JsonRpcRequest : JsonRpcMessage
    {
        [JsonPropertyName("id")]
        [JsonPropertyOrder(1)]
        public object? Id { get; set; }
        
        [JsonPropertyName("method")]
        [JsonPropertyOrder(2)]
        public string Method { get; set; } = "";
        
        [JsonPropertyName("params")]
        [JsonPropertyOrder(3)]
        public object? Params { get; set; }
    }

    public class JsonRpcResponse : JsonRpcMessage
    {
        [JsonPropertyName("id")]
        [JsonPropertyOrder(1)]
        public object? Id { get; set; }
        
        [JsonPropertyName("result")]
        [JsonPropertyOrder(2)]
        public object? Result { get; set; }
        
        [JsonPropertyName("error")]
        [JsonPropertyOrder(3)]
        public JsonRpcError? Error { get; set; }
    }

    public class JsonRpcNotification : JsonRpcMessage
    {
        [JsonPropertyName("method")]
        [JsonPropertyOrder(1)]
        public string Method { get; set; } = "";
        
        [JsonPropertyName("params")]
        [JsonPropertyOrder(2)]
        public object? Params { get; set; }
    }

    public class JsonRpcError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
        
        [JsonPropertyName("data")]
        public object? Data { get; set; }
    }

    // LSP specific message types
    public class InitializeParams
    {
        [JsonPropertyName("processId")]
        public int? ProcessId { get; set; }
        
        [JsonPropertyName("rootPath")]
        public string? RootPath { get; set; }
        
        [JsonPropertyName("rootUri")]
        public string? RootUri { get; set; }
        
        [JsonPropertyName("capabilities")]
        public ClientCapabilities Capabilities { get; set; } = new();
        
        [JsonPropertyName("workspaceFolders")]
        public WorkspaceFolder[]? WorkspaceFolders { get; set; }
    }

    public class ClientCapabilities
    {
        [JsonPropertyName("workspace")]
        public WorkspaceCapabilities? Workspace { get; set; }
        
        [JsonPropertyName("textDocument")]
        public TextDocumentCapabilities? TextDocument { get; set; }
    }

    public class WorkspaceCapabilities
    {
        [JsonPropertyName("workspaceFolders")]
        public bool? WorkspaceFolders { get; set; }
        
        [JsonPropertyName("configuration")]
        public bool? Configuration { get; set; }
    }

    public class TextDocumentCapabilities
    {
        [JsonPropertyName("synchronization")]
        public SynchronizationCapabilities? Synchronization { get; set; }
        
        [JsonPropertyName("completion")]
        public CompletionCapabilities? Completion { get; set; }
        
        [JsonPropertyName("hover")]
        public HoverCapabilities? Hover { get; set; }
        
        [JsonPropertyName("documentSymbol")]
        public DocumentSymbolCapabilities? DocumentSymbol { get; set; }
    }

    public class SynchronizationCapabilities
    {
        [JsonPropertyName("dynamicRegistration")]
        public bool? DynamicRegistration { get; set; }
        
        [JsonPropertyName("willSave")]
        public bool? WillSave { get; set; }
        
        [JsonPropertyName("willSaveWaitUntil")]
        public bool? WillSaveWaitUntil { get; set; }
        
        [JsonPropertyName("didSave")]
        public bool? DidSave { get; set; }
    }

    public class CompletionCapabilities
    {
        [JsonPropertyName("dynamicRegistration")]
        public bool? DynamicRegistration { get; set; }
        
        [JsonPropertyName("completionItem")]
        public CompletionItemCapabilities? CompletionItem { get; set; }
    }

    public class CompletionItemCapabilities
    {
        [JsonPropertyName("snippetSupport")]
        public bool? SnippetSupport { get; set; }
    }

    public class HoverCapabilities
    {
        [JsonPropertyName("dynamicRegistration")]
        public bool? DynamicRegistration { get; set; }
        
        [JsonPropertyName("contentFormat")]
        public string[]? ContentFormat { get; set; }
    }

    public class DocumentSymbolCapabilities
    {
        [JsonPropertyName("dynamicRegistration")]
        public bool? DynamicRegistration { get; set; }
        
        [JsonPropertyName("hierarchicalDocumentSymbolSupport")]
        public bool? HierarchicalDocumentSymbolSupport { get; set; }
    }

    public class WorkspaceFolder
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    public class InitializeResult
    {
        [JsonPropertyName("capabilities")]
        public ServerCapabilities Capabilities { get; set; } = new();
        
        [JsonPropertyName("serverInfo")]
        public ServerInfo? ServerInfo { get; set; }
    }

    public class ServerCapabilities
    {
        [JsonPropertyName("textDocumentSync")]
        public TextDocumentSyncKind? TextDocumentSync { get; set; }
        
        [JsonPropertyName("completionProvider")]
        public CompletionOptions? CompletionProvider { get; set; }
        
        [JsonPropertyName("hoverProvider")]
        public bool? HoverProvider { get; set; }
        
        [JsonPropertyName("documentSymbolProvider")]
        public bool? DocumentSymbolProvider { get; set; }
        
        [JsonPropertyName("definitionProvider")]
        public bool? DefinitionProvider { get; set; }
        
        [JsonPropertyName("referencesProvider")]
        public bool? ReferencesProvider { get; set; }
        
        [JsonPropertyName("documentFormattingProvider")]
        public bool? DocumentFormattingProvider { get; set; }
    }

    public class CompletionOptions
    {
        [JsonPropertyName("resolveProvider")]
        public bool? ResolveProvider { get; set; }
        
        [JsonPropertyName("triggerCharacters")]
        public string[]? TriggerCharacters { get; set; }
    }

    public class ServerInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("version")]
        public string? Version { get; set; }
    }

    public enum TextDocumentSyncKind
    {
        None = 0,
        Full = 1,
        Incremental = 2
    }

    // Document synchronization params
    public class DidOpenTextDocumentParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentItem TextDocument { get; set; } = new();
    }

    public class DidChangeTextDocumentParams
    {
        [JsonPropertyName("textDocument")]
        public VersionedTextDocumentIdentifier TextDocument { get; set; } = new();
        
        [JsonPropertyName("contentChanges")]
        public TextDocumentContentChangeEvent[] ContentChanges { get; set; } = Array.Empty<TextDocumentContentChangeEvent>();
    }

    public class TextDocumentContentChangeEvent
    {
        [JsonPropertyName("range")]
        public Range? Range { get; set; }
        
        [JsonPropertyName("rangeLength")]
        public int? RangeLength { get; set; }
        
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    public class DidCloseTextDocumentParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; } = new();
    }

    public class TextDocumentPositionParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; } = new();
        
        [JsonPropertyName("position")]
        public Position Position { get; set; } = new();
    }

    public class CompletionParams : TextDocumentPositionParams
    {
        [JsonPropertyName("context")]
        public CompletionContext? Context { get; set; }
    }

    public class CompletionContext
    {
        [JsonPropertyName("triggerKind")]
        public CompletionTriggerKind TriggerKind { get; set; }
        
        [JsonPropertyName("triggerCharacter")]
        public string? TriggerCharacter { get; set; }
    }

    public enum CompletionTriggerKind
    {
        Invoked = 1,
        TriggerCharacter = 2,
        TriggerForIncompleteCompletions = 3
    }

    public class HoverParams : TextDocumentPositionParams { }

    public class DocumentSymbolParams
    {
        [JsonPropertyName("textDocument")]
        public TextDocumentIdentifier TextDocument { get; set; } = new();
    }

    public class PublishDiagnosticsParams
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
        
        [JsonPropertyName("diagnostics")]
        public Diagnostic[] Diagnostics { get; set; } = Array.Empty<Diagnostic>();
    }
}
