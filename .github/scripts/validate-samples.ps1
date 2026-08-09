#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds and runs every sample project under samples/ with the todl CLI,
    then verifies each one's stdout against its stdout.expected.txt.

.DESCRIPTION
    A "sample" is auto-detected as any immediate subdirectory of samples/
    that contains a todl.json manifest - no project list is hardcoded here.
    The expected assembly name is read from each manifest's "name" field
    rather than assumed from the directory name (they can differ, e.g.
    samples/Fibonacci.Loop -> "fibonacci.loop").
#>
[CmdletBinding()]
param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")),
    [string]$Configuration = "Debug",
    [string]$TodlPath
)

$ErrorActionPreference = "Stop"

if (-not $TodlPath) {
    $TodlPath = Join-Path $RepoRoot "out/$Configuration/src/Todl/todl.dll"
}

if (-not (Test-Path $TodlPath)) {
    throw "todl CLI not found at '$TodlPath'. Build src/Todl/Todl.csproj first."
}

$samplesRoot = Join-Path $RepoRoot "samples"
$sampleDirs = Get-ChildItem -Path $samplesRoot -Directory |
    Where-Object { Test-Path (Join-Path $_.FullName "todl.json") }

if (-not $sampleDirs -or $sampleDirs.Count -eq 0) {
    throw "No sample projects (directories containing todl.json) found under '$samplesRoot'."
}

$failed = $false

foreach ($sample in $sampleDirs) {
    $sampleName = $sample.Name
    $manifest = Get-Content (Join-Path $sample.FullName "todl.json") -Raw | ConvertFrom-Json
    $outDir = Join-Path $sample.FullName "out"

    Write-Host "::group::todl build $sampleName"
    dotnet $TodlPath build $sample.FullName --output $outDir
    if ($LASTEXITCODE -ne 0) {
        Write-Host "::error::todl build failed for $sampleName"
        $failed = $true
        Write-Host "::endgroup::"
        continue
    }

    $assembly = Join-Path $outDir "$($manifest.name).dll"
    if (-not (Test-Path $assembly)) {
        Write-Host "::error::$assembly was not produced"
        $failed = $true
        Write-Host "::endgroup::"
        continue
    }

    $actual = ((dotnet $assembly) -join "`n").TrimEnd()
    if ($LASTEXITCODE -ne 0) {
        Write-Host "::error::running $assembly failed"
        $failed = $true
        Write-Host "::endgroup::"
        continue
    }

    $expectedPath = Join-Path $sample.FullName "stdout.expected.txt"
    if (-not (Test-Path $expectedPath)) {
        Write-Host "::error::$sampleName has no stdout.expected.txt"
        $failed = $true
        Write-Host "::endgroup::"
        continue
    }

    $expected = ((Get-Content $expectedPath) -join "`n").TrimEnd()
    if ($actual -ne $expected) {
        Write-Host "::error::$sampleName stdout did not match stdout.expected.txt"
        Write-Host "expected: [$expected]"
        Write-Host "actual:   [$actual]"
        $failed = $true
    } else {
        Write-Host "$sampleName OK"
    }
    Write-Host "::endgroup::"
}

if ($failed) {
    exit 1
}

Write-Host "All $($sampleDirs.Count) sample(s) passed."
