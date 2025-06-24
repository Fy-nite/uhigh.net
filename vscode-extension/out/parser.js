"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UhighParser = void 0;
const lexer_1 = require("./lexer");
class UhighParser {
    constructor() {
        this.tokens = [];
        this.current = 0;
        this.parseDepth = 0;
        this.maxParseDepth = 100;
    }
    parse(tokens) {
        this.tokens = tokens.filter(t => t.type !== lexer_1.TokenType.WHITESPACE && t.type !== lexer_1.TokenType.COMMENT);
        this.current = 0;
        this.parseDepth = 0;
        // Quick safety check - if too many tokens, don't parse
        if (this.tokens.length > 5000) {
            throw new Error('File too complex to parse');
        }
        return this.parseProgram();
    }
    parseOptimized(tokens) {
        this.tokens = tokens.filter(t => t.type !== lexer_1.TokenType.WHITESPACE && t.type !== lexer_1.TokenType.COMMENT);
        this.current = 0;
        this.parseDepth = 0;
        // Even more restrictive limits for optimized parsing
        if (this.tokens.length > 1000) {
            throw new Error('File too complex for optimized parsing');
        }
        return this.parseProgramOptimized();
    }
    parseProgram() {
        const children = [];
        let statementCount = 0;
        while (!this.isAtEnd() && statementCount < 1000) { // Limit statements
            if (this.match(lexer_1.TokenType.NEWLINE))
                continue;
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
    parseProgramOptimized() {
        const children = [];
        let statementCount = 0;
        while (!this.isAtEnd() && statementCount < 100) { // Reduced limit
            if (this.match(lexer_1.TokenType.NEWLINE))
                continue;
            try {
                const stmt = this.parseStatementOptimized();
                if (stmt) {
                    children.push(stmt);
                }
            }
            catch (error) {
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
    parseStatement() {
        // Prevent infinite recursion
        this.parseDepth++;
        if (this.parseDepth > this.maxParseDepth) {
            throw new Error('Maximum parse depth exceeded');
        }
        try {
            // Handle multiple access modifiers
            const accessModifiers = [];
            while (this.match(lexer_1.TokenType.PUBLIC, lexer_1.TokenType.PRIVATE, lexer_1.TokenType.PROTECTED, lexer_1.TokenType.INTERNAL, lexer_1.TokenType.STATIC, lexer_1.TokenType.ABSTRACT, lexer_1.TokenType.VIRTUAL, lexer_1.TokenType.OVERRIDE, lexer_1.TokenType.SEALED, lexer_1.TokenType.READONLY)) {
                accessModifiers.push(this.previous().value);
                // Prevent infinite loop - limit to reasonable number of modifiers
                if (accessModifiers.length > 5) {
                    break;
                }
            }
            // Handle access modifiers
            let accessModifier = null;
            if (this.match(lexer_1.TokenType.PUBLIC, lexer_1.TokenType.PRIVATE, lexer_1.TokenType.PROTECTED, lexer_1.TokenType.INTERNAL, lexer_1.TokenType.STATIC, lexer_1.TokenType.ABSTRACT, lexer_1.TokenType.VIRTUAL, lexer_1.TokenType.OVERRIDE, lexer_1.TokenType.SEALED, lexer_1.TokenType.READONLY)) {
                accessModifier = this.previous().value;
            }
            if (this.match(lexer_1.TokenType.FUNC)) {
                const func = this.parseFunctionDeclaration();
                if (accessModifier) {
                    func.value = accessModifier;
                }
                return func;
            }
            // If we consumed an access modifier but no valid statement follows, skip to next line
            if (accessModifier) {
                // Skip to end of line to recover from error
                while (!this.check(lexer_1.TokenType.NEWLINE) && !this.check(lexer_1.TokenType.EOF)) {
                    this.advance();
                }
                return null;
            }
            if (this.match(lexer_1.TokenType.VAR, lexer_1.TokenType.CONST)) {
                return this.parseVariableDeclaration();
            }
            if (this.match(lexer_1.TokenType.CLASS)) {
                return this.parseClassDeclaration();
            }
            if (this.match(lexer_1.TokenType.NAMESPACE)) {
                return this.parseNamespaceDeclaration();
            }
            if (this.match(lexer_1.TokenType.IF)) {
                return this.parseIfStatement();
            }
            if (this.match(lexer_1.TokenType.WHILE)) {
                return this.parseWhileStatement();
            }
            if (this.match(lexer_1.TokenType.RETURN)) {
                return this.parseReturnStatement();
            }
            // Expression statement
            const expr = this.parseExpression();
            this.consumeNewlineOrSemicolon();
            return expr;
        }
        finally {
            this.parseDepth--;
        }
    }
    parseStatementOptimized() {
        // Simplified statement parsing for performance
        this.parseDepth++;
        if (this.parseDepth > 20) { // Much lower depth limit
            throw new Error('Parse depth exceeded in optimized mode');
        }
        try {
            // Handle only the most common statements in optimized mode
            if (this.match(lexer_1.TokenType.FUNC)) {
                return this.parseFunctionDeclarationOptimized();
            }
            if (this.match(lexer_1.TokenType.VAR, lexer_1.TokenType.CONST)) {
                return this.parseVariableDeclarationOptimized();
            }
            if (this.match(lexer_1.TokenType.CLASS)) {
                return this.parseClassDeclarationOptimized();
            }
            // Skip other statement types in optimized mode
            this.skipToNextStatement();
            return null;
        }
        finally {
            this.parseDepth--;
        }
    }
    parseFunctionDeclaration() {
        // Add safety check to prevent infinite recursion
        if (!this.check(lexer_1.TokenType.IDENTIFIER)) {
            throw new Error("Expected function name after 'func' keyword");
        }
        const nameToken = this.consume(lexer_1.TokenType.IDENTIFIER, "Expected function name");
        // Ensure we have opening parenthesis
        if (!this.check(lexer_1.TokenType.LPAREN)) {
            throw new Error(`Expected '(' after function name '${nameToken.value}'`);
        }
        this.consume(lexer_1.TokenType.LPAREN, "Expected '(' after function name");
        const parameters = [];
        if (!this.check(lexer_1.TokenType.RPAREN)) {
            let paramCount = 0;
            do {
                // Safety check to prevent infinite parameter parsing
                if (paramCount > 50) {
                    throw new Error("Too many parameters in function declaration");
                }
                if (this.check(lexer_1.TokenType.IDENTIFIER)) {
                    const param = this.advance();
                    let paramType;
                    // Handle type annotation: param: type
                    if (this.match(lexer_1.TokenType.COLON)) {
                        if (this.check(lexer_1.TokenType.IDENTIFIER)) {
                            const typeToken = this.advance();
                            paramType = typeToken.value;
                        }
                        else {
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
                }
                else {
                    throw new Error("Expected parameter name in function declaration");
                }
            } while (this.match(lexer_1.TokenType.COMMA));
        }
        this.consume(lexer_1.TokenType.RPAREN, "Expected ')' after parameters");
        // Handle return type annotation: ): returnType
        let returnType;
        if (this.match(lexer_1.TokenType.COLON)) {
            if (this.check(lexer_1.TokenType.IDENTIFIER)) {
                const typeToken = this.advance();
                returnType = typeToken.value;
            }
            else {
                throw new Error("Expected return type after ':'");
            }
        }
        // Allow newlines before opening brace
        let newlineCount = 0;
        while (this.match(lexer_1.TokenType.NEWLINE)) {
            newlineCount++;
            // Prevent infinite newline consumption
            if (newlineCount > 10) {
                break;
            }
        }
        // Function body is required
        if (!this.check(lexer_1.TokenType.LBRACE)) {
            throw new Error(`Expected '{' before function body for function '${nameToken.value}'`);
        }
        this.consume(lexer_1.TokenType.LBRACE, "Expected '{' before function body");
        const body = this.parseBlock();
        const result = {
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
    parseFunctionDeclarationOptimized() {
        if (!this.check(lexer_1.TokenType.IDENTIFIER)) {
            throw new Error("Expected function name");
        }
        const nameToken = this.advance();
        // Skip parameter parsing in optimized mode - just find the opening brace
        this.skipToToken(lexer_1.TokenType.LBRACE);
        if (this.check(lexer_1.TokenType.LBRACE)) {
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
    parseVariableDeclaration() {
        const kindToken = this.previous();
        const nameToken = this.consume(lexer_1.TokenType.IDENTIFIER, "Expected variable name");
        let initializer = null;
        if (this.match(lexer_1.TokenType.ASSIGN)) {
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
    parseVariableDeclarationOptimized() {
        const kindToken = this.previous();
        if (!this.check(lexer_1.TokenType.IDENTIFIER)) {
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
    parseClassDeclaration() {
        const nameToken = this.consume(lexer_1.TokenType.IDENTIFIER, "Expected class name");
        // Allow newlines before opening brace
        while (this.match(lexer_1.TokenType.NEWLINE)) {
            // consume newlines
        }
        this.consume(lexer_1.TokenType.LBRACE, "Expected '{' before class body");
        const members = [];
        while (!this.check(lexer_1.TokenType.RBRACE) && !this.isAtEnd()) {
            if (this.match(lexer_1.TokenType.NEWLINE))
                continue;
            const member = this.parseStatement();
            if (member) {
                members.push(member);
            }
        }
        this.consume(lexer_1.TokenType.RBRACE, "Expected '}' after class body");
        return {
            type: 'ClassDeclaration',
            name: nameToken.value,
            children: members.filter((m) => m !== null),
            line: nameToken.line,
            column: nameToken.column
        };
    }
    parseClassDeclarationOptimized() {
        if (!this.check(lexer_1.TokenType.IDENTIFIER)) {
            throw new Error("Expected class name");
        }
        const nameToken = this.advance();
        // Skip class body
        this.skipToToken(lexer_1.TokenType.LBRACE);
        if (this.check(lexer_1.TokenType.LBRACE)) {
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
    parseNamespaceDeclaration() {
        const nameToken = this.consume(lexer_1.TokenType.IDENTIFIER, "Expected namespace name");
        // Allow newlines before opening brace
        while (this.match(lexer_1.TokenType.NEWLINE)) {
            // consume newlines
        }
        this.consume(lexer_1.TokenType.LBRACE, "Expected '{' before namespace body");
        const members = [];
        while (!this.check(lexer_1.TokenType.RBRACE) && !this.isAtEnd()) {
            if (this.match(lexer_1.TokenType.NEWLINE))
                continue;
            const member = this.parseStatement();
            if (member) {
                members.push(member);
            }
        }
        this.consume(lexer_1.TokenType.RBRACE, "Expected '}' after namespace body");
        return {
            type: 'NamespaceDeclaration',
            name: nameToken.value,
            children: members.filter((m) => m !== null),
            line: nameToken.line,
            column: nameToken.column
        };
    }
    parseIfStatement() {
        this.consume(lexer_1.TokenType.LPAREN, "Expected '(' after 'if'");
        const condition = this.parseExpression();
        this.consume(lexer_1.TokenType.RPAREN, "Expected ')' after if condition");
        // Allow newlines before statement
        while (this.match(lexer_1.TokenType.NEWLINE)) {
            // consume newlines
        }
        const thenBranch = this.parseStatement();
        let elseBranch = null;
        if (this.match(lexer_1.TokenType.ELSE)) {
            // Allow newlines after else
            while (this.match(lexer_1.TokenType.NEWLINE)) {
                // consume newlines
            }
            elseBranch = this.parseStatement();
        }
        const children = [condition];
        if (thenBranch)
            children.push(thenBranch);
        if (elseBranch)
            children.push(elseBranch);
        return {
            type: 'IfStatement',
            children
        };
    }
    parseWhileStatement() {
        this.consume(lexer_1.TokenType.LPAREN, "Expected '(' after 'while'");
        const condition = this.parseExpression();
        this.consume(lexer_1.TokenType.RPAREN, "Expected ')' after while condition");
        // Allow newlines before statement
        while (this.match(lexer_1.TokenType.NEWLINE)) {
            // consume newlines
        }
        const body = this.parseStatement();
        return {
            type: 'WhileStatement',
            children: [condition, ...(body ? [body] : [])]
        };
    }
    parseReturnStatement() {
        let value = null;
        if (!this.check(lexer_1.TokenType.NEWLINE) && !this.check(lexer_1.TokenType.SEMICOLON)) {
            value = this.parseExpression();
        }
        this.consumeNewlineOrSemicolon();
        return {
            type: 'ReturnStatement',
            children: value ? [value] : []
        };
    }
    parseBlock() {
        const statements = [];
        while (!this.check(lexer_1.TokenType.RBRACE) && !this.isAtEnd()) {
            if (this.match(lexer_1.TokenType.NEWLINE))
                continue;
            const stmt = this.parseStatement();
            if (stmt) {
                statements.push(stmt);
            }
        }
        this.consume(lexer_1.TokenType.RBRACE, "Expected '}' after block");
        return {
            type: 'Block',
            children: statements
        };
    }
    parseExpression() {
        return this.parseEquality();
    }
    parseEquality() {
        let expr = this.parseComparison();
        while (this.match(lexer_1.TokenType.EQUAL, lexer_1.TokenType.NOT_EQUAL)) {
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
    parseComparison() {
        let expr = this.parseTerm();
        while (this.match(lexer_1.TokenType.GREATER_THAN, lexer_1.TokenType.GREATER_EQUAL, lexer_1.TokenType.LESS_THAN, lexer_1.TokenType.LESS_EQUAL)) {
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
    parseTerm() {
        let expr = this.parseFactor();
        while (this.match(lexer_1.TokenType.MINUS, lexer_1.TokenType.PLUS)) {
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
    parseFactor() {
        let expr = this.parseUnary();
        while (this.match(lexer_1.TokenType.DIVIDE, lexer_1.TokenType.MULTIPLY, lexer_1.TokenType.MODULO)) {
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
    parseUnary() {
        if (this.match(lexer_1.TokenType.MINUS, lexer_1.TokenType.PLUS)) {
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
    parseCall() {
        let expr = this.parsePrimary();
        while (true) {
            if (this.match(lexer_1.TokenType.LPAREN)) {
                expr = this.finishCall(expr);
            }
            else if (this.match(lexer_1.TokenType.DOT)) {
                const name = this.consume(lexer_1.TokenType.IDENTIFIER, "Expected property name after '.'");
                expr = {
                    type: 'MemberExpression',
                    children: [expr],
                    name: name.value
                };
            }
            else {
                break;
            }
        }
        return expr;
    }
    finishCall(callee) {
        const args = [];
        if (!this.check(lexer_1.TokenType.RPAREN)) {
            do {
                args.push(this.parseExpression());
            } while (this.match(lexer_1.TokenType.COMMA));
        }
        this.consume(lexer_1.TokenType.RPAREN, "Expected ')' after arguments");
        return {
            type: 'CallExpression',
            children: [callee, ...args]
        };
    }
    parsePrimary() {
        if (this.match(lexer_1.TokenType.BOOLEAN)) {
            return {
                type: 'Literal',
                value: this.previous().value === 'true'
            };
        }
        if (this.match(lexer_1.TokenType.NUMBER)) {
            const value = this.previous().value;
            return {
                type: 'Literal',
                value: value.includes('.') ? parseFloat(value) : parseInt(value)
            };
        }
        if (this.match(lexer_1.TokenType.STRING)) {
            return {
                type: 'Literal',
                value: this.previous().value
            };
        }
        if (this.match(lexer_1.TokenType.IDENTIFIER)) {
            return {
                type: 'Identifier',
                name: this.previous().value
            };
        }
        if (this.match(lexer_1.TokenType.LPAREN)) {
            const expr = this.parseExpression();
            this.consume(lexer_1.TokenType.RPAREN, "Expected ')' after expression");
            return expr;
        }
        throw new Error(`Unexpected token: ${this.peek().value}`);
    }
    match(...types) {
        for (const type of types) {
            if (this.check(type)) {
                this.advance();
                return true;
            }
        }
        return false;
    }
    check(type) {
        if (this.isAtEnd())
            return false;
        return this.peek().type === type;
    }
    advance() {
        if (!this.isAtEnd())
            this.current++;
        return this.previous();
    }
    isAtEnd() {
        return this.peek().type === lexer_1.TokenType.EOF;
    }
    peek() {
        return this.tokens[this.current];
    }
    previous() {
        return this.tokens[this.current - 1];
    }
    consume(type, message) {
        if (this.check(type))
            return this.advance();
        const currentToken = this.peek();
        const error = new Error(`${message} at line ${currentToken.line}, column ${currentToken.column}. Got '${currentToken.value}'`);
        error.line = currentToken.line;
        error.column = currentToken.column;
        throw error;
    }
    consumeNewlineOrSemicolon() {
        if (this.match(lexer_1.TokenType.NEWLINE, lexer_1.TokenType.SEMICOLON)) {
            return;
        }
        // Allow end of file or closing brace without explicit terminator
        if (this.check(lexer_1.TokenType.EOF) || this.check(lexer_1.TokenType.RBRACE)) {
            return;
        }
    }
    skipToNextStatement() {
        while (!this.isAtEnd() && !this.check(lexer_1.TokenType.NEWLINE) && !this.check(lexer_1.TokenType.SEMICOLON) && !this.check(lexer_1.TokenType.RBRACE)) {
            this.advance();
        }
        if (this.match(lexer_1.TokenType.NEWLINE, lexer_1.TokenType.SEMICOLON)) {
            // consumed
        }
    }
    skipToToken(tokenType) {
        while (!this.isAtEnd() && !this.check(tokenType)) {
            this.advance();
        }
    }
    skipBlock() {
        if (!this.check(lexer_1.TokenType.LBRACE))
            return;
        this.advance(); // consume {
        let braceCount = 1;
        while (!this.isAtEnd() && braceCount > 0) {
            if (this.check(lexer_1.TokenType.LBRACE)) {
                braceCount++;
            }
            else if (this.check(lexer_1.TokenType.RBRACE)) {
                braceCount--;
            }
            this.advance();
        }
    }
}
exports.UhighParser = UhighParser;
//# sourceMappingURL=parser.js.map