//////////////////////////////////////////////////////////////////////
// μHigh Build Script (build.cake)
//////////////////////////////////////////////////////////////////////


// Arguments
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var solution = "./uhigh.net.sln";
var project = "./uhigh.csproj";
var targetLang = Argument("targetLang", "csharp");

// Tasks

Task("Clean")
    .Does(() =>
{
    CleanDirectories("./**/bin/" + configuration);
    CleanDirectories("./**/obj/" + configuration);
    Information("Cleaned build output directories.");
});

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetRestore(solution);
    Information("NuGet packages restored.");
});

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    // Example: pass targetLang to your CLI if needed
    // DotNetBuild(solution, ...); // For .NET projects
    // If you have a custom CLI, use:
    // StartProcess("dotnet", $"run -- compile --target {targetLang} ...");
    DotNetBuild(solution, new DotNetBuildSettings {
        Configuration = configuration,
        NoRestore = true
    });
    Information("Solution built.");
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    // If you have tests, adjust the path accordingly
    var testProjects = GetFiles("./tests/**/*.csproj");
    foreach(var testProject in testProjects)
    {
        DotNetTest(testProject.FullPath, new DotNetTestSettings {
            Configuration = configuration,
            NoBuild = true
        });
    }
    Information("Tests executed.");
});

Task("Pack")
    .IsDependentOn("Build")
    .Does(() =>
{
    DotNetPack(project, new DotNetPackSettings {
        Configuration = configuration,
        OutputDirectory = "./artifacts/",
        NoBuild = true
    });
    Information("NuGet package created.");
});

Task("Default")
    .IsDependentOn("Build");

// Run target
RunTarget(target);