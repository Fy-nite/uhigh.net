using System.Text.RegularExpressions;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Lexer
{
    /// <summary>
    /// The lexer class
    /// </summary>
    public class Lexer
    {
        /// <summary>
        /// The source
        /// </summary>
        private readonly string _source;
        /// <summary>
        /// The position
        /// </summary>
        private int _position;
        /// <summary>
        /// The line
        /// </summary>
        private int _line = 1;
        /// <summary>
        /// The column
        /// </summary>
        private int _column = 1;
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly DiagnosticsReporter _diagnostics;
        private readonly bool _noExceptionMode; // Add this
        
        /// <summary>
        /// The match
        /// </summary>
        private static readonly Dictionary<string, TokenType> Keywords = new()
        {
            { "new", TokenType.New },
            { "const", TokenType.Const },
            { "var", TokenType.Var },
            { "field", TokenType.Field },
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
            { "type", TokenType.Type },
            { "include", TokenType.Include },
            { "sharp", TokenType.Sharp },
            { "range", TokenType.Range },
            { "true", TokenType.True },
            { "false", TokenType.False },
            { "int", TokenType.Int },
            { "float", TokenType.Float },
            { "string", TokenType.StringType },
            { "bool", TokenType.Bool },
            { "void", TokenType.Void },
            { "class", TokenType.Class },
            { "namespace", TokenType.Namespace },
            { "import", TokenType.Import },
            { "using", TokenType.Using },
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
            



            // { "switch", TokenType.Switch },
            // { "case", TokenType.Case },
            // { "default", TokenType.Default },
            // { "struct", TokenType.Struct },
            // { "union", TokenType.Union },
            // { "module", TokenType.Module },
            // { "use", TokenType.Use },
            // { "let", TokenType.Let },
            // { "mut", TokenType.Mut },
            // { "macro", TokenType.Macro },
            // { "yield", TokenType.Yield },

            // { "loop", TokenType.Loop },
            // { "until", TokenType.Until },
            

            // { "finally", TokenType.Finally },
            

            // { "typeof", TokenType.Typeof },
            // { "sizeof", TokenType.Sizeof },
            
            // { "extern", TokenType.Extern },
            // { "inline", TokenType.Inline }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Lexer"/> class
        /// </summary>
        /// <param name="source">The source</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="verboseMode">The verbose mode</param>
        /// <param name="noExceptionMode">The no exception mode</param>
        public Lexer(string source, DiagnosticsReporter? diagnostics = null, bool verboseMode = false, bool noExceptionMode = false)
        {
            _source = source;
            _diagnostics = diagnostics ?? new DiagnosticsReporter(verboseMode);
            _noExceptionMode = noExceptionMode; // Add this
        }

        /// <summary>
        /// Tokenizes this instance
        /// </summary>
        /// <returns>The tokens</returns>
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

        /// <summary>
        /// Nexts the token
        /// </summary>
        /// <exception cref="Exception">Unexpected character '{current}' at line {line}, column {column}</exception>
        /// <returns>The token</returns>
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
                    if (_noExceptionMode) return null;
                    throw;
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
            if (_noExceptionMode) return null;
            throw new Exception($"Unexpected character '{current}' at line {line}, column {column}");
        }

        /// <summary>
        /// Skips the whitespace
        /// </summary>
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

        /// <summary>
        /// Skips the line comment
        /// </summary>
        private void SkipLineComment()
        {
            while (_position < _source.Length && _source[_position] != '\n')
            {
                _position++;
                _column++;
            }
        }

        /// <summary>
        /// Skips the block comment
        /// </summary>
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

        /// <summary>
        /// Peeks this instance
        /// </summary>
        /// <returns>The char</returns>
        private char Peek()
        {
            return _position + 1 < _source.Length ? _source[_position + 1] : '\0';
        }

        /// <summary>
        /// Reads the string using the specified line
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <exception cref="Exception">Unterminated string at line {line}</exception>
        /// <returns>The token</returns>
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
                if (_noExceptionMode) return null;
                throw new Exception($"Unterminated string at line {line}");
            }
            
            var value = _source.Substring(start, _position - start);
            _position++; // Skip closing quote
            _column++;
            
            return new Token(TokenType.String, value, line, column);
        }

        /// <summary>
        /// Reads the number using the specified line
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <returns>The token</returns>
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
                        // Stop here - don't include the second decimal point
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
                if (_noExceptionMode) return null;
            }
            
            return new Token(TokenType.Number, value, line, column);
        }

        /// <summary>
        /// Reads the identifier using the specified line
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <returns>The token</returns>
        private Token ReadIdentifier(int line, int column)
        {
            var start = _position;
            
            // Read the first identifier part
            while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
            {
                _position++;
                _column++;
            }
            
            var value = _source.Substring(start, _position - start);
            
            // Handle dotted identifiers (namespace.class.method, System.Collections.Generic, etc.)
            while (_position < _source.Length && _source[_position] == '.')
            {
                // Look ahead to see if there's an identifier after the dot
                if (_position + 1 < _source.Length && 
                    (char.IsLetter(_source[_position + 1]) || _source[_position + 1] == '_'))
                {
                    // Add the dot
                    value += ".";
                    _position++; // Skip dot
                    _column++;
                    
                    // Read the next identifier part
                    var identifierStart = _position;
                    while (_position < _source.Length && 
                           (char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
                    {
                        _position++;
                        _column++;
                    }
                    
                    // Add the identifier part after the dot
                    if (_position > identifierStart)
                    {
                        value += _source.Substring(identifierStart, _position - identifierStart);
                    }
                    else
                    {
                        // If no identifier after dot, back up and break
                        _position--;
                        _column--;
                        value = value.Substring(0, value.Length - 1); // Remove the dot
                        break;
                    }
                }
                else
                {
                    // No identifier after dot, stop here
                    break;
                }
            }
            
            // Handle array syntax for type identifiers (e.g., string[], List<string>[])
            // Only if this looks like a type (starts with uppercase or is a known type keyword)
            if (IsTypeIdentifier(value) && _position < _source.Length && _source[_position] == '[')
            {
                // Look ahead to see if it's an array type declaration
                var nextPos = _position + 1;
                if (nextPos < _source.Length && _source[nextPos] == ']')
                {
                    value += "[]";
                    _position += 2; // Skip []
                    _column += 2;
                }
            }
            
            // Check if this is a keyword (only check the base identifier for keywords)
            var baseIdentifier = value.Contains('.') ? value.Split('.')[0] : value;
            if (value.EndsWith("[]"))
            {
                baseIdentifier = baseIdentifier.Replace("[]", "");
            }
            
            var tokenType = Keywords.ContainsKey(baseIdentifier) ? Keywords[baseIdentifier] : TokenType.Identifier;
            
            // For dotted identifiers or array types, always treat as Identifier regardless of the first part being a keyword
            if (value.Contains('.') || value.EndsWith("[]"))
            {
                tokenType = TokenType.Identifier;
            }
            
            return new Token(tokenType, value, line, column);
        }
        
        /// <summary>
        /// Ises the type identifier using the specified identifier
        /// </summary>
        /// <param name="identifier">The identifier</param>
        /// <returns>The bool</returns>
        private bool IsTypeIdentifier(string identifier)
        {
            // Check if this looks like a type name
            if (string.IsNullOrEmpty(identifier)) return false;
            
            // Known built-in types
            var builtinTypes = new[] { "string", "int", "float", "bool", "void", "object", "double", "decimal" };
            if (builtinTypes.Contains(identifier)) return true;
            
            // Check if it's a generic type (contains < >)
            if (identifier.Contains('<') && identifier.Contains('>')) return true;
            
            // Check if it starts with uppercase (typical for class names)
            return char.IsUpper(identifier[0]);
        }

        /// <summary>
        /// Gets the two char token using the specified two char
        /// </summary>
        /// <param name="twoChar">The two char</param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <returns>The token</returns>
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

        /// <summary>
        /// Reads the interpolated string using the specified line
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <returns>The token</returns>
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

        /// <summary>
        /// Gets the single char token using the specified c
        /// </summary>
        /// <param name="c">The </param>
        /// <param name="line">The line</param>
        /// <param name="column">The column</param>
        /// <returns>The token</returns>
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
