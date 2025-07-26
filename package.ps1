dotnet publish uhigh.csproj -c Release -o ./publish
## get the version from the project file
$projectFile = Get-ChildItem -Path . -Filter "*.csproj" | Select-Object -First 1
if ($projectFile) {
    [xml]$xmlContent = Get-Content $projectFile.FullName
    $version = $xmlContent.Project.PropertyGroup.Version
    if ($version) {
        Write-Host "Version: $version"
        # zip the publish directory with the version
        $zipFileName = "uhigh-$version.zip"
        $zipFilePath = Join-Path -Path (Get-Location) -ChildPath $zipFileName
        Compress-Archive -Path "./publish/*" -DestinationPath $zipFilePath
        Write-Host "Package created: $zipFilePath"
    } else {
        Write-Host "Version not found in project file."
    }
} else {
    Write-Host "No project file found."
}
