# Define target runtimes
$runtimes = @(
    @{ Name = "win-x64"; Platform = "Windows" },
    @{ Name = "linux-x64"; Platform = "Linux" },
    @{ Name = "osx-x64"; Platform = "macOS" }
)

# Get the version from the project file
$projectFile = Get-ChildItem -Path . -Filter "*.csproj" | Select-Object -First 1
if ($projectFile) {
    [xml]$xmlContent = Get-Content $projectFile.FullName
    $version = $xmlContent.Project.PropertyGroup.Version
    if ($version) {
        Write-Host "Version: $version"
        
        # Build and package for each runtime
        foreach ($runtime in $runtimes) {
            Write-Host "Building for $($runtime.Platform) ($($runtime.Name))..."
            
            # Create platform-specific output directory
            $outputDir = "./publish/$($runtime.Name)"
            
            # Publish for specific runtime
            dotnet publish uhigh.csproj -c Release -r $($runtime.Name) --self-contained true -o $outputDir
            
            if ($LASTEXITCODE -eq 0) {
                # Create platform-specific zip file
                $zipFileName = "uhigh-$version-$($runtime.Name).zip"
                $zipFilePath = Join-Path -Path (Get-Location) -ChildPath $zipFileName
                Compress-Archive -Path "$outputDir/*" -DestinationPath $zipFilePath -Force
                Write-Host "Package created: $zipFilePath"
            } else {
                Write-Host "Failed to build for $($runtime.Platform)" -ForegroundColor Red
            }
        }
    } else {
        Write-Host "Version not found in project file." -ForegroundColor Red
    }
} else {
    Write-Host "No project file found." -ForegroundColor Red
}

# Clean up publish directory
Remove-Item -Path "./publish" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "Build process completed."
