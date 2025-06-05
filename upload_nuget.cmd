@echo off
setlocal

REM NuGet API Key（環境変数から取得）
set API_KEY=%NUGET_API_KEY%

if "%API_KEY%"=="" (
    echo [ERROR] Please set the NUGET_API_KEY environment variable.
    exit /b 1
)

REM アップロード元のフォルダ
set INPUT_DIR=publish_packages

REM NuGet サーバーの URL
set NUGET_SOURCE=https://api.nuget.org/v3/index.json

REM 一括 push
for %%F in (%INPUT_DIR%\*.nupkg) do (
    echo Pushing: %%~nxF
    dotnet nuget push "%%F" --api-key %API_KEY% --source %NUGET_SOURCE% --skip-duplicate
)

echo.
echo All packages pushed.