$ErrorActionPreference = 'Stop'

Write-Host 'Epic Camera Scanner - Windows EXE Builder' -ForegroundColor Cyan
Write-Host ''

$project = Join-Path $PSScriptRoot 'EpicCameraScanner.csproj'
$output = Join-Path $PSScriptRoot 'Published'
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue

if (-not $dotnet) {
    Write-Host '.NET 8 SDK was not found. Installing it for the current user...' -ForegroundColor Yellow
    $installer = Join-Path $env:TEMP 'dotnet-install.ps1'
    Invoke-WebRequest 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $installer -Channel 8.0 -InstallDir "$env:LOCALAPPDATA\Microsoft\dotnet"
    $env:PATH = "$env:LOCALAPPDATA\Microsoft\dotnet;$env:PATH"
    $dotnet = Get-Command dotnet -ErrorAction Stop
}

if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}

Write-Host 'Restoring packages...' -ForegroundColor Cyan
& dotnet restore $project -r win-x64

Write-Host 'Publishing self-contained Windows executable...' -ForegroundColor Cyan
& dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $output

$exe = Join-Path $output 'EpicCameraScanner.exe'
if (-not (Test-Path $exe)) {
    throw 'Build completed but EpicCameraScanner.exe was not found.'
}

Write-Host ''
Write-Host 'Build complete:' -ForegroundColor Green
Write-Host $exe -ForegroundColor Green
Write-Host ''
Write-Host 'Press Ctrl+Alt+S to scan. Output is wrapped as \BARCODE\.'
Start-Process explorer.exe -ArgumentList "/select,`"$exe`""
