using System.Text.Json.Serialization;

namespace uhigh.Net.LanguageServer.Protocol
{
    // Base types
    public class Position
    {
        [JsonPropertyName("line")]
        public int Line { get; set; }
        
        [JsonPropertyName("character")]
        public int Character { get; set; }
    }

    public class Range
    {
        [JsonPropertyName("start")]
        public Position Start { get; set; } = new();
        
        [JsonPropertyName("end")]
        public Position End { get; set; } = new();
    }

    public class Location
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
        
        [JsonPropertyName("range")]
        public Range Range { get; set; } = new();
    }

    public class TextDocumentIdentifier
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
    }

    public class VersionedTextDocumentIdentifier : TextDocumentIdentifier
    {
        [JsonPropertyName("version")]
        public int? Version { get; set; }
    }

    public class TextDocumentItem
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = "";
        
        [JsonPropertyName("languageId")]
        public string LanguageId { get; set; } = "";
        
        [JsonPropertyName("version")]
        public int Version { get; set; }
        
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }

    // Diagnostic types
    public enum DiagnosticSeverity
    {
        Error = 1,
        Warning = 2,
        Information = 3,
        Hint = 4
    }

    public class Diagnostic
    {
        [JsonPropertyName("range")]
        public Range Range { get; set; } = new();
        
        [JsonPropertyName("severity")]
        public DiagnosticSeverity? Severity { get; set; }
        
        [JsonPropertyName("code")]
        public string? Code { get; set; }
        
        [JsonPropertyName("source")]
        public string? Source { get; set; }
        
        [JsonPropertyName("message")]
        public string Message { get; set; } = "";
    }

    // Completion types
    public enum CompletionItemKind
    {
        Text = 1,
        Method = 2,
        Function = 3,
        Constructor = 4,
        Field = 5,
        Variable = 6,
        Class = 7,
        Interface = 8,
        Module = 9,
        Property = 10,
        Unit = 11,
        Value = 12,
        Enum = 13,
        Keyword = 14,
        Snippet = 15,
        Color = 16,
        File = 17,
        Reference = 18,
        Folder = 19,
        EnumMember = 20,
        Constant = 21,
        Struct = 22,
        Event = 23,
        Operator = 24,
        TypeParameter = 25
    }

    public class CompletionItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = "";
        
        [JsonPropertyName("kind")]
        public CompletionItemKind? Kind { get; set; }
        
        [JsonPropertyName("detail")]
        public string? Detail { get; set; }
        
        [JsonPropertyName("documentation")]
        public string? Documentation { get; set; }
        
        [JsonPropertyName("sortText")]
        public string? SortText { get; set; }
        
        [JsonPropertyName("filterText")]
        public string? FilterText { get; set; }
        
        [JsonPropertyName("insertText")]
        public string? InsertText { get; set; }
        
        [JsonPropertyName("commitCharacters")]
        public string[]? CommitCharacters { get; set; }
    }

    public class CompletionList
    {
        [JsonPropertyName("isIncomplete")]
        public bool IsIncomplete { get; set; }
        
        [JsonPropertyName("items")]
        public CompletionItem[] Items { get; set; } = Array.Empty<CompletionItem>();
    }

    // Hover types
    public class MarkupContent
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "markdown";
        
        [JsonPropertyName("value")]
        public string Value { get; set; } = "";
    }

    public class Hover
    {
        [JsonPropertyName("contents")]
        public MarkupContent Contents { get; set; } = new();
        
        [JsonPropertyName("range")]
        public Range? Range { get; set; }
    }

    // Symbol types
    public enum SymbolKind
    {
        File = 1,
        Module = 2,
        Namespace = 3,
        Package = 4,
        Class = 5,
        Method = 6,
        Property = 7,
        Field = 8,
        Constructor = 9,
        Enum = 10,
        Interface = 11,
        Function = 12,
        Variable = 13,
        Constant = 14,
        String = 15,
        Number = 16,
        Boolean = 17,
        Array = 18,
        Object = 19,
        Key = 20,
        Null = 21,
        EnumMember = 22,
        Struct = 23,
        Event = 24,
        Operator = 25,
        TypeParameter = 26
    }

    public class DocumentSymbol
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
        
        [JsonPropertyName("detail")]
        public string? Detail { get; set; }
        
        [JsonPropertyName("kind")]
        public SymbolKind Kind { get; set; }
        
        [JsonPropertyName("range")]
        public Range Range { get; set; } = new();
        
        [JsonPropertyName("selectionRange")]
        public Range SelectionRange { get; set; } = new();
        
        [JsonPropertyName("children")]
        public DocumentSymbol[]? Children { get; set; }
    }
}
