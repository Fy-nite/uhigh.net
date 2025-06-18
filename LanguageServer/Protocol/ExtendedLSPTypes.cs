namespace uhigh.Net.LanguageServer.Protocol
{
    // Additional LSP types not in the base protocol
    
    public enum InsertTextFormat
    {
        PlainText = 1,
        Snippet = 2
    }
    
    public enum MarkupKind
    {
        PlainText,
        Markdown
    }
    
    public class LocationLink
    {
        public Range? OriginSelectionRange { get; set; }
        public string TargetUri { get; set; } = "";
        public Range TargetRange { get; set; } = new();
        public Range TargetSelectionRange { get; set; } = new();
    }
    
    public class CodeAction
    {
        public string Title { get; set; } = "";
        public CodeActionKind? Kind { get; set; }
        public List<Diagnostic>? Diagnostics { get; set; }
        public bool? IsPreferred { get; set; }
        public WorkspaceEdit? Edit { get; set; }
        public Command? Command { get; set; }
    }
    
    public enum CodeActionKind
    {
        QuickFix,
        Refactor,
        RefactorExtract,
        RefactorInline,
        RefactorRewrite,
        Source,
        SourceOrganizeImports
    }
    
    public class WorkspaceEdit
    {
        public Dictionary<string, List<TextEdit>>? Changes { get; set; }
        public List<TextDocumentEdit>? DocumentChanges { get; set; }
    }
    
    public class TextEdit
    {
        public Range Range { get; set; } = new();
        public string NewText { get; set; } = "";
    }
    
    public class TextDocumentEdit
    {
        public VersionedTextDocumentIdentifier TextDocument { get; set; } = new();
        public List<TextEdit> Edits { get; set; } = new();
    }
    
    public class Command
    {
        public string Title { get; set; } = "";
        public string CommandId { get; set; } = "";
        public List<object>? Arguments { get; set; }
    }
    
    public class SignatureHelp
    {
        public List<SignatureInformation> Signatures { get; set; } = new();
        public int? ActiveSignature { get; set; }
        public int? ActiveParameter { get; set; }
    }
    
    public class SignatureInformation
    {
        public string Label { get; set; } = "";
        public MarkupContent? Documentation { get; set; }
        public List<ParameterInformation>? Parameters { get; set; }
    }
    
    public class ParameterInformation
    {
        public string Label { get; set; } = "";
        public MarkupContent? Documentation { get; set; }
    }
    
    public class FormattingOptions
    {
        public int TabSize { get; set; } = 4;
        public bool InsertSpaces { get; set; } = true;
        public bool TrimTrailingWhitespace { get; set; } = true;
        public bool InsertFinalNewline { get; set; } = false;
        public bool TrimFinalNewlines { get; set; } = true;
    }
}
