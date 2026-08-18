@echo off
setlocal

cd /d "%~dp0"

echo Building EclipticaOverlay (single-file, self-contained, win-x64)...

dotnet publish EclipticaOverlay.csproj ^
    -c Release ^
    -r win-x64 ^
    --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o publish

if %errorlevel% neq 0 (
    echo.
    echo Build FAILED.
    pause
    exit /b %errorlevel%
)

echo.
echo Build succeeded: %~dp0publish\EclipticaOverlay.exe
pause
