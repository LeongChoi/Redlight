@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo Install the .NET 10 SDK first: https://dotnet.microsoft.com/en-us/download/dotnet/10.0
    pause
    exit /b 1
)

dotnet publish RedLightForWindows.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --source https://api.nuget.org/v3/index.json --output publish
if errorlevel 1 (
    echo Build failed. See the errors above.
    pause
    exit /b 1
)

move /Y "publish\RedLightForWindows.exe" "publish\RedLightCompact.exe" >nul
if errorlevel 1 (
    echo The build succeeded, but renaming the executable failed.
    pause
    exit /b 1
)

echo Built: publish\RedLightCompact.exe
pause
