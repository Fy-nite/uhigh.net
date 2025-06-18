"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.UhighSymbolProvider = void 0;
const vscode = require("vscode");
class UhighSymbolProvider {
    constructor(parser) {
        this.parser = parser;
    }
    provideDocumentSymbols(document, token) {
        const parsed = this.parser.parseDocument(document);
        const symbols = [];
        // Add functions
        parsed.functions.forEach(func => {
            const symbol = new vscode.DocumentSymbol(func.name, this.getFunctionDetail(func), vscode.SymbolKind.Function, func.range, func.range);
            symbols.push(symbol);
        });
        // Add variables
        parsed.variables.forEach(variable => {
            const symbol = new vscode.DocumentSymbol(variable.name, variable.type || 'unknown', variable.isConstant ? vscode.SymbolKind.Constant : vscode.SymbolKind.Variable, variable.range, variable.range);
            symbols.push(symbol);
        });
        // Add classes
        parsed.classes.forEach(cls => {
            const classSymbol = new vscode.DocumentSymbol(cls.name, 'class', vscode.SymbolKind.Class, cls.range, cls.range);
            // Add class methods as children
            cls.methods.forEach(method => {
                const methodSymbol = new vscode.DocumentSymbol(method.name, this.getFunctionDetail(method), vscode.SymbolKind.Method, method.range, method.range);
                classSymbol.children.push(methodSymbol);
            });
            // Add class fields as children
            cls.fields.forEach(field => {
                const fieldSymbol = new vscode.DocumentSymbol(field.name, field.type || 'unknown', field.isConstant ? vscode.SymbolKind.Constant : vscode.SymbolKind.Field, field.range, field.range);
                classSymbol.children.push(fieldSymbol);
            });
            symbols.push(classSymbol);
        });
        return symbols;
    }
    getFunctionDetail(func) {
        const params = func.parameters.map((p) => p.type ? `${p.name}: ${p.type}` : p.name).join(', ');
        return `(${params})${func.returnType ? `: ${func.returnType}` : ''}`;
    }
}
exports.UhighSymbolProvider = UhighSymbolProvider;
//# sourceMappingURL=symbols.js.map