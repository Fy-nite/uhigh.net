using uhigh.Net.Diagnostics;

namespace uhigh.Net.Templates.BuiltIn
{
    /// <summary>
    /// Template for creating test projects
    /// </summary>
    public class TestProjectTemplate : BaseProjectTemplate
    {
        /// <summary>
        /// Gets the name of the template
        /// </summary>
        public override string Name => "test";
        
        /// <summary>
        /// Gets the description of the template
        /// </summary>
        public override string Description => "Creates a test project with testing infrastructure";
        
        /// <summary>
        /// Gets the output type this template creates
        /// </summary>
        public override string OutputType => "Exe";
        
        /// <summary>
        /// Gets the author of the template
        /// </summary>
        public override string Author => "μHigh Built-in";
        
        /// <summary>
        /// Gets the parameters that this template accepts
        /// </summary>
        public override Dictionary<string, string> GetParameters()
        {
            var baseParams = base.GetParameters();
            baseParams.Add("testFramework", "Testing framework to use (default: builtin)");
            return baseParams;
        }
        
        /// <summary>
        /// Creates the project file for the template
        /// </summary>
        protected override async Task<bool> CreateProjectFileAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics)
        {
            try
            {
                var description = GetParameter(parameters, "description", (string?)null);
                var author = GetParameter(parameters, "author", (string?)null);
                var targetFramework = GetParameter(parameters, "targetFramework", "net9.0");
                
                var project = new uhighProject
                {
                    Name = projectName,
                    Version = "1.0.0",
                    Description = description,
                    Author = author,
                    Target = targetFramework,
                    OutputType = OutputType,
                    SourceFiles = new List<string> { "Tests.uh" },
                    RootNamespace = projectName,
                    Nullable = true
                };
                
                var projectFilePath = Path.Combine(projectPath, $"{projectName}.uhighproj");
                return await ProjectFile.SaveAsync(project, projectFilePath, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to create project file: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Creates the source files for the template
        /// </summary>
        protected override async Task<bool> CreateSourceFilesAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics)
        {
            try
            {
                var testFilePath = Path.Combine(projectPath, "Tests.uh");
                
                var sanitizedNamespace = SanitizeNamespaceIdentifier(projectName);
                var sourceCode = $@"// {projectName} - Test Project
using System
using StdLib

namespace {sanitizedNamespace}
{{
    /// <summary>
    /// Test class for demonstrating μHigh testing features
    /// </summary>
    [TestingOnly]
    public class SampleTestClass
    {{
        [Expect(42)]
        public static field expectedValue: int = 0
        
        [Expect(""Hello, World!"")]
        public static field expectedString: string = """"
    }}
    
    /// <summary>
    /// Main test program
    /// </summary>
    public class Program
    {{
        /// <summary>
        /// Test method that demonstrates basic testing
        /// </summary>
        [TestWith(typeof(SampleTestClass))]
        public static func TestBasicFunctionality(testObj: {sanitizedNamespace}.SampleTestClass): void
        {{
            // Arrange
            testObj.expectedValue = 42;
            testObj.expectedString = ""Hello, World!"";
            
            // Act
            var actualValue = testObj.expectedValue;
            var actualString = testObj.expectedString;
            
            // Assert (using built-in test framework)
            Console.WriteLine(""Testing value: expected 42, got "" + actualValue);
            Console.WriteLine(""Testing string: expected 'Hello, World!', got '"" + actualString + ""'"");
        }}
        
        /// <summary>
        /// Another test method
        /// </summary>
        public static func TestMathOperations(): void
        {{
            var a = 5;
            var b = 3;
            var sum = a + b;
            var product = a * b;
            
            Console.WriteLine(""Math test: "" + a + "" + "" + b + "" = "" + sum);
            Console.WriteLine(""Math test: "" + a + "" * "" + b + "" = "" + product);
            
            // Simple assertions
            if (sum == 8)
            {{
                Console.WriteLine(""✓ Addition test passed"");
            }}
            else
            {{
                Console.WriteLine(""✗ Addition test failed"");
            }}
            
            if (product == 15)
            {{
                Console.WriteLine(""✓ Multiplication test passed"");
            }}
            else
            {{
                Console.WriteLine(""✗ Multiplication test failed"");
            }}
        }}
        
        /// <summary>
        /// Main entry point for the test application
        /// </summary>
        /// <param name=""args"">Command line arguments</param>
        /// <returns>void</returns>
        public static func Main(args: string[]): void
        {{
            Console.WriteLine(""Running {projectName} tests..."");
            Console.WriteLine(""==============================="");
            
            // Run custom tests
            TestMathOperations();
            
            Console.WriteLine();
            Console.WriteLine(""Running μHigh built-in tests..."");
            
            // Run built-in test framework
            InlineTestRunner.RunAllTests();
            
            Console.WriteLine(""==============================="");
            Console.WriteLine(""Tests completed."");
        }}
    }}
}}";
                
                await File.WriteAllTextAsync(testFilePath, sourceCode);
                diagnostics?.ReportInfo($"Created test source file: {testFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                diagnostics?.ReportError($"Failed to create source files: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Creates additional files for the template
        /// </summary>
        protected override async Task CreateAdditionalFilesAsync(string projectName, string projectPath, Dictionary<string, object>? parameters, DiagnosticsReporter? diagnostics)
        {
            try
            {
                // Create a README file
                var readmeFilePath = Path.Combine(projectPath, "README.md");
                var readmeContent = $@"# {projectName}

A test project created with μHigh.

## Running Tests

```bash
uhigh run {projectName}.uhighproj
```

## Test Framework

This project uses the μHigh built-in testing framework with the following features:

- `[TestingOnly]` attribute for test classes
- `[Expect(value)]` attribute for expected values
- `[TestWith(typeof(Class))]` attribute for test methods
- `InlineTestRunner.RunAllTests()` for running all tests

## Writing Tests

### Using Attributes

```csharp
[TestingOnly]
public class MyTestClass
{{
    [Expect(42)]
    public static field expectedNumber: int = 0
}}

[TestWith(typeof(MyTestClass))]
public static func TestSomething(testObj: MyTestClass): void
{{
    testObj.expectedNumber = 42;
    // Test logic here
}}
```

### Manual Testing

```csharp
public static func TestManually(): void
{{
    var result = SomeFunction();
    if (result == expectedValue)
    {{
        Console.WriteLine(""✓ Test passed"");
    }}
    else
    {{
        Console.WriteLine(""✗ Test failed"");
    }}
}}
```

## About

This project was created using the test project template for μHigh.
";
                
                await File.WriteAllTextAsync(readmeFilePath, readmeContent);
                diagnostics?.ReportInfo($"Created README file: {readmeFilePath}");
            }
            catch (Exception ex)
            {
                diagnostics?.ReportWarning($"Failed to create additional files: {ex.Message}");
            }
        }
    }
}