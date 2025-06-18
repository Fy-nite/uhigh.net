"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UhighDiagnosticsProvider = void 0;
const vscode = require("vscode");
class UhighDiagnosticsProvider {
    constructor(parser, diagnosticsCollection) {
        this.parser = parser;
        this.diagnosticsCollection = diagnosticsCollection;
    }
    updateDiagnostics(document) {
        const parsed = this.parser.parseDocument(document);
        const diagnostics = [];
        // Add parse errors
        parsed.errors.forEach(error => {
            const diagnostic = new vscode.Diagnostic(error.range, error.message, vscode.DiagnosticSeverity.Error);
            diagnostic.code = 'parse-error';
            diagnostic.source = 'uhigh';
            diagnostics.push(diagnostic);
        });
        // Create sets of defined symbols including built-ins
        const definedVars = new Set([
            ...parsed.variables.map(v => v.name),
            ...parsed.builtInVariables.map(v => v.name)
        ]);
        const definedFuncs = new Set([
            ...parsed.functions.map(f => f.name),
            ...parsed.builtInFunctions.map(f => f.name)
        ]);
        const definedClasses = new Set([
            ...parsed.classes.map(c => c.name),
            ...parsed.builtInClasses.map(c => c.name)
        ]);
        // Add qualified method names (Class.Method)
        parsed.builtInClasses.forEach(cls => {
            cls.methods.forEach(method => {
                definedFuncs.add(`${cls.name}.${method.name}`);
            });
            cls.fields.forEach(field => {
                definedVars.add(`${cls.name}.${field.name}`);
            });
        });
        parsed.classes.forEach(cls => {
            cls.methods.forEach(method => {
                definedFuncs.add(`${cls.name}.${method.name}`);
            });
            cls.fields.forEach(field => {
                definedVars.add(`${cls.name}.${field.name}`);
            });
        });
        // Simple regex to find identifiers that might be undefined
        const text = document.getText();
        const lines = text.split('\n');
        lines.forEach((line, lineIndex) => {
            // Skip comment lines
            if (line.trim().startsWith('//')) {
                return;
            }
            // Enhanced regex to handle qualified names and string literals
            const identifierRegex = /\b[a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)?\b/g;
            let match;
            // Track if we're inside a string literal
            let inString = false;
            let stringChar = '';
            while ((match = identifierRegex.exec(line)) !== null) {
                const identifier = match[0];
                const matchStart = match.index;
                // Check if this identifier is inside a string literal
                inString = false;
                for (let i = 0; i < matchStart; i++) {
                    const char = line[i];
                    if ((char === '"' || char === "'") && (i === 0 || line[i - 1] !== '\\')) {
                        if (!inString) {
                            inString = true;
                            stringChar = char;
                        }
                        else if (char === stringChar) {
                            inString = false;
                            stringChar = '';
                        }
                    }
                }
                // Skip identifiers inside strings
                if (inString) {
                    continue;
                }
                // Skip keywords and known identifiers
                const keywords = new Set([
                    'func', 'var', 'const', 'class', 'if', 'else', 'while', 'for', 'return',
                    'true', 'false', 'null', 'this', 'new', 'namespace', 'import',
                    'public', 'private', 'protected', 'static', 'readonly', 'break', 'continue'
                ]);
                if (keywords.has(identifier)) {
                    continue;
                }
                // Check if it's a type
                const types = new Set(['int', 'float', 'string', 'bool', 'void']);
                if (types.has(identifier)) {
                    continue;
                }
                // Check if it's a known variable, function, or class
                if (definedVars.has(identifier) || definedFuncs.has(identifier) || definedClasses.has(identifier)) {
                    continue;
                }
                // Check if it's a parameter in a function
                if (this.isParameterInFunction(identifier, parsed, lineIndex)) {
                    continue;
                }
                // Check if it's a local variable declaration on this line or previous lines
                if (this.isLocalVariable(identifier, lines, lineIndex)) {
                    continue;
                }
                // Check if it's followed by a parenthesis (likely a function call)
                const remainingLine = line.substring(match.index + identifier.length);
                const nextNonWhitespace = remainingLine.match(/^\s*(.)/);
                if (nextNonWhitespace && nextNonWhitespace[1] === '(') {
                    // This looks like a function call, be more lenient
                    continue;
                }
                const range = new vscode.Range(lineIndex, match.index, lineIndex, match.index + identifier.length);
                const diagnostic = new vscode.Diagnostic(range, `Undefined identifier: '${identifier}'`, vscode.DiagnosticSeverity.Warning);
                diagnostic.code = 'undefined-identifier';
                diagnostic.source = 'uhigh';
                diagnostics.push(diagnostic);
            }
        });
        this.diagnosticsCollection.set(document.uri, diagnostics);
    }
    isParameterInFunction(identifier, parsed, lineIndex) {
        // Find the function that contains this line
        for (const func of parsed.functions) {
            if (func.bodyRange &&
                lineIndex >= func.bodyRange.start.line &&
                lineIndex <= func.bodyRange.end.line) {
                // Check if identifier is a parameter of this function
                return func.parameters.some((param) => param.name === identifier);
            }
        }
        return false;
    }
    isLocalVariable(identifier, lines, currentLine) {
        // Check previous lines and current line for variable declarations
        for (let i = 0; i <= currentLine; i++) {
            const line = lines[i];
            // Simple pattern matching for variable declarations
            const varPattern = new RegExp(`\\b(?:var|const)\\s+${identifier}\\b`);
            if (varPattern.test(line)) {
                return true;
            }
            // Check for assignment that might be a declaration (but not inside strings)
            let inString = false;
            let stringChar = '';
            const cleanLine = line.split('').map((char, index) => {
                if ((char === '"' || char === "'") && (index === 0 || line[index - 1] !== '\\')) {
                    if (!inString) {
                        inString = true;
                        stringChar = char;
                        return ' ';
                    }
                    else if (char === stringChar) {
                        inString = false;
                        stringChar = '';
                        return ' ';
                    }
                }
                return inString ? ' ' : char;
            }).join('');
            const assignPattern = new RegExp(`\\b${identifier}\\s*=`);
            if (assignPattern.test(cleanLine)) {
                return true;
            }
        }
        return false;
    }
}
exports.UhighDiagnosticsProvider = UhighDiagnosticsProvider;
//# sourceMappingURL=diagnostics.js.map