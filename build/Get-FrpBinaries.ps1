[CmdletBinding()]
param(
    [string]$Version = '',
    [string]$CacheDirectory = '',
    [switch]$ResolveOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

if ([string]::IsNullOrWhiteSpace($CacheDirectory))
{
    $CacheDirectory = Join-Path (Split-Path $PSScriptRoot -Parent) 'src/obj/frp'
}

if ($PSVersionTable.PSVersion.Major -lt 6)
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
}

$architectures = @(
    @{ Abi = 'arm64-v8a'; Platform = 'android_arm64'; ElfClass = 2; ElfMachine = 183 },
    @{ Abi = 'armeabi-v7a'; Platform = 'linux_arm'; ElfClass = 1; ElfMachine = 40 }
)

function Invoke-FrpRequest
{
    param(
        [string]$Uri,
        [string]$OutFile = ''
    )

    for ($attempt = 1; $attempt -le 3; $attempt++)
    {
        try
        {
            $headers = @{ 'User-Agent' = 'FrpAndroid-build' }

            if ([string]::IsNullOrEmpty($OutFile))
            {
                $headers['Accept'] = 'application/vnd.github+json'
                $headers['X-GitHub-Api-Version'] = '2022-11-28'
                $token = $env:GITHUB_TOKEN

                if ([string]::IsNullOrWhiteSpace($token))
                {
                    $token = $env:GH_TOKEN
                }

                if (-not [string]::IsNullOrWhiteSpace($token))
                {
                    $headers['Authorization'] = "Bearer $token"
                }

                return Invoke-RestMethod -Uri $Uri -Headers $headers -TimeoutSec 60
            }

            Invoke-WebRequest -Uri $Uri -Headers $headers -OutFile $OutFile -UseBasicParsing -TimeoutSec 180 | Out-Null
            return
        }
        catch
        {
            if ($attempt -eq 3)
            {
                throw
            }

            Start-Sleep -Seconds ([Math]::Pow(2, $attempt))
        }
    }
}

function Get-FrpRelease
{
    param([string]$RequestedVersion)

    $releaseUri = 'https://api.github.com/repos/fatedier/frp/releases/latest'

    if (-not [string]::IsNullOrWhiteSpace($RequestedVersion))
    {
        if ($RequestedVersion -notmatch '^v?(\d+\.\d+\.\d+)$')
        {
            throw 'FrpVersion must be a stable version such as 0.71.1 or v0.71.1.'
        }

        $releaseUri = "https://api.github.com/repos/fatedier/frp/releases/tags/v$($Matches[1])"
    }

    $release = Invoke-FrpRequest -Uri $releaseUri

    if ($release.draft -or $release.prerelease -or $release.tag_name -notmatch '^v(\d+\.\d+\.\d+)$')
    {
        throw 'The selected frp release is not a stable release.'
    }

    $resolvedVersion = $Matches[1]

    foreach ($architecture in $architectures)
    {
        $assetName = "frp_${resolvedVersion}_$($architecture.Platform).tar.gz"
        $assets = @($release.assets | Where-Object { $_.name -eq $assetName })

        if ($assets.Count -ne 1)
        {
            throw "The frp release does not contain exactly one $assetName asset."
        }
    }

    $checksums = @($release.assets | Where-Object { $_.name -eq 'frp_sha256_checksums.txt' })

    if ($checksums.Count -ne 1)
    {
        throw 'The frp release is missing frp_sha256_checksums.txt.'
    }

    return @{ Version = $resolvedVersion; Release = $release }
}

function Test-FrpCache
{
    param(
        [string]$Directory,
        [string]$ExpectedVersion
    )

    try
    {
        $manifestPath = Join-Path $Directory 'manifest.json'

        if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf))
        {
            return $false
        }

        $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json

        if ($manifest.schema -ne 1 -or $manifest.version -ne $ExpectedVersion)
        {
            return $false
        }

        foreach ($architecture in $architectures)
        {
            foreach ($program in @('frpc', 'frps'))
            {
                $relativePath = "$($architecture.Abi)/lib$program.so"
                $filePath = Join-Path $Directory $relativePath
                $expectedHash = $manifest.files.PSObject.Properties[$relativePath]

                if ($null -eq $expectedHash -or $expectedHash.Value -notmatch '^[a-fA-F0-9]{64}$' -or -not (Test-Path -LiteralPath $filePath -PathType Leaf))
                {
                    return $false
                }

                if ((Get-FileHash -LiteralPath $filePath -Algorithm SHA256).Hash -ne $expectedHash.Value)
                {
                    return $false
                }
            }
        }

        return $true
    }
    catch
    {
        return $false
    }
}

function Assert-FrpElf
{
    param(
        [string]$Path,
        [hashtable]$Architecture
    )

    $stream = [IO.File]::OpenRead($Path)

    try
    {
        $header = New-Object byte[] 20
        $bytesRead = $stream.Read($header, 0, $header.Length)

        if ($bytesRead -ne 20 -or $header[0] -ne 127 -or $header[1] -ne 69 -or $header[2] -ne 76 -or $header[3] -ne 70 -or $header[4] -ne $Architecture.ElfClass -or $header[5] -ne 1 -or [BitConverter]::ToUInt16($header, 18) -ne $Architecture.ElfMachine)
        {
            throw "Invalid ELF architecture for $Path."
        }
    }
    finally
    {
        $stream.Dispose()
    }
}

try
{
    $resolved = Get-FrpRelease -RequestedVersion $Version
    $resolvedVersion = $resolved.Version
    $release = $resolved.Release

    if ($ResolveOnly)
    {
        [ordered]@{
            version = $resolvedVersion
            tag = $release.tag_name
            url = $release.html_url
        } | ConvertTo-Json -Compress
        exit 0
    }

    Get-Command tar -ErrorAction Stop | Out-Null
    $cacheRoot = [IO.Path]::GetFullPath($CacheDirectory)
    [IO.Directory]::CreateDirectory($cacheRoot) | Out-Null
    $nativeDirectory = Join-Path $cacheRoot $resolvedVersion
    $lockPath = Join-Path $cacheRoot 'download.lock'
    $lock = $null
    $stagingDirectory = $null
    $lockDeadline = [DateTime]::UtcNow.AddMinutes(15)

    try
    {
        while ($null -eq $lock)
        {
            try
            {
                $lock = [IO.File]::Open($lockPath, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            }
            catch [IO.IOException]
            {
                if ([DateTime]::UtcNow -ge $lockDeadline)
                {
                    throw 'Timed out waiting for another frp download to finish.'
                }

                Start-Sleep -Seconds 1
            }
        }

        if (Test-FrpCache -Directory $nativeDirectory -ExpectedVersion $resolvedVersion)
        {
            Write-Output $nativeDirectory
            exit 0
        }

        $stagingDirectory = Join-Path $cacheRoot ".download-$resolvedVersion-$([Guid]::NewGuid().ToString('N'))"
        [IO.Directory]::CreateDirectory($stagingDirectory) | Out-Null
        $checksumsPath = Join-Path $stagingDirectory 'frp_sha256_checksums.txt'
        $checksumsAsset = $release.assets | Where-Object { $_.name -eq 'frp_sha256_checksums.txt' }
        Invoke-FrpRequest -Uri $checksumsAsset.browser_download_url -OutFile $checksumsPath
        $checksums = @{}

        foreach ($line in (Get-Content -LiteralPath $checksumsPath))
        {
            if ($line -match '^(?<Hash>[a-fA-F0-9]{64})\s+\*?(?<Name>\S+)\s*$')
            {
                if ($checksums.ContainsKey($Matches.Name))
                {
                    throw "Duplicate checksum for $($Matches.Name)."
                }

                $checksums[$Matches.Name] = $Matches.Hash.ToLowerInvariant()
            }
        }

        $stagedNativeDirectory = Join-Path $stagingDirectory 'native'
        [IO.Directory]::CreateDirectory($stagedNativeDirectory) | Out-Null
        $fileHashes = [ordered]@{}
        $archiveHashes = [ordered]@{}

        foreach ($architecture in $architectures)
        {
            $archiveName = "frp_${resolvedVersion}_$($architecture.Platform).tar.gz"

            if (-not $checksums.ContainsKey($archiveName))
            {
                throw "No SHA-256 checksum found for $archiveName."
            }

            $archivePath = Join-Path $stagingDirectory $archiveName
            $asset = $release.assets | Where-Object { $_.name -eq $archiveName }
            Invoke-FrpRequest -Uri $asset.browser_download_url -OutFile $archivePath
            $archiveHash = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()

            if ($archiveHash -ne $checksums[$archiveName])
            {
                throw "SHA-256 verification failed for $archiveName."
            }

            $archiveHashes[$archiveName] = $archiveHash
            $archiveRoot = "frp_${resolvedVersion}_$($architecture.Platform)"
            $extractDirectory = Join-Path $stagingDirectory $architecture.Platform
            [IO.Directory]::CreateDirectory($extractDirectory) | Out-Null
            & tar -xzf $archivePath -C $extractDirectory "$archiveRoot/frpc" "$archiveRoot/frps"

            if ($LASTEXITCODE -ne 0)
            {
                throw "Failed to extract $archiveName (exit $LASTEXITCODE)."
            }

            $abiDirectory = Join-Path $stagedNativeDirectory $architecture.Abi
            [IO.Directory]::CreateDirectory($abiDirectory) | Out-Null

            foreach ($program in @('frpc', 'frps'))
            {
                $sourcePath = Join-Path $extractDirectory "$archiveRoot/$program"
                Assert-FrpElf -Path $sourcePath -Architecture $architecture
                $relativePath = "$($architecture.Abi)/lib$program.so"
                $destinationPath = Join-Path $stagedNativeDirectory $relativePath
                Move-Item -LiteralPath $sourcePath -Destination $destinationPath
                $fileHashes[$relativePath] = (Get-FileHash -LiteralPath $destinationPath -Algorithm SHA256).Hash.ToLowerInvariant()
            }
        }

        [ordered]@{
            schema = 1
            version = $resolvedVersion
            upstreamReleaseUrl = $release.html_url
            archives = $archiveHashes
            files = $fileHashes
        } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $stagedNativeDirectory 'manifest.json') -Encoding UTF8

        if (Test-Path -LiteralPath $nativeDirectory)
        {
            Remove-Item -LiteralPath $nativeDirectory -Recurse -Force
        }

        Move-Item -LiteralPath $stagedNativeDirectory -Destination $nativeDirectory
        Write-Output $nativeDirectory
    }
    finally
    {
        if ($null -ne $stagingDirectory -and (Test-Path -LiteralPath $stagingDirectory))
        {
            Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
        }

        if ($null -ne $lock)
        {
            $lock.Dispose()
        }
    }
}
catch
{
    Write-Error -ErrorRecord $_ -ErrorAction Continue
    exit 1
}

exit 0