[CmdletBinding()]
param(
    [ValidateRange(1, [int]::MaxValue)]
    [int]$BuildNumber
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$stableTags = @(git tag --list 'v*' --sort=-v:refname) |
    Where-Object { $_ -match '^v\d+\.\d+\.\d+$' }

if ($LASTEXITCODE -ne 0) {
    throw "Unable to read Git tags. Git exited with code $LASTEXITCODE."
}

$latestTag = $stableTags | Select-Object -First 1
$baseVersion = if ($null -eq $latestTag) { [version]'0.0.0' } else { [version]$latestTag.Substring(1) }
$commitRange = if ($null -eq $latestTag) { 'HEAD' } else { "$latestTag..HEAD" }
$commitMessages = @(git log $commitRange --format='%s%n%b%n---KEEP-ALIVE-COMMIT---') -join "`n"

if ($LASTEXITCODE -ne 0) {
    throw "Unable to read Git history. Git exited with code $LASTEXITCODE."
}

$bump = if ($commitMessages -match '(?m)^[A-Za-z][A-Za-z0-9-]*(\([^)]+\))?!:' -or
    $commitMessages -match '(?m)^BREAKING[ -]CHANGE:\s') {
    'major'
}
elseif ($commitMessages -match '(?m)^feat(\([^)]+\))?:\s') {
    'minor'
}
elseif ($commitMessages -match '(?m)^(fix|perf)(\([^)]+\))?:\s') {
    'patch'
}
else {
    'none'
}

$releaseVersion = switch ($bump) {
    'major' { [version]::new($baseVersion.Major + 1, 0, 0) }
    'minor' { [version]::new($baseVersion.Major, $baseVersion.Minor + 1, 0) }
    'patch' { [version]::new($baseVersion.Major, $baseVersion.Minor, $baseVersion.Build + 1) }
    default { $baseVersion }
}

$result = [pscustomobject]@{
    BaseVersion = $baseVersion.ToString(3)
    BuildVersion = "$($releaseVersion.ToString(3))-ci.$BuildNumber"
    FileVersion = "$($releaseVersion.ToString(3)).$BuildNumber"
    ReleaseVersion = $releaseVersion.ToString(3)
    Bump = $bump
    ShouldRelease = $bump -ne 'none'
}

$result | ConvertTo-Json -Compress
