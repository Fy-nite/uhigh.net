"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.deactivate = exports.activate = void 0;
const vscode = require("vscode");
const parser_1 = require("./parser");
const completion_1 = require("./completion");
const hover_1 = require("./hover");
const symbols_1 = require("./symbols");
const diagnostics_1 = require("./diagnostics");
function activate(context) {
    console.log('Uhigh Language Extension is now active!');
    // Create parser and diagnostics collection
    const parser = new parser_1.UhighParser();
    const diagnosticsCollection = vscode.languages.createDiagnosticCollection('uhigh');
    const diagnosticsProvider = new diagnostics_1.UhighDiagnosticsProvider(parser, diagnosticsCollection);
    // Register language providers
    const completionProvider = vscode.languages.registerCompletionItemProvider({ scheme: 'file', language: 'uhigh' }, new completion_1.UhighCompletionProvider(parser), '.', ':' // Trigger characters
    );
    const hoverProvider = vscode.languages.registerHoverProvider({ scheme: 'file', language: 'uhigh' }, new hover_1.UhighHoverProvider(parser));
    const symbolProvider = vscode.languages.registerDocumentSymbolProvider({ scheme: 'file', language: 'uhigh' }, new symbols_1.UhighSymbolProvider(parser));
    // Update diagnostics when document changes
    const diagnosticsWatcher = vscode.workspace.onDidChangeTextDocument(event => {
        if (event.document.languageId === 'uhigh') {
            diagnosticsProvider.updateDiagnostics(event.document);
        }
    });
    // Update diagnostics when document is opened
    const openDocumentWatcher = vscode.workspace.onDidOpenTextDocument(document => {
        if (document.languageId === 'uhigh') {
            diagnosticsProvider.updateDiagnostics(document);
        }
    });
    // Update diagnostics for already open documents
    vscode.workspace.textDocuments.forEach(document => {
        if (document.languageId === 'uhigh') {
            diagnosticsProvider.updateDiagnostics(document);
        }
    });
    // Clear diagnostics when document is closed
    const closeDocumentWatcher = vscode.workspace.onDidCloseTextDocument(document => {
        if (document.languageId === 'uhigh') {
            diagnosticsCollection.delete(document.uri);
        }
    });
    // Register compile command
    const compileCommand = vscode.commands.registerCommand('uhigh.compile', async (uri) => {
        const fileUri = uri || vscode.window.activeTextEditor?.document.uri;
        if (!fileUri) {
            vscode.window.showErrorMessage('No Uhigh file to compile');
            return;
        }
        if (!fileUri.fsPath.endsWith('.uh')) {
            vscode.window.showErrorMessage('Selected file is not a Uhigh file (.uh)');
            return;
        }
        try {
            // Show progress
            await vscode.window.withProgress({
                location: vscode.ProgressLocation.Notification,
                title: 'Compiling Uhigh file...',
                cancellable: false
            }, async (progress) => {
                progress.report({ increment: 0, message: 'Starting compilation...' });
                // Use the uhigh compiler
                const terminal = vscode.window.createTerminal('Uhigh Compiler');
                terminal.sendText(`dotnet run "${fileUri.fsPath}"`);
                terminal.show();
                progress.report({ increment: 100, message: 'Compilation started in terminal' });
            });
        }
        catch (error) {
            vscode.window.showErrorMessage(`Compilation failed: ${error}`);
        }
    });
    // Register status bar item
    const statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
    statusBarItem.text = "$(gear) Uhigh";
    statusBarItem.tooltip = "Uhigh Language Support";
    statusBarItem.command = 'uhigh.compile';
    // Show status bar item when Uhigh files are open
    const updateStatusBar = () => {
        const activeEditor = vscode.window.activeTextEditor;
        if (activeEditor && activeEditor.document.languageId === 'uhigh') {
            statusBarItem.show();
        }
        else {
            statusBarItem.hide();
        }
    };
    // Update status bar on editor change
    const statusBarWatcher = vscode.window.onDidChangeActiveTextEditor(updateStatusBar);
    updateStatusBar(); // Initial update
    // Add output channel for logging
    const outputChannel = vscode.window.createOutputChannel('Uhigh Language');
    outputChannel.appendLine('Uhigh Language Extension activated');
    outputChannel.appendLine(`Parser initialized with built-in language support`);
    // Register additional commands
    const showOutputCommand = vscode.commands.registerCommand('uhigh.showOutput', () => {
        outputChannel.show();
    });
    const parseCurrentFileCommand = vscode.commands.registerCommand('uhigh.parseCurrentFile', () => {
        const activeEditor = vscode.window.activeTextEditor;
        if (!activeEditor || activeEditor.document.languageId !== 'uhigh') {
            vscode.window.showErrorMessage('No active Uhigh file');
            return;
        }
        const parsed = parser.parseDocument(activeEditor.document);
        outputChannel.clear();
        outputChannel.appendLine('=== Parse Results ===');
        outputChannel.appendLine(`Functions: ${parsed.functions.length}`);
        parsed.functions.forEach(func => {
            outputChannel.appendLine(`  - ${func.name}(${func.parameters.map(p => p.name).join(', ')})`);
        });
        outputChannel.appendLine(`Variables: ${parsed.variables.length}`);
        parsed.variables.forEach(variable => {
            outputChannel.appendLine(`  - ${variable.name}${variable.type ? `: ${variable.type}` : ''}`);
        });
        outputChannel.appendLine(`Classes: ${parsed.classes.length}`);
        parsed.classes.forEach(cls => {
            outputChannel.appendLine(`  - ${cls.name} (${cls.methods.length} methods, ${cls.fields.length} fields)`);
        });
        outputChannel.appendLine(`Errors: ${parsed.errors.length}`);
        parsed.errors.forEach(error => {
            outputChannel.appendLine(`  - ${error.message}`);
        });
        outputChannel.show();
    });
    // Register all disposables
    context.subscriptions.push(completionProvider, hoverProvider, symbolProvider, diagnosticsCollection, diagnosticsWatcher, openDocumentWatcher, closeDocumentWatcher, compileCommand, showOutputCommand, parseCurrentFileCommand, statusBarItem, statusBarWatcher);
    // Log successful activation
    console.log('Uhigh Language Extension registered all providers successfully');
    outputChannel.appendLine('All language providers registered successfully');
}
exports.activate = activate;
function deactivate() {
    console.log('Uhigh Language Extension deactivated');
}
exports.deactivate = deactivate;
//# sourceMappingURL=extension.js.map