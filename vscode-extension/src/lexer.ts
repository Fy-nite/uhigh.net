export enum TokenType {
    // Literals
    NUMBER = 'NUMBER',
    STRING = 'STRING',
    IDENTIFIER = 'IDENTIFIER',
    NEW = 'NEW',
    
    // Keywords
    VAR = 'VAR', 
    CONST = 'CONST', 
    FUNC = 'FUNC', 
    RETURN = 'RETURN', 
    IF = 'IF', 
    ELSE = 'ELSE', 
    WHILE = 'WHILE', 
    FOR = 'FOR', 
    IN = 'IN',
    PRINT = 'PRINT', 
    INCLUDE = 'INCLUDE', 
    TRY = 'TRY', 
    CATCH = 'CATCH', 
    THROW = 'THROW', 
    FIELD = 'FIELD', 
    MATCH = 'MATCH',
    TRUE = 'TRUE', 
    FALSE = 'FALSE', 
    INT = 'INT', 
    FLOAT = 'FLOAT', 
    STRING_TYPE = 'STRING_TYPE', 
    BOOL = 'BOOL', 
    VOID = 'VOID',
    SHARP = 'SHARP',
    RANGE = 'RANGE',
    BREAK = 'BREAK', 
    CONTINUE = 'CONTINUE',
    CLASS = 'CLASS', 
    NAMESPACE = 'NAMESPACE', 
    IMPORT = 'IMPORT', 
    FROM = 'FROM', 
    THIS = 'THIS',
    GET = 'GET', 
    SET = 'SET',
    ENUM = 'ENUM', 
    INTERFACE = 'INTERFACE', 
    EXTENSION = 'EXTENSION', 
    RECORD = 'RECORD',
    SWITCH = 'SWITCH', 
    CASE = 'CASE', 
    DEFAULT = 'DEFAULT',
    STRUCT = 'STRUCT', 
    UNION = 'UNION',
    MODULE = 'MODULE', 
    USE = 'USE',
    LET = 'LET', 
    MUT = 'MUT',
    ASYNC = 'ASYNC', 
    AWAIT = 'AWAIT',
    MACRO = 'MACRO', 
    YIELD = 'YIELD',
    
    // Access Modifiers and Keywords
    PUBLIC = 'PUBLIC', 
    PRIVATE = 'PRIVATE', 
    PROTECTED = 'PROTECTED', 
    INTERNAL = 'INTERNAL', 
    STATIC = 'STATIC', 
    ABSTRACT = 'ABSTRACT', 
    VIRTUAL = 'VIRTUAL', 
    OVERRIDE = 'OVERRIDE', 
    SEALED = 'SEALED',
    READONLY = 'READONLY',
    
    // Nullable and null operators
    QUESTION = 'QUESTION',
    QUESTION_QUESTION = 'QUESTION_QUESTION',
    QUESTION_DOT = 'QUESTION_DOT',
    EXCLAMATION_MARK = 'EXCLAMATION_MARK',
    
    // String interpolation
    INTERPOLATED_STRING_START = 'INTERPOLATED_STRING_START',
    INTERPOLATED_STRING_MID = 'INTERPOLATED_STRING_MID',
    INTERPOLATED_STRING_END = 'INTERPOLATED_STRING_END',
    INTERPOLATION_START = 'INTERPOLATION_START',
    INTERPOLATION_END = 'INTERPOLATION_END',
    
    // Range operators
    DOT_DOT = 'DOT_DOT',
    DOT_DOT_LESS = 'DOT_DOT_LESS',
    
    // Operators
    PLUS = 'PLUS', 
    MINUS = 'MINUS', 
    MULTIPLY = 'MULTIPLY', 
    DIVIDE = 'DIVIDE', 
    MODULO = 'MODULO', 
    ASSIGN = 'ASSIGN',
    EQUAL = 'EQUAL', 
    NOT_EQUAL = 'NOT_EQUAL', 
    LESS_THAN = 'LESS_THAN', 
    GREATER_THAN = 'GREATER_THAN', 
    LESS_EQUAL = 'LESS_EQUAL', 
    GREATER_EQUAL = 'GREATER_EQUAL',
    AND = 'AND', 
    OR = 'OR', 
    NOT = 'NOT', 
    INCREMENT = 'INCREMENT', 
    DECREMENT = 'DECREMENT',
    PLUS_ASSIGN = 'PLUS_ASSIGN', 
    MINUS_ASSIGN = 'MINUS_ASSIGN', 
    MULTIPLY_ASSIGN = 'MULTIPLY_ASSIGN', 
    DIVIDE_ASSIGN = 'DIVIDE_ASSIGN',
    ARROW = 'ARROW',
    UNDERSCORE = 'UNDERSCORE',
    
    // Punctuation
    LPAREN = 'LPAREN', 
    RPAREN = 'RPAREN', 
    LBRACE = 'LBRACE', 
    RBRACE = 'RBRACE',
    LEFT_BRACKET = 'LEFT_BRACKET', 
    RIGHT_BRACKET = 'RIGHT_BRACKET', 
    COMMA = 'COMMA', 
    SEMICOLON = 'SEMICOLON', 
    COLON = 'COLON',
    AS = 'AS', 
    DOT = 'DOT',
    
    // Special
    COMMENT = 'COMMENT', 
    NEWLINE = 'NEWLINE', 
    EOF = 'EOF', 
    BOOLEAN = 'BOOLEAN',
    WHITESPACE = 'WHITESPACE',
    
    // Attributes
    ATTRIBUTE = 'ATTRIBUTE'
}

export interface Token {
    type: TokenType;
    value: string;
    line: number;
    column: number;
}

export class UhighLexer {
    // Simplified keyword map for performance
    private keywordsFast = new Set([
        'var', 'const', 'func', 'return', 'if', 'else', 'while', 'for', 'class', 'namespace',
        'public', 'private', 'static', 'true', 'false', 'void', 'int', 'string', 'bool'
    ]);

    private keywords: Map<string, TokenType> = new Map([
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

    public tokenize(input: string): Token[] {
        return this.tokenizeOptimized(input, false);
    }

    public tokenizeOptimized(input: string, fastMode: boolean = true): Token[] {
        const tokens: Token[] = [];
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
            } else {
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

    private tokenizeString(input: string, start: number, line: number, startColumn: number, quote: string, fastMode: boolean) {
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

    private tokenizeNumber(input: string, start: number, line: number, startColumn: number, fastMode: boolean) {
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

    private tokenizeIdentifier(input: string, start: number, line: number, startColumn: number, fastMode: boolean) {
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
        } else {
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

    private getSingleCharTokenFast(char: string): TokenType | null {
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

    private isDigit(char: string): boolean {
        return char >= '0' && char <= '9';
    }

    private isAlpha(char: string): boolean {
        return (char >= 'a' && char <= 'z') || (char >= 'A' && char <= 'Z');
    }

    private isAlphaNumeric(char: string): boolean {
        return this.isAlpha(char) || this.isDigit(char);
    }

    private getSingleCharToken(char: string): TokenType | null {
        const tokens: { [key: string]: TokenType } = {
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

    private getTwoCharToken(chars: string): TokenType | null {
        const tokens: { [key: string]: TokenType } = {
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

    private getThreeCharToken(chars: string): TokenType | null {
        const tokens: { [key: string]: TokenType } = {
            '..<': TokenType.DOT_DOT_LESS
        };

        return tokens[chars] || null;
    }
}
