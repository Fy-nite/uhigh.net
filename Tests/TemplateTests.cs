using uhigh.Net.Templates;
using uhigh.Net.Templates.BuiltIn;
using uhigh.Net.Testing;

namespace uhigh.Net.Tests
{
    /// <summary>
    /// Tests for project template functionality
    /// </summary>
    public class TemplateTests
    {
        /// <summary>
        /// Tests that SanitizeNamespaceIdentifier properly handles project names with special characters
        /// </summary>
        [Test]
        public static void TestNamespaceSanitization()
        {
            // Test basic dash replacement
            var sanitized = ConsoleAppTemplate.TestSanitizeNamespaceIdentifier("my-test-project");
            Assert.AreEqual("my_test_project", sanitized);
            
            // Test multiple special characters
            sanitized = ConsoleAppTemplate.TestSanitizeNamespaceIdentifier("test@#$project");
            Assert.AreEqual("test_project", sanitized);
            
            // Test starting with digit
            sanitized = ConsoleAppTemplate.TestSanitizeNamespaceIdentifier("123-my-project");
            Assert.AreEqual("_123_my_project", sanitized);
            
            // Test valid name remains unchanged
            sanitized = ConsoleAppTemplate.TestSanitizeNamespaceIdentifier("MyValidProject");
            Assert.AreEqual("MyValidProject", sanitized);
            
            // Test edge cases
            sanitized = ConsoleAppTemplate.TestSanitizeNamespaceIdentifier("--test--");
            Assert.AreEqual("test_", sanitized);
            
            // Test empty/null handling
            sanitized = ConsoleAppTemplate.TestSanitizeNamespaceIdentifier("");
            Assert.AreEqual("Project", sanitized);
            
            // Test underscores are preserved
            sanitized = ConsoleAppTemplate.TestSanitizeNamespaceIdentifier("my_valid_namespace");
            Assert.AreEqual("my_valid_namespace", sanitized);
        }
    }
}

// Extension to access the protected method for testing
namespace uhigh.Net.Templates.BuiltIn
{
    public partial class ConsoleAppTemplate
    {
        public static string TestSanitizeNamespaceIdentifier(string projectName)
        {
            return SanitizeNamespaceIdentifier(projectName);
        }
    }
}