import * as vscode from 'vscode';
import { UhighLexer, Token, TokenType } from './lexer';
import { UhighParser, ASTNode } from './parser';

interface LineCache {
    lineNumber: number;
    content: string;
    tokens: Token[];
    timestamp: number;
}

export class UhighLanguageServer {
    private lexer = new UhighLexer();
    private parser = new UhighParser();
    private lastValidationTime = 0;
    private validationDebounceMs = 1000;
    private validationCount = 0;
    private lineCache = new Map<string, LineCache>(); // Cache by document URI
    private cachedASTs = new Map<string, ASTNode>(); // Cache for parsed AST nodes
    private cacheMaxAge = 5000; // 5 seconds
    isTypingFast!: boolean;

    public validateDocument(document: vscode.TextDocument): vscode.Diagnostic[] {
        this.validationCount++;
        console.log(`validateDocument called #${this.validationCount} for ${document.uri.fsPath}`);
        
        // Add debouncing to prevent excessive parsing
        const now = Date.now();
        if (now - this.lastValidationTime < this.validationDebounceMs) {
            return [];
        }
        this.lastValidationTime = now;

        const diagnostics: vscode.Diagnostic[] = [];
        
        // Only do basic validation, don't parse the whole document
        try {
            // Just check for basic syntax issues without full parsing
            const text = document.getText();
            if (text.length > 50000) {
                diagnostics.push({
                    range: new vscode.Range(0, 0, 0, 1),
                    message: 'File too large for parsing',
                    severity: vscode.DiagnosticSeverity.Information
                } as vscode.Diagnostic);
            }
        } catch (error: any) {
            // ...existing error handling...
        }

        return diagnostics;
    }

    public validateLine(document: vscode.TextDocument, lineNumber: number): vscode.Diagnostic[] {
        const diagnostics: vscode.Diagnostic[] = [];
        const cacheKey = `${document.uri.fsPath}:${lineNumber}`;
        const now = Date.now();
        
        try {
            const line = document.lineAt(lineNumber);
            const lineContent = line.text;
            
            // Check cache first
            const cached = this.lineCache.get(cacheKey);
            if (cached && 
                cached.content === lineContent && 
                (now - cached.timestamp) < this.cacheMaxAge) {
                console.log(`Using cached tokens for line ${lineNumber}`);
                return diagnostics; // Return cached result
            }
            
            // Parse only this line
            console.log(`Parsing line ${lineNumber}: "${lineContent}"`);
            const tokens = this.lexer.tokenize(lineContent);
            
            // Cache the result
            this.lineCache.set(cacheKey, {
                lineNumber,
                content: lineContent,
                tokens,
                timestamp: now
            });
            
            // Clean old cache entries
            this.cleanCache();
            
            // Basic line-level validation
            if (lineContent.includes('func') && !lineContent.includes('(')) {
                diagnostics.push(new vscode.Diagnostic(
                    new vscode.Range(lineNumber, 0, lineNumber, lineContent.length),
                    'Function declaration may be incomplete',
                    vscode.DiagnosticSeverity.Warning
                ));
            }
            
        } catch (error: any) {
            diagnostics.push(new vscode.Diagnostic(
                new vscode.Range(lineNumber, 0, lineNumber, document.lineAt(lineNumber).text.length),
                `Line parse error: ${error.message}`,
                vscode.DiagnosticSeverity.Error
            ));
        }
        
        return diagnostics;
    }

    private cleanCache(): void {
        const now = Date.now();
        for (const [key, cached] of this.lineCache.entries()) {
            if ((now - cached.timestamp) > this.cacheMaxAge) {
                this.lineCache.delete(key);
            }
        }
    }

    private parseIncremental(text: string, documentKey: string): ASTNode {
        // For now, implement simple caching - in future this could be smarter
        const cachedAST = this.cachedASTs.get(documentKey);
        
        // If text is very similar to cached version, return cached AST
        if (cachedAST && this.shouldUseCachedAST(text)) {
            console.log('Using cached AST for incremental parsing');
            return cachedAST;
        }

        // Otherwise do full parse with optimizations
        console.log('Performing full parse');
        const tokens = this.lexer.tokenizeOptimized(text);
        return this.parser.parseOptimized(tokens);
    }

    private shouldUseCachedAST(text: string): boolean {
        // Simple heuristic - if text is very short, always reparse
        if (text.length < 100) return false;
        
        // For longer text, use cache more aggressively during fast typing
        return this.isTypingFast;
    }

    private validateASTLight(ast: ASTNode, diagnostics: vscode.Diagnostic[], document: vscode.TextDocument) {
        // Only do basic validation during fast typing or for large files
        if (this.isTypingFast || document.getText().length > 5000) {
            return; // Skip heavy validation
        }

        // Light validation - only check for critical errors
        if (ast.type === 'Program') {
            // Skip the main function check during typing
            console.log('Light AST validation complete');
        }
    }

    public getCompletions(document: vscode.TextDocument, position: vscode.Position): vscode.CompletionItem[] {
        // Disable completions during fast typing
        if (this.isTypingFast) {
            return [];
        }

        console.log(`getCompletions called at line ${position.line}, char ${position.character}`);
        
        try {
            const line = document.lineAt(position);
            const textBeforeCursor = line.text.substring(0, position.character);
            
            // Much more restrictive - only provide completions for very short lines
            if (textBeforeCursor.length > 5) {
                return [];
            }
            
            if (!this.shouldProvideCompletions(textBeforeCursor)) {
                return [];
            }

            // Minimal completion set
            const keywords = ['func', 'var', 'class', 'public', 'private', 'static'];
            
            return keywords.map(keyword => 
                new vscode.CompletionItem(keyword, vscode.CompletionItemKind.Keyword)
            );
        } catch (error) {
            console.error('Error in getCompletions:', error);
            return [];
        }
    }

    public getHover(document: vscode.TextDocument, position: vscode.Position): vscode.Hover | null {
        // Disable hover during fast typing
        if (this.isTypingFast) {
            return null;
        }

        console.log(`getHover called at line ${position.line}, char ${position.character}`);
        
        const range = document.getWordRangeAtPosition(position);
        if (!range) return null;

        const word = document.getText(range);
        
        // Minimal keyword info for performance
        const keywordInfo: { [key: string]: string } = {
            'func': 'Define a function',
            'var': 'Declare a variable',
            'class': 'Define a class',
            'public': 'Public access modifier',
            'private': 'Private access modifier',
            'static': 'Static modifier'
        };

        if (keywordInfo[word]) {
            return new vscode.Hover(new vscode.MarkdownString(keywordInfo[word]), range);
        }

        return null;
    }

    private shouldProvideCompletions(textBeforeCursor: string): boolean {
        // Much more restrictive conditions to prevent slowdown
        const trimmed = textBeforeCursor.trim();
        
        // Don't provide completions inside strings
        const quotes = (textBeforeCursor.match(/"/g) || []).length;
        if (quotes % 2 === 1) return false;
        
        // Don't provide completions inside comments
        if (textBeforeCursor.includes('//')) return false;
        
        // Only provide completions at very specific trigger points
        return (
            trimmed === '' ||                           // Start of line
            textBeforeCursor.endsWith(' ') ||          // After space
            textBeforeCursor.endsWith('\t') ||         // After tab
            textBeforeCursor.endsWith('{') ||          // After opening brace
            textBeforeCursor.endsWith('}') ||          // After closing brace
            textBeforeCursor.endsWith(';') ||          // After semicolon
            textBeforeCursor.endsWith('\n')            // After newline
        );
    }

    private validateAST(ast: ASTNode, diagnostics: vscode.Diagnostic[], document: vscode.TextDocument) {
        // Basic validation rules
        if (ast.type === 'Program') {
            let hasMainFunction = false;
            
            for (const child of ast.children || []) {
                if (child.type === 'FunctionDeclaration' && child.name === 'main') {
                    hasMainFunction = true;
                }
            }

            if (!hasMainFunction) {
                const diagnostic = new vscode.Diagnostic(
                    new vscode.Range(0, 0, 0, 1),
                    'No main function found. Uhigh programs must have a main() function.',
                    vscode.DiagnosticSeverity.Warning
                );
                diagnostics.push(diagnostic);
            }
        }
        console.log(`Validating AST node of type: ${ast.type}, name: ${ast.name || 'N/A'}, value: ${ast.value || 'N/A'}`);
        // Recursively validate children
        for (const child of ast.children || []) {
            this.validateAST(child, diagnostics, document);
        }
    }
}
