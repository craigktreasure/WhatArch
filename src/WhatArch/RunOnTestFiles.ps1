param(
    [switch] $UseTestAppOutputs,

    [switch] $SkipBuild
)

$ErrorActionPreference = 'Stop'

$sampleAppProjectName = 'SampleApp'

$repoRoot = & git rev-parse --show-toplevel
$testAssetsDir = Join-Path $repoRoot 'tests/assets'
$sampleAppPublishProjectOutput = Join-Path $repoRoot "__artifacts/publish/$sampleAppProjectName"

$whatArchProjectPath = Join-Path $repoRoot 'src/WhatArch/WhatArch.csproj'

$testAssetDirectory = if ($UseTestAppOutputs) {
    $sampleAppPublishProjectOutput
} else {
    $testAssetsDir
}

$testFiles = Get-ChildItem -Path $testAssetDirectory -Recurse -Include *.dll,*.exe -File

if (-not $SkipBuild) {
    Write-Host "Building WhatArch..." -ForegroundColor Cyan
    & dotnet build $whatArchProjectPath

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to build WhatArch." -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host
}

foreach ($testFile in $testFiles) {
    Write-Host "Running WhatArch on $($testFile.FullName)..." -ForegroundColor Cyan

    $result = & dotnet run --project $whatArchProjectPath --no-build -- $testFile.FullName

    if ($LASTEXITCODE -ne 0) {
        Write-Host "WhatArch failed on test file: $($testFile.FullName) with:`n$result" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host "Result: $result" -ForegroundColor Green
}
