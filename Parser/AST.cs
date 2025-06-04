using Wake.Net.Lexer;

namespace Wake.Net.Parser
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
    }

    public class ArrayExpression : Expression 
    {
        public List<Expression> Elements { get; set; } = new();
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
    }
    
    public class ReturnStatement : Statement 
    {
        public Expression? Value { get; set; }
    }
    
    public class BreakStatement : Statement { }
    
    public class ContinueStatement : Statement { }
    
    public class ClassDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public string? BaseClass { get; set; }
        public List<Statement> Members { get; set; } = new();
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
    
    public class MethodDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public List<Parameter> Parameters { get; set; } = new();
        public List<Statement> Body { get; set; } = new();
        public string? ReturnType { get; set; }
        public bool IsStatic { get; set; }
        public bool IsConstructor { get; set; }
    }
    
    public class PropertyDeclaration : Statement
    {
        public string Name { get; set; } = "";
        public string? Type { get; set; }
        public Expression? Initializer { get; set; }
        public bool IsStatic { get; set; }
    }
    
    // Top-level
    public class FunctionDeclaration : Statement 
    {
        public string Name { get; set; } = "";
        public List<Parameter> Parameters { get; set; } = new();
        public List<Statement> Body { get; set; } = new();
        public string? ReturnType { get; set; }
    }
    
    public class Parameter : ASTNode 
    {
        public string Name { get; set; } = "";
        public string? Type { get; set; }
    }
    
    public class Program : ASTNode 
    {
        public List<Statement> Statements { get; set; } = new();
    }
}
