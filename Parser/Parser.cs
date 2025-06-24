using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;
using System.Text;

namespace uhigh.Net.Parser
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
                catch (ParseException)
                {
                    // ParseException already reported the error, just synchronize
                    Synchronize();
                }
                catch (Exception ex)
                {
                    // For unexpected exceptions, report them
                    _diagnostics.ReportError($"Unexpected parse error: {ex.Message}", Peek().Line, Peek().Column, "UH100", ex);
                    Synchronize();
                }
            }

            _methodChecker.PrintMethodSummary();
            _diagnostics.ReportInfo($"Parsing completed. Generated AST with {statements.Count} statements");
            return new Program { Statements = statements };
        }

        public DiagnosticsReporter GetDiagnostics()
        {
            return _diagnostics;
        }


        private void RegisterAllMethods()
        {
            var tempCurrent = _current;
            _current = 0;

            while (!IsAtEnd())
            {
                try
                {
                    // Check for attributes first
                    var attributes = new List<AttributeDeclaration>();
                    while (Check(TokenType.LeftBracket))
                    {
                        attributes.Add(ParseAttribute());
                    }

                    // Check for external attribute - skip registration if found
                    var hasExternalAttribute = attributes.Any(attr => attr.IsExternal);

                    if (Check(TokenType.Func))
                    {
                        var funcToken = Advance();
                        var nameToken = Consume(TokenType.Identifier, "Expected function name");

                        // Handle dotted function names (e.g., Console.WriteLine)
                        var functionName = nameToken.Value;
                        while (Match(TokenType.Dot))
                        {
                            functionName += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
                        }

                        // Check if this has dotnetfunc or external attribute - skip registration if it does
                        var hasDotNetFuncAttribute = attributes.Any(attr => attr.IsDotNetFunc);

                        if (hasDotNetFuncAttribute || hasExternalAttribute)
                        {
                            _diagnostics.ReportInfo($"Skipping registration for external/dotnet function: {functionName}");
                            // Skip to end of function declaration
                            while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                            {
                                Advance();
                            }
                            if (Check(TokenType.LeftBrace))
                            {
                                Advance(); // Skip opening brace
                                // Skip function body
                                var braceCount = 1;
                                while (braceCount > 0 && !IsAtEnd())
                                {
                                    if (Check(TokenType.LeftBrace)) braceCount++;
                                    else if (Check(TokenType.RightBrace)) braceCount--;
                                    Advance();
                                }
                            }
                            continue;
                        }

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
                            Name = functionName,
                            Parameters = parameters,
                            ReturnType = returnType,
                            Attributes = attributes
                        };
                        var location = new SourceLocation(nameToken.Line, nameToken.Column);
                        _methodChecker.RegisterMethod(func, location);

                        _diagnostics.ReportInfo($"Registered function: {functionName} with {parameters.Count} parameters");
                    }
                    else if (Check(TokenType.Class))
                    {
                        Advance(); // Skip 'class'

                        // Parse modifiers before class name
                        var modifiers = new List<string>();
                        while (IsModifierToken(Previous()))
                        {
                            modifiers.Add(Previous().Value);
                            if (!IsAtEnd()) Advance();
                        }

                        var classNameToken = Consume(TokenType.Identifier, "Expected class name");

                        // Skip external classes
                        if (hasExternalAttribute)
                        {
                            _diagnostics.ReportInfo($"Skipping registration for external class: {classNameToken.Value}");
                            // Skip to end of class declaration
                            while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                            {
                                Advance();
                            }
                            if (Check(TokenType.LeftBrace))
                            {
                                Advance(); // Skip opening brace
                                var braceCount = 1;
                                while (braceCount > 0 && !IsAtEnd())
                                {
                                    if (Check(TokenType.LeftBrace)) braceCount++;
                                    else if (Check(TokenType.RightBrace)) braceCount--;
                                    Advance();
                                }
                            }
                            continue;
                        }

                        // Create a temporary class declaration for registration
                        var tempClassDecl = new ClassDeclaration
                        {
                            Name = classNameToken.Value,
                            Modifiers = modifiers,
                            Members = new List<Statement>()
                        };

                        var location = new SourceLocation(classNameToken.Line, classNameToken.Column);
                        _methodChecker.RegisterClass(tempClassDecl, location);

                        // Skip to class body and register methods
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
                                    var methodLocation = new SourceLocation(methodNameToken.Line, methodNameToken.Column);
                                    _methodChecker.RegisterMethod(method, classNameToken.Value, methodLocation);
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
                // Check for attributes
                var attributes = new List<AttributeDeclaration>();
                while (Check(TokenType.LeftBracket))
                {
                    attributes.Add(ParseAttribute());
                }

                // Parse modifiers for top-level declarations
                var modifiers = new List<string>();
                while (IsModifierToken(Peek()))
                {
                    modifiers.Add(Advance().Value);
                }

                if (Match(TokenType.Include)) return ParseIncludeStatement();
                if (Match(TokenType.Import)) return ParseImportStatement();
                if (Match(TokenType.Namespace)) return ParseNamespaceDeclaration();
                if (Match(TokenType.Enum)) return ParseEnumDeclaration(modifiers);
                if (Match(TokenType.Interface)) return ParseInterfaceDeclaration(modifiers);
                if (Match(TokenType.Class))
                {
                    var classDecl = ParseClassDeclaration() as ClassDeclaration;
                    if (classDecl != null)
                    {
                        classDecl.Modifiers.AddRange(modifiers);
                    }
                    return classDecl;
                }
                if (Match(TokenType.Const)) return ParseConstDeclaration();
                if (Match(TokenType.Var)) return ParseVariableDeclaration();
                if (Match(TokenType.Func))
                {
                    var funcDecl = ParseFunctionDeclaration() as FunctionDeclaration;
                    if (funcDecl != null)
                    {
                        funcDecl.Attributes = attributes;
                        funcDecl.Modifiers = modifiers;
                    }
                    return funcDecl;
                }
                if (Match(TokenType.If)) return ParseIfStatement();
                if (Match(TokenType.While)) return ParseWhileStatement();
                if (Match(TokenType.For)) return ParseForStatement();
                if (Match(TokenType.Return)) return ParseReturnStatement();
                if (Match(TokenType.Break)) return ParseBreakStatement();
                if (Match(TokenType.Continue)) return ParseContinueStatement();
                if (Match(TokenType.Sharp)) return ParseSharpBlock();

                // Support top-level match statement
                if (Match(TokenType.Match)) return ParseMatchStatement();


                // if (Match(TokenType.Switch)) return ParseSwitchStatement();
                // if (Match(TokenType.Struct)) return ParseStructDeclaration();
                // if (Match(TokenType.Module)) return ParseModuleDeclaration();
                // if (Match(TokenType.Let)) return ParseLetDeclaration();
                // if (Match(TokenType.Loop)) return ParseLoopStatement();

                return ParseExpressionStatement();
            }
            catch (ParseException)
            {
                // ParseException already reported the error, just synchronize
                Synchronize();
                return null;
            }
            catch (Exception ex)
            {
                // For unexpected exceptions, report them
                _diagnostics.ReportError($"Unexpected parse error: {ex.Message}", Peek().Line, Peek().Column, "UH100", ex);
                Synchronize();
                return null;
            }
        }

        private Statement ParseEnumDeclaration(List<string> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected enum name").Value;

            string? baseType = null;
            if (Match(TokenType.Colon))
            {
                baseType = Consume(TokenType.Identifier, "Expected enum base type").Value;
            }

            Consume(TokenType.LeftBrace, "Expected '{' before enum body");
            var members = new List<EnumMember>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var memberName = Consume(TokenType.Identifier, "Expected enum member name").Value;
                Expression? memberValue = null;

                if (Match(TokenType.Assign))
                {
                    memberValue = ParseExpression();
                }

                members.Add(new EnumMember { Name = memberName, Value = memberValue });

                if (!Check(TokenType.RightBrace))
                {
                    Consume(TokenType.Comma, "Expected ',' after enum member");
                }
            }

            Consume(TokenType.RightBrace, "Expected '}' after enum body");

            return new EnumDeclaration
            {
                Name = name,
                BaseType = baseType,
                Members = members,
                Modifiers = modifiers
            };
        }

        private Statement ParseInterfaceDeclaration(List<string> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected interface name").Value;

            var baseInterfaces = new List<string>();
            if (Match(TokenType.Colon))
            {
                do
                {
                    baseInterfaces.Add(Consume(TokenType.Identifier, "Expected interface name").Value);
                } while (Match(TokenType.Comma));
            }

            Consume(TokenType.LeftBrace, "Expected '{' before interface body");
            var members = new List<Statement>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var member = ParseInterfaceMember();
                if (member != null) members.Add(member);
            }

            Consume(TokenType.RightBrace, "Expected '}' after interface body");

            return new InterfaceDeclaration
            {
                Name = name,
                BaseInterfaces = baseInterfaces,
                Members = members,
                Modifiers = modifiers
            };
        }

        private Statement? ParseInterfaceMember()
        {
            if (Match(TokenType.Func))
            {
                // Interface method signature (no body)
                var name = Consume(TokenType.Identifier, "Expected method name").Value;

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
                            paramType = ParseTypeName();
                        }

                        parameters.Add(new Parameter { Name = paramName, Type = paramType });
                    } while (Match(TokenType.Comma));
                }

                Consume(TokenType.RightParen, "Expected ')' after parameters");

                string? returnType = null;
                if (Match(TokenType.Colon))
                {
                    returnType = ParseTypeName();
                }

                return new MethodDeclaration
                {
                    Name = name,
                    Parameters = parameters,
                    ReturnType = returnType,
                    Body = new List<Statement>() // Empty body for interface
                };
            }

            if (Match(TokenType.Var))
            {
                // Interface property
                return ParsePropertyDeclaration();
            }

            if (!IsAtEnd())
            {
                _diagnostics.ReportParseError("Expected interface member", Peek());
                Advance();
            }

            return null;
        }

        private string ParseTypeName()
        {
            var typeName = "";

            if (Check(TokenType.StringType))
            {
                typeName = "string";
                Advance();
            }
            else if (Check(TokenType.Int))
            {
                typeName = "int";
                Advance();
            }
            else if (Check(TokenType.Float))
            {
                typeName = "float";
                Advance();
            }
            else if (Check(TokenType.Bool))
            {
                typeName = "bool";
                Advance();
            }
            else
            {
                typeName = Consume(TokenType.Identifier, "Expected type name").Value;
            }

            // Handle nullable types
            if (Match(TokenType.Question))
            {
                typeName += "?";
            }

            return typeName;
        }

        private Statement ParseFieldDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected field name").Value;
            string? type = null;
            Expression? initializer = null;

            if (Match(TokenType.Colon))
            {
                // Handle type tokens properly
                if (Check(TokenType.StringType))
                {
                    type = "string";
                    Advance();
                }
                else if (Check(TokenType.Int))
                {
                    type = "int";
                    Advance();
                }
                else if (Check(TokenType.Float))
                {
                    type = "float";
                    Advance();
                }
                else if (Check(TokenType.Bool))
                {
                    type = "bool";
                    Advance();
                }
                else
                {
                    type = Consume(TokenType.Identifier, "Expected field type").Value;
                }
            }

            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }

            return new FieldDeclaration { Name = name, Type = type, Initializer = initializer };
        }

        private Statement ParsePropertyDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected property name").Value;
            string? type = null;
            Expression? initializer = null;
            var accessors = new List<PropertyAccessor>();

            if (Match(TokenType.Colon))
            {
                // Handle type tokens properly
                if (Check(TokenType.StringType))
                {
                    type = "string";
                    Advance();
                }
                else if (Check(TokenType.Int))
                {
                    type = "int";
                    Advance();
                }
                else if (Check(TokenType.Float))
                {
                    type = "float";
                    Advance();
                }
                else if (Check(TokenType.Bool))
                {
                    type = "bool";
                    Advance();
                }
                else
                {
                    type = Consume(TokenType.Identifier, "Expected property type").Value;
                }
            }

            // Check for property accessors { get; set; } or { get = ...; set = ...; }
            if (Match(TokenType.LeftBrace))
            {
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    if (Match(TokenType.Get))
                    {
                        var getter = new PropertyAccessor { Type = "get" };

                        if (Match(TokenType.Assign))
                        {
                            // Custom getter: get = expression
                            getter.Body = ParseExpression();
                        }
                        else if (Match(TokenType.LeftBrace))
                        {
                            // Block getter: get { ... }
                            while (!Check(TokenType.RightBrace) && !IsAtEnd())
                            {
                                var stmt = ParseStatement();
                                if (stmt != null) getter.Statements.Add(stmt);
                            }
                            Consume(TokenType.RightBrace, "Expected '}' after getter body");
                        }
                        // else auto-implemented getter

                        accessors.Add(getter);
                    }
                    else if (Match(TokenType.Set))
                    {
                        var setter = new PropertyAccessor { Type = "set" };

                        if (Match(TokenType.Assign))
                        {
                            // Custom setter: set = expression
                            setter.Body = ParseExpression();
                        }
                        else if (Match(TokenType.LeftBrace))
                        {
                            // Block setter: set { ... }
                            while (!Check(TokenType.RightBrace) && !IsAtEnd())
                            {
                                var stmt = ParseStatement();
                                if (stmt != null) setter.Statements.Add(stmt);
                            }
                            Consume(TokenType.RightBrace, "Expected '}' after setter body");
                        }
                        // else auto-implemented setter

                        accessors.Add(setter);
                    }
                    else
                    {
                        _diagnostics.ReportParseError("Expected 'get' or 'set' in property accessor", Peek());
                        break;
                    }

                    // Optional semicolon after accessor
                    if (Check(TokenType.Semicolon))
                    {
                        Advance();
                    }
                }

                Consume(TokenType.RightBrace, "Expected '}' after property accessors");
            }
            else if (Match(TokenType.Assign))
            {
                // Traditional property with initializer
                initializer = ParseExpression();
            }

            return new PropertyDeclaration
            {
                Name = name,
                Type = type,
                Initializer = initializer,
                Accessors = accessors,
                //Modifiers = new List<string>()
            };
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
            var nameToken = Consume(TokenType.Identifier, "Expected function name");
            var functionName = nameToken.Value;

            // Handle qualified function names (e.g., Console.WriteLine)
            while (Match(TokenType.Dot))
            {
                functionName += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
            }

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
                Name = functionName,
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

        private Statement ParseSharpBlock()
        {
            Consume(TokenType.LeftBrace, "Expected '{' after 'sharp'");

            var code = new StringBuilder();
            var braceCount = 1;

            while (!IsAtEnd() && braceCount > 0)
            {
                var token = Advance();

                if (token.Type == TokenType.LeftBrace)
                {
                    braceCount++;
                    code.Append("{");
                }
                else if (token.Type == TokenType.RightBrace)
                {
                    braceCount--;
                    if (braceCount > 0)
                    {
                        code.Append("}");
                    }
                }
                else if (token.Type == TokenType.EOF)
                {
                    _diagnostics.ReportError("Unterminated sharp block", token.Line, token.Column, "UH008");
                    break;
                }
                else
                {
                    // Reconstruct the original text from tokens
                    code.Append(token.Value);

                    // Add space after most tokens for readability
                    if (token.Type != TokenType.Dot &&
                        token.Type != TokenType.LeftParen &&
                        token.Type != TokenType.LeftBracket &&
                        !IsAtEnd() && Peek().Type != TokenType.Dot &&
                        Peek().Type != TokenType.RightParen &&
                        Peek().Type != TokenType.RightBracket &&
                        Peek().Type != TokenType.Semicolon &&
                        Peek().Type != TokenType.Comma)
                    {
                        code.Append(" ");
                    }
                }
            }

            return new SharpBlock { Code = code.ToString().Trim() };
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

            // Check if this is a constructor call (capitalized identifier without dots)
            if (callee is IdentifierExpression identExpr &&
                char.IsUpper(identExpr.Name[0]) &&
                !identExpr.Name.Contains('.'))
            {
                // This is a constructor call - convert to ConstructorCallExpression
                _methodChecker.ValidateConstructorCall(identExpr.Name, arguments, rightParen);
                return new ConstructorCallExpression
                {
                    ClassName = identExpr.Name,
                    Arguments = arguments
                };
            }

            // Validate regular method calls
            if (callee is IdentifierExpression identExpr2)
            {
                _methodChecker.ValidateCall(identExpr2.Name, arguments, rightParen);
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
                    if (value.Contains('.'))
                        return new LiteralExpression { Value = double.Parse(value), Type = TokenType.Number };
                    else
                        return new LiteralExpression { Value = int.Parse(value), Type = TokenType.Number };
                }
                catch (FormatException)
                {
                    _diagnostics.ReportError($"Invalid number format: {value}", Previous().Line, Previous().Column, "UH006");
                    return new LiteralExpression { Value = 0, Type = TokenType.Number };
                }
            }

            if (Match(TokenType.String))
                return new LiteralExpression { Value = Previous().Value, Type = TokenType.String };

            // Handle interpolated strings
            if (Match(TokenType.InterpolatedStringStart))
            {
                return ParseInterpolatedString();
            }

            // Handle 'new' keyword for constructor calls
            if (Match(TokenType.New))
            {
                var className = Consume(TokenType.Identifier, "Expected class name after 'new'").Value;

                if (!Check(TokenType.LeftParen))
                {
                    _diagnostics.ReportError("Expected '(' after class name in constructor call", Peek().Line, Peek().Column, "UH007");
                    return new ConstructorCallExpression
                    {
                        ClassName = className,
                        Arguments = new List<Expression>()
                    };
                }

                Consume(TokenType.LeftParen, "Expected '(' after class name");

                var arguments = new List<Expression>();
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        arguments.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }

                var rightParen = Consume(TokenType.RightParen, "Expected ')' after constructor arguments");

                _methodChecker.ValidateConstructorCall(className, arguments, rightParen);

                return new ConstructorCallExpression
                {
                    ClassName = className,
                    Arguments = arguments
                };
            }

            if (Match(TokenType.Identifier))
            {
                var identifier = Previous().Value;

                if (identifier.Contains('.'))
                {
                    return new QualifiedIdentifierExpression { Name = identifier };
                }

                return new IdentifierExpression { Name = identifier };
            }

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

        private Expression ParseMatchExpression(Expression? value = null)
        {
            // If value is null, parse it (for legacy/fallback)
            if (value == null)
                value = ParseExpression();

            Consume(TokenType.LeftBrace, "Expected '{' after match value");

            var arms = new List<MatchArm>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                arms.Add(ParseMatchArm());

                // Optional comma after each arm
                if (Check(TokenType.Comma))
                {
                    Advance();
                }
            }

            Consume(TokenType.RightBrace, "Expected '}' after match arms");

            return new MatchExpression
            {
                Value = value,
                Arms = arms
            };
        }

        private MatchArm ParseMatchArm()
        {
            var patterns = new List<Expression>();
            bool isDefault = false;

            if (Match(TokenType.Underscore))
            {
                isDefault = true;
            }
            else
            {
                // Parse pattern(s) - support comma-separated patterns
                do
                {
                    patterns.Add(ParseExpression());
                } while (Match(TokenType.Comma) && !Check(TokenType.Arrow));
            }

            Consume(TokenType.Arrow, "Expected '=>' after match pattern");
            var result = ParseExpression();

            return new MatchArm
            {
                Patterns = patterns,
                Result = result,
                IsDefault = isDefault
            };
        }

        private Expression ParseInterpolatedString()
        {
            var parts = new List<InterpolationPart>();
            var currentText = "";

            // This is a simplified version - a full implementation would need
            // more sophisticated tokenization for interpolated strings
            var stringValue = Previous().Value;

            // Parse the interpolated string format $"text{expr}text"
            // For now, convert to string concatenation
            return new LiteralExpression { Value = stringValue, Type = TokenType.String };
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

            // Only report the error once here
            _diagnostics.ReportUnexpectedToken(Peek(), message);
            throw new ParseException($"{message}. Got {Peek().Type}");
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

        private bool IsModifierToken(Token token)
        {
            return token.Type switch
            {
                TokenType.Public or TokenType.Private or TokenType.Protected or TokenType.Internal or
                TokenType.Static or TokenType.Abstract or TokenType.Virtual or TokenType.Override or
                TokenType.Sealed or TokenType.Readonly or TokenType.Async => true,
                _ => false
            };
        }

        private AttributeDeclaration ParseAttribute()
        {
            Consume(TokenType.LeftBracket, "Expected '['");
            var name = Consume(TokenType.Identifier, "Expected attribute name").Value;

            var arguments = new List<Expression>();
            if (Match(TokenType.LeftParen))
            {
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        arguments.Add(ParseExpression());
                    } while (Match(TokenType.Comma));
                }
                Consume(TokenType.RightParen, "Expected ')' after attribute arguments");
            }

            Consume(TokenType.RightBracket, "Expected ']'");

            return new AttributeDeclaration
            {
                Name = name,
                Arguments = arguments
            };
        }

        private Statement ParseImportStatement()
        {
            var assemblyName = Consume(TokenType.String, "Expected assembly name").Value;
            var className = "";

            if (Match(TokenType.As))
            {
                className = Consume(TokenType.Identifier, "Expected class name after 'as'").Value;
            }
            else
            {
                // Extract class name from assembly path
                className = assemblyName;
                if (assemblyName.Contains("/") || assemblyName.Contains("\\"))
                {
                    var lastSlash = Math.Max(assemblyName.LastIndexOf('/'), assemblyName.LastIndexOf('\\'));
                    className = assemblyName.Substring(lastSlash + 1);
                }
                if (className.EndsWith(".dll"))
                {
                    className = className.Substring(0, className.Length - 4);
                }
            }

            return new ImportStatement
            {
                AssemblyName = assemblyName,
                ClassName = className
            };
        }

        private Statement ParseNamespaceDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected namespace name").Value;

            // Handle qualified namespace names (e.g., System.Collections)
            while (Match(TokenType.Dot))
            {
                name += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
            }

            Consume(TokenType.LeftBrace, "Expected '{' after namespace name");
            var members = new List<Statement>();

            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                var member = ParseStatement();
                if (member != null) members.Add(member);
            }

            Consume(TokenType.RightBrace, "Expected '}' after namespace body");

            return new NamespaceDeclaration
            {
                Name = name,
                Members = members
            };
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

            return new ClassDeclaration
            {
                Name = name,
                BaseClass = baseClass,
                Members = members,
                Modifiers = new List<string>()
            };
        }

        private Statement? ParseClassMember()
        {
            // Check for attributes first
            var attributes = new List<AttributeDeclaration>();
            while (Check(TokenType.LeftBracket))
            {
                attributes.Add(ParseAttribute());
            }

            // Parse modifiers
            var modifiers = new List<string>();
            while (IsModifierToken(Peek()))
            {
                modifiers.Add(Advance().Value);
            }

            if (Match(TokenType.Func))
            {
                var methodDecl = ParseMethodDeclaration() as MethodDeclaration;
                if (methodDecl != null)
                {
                    methodDecl.Attributes = attributes;
                    methodDecl.Modifiers = modifiers;
                }
                return methodDecl;
            }

            if (Match(TokenType.Var))
            {
                // Could be field or property
                var propDecl = ParsePropertyDeclaration() as PropertyDeclaration;
                return propDecl;
            }

            if (Match(TokenType.Field))
            {
                var fieldDecl = ParseFieldDeclaration() as FieldDeclaration;
                if (fieldDecl != null)
                {
                    fieldDecl.Modifiers = modifiers;
                }
                return fieldDecl;
            }

            // If we have modifiers but no specific keyword, this might be a field declaration
            if (modifiers.Count > 0 && Check(TokenType.Identifier))
            {
                // This is likely a field declaration without the 'field' keyword
                // e.g., "private field name: string"
                // Check if the next token after identifier is 'field'
                var nameToken = Advance(); // consume identifier

                if (Match(TokenType.Field))
                {
                    // This is "private field name: string" pattern
                    var fieldName = Consume(TokenType.Identifier, "Expected field name after 'field'").Value;
                    string? type = null;
                    Expression? initializer = null;

                    if (Match(TokenType.Colon))
                    {
                        type = ParseTypeName();
                    }

                    if (Match(TokenType.Assign))
                    {
                        initializer = ParseExpression();
                    }

                    return new FieldDeclaration
                    {
                        Name = fieldName,
                        Type = type,
                        Initializer = initializer,
                        Modifiers = modifiers
                    };
                }
                else
                {
                    // Put back the identifier token and parse as normal field
                    _current--; // Back up to re-parse the identifier

                    var fieldName = Consume(TokenType.Identifier, "Expected field name").Value;
                    string? type = null;
                    Expression? initializer = null;

                    if (Match(TokenType.Colon))
                    {
                        type = ParseTypeName();
                    }

                    if (Match(TokenType.Assign))
                    {
                        initializer = ParseExpression();
                    }

                    return new FieldDeclaration
                    {
                        Name = fieldName,
                        Type = type,
                        Initializer = initializer,
                        Modifiers = modifiers
                    };
                }
            }

            if (!IsAtEnd())
            {
                _diagnostics.ReportParseError("Expected class member", Peek());
                Advance(); // Skip unknown token
            }

            return null;
        }

        private Statement ParseMatchStatement()
        {
            // Parse: match <value> { ... }
            var value = ParseExpression();
            Consume(TokenType.LeftBrace, "Expected '{' after match value");

            var arms = new List<MatchArm>();
            while (!Check(TokenType.RightBrace) && !IsAtEnd())
            {
                arms.Add(ParseMatchArm());
                if (Check(TokenType.Comma))
                    Advance();
            }
            Consume(TokenType.RightBrace, "Expected '}' after match arms");

            return new MatchStatement
            {
                Value = value,
                Arms = arms
            };
        }

        private Statement ParseIncludeStatement()
        {
            // include "filename.uh"
            var fileToken = Consume(TokenType.String, "Expected file name after include");
            return new IncludeStatement { FileName = fileToken.Value };
        }
    }

    // Add this AST node at the end of the file (or in your AST definitions)
    public class IncludeStatement : Statement
    {
        public string FileName { get; set; } = "";
    }
}