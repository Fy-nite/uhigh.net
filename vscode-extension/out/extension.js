"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || function (mod) {
    if (mod && mod.__esModule) return mod;
    var result = {};
    if (mod != null) for (var k in mod) if (k !== "default" && Object.prototype.hasOwnProperty.call(mod, k)) __createBinding(result, mod, k);
    __setModuleDefault(result, mod);
    return result;
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.deactivate = exports.activate = void 0;
const vscode = __importStar(require("vscode"));
const languageServer_1 = require("./languageServer");
let languageServer;
let diagnosticCollection;
let isActivated = false; // Add flag to prevent multiple activations
function activate(context) {
    console.log('Uhigh extension activate() called');
    // Prevent multiple activations
    if (isActivated) {
        console.warn('Uhigh extension already activated, skipping duplicate activation');
        return;
    }
    isActivated = true;
    languageServer = new languageServer_1.UhighLanguageServer();
    diagnosticCollection = vscode.languages.createDiagnosticCollection('uhigh');
    if (languageServer.isTypingFast) {
        console.log('Skipping validation due to fast typing');
        return;
    }
    // Register commands
    const compileCommand = vscode.commands.registerCommand('uhigh.compile', () => {
        if (languageServer.isTypingFast) {
            console.log('Skipping validation due to fast typing');
            return;
        }
        const editor = vscode.window.activeTextEditor;
        if (editor && editor.document.languageId === 'uhigh') {
            vscode.window.showInformationMessage('Compiling Uhigh file...');
            // TODO: Implement compilation
        }
    });
    const parseCommand = vscode.commands.registerCommand('uhigh.parseCurrentFile', () => {
        if (languageServer.isTypingFast) {
            console.log('Skipping validation due to fast typing');
            return;
        }
        const editor = vscode.window.activeTextEditor;
        if (editor && editor.document.languageId === 'uhigh') {
            const diagnostics = languageServer.validateDocument(editor.document);
            diagnosticCollection.set(editor.document.uri, diagnostics);
            vscode.window.showInformationMessage(`Parse complete. Found ${diagnostics.length} issues.`);
        }
    });
    // Temporarily disable completion provider to test performance
    // const completionProvider = vscode.languages.registerCompletionItemProvider(
    //     'uhigh',
    //     {
    //         provideCompletionItems(document: vscode.TextDocument, position: vscode.Position) {
    //             return languageServer.getCompletions(document, position);
    //         }
    //     },
    //     // Add trigger characters to limit when completions are provided
    //     '.', ':', ' ', '\n'
    // );
    // Disable ALL language features temporarily to isolate the issue
    // Re-enable hover provider
    console.log('Registering language providers...');
    const hoverProvider = vscode.languages.registerHoverProvider('uhigh', {
        provideHover(document, position) {
            if (languageServer.isTypingFast) {
                console.log('Skipping validation due to fast typing');
                return;
            }
            return languageServer.getHover(document, position);
        }
    });
    // Replace document-level validation with line-level validation
    const onDidChangeTextEditorSelection = vscode.window.onDidChangeTextEditorSelection((event) => {
        // add a 100ms debounce to avoid excessive validation
        if (languageServer.isTypingFast) {
            console.log('Skipping validation due to fast typing');
            return;
        }
        const editor = event.textEditor;
        if (editor && editor.document.languageId === 'uhigh') {
            const position = editor.selection.active;
            const diagnostics = languageServer.validateLine(editor.document, position.line);
            // Only update diagnostics for the current line
            const allDiagnostics = diagnosticCollection.get(editor.document.uri) || [];
            const otherLineDiagnostics = allDiagnostics.filter(d => d.range.start.line !== position.line);
            const newDiagnostics = [...otherLineDiagnostics, ...diagnostics];
            diagnosticCollection.set(editor.document.uri, newDiagnostics);
        }
    });
    // Keep save validation for full document check
    const onDidSaveTextDocument = vscode.workspace.onDidSaveTextDocument((document) => {
        if (document.languageId === 'uhigh') {
            const diagnostics = languageServer.validateDocument(document);
            diagnosticCollection.set(document.uri, diagnostics);
        }
    });
    console.log('Language providers registered successfully');
    // Be more careful about what we push to subscriptions
    const subscriptions = [
        compileCommand,
        parseCommand,
        hoverProvider,
        diagnosticCollection,
        onDidChangeTextEditorSelection,
        onDidSaveTextDocument
    ];
    console.log(`Adding ${subscriptions.length} subscriptions to context`);
    context.subscriptions.push(...subscriptions);
    console.log('Uhigh extension activation complete');
}
exports.activate = activate;
function deactivate() {
    console.log('Uhigh extension deactivate() called');
    isActivated = false;
    if (diagnosticCollection) {
        diagnosticCollection.dispose();
    }
    return Promise.resolve();
}
exports.deactivate = deactivate;
//# sourceMappingURL=extension.js.map