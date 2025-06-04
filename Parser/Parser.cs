using Wake.Net.Lexer;
using Wake.Net.Diagnostics;

namespace Wake.Net.Parser
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current = 0;
        private readonly DiagnosticsReporter _diagnostics;
        private readonly MethodChecker _methodChecker;

        public Parser(List<Token> tokens, DiagnosticsReporter? diagnostics = null)
        {
            _tokens = tokens;
            _diagnostics = diagnostics ?? new DiagnosticsReporter();
            _methodChecker = new MethodChecker(_diagnostics);
        }

        public Program Parse()
        {
            var statements = new List<Statement>();
            
            _diagnostics.ReportInfo($"Starting parsing of {_tokens.Count} tokens");
            
            // First pass: Register all methods
            RegisterAllMethods();
            
            // Reset current position for actual parsing
            _current = 0;
            
            while (!IsAtEnd())
            {
                try
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                    {
                        statements.Add(stmt);
                    }
                }
                catch (Exception ex)
                {
                    _diagnostics.ReportError($"Parse error: {ex.Message}", Peek().Line, Peek().Column, "UH100", ex);
                    Synchronize();
                }
            }
            
            _methodChecker.PrintMethodSummary();
            _diagnostics.ReportInfo($"Parsing completed. Generated AST with {statements.Count} statements");
            return new Program { Statements = statements };
        }

        private void RegisterAllMethods()
        {
            var tempCurrent = _current;
            _current = 0;
            
            while (!IsAtEnd())
            {
                try
                {
                    if (Check(TokenType.Func))
                    {
                        var funcToken = Advance();
                        var nameToken = Consume(TokenType.Identifier, "Expected function name");
                        
                        // Parse parameters properly
                        var parameters = new List<Parameter>();
                        Consume(TokenType.LeftParen, "Expected '(' after function name");
                        
                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Value;
                                string? paramType = null;
                                
                                if (Match(TokenType.Colon))
                                {
                                    // Handle type tokens properly
                                    if (Check(TokenType.StringType))
                                    {
                                        paramType = "string";
                                        Advance();
                                    }
                                    else if (Check(TokenType.Int))
                                    {
                                        paramType = "int";
                                        Advance();
                                    }
                                    else if (Check(TokenType.Float))
                                    {
                                        paramType = "float";
                                        Advance();
                                    }
                                    else if (Check(TokenType.Bool))
                                    {
                                        paramType = "bool";
                                        Advance();
                                    }
                                    else
                                    {
                                        paramType = Consume(TokenType.Identifier, "Expected parameter type").Value;
                                    }
                                }
                                
                                parameters.Add(new Parameter { Name = paramName, Type = paramType });
                            } while (Match(TokenType.Comma));
                        }
                        
                        Consume(TokenType.RightParen, "Expected ')' after parameters");
                        
                        // Parse return type if present
                        string? returnType = null;
                        if (Match(TokenType.Colon))
                        {
                            if (Check(TokenType.StringType))
                            {
                                returnType = "string";
                                Advance();
                            }
                            else if (Check(TokenType.Int))
                            {
                                returnType = "int";
                                Advance();
                            }
                            else if (Check(TokenType.Float))
                            {
                                returnType = "float";
                                Advance();
                            }
                            else if (Check(TokenType.Bool))
                            {
                                returnType = "bool";
                                Advance();
                            }
                            else
                            {
                                returnType = Consume(TokenType.Identifier, "Expected return type").Value;
                            }
                        }
                        
                        // Skip to function body
                        while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                        {
                            Advance();
                        }
                        
                        // Create function declaration for registration
                        var func = new FunctionDeclaration 
                        { 
                            Name = nameToken.Value, 
                            Parameters = parameters,
                            ReturnType = returnType 
                        };
                        var location = new SourceLocation(nameToken.Line, nameToken.Column);
                        _methodChecker.RegisterMethod(func, location);
                        
                        _diagnostics.ReportInfo($"Registered function: {nameToken.Value} with {parameters.Count} parameters");
                    }
                    else if (Check(TokenType.Class))
                    {
                        Advance(); // Skip 'class'
                        var classNameToken = Consume(TokenType.Identifier, "Expected class name");
                        
                        // Skip to class body
                        while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                        {
                            Advance();
                        }
                        
                        if (Check(TokenType.LeftBrace))
                        {
                            Advance(); // Skip '{'
                            
                            // Register methods in class
                            while (!Check(TokenType.RightBrace) && !IsAtEnd())
                            {
                                if (Check(TokenType.Func))
                                {
                                    Advance(); // Skip 'func'
                                    var methodNameToken = Consume(TokenType.Identifier, "Expected method name");
                                    
                                    var method = new MethodDeclaration { Name = methodNameToken.Value };
                                    var location = new SourceLocation(methodNameToken.Line, methodNameToken.Column);
                                    _methodChecker.RegisterMethod(method, classNameToken.Value, location);
                                }
                                else
                                {
                                    Advance();
                                }
                            }
                        }
                    }
                    else
                    {
                        Advance();
                    }
                }
                catch
                {
                    Advance();
                }
            }
            
            _current = tempCurrent;
        }

        private Statement? ParseStatement()
        {
            try
            {
                if (Match(TokenType.Import)) return ParseImportStatement();
                if (Match(TokenType.Namespace)) return ParseNamespaceDeclaration();
                if (Match(TokenType.Class)) return ParseClassDeclaration();
                if (Match(TokenType.Const)) return ParseConstDeclaration();
                if (Match(TokenType.Var)) return ParseVariableDeclaration();
                if (Match(TokenType.Func)) return ParseFunctionDeclaration();
                if (Match(TokenType.If)) return ParseIfStatement();
                if (Match(TokenType.While)) return ParseWhileStatement();
                if (Match(TokenType.For)) return ParseForStatement();
                if (Match(TokenType.Return)) return ParseReturnStatement();
                if (Match(TokenType.Break)) return ParseBreakStatement();
                if (Match(TokenType.Continue)) return ParseContinueStatement();
                
                return ParseExpressionStatement();
            }
            catch (Exception ex)
            {
                _diagnostics.ReportParseError(ex.Message, Peek());
                Synchronize();
                return null;
            }
        }

        private Statement ParseImportStatement()
        {
            var className = Consume(TokenType.Identifier, "Expected class name after 'import'").Value;
            Consume(TokenType.From, "Expected 'from' after class name");
            var assemblyName = Consume(TokenType.String, "Expected assembly name after 'from'").Value;
            
            return new ImportStatement 
            { 
                ClassName = className, 
                AssemblyName = assemblyName 
            };
        }

        private Statement ParseNamespaceDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected namespace name").Value;
            
            // Handle dotted namespace names
            while (Match(TokenType.Dot))
            {
                name += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' after namespace name");
            var members = new List<Statement>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) members.Add(stmt);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after namespace body");
            
            return new NamespaceDeclaration { Name = name, Members = members };
        }

        private Statement ParseClassDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected class name").Value;
            
            string? baseClass = null;
            if (Match(TokenType.Colon))
            {
                baseClass = Consume(TokenType.Identifier, "Expected base class name").Value;
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' before class body");
            var members = new List<Statement>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var member = ParseClassMember();
                if (member != null) members.Add(member);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after class body");
            
            return new ClassDeclaration { Name = name, BaseClass = baseClass, Members = members };
        }

        private Statement? ParseClassMember()
        {
            if (Match(TokenType.Var)) return ParsePropertyDeclaration();
            if (Match(TokenType.Func)) return ParseMethodDeclaration();
            
            // Skip unknown tokens in class body
            Advance();
            return null;
        }

        private Statement ParsePropertyDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected property name").Value;
            string? type = null;
            Expression? initializer = null;

            if (Match(TokenType.Colon))
            {
                type = Consume(TokenType.Identifier, "Expected property type").Value;
            }

            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }

            return new PropertyDeclaration { Name = name, Type = type, Initializer = initializer };
        }

        private Statement ParseMethodDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected method name").Value;
            bool isConstructor = name == "constructor";
            
            Consume(TokenType.LeftParen, "Expected '(' after method name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name").Value;
                    string? paramType = null;
                    
                    if (Match(TokenType.Colon))
                    {
                        // Handle type tokens properly
                        if (Check(TokenType.StringType))
                        {
                            paramType = "string";
                            Advance();
                        }
                        else if (Check(TokenType.Int))
                        {
                            paramType = "int";
                            Advance();
                        }
                        else if (Check(TokenType.Float))
                        {
                            paramType = "float";
                            Advance();
                        }
                        else if (Check(TokenType.Bool))
                        {
                            paramType = "bool";
                            Advance();
                        }
                        else
                        {
                            paramType = Consume(TokenType.Identifier, "Expected parameter type").Value;
                        }
                    }
                    
                    parameters.Add(new Parameter { Name = paramName, Type = paramType });
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            
            string? returnType = null;
            if (Match(TokenType.Colon))
            {
                // Handle return type tokens properly
                if (Check(TokenType.StringType))
                {
                    returnType = "string";
                    Advance();
                }
                else if (Check(TokenType.Int))
                {
                    returnType = "int";
                    Advance();
                }
                else if (Check(TokenType.Float))
                {
                    returnType = "float";
                    Advance();
                }
                else if (Check(TokenType.Bool))
                {
                    returnType = "bool";
                    Advance();
                }
                else
                {
                    returnType = Consume(TokenType.Identifier, "Expected return type").Value;
                }
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' before method body");
            var body = new List<Statement>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) body.Add(stmt);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after method body");
            
            return new MethodDeclaration 
            { 
                Name = name, 
                Parameters = parameters, 
                Body = body, 
                ReturnType = returnType,
                IsConstructor = isConstructor
            };
        }

        private Statement ParseConstDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected variable name").Value;
            Consume(TokenType.Assign, "Expected '=' after const name");
            var initializer = ParseExpression();
            return new VariableDeclaration { Name = name, Initializer = initializer, IsConstant = true };
        }

        private Statement ParseVariableDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected variable name").Value;
            string? type = null;
            Expression? initializer = null;

            if (Match(TokenType.Colon))
            {
                type = Consume(TokenType.Identifier, "Expected type name").Value;
            }

            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }

            return new VariableDeclaration { Name = name, Initializer = initializer, Type = type };
        }

        private Statement ParseFunctionDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected function name").Value;
            
            Consume(TokenType.LeftParen, "Expected '(' after function name");
            var parameters = new List<Parameter>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    var paramName = Consume(TokenType.Identifier, "Expected parameter name").Value;
                    string? paramType = null;
                    
                    if (Match(TokenType.Colon))
                    {
                        // Handle type tokens properly
                        if (Check(TokenType.StringType))
                        {
                            paramType = "string";
                            Advance();
                        }
                        else if (Check(TokenType.Int))
                        {
                            paramType = "int";
                            Advance();
                        }
                        else if (Check(TokenType.Float))
                        {
                            paramType = "float";
                            Advance();
                        }
                        else if (Check(TokenType.Bool))
                        {
                            paramType = "bool";
                            Advance();
                        }
                        else
                        {
                            paramType = Consume(TokenType.Identifier, "Expected parameter type").Value;
                        }
                    }
                    
                    parameters.Add(new Parameter { Name = paramName, Type = paramType });
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after parameters");
            
            string? returnType = null;
            if (Match(TokenType.Colon))
            {
                // Handle return type tokens properly
                if (Check(TokenType.StringType))
                {
                    returnType = "string";
                    Advance();
                }
                else if (Check(TokenType.Int))
                {
                    returnType = "int";
                    Advance();
                }
                else if (Check(TokenType.Float))
                {
                    returnType = "float";
                    Advance();
                }
                else if (Check(TokenType.Bool))
                {
                    returnType = "bool";
                    Advance();
                }
                else
                {
                    returnType = Consume(TokenType.Identifier, "Expected return type").Value;
                }
            }
            
            Consume(TokenType.LeftBrace, "Expected '{' before function body");
            var body = new List<Statement>();
            
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) body.Add(stmt);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after function body");
            
            return new FunctionDeclaration 
            { 
                Name = name, 
                Parameters = parameters, 
                Body = body, 
                ReturnType = returnType 
            };
        }

        private Statement ParseIfStatement()
        {
            var condition = ParseExpression();
            Consume(TokenType.LeftBrace, "Expected '{' after if condition");
            
            var thenBranch = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) thenBranch.Add(stmt);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after if body");
            
            List<Statement>? elseBranch = null;
            if (Match(TokenType.Else))
            {
                // Handle "else if" as a nested if statement
                if (Check(TokenType.If))
                {
                    var elseIfStmt = ParseStatement(); // This will parse the "if" statement
                    elseBranch = new List<Statement> { elseIfStmt };
                }
                else
                {
                    Consume(TokenType.LeftBrace, "Expected '{' after else");
                    elseBranch = new List<Statement>();
                    
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        var stmt = ParseStatement();
                        if (stmt != null) elseBranch.Add(stmt);
                    }
                    
                    Consume(TokenType.RightBrace, "Expected '}' after else body");
                }
            }
            
            return new IfStatement { Condition = condition, ThenBranch = thenBranch, ElseBranch = elseBranch };
        }

        private Statement ParseWhileStatement()
        {
            var condition = ParseExpression();
            Consume(TokenType.LeftBrace, "Expected '{' after while condition");
            
            var body = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) body.Add(stmt);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after while body");
            
            return new WhileStatement { Condition = condition, Body = body };
        }

        private Statement ParseForStatement()
        {
            // Simplified for loop parsing
            var init = ParseExpressionStatement();
            Consume(TokenType.Semicolon, "Expected ';' after for loop initializer");
            var condition = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after for loop condition");
            var update = ParseExpressionStatement();
            
            Consume(TokenType.LeftBrace, "Expected '{' after for loop header");
            
            var body = new List<Statement>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var stmt = ParseStatement();
                if (stmt != null) body.Add(stmt);
            }
            
            Consume(TokenType.RightBrace, "Expected '}' after for loop body");
            
            return new ForStatement 
            { 
                Initializer = init, 
                Condition = condition, 
                Increment = update, 
                Body = body 
            };
        }

        private Statement ParseReturnStatement()
        {
            Expression? value = null;
            if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
            {
                value = ParseExpression();
            }
            return new ReturnStatement { Value = value };
        }

        private Statement ParseBreakStatement()
        {
            return new BreakStatement();
        }

        private Statement ParseContinueStatement()
        {
            return new ContinueStatement();
        }

        private Statement ParseExpressionStatement()
        {
            var expr = ParseExpression();
            // Don't require semicolons for now, just consume them if present
            if (Check(TokenType.Semicolon))
            {
                Advance();
            }
            return new ExpressionStatement { Expression = expr };
        }

        private Expression ParseExpression()
        {
            return ParseAssignment();
        }

        private Expression ParseAssignment()
        {
            var expr = ParseOr();
            
            if (Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign, 
                     TokenType.MultiplyAssign, TokenType.DivideAssign))
            {
                var op = Previous().Type;
                var value = ParseAssignment();
                return new AssignmentExpression { Target = expr, Operator = op, Value = value };
            }
            
            return expr;
        }

        private Expression ParseOr()
        {
            var expr = ParseAnd();
            
            while (Match(TokenType.Or))
            {
                var op = Previous().Type;
                var right = ParseAnd();
                expr = new BinaryExpression { Left = expr, Operator = op, Right = right };
            }
            
            return expr;
        }

        private Expression ParseAnd()
        {
            var expr = ParseEquality();
            
            while (Match(TokenType.And))
            {
                var op = Previous().Type;
                var right = ParseEquality();
                expr = new BinaryExpression { Left = expr, Operator = op, Right = right };
            }
            
            return expr;
        }

        private Expression ParseEquality()
        {
            var expr = ParseComparison();
            
            while (Match(TokenType.Equal, TokenType.NotEqual))
            {
                var op = Previous().Type;
                var right = ParseComparison();
                expr = new BinaryExpression { Left = expr, Operator = op, Right = right };
            }
            
            return expr;
        }

        private Expression ParseComparison()
        {
            var expr = ParseTerm();
            
            while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
            {
                var op = Previous().Type;
                var right = ParseTerm();
                expr = new BinaryExpression { Left = expr, Operator = op, Right = right };
            }
            
            return expr;
        }

        private Expression ParseTerm()
        {
            var expr = ParseFactor();
            
            while (Match(TokenType.Minus, TokenType.Plus))
            {
                var op = Previous().Type;
                var right = ParseFactor();
                expr = new BinaryExpression { Left = expr, Operator = op, Right = right };
            }
            
            return expr;
        }

        private Expression ParseFactor()
        {
            var expr = ParseUnary();
            
            while (Match(TokenType.Divide, TokenType.Multiply, TokenType.Modulo))
            {
                var op = Previous().Type;
                var right = ParseUnary();
                expr = new BinaryExpression { Left = expr, Operator = op, Right = right };
            }
            
            return expr;
        }

        private Expression ParseUnary()
        {
            if (Match(TokenType.Not, TokenType.Minus))
            {
                var op = Previous().Type;
                var right = ParseUnary();
                return new UnaryExpression { Operator = op, Operand = right };
            }

            // Handle prefix increment/decrement
            if (Match(TokenType.Increment, TokenType.Decrement))
            {
                var op = Previous().Type;
                var operand = ParseUnary();
                return new UnaryExpression { Operator = op, Operand = operand };
            }
            
            return ParsePostfix();
        }

        private Expression ParsePostfix()
        {
            var expr = ParseCall();
            
            // Handle postfix increment/decrement
            if (Match(TokenType.Increment, TokenType.Decrement))
            {
                var op = Previous().Type;
                return new UnaryExpression { Operator = op, Operand = expr };
            }
            
            return expr;
        }

        private Expression ParseCall()
        {
            var expr = ParsePrimary();
            
            while (true)
            {
                if (Match(TokenType.LeftParen))
                {
                    expr = FinishCall(expr);
                }
                else if (Match(TokenType.LeftBracket))
                {
                    var index = ParseExpression();
                    Consume(TokenType.RightBracket, "Expected ']' after array index");
                    expr = new IndexExpression { Object = expr, Index = index };
                }
                else if (Match(TokenType.Dot))
                {
                    var name = Consume(TokenType.Identifier, "Expected property name after '.'").Value;
                    expr = new MemberAccessExpression { Object = expr, MemberName = name };
                }
                else
                {
                    break;
                }
            }
            
            return expr;
        }

        private Expression FinishCall(Expression callee)
        {
            var arguments = new List<Expression>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (Match(TokenType.Comma));
            }
            
            var rightParen = Consume(TokenType.RightParen, "Expected ')' after arguments");
            
            // Validate method call
            if (callee is IdentifierExpression identExpr)
            {
                _methodChecker.ValidateCall(identExpr.Name, arguments, rightParen);
            }
            else if (callee is MemberAccessExpression memberExpr && 
                     memberExpr.Object is IdentifierExpression objIdent)
            {
                _methodChecker.ValidateMethodCall(objIdent.Name, memberExpr.MemberName, arguments, rightParen);
            }
            
            return new CallExpression { Function = callee, Arguments = arguments };
        }

        private Expression ParsePrimary()
        {
            if (Match(TokenType.This))
                return new ThisExpression();
                
            if (Match(TokenType.True))
                return new LiteralExpression { Value = true, Type = TokenType.Boolean };
            
            if (Match(TokenType.False))
                return new LiteralExpression { Value = false, Type = TokenType.Boolean };
            
            if (Match(TokenType.Number))
            {
                var value = Previous().Value;
                try
                {
                    return new LiteralExpression 
                    { 
                        Value = value.Contains('.') ? double.Parse(value) : int.Parse(value), 
                        Type = TokenType.Number 
                    };
                }
                catch (FormatException)
                {
                    _diagnostics.ReportInvalidNumber(value, Previous().Line, Previous().Column);
                    return new LiteralExpression { Value = 0, Type = TokenType.Number };
                }
            }
            
            if (Match(TokenType.String))
                return new LiteralExpression { Value = Previous().Value, Type = TokenType.String };
            
            if (Match(TokenType.Identifier))
                return new IdentifierExpression { Name = Previous().Value };
            
            if (Match(TokenType.LeftParen))
            {
                var expr = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after expression");
                return expr;
            }
            
            if (Match(TokenType.LeftBracket))
            {
                var elements = new List<Expression>();
                
                if (!Check(TokenType.RightBracket))
                {
                    do
                    {
                        elements.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightBracket, "Expected ']' after array elements");
                return new ArrayExpression { Elements = elements };
            }
            
            _diagnostics.ReportUnexpectedToken(Peek(), "expression");
            throw new Exception($"Unexpected token {Peek().Type}");
        }

        private string ConvertTokenTypeToString(TokenType tokenType)
        {
            return tokenType switch
            {
                TokenType.Plus => "+",
                TokenType.Minus => "-",
                TokenType.Multiply => "*",
                TokenType.Divide => "/",
                TokenType.Modulo => "%",
                TokenType.Assign => "=",
                TokenType.PlusAssign => "+=",
                TokenType.MinusAssign => "-=",
                TokenType.MultiplyAssign => "*=",
                TokenType.DivideAssign => "/=",
                TokenType.Equal => "==",
                TokenType.NotEqual => "!=",
                TokenType.Less => "<",
                TokenType.Greater => ">",
                TokenType.LessEqual => "<=",
                TokenType.GreaterEqual => ">=",
                TokenType.And => "&&",
                TokenType.Or => "||",
                TokenType.Not => "!",
                _ => tokenType.ToString()
            };
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }

        private Token Peek()
        {
            return _tokens[_current];
        }

        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            
            _diagnostics.ReportUnexpectedToken(Peek(), message);
            throw new Exception($"{message}. Got {Peek().Type}");
        }

        private void Synchronize()
        {
            Advance();
            
            while (!IsAtEnd())
            {
                if (Previous().Type == TokenType.Semicolon) return;
                
                switch (Peek().Type)
                {
                    case TokenType.Func:
                    case TokenType.Var:
                    case TokenType.Const:
                    case TokenType.For:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.Return:
                    case TokenType.LeftBrace:
                    case TokenType.RightBrace:
                        return;
                }
                
                Advance();
            }
        }
    }
}
