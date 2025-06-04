namespace Wake.Net.Lexer
{
    public enum TokenType
    {
        // Literals
        Number,
        String,
        Identifier,
        New,
        
        // Keywords
        Var, Const, Func, Return, If, Else, While, For, In,
        Print, Include, Try, Catch, Throw, Field, // Added Field
        True, False, Int, Float, StringType, Bool, Asm, Range,
        Break, Continue, // Add break and continue support
        Class, Namespace, Import, From, This, // New keywords
        
        // Access Modifiers and Keywords
        Public, Private, Protected, Internal, Static, Abstract, Virtual, Override, Sealed,
        Readonly, Async, Await,
        
        // Operators
        Plus, Minus, Multiply, Divide, Modulo, Assign,
        Equal, NotEqual, Less, Greater, LessEqual, GreaterEqual,
        And, Or, Not, Increment, Decrement,
        PlusAssign, MinusAssign, MultiplyAssign, DivideAssign,
        
        // Punctuation
        LeftParen, RightParen, LeftBrace, RightBrace,
        LeftBracket, RightBracket, Comma, Semicolon, Colon,
        Question, Dot,
        
        // Special
        Comment, Newline, EOF, Boolean,
        
        // Attributes
        Attribute
    }

    public record Token(TokenType Type, string Value, int Line, int Column);
}
