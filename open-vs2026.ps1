$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (-not (Test-Path $vswhere)) {
    Write-Error "vswhere.exe が見つかりません: $vswhere"
    exit 1
}

$vsPath = & $vswhere -version "[18.0,19.0)" -latest -property installationPath

if (-not $vsPath) {
    Write-Error "Visual Studio 2026 (version 18.x) がインストールされていません。"
    exit 1
}

$devenv = "$vsPath\Common7\IDE\devenv.exe"
$sln = Join-Path $PSScriptRoot "FrostNova.slnx"

Write-Host "VS 2026: $devenv"
Start-Process -FilePath $devenv -ArgumentList "`"$sln`""
