#!/usr/bin/pwsh -c

$ErrorActionPreference = "Stop"

Write-Host "Building μHigh compiler..."
dotnet pack uhigh.csproj
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

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