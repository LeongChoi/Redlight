$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

dotnet publish RedLightForWindows.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --source https://api.nuget.org/v3/index.json --output publish

if ($LASTEXITCODE -ne 0) {
    throw "The Windows build failed with exit code $LASTEXITCODE. See the errors above."
}

Move-Item -Path publish/RedLightForWindows.exe -Destination publish/RedLightCompact.exe -Force
Write-Host "Built publish/RedLightCompact.exe"
