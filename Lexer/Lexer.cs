using System.Text.RegularExpressions;
using Wake.Net.Diagnostics;

namespace Wake.Net.Lexer
{
    public class Lexer
    {
        private readonly string _source;
        private int _position;
        private int _line = 1;
        private int _column = 1;
        private readonly DiagnosticsReporter _diagnostics;
        
        private static readonly Dictionary<string, TokenType> Keywords = new()
        {
            { "const", TokenType.Const },
            { "var", TokenType.Var },
            { "field", TokenType.Field }, // Added field keyword
            { "if", TokenType.If },
            { "else", TokenType.Else },
            { "while", TokenType.While },
            { "for", TokenType.For },
            { "in", TokenType.In },
            { "func", TokenType.Func },
            { "return", TokenType.Return },
            { "break", TokenType.Break },
            { "continue", TokenType.Continue },
            { "try", TokenType.Try },
            { "catch", TokenType.Catch },
            { "throw", TokenType.Throw },
            { "include", TokenType.Include },
            { "sharp", TokenType.Sharp },
            { "range", TokenType.Range },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "int", TokenType.Int },
            { "float", TokenType.Float },
            { "string", TokenType.StringType },
            { "bool", TokenType.Bool },
            { "class", TokenType.Class },
            { "namespace", TokenType.Namespace },
            { "import", TokenType.Import },
            { "from", TokenType.From },
            { "this", TokenType.This },
            { "enum", TokenType.Enum },
            { "interface", TokenType.Interface },
            { "extension", TokenType.Extension },
            { "record", TokenType.Record },
            { "get", TokenType.Get },
            { "set", TokenType.Set },
            
            // Access Modifiers and Keywords
            { "public", TokenType.Public },
            { "private", TokenType.Private },
            { "protected", TokenType.Protected },
            { "internal", TokenType.Internal },
            { "static", TokenType.Static },
            { "abstract", TokenType.Abstract },
            { "virtual", TokenType.Virtual },
            { "override", TokenType.Override },
            { "sealed", TokenType.Sealed },
            { "readonly", TokenType.Readonly },
            { "async", TokenType.Async },
            { "await", TokenType.Await },
            { "match", TokenType.Match }
        };

        public Lexer(string source, DiagnosticsReporter? diagnostics = null)
        {
            _source = source;
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            
            _diagnostics.ReportInfo($"Starting tokenization of {_source.Length} characters");
            
            while (_position < _source.Length)
            {
                try
                {
                    var token = NextToken();
                    if (token != null)
                    {
                        tokens.Add(token);
                    }
                }
                catch (Exception ex)
                {
                    _diagnostics.ReportError($"Tokenization error: {ex.Message}", _line, _column, "UH005", ex);
                    // Try to recover by skipping the problematic character
                    _position++;
                    _column++;
                }
            }
            
            tokens.Add(new Token(TokenType.EOF, "", _line, _column));
            _diagnostics.ReportInfo($"Tokenization completed. Generated {tokens.Count} tokens");
            return tokens;
        }

        private Token? NextToken()
        {
            SkipWhitespace();
            
            if (_position >= _source.Length)
                return null;

            var current = _source[_position];
            var line = _line;
            var column = _column;

            // Comments
            if (current == '/' && Peek() == '/')
            {
                SkipLineComment();
                return NextToken();
            }
            
            if (current == '/' && Peek() == '*')
            {
                SkipBlockComment();
                return NextToken();
            }

            // Numbers
            if (char.IsDigit(current))
            {
                return ReadNumber(line, column);
            }

            // Strings
            if (current == '"')
            {
                try
                {
                    return ReadString(line, column);
                }
                catch (Exception)
                {
                    _diagnostics.ReportUnterminatedString(line, column);
                    return null;
                }
            }

            // Identifiers
            if (char.IsLetter(current) || current == '_')
            {
                return ReadIdentifier(line, column);
            }

            // Two-character operators
            var twoChar = _source.Substring(_position, Math.Min(2, _source.Length - _position));
            var twoCharToken = GetTwoCharToken(twoChar, line, column);
            if (twoCharToken != null)
            {
                _position += 2;
                _column += 2;
                return twoCharToken;
            }

            // Single-character tokens
            var singleToken = GetSingleCharToken(current, line, column);
            if (singleToken != null)
            {
                _position++;
                _column++;
                return singleToken;
            }

            _diagnostics.ReportUnknownCharacter(current, line, column);
            throw new Exception($"Unexpected character '{current}' at line {line}, column {column}");
        }

        private void SkipWhitespace()
        {
            while (_position < _source.Length && char.IsWhiteSpace(_source[_position]))
            {
                if (_source[_position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else if (_source[_position] != '\r') // Don't count carriage returns
                {
                    _column++;
                }
                _position++;
            }
        }

        private void SkipLineComment()
        {
            while (_position < _source.Length && _source[_position] != '\n')
            {
                _position++;
                _column++;
            }
        }

        private void SkipBlockComment()
        {
            _position += 2; // Skip /*
            _column += 2;
            
            while (_position < _source.Length - 1)
            {
                if (_source[_position] == '*' && _source[_position + 1] == '/')
                {
                    _position += 2;
                    _column += 2;
                    return;
                }
                
                if (_source[_position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
        }

        private char Peek()
        {
            return _position + 1 < _source.Length ? _source[_position + 1] : '\0';
        }

        private Token ReadString(int line, int column)
        {
            _position++; // Skip opening quote
            _column++;
            var start = _position;
            
            while (_position < _source.Length && _source[_position] != '"')
            {
                if (_source[_position] == '\n')
                {
                    _line++;
                    _column = 1;
                }
                else
                {
                    _column++;
                }
                _position++;
            }
            
            if (_position >= _source.Length)
            {
                _diagnostics.ReportUnterminatedString(line, column);
                throw new Exception($"Unterminated string at line {line}");
            }
            
            var value = _source.Substring(start, _position - start);
            _position++; // Skip closing quote
            _column++;
            
            return new Token(TokenType.String, value, line, column);
        }

        private Token ReadNumber(int line, int column)
        {
            var start = _position;
            bool hasDecimalPoint = false;
            
            while (_position < _source.Length && (char.IsDigit(_source[_position]) || _source[_position] == '.'))
            {
                if (_source[_position] == '.')
                {
                    if (hasDecimalPoint)
                    {
                        _diagnostics.ReportInvalidNumber(_source.Substring(start, _position - start + 1), line, column);
                        break;
                    }
                    hasDecimalPoint = true;
                }
                _position++;
                _column++;
            }
            
            var value = _source.Substring(start, _position - start);
            
            // Validate the number format
            if (!double.TryParse(value, out _))
            {
                _diagnostics.ReportInvalidNumber(value, line, column);
            }
            
            return new Token(TokenType.Number, value, line, column);
        }

        private Token ReadIdentifier(int line, int column)
        {
            var start = _position;
            
            while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
            {
                _position++;
                _column++;
            }
            
            // Check if this is a dotted identifier (namespace.class or class.method)
            var value = _source.Substring(start, _position - start);
            
            // Look ahead for dots followed by identifiers
            var tempPos = _position;
            var tempCol = _column;
            
            while (tempPos < _source.Length && _source[tempPos] == '.' && 
                   tempPos + 1 < _source.Length && (char.IsLetter(_source[tempPos + 1]) || _source[tempPos + 1] == '_'))
            {
                tempPos++; // Skip dot
                tempCol++;
                
                // Read the next identifier part
                while (tempPos < _source.Length && (char.IsLetterOrDigit(_source[tempPos]) || _source[tempPos] == '_'))
                {
                    tempPos++;
                    tempCol++;
                }
                
                // Update the value to include the dotted part
                value = _source.Substring(start, tempPos - start);
                _position = tempPos;
                _column = tempCol;
            }
            
            var tokenType = Keywords.ContainsKey(value) ? Keywords[value] : TokenType.Identifier;
            
            return new Token(tokenType, value, line, column);
        }

        private Token? GetTwoCharToken(string twoChar, int line, int column)
        {
            return twoChar switch
            {
                "==" => new Token(TokenType.Equal, twoChar, line, column),
                "!=" => new Token(TokenType.NotEqual, twoChar, line, column),
                "<=" => new Token(TokenType.LessEqual, twoChar, line, column),
                ">=" => new Token(TokenType.GreaterEqual, twoChar, line, column),
                "&&" => new Token(TokenType.And, twoChar, line, column),
                "||" => new Token(TokenType.Or, twoChar, line, column),
                "++" => new Token(TokenType.Increment, twoChar, line, column),
                "--" => new Token(TokenType.Decrement, twoChar, line, column),
                "+=" => new Token(TokenType.PlusAssign, twoChar, line, column),
                "-=" => new Token(TokenType.MinusAssign, twoChar, line, column),
                "*=" => new Token(TokenType.MultiplyAssign, twoChar, line, column),
                "/=" => new Token(TokenType.DivideAssign, twoChar, line, column),
                "??" => new Token(TokenType.QuestionQuestion, twoChar, line, column),
                "?." => new Token(TokenType.QuestionDot, twoChar, line, column),
                ".." => new Token(TokenType.DotDot, twoChar, line, column),
                "$\"" => new Token(TokenType.InterpolatedStringStart, twoChar, line, column),
                "=>" => new Token(TokenType.Arrow, twoChar, line, column),
                _ => null
            };
        }

        private Token ReadInterpolatedString(int line, int column)
        {
            _position += 2; // Skip $"
            _column += 2;
            var parts = new List<string>();
            var expressions = new List<string>();
            
            while (_position < _source.Length && _source[_position] != '"')
            {
                if (_source[_position] == '{')
                {
                    // Read interpolation expression
                    _position++; // Skip {
                    _column++;
                    var exprStart = _position;
                    var braceCount = 1;
                    
                    while (_position < _source.Length && braceCount > 0)
                    {
                        if (_source[_position] == '{') braceCount++;
                        else if (_source[_position] == '}') braceCount--;
                        _position++;
                        _column++;
                    }
                    
                    var expression = _source.Substring(exprStart, _position - exprStart - 1);
                    expressions.Add(expression);
                }
                else
                {
                    var textStart = _position;
                    while (_position < _source.Length && _source[_position] != '{' && _source[_position] != '"')
                    {
                        _position++;
                        _column++;
                    }
                    
                    if (_position > textStart)
                    {
                        parts.Add(_source.Substring(textStart, _position - textStart));
                    }
                }
            }
            
            if (_position < _source.Length)
            {
                _position++; // Skip closing "
                _column++;
            }
            
            // For now, return as a string with special marker
            var value = string.Join("", parts.Zip(expressions, (p, e) => p + "{" + e + "}"));
            return new Token(TokenType.String, "$\"" + value + "\"", line, column);
        }

        private Token? GetSingleCharToken(char c, int line, int column)
        {
            return c switch
            {
                '+' => new Token(TokenType.Plus, c.ToString(), line, column),
                '-' => new Token(TokenType.Minus, c.ToString(), line, column),
                '*' => new Token(TokenType.Multiply, c.ToString(), line, column),
                '/' => new Token(TokenType.Divide, c.ToString(), line, column),
                '%' => new Token(TokenType.Modulo, c.ToString(), line, column),
                '=' => new Token(TokenType.Assign, c.ToString(), line, column),
                '<' => new Token(TokenType.Less, c.ToString(), line, column),
                '>' => new Token(TokenType.Greater, c.ToString(), line, column),
                '!' => new Token(TokenType.Not, c.ToString(), line, column),
                '(' => new Token(TokenType.LeftParen, c.ToString(), line, column),
                ')' => new Token(TokenType.RightParen, c.ToString(), line, column),
                '{' => new Token(TokenType.LeftBrace, c.ToString(), line, column),
                '}' => new Token(TokenType.RightBrace, c.ToString(), line, column),
                '[' => new Token(TokenType.LeftBracket, c.ToString(), line, column),
                ']' => new Token(TokenType.RightBracket, c.ToString(), line, column),
                ',' => new Token(TokenType.Comma, c.ToString(), line, column),
                ';' => new Token(TokenType.Semicolon, c.ToString(), line, column),
                ':' => new Token(TokenType.Colon, c.ToString(), line, column),
                '?' => new Token(TokenType.Question, c.ToString(), line, column),
                '.' => new Token(TokenType.Dot, c.ToString(), line, column),
                '_' => new Token(TokenType.Underscore, c.ToString(), line, column),
                _ => null
            };
        }
    }
}
