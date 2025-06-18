using System.Collections.Concurrent;
using uhigh.Net.LanguageServer.Protocol;

namespace uhigh.Net.LanguageServer.Core
{
    public class TextDocument
    {
        public string Uri { get; set; } = "";
        public string LanguageId { get; set; } = "";
        public int Version { get; set; }
        public string Text { get; set; } = "";
        public string[] Lines { get; set; } = Array.Empty<string>();
        
        public void UpdateText(string newText)
        {
            Text = newText;
            Lines = newText.Split('\n');
        }
        
        public void ApplyChanges(TextDocumentContentChangeEvent[] changes)
        {
            foreach (var change in changes)
            {
                if (change.Range == null)
                {
                    // Full document update
                    UpdateText(change.Text);
                }
                else
                {
                    // Incremental update
                    ApplyIncrementalChange(change);
                }
            }
        }
        
        private void ApplyIncrementalChange(TextDocumentContentChangeEvent change)
        {
            if (change.Range == null) return;
            
            var startLine = change.Range.Start.Line;
            var startChar = change.Range.Start.Character;
            var endLine = change.Range.End.Line;
            var endChar = change.Range.End.Character;
            
            var lines = Text.Split('\n').ToList();
            
            if (startLine == endLine)
            {
                // Single line change
                var line = lines[startLine];
                var newLine = line.Substring(0, startChar) + change.Text + line.Substring(endChar);
                lines[startLine] = newLine;
            }
            else
            {
                // Multi-line change
                var firstPart = lines[startLine].Substring(0, startChar);
                var lastPart = lines[endLine].Substring(endChar);
                var newText = firstPart + change.Text + lastPart;
                
                // Remove the old lines
                for (int i = endLine; i >= startLine; i--)
                {
                    lines.RemoveAt(i);
                }
                
                // Insert the new lines
                var newLines = newText.Split('\n');
                for (int i = 0; i < newLines.Length; i++)
                {
                    lines.Insert(startLine + i, newLines[i]);
                }
            }
            
            UpdateText(string.Join('\n', lines));
        }
        
        public string GetTextAtPosition(Position position)
        {
            if (position.Line < 0 || position.Line >= Lines.Length)
                return "";
                
            var line = Lines[position.Line];
            if (position.Character < 0 || position.Character >= line.Length)
                return "";
                
            return line[position.Character].ToString();
        }
        
        public string GetWordAtPosition(Position position)
        {
            if (position.Line >= Lines.Length)
                return string.Empty;
            
            var line = Lines[position.Line];
            if (position.Character >= line.Length)
                return string.Empty;
            
            // Find word boundaries
            var start = position.Character;
            var end = position.Character;
            
            // Move start backwards to find word start
            while (start > 0 && IsWordCharacter(line[start - 1]))
                start--;
            
            // Move end forwards to find word end
            while (end < line.Length && IsWordCharacter(line[end]))
                end++;
            
            if (start == end)
                return string.Empty;
            
            return line.Substring(start, end - start);
        }
        
        public Protocol.Range GetWordRangeAtPosition(Position position)
        {
            if (position.Line >= Lines.Length)
                return new Protocol.Range { Start = position, End = position };
            
            var line = Lines[position.Line];
            if (position.Character >= line.Length)
                return new Protocol.Range { Start = position, End = position };
            
            // Find word boundaries
            var start = position.Character;
            var end = position.Character;
            
            // Move start backwards to find word start
            while (start > 0 && IsWordCharacter(line[start - 1]))
                start--;
            
            // Move end forwards to find word end
            while (end < line.Length && IsWordCharacter(line[end]))
                end++;
            
            return new Protocol.Range
            {
                Start = new Position { Line = position.Line, Character = start },
                End = new Position { Line = position.Line, Character = end }
            };
        }
        
        private static bool IsWordCharacter(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
    
    public class DocumentManager
    {
        private readonly ConcurrentDictionary<string, TextDocument> _documents = new();
        
        public event Action<TextDocument>? DocumentOpened;
        public event Action<TextDocument>? DocumentChanged;
        public event Action<string>? DocumentClosed;
        
        public void OpenDocument(TextDocumentItem item)
        {
            var document = new TextDocument
            {
                Uri = item.Uri,
                LanguageId = item.LanguageId,
                Version = item.Version,
                Text = item.Text
            };
            document.UpdateText(item.Text);
            
            _documents[item.Uri] = document;
            DocumentOpened?.Invoke(document);
        }
        
        public void ChangeDocument(DidChangeTextDocumentParams changeParams)
        {
            if (_documents.TryGetValue(changeParams.TextDocument.Uri, out var document))
            {
                if (changeParams.TextDocument.Version.HasValue)
                {
                    document.Version = changeParams.TextDocument.Version.Value;
                }
                
                document.ApplyChanges(changeParams.ContentChanges);
                DocumentChanged?.Invoke(document);
            }
        }
        
        public void CloseDocument(DidCloseTextDocumentParams closeParams)
        {
            if (_documents.TryRemove(closeParams.TextDocument.Uri, out _))
            {
                DocumentClosed?.Invoke(closeParams.TextDocument.Uri);
            }
        }
        
        public TextDocument? GetDocument(string uri)
        {
            _documents.TryGetValue(uri, out var document);
            return document;
        }
        
        public IEnumerable<TextDocument> GetAllDocuments()
        {
            return _documents.Values;
        }
        
        public bool HasDocument(string uri)
        {
            return _documents.ContainsKey(uri);
        }
    }
}
