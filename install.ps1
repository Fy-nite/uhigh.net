#!/usr/bin/pwsh -c

$ErrorActionPreference = "Stop"

dotnet pack uhigh.csproj
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet tool uninstall --global uhigh
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet tool update --global --add-source ./bin/Release uhigh
exit $LASTEXITCODE
