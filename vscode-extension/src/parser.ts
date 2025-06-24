import { Token, TokenType } from './lexer';

export interface ASTNode {
    type: string;
    name?: string;
    value?: any;
    children?: ASTNode[];
    line?: number;
    column?: number;
}

export class UhighParser {
    private tokens: Token[] = [];
    private current = 0;
    private parseDepth = 0;
    private maxParseDepth = 100;

    public parse(tokens: Token[]): ASTNode {
        this.tokens = tokens.filter(t => t.type !== TokenType.WHITESPACE && t.type !== TokenType.COMMENT);
        this.current = 0;
        this.parseDepth = 0;

        // Quick safety check - if too many tokens, don't parse
        if (this.tokens.length > 5000) {
            throw new Error('File too complex to parse');
        }

        return this.parseProgram();
    }

    public parseOptimized(tokens: Token[]): ASTNode {
        this.tokens = tokens.filter(t => t.type !== TokenType.WHITESPACE && t.type !== TokenType.COMMENT);
        this.current = 0;
        this.parseDepth = 0;

        // Even more restrictive limits for optimized parsing
        if (this.tokens.length > 1000) {
            throw new Error('File too complex for optimized parsing');
        }

        return this.parseProgramOptimized();
    }

    private parseProgram(): ASTNode {
        const children: ASTNode[] = [];
        let statementCount = 0;

        while (!this.isAtEnd() && statementCount < 1000) { // Limit statements
            if (this.match(TokenType.NEWLINE)) continue;
            
            const stmt = this.parseStatement();
            if (stmt) {
                children.push(stmt);
            }
            statementCount++;
        }

        return {
            type: 'Program',
            children
        };
    }

    private parseProgramOptimized(): ASTNode {
        const children: ASTNode[] = [];
        let statementCount = 0;

        while (!this.isAtEnd() && statementCount < 100) { // Reduced limit
            if (this.match(TokenType.NEWLINE)) continue;
            
            try {
                const stmt = this.parseStatementOptimized();
                if (stmt) {
                    children.push(stmt);
                }
            } catch (error) {
                // Skip errors in optimized mode and continue
                console.log('Skipping statement due to parse error:', error);
                this.skipToNextStatement();
            }
            statementCount++;
        }

        return {
            type: 'Program',
            children
        };
    }

    private parseStatement(): ASTNode | null {
        // Prevent infinite recursion
        this.parseDepth++;
        if (this.parseDepth > this.maxParseDepth) {
            throw new Error('Maximum parse depth exceeded');
        }

        try {
            // Handle multiple access modifiers
            const accessModifiers: string[] = [];
            while (this.match(TokenType.PUBLIC, TokenType.PRIVATE, TokenType.PROTECTED, TokenType.INTERNAL, TokenType.STATIC, TokenType.ABSTRACT, TokenType.VIRTUAL, TokenType.OVERRIDE, TokenType.SEALED, TokenType.READONLY)) {
                accessModifiers.push(this.previous().value);
                // Prevent infinite loop - limit to reasonable number of modifiers
                if (accessModifiers.length > 5) {
                    break;
                }
            }

            // Handle access modifiers
            let accessModifier: string | null = null;
            if (this.match(TokenType.PUBLIC, TokenType.PRIVATE, TokenType.PROTECTED, TokenType.INTERNAL, TokenType.STATIC, TokenType.ABSTRACT, TokenType.VIRTUAL, TokenType.OVERRIDE, TokenType.SEALED, TokenType.READONLY)) {
                accessModifier = this.previous().value;
            }

            if (this.match(TokenType.FUNC)) {
                const func = this.parseFunctionDeclaration();
                if (accessModifier) {
                    func.value = accessModifier;
                }
                return func;
            }

            // If we consumed an access modifier but no valid statement follows, skip to next line
            if (accessModifier) {
                // Skip to end of line to recover from error
                while (!this.check(TokenType.NEWLINE) && !this.check(TokenType.EOF)) {
                    this.advance();
                }
                return null;
            }

            if (this.match(TokenType.VAR, TokenType.CONST)) {
                return this.parseVariableDeclaration();
            }

            if (this.match(TokenType.CLASS)) {
                return this.parseClassDeclaration();
            }

            if (this.match(TokenType.NAMESPACE)) {
                return this.parseNamespaceDeclaration();
            }

            if (this.match(TokenType.IF)) {
                return this.parseIfStatement();
            }

            if (this.match(TokenType.WHILE)) {
                return this.parseWhileStatement();
            }

            if (this.match(TokenType.RETURN)) {
                return this.parseReturnStatement();
            }

            // Expression statement
            const expr = this.parseExpression();
            this.consumeNewlineOrSemicolon();
            return expr;
        } finally {
            this.parseDepth--;
        }
    }

    private parseStatementOptimized(): ASTNode | null {
        // Simplified statement parsing for performance
        this.parseDepth++;
        if (this.parseDepth > 20) { // Much lower depth limit
            throw new Error('Parse depth exceeded in optimized mode');
        }

        try {
            // Handle only the most common statements in optimized mode
            if (this.match(TokenType.FUNC)) {
                return this.parseFunctionDeclarationOptimized();
            }

            if (this.match(TokenType.VAR, TokenType.CONST)) {
                return this.parseVariableDeclarationOptimized();
            }

            if (this.match(TokenType.CLASS)) {
                return this.parseClassDeclarationOptimized();
            }

            // Skip other statement types in optimized mode
            this.skipToNextStatement();
            return null;

        } finally {
            this.parseDepth--;
        }
    }

    private parseFunctionDeclaration(): ASTNode {
        // Add safety check to prevent infinite recursion
        if (!this.check(TokenType.IDENTIFIER)) {
            throw new Error("Expected function name after 'func' keyword");
        }
        
        const nameToken = this.consume(TokenType.IDENTIFIER, "Expected function name");
        
        // Ensure we have opening parenthesis
        if (!this.check(TokenType.LPAREN)) {
            throw new Error(`Expected '(' after function name '${nameToken.value}'`);
        }
        this.consume(TokenType.LPAREN, "Expected '(' after function name");
        
        const parameters: ASTNode[] = [];
        if (!this.check(TokenType.RPAREN)) {
            let paramCount = 0;
            do {
                // Safety check to prevent infinite parameter parsing
                if (paramCount > 50) {
                    throw new Error("Too many parameters in function declaration");
                }
                
                if (this.check(TokenType.IDENTIFIER)) {
                    const param = this.advance();
                    let paramType: string | undefined;
                    
                    // Handle type annotation: param: type
                    if (this.match(TokenType.COLON)) {
                        if (this.check(TokenType.IDENTIFIER)) {
                            const typeToken = this.advance();
                            paramType = typeToken.value;
                        } else {
                            throw new Error("Expected type after ':' in parameter declaration");
                        }
                    }
                    
                    parameters.push({
                        type: 'Parameter',
                        name: param.value,
                        value: paramType,
                        line: param.line,
                        column: param.column
                    });
                    paramCount++;
                } else {
                    throw new Error("Expected parameter name in function declaration");
                }
            } while (this.match(TokenType.COMMA));
        }
        
        this.consume(TokenType.RPAREN, "Expected ')' after parameters");
        
        // Handle return type annotation: ): returnType
        let returnType: string | undefined;
        if (this.match(TokenType.COLON)) {
            if (this.check(TokenType.IDENTIFIER)) {
                const typeToken = this.advance();
                returnType = typeToken.value;
            } else {
                throw new Error("Expected return type after ':'");
            }
        }
        
        // Allow newlines before opening brace
        let newlineCount = 0;
        while (this.match(TokenType.NEWLINE)) {
            newlineCount++;
            // Prevent infinite newline consumption
            if (newlineCount > 10) {
                break;
            }
        }
        
        // Function body is required
        if (!this.check(TokenType.LBRACE)) {
            throw new Error(`Expected '{' before function body for function '${nameToken.value}'`);
        }
        this.consume(TokenType.LBRACE, "Expected '{' before function body");
        
        const body = this.parseBlock();
        
        const result: ASTNode = {
            type: 'FunctionDeclaration',
            name: nameToken.value,
            children: [
                { type: 'ParameterList', children: parameters },
                body
            ],
            line: nameToken.line,
            column: nameToken.column
        };
        
        if (returnType) {
            result.value = returnType;
        }
        
        return result;
    }

    private parseFunctionDeclarationOptimized(): ASTNode {
        if (!this.check(TokenType.IDENTIFIER)) {
            throw new Error("Expected function name");
        }
        
        const nameToken = this.advance();
        
        // Skip parameter parsing in optimized mode - just find the opening brace
        this.skipToToken(TokenType.LBRACE);
        if (this.check(TokenType.LBRACE)) {
            this.advance(); // consume {
        }
        
        // Skip function body
        this.skipBlock();
        
        return {
            type: 'FunctionDeclaration',
            name: nameToken.value,
            children: [],
            line: nameToken.line,
            column: nameToken.column
        };
    }

    private parseVariableDeclaration(): ASTNode {
        const kindToken = this.previous();
        const nameToken = this.consume(TokenType.IDENTIFIER, "Expected variable name");
        
        let initializer: ASTNode | null = null;
        if (this.match(TokenType.ASSIGN)) {
            initializer = this.parseExpression();
        }
        
        this.consumeNewlineOrSemicolon();
        
        return {
            type: 'VariableDeclaration',
            name: nameToken.value,
            value: kindToken.value,
            children: initializer ? [initializer] : [],
            line: nameToken.line,
            column: nameToken.column
        };
    }

    private parseVariableDeclarationOptimized(): ASTNode {
        const kindToken = this.previous();
        
        if (!this.check(TokenType.IDENTIFIER)) {
            throw new Error("Expected variable name");
        }
        
        const nameToken = this.advance();
        
        // Skip to end of statement
        this.skipToNextStatement();
        
        return {
            type: 'VariableDeclaration',
            name: nameToken.value,
            value: kindToken.value,
            children: [],
            line: nameToken.line,
            column: nameToken.column
        };
    }

    private parseClassDeclaration(): ASTNode {
        const nameToken = this.consume(TokenType.IDENTIFIER, "Expected class name");
        
        // Allow newlines before opening brace
        while (this.match(TokenType.NEWLINE)) {
            // consume newlines
        }
        
        this.consume(TokenType.LBRACE, "Expected '{' before class body");
        
        const members: ASTNode[] = [];
        while (!this.check(TokenType.RBRACE) && !this.isAtEnd()) {
            if (this.match(TokenType.NEWLINE)) continue;
            
            const member = this.parseStatement();
            if (member) {
                members.push(member);
            }
        }
        
        this.consume(TokenType.RBRACE, "Expected '}' after class body");
        
        return {
            type: 'ClassDeclaration',
            name: nameToken.value,
            children: members.filter((m): m is ASTNode => m !== null),
            line: nameToken.line,
            column: nameToken.column
        };
    }

    private parseClassDeclarationOptimized(): ASTNode {
        if (!this.check(TokenType.IDENTIFIER)) {
            throw new Error("Expected class name");
        }
        
        const nameToken = this.advance();
        
        // Skip class body
        this.skipToToken(TokenType.LBRACE);
        if (this.check(TokenType.LBRACE)) {
            this.skipBlock();
        }
        
        return {
            type: 'ClassDeclaration',
            name: nameToken.value,
            children: [],
            line: nameToken.line,
            column: nameToken.column
        };
    }

    private parseNamespaceDeclaration(): ASTNode {
        const nameToken = this.consume(TokenType.IDENTIFIER, "Expected namespace name");
        
        // Allow newlines before opening brace
        while (this.match(TokenType.NEWLINE)) {
            // consume newlines
        }
        
        this.consume(TokenType.LBRACE, "Expected '{' before namespace body");
        
        const members: ASTNode[] = [];
        while (!this.check(TokenType.RBRACE) && !this.isAtEnd()) {
            if (this.match(TokenType.NEWLINE)) continue;
            
            const member = this.parseStatement();
            if (member) {
                members.push(member);
            }
        }
        
        this.consume(TokenType.RBRACE, "Expected '}' after namespace body");
        
        return {
            type: 'NamespaceDeclaration',
            name: nameToken.value,
            children: members.filter((m): m is ASTNode => m !== null),
            line: nameToken.line,
            column: nameToken.column
        };
    }

    private parseIfStatement(): ASTNode {
        this.consume(TokenType.LPAREN, "Expected '(' after 'if'");
        const condition = this.parseExpression();
        this.consume(TokenType.RPAREN, "Expected ')' after if condition");
        
        // Allow newlines before statement
        while (this.match(TokenType.NEWLINE)) {
            // consume newlines
        }
        
        const thenBranch = this.parseStatement();
        let elseBranch: ASTNode | null = null;
        
        if (this.match(TokenType.ELSE)) {
            // Allow newlines after else
            while (this.match(TokenType.NEWLINE)) {
                // consume newlines
            }
            elseBranch = this.parseStatement();
        }
        
        const children: ASTNode[] = [condition];
        if (thenBranch) children.push(thenBranch);
        if (elseBranch) children.push(elseBranch);
        
        return {
            type: 'IfStatement',
            children
        };
    }

    private parseWhileStatement(): ASTNode {
        this.consume(TokenType.LPAREN, "Expected '(' after 'while'");
        const condition = this.parseExpression();
        this.consume(TokenType.RPAREN, "Expected ')' after while condition");
        
        // Allow newlines before statement
        while (this.match(TokenType.NEWLINE)) {
            // consume newlines
        }
        
        const body = this.parseStatement();
        
        return {
            type: 'WhileStatement',
            children: [condition, ...(body ? [body] : [])]
        };
    }

    private parseReturnStatement(): ASTNode {
        let value: ASTNode | null = null;
        if (!this.check(TokenType.NEWLINE) && !this.check(TokenType.SEMICOLON)) {
            value = this.parseExpression();
        }
        
        this.consumeNewlineOrSemicolon();
        
        return {
            type: 'ReturnStatement',
            children: value ? [value] : []
        };
    }

    private parseBlock(): ASTNode {
        const statements: ASTNode[] = [];
        
        while (!this.check(TokenType.RBRACE) && !this.isAtEnd()) {
            if (this.match(TokenType.NEWLINE)) continue;
            
            const stmt = this.parseStatement();
            if (stmt) {
                statements.push(stmt);
            }
        }
        
        this.consume(TokenType.RBRACE, "Expected '}' after block");
        
        return {
            type: 'Block',
            children: statements
        };
    }

    private parseExpression(): ASTNode {
        return this.parseEquality();
    }

    private parseEquality(): ASTNode {
        let expr = this.parseComparison();
        
        while (this.match(TokenType.EQUAL, TokenType.NOT_EQUAL)) {
            const operator = this.previous();
            const right = this.parseComparison();
            expr = {
                type: 'BinaryExpression',
                value: operator.value,
                children: [expr, right]
            };
        }
        
        return expr;
    }

    private parseComparison(): ASTNode {
        let expr = this.parseTerm();
        
        while (this.match(TokenType.GREATER_THAN, TokenType.GREATER_EQUAL, TokenType.LESS_THAN, TokenType.LESS_EQUAL)) {
            const operator = this.previous();
            const right = this.parseTerm();
            expr = {
                type: 'BinaryExpression',
                value: operator.value,
                children: [expr, right]
            };
        }
        
        return expr;
    }

    private parseTerm(): ASTNode {
        let expr = this.parseFactor();
        
        while (this.match(TokenType.MINUS, TokenType.PLUS)) {
            const operator = this.previous();
            const right = this.parseFactor();
            expr = {
                type: 'BinaryExpression',
                value: operator.value,
                children: [expr, right]
            };
        }
        
        return expr;
    }

    private parseFactor(): ASTNode {
        let expr = this.parseUnary();
        
        while (this.match(TokenType.DIVIDE, TokenType.MULTIPLY, TokenType.MODULO)) {
            const operator = this.previous();
            const right = this.parseUnary();
            expr = {
                type: 'BinaryExpression',
                value: operator.value,
                children: [expr, right]
            };
        }
        
        return expr;
    }

    private parseUnary(): ASTNode {
        if (this.match(TokenType.MINUS, TokenType.PLUS)) {
            const operator = this.previous();
            const right = this.parseUnary();
            return {
                type: 'UnaryExpression',
                value: operator.value,
                children: [right]
            };
        }
        
        return this.parseCall();
    }

    private parseCall(): ASTNode {
        let expr = this.parsePrimary();
        
        while (true) {
            if (this.match(TokenType.LPAREN)) {
                expr = this.finishCall(expr);
            } else if (this.match(TokenType.DOT)) {
                const name = this.consume(TokenType.IDENTIFIER, "Expected property name after '.'");
                expr = {
                    type: 'MemberExpression',
                    children: [expr],
                    name: name.value
                };
            } else {
                break;
            }
        }
        
        return expr;
    }

    private finishCall(callee: ASTNode): ASTNode {
        const args: ASTNode[] = [];
        
        if (!this.check(TokenType.RPAREN)) {
            do {
                args.push(this.parseExpression());
            } while (this.match(TokenType.COMMA));
        }
        
        this.consume(TokenType.RPAREN, "Expected ')' after arguments");
        
        return {
            type: 'CallExpression',
            children: [callee, ...args]
        };
    }

    private parsePrimary(): ASTNode {
        if (this.match(TokenType.BOOLEAN)) {
            return {
                type: 'Literal',
                value: this.previous().value === 'true'
            };
        }
        
        if (this.match(TokenType.NUMBER)) {
            const value = this.previous().value;
            return {
                type: 'Literal',
                value: value.includes('.') ? parseFloat(value) : parseInt(value)
            };
        }
        
        if (this.match(TokenType.STRING)) {
            return {
                type: 'Literal',
                value: this.previous().value
            };
        }
        
        if (this.match(TokenType.IDENTIFIER)) {
            return {
                type: 'Identifier',
                name: this.previous().value
            };
        }
        
        if (this.match(TokenType.LPAREN)) {
            const expr = this.parseExpression();
            this.consume(TokenType.RPAREN, "Expected ')' after expression");
            return expr;
        }
        
        throw new Error(`Unexpected token: ${this.peek().value}`);
    }

    private match(...types: TokenType[]): boolean {
        for (const type of types) {
            if (this.check(type)) {
                this.advance();
                return true;
            }
        }
        return false;
    }

    private check(type: TokenType): boolean {
        if (this.isAtEnd()) return false;
        return this.peek().type === type;
    }

    private advance(): Token {
        if (!this.isAtEnd()) this.current++;
        return this.previous();
    }

    private isAtEnd(): boolean {
        return this.peek().type === TokenType.EOF;
    }

    private peek(): Token {
        return this.tokens[this.current];
    }

    private previous(): Token {
        return this.tokens[this.current - 1];
    }

    private consume(type: TokenType, message: string): Token {
        if (this.check(type)) return this.advance();
        
        const currentToken = this.peek();
        const error = new Error(`${message} at line ${currentToken.line}, column ${currentToken.column}. Got '${currentToken.value}'`);
        (error as any).line = currentToken.line;
        (error as any).column = currentToken.column;
        throw error;
    }

    private consumeNewlineOrSemicolon(): void {
        if (this.match(TokenType.NEWLINE, TokenType.SEMICOLON)) {
            return;
        }
        // Allow end of file or closing brace without explicit terminator
        if (this.check(TokenType.EOF) || this.check(TokenType.RBRACE)) {
            return;
        }
    }

    private skipToNextStatement(): void {
        while (!this.isAtEnd() && !this.check(TokenType.NEWLINE) && !this.check(TokenType.SEMICOLON) && !this.check(TokenType.RBRACE)) {
            this.advance();
        }
        if (this.match(TokenType.NEWLINE, TokenType.SEMICOLON)) {
            // consumed
        }
    }

    private skipToToken(tokenType: TokenType): void {
        while (!this.isAtEnd() && !this.check(tokenType)) {
            this.advance();
        }
    }

    private skipBlock(): void {
        if (!this.check(TokenType.LBRACE)) return;
        
        this.advance(); // consume {
        let braceCount = 1;
        
        while (!this.isAtEnd() && braceCount > 0) {
            if (this.check(TokenType.LBRACE)) {
                braceCount++;
            } else if (this.check(TokenType.RBRACE)) {
                braceCount--;
            }
            this.advance();
        }
    }
}
