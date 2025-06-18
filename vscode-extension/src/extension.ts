import * as vscode from 'vscode';
import { UhighParser } from './parser';
import { UhighCompletionProvider } from './completion';
import { UhighHoverProvider } from './hover';
import { UhighSymbolProvider } from './symbols';
import { UhighDiagnosticsProvider } from './diagnostics';

export function activate(context: vscode.ExtensionContext) {
    console.log('Uhigh Language Extension is now active!');

    // Create parser and diagnostics collection
    const parser = new UhighParser();
    const diagnosticsCollection = vscode.languages.createDiagnosticCollection('uhigh');
    const diagnosticsProvider = new UhighDiagnosticsProvider(parser, diagnosticsCollection);

    // Debounce map for diagnostics updates
    const debounceTimers = new Map<string, NodeJS.Timeout>();

    // Register language providers
    const completionProvider = vscode.languages.registerCompletionItemProvider(
        { scheme: 'file', language: 'uhigh' },
        new UhighCompletionProvider(parser),
        '.', ':' // Trigger characters
    );

    const hoverProvider = vscode.languages.registerHoverProvider(
        { scheme: 'file', language: 'uhigh' },
        new UhighHoverProvider(parser)
    );

    const symbolProvider = vscode.languages.registerDocumentSymbolProvider(
        { scheme: 'file', language: 'uhigh' },
        new UhighSymbolProvider(parser)
    );

    // Debounced diagnostics update function
    const updateDiagnosticsDebounced = (document: vscode.TextDocument) => {
        const uri = document.uri.toString();
        
        // Clear existing timer
        if (debounceTimers.has(uri)) {
            clearTimeout(debounceTimers.get(uri)!);
        }
        
        // Set new timer
        const timer = setTimeout(() => {
            try {
                diagnosticsProvider.updateDiagnostics(document);
            } catch (error) {
                console.error('Error updating diagnostics:', error);
            }
            debounceTimers.delete(uri);
        }, 500); // 500ms debounce
        
        debounceTimers.set(uri, timer);
    };

    // Update diagnostics when document changes (debounced)
    const diagnosticsWatcher = vscode.workspace.onDidChangeTextDocument(event => {
        if (event.document.languageId === 'uhigh') {
            updateDiagnosticsDebounced(event.document);
        }
    });

    // Update diagnostics when document is opened (immediate)
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
            const uri = document.uri.toString();
            if (debounceTimers.has(uri)) {
                clearTimeout(debounceTimers.get(uri)!);
                debounceTimers.delete(uri);
            }
            diagnosticsCollection.delete(document.uri);
        }
    });

    // Register compile command
    const compileCommand = vscode.commands.registerCommand('uhigh.compile', async (uri?: vscode.Uri) => {
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

        } catch (error) {
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
        } else {
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
    context.subscriptions.push(
        completionProvider,
        hoverProvider,
        symbolProvider,
        diagnosticsCollection,
        diagnosticsWatcher,
        openDocumentWatcher,
        closeDocumentWatcher,
        compileCommand,
        showOutputCommand,
        parseCurrentFileCommand,
        statusBarItem,
        statusBarWatcher
    );

    // Log successful activation
    console.log('Uhigh Language Extension registered all providers successfully');
    outputChannel.appendLine('All language providers registered successfully');
}

export function deactivate() {
    console.log('Uhigh Language Extension deactivated');
}
