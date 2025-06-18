import * as vscode from 'vscode';
import { UhighParser } from './parser';

export class UhighCompletionProvider implements vscode.CompletionItemProvider {
    constructor(private parser: UhighParser) {}

    provideCompletionItems(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken,
        context: vscode.CompletionContext
    ): vscode.ProviderResult<vscode.CompletionItem[] | vscode.CompletionList> {
        
        const parsed = this.parser.parseDocument(document);
        const completions: vscode.CompletionItem[] = [];

        // Keywords and modifiers
        const keywords = [
            'func', 'var', 'const', 'class', 'namespace', 'import',
            'if', 'else', 'while', 'for', 'return', 'break', 'continue',
            'public', 'private', 'protected', 'static', 'readonly',
            'int', 'float', 'string', 'bool', 'void', 'true', 'false', 'null', 'this', 'new'
        ];

        keywords.forEach(keyword => {
            const item = new vscode.CompletionItem(keyword, vscode.CompletionItemKind.Keyword);
            if (['public', 'private', 'protected', 'static', 'readonly'].includes(keyword)) {
                item.detail = 'Access modifier';
                item.documentation = `${keyword} access modifier`;
            }
            completions.push(item);
        });

        // Built-in functions
        parsed.builtInFunctions.forEach(func => {
            const item = new vscode.CompletionItem(func.name, vscode.CompletionItemKind.Function);
            item.detail = this.getFunctionSignature(func);
            item.documentation = `Built-in function: ${func.name}`;
            
            // Create snippet for function call
            const paramSnippets = func.parameters.map((param, index) => `\${${index + 1}:${param.name}}`).join(', ');
            item.insertText = new vscode.SnippetString(`${func.name}(${paramSnippets})`);
            
            completions.push(item);
        });

        // Built-in classes and their methods
        parsed.builtInClasses.forEach(cls => {
            const item = new vscode.CompletionItem(cls.name, vscode.CompletionItemKind.Class);
            item.detail = `Built-in class ${cls.name}`;
            item.documentation = `Built-in class with ${cls.methods.length} methods and ${cls.fields.length} fields`;
            completions.push(item);

            // Add class methods
            cls.methods.forEach(method => {
                const methodItem = new vscode.CompletionItem(`${cls.name}.${method.name}`, vscode.CompletionItemKind.Method);
                methodItem.detail = this.getFunctionSignature(method);
                methodItem.documentation = `Static method of ${cls.name}`;
                
                const paramSnippets = method.parameters.map((param, index) => `\${${index + 1}:${param.name}}`).join(', ');
                methodItem.insertText = new vscode.SnippetString(`${cls.name}.${method.name}(${paramSnippets})`);
                
                completions.push(methodItem);
            });

            // Add class fields
            cls.fields.forEach(field => {
                const fieldItem = new vscode.CompletionItem(`${cls.name}.${field.name}`, vscode.CompletionItemKind.Field);
                fieldItem.detail = field.type || 'unknown type';
                fieldItem.documentation = `Static field of ${cls.name}`;
                completions.push(fieldItem);
            });
        });

        // Built-in variables (like args)
        parsed.builtInVariables.forEach(variable => {
            const item = new vscode.CompletionItem(variable.name, vscode.CompletionItemKind.Variable);
            item.detail = variable.type || 'unknown type';
            item.documentation = 'Built-in variable';
            completions.push(item);
        });

        // User-defined functions
        parsed.functions.forEach(func => {
            const item = new vscode.CompletionItem(func.name, vscode.CompletionItemKind.Function);
            item.detail = this.getFunctionSignature(func);
            item.documentation = `User function: ${func.name}`;
            
            // Create snippet for function call
            const paramSnippets = func.parameters.map((param, index) => `\${${index + 1}:${param.name}}`).join(', ');
            item.insertText = new vscode.SnippetString(`${func.name}(${paramSnippets})`);
            
            completions.push(item);
        });

        // User-defined variables
        parsed.variables.forEach(variable => {
            const item = new vscode.CompletionItem(variable.name, vscode.CompletionItemKind.Variable);
            item.detail = variable.type || 'unknown type';
            item.documentation = variable.isConstant ? 'Constant' : 'Variable';
            completions.push(item);
        });

        // User-defined classes
        parsed.classes.forEach(cls => {
            const item = new vscode.CompletionItem(cls.name, vscode.CompletionItemKind.Class);
            item.detail = `class ${cls.name}`;
            item.documentation = `Class with ${cls.methods.length} methods and ${cls.fields.length} fields`;
            completions.push(item);

            // Add class methods
            cls.methods.forEach(method => {
                const methodItem = new vscode.CompletionItem(`${cls.name}.${method.name}`, vscode.CompletionItemKind.Method);
                methodItem.detail = this.getFunctionSignature(method);
                methodItem.documentation = `Method of ${cls.name}`;
                
                const paramSnippets = method.parameters.map((param, index) => `\${${index + 1}:${param.name}}`).join(', ');
                methodItem.insertText = new vscode.SnippetString(`${cls.name}.${method.name}(${paramSnippets})`);
                
                completions.push(methodItem);
            });

            // Add class fields
            cls.fields.forEach(field => {
                const fieldItem = new vscode.CompletionItem(`${cls.name}.${field.name}`, vscode.CompletionItemKind.Field);
                fieldItem.detail = field.type || 'unknown type';
                fieldItem.documentation = `Field of ${cls.name}`;
                completions.push(fieldItem);
            });
        });

        return completions;
    }

    private getFunctionSignature(func: any): string {
        const modifiers = func.modifiers && func.modifiers.length > 0 ? func.modifiers.join(' ') + ' ' : '';
        const params = func.parameters.map((p: any) => `${p.name}${p.type ? `: ${p.type}` : ''}`).join(', ');
        return `${modifiers}${func.name}(${params})${func.returnType ? `: ${func.returnType}` : ''}`;
    }
}
