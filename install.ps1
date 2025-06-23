#!/usr/bin/pwsh -c
dotnet pack uhigh.csproj
dotnet tool uninstall --global uhigh
dotnet tool update --global --add-source ./bin/Release  uhigh
