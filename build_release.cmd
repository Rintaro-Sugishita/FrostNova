@echo off
setlocal
rem ============================================================
rem  FrostNova Release build
rem ============================================================
cd /d "%~dp0"

if "%~1"=="" (
    echo === Release build: FrostNova.slnx ===
    dotnet build "FrostNova.slnx" -c Release
) else (
    echo === Release rebuild: %~1 ===
    dotnet build "%~1" -c Release -t:Rebuild -p:SolutionDir=%~dp0
)

if errorlevel 1 (
    echo.
    echo *** BUILD FAILED ***
    exit /b 1
)

echo.
echo === Done. nupkg output: %~dp0.localNuget ===
endlocal
