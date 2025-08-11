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

    // just run the test command 
    // for the compiler it's dotnet run test

    // run tests
    StartProcess("dotnet run test", new ProcessSettings {
        RedirectStandardOutput = true,
        RedirectStandardError = true
    });

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

Task("All")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack");
// Run target
RunTarget(target);