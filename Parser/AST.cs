using uhigh.Net.Lexer;

namespace uhigh.Net.Parser
{
    /// <summary>
    /// The ast node class
    /// </summary>
    public abstract class ASTNode { }

    // Expressions
    /// <summary>
    /// The expression class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public abstract class Expression : ASTNode { }

    /// <summary>
    /// The literal expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class LiteralExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        public object Value { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public TokenType Type { get; set; }
    }

    /// <summary>
    /// The identifier expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class IdentifierExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// The qualified identifier expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class QualifiedIdentifierExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets the parts
        /// </summary>
        /// <returns>The string array</returns>
        public string[] GetParts()
        {
            return Name.Split('.');
        }

        /// <summary>
        /// Gets the namespace
        /// </summary>
        /// <returns>The string</returns>
        public string GetNamespace()
        {
            var parts = GetParts();
            return parts.Length > 1 ? string.Join(".", parts.Take(parts.Length - 1)) : "";
        }

        /// <summary>
        /// Gets the method name
        /// </summary>
        /// <returns>The string</returns>
        public string GetMethodName()
        {
            var parts = GetParts();
            return parts.Last();
        }
    }

    /// <summary>
    /// The binary expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class BinaryExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the left
        /// </summary>
        public Expression Left { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the operator
        /// </summary>
        public TokenType Operator { get; set; }
        /// <summary>
        /// Gets or sets the value of the right
        /// </summary>
        public Expression Right { get; set; } = null!;
    }

    /// <summary>
    /// The unary expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class UnaryExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the operator
        /// </summary>
        public TokenType Operator { get; set; }
        /// <summary>
        /// Gets or sets the value of the operand
        /// </summary>
        public Expression Operand { get; set; } = null!;
    }

    /// <summary>
    /// The call expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class CallExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the function
        /// </summary>
        public Expression Function { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the arguments
        /// </summary>
        public List<Expression> Arguments { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the supports variable arguments
        /// </summary>
        public bool SupportsVariableArguments { get; set; } // For functions like print that can take multiple args
    }

    /// <summary>
    /// The constructor call expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class ConstructorCallExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the class name
        /// </summary>
        public string ClassName { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the arguments
        /// </summary>
        public List<Expression> Arguments { get; set; } = new();
    }

    /// <summary>
    /// The array expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class ArrayExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the elements
        /// </summary>
        public List<Expression> Elements { get; set; } = new();

        // Add support for typed arrays
        /// <summary>
        /// Gets or sets the value of the element type
        /// </summary>
        public string? ElementType { get; set; }

        // Add support for array method chaining
        /// <summary>
        /// Gets or sets the value of the method calls
        /// </summary>
        public List<MethodCallChain> MethodCalls { get; set; } = new();

        // Add support for array indices
        /// <summary>
        /// Gets or sets the value of the is indice
        /// </summary>
        public bool IsIndice { get; set; }
        /// <summary>
        /// Gets or sets the value of the start offset
        /// </summary>
        public Expression? StartOffset { get; set; }

        // Add array type specification for explicit array types
        /// <summary>
        /// Gets or sets the value of the array type
        /// </summary>
        public string? ArrayType { get; set; } // e.g., "string[]", "int[]"
    }

    /// <summary>
    /// The index expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class IndexExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the object
        /// </summary>
        public Expression Object { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the index
        /// </summary>
        public Expression Index { get; set; } = null!;
    }

    /// <summary>
    /// The assignment expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class AssignmentExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the target
        /// </summary>
        public Expression Target { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the operator
        /// </summary>
        public TokenType Operator { get; set; }
        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        public Expression Value { get; set; } = null!;
    }

    /// <summary>
    /// The member access expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class MemberAccessExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the object
        /// </summary>
        public Expression Object { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the member name
        /// </summary>
        public string MemberName { get; set; } = "";
    }

    /// <summary>
    /// The this expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class ThisExpression : Expression { }

    // Statements
    /// <summary>
    /// The statement class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public abstract class Statement : ASTNode { }

    /// <summary>
    /// The expression statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class ExpressionStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the expression
        /// </summary>
        public Expression Expression { get; set; } = null!;
    }

    /// <summary>
    /// The match statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class MatchStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        public Expression Value { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the arms
        /// </summary>
        public List<MatchArm> Arms { get; set; } = new();
    }
    /// <summary>
    /// The variable declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class VariableDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the initializer
        /// </summary>
        public Expression? Initializer { get; set; }
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public string? Type { get; set; }
        /// <summary>
        /// Gets or sets the value of the is constant
        /// </summary>
        public bool IsConstant { get; set; }
    }

    /// <summary>
    /// The if statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class IfStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the condition
        /// </summary>
        public Expression Condition { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the then branch
        /// </summary>
        public List<Statement> ThenBranch { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the else branch
        /// </summary>
        public List<Statement>? ElseBranch { get; set; }
    }

    /// <summary>
    /// The while statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class WhileStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the condition
        /// </summary>
        public Expression Condition { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the body
        /// </summary>
        public List<Statement> Body { get; set; } = new();
    }

    /// <summary>
    /// The for statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class ForStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the initializer
        /// </summary>
        public Statement? Initializer { get; set; }
        /// <summary>
        /// Gets or sets the value of the condition
        /// </summary>
        public Expression? Condition { get; set; }
        /// <summary>
        /// Gets or sets the value of the increment
        /// </summary>
        public Statement? Increment { get; set; }
        /// <summary>
        /// Gets or sets the value of the body
        /// </summary>
        public List<Statement> Body { get; set; } = new();

        // Add support for for-in loops
        /// <summary>
        /// Gets or sets the value of the iterator variable
        /// </summary>
        public string? IteratorVariable { get; set; }
        /// <summary>
        /// Gets or sets the value of the iterable expression
        /// </summary>
        public Expression? IterableExpression { get; set; }
        /// <summary>
        /// Gets the value of the is for in loop
        /// </summary>
        public bool IsForInLoop => IteratorVariable != null && IterableExpression != null;
    }

    /// <summary>
    /// The return statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class ReturnStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        public Expression? Value { get; set; }
    }

    /// <summary>
    /// The break statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class BreakStatement : Statement { }

    /// <summary>
    /// The continue statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class ContinueStatement : Statement { }

    /// <summary>
    /// The sharp block class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class SharpBlock : Statement
    {
        /// <summary>
        /// Gets or sets the value of the code
        /// </summary>
        public string Code { get; set; } = "";
    }

    /// <summary>
    /// The class declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class ClassDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the base class
        /// </summary>
        public string? BaseClass { get; set; }
        /// <summary>
        /// Gets or sets the value of the members
        /// </summary>
        public List<Statement> Members { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the attributes
        /// </summary>
        public List<AttributeDeclaration> Attributes { get; set; } = new(); // Add this
        /// <summary>
        /// Gets or sets the value of the generic parameters
        /// </summary>
        public List<string> GenericParameters { get; set; } = new(); // Add this

        /// <summary>
        /// Gets the value of the is public
        /// </summary>
        public bool IsPublic => Modifiers.Contains("public");
    }

    /// <summary>
    /// The namespace declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class NamespaceDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the members
        /// </summary>
        public List<Statement> Members { get; set; } = new();
    }

    /// <summary>
    /// The import statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class ImportStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the class name
        /// </summary>
        public string ClassName { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the assembly name
        /// </summary>
        public string AssemblyName { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the alias
        /// </summary>
        public string? Alias { get; set; }
    }

    /// <summary>
    /// The include statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class IncludeStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the file name
        /// </summary>
        public string FileName { get; set; } = "";
    }

    /// <summary>
    /// The method declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class MethodDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the parameters
        /// </summary>
        public List<Parameter> Parameters { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the body
        /// </summary>
        public List<Statement> Body { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the return type
        /// </summary>
        public string? ReturnType { get; set; }
        /// <summary>
        /// Gets or sets the value of the is static
        /// </summary>
        public bool IsStatic { get; set; }
        /// <summary>
        /// Gets or sets the value of the is constructor
        /// </summary>
        public bool IsConstructor { get; set; }
        /// <summary>
        /// Gets or sets the value of the attributes
        /// </summary>
        public List<AttributeDeclaration> Attributes { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new(); // New: access modifiers
        /// <summary>
        /// Gets or sets the value of the generic parameters
        /// </summary>
        public List<string> GenericParameters { get; set; } = new(); // Add this
    }

    /// <summary>
    /// The property accessor class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class PropertyAccessor : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public string Type { get; set; } = ""; // "get" or "set"
        /// <summary>
        /// Gets or sets the value of the body
        /// </summary>
        public Expression? Body { get; set; } // null for auto-implemented
        /// <summary>
        /// Gets or sets the value of the statements
        /// </summary>
        public List<Statement> Statements { get; set; } = new(); // for block body
    }

    /// <summary>
    /// The property declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class PropertyDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public string? Type { get; set; }
        /// <summary>
        /// Gets or sets the value of the initializer
        /// </summary>
        public Expression? Initializer { get; set; }
        /// <summary>
        /// Gets or sets the value of the is static
        /// </summary>
        public bool IsStatic { get; set; }
        /// <summary>
        /// Gets or sets the value of the accessors
        /// </summary>
        public List<PropertyAccessor> Accessors { get; set; } = new(); // New: getter/setter
        /// <summary>
        /// Gets the value of the has auto implemented accessors
        /// </summary>
        public bool HasAutoImplementedAccessors => Accessors.Any() && Accessors.All(a => a.Body == null && a.Statements.Count == 0);
        /// <summary>
        /// Gets the value of the has custom accessors
        /// </summary>
        public bool HasCustomAccessors => Accessors.Any(a => a.Body != null || a.Statements.Count > 0);
    }

    /// <summary>
    /// The field declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class FieldDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public string? Type { get; set; }
        /// <summary>
        /// Gets or sets the value of the initializer
        /// </summary>
        public Expression? Initializer { get; set; }
        /// <summary>
        /// Gets or sets the value of the is static
        /// </summary>
        public bool IsStatic { get; set; }
        /// <summary>
        /// Gets or sets the value of the is readonly
        /// </summary>
        public bool IsReadonly { get; set; }
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new();
        public List<uhigh.Net.Parser.AttributeDeclaration> Attributes { get; set; } = new(); // New: attributes for fields
    }

    /// <summary>
    /// The enum declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class EnumDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the members
        /// </summary>
        public List<EnumMember> Members { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the base type
        /// </summary>
        public string? BaseType { get; set; } // int, string, etc.
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new();
    }

    /// <summary>
    /// The enum member class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class EnumMember : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        public Expression? Value { get; set; }
    }

    /// <summary>
    /// The interface declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class InterfaceDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the base interfaces
        /// </summary>
        public List<string> BaseInterfaces { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the members
        /// </summary>
        public List<Statement> Members { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the generic parameters
        /// </summary>
        public List<string> GenericParameters { get; set; } = new();
    }

    /// <summary>
    /// The nullable type expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class NullableTypeExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the base type
        /// </summary>
        public Expression BaseType { get; set; } = null!;
    }

    /// <summary>
    /// The null coalescing expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class NullCoalescingExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the left
        /// </summary>
        public Expression Left { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the right
        /// </summary>
        public Expression Right { get; set; } = null!;
    }

    /// <summary>
    /// The null conditional expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class NullConditionalExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the object
        /// </summary>
        public Expression Object { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the member
        /// </summary>
        public Expression Member { get; set; } = null!;
    }

    /// <summary>
    /// The interpolated string expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class InterpolatedStringExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the parts
        /// </summary>
        public List<InterpolationPart> Parts { get; set; } = new();
    }

    /// <summary>
    /// The interpolation part class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class InterpolationPart : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the text
        /// </summary>
        public string? Text { get; set; }
        /// <summary>
        /// Gets or sets the value of the expression
        /// </summary>
        public Expression? Expression { get; set; }
        /// <summary>
        /// Gets or sets the value of the format specifier
        /// </summary>
        public string? FormatSpecifier { get; set; }
    }

    /// <summary>
    /// The range expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class RangeExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the start
        /// </summary>
        public Expression? Start { get; set; }
        /// <summary>
        /// Gets or sets the value of the end
        /// </summary>
        public Expression? End { get; set; }
        /// <summary>
        /// Gets or sets the value of the is exclusive
        /// </summary>
        public bool IsExclusive { get; set; } // true for ..<, false for ..

        // Add support for simple range(n) calls
        /// <summary>
        /// Gets the value of the is simple range
        /// </summary>
        public bool IsSimpleRange => Start == null && End != null;
    }

    /// <summary>
    /// The match expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class MatchExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        public Expression Value { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the arms
        /// </summary>
        public List<MatchArm> Arms { get; set; } = new();
    }

    /// <summary>
    /// The match arm class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class MatchArm : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the patterns
        /// </summary>
        public List<Expression> Patterns { get; set; } = new(); // Support multiple patterns like case 1, 2, 3:
        /// <summary>
        /// Gets or sets the value of the result
        /// </summary>
        public Expression Result { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the is default
        /// </summary>
        public bool IsDefault { get; set; } // true for _ pattern
    }

    // Top-level
    /// <summary>
    /// The function declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class FunctionDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the parameters
        /// </summary>
        public List<Parameter> Parameters { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the body
        /// </summary>
        public List<Statement> Body { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the return type
        /// </summary>
        public string? ReturnType { get; set; }
        /// <summary>
        /// Gets or sets the value of the attributes
        /// </summary>
        public List<AttributeDeclaration> Attributes { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new(); // New: access modifiers
        /// <summary>
        /// Gets or sets the value of the generic parameters
        /// </summary>
        public List<string> GenericParameters { get; set; } = new(); // Add this
    }

    /// <summary>
    /// The attribute declaration class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class AttributeDeclaration : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the arguments
        /// </summary>
        public List<Expression> Arguments { get; set; } = new();

        // Helper properties for common attributes
        /// <summary>
        /// Gets the value of the is external
        /// </summary>
        public bool IsExternal => Name.Equals("external", StringComparison.OrdinalIgnoreCase);
        /// <summary>
        /// Gets the value of the is dot net func
        /// </summary>
        public bool IsDotNetFunc => Name.Equals("dotnetfunc", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The parameter class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class Parameter : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public string? Type { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class
        /// </summary>
        public Parameter() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="type">The type</param>
        public Parameter(string name, string? type = null)
        {
            Name = name;
            Type = type;
        }
    }

    /// <summary>
    /// The program class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class Program : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the statements
        /// </summary>
        public List<Statement> Statements { get; set; } = new();
    }

    /// <summary>
    /// The parse exception class
    /// </summary>
    /// <seealso cref="Exception"/>
    public class ParseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParseException"/> class
        /// </summary>
        /// <param name="message">The message</param>
        public ParseException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="ParseException"/> class
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        public ParseException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Add new AST nodes for your keywords
    /// <summary>
    /// The switch statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class SwitchStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the value
        /// </summary>
        public Expression Value { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the cases
        /// </summary>
        public List<SwitchCase> Cases { get; set; } = new();
    }

    /// <summary>
    /// The switch case class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class SwitchCase : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the pattern
        /// </summary>
        public Expression? Pattern { get; set; }
        /// <summary>
        /// Gets or sets the value of the statements
        /// </summary>
        public List<Statement> Statements { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the is default
        /// </summary>
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// The struct declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class StructDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the fields
        /// </summary>
        public List<FieldDeclaration> Fields { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new();
    }

    /// <summary>
    /// The module declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class ModuleDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the members
        /// </summary>
        public List<Statement> Members { get; set; } = new();
    }

    /// <summary>
    /// The let declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class LetDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the initializer
        /// </summary>
        public Expression? Initializer { get; set; }
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public string? Type { get; set; }
        /// <summary>
        /// Gets or sets the value of the is mutable
        /// </summary>
        public bool IsMutable { get; set; }
    }

    /// <summary>
    /// The loop statement class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class LoopStatement : Statement
    {
        /// <summary>
        /// Gets or sets the value of the body
        /// </summary>
        public List<Statement> Body { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the condition
        /// </summary>
        public Expression? Condition { get; set; } // for until loops
    }

    /// <summary>
    /// The type alias declaration class
    /// </summary>
    /// <seealso cref="Statement"/>
    public class TypeAliasDeclaration : Statement
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the type
        /// </summary>
        public TypeAnnotation Type { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the modifiers
        /// </summary>
        public List<string> Modifiers { get; set; } = new();
    }

    // Add a type annotation class to represent generic types
    /// <summary>
    /// The type annotation class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class TypeAnnotation : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the type arguments
        /// </summary>
        public List<TypeAnnotation> TypeArguments { get; set; } = new();
    }

    /// <summary>
    /// The block expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class BlockExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the statements
        /// </summary>
        public List<Statement> Statements { get; set; } = new();
    }

    // Add lambda expression support
    /// <summary>
    /// The lambda expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class LambdaExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the parameters
        /// </summary>
        public List<Parameter> Parameters { get; set; } = new();
        /// <summary>
        /// Gets or sets the value of the body
        /// </summary>
        public Expression? Body { get; set; } // For expression lambdas
        /// <summary>
        /// Gets or sets the value of the statements
        /// </summary>
        public List<Statement> Statements { get; set; } = new(); // For block lambdas
        /// <summary>
        /// Gets or sets the value of the is async
        /// </summary>
        public bool IsAsync { get; set; }

        /// <summary>
        /// Gets the value of the is expression lambda
        /// </summary>
        public bool IsExpressionLambda => Body != null;
        /// <summary>
        /// Gets the value of the is block lambda
        /// </summary>
        public bool IsBlockLambda => Statements.Count > 0;
    }

    // Add new AST nodes for array operations
    /// <summary>
    /// The array indice expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class ArrayIndiceExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the source array
        /// </summary>
        public Expression SourceArray { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the start offset
        /// </summary>
        public Expression StartOffset { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the element type
        /// </summary>
        public string? ElementType { get; set; }
    }

    /// <summary>
    /// The array method call expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class ArrayMethodCallExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the array
        /// </summary>
        public Expression Array { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the method name
        /// </summary>
        public string MethodName { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the arguments
        /// </summary>
        public List<Expression> Arguments { get; set; } = new();
    }

    /// <summary>
    /// The array collect expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class ArrayCollectExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the array
        /// </summary>
        public Expression Array { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the indices
        /// </summary>
        public List<Expression> Indices { get; set; } = new();
    }

    /// <summary>
    /// The array at expression class
    /// </summary>
    /// <seealso cref="Expression"/>
    public class ArrayAtExpression : Expression
    {
        /// <summary>
        /// Gets or sets the value of the array
        /// </summary>
        public Expression Array { get; set; } = null!;
        /// <summary>
        /// Gets or sets the value of the index
        /// </summary>
        public Expression Index { get; set; } = null!;
    }

    // Add method call chain for array methods
    /// <summary>
    /// The method call chain class
    /// </summary>
    /// <seealso cref="ASTNode"/>
    public class MethodCallChain : ASTNode
    {
        /// <summary>
        /// Gets or sets the value of the method name
        /// </summary>
        public string MethodName { get; set; } = "";
        /// <summary>
        /// Gets or sets the value of the arguments
        /// </summary>
        public List<Expression> Arguments { get; set; } = new();
    }
}

// All code generators traverse these AST node classes to generate code.
// Add your code generator classes in the appropriate project directory.
