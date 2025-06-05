@echo off
setlocal enabledelayedexpansion

REM 集める先のフォルダ
set OUTPUT_DIR=publish_packages

REM フォルダ作成
if not exist %OUTPUT_DIR% (
    mkdir %OUTPUT_DIR%
)

REM .nupkg を探してコピー（Releaseフォルダを対象）
for /R %%F in (*.nupkg) do (
    echo Copying: %%~nxF
    copy /Y "%%F" "%OUTPUT_DIR%\%%~nxF" >nul
)

echo.
echo All .nupkg files copied to "%OUTPUT_DIR%"