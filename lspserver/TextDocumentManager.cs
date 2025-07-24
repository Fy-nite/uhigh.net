using LanguageServer.Parameters.TextDocument;

namespace UhighLanguageServer
{
    public class TextDocumentManager
    {
        private readonly List<TextDocumentItem> _all = new List<TextDocumentItem>();
        public IReadOnlyList<TextDocumentItem> All => _all;

        public void Add(TextDocumentItem document)
        {
            if (_all.Any(x => x.uri == document.uri))
            {
                return;
            }
            _all.Add(document);
            OnChanged(document);
        }

        public void Change(Uri uri, long version, TextDocumentContentChangeEvent[] changeEvents)
        {
            var index = _all.FindIndex(x => x.uri == uri);
            if (index < 0)
            {
                return;
            }
            var document = _all[index];
            if (document.version >= version)
            {
                return;
            }
            foreach (var ev in changeEvents)
            {
                Apply(document, ev);
            }
            document.version = version;
            OnChanged(document);
        }

        private void Apply(TextDocumentItem document, TextDocumentContentChangeEvent ev)
        {
            if (ev.range != null)
            {
                var startPos = GetPosition(document.text, (int)ev.range.start.line, (int)ev.range.start.character);
                var endPos = GetPosition(document.text, (int)ev.range.end.line, (int)ev.range.end.character);
                var newText = document.text.Substring(0, startPos) + ev.text + document.text.Substring(endPos);
                document.text = newText;
            }
            else
            {
                document.text = ev.text;
            }
        }

        private static int GetPosition(string text, int line, int character)
        {
            // Split lines using both \r\n and \n
            var lines = text.Replace("\r\n", "\n").Split('\n');
            if (line < 0) line = 0;
            if (line >= lines.Length) line = lines.Length - 1;
            int pos = 0;
            for (int i = 0; i < line; i++)
            {
                // Add length of line + 1 for the newline character
                pos += lines[i].Length + 1;
            }
            // Clamp character to line length
            int charInLine = Math.Min(character, lines[line].Length);
            pos += charInLine;
            // Clamp to text length
            if (pos > text.Length) pos = text.Length;
            return pos;
        }

        public void Remove(Uri uri)
        {
            var index = _all.FindIndex(x => x.uri == uri);
            if (index < 0)
            {
                return;
            }
            _all.RemoveAt(index);
        }

        public event EventHandler<TextDocumentChangedEventArgs> Changed;

        protected virtual void OnChanged(TextDocumentItem document)
        {
            Changed?.Invoke(this, new TextDocumentChangedEventArgs(document));
        }
    }
}
