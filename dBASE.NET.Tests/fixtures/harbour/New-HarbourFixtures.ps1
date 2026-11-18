param(
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string]$Hbmk2 = 'hbmk2',
    [string]$Compiler,
    [string]$CompilerBin,
    [string]$CompilerLib,
    [ValidateRange(0, 1000000)][int]$BenchmarkRows = 0
)

$ErrorActionPreference = 'Stop'
$fixtureOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
if (Test-Path -LiteralPath $fixtureOutput) {
    throw "Expected a new output directory: $fixtureOutput"
}

$fixtureBuild = Join-Path ([IO.Path]::GetTempPath()) ('harbour-fixtures-' + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $fixtureBuild | Out-Null
$originalPath = $env:PATH
if ($CompilerBin) { $env:PATH = "$CompilerBin;$originalPath" }

function Invoke-Tool {
    param([string]$Tool, [string[]]$Arguments)
    & $Tool @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Tool exited with code $LASTEXITCODE" }
}

Push-Location $fixtureBuild
try {
    $compileOptions = @('-q', '-gtstd', '-w3', '-es2')
    if ($Compiler) { $compileOptions += "-comp=$Compiler" }
    if ($CompilerLib) { $compileOptions += "-L$CompilerLib" }
    Invoke-Tool $Hbmk2 (@((Join-Path $PSScriptRoot 'generate.prg'), '-ogenerate') + $compileOptions)
    if (!$BenchmarkRows) {
        Invoke-Tool $Hbmk2 (@((Join-Path $PSScriptRoot 'capture.prg'), '-ocapture') + $compileOptions)
    }
    New-Item -ItemType Directory -Path $fixtureOutput | Out-Null
    if ($BenchmarkRows) {
        Invoke-Tool (Join-Path $fixtureBuild 'generate.exe') @($fixtureOutput, 'benchmark', "$BenchmarkRows")
    }
    else {
        Invoke-Tool (Join-Path $fixtureBuild 'generate.exe') @($fixtureOutput)
        foreach ($table in Get-ChildItem -LiteralPath $fixtureOutput -Filter '*.dbf' -File) {
            Invoke-Tool (Join-Path $fixtureBuild 'capture.exe') @($table.FullName, [IO.Path]::ChangeExtension($table.FullName, '.json'))
        }
    }
}
finally {
    Pop-Location
    $env:PATH = $originalPath
    $resolvedBuild = (Resolve-Path -LiteralPath $fixtureBuild).Path
    if ($resolvedBuild -eq $fixtureBuild) { Remove-Item -LiteralPath $resolvedBuild -Recurse -Force }
}
