[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $repositoryRoot 'artifacts\releases'
$stagingRoot = Join-Path $artifactsRoot ('.staging-' + [Guid]::NewGuid().ToString('N'))
$publishRoot = Join-Path $stagingRoot 'KeepAlive'
$archivePath = Join-Path $artifactsRoot "KeepAlive-$Version-win-x64.zip"
$checksumPath = "$archivePath.sha256"
$projectPath = Join-Path $repositoryRoot 'src\KeepAlive\KeepAlive.csproj'
$versionMatch = [regex]::Match($Version, '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(-ci\.(?<build>\d+))?')
$buildNumber = if ($versionMatch.Groups['build'].Success) { $versionMatch.Groups['build'].Value } else { '0' }
$fileVersion = '{0}.{1}.{2}.{3}' -f `
    $versionMatch.Groups['major'].Value, `
    $versionMatch.Groups['minor'].Value, `
    $versionMatch.Groups['patch'].Value, `
    $buildNumber

New-Item -ItemType Directory -Force -Path $artifactsRoot | Out-Null

try {
    dotnet restore $projectPath --runtime win-x64

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed with exit code $LASTEXITCODE."
    }

    dotnet publish $projectPath `
        --configuration Release `
        --runtime win-x64 `
        --self-contained true `
        --no-restore `
        --output $publishRoot `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:Version=$Version `
        -p:FileVersion=$fileVersion

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE."
    }

    Compress-Archive -Path (Join-Path $publishRoot '*') -DestinationPath $archivePath -CompressionLevel Optimal -Force
    $checksum = (Get-FileHash -Path $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    "$checksum  $(Split-Path -Leaf $archivePath)" | Set-Content -Path $checksumPath -Encoding ascii

    Write-Output $archivePath
    Write-Output $checksumPath
}
finally {
    $resolvedArtifactsRoot = [IO.Path]::GetFullPath($artifactsRoot).TrimEnd([IO.Path]::DirectorySeparatorChar)
    $resolvedStagingRoot = [IO.Path]::GetFullPath($stagingRoot)

    if ($resolvedStagingRoot.StartsWith($resolvedArtifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedStagingRoot)) {
        Remove-Item -LiteralPath $resolvedStagingRoot -Recurse -Force
    }
}
