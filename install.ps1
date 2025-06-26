#!/usr/bin/pwsh -c

$ErrorActionPreference = "Stop"

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
# dotnet pack uhigh.csproj
dotnet tool update --global --add-source ./bin/Release uhigh
exit $LASTEXITCODE