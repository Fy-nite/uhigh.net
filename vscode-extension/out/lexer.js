"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UhighLexer = exports.TokenType = void 0;
var TokenType;
(function (TokenType) {
    // Literals
    TokenType["NUMBER"] = "NUMBER";
    TokenType["STRING"] = "STRING";
    TokenType["IDENTIFIER"] = "IDENTIFIER";
    TokenType["NEW"] = "NEW";
    // Keywords
    TokenType["VAR"] = "VAR";
    TokenType["CONST"] = "CONST";
    TokenType["FUNC"] = "FUNC";
    TokenType["RETURN"] = "RETURN";
    TokenType["IF"] = "IF";
    TokenType["ELSE"] = "ELSE";
    TokenType["WHILE"] = "WHILE";
    TokenType["FOR"] = "FOR";
    TokenType["IN"] = "IN";
    TokenType["PRINT"] = "PRINT";
    TokenType["INCLUDE"] = "INCLUDE";
    TokenType["TRY"] = "TRY";
    TokenType["CATCH"] = "CATCH";
    TokenType["THROW"] = "THROW";
    TokenType["FIELD"] = "FIELD";
    TokenType["MATCH"] = "MATCH";
    TokenType["TRUE"] = "TRUE";
    TokenType["FALSE"] = "FALSE";
    TokenType["INT"] = "INT";
    TokenType["FLOAT"] = "FLOAT";
    TokenType["STRING_TYPE"] = "STRING_TYPE";
    TokenType["BOOL"] = "BOOL";
    TokenType["VOID"] = "VOID";
    TokenType["SHARP"] = "SHARP";
    TokenType["RANGE"] = "RANGE";
    TokenType["BREAK"] = "BREAK";
    TokenType["CONTINUE"] = "CONTINUE";
    TokenType["CLASS"] = "CLASS";
    TokenType["NAMESPACE"] = "NAMESPACE";
    TokenType["IMPORT"] = "IMPORT";
    TokenType["FROM"] = "FROM";
    TokenType["THIS"] = "THIS";
    TokenType["GET"] = "GET";
    TokenType["SET"] = "SET";
    TokenType["ENUM"] = "ENUM";
    TokenType["INTERFACE"] = "INTERFACE";
    TokenType["EXTENSION"] = "EXTENSION";
    TokenType["RECORD"] = "RECORD";
    TokenType["SWITCH"] = "SWITCH";
    TokenType["CASE"] = "CASE";
    TokenType["DEFAULT"] = "DEFAULT";
    TokenType["STRUCT"] = "STRUCT";
    TokenType["UNION"] = "UNION";
    TokenType["MODULE"] = "MODULE";
    TokenType["USE"] = "USE";
    TokenType["LET"] = "LET";
    TokenType["MUT"] = "MUT";
    TokenType["ASYNC"] = "ASYNC";
    TokenType["AWAIT"] = "AWAIT";
    TokenType["MACRO"] = "MACRO";
    TokenType["YIELD"] = "YIELD";
    // Access Modifiers and Keywords
    TokenType["PUBLIC"] = "PUBLIC";
    TokenType["PRIVATE"] = "PRIVATE";
    TokenType["PROTECTED"] = "PROTECTED";
    TokenType["INTERNAL"] = "INTERNAL";
    TokenType["STATIC"] = "STATIC";
    TokenType["ABSTRACT"] = "ABSTRACT";
    TokenType["VIRTUAL"] = "VIRTUAL";
    TokenType["OVERRIDE"] = "OVERRIDE";
    TokenType["SEALED"] = "SEALED";
    TokenType["READONLY"] = "READONLY";
    // Nullable and null operators
    TokenType["QUESTION"] = "QUESTION";
    TokenType["QUESTION_QUESTION"] = "QUESTION_QUESTION";
    TokenType["QUESTION_DOT"] = "QUESTION_DOT";
    TokenType["EXCLAMATION_MARK"] = "EXCLAMATION_MARK";
    // String interpolation
    TokenType["INTERPOLATED_STRING_START"] = "INTERPOLATED_STRING_START";
    TokenType["INTERPOLATED_STRING_MID"] = "INTERPOLATED_STRING_MID";
    TokenType["INTERPOLATED_STRING_END"] = "INTERPOLATED_STRING_END";
    TokenType["INTERPOLATION_START"] = "INTERPOLATION_START";
    TokenType["INTERPOLATION_END"] = "INTERPOLATION_END";
    // Range operators
    TokenType["DOT_DOT"] = "DOT_DOT";
    TokenType["DOT_DOT_LESS"] = "DOT_DOT_LESS";
    // Operators
    TokenType["PLUS"] = "PLUS";
    TokenType["MINUS"] = "MINUS";
    TokenType["MULTIPLY"] = "MULTIPLY";
    TokenType["DIVIDE"] = "DIVIDE";
    TokenType["MODULO"] = "MODULO";
    TokenType["ASSIGN"] = "ASSIGN";
    TokenType["EQUAL"] = "EQUAL";
    TokenType["NOT_EQUAL"] = "NOT_EQUAL";
    TokenType["LESS_THAN"] = "LESS_THAN";
    TokenType["GREATER_THAN"] = "GREATER_THAN";
    TokenType["LESS_EQUAL"] = "LESS_EQUAL";
    TokenType["GREATER_EQUAL"] = "GREATER_EQUAL";
    TokenType["AND"] = "AND";
    TokenType["OR"] = "OR";
    TokenType["NOT"] = "NOT";
    TokenType["INCREMENT"] = "INCREMENT";
    TokenType["DECREMENT"] = "DECREMENT";
    TokenType["PLUS_ASSIGN"] = "PLUS_ASSIGN";
    TokenType["MINUS_ASSIGN"] = "MINUS_ASSIGN";
    TokenType["MULTIPLY_ASSIGN"] = "MULTIPLY_ASSIGN";
    TokenType["DIVIDE_ASSIGN"] = "DIVIDE_ASSIGN";
    TokenType["ARROW"] = "ARROW";
    TokenType["UNDERSCORE"] = "UNDERSCORE";
    // Punctuation
    TokenType["LPAREN"] = "LPAREN";
    TokenType["RPAREN"] = "RPAREN";
    TokenType["LBRACE"] = "LBRACE";
    TokenType["RBRACE"] = "RBRACE";
    TokenType["LEFT_BRACKET"] = "LEFT_BRACKET";
    TokenType["RIGHT_BRACKET"] = "RIGHT_BRACKET";
    TokenType["COMMA"] = "COMMA";
    TokenType["SEMICOLON"] = "SEMICOLON";
    TokenType["COLON"] = "COLON";
    TokenType["AS"] = "AS";
    TokenType["DOT"] = "DOT";
    // Special
    TokenType["COMMENT"] = "COMMENT";
    TokenType["NEWLINE"] = "NEWLINE";
    TokenType["EOF"] = "EOF";
    TokenType["BOOLEAN"] = "BOOLEAN";
    TokenType["WHITESPACE"] = "WHITESPACE";
    // Attributes
    TokenType["ATTRIBUTE"] = "ATTRIBUTE";
})(TokenType = exports.TokenType || (exports.TokenType = {}));
class UhighLexer {
    constructor() {
        // Simplified keyword map for performance
        this.keywordsFast = new Set([
            'var', 'const', 'func', 'return', 'if', 'else', 'while', 'for', 'class', 'namespace',
            'public', 'private', 'static', 'true', 'false', 'void', 'int', 'string', 'bool'
        ]);
        this.keywords = new Map([
            ['var', TokenType.VAR],
            ['const', TokenType.CONST],
            ['func', TokenType.FUNC],
            ['return', TokenType.RETURN],
            ['if', TokenType.IF],
            ['else', TokenType.ELSE],
            ['while', TokenType.WHILE],
            ['for', TokenType.FOR],
            ['in', TokenType.IN],
            ['print', TokenType.PRINT],
            ['include', TokenType.INCLUDE],
            ['try', TokenType.TRY],
            ['catch', TokenType.CATCH],
            ['throw', TokenType.THROW],
            ['field', TokenType.FIELD],
            ['match', TokenType.MATCH],
            ['true', TokenType.TRUE],
            ['false', TokenType.FALSE],
            ['int', TokenType.INT],
            ['float', TokenType.FLOAT],
            ['string', TokenType.STRING_TYPE],
            ['bool', TokenType.BOOL],
            ['void', TokenType.VOID],
            ['sharp', TokenType.SHARP],
            ['range', TokenType.RANGE],
            ['break', TokenType.BREAK],
            ['continue', TokenType.CONTINUE],
            ['class', TokenType.CLASS],
            ['namespace', TokenType.NAMESPACE],
            ['import', TokenType.IMPORT],
            ['from', TokenType.FROM],
            ['this', TokenType.THIS],
            ['get', TokenType.GET],
            ['set', TokenType.SET],
            ['enum', TokenType.ENUM],
            ['interface', TokenType.INTERFACE],
            ['extension', TokenType.EXTENSION],
            ['record', TokenType.RECORD],
            ['switch', TokenType.SWITCH],
            ['case', TokenType.CASE],
            ['default', TokenType.DEFAULT],
            ['struct', TokenType.STRUCT],
            ['union', TokenType.UNION],
            ['module', TokenType.MODULE],
            ['use', TokenType.USE],
            ['let', TokenType.LET],
            ['mut', TokenType.MUT],
            ['async', TokenType.ASYNC],
            ['await', TokenType.AWAIT],
            ['macro', TokenType.MACRO],
            ['yield', TokenType.YIELD],
            ['public', TokenType.PUBLIC],
            ['private', TokenType.PRIVATE],
            ['protected', TokenType.PROTECTED],
            ['internal', TokenType.INTERNAL],
            ['static', TokenType.STATIC],
            ['abstract', TokenType.ABSTRACT],
            ['virtual', TokenType.VIRTUAL],
            ['override', TokenType.OVERRIDE],
            ['sealed', TokenType.SEALED],
            ['readonly', TokenType.READONLY],
            ['new', TokenType.NEW],
            ['as', TokenType.AS]
        ]);
    }
    tokenize(input) {
        return this.tokenizeOptimized(input, false);
    }
    tokenizeOptimized(input, fastMode = true) {
        const tokens = [];
        let current = 0;
        let line = 1;
        let column = 1;
        let iterations = 0;
        // Reduced max iterations for performance
        const maxIterations = fastMode ? input.length : input.length * 2;
        while (current < input.length && iterations < maxIterations) {
            iterations++;
            const char = input[current];
            // Early exit for very long lines during fast tokenization
            if (fastMode && column > 200) {
                // Skip to next line
                while (current < input.length && input[current] !== '\n') {
                    current++;
                }
                continue;
            }
            // Skip whitespace (including carriage returns)
            if (char === ' ' || char === '\t' || char === '\r') {
                current++;
                column++;
                continue;
            }
            // Newlines
            if (char === '\n') {
                if (!fastMode || tokens.length < 1000) { // Limit tokens in fast mode
                    tokens.push({ type: TokenType.NEWLINE, value: char, line, column });
                }
                current++;
                line++;
                column = 1;
                continue;
            }
            // Comments - simplified in fast mode
            if (char === '/' && input[current + 1] === '/') {
                // Skip entire comment in fast mode
                while (current < input.length && input[current] !== '\n') {
                    current++;
                    column++;
                }
                continue;
            }
            // Strings - simplified processing
            if (char === '"' || char === "'") {
                const result = this.tokenizeString(input, current, line, column, char, fastMode);
                if (result) {
                    if (!fastMode || tokens.length < 1000) {
                        tokens.push(result.token);
                    }
                    current = result.newCurrent;
                    column = result.newColumn;
                    continue;
                }
            }
            // Numbers
            if (this.isDigit(char)) {
                const result = this.tokenizeNumber(input, current, line, column, fastMode);
                if (result) {
                    if (!fastMode || tokens.length < 1000) {
                        tokens.push(result.token);
                    }
                    current = result.newCurrent;
                    column = result.newColumn;
                    continue;
                }
            }
            // Identifiers and keywords - optimized
            if (this.isAlpha(char)) {
                const result = this.tokenizeIdentifier(input, current, line, column, fastMode);
                if (result) {
                    if (!fastMode || tokens.length < 1000) {
                        tokens.push(result.token);
                    }
                    current = result.newCurrent;
                    column = result.newColumn;
                    continue;
                }
            }
            // Operators - simplified in fast mode
            if (fastMode) {
                // Only handle single character tokens in fast mode
                const singleChar = this.getSingleCharTokenFast(char);
                if (singleChar) {
                    tokens.push({ type: singleChar, value: char, line, column });
                    current++;
                    column++;
                    continue;
                }
            }
            else {
                // Full operator parsing in normal mode
                // ...existing operator parsing code...
            }
            // Skip unrecognized characters
            current++;
            column++;
        }
        tokens.push({ type: TokenType.EOF, value: '', line, column });
        return tokens;
    }
    tokenizeString(input, start, line, startColumn, quote, fastMode) {
        let current = start + 1; // Skip opening quote
        let column = startColumn + 1;
        let value = '';
        while (current < input.length && input[current] !== quote) {
            if (fastMode && value.length > 100) {
                // Truncate very long strings in fast mode
                break;
            }
            if (input[current] === '\\' && !fastMode) {
                current++; // Skip escape character
                column++;
            }
            if (current < input.length) {
                value += input[current];
                current++;
                column++;
            }
        }
        if (current < input.length) {
            current++; // Skip closing quote
            column++;
        }
        return {
            token: { type: TokenType.STRING, value, line, column: startColumn },
            newCurrent: current,
            newColumn: column
        };
    }
    tokenizeNumber(input, start, line, startColumn, fastMode) {
        let current = start;
        let column = startColumn;
        let value = '';
        while (current < input.length && (this.isDigit(input[current]) || (!fastMode && input[current] === '.'))) {
            value += input[current];
            current++;
            column++;
        }
        return {
            token: { type: TokenType.NUMBER, value, line, column: startColumn },
            newCurrent: current,
            newColumn: column
        };
    }
    tokenizeIdentifier(input, start, line, startColumn, fastMode) {
        let current = start;
        let column = startColumn;
        let value = '';
        while (current < input.length && (this.isAlphaNumeric(input[current]) || input[current] === '_')) {
            value += input[current];
            current++;
            column++;
            // Limit identifier length in fast mode
            if (fastMode && value.length > 50) {
                break;
            }
        }
        // Simplified keyword lookup in fast mode
        let tokenType = TokenType.IDENTIFIER;
        if (fastMode) {
            if (this.keywordsFast.has(value)) {
                tokenType = this.keywords.get(value) || TokenType.IDENTIFIER;
            }
        }
        else {
            tokenType = this.keywords.get(value) || TokenType.IDENTIFIER;
        }
        // Handle boolean literals
        if (value === 'true' || value === 'false') {
            tokenType = TokenType.BOOLEAN;
        }
        return {
            token: { type: tokenType, value, line, column: startColumn },
            newCurrent: current,
            newColumn: column
        };
    }
    getSingleCharTokenFast(char) {
        // Simplified single char tokens for fast mode
        switch (char) {
            case '(': return TokenType.LPAREN;
            case ')': return TokenType.RPAREN;
            case '{': return TokenType.LBRACE;
            case '}': return TokenType.RBRACE;
            case ';': return TokenType.SEMICOLON;
            case ',': return TokenType.COMMA;
            case '.': return TokenType.DOT;
            case ':': return TokenType.COLON;
            case '=': return TokenType.ASSIGN;
            default: return null;
        }
    }
    isDigit(char) {
        return char >= '0' && char <= '9';
    }
    isAlpha(char) {
        return (char >= 'a' && char <= 'z') || (char >= 'A' && char <= 'Z');
    }
    isAlphaNumeric(char) {
        return this.isAlpha(char) || this.isDigit(char);
    }
    getSingleCharToken(char) {
        const tokens = {
            '(': TokenType.LPAREN,
            ')': TokenType.RPAREN,
            '{': TokenType.LBRACE,
            '}': TokenType.RBRACE,
            '[': TokenType.LEFT_BRACKET,
            ']': TokenType.RIGHT_BRACKET,
            ';': TokenType.SEMICOLON,
            ',': TokenType.COMMA,
            '.': TokenType.DOT,
            ':': TokenType.COLON,
            '+': TokenType.PLUS,
            '-': TokenType.MINUS,
            '*': TokenType.MULTIPLY,
            '/': TokenType.DIVIDE,
            '%': TokenType.MODULO,
            '=': TokenType.ASSIGN,
            '<': TokenType.LESS_THAN,
            '>': TokenType.GREATER_THAN,
            '!': TokenType.EXCLAMATION_MARK,
            '?': TokenType.QUESTION,
            '&': TokenType.AND,
            '|': TokenType.OR,
            '_': TokenType.UNDERSCORE
        };
        return tokens[char] || null;
    }
    getTwoCharToken(chars) {
        const tokens = {
            '==': TokenType.EQUAL,
            '!=': TokenType.NOT_EQUAL,
            '<=': TokenType.LESS_EQUAL,
            '>=': TokenType.GREATER_EQUAL,
            '++': TokenType.INCREMENT,
            '--': TokenType.DECREMENT,
            '+=': TokenType.PLUS_ASSIGN,
            '-=': TokenType.MINUS_ASSIGN,
            '*=': TokenType.MULTIPLY_ASSIGN,
            '/=': TokenType.DIVIDE_ASSIGN,
            '=>': TokenType.ARROW,
            '??': TokenType.QUESTION_QUESTION,
            '?.': TokenType.QUESTION_DOT,
            '..': TokenType.DOT_DOT,
            '&&': TokenType.AND,
            '||': TokenType.OR
        };
        return tokens[chars] || null;
    }
    getThreeCharToken(chars) {
        const tokens = {
            '..<': TokenType.DOT_DOT_LESS
        };
        return tokens[chars] || null;
    }
}
exports.UhighLexer = UhighLexer;
//# sourceMappingURL=lexer.js.map