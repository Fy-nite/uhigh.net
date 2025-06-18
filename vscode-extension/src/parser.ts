import * as vscode from 'vscode';

export interface UhighToken {
    type: TokenType;
    value: string;
    range: vscode.Range;
}

export enum TokenType {
    // Keywords
    Func = 'func',
    Var = 'var',
    Const = 'const',
    Class = 'class',
    If = 'if',
    Else = 'else',
    While = 'while',
    For = 'for',
    Return = 'return',
    Namespace = 'namespace',
    Import = 'import',
    
    // Access modifiers
    Public = 'public',
    Private = 'private',
    Protected = 'protected',
    Static = 'static',
    Readonly = 'readonly',
    
    // Types
    Int = 'int',
    Float = 'float',
    String = 'string',
    Bool = 'bool',
    Void = 'void',
    
    // Identifiers and literals
    Identifier = 'identifier',
    Number = 'number',
    StringLiteral = 'string_literal',
    
    // Operators
    Assign = '=',
    Plus = '+',
    Minus = '-',
    
    // Punctuation
    LeftParen = '(',
    RightParen = ')',
    LeftBrace = '{',
    RightBrace = '}',
    Colon = ':',
    Comma = ',',
    Dot = '.',
    
    // Special
    Comment = 'comment',
    EOF = 'eof',
    Unknown = 'unknown'
}

export interface UhighFunction {
    name: string;
    parameters: Array<{ name: string; type?: string }>;
    returnType?: string;
    range: vscode.Range;
    bodyRange?: vscode.Range;
    modifiers: string[];
}

export interface UhighVariable {
    name: string;
    type?: string;
    range: vscode.Range;
    isConstant: boolean;
    modifiers: string[];
}

export interface UhighClass {
    name: string;
    range: vscode.Range;
    methods: UhighFunction[];
    fields: UhighVariable[];
    modifiers: string[];
}

export interface ParsedDocument {
    functions: UhighFunction[];
    variables: UhighVariable[];
    classes: UhighClass[];
    errors: Array<{ message: string; range: vscode.Range }>;
    // Built-in symbols for intellisense
    builtInFunctions: UhighFunction[];
    builtInClasses: UhighClass[];
    builtInVariables: UhighVariable[];
}

export class UhighParser {
    private keywords = new Set([
        'func', 'var', 'const', 'class', 'if', 'else', 'while', 'for', 'return',
        'int', 'float', 'string', 'bool', 'void', 'true', 'false', 'null', 'namespace',
        'import', 'public', 'private', 'protected', 'static', 'readonly', 'this', 'new'
    ]);

    parseDocument(document: vscode.TextDocument): ParsedDocument {
        const text = document.getText();
        const tokens = this.tokenize(text, document);
        const parsed = this.parseTokens(tokens);
        
        // Add built-in symbols
        parsed.builtInFunctions = this.getBuiltInFunctions();
        parsed.builtInClasses = this.getBuiltInClasses();
        parsed.builtInVariables = this.getBuiltInVariables();
        
        return parsed;
    }

    private getBuiltInFunctions(): UhighFunction[] {
        return [
            {
                name: 'print',
                parameters: [{ name: 'value', type: 'object' }],
                returnType: 'void',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            },
            {
                name: 'input',
                parameters: [],
                returnType: 'string',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            },
            {
                name: 'len',
                parameters: [{ name: 'obj', type: 'object' }],
                returnType: 'int',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            },
            {
                name: 'abs',
                parameters: [{ name: 'value', type: 'float' }],
                returnType: 'float',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            },
            {
                name: 'sqrt',
                parameters: [{ name: 'value', type: 'float' }],
                returnType: 'float',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            },
            // String functions
            {
                name: 'string',
                parameters: [{ name: 'value', type: 'object' }],
                returnType: 'string',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            },
            {
                name: 'int',
                parameters: [{ name: 'value', type: 'object' }],
                returnType: 'int',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            },
            {
                name: 'float',
                parameters: [{ name: 'value', type: 'object' }],
                returnType: 'float',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static']
            }
        ];
    }

    private getBuiltInClasses(): UhighClass[] {
        return [
            {
                name: 'Console',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static'],
                methods: [
                    {
                        name: 'WriteLine',
                        parameters: [{ name: 'value', type: 'object' }],
                        returnType: 'void',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    },
                    {
                        name: 'Write',
                        parameters: [{ name: 'value', type: 'object' }],
                        returnType: 'void',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    },
                    {
                        name: 'ReadLine',
                        parameters: [],
                        returnType: 'string',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    }
                ],
                fields: []
            },
            {
                name: 'Math',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public', 'static'],
                methods: [
                    {
                        name: 'Abs',
                        parameters: [{ name: 'value', type: 'float' }],
                        returnType: 'float',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    },
                    {
                        name: 'Sqrt',
                        parameters: [{ name: 'value', type: 'float' }],
                        returnType: 'float',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    },
                    {
                        name: 'Pow',
                        parameters: [{ name: 'x', type: 'float' }, { name: 'y', type: 'float' }],
                        returnType: 'float',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    },
                    {
                        name: 'Min',
                        parameters: [{ name: 'a', type: 'float' }, { name: 'b', type: 'float' }],
                        returnType: 'float',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    },
                    {
                        name: 'Max',
                        parameters: [{ name: 'a', type: 'float' }, { name: 'b', type: 'float' }],
                        returnType: 'float',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public', 'static']
                    }
                ],
                fields: [
                    {
                        name: 'PI',
                        type: 'float',
                        range: new vscode.Range(0, 0, 0, 0),
                        isConstant: true,
                        modifiers: ['public', 'static', 'readonly']
                    },
                    {
                        name: 'E',
                        type: 'float',
                        range: new vscode.Range(0, 0, 0, 0),
                        isConstant: true,
                        modifiers: ['public', 'static', 'readonly']
                    }
                ]
            },
            {
                name: 'String',
                range: new vscode.Range(0, 0, 0, 0),
                modifiers: ['public'],
                methods: [
                    {
                        name: 'Length',
                        parameters: [],
                        returnType: 'int',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public']
                    },
                    {
                        name: 'ToUpper',
                        parameters: [],
                        returnType: 'string',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public']
                    },
                    {
                        name: 'ToLower',
                        parameters: [],
                        returnType: 'string',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public']
                    },
                    {
                        name: 'Substring',
                        parameters: [{ name: 'startIndex', type: 'int' }, { name: 'length', type: 'int' }],
                        returnType: 'string',
                        range: new vscode.Range(0, 0, 0, 0),
                        modifiers: ['public']
                    }
                ],
                fields: []
            }
        ];
    }

    private getBuiltInVariables(): UhighVariable[] {
        return [
            {
                name: 'args',
                type: 'string[]',
                range: new vscode.Range(0, 0, 0, 0),
                isConstant: false,
                modifiers: []
            }
        ];
    }

    private tokenize(text: string, document: vscode.TextDocument): UhighToken[] {
        const tokens: UhighToken[] = [];
        const lines = text.split('\n');
        
        for (let lineNumber = 0; lineNumber < lines.length; lineNumber++) {
            const line = lines[lineNumber];
            let column = 0;
            
            while (column < line.length) {
                const char = line[column];
                
                // Skip whitespace
                if (/\s/.test(char)) {
                    column++;
                    continue;
                }
                
                // Comments
                if (char === '/' && line[column + 1] === '/') {
                    const start = new vscode.Position(lineNumber, column);
                    const end = new vscode.Position(lineNumber, line.length);
                    tokens.push({
                        type: TokenType.Comment,
                        value: line.substring(column),
                        range: new vscode.Range(start, end)
                    });
                    break; // Rest of line is comment
                }
                
                // String literals
                if (char === '"') {
                    const start = new vscode.Position(lineNumber, column);
                    let endColumn = column + 1;
                    while (endColumn < line.length && line[endColumn] !== '"') {
                        if (line[endColumn] === '\\') endColumn++; // Skip escaped chars
                        endColumn++;
                    }
                    endColumn++; // Include closing quote
                    const end = new vscode.Position(lineNumber, endColumn);
                    
                    tokens.push({
                        type: TokenType.StringLiteral,
                        value: line.substring(column, endColumn),
                        range: new vscode.Range(start, end)
                    });
                    column = endColumn;
                    continue;
                }
                
                // Numbers
                if (/\d/.test(char)) {
                    const start = new vscode.Position(lineNumber, column);
                    let endColumn = column;
                    while (endColumn < line.length && /[\d.]/.test(line[endColumn])) {
                        endColumn++;
                    }
                    const end = new vscode.Position(lineNumber, endColumn);
                    
                    tokens.push({
                        type: TokenType.Number,
                        value: line.substring(column, endColumn),
                        range: new vscode.Range(start, end)
                    });
                    column = endColumn;
                    continue;
                }
                
                // Identifiers and keywords (including qualified names like Console.WriteLine)
                if (/[a-zA-Z_]/.test(char)) {
                    const start = new vscode.Position(lineNumber, column);
                    let endColumn = column;
                    
                    // Read the first identifier part
                    while (endColumn < line.length && /[a-zA-Z0-9_]/.test(line[endColumn])) {
                        endColumn++;
                    }
                    
                    // Check for qualified names (Class.Method, namespace.Class.Method, etc.)
                    while (endColumn < line.length && line[endColumn] === '.') {
                        endColumn++; // Skip the dot
                        
                        // Read the next identifier part
                        if (endColumn < line.length && /[a-zA-Z_]/.test(line[endColumn])) {
                            while (endColumn < line.length && /[a-zA-Z0-9_]/.test(line[endColumn])) {
                                endColumn++;
                            }
                        } else {
                            // Invalid qualified name, back up
                            endColumn--;
                            break;
                        }
                    }
                    
                    const end = new vscode.Position(lineNumber, endColumn);
                    const value = line.substring(column, endColumn);
                    
                    let tokenType: TokenType;
                    // Only check for keywords if it's a simple identifier (no dots)
                    if (!value.includes('.') && this.keywords.has(value)) {
                        tokenType = value as TokenType;
                    } else {
                        tokenType = TokenType.Identifier;
                    }
                    
                    tokens.push({
                        type: tokenType,
                        value: value,
                        range: new vscode.Range(start, end)
                    });
                    column = endColumn;
                    continue;
                }
                
                // Single character tokens
                const start = new vscode.Position(lineNumber, column);
                const end = new vscode.Position(lineNumber, column + 1);
                let tokenType: TokenType;
                
                switch (char) {
                    case '=': tokenType = TokenType.Assign; break;
                    case '+': tokenType = TokenType.Plus; break;
                    case '-': tokenType = TokenType.Minus; break;
                    case '(': tokenType = TokenType.LeftParen; break;
                    case ')': tokenType = TokenType.RightParen; break;
                    case '{': tokenType = TokenType.LeftBrace; break;
                    case '}': tokenType = TokenType.RightBrace; break;
                    case ':': tokenType = TokenType.Colon; break;
                    case ',': tokenType = TokenType.Comma; break;
                    case '.': tokenType = TokenType.Dot; break;
                    default: tokenType = TokenType.Unknown; break;
                }
                
                tokens.push({
                    type: tokenType,
                    value: char,
                    range: new vscode.Range(start, end)
                });
                column++;
            }
        }
        
        return tokens;
    }

    private parseTokens(tokens: UhighToken[]): ParsedDocument {
        const result: ParsedDocument = {
            functions: [],
            variables: [],
            classes: [],
            errors: [],
            builtInFunctions: [],
            builtInClasses: [],
            builtInVariables: []
        };
        
        let current = 0;
        
        while (current < tokens.length) {
            const token = tokens[current];
            
            try {
                // Parse modifiers first
                const modifiers: string[] = [];
                while (current < tokens.length && this.isModifier(tokens[current].value)) {
                    modifiers.push(tokens[current].value);
                    current++;
                }

                if (current >= tokens.length) break;

                const currentToken = tokens[current];
                
                if (currentToken.type === TokenType.Func) {
                    const func = this.parseFunction(tokens, current, modifiers);
                    if (func.function) {
                        result.functions.push(func.function);
                    }
                    current = func.nextIndex;
                } else if (currentToken.type === TokenType.Var || currentToken.type === TokenType.Const) {
                    const variable = this.parseVariable(tokens, current, modifiers);
                    if (variable.variable) {
                        result.variables.push(variable.variable);
                    }
                    current = variable.nextIndex;
                } else if (currentToken.type === TokenType.Class) {
                    const cls = this.parseClass(tokens, current, modifiers);
                    if (cls.class) {
                        result.classes.push(cls.class);
                    }
                    current = cls.nextIndex;
                } else {
                    current++;
                }
            } catch (error) {
                result.errors.push({
                    message: `Parse error: ${error}`,
                    range: token.range
                });
                current++;
            }
        }
        
        return result;
    }

    private isModifier(value: string): boolean {
        return ['public', 'private', 'protected', 'static', 'readonly'].includes(value);
    }

    private parseFunction(tokens: UhighToken[], start: number, modifiers: string[]): { function?: UhighFunction; nextIndex: number } {
        let current = start + 1; // Skip 'func'
        
        if (current >= tokens.length || tokens[current].type !== TokenType.Identifier) {
            return { nextIndex: current };
        }
        
        const nameToken = tokens[current];
        const name = nameToken.value;
        current++;
        
        // Parse parameters
        const parameters: Array<{ name: string; type?: string }> = [];
        
        if (current < tokens.length && tokens[current].type === TokenType.LeftParen) {
            current++; // Skip '('
            
            while (current < tokens.length && tokens[current].type !== TokenType.RightParen) {
                if (tokens[current].type === TokenType.Identifier) {
                    const paramName = tokens[current].value;
                    let paramType: string | undefined;
                    current++;
                    
                    if (current < tokens.length && tokens[current].type === TokenType.Colon) {
                        current++; // Skip ':'
                        if (current < tokens.length && this.isTypeToken(tokens[current])) {
                            paramType = tokens[current].value;
                            current++;
                        }
                    }
                    
                    parameters.push({ name: paramName, type: paramType });
                }
                
                if (current < tokens.length && tokens[current].type === TokenType.Comma) {
                    current++;
                } else if (current < tokens.length && tokens[current].type !== TokenType.RightParen) {
                    current++;
                }
            }
            
            if (current < tokens.length && tokens[current].type === TokenType.RightParen) {
                current++; // Skip ')'
            }
        }
        
        // Parse return type
        let returnType: string | undefined;
        if (current < tokens.length && tokens[current].type === TokenType.Colon) {
            current++; // Skip ':'
            if (current < tokens.length && this.isTypeToken(tokens[current])) {
                returnType = tokens[current].value;
                current++;
            }
        }
        
        // Find function body
        let bodyRange: vscode.Range | undefined;
        if (current < tokens.length && tokens[current].type === TokenType.LeftBrace) {
            const bodyStart = tokens[current].range.start;
            let braceCount = 1;
            current++; // Skip '{'
            
            while (current < tokens.length && braceCount > 0) {
                if (tokens[current].type === TokenType.LeftBrace) {
                    braceCount++;
                } else if (tokens[current].type === TokenType.RightBrace) {
                    braceCount--;
                }
                current++;
            }
            
            if (braceCount === 0) {
                bodyRange = new vscode.Range(bodyStart, tokens[current - 1].range.end);
            }
        }
        
        const functionRange = new vscode.Range(
            tokens[start].range.start,
            current > start ? tokens[current - 1].range.end : nameToken.range.end
        );
        
        return {
            function: {
                name,
                parameters,
                returnType,
                range: functionRange,
                bodyRange,
                modifiers
            },
            nextIndex: current
        };
    }

    private parseVariable(tokens: UhighToken[], start: number, modifiers: string[]): { variable?: UhighVariable; nextIndex: number } {
        const isConstant = tokens[start].type === TokenType.Const;
        let current = start + 1; // Skip 'var' or 'const'
        
        if (current >= tokens.length || tokens[current].type !== TokenType.Identifier) {
            return { nextIndex: current };
        }
        
        const nameToken = tokens[current];
        const name = nameToken.value;
        current++;
        
        let type: string | undefined;
        if (current < tokens.length && tokens[current].type === TokenType.Colon) {
            current++; // Skip ':'
            if (current < tokens.length && this.isTypeToken(tokens[current])) {
                type = tokens[current].value;
                current++;
            }
        }
        
        const range = new vscode.Range(
            tokens[start].range.start,
            nameToken.range.end
        );
        
        return {
            variable: {
                name,
                type,
                range,
                isConstant,
                modifiers
            },
            nextIndex: current
        };
    }

    private parseClass(tokens: UhighToken[], start: number, modifiers: string[]): { class?: UhighClass; nextIndex: number } {
        let current = start + 1; // Skip 'class'
        
        if (current >= tokens.length || tokens[current].type !== TokenType.Identifier) {
            return { nextIndex: current };
        }
        
        const nameToken = tokens[current];
        const name = nameToken.value;
        current++;
        
        const methods: UhighFunction[] = [];
        const fields: UhighVariable[] = [];
        
        // Find class body
        if (current < tokens.length && tokens[current].type === TokenType.LeftBrace) {
            current++; // Skip '{'
            
            while (current < tokens.length && tokens[current].type !== TokenType.RightBrace) {
                // Parse member modifiers
                const memberModifiers: string[] = [];
                while (current < tokens.length && this.isModifier(tokens[current].value)) {
                    memberModifiers.push(tokens[current].value);
                    current++;
                }

                if (current >= tokens.length) break;

                if (tokens[current].type === TokenType.Func) {
                    const func = this.parseFunction(tokens, current, memberModifiers);
                    if (func.function) {
                        methods.push(func.function);
                    }
                    current = func.nextIndex;
                } else if (tokens[current].type === TokenType.Var || tokens[current].type === TokenType.Const) {
                    const variable = this.parseVariable(tokens, current, memberModifiers);
                    if (variable.variable) {
                        fields.push(variable.variable);
                    }
                    current = variable.nextIndex;
                } else {
                    current++;
                }
            }
            
            if (current < tokens.length && tokens[current].type === TokenType.RightBrace) {
                current++; // Skip '}'
            }
        }
        
        const classRange = new vscode.Range(
            tokens[start].range.start,
            current > start ? tokens[current - 1].range.end : nameToken.range.end
        );
        
        return {
            class: {
                name,
                range: classRange,
                methods,
                fields,
                modifiers
            },
            nextIndex: current
        };
    }

    private isTypeToken(token: UhighToken): boolean {
        return token.type === TokenType.Int || 
               token.type === TokenType.Float || 
               token.type === TokenType.String || 
               token.type === TokenType.Bool || 
               token.type === TokenType.Void ||
               token.type === TokenType.Identifier;
    }
}
