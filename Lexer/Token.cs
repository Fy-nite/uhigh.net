namespace uhigh.Net.Lexer
{
    public enum TokenType
    {
        // Literals
        Number,
        Array,
        Using,
        String,
        Identifier,
        
        // Keywords
        New,
        Var, Const, Func, Return, If, Else, While, For, In,
        Print, Include, Try, Catch, Throw, Field, Match, Type, // Added Type
        True, False, Int, Float, StringType, Bool, Void,
        Sharp, Range,
        Break, Continue, // Add break and continue support
        Class, Namespace, Import,  From, This, // Add Using token
        Get, Set, // Add getter/setter keywords
        Enum, Interface, Extension, Record, // New feature keywords
        Switch, Case, Default, // Example: switch statement keywords
        Struct, Union,         // Example: data structure keywords
        Module, Use,           // Example: module system keywords
        Let, Mut,             // Example: variable declaration keywords
        Async, Await,         // Already exists, but example of async keywords
        Macro, Yield,         // Example: advanced language features
        
        // Access Modifiers and Keywords
        Public, Private, Protected, Internal, Static, Abstract, Virtual, Override, Sealed,
        Readonly,
        
        // Nullable and null operators
        Question, // ? for nullable types
        QuestionQuestion, // ?? null coalescing
        QuestionDot, // ?. null conditional
        ExclamationMark, // ! null forgiving
        
        // String interpolation
        InterpolatedStringStart, // $"
        InterpolatedStringMid,   // middle part
        InterpolatedStringEnd,   // end part
        InterpolationStart,      // {
        InterpolationEnd,        // }
        
        // Range operators
        DotDot,          // ..
        DotDotLess,      // ..<
        
        // Operators
        Plus, Minus, Multiply, Divide, Modulo, Assign,
        Equal, NotEqual, Less, Greater, LessEqual, GreaterEqual,
        And, Or, Not, Increment, Decrement,
        PlusAssign, MinusAssign, MultiplyAssign, DivideAssign,
        Arrow, // => for match arms and lambda expressions
        Underscore, // _ for default case
        
        // Punctuation
        LeftParen, RightParen, LeftBrace, RightBrace,
        LeftBracket, RightBracket, Comma, Semicolon, Colon,
        As, Dot,
        
        // Special
        Comment, Newline, EOF, Boolean,
        
        // Attributes
        Attribute
    }

    public record Token(TokenType Type, string Value, int Line, int Column);
}
