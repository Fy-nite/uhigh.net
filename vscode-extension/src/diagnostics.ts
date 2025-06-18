import * as vscode from 'vscode';
import { UhighParser } from './parser';

export class UhighDiagnosticsProvider {
    constructor(
        private parser: UhighParser,
        private diagnosticsCollection: vscode.DiagnosticCollection
    ) {}

    updateDiagnostics(document: vscode.TextDocument): void {
        try {
            const parsed = this.parser.parseDocument(document);
            const diagnostics: vscode.Diagnostic[] = [];

            // Add parse errors
            parsed.errors.forEach(error => {
                const diagnostic = new vscode.Diagnostic(
                    error.range,
                    error.message,
                    vscode.DiagnosticSeverity.Error
                );
                diagnostic.code = 'parse-error';
                diagnostic.source = 'uhigh';
                diagnostics.push(diagnostic);
            });

            // Only run undefined identifier check for smaller files or less frequently
            if (document.getText().length < 10000) { // Only for files < 10KB
                this.addUndefinedIdentifierDiagnostics(document, parsed, diagnostics);
            }

            this.diagnosticsCollection.set(document.uri, diagnostics);
        } catch (error) {
            console.error('Error in diagnostics provider:', error);
            // Clear diagnostics on error to prevent issues
            this.diagnosticsCollection.set(document.uri, []);
        }
    }

    private addUndefinedIdentifierDiagnostics(
        document: vscode.TextDocument, 
        parsed: any, 
        diagnostics: vscode.Diagnostic[]
    ): void {
        // Create sets of defined symbols including built-ins
        const definedVars = new Set([
            ...parsed.variables.map((v: any) => v.name),
            ...parsed.builtInVariables.map((v: any) => v.name)
        ]);
        
        const definedFuncs = new Set([
            ...parsed.functions.map((f: any) => f.name),
            ...parsed.builtInFunctions.map((f: any) => f.name)
        ]);

        const definedClasses = new Set([
            ...parsed.classes.map((c: any) => c.name),
            ...parsed.builtInClasses.map((c: any) => c.name)
        ]);

        // Add qualified method names (Class.Method)
        parsed.builtInClasses.forEach((cls: any) => {
            cls.methods.forEach((method: any) => {
                definedFuncs.add(`${cls.name}.${method.name}`);
            });
            cls.fields.forEach((field: any) => {
                definedVars.add(`${cls.name}.${field.name}`);
            });
        });

        parsed.classes.forEach((cls: any) => {
            cls.methods.forEach((method: any) => {
                definedFuncs.add(`${cls.name}.${method.name}`);
            });
            cls.fields.forEach((field: any) => {
                definedVars.add(`${cls.name}.${field.name}`);
            });
        });

        const text = document.getText();
        const lines = text.split('\n');
        
        // Keywords to skip
        const keywords = new Set([
            'func', 'var', 'const', 'class', 'if', 'else', 'while', 'for', 'return', 
            'true', 'false', 'null', 'this', 'new', 'namespace', 'import',
            'public', 'private', 'protected', 'static', 'readonly', 'break', 'continue',
            'int', 'float', 'string', 'bool', 'void'
        ]);

        lines.forEach((line, lineIndex) => {
            // Skip comment lines and empty lines
            const trimmedLine = line.trim();
            if (trimmedLine.startsWith('//') || trimmedLine === '') {
                return;
            }

            // Optimized string literal detection
            const { cleanedLine, stringRanges } = this.removeStringLiterals(line);
            
            // Simple identifier regex (no qualified names for performance)
            const identifierRegex = /\b[a-zA-Z_][a-zA-Z0-9_]*\b/g;
            let match;
            
            while ((match = identifierRegex.exec(cleanedLine)) !== null) {
                const identifier = match[0];
                const matchStart = match.index;
                
                // Skip if this position was inside a string literal
                if (this.isInStringRange(matchStart, stringRanges)) {
                    continue;
                }
                
                // Skip keywords and known identifiers
                if (keywords.has(identifier)) {
                    continue;
                }

                // Check if it's a known variable, function, or class
                if (definedVars.has(identifier) || definedFuncs.has(identifier) || definedClasses.has(identifier)) {
                    continue;
                }

                // Quick check if it's a parameter or local variable
                if (this.isKnownLocalIdentifier(identifier, parsed, lines, lineIndex)) {
                    continue;
                }

                // Check if it looks like a function call (followed by parenthesis)
                const remainingLine = cleanedLine.substring(match.index + identifier.length);
                if (/^\s*\(/.test(remainingLine)) {
                    continue; // Likely a function call, be lenient
                }

                const range = new vscode.Range(
                    lineIndex,
                    matchStart,
                    lineIndex,
                    matchStart + identifier.length
                );

                const diagnostic = new vscode.Diagnostic(
                    range,
                    `Undefined identifier: '${identifier}'`,
                    vscode.DiagnosticSeverity.Warning
                );
                diagnostic.code = 'undefined-identifier';
                diagnostic.source = 'uhigh';
                diagnostics.push(diagnostic);
            }
        });
    }

    private removeStringLiterals(line: string): { cleanedLine: string, stringRanges: Array<{start: number, end: number}> } {
        const stringRanges: Array<{start: number, end: number}> = [];
        let cleanedLine = '';
        let i = 0;
        
        while (i < line.length) {
            const char = line[i];
            
            if (char === '"' || char === "'") {
                const quote = char;
                const start = i;
                i++; // Skip opening quote
                
                // Find closing quote
                while (i < line.length && line[i] !== quote) {
                    if (line[i] === '\\') {
                        i++; // Skip escaped character
                    }
                    i++;
                }
                
                if (i < line.length) {
                    i++; // Skip closing quote
                }
                
                stringRanges.push({ start, end: i });
                cleanedLine += ' '.repeat(i - start); // Replace with spaces
            } else {
                cleanedLine += char;
                i++;
            }
        }
        
        return { cleanedLine, stringRanges };
    }

    private isInStringRange(position: number, stringRanges: Array<{start: number, end: number}>): boolean {
        return stringRanges.some(range => position >= range.start && position < range.end);
    }

    private isKnownLocalIdentifier(identifier: string, parsed: any, lines: string[], currentLine: number): boolean {
        // Quick check for function parameters
        for (const func of parsed.functions) {
            if (func.bodyRange && 
                currentLine >= func.bodyRange.start.line && 
                currentLine <= func.bodyRange.end.line) {
                if (func.parameters.some((param: any) => param.name === identifier)) {
                    return true;
                }
            }
        }

        // Quick check for local variable declarations (only check a few lines back for performance)
        const startLine = Math.max(0, currentLine - 10);
        for (let i = startLine; i <= currentLine; i++) {
            const line = lines[i];
            
            // Simple patterns for variable declarations
            if (new RegExp(`\\b(?:var|const)\\s+${identifier}\\b`).test(line)) {
                return true;
            }
            
            // Check for assignment (simple heuristic)
            if (new RegExp(`\\b${identifier}\\s*=`).test(line) && !line.includes('==')) {
                return true;
            }
        }
        
        return false;
    }
}
