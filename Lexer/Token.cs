namespace uhigh.Net.Lexer
{
    /// <summary>
    /// The token type enum
    /// </summary>
    public enum TokenType
    {
        // Literals
        /// <summary>
        /// The number token type
        /// </summary>
        Number,
        /// <summary>
        /// The array token type
        /// </summary>
        Array,
        /// <summary>
        /// The using token type
        /// </summary>
        Using,
        /// <summary>
        /// The string token type
        /// </summary>
        String,
        /// <summary>
        /// The identifier token type
        /// </summary>
        Identifier,
        
        // Keywords
        /// <summary>
        /// The new token type
        /// </summary>
        New,
        /// <summary>
        /// The var token type
        /// </summary>
        Var, /// <summary>
/// The const token type
/// </summary>
Const, /// <summary>
/// The func token type
/// </summary>
Func, /// <summary>
/// The return token type
/// </summary>
Return, /// <summary>
/// The if token type
/// </summary>
If, /// <summary>
/// The else token type
/// </summary>
Else, /// <summary>
/// The while token type
/// </summary>
While, /// <summary>
/// The for token type
/// </summary>
For, /// <summary>
/// The in token type
/// </summary>
In,
        /// <summary>
        /// The include token type
        /// </summary>
        Include, /// <summary>
/// The try token type
/// </summary>
Try, /// <summary>
/// The catch token type
/// </summary>
Catch, /// <summary>
/// The throw token type
/// </summary>
Throw, /// <summary>
/// The field token type
/// </summary>
Field, /// <summary>
/// The match token type
/// </summary>
Match, /// <summary>
/// The type token type
/// </summary>
Type, // Removed Print - it should be an identifier
        /// <summary>
        /// The true token type
        /// </summary>
        True, /// <summary>
/// The false token type
/// </summary>
False, /// <summary>
/// The int token type
/// </summary>
Int, /// <summary>
/// The float token type
/// </summary>
Float, /// <summary>
/// The string type token type
/// </summary>
StringType, /// <summary>
/// The bool token type
/// </summary>
Bool, /// <summary>
/// The void token type
/// </summary>
Void,
        /// <summary>
        /// The sharp token type
        /// </summary>
        Sharp, /// <summary>
/// The range token type
/// </summary>
Range, // Add Range token
        /// <summary>
        /// The break token type
        /// </summary>
        Break, /// <summary>
/// The continue token type
/// </summary>
Continue, // Add break and continue support
        /// <summary>
        /// The  token type
        /// </summary>
        Class, /// <summary>
/// The namespace token type
/// </summary>
Namespace, /// <summary>
/// The import token type
/// </summary>
Import,  /// <summary>
/// The from token type
/// </summary>
From, /// <summary>
/// The this token type
/// </summary>
This, // Add Using token
        /// <summary>
        /// The get token type
        /// </summary>
        Get, /// <summary>
/// The set token type
/// </summary>
Set, // Add getter/setter keywords
        /// <summary>
        /// The enum token type
        /// </summary>
        Enum, /// <summary>
/// The interface token type
/// </summary>
Interface, /// <summary>
/// The extension token type
/// </summary>
Extension, /// <summary>
/// The record token type
/// </summary>
Record, // New feature keywords
        /// <summary>
        /// The switch token type
        /// </summary>
        Switch, /// <summary>
/// The case token type
/// </summary>
Case, /// <summary>
/// The default token type
/// </summary>
Default, // Example: switch statement keywords
        /// <summary>
        /// The struct token type
        /// </summary>
        Struct, /// <summary>
/// The union token type
/// </summary>
Union,         // Example: data structure keywords
        /// <summary>
        /// The module token type
        /// </summary>
        Module, /// <summary>
/// The use token type
/// </summary>
Use,           // Example: module system keywords
        /// <summary>
        /// The let token type
        /// </summary>
        Let, /// <summary>
/// The mut token type
/// </summary>
Mut,             // Example: variable declaration keywords
        /// <summary>
        /// The  token type
        /// </summary>
        Async, /// <summary>
/// The await token type
/// </summary>
Await,         // Already exists, but example of async keywords
        /// <summary>
        /// The macro token type
        /// </summary>
        Macro, /// <summary>
/// The yield token type
/// </summary>
Yield,         // Example: advanced language features
        
        // Access Modifiers and Keywords
        /// <summary>
        /// The public token type
        /// </summary>
        Public, /// <summary>
/// The private token type
/// </summary>
Private, /// <summary>
/// The protected token type
/// </summary>
Protected, /// <summary>
/// The internal token type
/// </summary>
Internal, /// <summary>
/// The static token type
/// </summary>
Static, /// <summary>
/// The abstract token type
/// </summary>
Abstract, /// <summary>
/// The virtual token type
/// </summary>
Virtual, /// <summary>
/// The override token type
/// </summary>
Override, /// <summary>
/// The sealed token type
/// </summary>
Sealed,
        /// <summary>
        /// The readonly token type
        /// </summary>
        Readonly,
        
        // Nullable and null operators
        /// <summary>
        /// The question token type
        /// </summary>
        Question, // ? for nullable types
        /// <summary>
        /// The question question token type
        /// </summary>
        QuestionQuestion, // ?? null coalescing
        /// <summary>
        /// The question dot token type
        /// </summary>
        QuestionDot, // ?. null conditional
        /// <summary>
        /// The exclamation mark token type
        /// </summary>
        ExclamationMark, // ! null forgiving
        
        // String interpolation
        /// <summary>
        /// The interpolated string start token type
        /// </summary>
        InterpolatedStringStart, // $"
        /// <summary>
        /// The interpolated string mid token type
        /// </summary>
        InterpolatedStringMid,   // middle part
        /// <summary>
        /// The interpolated string end token type
        /// </summary>
        InterpolatedStringEnd,   // end part
        /// <summary>
        /// The interpolation start token type
        /// </summary>
        InterpolationStart,      // {
        /// <summary>
        /// The interpolation end token type
        /// </summary>
        InterpolationEnd,        // }
        
        // Range operators
        /// <summary>
        /// The dot dot token type
        /// </summary>
        DotDot,          // ..
        /// <summary>
        /// The dot dot less token type
        /// </summary>
        DotDotLess,      // ..<
        
        // Operators
        /// <summary>
        /// The plus token type
        /// </summary>
        Plus, /// <summary>
/// The minus token type
/// </summary>
Minus, /// <summary>
/// The multiply token type
/// </summary>
Multiply, /// <summary>
/// The divide token type
/// </summary>
Divide, /// <summary>
/// The modulo token type
/// </summary>
Modulo, /// <summary>
/// The assign token type
/// </summary>
Assign,
        /// <summary>
        /// The equal token type
        /// </summary>
        Equal, /// <summary>
/// The not equal token type
/// </summary>
NotEqual, /// <summary>
/// The less token type
/// </summary>
Less, /// <summary>
/// The greater token type
/// </summary>
Greater, /// <summary>
/// The less equal token type
/// </summary>
LessEqual, /// <summary>
/// The greater equal token type
/// </summary>
GreaterEqual,
        /// <summary>
        /// The and token type
        /// </summary>
        And, /// <summary>
/// The or token type
/// </summary>
Or, /// <summary>
/// The not token type
/// </summary>
Not, /// <summary>
/// The increment token type
/// </summary>
Increment, /// <summary>
/// The decrement token type
/// </summary>
Decrement,
        /// <summary>
        /// The plus assign token type
        /// </summary>
        PlusAssign, /// <summary>
/// The minus assign token type
/// </summary>
MinusAssign, /// <summary>
/// The multiply assign token type
/// </summary>
MultiplyAssign, /// <summary>
/// The divide assign token type
/// </summary>
DivideAssign,
        /// <summary>
        /// The arrow token type
        /// </summary>
        Arrow, // => for match arms and lambda expressions
        /// <summary>
        /// The underscore token type
        /// </summary>
        Underscore, // _ for default case
        
        // Punctuation
        /// <summary>
        /// The left paren token type
        /// </summary>
        LeftParen, /// <summary>
/// The right paren token type
/// </summary>
RightParen, /// <summary>
/// The left brace token type
/// </summary>
LeftBrace, /// <summary>
/// The right brace token type
/// </summary>
RightBrace,
        /// <summary>
        /// The left bracket token type
        /// </summary>
        LeftBracket, /// <summary>
/// The right bracket token type
/// </summary>
RightBracket, /// <summary>
/// The comma token type
/// </summary>
Comma, /// <summary>
/// The semicolon token type
/// </summary>
Semicolon, /// <summary>
/// The colon token type
/// </summary>
Colon,
        /// <summary>
        /// The as token type
        /// </summary>
        As, /// <summary>
/// The dot token type
/// </summary>
Dot,
        
        // Special
        /// <summary>
        /// The comment token type
        /// </summary>
        Comment, /// <summary>
/// The newline token type
/// </summary>
Newline, /// <summary>
/// The eof token type
/// </summary>
EOF, /// <summary>
/// The boolean token type
/// </summary>
Boolean,
        
        // Attributes
        /// <summary>
        /// The attribute token type
        /// </summary>
        Attribute
    }

    /// <summary>
    /// The token
    /// </summary>
    public record Token(TokenType Type, string Value, int Line, int Column);
}
