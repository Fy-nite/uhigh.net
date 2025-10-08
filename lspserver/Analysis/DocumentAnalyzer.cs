// using LanguageServer.Parameters;
// using LanguageServer.Parameters.TextDocument;
// using uhigh.Net.Lexer;
// using uhigh.Net.Parser;

// namespace UhighLanguageServer.Analysis;
// using CompilerDiagnostic = uhigh.Net.Diagnostics.Diagnostic;
// using CompilerDiagnosticSeverity = uhigh.Net.Diagnostics.DiagnosticSeverity;
// using CompilerDiagnosticsReporter = uhigh.Net.Diagnostics.DiagnosticsReporter;
// using CompilerSourceLocation = uhigh.Net.Diagnostics.SourceLocation;
// using LspDiagnostic = LanguageServer.Parameters.TextDocument.Diagnostic;
// // using LspDiagnosticSeverity = LanguageServer.Parameters.DiagnosticSeverity;
// using LspPosition = LanguageServer.Parameters.Position;
// using LspRange = LanguageServer.Parameters.Range;

// /// <summary>
// /// Parses μHigh source files and maps compiler diagnostics to LSP diagnostics.
// /// </summary>
// public sealed class DocumentAnalyzer
// {
//     private const string DiagnosticSource = "uhigh";

//     public DocumentAnalysisResult Analyze(DocumentSnapshot snapshot)
//     {
//         try
//         {
//             var diagnosticsReporter = new CompilerDiagnosticsReporter(
//                 verboseMode: false,
//                 sourceFileName: snapshot.Uri.IsFile ? snapshot.Uri.LocalPath : null,
//                 suppressOutput: true);

//             List<Token> tokens;
//             Program? program = null;

//             try
//             {
//                 var lexer = new Lexer(snapshot.Text);
//                 tokens = lexer.Tokenize();
//             }
//             catch (Exception ex)
//             {
//                 var diagnostic = CreateCrashDiagnostic(snapshot, ex, "Lexer crash");
//                 return DocumentAnalysisResult.FromCrash([diagnostic]);
//             }

//             try
//             {
//                 var parser = new Parser(tokens, diagnosticsReporter);
//                 program = parser.Parse();
//             }
//             catch (Exception ex)
//             {
//                 diagnosticsReporter.ReportFatal($"Parser crashed: {ex.Message}");
//             }

//             var lspDiagnostics = diagnosticsReporter.Diagnostics
//                 .Select(MapDiagnostic)
//                 .Take(512)
//                 .ToArray();

//             if (lspDiagnostics.Length == 0)
//             {
//                 // Provide a friendly informational diagnostic so users know analysis succeeded.
//                 lspDiagnostics =
//                 [
//                     new LspDiagnostic
//                     {
//                         severity = LspDiagnosticSeverity.Information,
//                         message = "μHigh: no issues detected",
//                         source = DiagnosticSource,
//                         range = new LspRange
//                         {
//                             start = new Position { line = 0, character = 0 },
//                             end = new Position { line = 0, character = 0 }
//                         }
//                     }
//                 ];
//             }

//             return new DocumentAnalysisResult(lspDiagnostics, tokens, program);
//         }
//         catch (Exception ex)
//         {
//             var fallback = CreateCrashDiagnostic(snapshot, ex, "Analysis crash");
//             return DocumentAnalysisResult.FromCrash([fallback]);
//         }
//     }

//     private static LspDiagnostic MapDiagnostic(CompilerDiagnostic diagnostic)
//     {
//         var severity = diagnostic.Severity switch
//         {
//             CompilerDiagnosticSeverity.Fatal or CompilerDiagnosticSeverity.Error => LspDiagnosticSeverity.Error,
//             CompilerDiagnosticSeverity.Warning => LspDiagnosticSeverity.Warning,
//             CompilerDiagnosticSeverity.Info => LspDiagnosticSeverity.Information,
//             _ => LspDiagnosticSeverity.Information
//         };

//         var (line, character) = NormalizeLocation(diagnostic.Location);

//         var range = new LspRange
//         {
//             start = new LspPosition { line = line, character = character },
//             end = new LspPosition { line = line, character = Math.Max(character, 0) + 1 }
//         };

//         var messageBuilder = new System.Text.StringBuilder(diagnostic.Message);
//         if (!string.IsNullOrWhiteSpace(diagnostic.Suggestion))
//         {
//             messageBuilder.Append("\nSuggestion: ");
//             messageBuilder.Append(diagnostic.Suggestion);
//         }

//         if (diagnostic.Exception != null)
//         {
//             messageBuilder.Append("\nDetails: ");
//             messageBuilder.Append(diagnostic.Exception.Message);
//         }

//         return new LspDiagnostic
//         {
//             severity = severity,
//             message = messageBuilder.ToString(),
//             source = DiagnosticSource,
//             code = diagnostic.Code,
//             range = range
//         };
//     }

//     private static LspDiagnostic CreateCrashDiagnostic(DocumentSnapshot snapshot, Exception ex, string context)
//     {
//         return new LspDiagnostic
//         {
//             severity = LspDiagnosticSeverity.Error,
//             source = DiagnosticSource,
//             message = $"{context}: {ex.Message}",
//             range = new LspRange
//             {
//                 start = new LspPosition { line = 0, character = 0 },
//                 end = new LspPosition { line = 0, character = 0 }
//             }
//         };
//     }

//     private static (int line, int character) NormalizeLocation(CompilerSourceLocation? location)
//     {
//         if (location == null)
//         {
//             return (0, 0);
//         }

//         var line = Math.Max(0, location.Line - 1);
//         var character = Math.Max(0, location.Column - 1);
//         return (line, character);
//     }
// }

// public sealed record DocumentAnalysisResult(
//     IReadOnlyList<LspDiagnostic> Diagnostics,
//     IReadOnlyList<Token> Tokens,
//     Program? Program)
// {
//     public static DocumentAnalysisResult FromCrash(IReadOnlyList<LspDiagnostic> diagnostics) =>
//         new(diagnostics, Array.Empty<Token>(), null);
// }
