@echo off
setlocal
dotnet publish "%~dp0QuickSlots.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "%~dp0publish-v0.12"
if errorlevel 1 exit /b %errorlevel%
echo Ready: %~dp0publish-v0.12\QuickSlots.exe
pause
