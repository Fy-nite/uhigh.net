using uhigh.Net.Lexer;
using uhigh.Net.Diagnostics;
using System.Text;

namespace uhigh.Net.Parser
{
    /// <summary>
    /// The parser class
    /// </summary>
    public class Parser
    {
        /// <summary>
        /// The tokens
        /// </summary>
        private readonly List<Token> _tokens;
        /// <summary>
        /// The current
        /// </summary>
        private int _current = 0;
        /// <summary>
        /// The diagnostics
        /// </summary>
        private readonly DiagnosticsReporter _diagnostics;
        /// <summary>
        /// The method checker
        /// </summary>
        private readonly MethodChecker _methodChecker;
        /// <summary>
        /// The attribute resolver
        /// </summary>
        private readonly ReflectionAttributeResolver _attributeResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="Parser"/> class
        /// </summary>
        /// <param name="tokens">The tokens</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <param name="verboseMode">The verbose mode</param>
        public Parser(List<Token> tokens, DiagnosticsReporter? diagnostics = null, bool verboseMode = false)
        {
            _tokens = tokens;
            _diagnostics = diagnostics ?? new DiagnosticsReporter(verboseMode);
            _methodChecker = new MethodChecker(_diagnostics);
            _attributeResolver = _methodChecker.GetAttributeResolver();
        }

        /// <summary>
        /// Parses this instance
        /// </summary>
        /// <returns>The program</returns>
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

        /// <summary>
        /// Gets the diagnostics
        /// </summary>
        /// <returns>The diagnostics</returns>
        public DiagnosticsReporter GetDiagnostics()
        {
            return _diagnostics;
        }


        /// <summary>
        /// Registers the all methods
        /// </summary>
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

                    // Parse modifiers
                    var modifiers = new List<string>();
                    while (IsModifierToken(Peek()))
                    {
                        modifiers.Add(Advance().Value);
                    }

                    if (Check(TokenType.Namespace))
                    {
                        Advance(); // Skip 'namespace'
                        var namespaceName = Consume(TokenType.Identifier, "Expected namespace name").Value;
                        
                        // Skip to namespace body
                        while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                        {
                            Advance();
                        }
                        
                        if (Check(TokenType.LeftBrace))
                        {
                            Advance(); // Skip '{'
                            
                            // Process namespace contents
                            while (!Check(TokenType.RightBrace) && !IsAtEnd())
                            {
                                if (Check(TokenType.Class))
                                {
                                    Advance(); // Skip 'class'
                                    var classNameToken = Consume(TokenType.Identifier, "Expected class name");
                                    var fullClassName = $"{namespaceName}.{classNameToken.Value}";
                                    
                                    // Skip to class body and register methods
                                    while (!Check(TokenType.LeftBrace) && !IsAtEnd())
                                    {
                                        Advance();
                                    }
                                    
                                    if (Check(TokenType.LeftBrace))
                                    {
                                        Advance(); // Skip '{'
                                        
                                        while (!Check(TokenType.RightBrace) && !IsAtEnd())
                                        {
                                            if (Check(TokenType.Func))
                                            {
                                                Advance(); // Skip 'func'
                                                var methodNameToken = Consume(TokenType.Identifier, "Expected method name");
                                                
                                                var method = new MethodDeclaration { Name = methodNameToken.Value };
                                                var methodLocation = new SourceLocation(methodNameToken.Line, methodNameToken.Column);
                                                _methodChecker.RegisterMethod(method, fullClassName, methodLocation);
                                            }
                                            else
                                            {
                                                Advance();
                                            }
                                        }
                                        
                                        if (Check(TokenType.RightBrace)) Advance(); // Skip '}'
                                    }
                                }
                                else
                                {
                                    Advance();
                                }
                            }
                            
                            if (Check(TokenType.RightBrace)) Advance(); // Skip '}'
                        }
                    }
                    else if (Check(TokenType.Class))
                    {
                        Advance(); // Skip 'class'

                        var classNameToken = Consume(TokenType.Identifier, "Expected class name");

                        // Handle qualified class names (e.g., microshell.shell)
                        var fullClassName = classNameToken.Value;
                        while (Match(TokenType.Dot))
                        {
                            fullClassName += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
                        }

                        // Skip external classes
                        if (hasExternalAttribute)
                        {
                            _diagnostics.ReportInfo($"Skipping registration for external class: {fullClassName}");
                            // Skip to end of class declaration
                            SkipToEndOfBlock();
                            continue;
                        }

                        // Create a temporary class declaration for registration
                        var tempClassDecl = new ClassDeclaration
                        {
                            Name = fullClassName,
                            Modifiers = modifiers,
                            Attributes = attributes,
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
                            
                            if (Check(TokenType.RightBrace)) Advance(); // Skip '}'
                        }
                    }
                    else if (Check(TokenType.Func))
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
                            SkipToEndOfBlock();
                            continue;
                        }

                        // Skip function parameters and body for registration
                        SkipFunctionSignature();
                        SkipToEndOfBlock();

                        // Create function declaration for registration
                        var func = new FunctionDeclaration
                        {
                            Name = functionName,
                            Parameters = new List<Parameter>(),
                            ReturnType = null,
                            Attributes = attributes
                        };
                        var location = new SourceLocation(nameToken.Line, nameToken.Column);
                        _methodChecker.RegisterMethod(func, location);

                        _diagnostics.ReportInfo($"Registered function: {functionName}");
                    }
                    else
                    {
                        Advance();
                    }
                }
                catch (Exception ex)
                {
                    _diagnostics.ReportWarning($"Error during method registration: {ex.Message}");
                    Advance();
                }
            }

            _current = tempCurrent; // Reset to original position
        }

        // Helper method to skip to end of block
        private void SkipToEndOfBlock()
        {
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
        }

        // Helper method to skip function signature
        private void SkipFunctionSignature()
        {
            // Skip parameters
            if (Check(TokenType.LeftParen))
            {
                Advance(); // Skip '('
                var parenCount = 1;
                while (parenCount > 0 && !IsAtEnd())
                {
                    if (Check(TokenType.LeftParen)) parenCount++;
                    else if (Check(TokenType.RightParen)) parenCount--;
                    Advance();
                }
            }
            
            // Skip return type
            if (Check(TokenType.Colon))
            {
                Advance(); // Skip ':'
                Advance(); // Skip return type
            }
        }

        /// <summary>
        /// Parses the statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement? ParseStatement()
        {
            try
            {
                // Parse attributes first - this is the critical fix
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

                // Now we must have a valid declaration if we have attributes or modifiers
                if (attributes.Count > 0 || modifiers.Count > 0)
                {
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
                    if (Match(TokenType.Class))
                    {
                        var classDecl = ParseClassDeclaration() as ClassDeclaration;
                        if (classDecl != null)
                        {
                            classDecl.Modifiers.AddRange(modifiers);
                            classDecl.Attributes.AddRange(attributes);
                        }
                        return classDecl;
                    }
                    // Add other declaration types as needed
                    
                    // If we have attributes/modifiers but no valid declaration, that's an error
                    _diagnostics.ReportParseError($"Expected declaration after attributes/modifiers, found '{Peek().Value}'", Peek());
                    return null;
                }

                // Regular statement parsing without attributes/modifiers
                if (Match(TokenType.Type)) return ParseTypeAliasDeclaration(modifiers);
                if (Match(TokenType.Include)) return ParseIncludeStatement();
                if (Match(TokenType.Import)) return ParseImportStatement();
                if (Match(TokenType.Using)) return ParseUsingStatement();
                if (Match(TokenType.Namespace)) return ParseNamespaceDeclaration();
                if (Match(TokenType.Enum)) return ParseEnumDeclaration(modifiers);
                if (Match(TokenType.Interface)) return ParseInterfaceDeclaration(modifiers);
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
                if (Match(TokenType.Sharp)) return ParseSharpBlock();
                // Fix: Parse match as a statement, not an expression
                if (Match(TokenType.Match)) return ParseMatchStatement();

                // Handle legacy using statements
                if (Check(TokenType.Identifier) && Peek().Value.Equals("Using", StringComparison.OrdinalIgnoreCase))
                {
                    Advance();
                    if (Check(TokenType.Identifier))
                    {
                        var namespaceName = Consume(TokenType.Identifier, "Expected namespace name after Using").Value;
                        while (Match(TokenType.Dot))
                        {
                            namespaceName += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
                        }
                        if (Check(TokenType.Semicolon)) Advance();
                        return new ImportStatement
                        {
                            ClassName = namespaceName,
                            AssemblyName = namespaceName
                        };
                    }
                }

                return ParseExpressionStatement();
            }
            catch (ParseException)
            {
                Synchronize();
                return null;
            }
            catch (Exception ex)
            {
                _diagnostics.ReportParseError($"Unexpected error: {ex.Message}", Peek());
                Synchronize();
                return null;
            }
        }

        // Add method to parse using statements
        /// <summary>
        /// Parses the using statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseUsingStatement()
        {
            var identifier = Consume(TokenType.Identifier, "Expected namespace or type name after 'using'").Value;
            
            // Handle qualified names (e.g., System.Collections.Generic)
            while (Match(TokenType.Dot))
            {
                identifier += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
            }

            // Optional semicolon
            if (Check(TokenType.Semicolon)) Advance();

            return new ImportStatement
            {
                ClassName = identifier,
                AssemblyName = identifier // For using statements, class name and assembly are the same
            };
        }

        /// <summary>
        /// Parses the enum declaration using the specified modifiers
        /// </summary>
        /// <param name="modifiers">The modifiers</param>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the interface declaration using the specified modifiers
        /// </summary>
        /// <param name="modifiers">The modifiers</param>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the interface member
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the type alias declaration using the specified modifiers
        /// </summary>
        /// <param name="modifiers">The modifiers</param>
        /// <returns>The statement</returns>
        private Statement ParseTypeAliasDeclaration(List<string> modifiers)
        {
            var name = Consume(TokenType.Identifier, "Expected type alias name").Value;
            Consume(TokenType.Assign, "Expected '=' in type alias");
            var type = ParseTypeAnnotation();
            // Optional semicolon
            if (Check(TokenType.Semicolon)) Advance();
            return new TypeAliasDeclaration
            {
                Name = name,
                Type = type,
                Modifiers = modifiers
            };
        }

        // Update this method to use ParseTypeAnnotation for type names
        /// <summary>
        /// Parses the type name
        /// </summary>
        /// <exception cref="ParseException">Unexpected attribute in type context</exception>
        /// <returns>The type name</returns>
        private string ParseTypeName()
        {
            string typeName;
            
            // Handle built-in type keywords
            if (Check(TokenType.Array))
            {
                typeName = "array";
                Advance();
            }
            else if (Check(TokenType.StringType))
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
            else if (Check(TokenType.Void))
            {
                typeName = "void";
                Advance();
            }
            else
            {
                // Check for attributes that shouldn't be parsed as type names
                if (Check(TokenType.LeftBracket))
                {
                    _diagnostics.ReportParseError("Attributes cannot appear in type context", Peek());
                    throw new ParseException("Unexpected attribute in type context");
                }
                
                typeName = Consume(TokenType.Identifier, "Expected type name").Value;
            }
            
            // Handle generic type parameters if not already included in the identifier
            if (!typeName.Contains('<') && Match(TokenType.Less)) // <
            {
                typeName += "<";
                do
                {
                    var typeArg = ParseTypeName();
                    typeName += typeArg;
                    if (Match(TokenType.Comma))
                    {
                        typeName += ", ";
                    }
                } while (!Check(TokenType.Greater) && !IsAtEnd());
                
                Consume(TokenType.Greater, "Expected '>' after generic type arguments");
                typeName += ">";
                
                // Handle array syntax after generic parameters if not already included
                if (!typeName.Contains("[]") && Match(TokenType.LeftBracket))
                {
                    Consume(TokenType.RightBracket, "Expected ']' after '['");
                    typeName += "[]";
                }
            }
            
            // Validate the type using reflection - but be more lenient
            if (!_methodChecker.ValidateType(typeName, Peek()))
            {
                // Only warn if it's not a potential type parameter or generic type
                if (!IsLikelyValidType(typeName))
                {
                    _diagnostics.ReportWarning($"Type '{typeName}' may not be valid", Peek().Line, Peek().Column, "UH300");
                }
            }
            
            return typeName;
        }

        // Add method to parse type annotations with generics and arrays
        /// <summary>
        /// Parses the type annotation
        /// </summary>
        /// <returns>The type ann</returns>
        private TypeAnnotation ParseTypeAnnotation()
        {
            string typeName;
            
            // Handle built-in type keywords
            if (Check(TokenType.Array))
            {
                typeName = "array";
                Advance();
            }
            else if (Check(TokenType.StringType))
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
            else if (Check(TokenType.Void))
            {
                typeName = "void";
                Advance();
            }
            else
            {
                typeName = Consume(TokenType.Identifier, "Expected type name").Value;
            }
            
            var typeAnn = new TypeAnnotation { Name = typeName };

            // Handle generic type parameters first (e.g., List<string>)
            if (Match(TokenType.Less)) // <
            {
                do
                {
                    typeAnn.TypeArguments.Add(ParseTypeAnnotation());
                } while (Match(TokenType.Comma));
                Consume(TokenType.Greater, "Expected '>' after generic type arguments");
            }

            // Handle array syntax after generic parameters (e.g., List<string>[] or string[])
            if (Match(TokenType.LeftBracket))
            {
                Consume(TokenType.RightBracket, "Expected ']' after '['");
                // Build the generic part first, then add array brackets
                var baseTypeName = typeAnn.Name;
                if (typeAnn.TypeArguments.Count > 0)
                {
                    baseTypeName = $"{typeAnn.Name}<{string.Join(", ", typeAnn.TypeArguments.Select(t => BuildTypeString(t)))}>";
                }
                typeAnn.Name = baseTypeName + "[]";
                typeAnn.TypeArguments.Clear(); // Clear since we've built the full name
            }

            return typeAnn;
        }

        // Helper method to build type string from TypeAnnotation
        /// <summary>
        /// Builds the type string using the specified type ann
        /// </summary>
        /// <param name="typeAnn">The type ann</param>
        /// <returns>The string</returns>
        private string BuildTypeString(TypeAnnotation typeAnn)
        {
            if (typeAnn.TypeArguments.Count == 0)
                return typeAnn.Name;
            
            return $"{typeAnn.Name}<{string.Join(",", typeAnn.TypeArguments.Select(t => BuildTypeString(t)))}>";
        }

        // Helper method to check if a type name is likely valid
        /// <summary>
        /// Ises the likely valid type using the specified type name
        /// </summary>
        /// <param name="typeName">The type name</param>
        /// <returns>The bool</returns>
        private bool IsLikelyValidType(string typeName)
        {
            // Allow type parameters (single uppercase letters or T-prefixed names)
            if (typeName.Length == 1 && char.IsUpper(typeName[0]))
                return true;
            
            if (typeName.StartsWith("T") && typeName.Length <= 10 && char.IsUpper(typeName[0]))
                return true;
            
            // Allow common generic patterns
            if (typeName.Contains('<') && typeName.Contains('>'))
                return true;
            
            // Allow common framework types
            var commonTypes = new[] { 
                "string", "int", "float", "bool", "void", "object", "double", "decimal",
                "List", "Dictionary", "Array", "IEnumerable", "ICollection", "HashSet",
                "TimestampedEvent", "Observable", "EventStream"
            };
            
            return commonTypes.Any(ct => typeName.Contains(ct));
        }

        /// <summary>
        /// Parses the field declaration
        /// </summary>
        /// <returns>The statement</returns>
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
                    type = ParseTypeName();
                }
            }

            if (Match(TokenType.Assign))
            {
                initializer = ParseExpression();
            }

            return new FieldDeclaration { Name = name, Type = type, Initializer = initializer };
        }

        /// <summary>
        /// Parses the property declaration
        /// </summary>
        /// <returns>The statement</returns>
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
                    type = ParseTypeName();
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

        /// <summary>
        /// Parses the method declaration
        /// </summary>
        /// <returns>The statement</returns>
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
                            paramType = ParseTypeName();
                        }
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

        /// <summary>
        /// Parses the const declaration
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseConstDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected variable name").Value;
            Consume(TokenType.Assign, "Expected '=' after const name");
            var initializer = ParseExpression();
            return new VariableDeclaration { Name = name, Initializer = initializer, IsConstant = true };
        }

        /// <summary>
        /// Parses the variable declaration
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the function declaration
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the if statement
        /// </summary>
        /// <returns>The statement</returns>
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
                    elseBranch = new List<Statement> { elseIfStmt! };
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

        /// <summary>
        /// Parses the while statement
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the for statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseForStatement()
        {
            // Check for for-in syntax: for var i in expression
            if (Check(TokenType.Var) || Check(TokenType.Identifier))
            {
                var isNewVariable = Match(TokenType.Var);
                var iteratorName = Consume(TokenType.Identifier, "Expected iterator variable name").Value;
                
                if (Match(TokenType.In))
                {
                    // This is a for-in loop
                    var iterableExpr = ParseExpression();
                    
                    Consume(TokenType.LeftBrace, "Expected '{' after for-in expression");
                    
                    var body = new List<Statement>();
                    while (!Check(TokenType.RightBrace) && !IsAtEnd())
                    {
                        var stmt = ParseStatement();
                        if (stmt != null) body.Add(stmt);
                    }
                    
                    Consume(TokenType.RightBrace, "Expected '}' after for loop body");
                    
                    return new ForStatement
                    {
                        IteratorVariable = iteratorName,
                        IterableExpression = iterableExpr,
                        Body = body
                    };
                }
                else
                {
                    // Traditional for loop starting with variable declaration
                    // Put back the tokens and parse as traditional for loop
                    _current -= isNewVariable ? 2 : 1;
                    return ParseTraditionalForStatement();
                }
            }
            else
            {
                // Traditional for loop
                return ParseTraditionalForStatement();
            }
        }

        /// <summary>
        /// Parses the traditional for statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseTraditionalForStatement()
        {
            // Traditional for loop: for (init; condition; increment)
            Consume(TokenType.LeftParen, "Expected '(' after 'for'");
            
            var init = ParseStatement();
            Consume(TokenType.Semicolon, "Expected ';' after for loop initializer");
            var condition = ParseExpression();
            Consume(TokenType.Semicolon, "Expected ';' after for loop condition");
            var update = ParseExpressionStatement();
            
            Consume(TokenType.RightParen, "Expected ')' after for loop header");
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

        /// <summary>
        /// Parses the return statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseReturnStatement()
        {
            Expression? value = null;
            if (!Check(TokenType.Semicolon) && !Check(TokenType.RightBrace))
            {
                value = ParseExpression();
            }
            return new ReturnStatement { Value = value };
        }

        /// <summary>
        /// Parses the break statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseBreakStatement()
        {
            return new BreakStatement();
        }

        /// <summary>
        /// Parses the continue statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseContinueStatement()
        {
            return new ContinueStatement();
        }

        /// <summary>
        /// Parses the sharp block
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the expression statement
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the expression
        /// </summary>
        /// <returns>The expression</returns>
        private Expression ParseExpression()
        {
            return ParseAssignment();
        }

        /// <summary>
        /// Parses the assignment
        /// </summary>
        /// <returns>The expr</returns>
        private Expression ParseAssignment()
        {
            var expr = ParseOr();

            if (Match(TokenType.Assign, TokenType.PlusAssign, TokenType.MinusAssign,
                     TokenType.MultiplyAssign, TokenType.DivideAssign))
            {
                var op = Previous().Type;
                var value = ParseAssignment();
                
                // Ensure we have a valid assignment target
                if (expr is IdentifierExpression || expr is MemberAccessExpression || expr is IndexExpression
                    || expr is QualifiedIdentifierExpression) // <-- allow qualified identifiers
                {
                    return new AssignmentExpression { Target = expr, Operator = op, Value = value };
                }
                else
                {
                    _diagnostics.ReportParseError($"Invalid assignment target: {expr?.GetType().Name}", Previous());
                    return new AssignmentExpression { Target = expr!, Operator = op, Value = value };
                }
            }

            return expr;
        }

        /// <summary>
        /// Parses the or
        /// </summary>
        /// <returns>The expr</returns>
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

        /// <summary>
        /// Parses the and
        /// </summary>
        /// <returns>The expr</returns>
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

        /// <summary>
        /// Parses the equality
        /// </summary>
        /// <returns>The expr</returns>
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

        /// <summary>
        /// Parses the comparison
        /// </summary>
        /// <returns>The expr</returns>
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

        /// <summary>
        /// Parses the term
        /// </summary>
        /// <returns>The expr</returns>
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

        /// <summary>
        /// Parses the factor
        /// </summary>
        /// <returns>The expr</returns>
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

        /// <summary>
        /// Parses the unary
        /// </summary>
        /// <returns>The expression</returns>
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

        /// <summary>
        /// Parses the postfix
        /// </summary>
        /// <returns>The expr</returns>
        private Expression ParsePostfix()
        {
            var expr = ParseCall();

            // Handle postfix increment/decrement
            if (Match(TokenType.Increment, TokenType.Decrement))
            {
                var op = Previous().Type;
                return new UnaryExpression { Operator = op, Operand = expr };
            }

            // Remove the match expression parsing from here - match should only be parsed as statements
            // The match keyword should be handled in ParseStatement(), not as a postfix operator

            return expr;
        }

        /// <summary>
        /// Parses the call
        /// </summary>
        /// <returns>The expr</returns>
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
                    
                    // Check for array-specific method calls
                    if (Check(TokenType.LeftParen) && IsArrayMethod(name))
                    {
                        Advance(); // consume '('
                        var arguments = new List<Expression>();
                        
                        if (!Check(TokenType.RightParen))
                        {
                            do
                            {
                                arguments.Add(ParseExpression());
                            } while (Match(TokenType.Comma));
                        }
                        
                        Consume(TokenType.RightParen, "Expected ')' after arguments");
                        expr = new ArrayMethodCallExpression 
                        { 
                            Array = expr, 
                            MethodName = name, 
                            Arguments = arguments 
                        };
                    }
                    else
                    {
                        expr = new MemberAccessExpression { Object = expr, MemberName = name };
                    }
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        /// <summary>
        /// Ises the array method using the specified method name
        /// </summary>
        /// <param name="methodName">The method name</param>
        /// <returns>The bool</returns>
        private bool IsArrayMethod(string methodName)
        {
            var arrayMethods = new[] { 
                "createIndice", "collect", "mapToArray", "collectAll", 
                "at", "add", "return", "append", "pop", "sort", "reverse",
                "chunk", "flatten", "rotate", "slidingWindow", "mostFrequent", "diff"
            };
            return arrayMethods.Contains(methodName);
        }

        /// <summary>
        /// Parses the primary
        /// </summary>
        /// <exception cref="ParseException">Unexpected token: {Peek().Value}</exception>
        /// <returns>The expression</returns>
        private Expression ParsePrimary()
        {
            if (Match(TokenType.This))
                return new ThisExpression();

            if (Match(TokenType.True))
                return new LiteralExpression { Value = true, Type = TokenType.True };

            if (Match(TokenType.False))
                return new LiteralExpression { Value = false, Type = TokenType.False };

            // Check for potential attributes at expression level
            if (Check(TokenType.LeftBracket))
            {
                // Look ahead to see if this might be an attribute
                var nextToken = _current + 1 < _tokens.Count ? _tokens[_current + 1] : null;
                if (nextToken != null && nextToken.Type == TokenType.Identifier)
                {
                    // Check if the token after the identifier suggests this is an attribute
                    var tokenAfterIdent = _current + 2 < _tokens.Count ? _tokens[_current + 2] : null;
                    if (tokenAfterIdent != null && 
                        (tokenAfterIdent.Type == TokenType.RightBracket || 
                         tokenAfterIdent.Type == TokenType.LeftParen))
                    {
                        // This looks like an attribute, but we're in expression context
                        // This might be a parsing error - attributes should be at statement level
                        _diagnostics.ReportParseError("Attributes cannot appear in expression context", Peek());
                        // Skip the malformed attribute and continue
                        while (!Check(TokenType.RightBracket) && !IsAtEnd())
                        {
                            Advance();
                        }
                        if (Check(TokenType.RightBracket)) Advance();
                        return ParsePrimary(); // Try again
                    }
                }
                // If not an attribute, fall through to array parsing
            }

            // Add support for range expressions
            if (Match(TokenType.Range))
            {
                Consume(TokenType.LeftParen, "Expected '(' after 'range'");
                var end = ParseExpression();
                Consume(TokenType.RightParen, "Expected ')' after range expression");
                
                return new RangeExpression 
                { 
                    Start = new LiteralExpression { Value = 0, Type = TokenType.Number },
                    End = end,
                    IsExclusive = false
                };
            }

            if (Match(TokenType.Number))
            {
                var value = Previous().Value;
                if (value.Contains('.'))
                {
                    return new LiteralExpression { Value = double.Parse(value), Type = TokenType.Number };
                }
                else
                {
                    return new LiteralExpression { Value = int.Parse(value), Type = TokenType.Number };
                }
            }

            if (Match(TokenType.String))
                return new LiteralExpression { Value = Previous().Value, Type = TokenType.String };

            if (Match(TokenType.InterpolatedStringStart))
            {
                return ParseInterpolatedString();
            }
            
            if (Match(TokenType.New))
            {
                var className = Consume(TokenType.Identifier, "Expected class name after 'new'").Value;
                
                // Handle qualified class names (e.g., microshell.shell)
                while (Match(TokenType.Dot))
                {
                    className += "." + Consume(TokenType.Identifier, "Expected identifier after '.'").Value;
                }
                
                // Handle generic type parameters
                if (Match(TokenType.Less))
                {
                    className += "<";
                    do
                    {
                        var typeArg = ParseTypeName();
                        className += typeArg;
                        if (Match(TokenType.Comma))
                        {
                            className += ", ";
                        }
                    } while (!Check(TokenType.Greater) && !IsAtEnd());
                    
                    Consume(TokenType.Greater, "Expected '>' after generic type arguments");
                    className += ">";
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

                Consume(TokenType.RightParen, "Expected ')' after constructor arguments");
                
                return new ConstructorCallExpression
                {
                    ClassName = className,
                    Arguments = arguments
                };
            }

            if (Match(TokenType.Identifier))
            {
                var identifier = Previous().Value;
                
                // Check for qualified identifiers (e.g., object.method)
                if (identifier.Contains('.'))
                {
                    return new QualifiedIdentifierExpression { Name = identifier };
                }
                else
                {
                    return new IdentifierExpression { Name = identifier };
                }
            }
            

            if (Match(TokenType.LeftParen))
            {
                // Look ahead to see if this is a lambda or grouping expression
                var start = _current;
                var isLambda = false;
                var paramCount = 0;
                
                // Parse potential parameter list
                if (!Check(TokenType.RightParen))
                {
                    do
                    {
                        if (Check(TokenType.Identifier))
                        {
                            Advance();
                            paramCount++;
                            
                            // Optional type annotation
                            if (Match(TokenType.Colon))
                            {
                                if (Check(TokenType.Identifier)) Advance();
                            }
                        }
                        else
                        {
                            break; // Not a parameter list
                        }
                    } while (Match(TokenType.Comma));
                }
                
                // Check if followed by ) =>
                if (Check(TokenType.RightParen))
                {
                    Advance();
                    if (Check(TokenType.Arrow))
                    {
                        isLambda = true;
                    }
                }
                
                // Reset position
                _current = start - 1; // Back to before the '('
                Advance(); // consume '('
                
                if (isLambda)
                {
                    // Parse as lambda expression
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
                            
                            parameters.Add(new Parameter(paramName, paramType));
                        } while (Match(TokenType.Comma));
                    }
                    
                    Consume(TokenType.RightParen, "Expected ')' after lambda parameters");
                    Consume(TokenType.Arrow, "Expected '=>' after lambda parameters");
                    
                    var body = ParseExpression();
                    return new LambdaExpression
                    {
                        Parameters = parameters,
                        Body = body
                    };
                }
                else
                {
                    // Parse as grouping expression
                    var expr = ParseExpression();
                    Consume(TokenType.RightParen, "Expected ')' after expression");
                    return expr;
                }
            }

            // Remove match expression parsing from primary expressions
            // Match should only be parsed as statements, not expressions

            throw new ParseException($"Unexpected token: {Peek().Value}");
        }

        /// <summary>
        /// Parses the match expression using the specified value
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The expression</returns>
        private Expression ParseMatchExpression(Expression? value = null)
        {
            // If value is null, parse it (this should not happen in postfix context)
            if (value == null)
            {
                _diagnostics.ReportParseError("Match expression missing value", Peek());
                return new LiteralExpression { Value = null!, Type = TokenType.String };
            }

            // Expect opening brace immediately after match keyword and value
            if (!Check(TokenType.LeftBrace))
            {
                _diagnostics.ReportParseError($"Expected '{{' after match value, found '{Peek().Value}'", Peek());
                return new LiteralExpression { Value = null!, Type = TokenType.String };
            }

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

        /// <summary>
        /// Parses the match arm
        /// </summary>
        /// <returns>The match arm</returns>
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
            
            // Support both expression and block forms
            Expression result;
            if (Check(TokenType.LeftBrace))
            {
                // Block form: { statements... }
                Advance(); // consume '{'
                
                var statements = new List<Statement>();
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    var stmt = ParseStatement();
                    if (stmt != null) statements.Add(stmt);
                }
                
                Consume(TokenType.RightBrace, "Expected '}' after match arm block");
                
                // Wrap the block in a special expression
                result = new BlockExpression { Statements = statements };
            }
            else
            {
                // Expression form: expression
                result = ParseExpression();
            }

            return new MatchArm
            {
                Patterns = patterns,
                Result = result,
                IsDefault = isDefault
            };
        }

        /// <summary>
        /// Parses the interpolated string
        /// </summary>
        /// <returns>The expression</returns>
        private Expression ParseInterpolatedString()
        {
            var parts = new List<InterpolationPart>();
            // var currentText = "";
    
            // This is a simplified version - a full implementation would need
            // more sophisticated tokenization for interpolated strings
            var stringValue = Previous().Value;
    
            // Parse the interpolated string format $"text{expr}text"
            // For now, convert to string concatenation
            return new LiteralExpression { Value = stringValue, Type = TokenType.String };
        }
        
        /// <summary>
        /// Finishes the call using the specified callee
        /// </summary>
        /// <param name="callee">The callee</param>
        /// <returns>The expression</returns>
        private Expression FinishCall(Expression callee)
        {
            var arguments = new List<Expression>();
            
            if (!Check(TokenType.RightParen))
            {
                do
                {
                    // Check for lambda expressions more carefully
                    if (IsLambdaExpression())
                    {
                        arguments.Add(ParseLambdaExpression());
                    }
                    else
                    {
                        arguments.Add(ParseExpression());
                    }
                } while (Match(TokenType.Comma));
            }
            
            Consume(TokenType.RightParen, "Expected ')' after arguments");
            
            return new CallExpression { Function = callee, Arguments = arguments };
        }

        // Helper method to detect lambda expressions
        private bool IsLambdaExpression()
        {
            var checkpoint = _current;
            
            // Case 1: Single parameter lambda: identifier =>
            if (Check(TokenType.Identifier))
            {
                Advance(); // consume identifier
                if (Check(TokenType.Arrow))
                {
                    _current = checkpoint; // reset
                    return true;
                }
            }
            
            // Case 2: Multi-parameter lambda: (param1, param2) =>
            _current = checkpoint; // reset
            if (Check(TokenType.LeftParen))
            {
                Advance(); // consume (
                
                // Skip parameter list
                var parenCount = 1;
                while (parenCount > 0 && !IsAtEnd())
                {
                    if (Check(TokenType.LeftParen)) parenCount++;
                    else if (Check(TokenType.RightParen)) parenCount--;
                    
                    if (parenCount > 0) Advance();
                }
                
                if (parenCount == 0)
                {
                    Advance(); // consume )
                    if (Check(TokenType.Arrow))
                    {
                        _current = checkpoint; // reset
                        return true;
                    }
                }
            }
            
            _current = checkpoint; // reset
            return false;
        }

        // Helper method to parse lambda expression
        private LambdaExpression ParseLambdaExpression()
        {
            var parameters = new List<Parameter>();
            
            // Single parameter case: identifier =>
            if (Check(TokenType.Identifier) && PeekAhead(1)?.Type == TokenType.Arrow)
            {
                var paramName = Consume(TokenType.Identifier, "Expected parameter name").Value;
                parameters.Add(new Parameter(paramName));
                Consume(TokenType.Arrow, "Expected '=>' in lambda expression");
            }
            // Multi-parameter case: (param1, param2) =>
            else if (Check(TokenType.LeftParen))
            {
                Consume(TokenType.LeftParen, "Expected '(' for lambda parameters");
                
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
                        
                        parameters.Add(new Parameter(paramName, paramType));
                    } while (Match(TokenType.Comma));
                }
                
                Consume(TokenType.RightParen, "Expected ')' after lambda parameters");
                Consume(TokenType.Arrow, "Expected '=>' after lambda parameters");
            }
            
            var body = ParseLambdaBody();
            
            return new LambdaExpression
            {
                Parameters = parameters,
                Body = body.expression,
                Statements = body.statements
            };
        }

        // Helper method to parse lambda body
        private (Expression? expression, List<Statement> statements) ParseLambdaBody()
        {
            if (Check(TokenType.LeftBrace))
            {
                // Block lambda: { statements }
                Advance(); // consume {
                var statements = new List<Statement>();
                
                while (!Check(TokenType.RightBrace) && !IsAtEnd())
                {
                    var stmt = ParseStatement();
                    if (stmt != null)
                        statements.Add(stmt);
                }
                
                Consume(TokenType.RightBrace, "Expected '}' after lambda body");
                return (null, statements);
            }
            else
            {
                // Expression lambda: expression
                var expression = ParseExpression();
                return (expression, new List<Statement>());
            }
        }

        // Helper method to peek ahead in the token list by a given offset
        private Token? PeekAhead(int offset)
        {
            int index = _current + offset;
            if (index >= 0 && index < _tokens.Count)
                return _tokens[index];
            return null;
        }
        /// <summary>
        /// Converts the token type to string using the specified token type
        /// </summary>
        /// <param name="tokenType">The token type</param>
        /// <returns>The string</returns>
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

        /// <summary>
        /// Matches the types
        /// </summary>
        /// <param name="types">The types</param>
        /// <returns>The bool</returns>
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

        /// <summary>
        /// Checks the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <returns>The bool</returns>
        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        /// <summary>
        /// Advances this instance
        /// </summary>
        /// <returns>The token</returns>
        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return Previous();
        }

        /// <summary>
        /// Ises the at end
        /// </summary>
        /// <returns>The bool</returns>
        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }

        /// <summary>
        /// Peeks this instance
        /// </summary>
        /// <returns>The token</returns>
        private Token Peek()
        {
            return _tokens[_current];
        }

        /// <summary>
        /// Previouses this instance
        /// </summary>
        /// <returns>The token</returns>
        private Token Previous()
        {
            return _tokens[_current - 1];
        }

        /// <summary>
        /// Consumes the type
        /// </summary>
        /// <param name="type">The type</param>
        /// <param name="message">The message</param>
        /// <exception cref="ParseException">{message}. Got {Peek().Type}</exception>
        /// <returns>The token</returns>
        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();

            // Only report the error once here
            _diagnostics.ReportUnexpectedToken(Peek(), message);
            throw new ParseException($"{message}. Got {Peek().Type}");
        }

        /// <summary>
        /// Synchronizes this instance
        /// </summary>
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

        /// <summary>
        /// Ises the modifier token using the specified token
        /// </summary>
        /// <param name="token">The token</param>
        /// <returns>The bool</returns>
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

        /// <summary>
        /// Parses the attribute
        /// </summary>
        /// <returns>The attribute declaration</returns>
        private AttributeDeclaration ParseAttribute()
        {
            Consume(TokenType.LeftBracket, "Expected '['");
            
            var nameToken = Consume(TokenType.Identifier, "Expected attribute name");
            var name = nameToken.Value;

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

        /// <summary>
        /// Parses the import statement
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the namespace declaration
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the class declaration
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseClassDeclaration()
        {
            var name = Consume(TokenType.Identifier, "Expected class name").Value;

            string? baseClass = null;
            if (Match(TokenType.Colon))
            {
                baseClass = ParseTypeName();
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
                Modifiers = new List<string>(),
                Attributes = new List<AttributeDeclaration>()
            };
        }

        /// <summary>
        /// Parses the class member
        /// </summary>
        /// <returns>The statement</returns>
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

        /// <summary>
        /// Parses the match statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseMatchStatement()
        {
            // Parse: match <value> { ... } (match keyword already consumed)
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

        /// <summary>
        /// Parses the include statement
        /// </summary>
        /// <returns>The statement</returns>
        private Statement ParseIncludeStatement()
        {
            // include "filename.uh"
            var fileToken = Consume(TokenType.String, "Expected file name after include");
            return new IncludeStatement { FileName = fileToken.Value };
        }
    }


}