using System.IO;
using System.Threading.Tasks;
using uhigh.Net.Testing;
using uhigh.Net.UbPackage;
using uhigh.Net.Diagnostics;

namespace uhigh.Net.Tests
{
    /// <summary>
    /// Tests for .ub package functionality
    /// </summary>
    public class UbPackageTests
    {
        [Test]
        public static async Task TestPackageManifestSerialization()
        {
            var manifest = new PackageManifest
            {
                Name = "TestPackage",
                Version = "1.2.3",
                Description = "Test package description",
                Author = "Test Author",
                TargetFramework = "net9.0",
                OutputType = "Library",
                SourceFiles = new List<string> { "main.uh", "utils.uh" }
            };

            manifest.NugetDependencies["Newtonsoft.Json"] = "13.0.1";
            manifest.UbDependencies["OtherPackage"] = "2.0.0";

            var json = manifest.ToJson();
            Assert.IsNotNull(json, "JSON should not be null");
            Assert.IsTrue(json.Contains("TestPackage"), "JSON should contain package name");
            Assert.IsTrue(json.Contains("1.2.3"), "JSON should contain version");

            var deserialized = PackageManifest.FromJson(json);
            Assert.IsNotNull(deserialized, "Deserialized manifest should not be null");
            Assert.AreEqual("TestPackage", deserialized.Name, "Name should match");
            Assert.AreEqual("1.2.3", deserialized.Version, "Version should match");
            Assert.AreEqual(2, deserialized.SourceFiles.Count, "Should have 2 source files");
            Assert.AreEqual("net9.0", deserialized.TargetFramework, "Target framework should match");
        }

        [Test]
        public static async Task TestPackageManifestValidation()
        {
            var validManifest = new PackageManifest
            {
                Name = "Valid",
                Version = "1.0.0",
                SourceFiles = new List<string> { "main.uh" }
            };

            Assert.IsTrue(validManifest.IsValid(), "Valid manifest should pass validation");

            var invalidManifest = new PackageManifest
            {
                Name = "",
                Version = "1.0.0",
                SourceFiles = new List<string>()
            };

            Assert.IsFalse(invalidManifest.IsValid(), "Invalid manifest should fail validation");
        }

        [Test]
        public static async Task TestPackageManifestFromProject()
        {
            var project = new uhighProject
            {
                Name = "TestProject",
                Version = "2.0.0",
                Description = "Test project",
                Author = "Test Author",
                Target = "net9.0",
                OutputType = "Exe",
                SourceFiles = new List<string> { "Program.uh" }
            };

            project.Dependencies.Add(new PackageReference
            {
                Name = "System.Text.Json",
                Version = "7.0.0"
            });

            var manifest = PackageManifest.FromProject(project);

            Assert.AreEqual("TestProject", manifest.Name, "Name should match");
            Assert.AreEqual("2.0.0", manifest.Version, "Version should match");
            Assert.AreEqual("Exe", manifest.OutputType, "OutputType should match");
            Assert.AreEqual(1, manifest.SourceFiles.Count, "Should have 1 source file");
            Assert.AreEqual(1, manifest.NugetDependencies.Count, "Should have 1 NuGet dependency");
            Assert.IsTrue(manifest.NugetDependencies.ContainsKey("System.Text.Json"), "Should contain NuGet dependency");
        }

        [Test]
        public static async Task TestProjectFromPackageManifest()
        {
            var manifest = new PackageManifest
            {
                Name = "TestLib",
                Version = "1.5.0",
                Description = "Test library",
                Author = "Test Author",
                TargetFramework = "net9.0",
                OutputType = "Library",
                SourceFiles = new List<string> { "lib.uh" }
            };

            manifest.NugetDependencies["Microsoft.Extensions.Logging"] = "7.0.0";

            var project = manifest.ToProject();

            Assert.AreEqual("TestLib", project.Name, "Name should match");
            Assert.AreEqual("1.5.0", project.Version, "Version should match");
            Assert.AreEqual("Library", project.OutputType, "OutputType should match");
            Assert.AreEqual(1, project.SourceFiles.Count, "Should have 1 source file");
            Assert.AreEqual(1, project.Dependencies.Count, "Should have 1 dependency");
            Assert.AreEqual("Microsoft.Extensions.Logging", project.Dependencies[0].Name, "Dependency name should match");
        }

        [Test]
        public static async Task TestPackageCreationAndExtraction()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "uhigh-package-test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a test project
                var projectPath = Path.Combine(tempDir, "TestProject.uhighproj");
                var sourceFile = Path.Combine(tempDir, "test.uh");

                var project = new uhighProject
                {
                    Name = "TestProject",
                    Version = "1.0.0",
                    Description = "Test project for packaging",
                    SourceFiles = new List<string> { "test.uh" }
                };

                await File.WriteAllTextAsync(sourceFile, "// Test source file\nnamespace TestProject { }");
                await ProjectFile.SaveAsync(project, projectPath);

                // Create package
                var packagePath = Path.Combine(tempDir, "TestProject.ub");
                var packageManager = new UbPackageManager();

                var packSuccess = await packageManager.PackAsync(projectPath, packagePath);
                Assert.IsTrue(packSuccess, "Package creation should succeed");
                Assert.IsTrue(File.Exists(packagePath), "Package file should exist");

                // Read manifest from package
                var manifest = await packageManager.ReadManifestAsync(packagePath);
                Assert.IsNotNull(manifest, "Manifest should not be null");
                Assert.AreEqual("TestProject", manifest.Name, "Package name should match");
                Assert.AreEqual("1.0.0", manifest.Version, "Package version should match");

                // Extract package
                var extractPath = Path.Combine(tempDir, "extracted");
                var extractSuccess = await packageManager.UnpackAsync(packagePath, extractPath);
                Assert.IsTrue(extractSuccess, "Package extraction should succeed");

                var extractedSourceFile = Path.Combine(extractPath, "test.uh");
                Assert.IsTrue(File.Exists(extractedSourceFile), "Extracted source file should exist");

                var extractedManifest = Path.Combine(extractPath, "package.json");
                Assert.IsTrue(File.Exists(extractedManifest), "Extracted manifest should exist");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public static async Task TestPackageInstallation()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "uhigh-install-test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a library package
                var libDir = Path.Combine(tempDir, "lib");
                Directory.CreateDirectory(libDir);

                var libProjectPath = Path.Combine(libDir, "TestLib.uhighproj");
                var libSourceFile = Path.Combine(libDir, "lib.uh");

                var libProject = new uhighProject
                {
                    Name = "TestLib",
                    Version = "1.0.0",
                    Description = "Test library",
                    OutputType = "Library",
                    SourceFiles = new List<string> { "lib.uh" }
                };

                await File.WriteAllTextAsync(libSourceFile, "// Test library\nnamespace TestLib { public class Utils { } }");
                await ProjectFile.SaveAsync(libProject, libProjectPath);

                // Create package
                var packagePath = Path.Combine(tempDir, "TestLib.ub");
                var packageManager = new UbPackageManager();
                await packageManager.PackAsync(libProjectPath, packagePath);

                // Create consumer project
                var appDir = Path.Combine(tempDir, "app");
                Directory.CreateDirectory(appDir);

                var appProjectPath = Path.Combine(appDir, "TestApp.uhighproj");
                var appSourceFile = Path.Combine(appDir, "main.uh");

                var appProject = new uhighProject
                {
                    Name = "TestApp",
                    Version = "1.0.0",
                    Description = "Test application",
                    OutputType = "Exe",
                    SourceFiles = new List<string> { "main.uh" }
                };

                await File.WriteAllTextAsync(appSourceFile, "// Test app\nnamespace TestApp { }");
                await ProjectFile.SaveAsync(appProject, appProjectPath);

                // Install package
                var installSuccess = await packageManager.InstallAsync(packagePath, appProjectPath);
                Assert.IsTrue(installSuccess, "Package installation should succeed");

                // Verify package is listed as installed
                var installedPackages = await packageManager.ListInstalledAsync(appProjectPath);
                Assert.AreEqual(1, installedPackages.Count, "Should have 1 installed package");
                Assert.AreEqual("TestLib", installedPackages[0].Name, "Package name should match");
                Assert.AreEqual("1.0.0", installedPackages[0].Version, "Package version should match");

                // Verify package cache exists
                var cachePath = Path.Combine(appDir, ".ub-packages", "TestLib-1.0.0");
                Assert.IsTrue(Directory.Exists(cachePath), "Package cache directory should exist");

                var cachedSource = Path.Combine(cachePath, "lib.uh");
                Assert.IsTrue(File.Exists(cachedSource), "Cached source file should exist");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Test]
        public static async Task TestBuildFromPackageWithoutOutputPlacesFileInSameDirectory()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "uhigh-build-test", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // Create a simple executable project
                var projectDir = Path.Combine(tempDir, "project");
                Directory.CreateDirectory(projectDir);

                var projectPath = Path.Combine(projectDir, "TestExe.uhighproj");
                var sourceFile = Path.Combine(projectDir, "main.uh");

                var project = new uhighProject
                {
                    Name = "TestExe",
                    Version = "1.0.0",
                    Description = "Test executable",
                    OutputType = "Exe",
                    SourceFiles = new List<string> { "main.uh" }
                };

                // Create a simple main function
                var sourceCode = @"func main() {
    print(""Hello World"");
}";

                await File.WriteAllTextAsync(sourceFile, sourceCode);
                var saveSuccess = await uhigh.Net.ProjectFile.SaveAsync(project, projectPath);
                Assert.IsTrue(saveSuccess, "Project file save should succeed");

                // Create package in a different directory from the project
                var packageDir = Path.Combine(tempDir, "packages");
                Directory.CreateDirectory(packageDir);
                var packagePath = Path.Combine(packageDir, "TestExe.ub");

                var diagnostics = new uhigh.Net.Diagnostics.DiagnosticsReporter();
                var packageManager = new uhigh.Net.UbPackage.UbPackageManager(diagnostics);

                var packSuccess = await packageManager.PackAsync(projectPath, packagePath);
                Assert.IsTrue(packSuccess, "Package creation should succeed");
                Assert.IsTrue(File.Exists(packagePath), "Package file should exist");

                // Now build from package without specifying output
                var buildSuccess = await packageManager.BuildFromPackageAsync(packagePath);
                Assert.IsTrue(buildSuccess, "Build from package should succeed");

                // The expected output should be in the same directory as the .ub file
                var expectedOutputPath = Path.Combine(packageDir, "TestExe.exe");
                Assert.IsTrue(File.Exists(expectedOutputPath), 
                    $"Output executable should exist at {expectedOutputPath} when --output is not specified");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}