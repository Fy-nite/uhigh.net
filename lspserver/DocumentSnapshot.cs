using System;
using System.Diagnostics.CodeAnalysis;

namespace UhighLanguageServer;

/// <summary>
/// Represents an immutable snapshot of a document tracked by the language server.
/// </summary>
public sealed class DocumentSnapshot
{
    private readonly Lazy<string[]> _lines;

    public DocumentSnapshot(Uri uri, string languageId, int version, string text)
    {
        Uri = uri;
        LanguageId = languageId;
        Version = version;
        Text = text;
        _lines = new Lazy<string[]>(() => Text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n'));
    }

    public Uri Uri { get; }

    public string LanguageId { get; }

    public int Version { get; }

    public string Text { get; }

    public string[] GetLines() => _lines.Value;

    public DocumentSnapshot With(int version, string text) =>
        new DocumentSnapshot(Uri, LanguageId, version, text);

    public override string ToString() => $"{Uri} (v{Version})";

    public bool TryGetLine(int line, [NotNullWhen(true)] out string? value)
    {
        var lines = _lines.Value;
        if (line < 0 || line >= lines.Length)
        {
            value = null;
            return false;
        }

        value = lines[line];
        return true;
    }
}
