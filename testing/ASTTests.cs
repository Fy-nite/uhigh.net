using uhigh.Net.Parser;
using uhigh.Net.Lexer;

namespace uhigh.Net.Testing
{
    public class ASTTests
    {
        [Test]
        public void TestLiteralExpressionTypes()
        {
            var stringLiteral = new LiteralExpression { Value = "test", Type = TokenType.String };
            var numberLiteral = new LiteralExpression { Value = 42, Type = TokenType.Number };
            var boolLiteral = new LiteralExpression { Value = true, Type = TokenType.Boolean };

            Assert.AreEqual("test", stringLiteral.Value);
            Assert.AreEqual(TokenType.String, stringLiteral.Type);
            Assert.AreEqual(42, numberLiteral.Value);
            Assert.AreEqual(TokenType.Number, numberLiteral.Type);
            Assert.AreEqual(true, boolLiteral.Value);
            Assert.AreEqual(TokenType.Boolean, boolLiteral.Type);
        }

        [Test]
        public void TestBinaryExpressionStructure()
        {
            var left = new IdentifierExpression { Name = "x" };
            var right = new LiteralExpression { Value = 5, Type = TokenType.Number };
            var binExpr = new BinaryExpression 
            { 
                Left = left, 
                Operator = TokenType.Plus, 
                Right = right 
            };

            Assert.AreEqual(left, binExpr.Left);
            Assert.AreEqual(TokenType.Plus, binExpr.Operator);
            Assert.AreEqual(right, binExpr.Right);
            Assert.IsTrue(binExpr.Left is IdentifierExpression);
            Assert.IsTrue(binExpr.Right is LiteralExpression);
        }

        [Test]
        public void TestFunctionDeclarationStructure()
        {
            var param1 = new Parameter("x", "int");
            var param2 = new Parameter("y", "int");
            var returnStmt = new ReturnStatement 
            { 
                Value = new BinaryExpression 
                { 
                    Left = new IdentifierExpression { Name = "x" },
                    Operator = TokenType.Plus,
                    Right = new IdentifierExpression { Name = "y" }
                }
            };

            var funcDecl = new FunctionDeclaration
            {
                Name = "add",
                Parameters = new List<Parameter> { param1, param2 },
                ReturnType = "int",
                Body = new List<Statement> { returnStmt }
            };

            Assert.AreEqual("add", funcDecl.Name);
            Assert.AreEqual(2, funcDecl.Parameters.Count);
            Assert.AreEqual("x", funcDecl.Parameters[0].Name);
            Assert.AreEqual("int", funcDecl.Parameters[0].Type);
            Assert.AreEqual("int", funcDecl.ReturnType);
            Assert.AreEqual(1, funcDecl.Body.Count);
            Assert.IsTrue(funcDecl.Body[0] is ReturnStatement);
        }

        [Test]
        public void TestClassDeclarationStructure()
        {
            var field = new FieldDeclaration 
            { 
                Name = "name", 
                Type = "string",
                Modifiers = new List<string> { "private" }
            };

            var method = new MethodDeclaration
            {
                Name = "getName",
                ReturnType = "string",
                Body = new List<Statement>
                {
                    new ReturnStatement 
                    { 
                        Value = new MemberAccessExpression 
                        { 
                            Object = new ThisExpression(), 
                            MemberName = "name" 
                        }
                    }
                },
                Modifiers = new List<string> { "public" }
            };

            var classDecl = new ClassDeclaration
            {
                Name = "Person",
                Members = new List<Statement> { field, method },
                Modifiers = new List<string> { "public" }
            };

            Assert.AreEqual("Person", classDecl.Name);
            Assert.IsTrue(classDecl.IsPublic);
            Assert.AreEqual(2, classDecl.Members.Count);
        }

        [Test]
        public void TestQualifiedIdentifierFunctionality()
        {
            var qualifiedId = new QualifiedIdentifierExpression { Name = "System.Console.WriteLine" };

            var parts = qualifiedId.GetParts();
            Assert.AreEqual(3, parts.Length);
            Assert.AreEqual("System", parts[0]);
            Assert.AreEqual("Console", parts[1]);
            Assert.AreEqual("WriteLine", parts[2]);

            Assert.AreEqual("System.Console", qualifiedId.GetNamespace());
            Assert.AreEqual("WriteLine", qualifiedId.GetMethodName());
        }

        [Test]
        public void TestArrayExpressionStructure()
        {
            var elements = new List<Expression>
            {
                new LiteralExpression { Value = 1, Type = TokenType.Number },
                new LiteralExpression { Value = 2, Type = TokenType.Number },
                new LiteralExpression { Value = 3, Type = TokenType.Number }
            };

            var arrayExpr = new ArrayExpression { Elements = elements };

            Assert.AreEqual(3, arrayExpr.Elements.Count);
            Assert.IsTrue(arrayExpr.Elements.All(e => e is LiteralExpression));
        }

        [Test]
        public void TestCallExpressionStructure()
        {
            var function = new IdentifierExpression { Name = "print" };
            var argument = new LiteralExpression { Value = "Hello", Type = TokenType.String };
            var callExpr = new CallExpression 
            { 
                Function = function, 
                Arguments = new List<Expression> { argument } 
            };

            Assert.AreEqual(function, callExpr.Function);
            Assert.AreEqual(1, callExpr.Arguments.Count);
            Assert.AreEqual(argument, callExpr.Arguments[0]);
        }

        [Test]
        public void TestConstructorCallExpression()
        {
            var arg1 = new LiteralExpression { Value = "John", Type = TokenType.String };
            var arg2 = new LiteralExpression { Value = 25, Type = TokenType.Number };
            var ctorCall = new ConstructorCallExpression
            {
                ClassName = "Person",
                Arguments = new List<Expression> { arg1, arg2 }
            };

            Assert.AreEqual("Person", ctorCall.ClassName);
            Assert.AreEqual(2, ctorCall.Arguments.Count);
            Assert.AreEqual("John", ((LiteralExpression)ctorCall.Arguments[0]).Value);
            Assert.AreEqual(25, ((LiteralExpression)ctorCall.Arguments[1]).Value);
        }

        [Test]
        public void TestMemberAccessExpression()
        {
            var obj = new IdentifierExpression { Name = "person" };
            var memberAccess = new MemberAccessExpression 
            { 
                Object = obj, 
                MemberName = "name" 
            };

            Assert.AreEqual(obj, memberAccess.Object);
            Assert.AreEqual("name", memberAccess.MemberName);
        }

        [Test]
        public void TestPropertyDeclarationWithAccessors()
        {
            var getter = new PropertyAccessor { Type = "get" };
            var setter = new PropertyAccessor { Type = "set" };
            var property = new PropertyDeclaration
            {
                Name = "Name",
                Type = "string",
                Accessors = new List<PropertyAccessor> { getter, setter }
            };

            Assert.AreEqual("Name", property.Name);
            Assert.AreEqual("string", property.Type);
            Assert.AreEqual(2, property.Accessors.Count);
            Assert.IsTrue(property.HasAutoImplementedAccessors);
            Assert.IsFalse(property.HasCustomAccessors);
        }

        [Test]
        public void TestAttributeDeclaration()
        {
            var arg = new LiteralExpression { Value = "value", Type = TokenType.String };
            var attribute = new AttributeDeclaration
            {
                Name = "Test",
                Arguments = new List<Expression> { arg }
            };

            Assert.AreEqual("Test", attribute.Name);
            Assert.AreEqual(1, attribute.Arguments.Count);
        }
    }
}
