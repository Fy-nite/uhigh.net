#!/usr/bin/pwsh

param(
    [string]$Action
)

$ErrorActionPreference = "Stop"

$Welcome = @"
============================
μHigh Compiler Installation Script
============================
"@
Write-Host $Welcome -ForegroundColor Cyan

function Show-Menu {
    Write-Host ""
    Write-Host "Select an option:"
    Write-Host "  1) Build project (Release)"
    Write-Host "  2) Pack NuGet package"
    Write-Host "  3) Publish (self-contained)"
    Write-Host "  4) Install as global tool"
    Write-Host "  5) Uninstall global tool"
    Write-Host "  6) Clean build output"
    Write-Host "  7) Exit"
    Write-Host ""
}

function Require-DotNet {
    $dotnetVersion = dotnet --version 2>$null
    if (-not $dotnetVersion) {
        Write-Host "Error: .NET SDK is not installed or not found in PATH." -ForegroundColor Red
        Write-Host "Please install the .NET SDK from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host ".NET SDK version $dotnetVersion found." -ForegroundColor Green
    }
}

function Ensure-ProjectRoot {
    # doesnt quite work?
    # $currentDir = Get-Location
    # $projectRoot = Get-ChildItem -Path . -Recurse -Filter "uhigh.csproj" | Get-Item  | Split-Path -Parent
    # Write-Host "Current directory: $currentDir"
    # Write-Host "Project root directory: $projectRoot"
    # if ($currentDir -ne $projectRoot) {
    #     Write-Host "Error: Current directory is not the project root." -ForegroundColor Red
    #     Write-Host "Please navigate to the project root directory and run the script again." -ForegroundColor Yellow
    #     exit 1
    # }
}

function Build-Project {
    Write-Host "Building the project in Release mode..."
    dotnet build -c Release 
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    } else {
        Write-Host "Build succeeded." -ForegroundColor Green
    }
}

function Pack-Project {
    Write-Host "Packing NuGet package..."
    dotnet pack uhigh.csproj -c Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Pack failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    } else {
        Write-Host "Pack succeeded." -ForegroundColor Green
    }
}

function Publish-Project {
    Write-Host "Publishing self-contained binaries (win-x64, linux-x64, osx-x64)..."
    dotnet publish -c Release -r win-x64 --self-contained true uhigh.csproj
    dotnet publish -c Release -r linux-x64 --self-contained true uhigh.csproj
    dotnet publish -c Release -r osx-x64 --self-contained true uhigh.csproj
    Write-Host "Publish completed."
}

function Install-Tool {
    # Check if the tool is already installed
    $toolInstalled = dotnet tool list --global | Select-String -Pattern "uhigh"
    if ($toolInstalled) {
        Write-Host "Uninstalling previous version of uhigh..."
        dotnet tool uninstall --global uhigh
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    } else {
        Write-Host "uhigh is not currently installed."
    }

    Write-Host "Installing μHigh as global tool..."
    dotnet tool install --global --add-source ./bin/Release uhigh
    if ($LASTEXITCODE -eq 0) {
        Write-Host "μHigh installed successfully!"
        Write-Host "You can now use 'uhigh' command from anywhere."
        Write-Host ""
        Write-Host "Try: uhigh --help"
    } else {
        Write-Host "Installation failed with exit code $LASTEXITCODE"
    }
    exit $LASTEXITCODE
}

function Uninstall-Tool {
    Write-Host "Uninstalling μHigh global tool..."
    dotnet tool uninstall --global uhigh
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Uninstalled successfully."
    } else {
        Write-Host "Uninstall failed or tool was not installed."
    }
}

function Clean-Build {
    Write-Host "Cleaning build output..."
    dotnet clean
    Remove-Item -Recurse -Force ./bin,./obj -ErrorAction SilentlyContinue
    Write-Host "Clean complete."
}

Require-DotNet
Ensure-ProjectRoot

if ($Action) {
    switch ($Action.ToLower()) {
        "build"      { Build-Project; exit 0 }
        "pack"       { Pack-Project; exit 0 }
        "publish"    { Publish-Project; exit 0 }
        "install"    { Install-Tool; exit 0 }
        "uninstall"  { Uninstall-Tool; exit 0 }
        "clean"      { Clean-Build; exit 0 }
        "help"       { Show-Menu; exit 0 }
        default      { Write-Host "Unknown action: $Action"; Show-Menu; exit 1 }
    }
}

while ($true) {
    Show-Menu
    $choice = Read-Host "Enter your choice (1-7)"
    switch ($choice) {
        "1" { Build-Project }
        "2" { Pack-Project }
        "3" { Publish-Project }
        "4" { Install-Tool }
        "5" { Uninstall-Tool }
        "6" { Clean-Build }
        "7" { Write-Host "Exiting..."; exit 0 }
        default { Write-Host "Invalid choice. Please select a valid option." }
    }
}