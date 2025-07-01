using uhigh.Net.Lexer;

namespace uhigh.Net.Parser
{
    public abstract class ASTNode { }
    
    // Expressions
    public abstract class Expression : ASTNode { }
    
    public class LiteralExpression : Expression 
    {
        public object Value { get; set; } = null!;
        public TokenType Type { get; set; }
    }
    
    public class IdentifierExpression : Expression 
    {
        public string Name { get; set; } = "";
    }
    
    public class QualifiedIdentifierExpression : Expression 
    {
        public string Name { get; set; } = "";
        
        public string[] GetParts()
        {
            return Name.Split('.');
        }
        
        public string GetNamespace()
        {
            var parts = GetParts();
            return parts.Length > 1 ? string.Join(".", parts.Take(parts.Length - 1)) : "";
        }
        
        public string GetMethodName()
        {
            var parts = GetParts();
            return parts.Last();
        }
    }
    
    public class BinaryExpression : Expression 
    {
        public Expression Left { get; set; } = null!;
        public TokenType Operator { get; set; }
        public Expression Right { get; set; } = null!;
    }
    
    public class UnaryExpression : Expression 
    {
        public TokenType Operator { get; set; }
        public Expression Operand { get; set; } = null!;
    }
    
    public class CallExpression : Expression 
    {
        public Expression Function { get; set; } = null!;
        public List<Expression> Arguments { get; set; } = new();
        public bool SupportsVariableArguments { get; set; } // For functions like print that can take multiple args
    }
    
    public class ConstructorCallExpression : Expression 
    {
        public string ClassName { get; set; } = "";
        public List<Expression> Arguments { get; set; } = new();
    }

    public class ArrayExpression : Expression 
    {
        public List<Expression> Elements { get; set; } = new();
        
        // Add support for typed arrays
        public string? ElementType { get; set; }
        
        // Add support for array method chaining
        public List<MethodCallChain> MethodCalls { get; set; } = new();
        
        // Add support for array indices
        public bool IsIndice { get; set; }
        public Expression? StartOffset { get; set; }
    }
    
    public class IndexExpression : Expression 
    {
        public Expression Object { get; set; } = null!;
        public Expression Index { get; set; } = null!;
    }
    
    public class AssignmentExpression : Expression 
    {
        public Expression Target { get; set; } = null!;
        public TokenType Operator { get; set; }
        public Expression Value { get; set; } = null!;
    }
    
    public class MemberAccessExpression : Expression
    {
        public Expression Object { get; set; } = null!;
        public string MemberName { get; set; } = "";
    }
    
    public class ThisExpression : Expression { }
    
    // Statements
    public abstract class Statement : ASTNode { }
    
    public class ExpressionStatement : Statement 
    {
        public Expression Expression { get; set; } = null!;
    }
    
    public class MatchStatement : Statement 
    {
        public Expression Value { get; set; } = null!;
        public List<MatchArm> Arms { get; set; } = new();
    }
    public class VariableDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public Expression? Initializer { get; set; }
        public string? Type { get; set; }
        public bool IsConstant { get; set; }
    }
    
    public class IfStatement : Statement 
    {
        public Expression Condition { get; set; } = null!;
        public List<Statement> ThenBranch { get; set; } = new();
        public List<Statement>? ElseBranch { get; set; }
    }
    
    public class WhileStatement : Statement 
    {
        public Expression Condition { get; set; } = null!;
        public List<Statement> Body { get; set; } = new();
    }
    
    public class ForStatement : Statement 
    {
        public Statement? Initializer { get; set; }
        public Expression? Condition { get; set; }
        public Statement? Increment { get; set; }
        public List<Statement> Body { get; set; } = new();
        
        // Add support for for-in loops
        public string? IteratorVariable { get; set; }
        public Expression? IterableExpression { get; set; }
        public bool IsForInLoop => IteratorVariable != null && IterableExpression != null;
    }
    
    public class ReturnStatement : Statement 
    {
        public Expression? Value { get; set; }
    }
    
    public class BreakStatement : Statement { }
    
    public class ContinueStatement : Statement { }
    
    public class SharpBlock : Statement 
    {
        public string Code { get; set; } = "";
    }
    
    public class ClassDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public string? BaseClass { get; set; }
        public List<Statement> Members { get; set; } = new();
        public List<string> Modifiers { get; set; } = new(); // New: access modifiers
        
        public bool IsPublic => Modifiers.Contains("public");
    }
    
    public class NamespaceDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public List<Statement> Members { get; set; } = new();
    }
    
    public class ImportStatement : Statement
    {
        public string ClassName { get; set; } = "";
        public string AssemblyName { get; set; } = "";
        public string? Alias { get; set; }
    }
    
    public class IncludeStatement : Statement
    {
        public string FileName { get; set; } = "";
    }
    
    public class MethodDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public List<Parameter> Parameters { get; set; } = new();
        public List<Statement> Body { get; set; } = new();
        public string? ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public bool IsConstructor { get; set; }
        public List<AttributeDeclaration> Attributes { get; set; } = new();
        public List<string> Modifiers { get; set; } = new(); // New: access modifiers
    }
    
    public class PropertyAccessor : ASTNode
    {
        public string Type { get; set; } = ""; // "get" or "set"
        public Expression? Body { get; set; } // null for auto-implemented
        public List<Statement> Statements { get; set; } = new(); // for block body
    }
    
    public class PropertyDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public Expression? Initializer { get; set; }
        public bool IsStatic { get; set; }
        public List<PropertyAccessor> Accessors { get; set; } = new(); // New: getter/setter
        public bool HasAutoImplementedAccessors => Accessors.Any() && Accessors.All(a => a.Body == null && a.Statements.Count == 0);
        public bool HasCustomAccessors => Accessors.Any(a => a.Body != null || a.Statements.Count > 0);
    }
    
    public class FieldDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public Expression? Initializer { get; set; }
        public bool IsStatic { get; set; }
        public bool IsReadonly { get; set; }
        public List<string> Modifiers { get; set; } = new();
    }
    
    public class EnumDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public List<EnumMember> Members { get; set; } = new();
        public string? BaseType { get; set; } // int, string, etc.
        public List<string> Modifiers { get; set; } = new();
    }

    public class EnumMember : ASTNode
    {
        public string Name { get; set; } = "";
        public Expression? Value { get; set; }
    }

    public class InterfaceDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public List<string> BaseInterfaces { get; set; } = new();
        public List<Statement> Members { get; set; } = new();
        public List<string> Modifiers { get; set; } = new();
        public List<string> GenericParameters { get; set; } = new();
    }

    public class NullableTypeExpression : Expression
    {
        public Expression BaseType { get; set; } = null!;
    }

    public class NullCoalescingExpression : Expression
    {
        public Expression Left { get; set; } = null!;
        public Expression Right { get; set; } = null!;
    }

    public class NullConditionalExpression : Expression
    {
        public Expression Object { get; set; } = null!;
        public Expression Member { get; set; } = null!;
    }

    public class InterpolatedStringExpression : Expression
    {
        public List<InterpolationPart> Parts { get; set; } = new();
    }

    public class InterpolationPart : ASTNode
    {
        public string? Text { get; set; }
        public Expression? Expression { get; set; }
        public string? FormatSpecifier { get; set; }
    }

    public class RangeExpression : Expression
    {
        public Expression? Start { get; set; }
        public Expression? End { get; set; }
        public bool IsExclusive { get; set; } // true for ..<, false for ..
        
        // Add support for simple range(n) calls
        public bool IsSimpleRange => Start == null && End != null;
    }

    public class MatchExpression : Expression
    {
        public Expression Value { get; set; } = null!;
        public List<MatchArm> Arms { get; set; } = new();
    }
    
    public class MatchArm : ASTNode
    {
        public List<Expression> Patterns { get; set; } = new(); // Support multiple patterns like case 1, 2, 3:
        public Expression Result { get; set; } = null!;
        public bool IsDefault { get; set; } // true for _ pattern
    }
    
    // Top-level
    public class FunctionDeclaration : Statement 
    {
        public string Name { get; set; } = "";
        public List<Parameter> Parameters { get; set; } = new();
        public List<Statement> Body { get; set; } = new();
        public string? ReturnType { get; set; }
        public List<AttributeDeclaration> Attributes { get; set; } = new();
        public List<string> Modifiers { get; set; } = new(); // New: access modifiers
    }
    
    public class AttributeDeclaration : ASTNode
    {
        public string Name { get; set; } = "";
        public List<Expression> Arguments { get; set; } = new();
        
        // Helper properties for common attributes
        public bool IsExternal => Name.Equals("external", StringComparison.OrdinalIgnoreCase);
        public bool IsDotNetFunc => Name.Equals("dotnetfunc", StringComparison.OrdinalIgnoreCase);
    }
    
    public class Parameter : ASTNode 
    {
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        
        public Parameter() { }
        
        public Parameter(string name, string? type = null)
        {
            Name = name;
            Type = type;
        }
    }
    
    public class Program : ASTNode 
    {
        public List<Statement> Statements { get; set; } = new();
    }

    public class ParseException : Exception
    {
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception innerException) : base(message, innerException) { }
    }

    // Add new AST nodes for your keywords
    public class SwitchStatement : Statement
    {
        public Expression Value { get; set; } = null!;
        public List<SwitchCase> Cases { get; set; } = new();
    }

    public class SwitchCase : ASTNode
    {
        public Expression? Pattern { get; set; }
        public List<Statement> Statements { get; set; } = new();
        public bool IsDefault { get; set; }
    }

    public class StructDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public List<FieldDeclaration> Fields { get; set; } = new();
        public List<string> Modifiers { get; set; } = new();
    }

    public class ModuleDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public List<Statement> Members { get; set; } = new();
    }

    public class LetDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public Expression? Initializer { get; set; }
        public string? Type { get; set; }
        public bool IsMutable { get; set; }
    }

    public class LoopStatement : Statement
    {
        public List<Statement> Body { get; set; } = new();
        public Expression? Condition { get; set; } // for until loops
    }

    public class TypeAliasDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public TypeAnnotation Type { get; set; } = null!;
        public List<string> Modifiers { get; set; } = new();
    }

    // Add a type annotation class to represent generic types
    public class TypeAnnotation : ASTNode
    {
        public string Name { get; set; } = "";
        public List<TypeAnnotation> TypeArguments { get; set; } = new();
    }

    public class BlockExpression : Expression
    {
        public List<Statement> Statements { get; set; } = new();
    }

    // Add lambda expression support
    public class LambdaExpression : Expression
    {
        public List<Parameter> Parameters { get; set; } = new();
        public Expression? Body { get; set; } // For expression lambdas
        public List<Statement> Statements { get; set; } = new(); // For block lambdas
        public bool IsAsync { get; set; }
        
        public bool IsExpressionLambda => Body != null;
        public bool IsBlockLambda => Statements.Count > 0;
    }

    // Add new AST nodes for array operations
    public class ArrayIndiceExpression : Expression
    {
        public Expression SourceArray { get; set; } = null!;
        public Expression StartOffset { get; set; } = null!;
        public string? ElementType { get; set; }
    }

    public class ArrayMethodCallExpression : Expression
    {
        public Expression Array { get; set; } = null!;
        public string MethodName { get; set; } = "";
        public List<Expression> Arguments { get; set; } = new();
    }

    public class ArrayCollectExpression : Expression
    {
        public Expression Array { get; set; } = null!;
        public List<Expression> Indices { get; set; } = new();
    }

    public class ArrayAtExpression : Expression
    {
        public Expression Array { get; set; } = null!;
        public Expression Index { get; set; } = null!;
    }

    // Add method call chain for array methods
    public class MethodCallChain : ASTNode
    {
        public string MethodName { get; set; } = "";
        public List<Expression> Arguments { get; set; } = new();
    }
}
