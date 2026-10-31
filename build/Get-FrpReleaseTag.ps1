[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [AllowEmptyCollection()]
    [object[]]$Releases,
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$FrpVersion,
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+(\.\d+){1,3}$')]
    [string]$FallbackVersion
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$baseVersion = $FallbackVersion
$previousVersion = $null
$coveredReleaseTag = ''

foreach ($release in $Releases)
{
    if ($release.draft -or $release.prerelease)
    {
        continue
    }

    $version = [regex]::Match($release.tag_name, '^v(?<AppVersion>\d+(\.\d+){1,3})(?:-frp(?<FrpVersion>\d+\.\d+\.\d+))?$')

    if (-not $version.Success)
    {
        continue
    }

    $applicationVersion = $version.Groups['AppVersion'].Value

    if ($null -eq $previousVersion -or [version]$applicationVersion -gt $previousVersion)
    {
        $previousVersion = [version]$applicationVersion
        $baseVersion = $applicationVersion
    }

    $bundledVersion = $version.Groups['FrpVersion'].Value

    if (-not [string]::IsNullOrEmpty($bundledVersion) -and [version]$bundledVersion -ge [version]$FrpVersion)
    {
        $coveredReleaseTag = $release.tag_name
    }
}

[pscustomobject]@{
    Tag = "v$baseVersion-frp$FrpVersion"
    BaseVersion = $baseVersion
    ShouldBuild = [string]::IsNullOrEmpty($coveredReleaseTag)
    CoveredReleaseTag = $coveredReleaseTag
}