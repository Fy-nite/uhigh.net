using uhigh.Net.Diagnostics;

namespace uhigh.Net.Parser
{
     
    public static class SemanticAnalyzer
    {
        public static void Analyze(Program program, DiagnosticsReporter diagnostics)
        {
            var functionNames = new HashSet<string>();
            foreach (var stmt in program.Statements)
            {
                if (stmt is ClassDeclaration cls)
                {
                    AnalyzeClass(cls, diagnostics);
                }
                if (stmt is FunctionDeclaration func)
                {
                    // Detect duplicate function names
                    if (!functionNames.Add(func.Name))
                    {
                        diagnostics.ReportWarning(
                            $"Duplicate function name '{func.Name}' detected.",
                            0, 0, "UH305");
                    }
                    AnalyzeFunction(func, diagnostics);
                }

            }
        }

        private static void AnalyzeClass(ClassDeclaration cls, DiagnosticsReporter diagnostics)
        {
            var nonNullFields = cls.Members
                .OfType<FieldDeclaration>()
                .Where(f => f.Type != null && !f.Type.EndsWith("?") && f.Initializer == null)
                .ToList();

            var constructors = cls.Members
                .OfType<MethodDeclaration>()
                .Where(m => m.IsConstructor)
                .ToList();

            foreach (var field in nonNullFields)
            {
                bool initializedInCtor = constructors.Any(ctor =>
                    ctor.Body.OfType<AssignmentExpression>().Any(assign =>
                        assign.Target is IdentifierExpression id && id.Name == field.Name));

                if (!initializedInCtor)
                {
                    diagnostics.ReportWarning(
                        $"Non-nullable field '{field.Name}' is not initialized in any constructor.",
                        field.Line, field.Column, "UH301");
                }
            }

            // New: Check for duplicate member names
            var memberNames = new HashSet<string>();
            foreach (var member in cls.Members)
            {
                string? name = member switch
                {
                    FieldDeclaration f => f.Name,
                    PropertyDeclaration p => p.Name,
                    MethodDeclaration m => m.Name,
                    _ => null
                };
                if (name != null)
                {
                    if (!memberNames.Add(name))
                    {
                        diagnostics.ReportWarning(
                            $"Duplicate member name '{name}' in class '{cls.Name}'.",
                            0, 0, "UH302");
                    }
                }
            }
        }

        // New: Analyze function for unused variables and return type validation
        private static void AnalyzeFunction(FunctionDeclaration func, DiagnosticsReporter diagnostics)
        {
            // Unused variable detection (local variables)
            var declaredVars = func.Body.OfType<VariableDeclaration>().Select(v => v.Name).ToHashSet();
            var usedVars = new HashSet<string>();

            // Unused parameter detection
            var paramNames = func.Parameters.Select(p => p.Name).ToHashSet();
            var usedParams = new HashSet<string>();

            // Shadowed variable detection
            foreach (var varName in declaredVars)
            {
                if (paramNames.Contains(varName))
                {
                    diagnostics.ReportWarning(
                        $"Variable '{varName}' shadows a parameter in function '{func.Name}'.",
                        0, 0, "UH306");
                }
            }

            // Too many parameters
            if (func.Parameters.Count > 7)
            {
                diagnostics.ReportWarning(
                    $"Function '{func.Name}' has {func.Parameters.Count} parameters (consider refactoring).",
                    0, 0, "UH307");
            }

            // Overly long function
            if (func.Body.Count > 50)
            {
                diagnostics.ReportWarning(
                    $"Function '{func.Name}' has {func.Body.Count} statements (consider splitting).",
                    0, 0, "UH308");
            }

            // Deeply nested block detection
            int maxDepth = 0;
            void CheckDepth(List<Statement> stmts, int depth)
            {
                if (depth > maxDepth) maxDepth = depth;
                foreach (var s in stmts)
                {
                    switch (s)
                    {
                        case IfStatement ifs:
                            CheckDepth(ifs.ThenBranch, depth + 1);
                            if (ifs.ElseBranch != null) CheckDepth(ifs.ElseBranch, depth + 1);
                            break;
                        case WhileStatement ws:
                            CheckDepth(ws.Body, depth + 1);
                            break;
                        case ForStatement fs:
                            CheckDepth(fs.Body, depth + 1);
                            break;
                        // Add more block types as needed
                    }
                }
            }
            CheckDepth(func.Body, 1);
            if (maxDepth > 3)
            {
                diagnostics.ReportWarning(
                    $"Function '{func.Name}' has deeply nested blocks (nesting level {maxDepth}).",
                    0, 0, "UH309");
            }

            // Magic number detection
            void CheckMagicNumbers(Expression expr)
            {
                switch (expr)
                {
                    case LiteralExpression lit when lit.Value is int val:
                        if (val != 0 && val != 1 && val != -1 && val != 100)
                        {
                            diagnostics.ReportWarning(
                                $"Magic number '{val}' detected in function '{func.Name}'.",
                                0, 0, "UH310");
                        }
                        break;
                    case BinaryExpression bin:
                        CheckMagicNumbers(bin.Left);
                        CheckMagicNumbers(bin.Right);
                        break;
                    case UnaryExpression unary:
                        CheckMagicNumbers(unary.Operand);
                        break;
                    case AssignmentExpression assign:
                        CheckMagicNumbers(assign.Value);
                        break;
                    case CallExpression call:
                        foreach (var arg in call.Arguments) CheckMagicNumbers(arg);
                        break;
                    case ArrayExpression arr:
                        foreach (var el in arr.Elements) CheckMagicNumbers(el);
                        break;
                    // ...add more as needed
                }
            }

            // Unreachable code detection
            bool foundReturn = false;
            // Helper to collect used variable and parameter names from expressions
            void CollectUsedVars(Expression expr)
            {
                switch (expr)
                {
                    case IdentifierExpression id:
                        if (declaredVars.Contains(id.Name))
                            usedVars.Add(id.Name);
                        if (paramNames.Contains(id.Name))
                            usedVars.Add(id.Name);
                        break;
                    case AssignmentExpression assign:
                        CollectUsedVars(assign.Target);
                        CollectUsedVars(assign.Value);
                        break;
                    case BinaryExpression bin:
                        CollectUsedVars(bin.Left);
                        CollectUsedVars(bin.Right);
                        break;
                    case UnaryExpression unary:
                        CollectUsedVars(unary.Operand);
                        break;
                    case CallExpression call:
                        foreach (var arg in call.Arguments) CollectUsedVars(arg);
                        break;
                    case ArrayExpression arr:
                        foreach (var el in arr.Elements) CollectUsedVars(el);
                        break;
 
                }
            }

            foreach (var stmt in func.Body)
            {
                if (foundReturn)
                {
                    diagnostics.ReportWarning(
                        $"Unreachable code detected after return in function '{func.Name}'.",
                        0, 0, "UH311");
                    break;
                }
                if (stmt is ReturnStatement)
                {
                    foundReturn = true;
                }
                if (stmt is ExpressionStatement exprStmt)
                {
                    CollectUsedVars(exprStmt.Expression);
                    CheckMagicNumbers(exprStmt.Expression);
                    if (exprStmt.Expression is AssignmentExpression assignExpr)
                        CollectUsedVars(assignExpr.Value);
                }
            }

            foreach (var varName in declaredVars)
            {
                if (!usedVars.Contains(varName))
                {
                    diagnostics.ReportWarning(
                        $"Variable '{varName}' declared but never used in function '{func.Name}'.",
                        0, 0, "UH303");
                }
            }

            // Unused parameter detection
            foreach (var paramName in paramNames)
            {
                if (!usedVars.Contains(paramName))
                {
                    diagnostics.ReportWarning(
                        $"Parameter '{paramName}' is never used in function '{func.Name}'.",
                        0, 0, "UH312");
                }
            }

            // Return type validation
            if (func.ReturnType != null && func.ReturnType != "void")
            {
                bool hasReturn = func.Body.OfType<ReturnStatement>().Any();
                if (!hasReturn)
                {
                    diagnostics.ReportWarning(
                        $"Function '{func.Name}' declares return type '{func.ReturnType}' but has no return statement.",
                        0, 0, "UH304");
                }
            }
        }
    }
}
