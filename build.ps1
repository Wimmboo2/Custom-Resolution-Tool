$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (-not (Test-Path $csc)) {
    throw "csc.exe not found at $csc"
}

$sourceDir = Join-Path $root "src"
$outDir = Join-Path $root "bin"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$sources = @(Get-ChildItem -Path $sourceDir -Filter "*.cs" | ForEach-Object { $_.FullName })
if ($sources.Count -eq 0) {
    throw "No C# sources found in $sourceDir"
}

$output = Join-Path $outDir "crt.exe"

& $csc /nologo /target:exe /platform:x64 /optimize+ /out:"$output" /r:System.dll /r:System.Core.dll $sources

if ($LASTEXITCODE -ne 0) {
    throw "build failed with exit code $LASTEXITCODE"
}

Write-Host "Built $output"
