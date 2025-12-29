param(
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release',

    [switch] $UpdateTestAssets
)

$ErrorActionPreference = 'Stop'

$projectName = 'SampleApp'

$repoRoot = & git rev-parse --show-toplevel
$testAssetsDir = Join-Path $repoRoot 'tests/assets'
$sampleAppProjectPath = Join-Path $testAssetsDir $projectName "$projectName.csproj"
$sampleAppPublishProjectOutput = Join-Path $repoRoot "__artifacts/publish/$projectName"

function PublishNetPlatform() {
    param(
        [string] $Platform,

        [ValidateSet('net10.0', 'net48')]
        [string] $TargetFramework,

        [switch] $Prefer32Bit
    )

    if ($Prefer32Bit -and ($Platform -ne 'Any CPU' -or $TargetFramework -ne 'net48')) {
        throw "Prefer32Bit can only be set when Platform is 'Any CPU' and TargetFramework is 'net48'."
    }

    $suffix = if ($Prefer32Bit) { ' (Prefer 32-bit)' } else { '' }
    Write-Host "Publishing for .NET $TargetFramework/$Platform$suffix" -ForegroundColor Cyan

    $extraParams = @()
    if ($Platform -eq 'Any CPU' -or ($TargetFramework -eq 'net48')) {
        $artifactsPivot = ("$($Configuration)_$($TargetFramework)_$Platform" -replace ' ', '').ToLower()

        if ($Prefer32Bit) {
            $artifactsPivot += '_prefer32bit'
        }

        $extraParams += "/p:Platform=$Platform", "/p:ArtifactsPivots=$artifactsPivot"
    } else {
        $rid = switch ($Platform) {
            'x64'   { 'win-x64' }
            'x86'   { 'win-x86' }
            'ARM64' { 'win-arm64' }
            default { throw "Unsupported platform: $Platform" }
        }
        $extraParams += '-r', $rid
    }

    if ($Prefer32Bit) {
        $extraParams += '/p:Prefer32Bit=true'
    }

    dotnet publish $sampleAppProjectPath -c $Configuration -f $TargetFramework @extraParams

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to publish for .NET $TargetFramework/$Platform$suffix" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host 'Done' -ForegroundColor Green
}

function PublishAot() {
    param(
        [string] $RuntimeIdentifier,

        [string] $TargetFramework = 'net10.0'
    )
    Write-Host "Publishing AOT for $TargetFramework/$RuntimeIdentifier" -ForegroundColor Cyan

    dotnet publish $sampleAppProjectPath -c $Configuration -f $TargetFramework -r $RuntimeIdentifier `
        /p:PublishAot=true `
        /p:OptimizationPreference=Size `
        /p:TrimMode=link `
        /p:UseSystemResourceKeys=true `
        /p:StackTraceSupport=false `
        /p:StripSymbols=true

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to publish AOT for $TargetFramework/$RuntimeIdentifier" -ForegroundColor Red
        exit $LASTEXITCODE
    }

    Write-Host 'Done' -ForegroundColor Green
}

function RunBuilds() {
    PublishNetPlatform -TargetFramework 'net10.0' -Platform 'Any CPU'
    PublishNetPlatform -TargetFramework 'net10.0' -Platform x64
    PublishNetPlatform -TargetFramework 'net10.0' -Platform x86
    PublishNetPlatform -TargetFramework 'net10.0' -Platform ARM64

    PublishNetPlatform -TargetFramework 'net48' -Platform 'Any CPU'
    PublishNetPlatform -TargetFramework 'net48' -Platform 'Any CPU' -Prefer32Bit
    PublishNetPlatform -TargetFramework 'net48' -Platform x64
    PublishNetPlatform -TargetFramework 'net48' -Platform x86
    PublishNetPlatform -TargetFramework 'net48' -Platform ARM64

    # PublishAot -RuntimeIdentifier win-x86
    # PublishAot -RuntimeIdentifier win-x64
    # PublishAot -RuntimeIdentifier win-arm64
}

function CleanOutputs {
    Get-ChildItem -Recurse -Path $sampleAppPublishProjectOutput/* -Include *.pdb,*.json,*.config |
        Remove-Item -Force -ErrorAction SilentlyContinue
    Write-Host "Cleaned build outputs." -ForegroundColor Green
}

function UpdateTestAssets {
    CleanOutputs

    $testBinariesDir = Join-Path $testAssetsDir 'binaries'

    Write-Host "Updating test assets from $sampleAppPublishProjectOutput to $testBinariesDir" -ForegroundColor Cyan

    # Clear existing test assets
    if (Test-Path $testBinariesDir) {
        Remove-Item $testBinariesDir -Force -Recurse -ErrorAction SilentlyContinue
    }

    New-Item -ItemType Directory -Path $testBinariesDir -Force | Out-Null

    # Copy new build outputs to test assets
    Copy-Item -Path "$sampleAppPublishProjectOutput/*" -Destination $testBinariesDir -Recurse

    & git add "$testBinariesDir/**/*" --force

    Write-Host "Test assets updated and staged." -ForegroundColor Green
}

Push-Location $PSScriptRoot

try {
    Remove-Item -Recurse -Force $sampleAppPublishProjectOutput -ErrorAction SilentlyContinue
    RunBuilds

    if ($UpdateTestAssets) {
        UpdateTestAssets
    }
}
finally {
    Pop-Location
}
