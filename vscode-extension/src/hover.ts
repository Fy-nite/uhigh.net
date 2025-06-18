import * as vscode from 'vscode';
import { UhighParser } from './parser';

export class UhighHoverProvider implements vscode.HoverProvider {
    constructor(private parser: UhighParser) {}

    provideHover(
        document: vscode.TextDocument,
        position: vscode.Position,
        token: vscode.CancellationToken
    ): vscode.ProviderResult<vscode.Hover> {
        
        const parsed = this.parser.parseDocument(document);
        const wordRange = document.getWordRangeAtPosition(position);
        
        if (!wordRange) {
            return undefined;
        }

        const word = document.getText(wordRange);

        // Check functions
        const func = parsed.functions.find(f => f.name === word);
        if (func) {
            const signature = this.getFunctionSignature(func);
            const markdown = new vscode.MarkdownString();
            markdown.appendCodeblock(`func ${signature}`, 'uhigh');
            markdown.appendMarkdown(`\n\n**Function**: ${func.name}`);
            if (func.parameters.length > 0) {
                markdown.appendMarkdown(`\n\n**Parameters**: ${func.parameters.length}`);
            }
            return new vscode.Hover(markdown, wordRange);
        }

        // Check variables
        const variable = parsed.variables.find(v => v.name === word);
        if (variable) {
            const markdown = new vscode.MarkdownString();
            const varType = variable.isConstant ? 'const' : 'var';
            markdown.appendCodeblock(`${varType} ${variable.name}${variable.type ? `: ${variable.type}` : ''}`, 'uhigh');
            markdown.appendMarkdown(`\n\n**${variable.isConstant ? 'Constant' : 'Variable'}**: ${variable.name}`);
            if (variable.type) {
                markdown.appendMarkdown(`\n\n**Type**: ${variable.type}`);
            }
            return new vscode.Hover(markdown, wordRange);
        }

        // Check classes
        const cls = parsed.classes.find(c => c.name === word);
        if (cls) {
            const markdown = new vscode.MarkdownString();
            markdown.appendCodeblock(`class ${cls.name}`, 'uhigh');
            markdown.appendMarkdown(`\n\n**Class**: ${cls.name}`);
            markdown.appendMarkdown(`\n\n**Methods**: ${cls.methods.length}`);
            markdown.appendMarkdown(`\n\n**Fields**: ${cls.fields.length}`);
            return new vscode.Hover(markdown, wordRange);
        }

        // Built-in functions
        const builtins: { [key: string]: string } = {
            'print': 'Built-in function to print values to console',
            'input': 'Built-in function to read user input',
            'Console': 'System console class',
            'WriteLine': 'Write a line to the console'
        };

        if (builtins[word]) {
            const markdown = new vscode.MarkdownString();
            markdown.appendMarkdown(`**${word}**: ${builtins[word]}`);
            return new vscode.Hover(markdown, wordRange);
        }

        return undefined;
    }

    private getFunctionSignature(func: any): string {
        const params = func.parameters.map((p: any) => `${p.name}${p.type ? `: ${p.type}` : ''}`).join(', ');
        return `${func.name}(${params})${func.returnType ? `: ${func.returnType}` : ''}`;
    }
}
