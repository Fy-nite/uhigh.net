// using LanguageServer.Parameters.TextDocument;

// namespace UhighLanguageServer;

// /// <summary>
// /// Tracks open documents and applies incremental updates from LSP notifications.
// /// </summary>
// public sealed class DocumentStore
// {
//     private readonly Dictionary<Uri, DocumentSnapshot> _documents = new();
//     private readonly object _gate = new();

//     public IReadOnlyCollection<DocumentSnapshot> Documents
//     {
//         get
//         {
//             lock (_gate)
//             {
//                 return _documents.Values.ToArray();
//             }
//         }
//     }

//     public DocumentSnapshot Open(TextDocumentItem document)
//     {
//         var snapshot = new DocumentSnapshot(
//             document.uri,
//             document.languageId ?? "uhigh",
//             Convert.ToInt32(document.version),
//             document.text ?? string.Empty);

//         lock (_gate)
//         {
//             _documents[document.uri] = snapshot;
//         }

//         return snapshot;
//     }

//     public DocumentSnapshot? ApplyChanges(DidChangeTextDocumentParams @params)
//     {
//         if (@params == null) return null;

//         lock (_gate)
//         {
//             if (!_documents.TryGetValue(@params.textDocument.uri, out var current))
//             {
//                 return null;
//             }

//             var updatedText = current.Text;
//             foreach (var change in @params.contentChanges)
//             {
//                 updatedText = ApplyChange(updatedText, change);
//             }

//             var version = @params.textDocument.version.HasValue
//                 ? Convert.ToInt32(@params.textDocument.version.Value)
//                 : current.Version + 1;

//             var snapshot = current.With(version, updatedText);
//             _documents[current.Uri] = snapshot;
//             return snapshot;
//         }
//     }

//     public DocumentSnapshot? Close(Uri uri)
//     {
//         lock (_gate)
//         {
//             if (_documents.TryGetValue(uri, out var snapshot))
//             {
//                 _documents.Remove(uri);
//                 return snapshot;
//             }
//         }

//         return null;
//     }

//     public DocumentSnapshot? Get(Uri uri)
//     {
//         lock (_gate)
//         {
//             return _documents.TryGetValue(uri, out var snapshot) ? snapshot : null;
//         }
//     }

//     private static string ApplyChange(string text, TextDocumentContentChangeEvent change)
//     {
//         if (change.range == null)
//         {
//             return change.text ?? string.Empty;
//         }

//         var startOffset = GetOffset(text, (int)change.range.start.line, (int)change.range.start.character);
//         var endOffset = GetOffset(text, (int)change.range.end.line, (int)change.range.end.character);
//         if (startOffset > endOffset)
//         {
//             (startOffset, endOffset) = (endOffset, startOffset);
//         }

//         var builder = new System.Text.StringBuilder(text.Length + Math.Max(0, (change.text?.Length ?? 0) - (endOffset - startOffset)));
//         builder.Append(text, 0, startOffset);
//         if (!string.IsNullOrEmpty(change.text))
//         {
//             builder.Append(change.text);
//         }
//         builder.Append(text, endOffset, text.Length - endOffset);
//         return builder.ToString();
//     }

//     private static int GetOffset(string text, int line, int character)
//     {
//         line = Math.Max(0, line);
//         character = Math.Max(0, character);

//         var currentLine = 0;
//         var index = 0;

//         while (currentLine < line && index < text.Length)
//         {
//             var nextLineBreak = text.IndexOfAny(['\r', '\n'], index);
//             if (nextLineBreak < 0)
//             {
//                 return text.Length;
//             }

//             if (text[nextLineBreak] == '\r' && nextLineBreak + 1 < text.Length && text[nextLineBreak + 1] == '\n')
//             {
//                 index = nextLineBreak + 2;
//             }
//             else
//             {
//                 index = nextLineBreak + 1;
//             }

//             currentLine++;
//         }

//         if (currentLine < line)
//         {
//             return text.Length;
//         }

//         var lineEnd = text.IndexOfAny(['\r', '\n'], index);
//         if (lineEnd < 0)
//         {
//             lineEnd = text.Length;
//         }

//         return Math.Min(index + character, lineEnd);
//     }
// }
