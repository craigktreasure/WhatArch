$ErrorActionPreference = 'Stop'

if (-not (Get-Command magick -ErrorAction SilentlyContinue)) {
    Write-Host "ImageMagick 'magick' command not found. Please install ImageMagick to proceed." -ForegroundColor Red
    exit 1
}

if (-not (Get-Command pngquant -ErrorAction SilentlyContinue)) {
    Write-Host "pngquant command not found. Please install pngquant to proceed." -ForegroundColor Red
    exit 1
}

function Convert-SVGToPNG {
    param (
        [string]$inputPath,
        [string]$outputPath
    )

    & magick -background none -density 256 $inputPath -resize 128x128 $outputPath

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to generate icon." --ForegroundColor Red
        exit 1
    }
}

function Compress-PNG {
    param (
        [string]$inputPath,
        [string]$outputPath
    )

    & pngquant $inputPath --output $outputPath --force --strip --skip-if-larger

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Failed to compress PNG." --ForegroundColor Red
        exit 1
    }
}

$inputImagePath = Join-Path $PSScriptRoot "icon.svg"
$outputImagePath = Join-Path $PSScriptRoot "icon.png"

Convert-SVGToPNG -inputPath $inputImagePath -outputPath $outputImagePath
$imageSize = Get-Item $outputImagePath | Select-Object -ExpandProperty Length
Write-Host "Generated PNG size: $imageSize bytes" -ForegroundColor Yellow

Compress-PNG -inputPath $outputImagePath -outputPath $outputImagePath
$imageSize = Get-Item $outputImagePath | Select-Object -ExpandProperty Length
Write-Host "Compressed PNG size: $imageSize bytes" -ForegroundColor Yellow

Write-Host "Icon generated at $outputImagePath" -ForegroundColor Green