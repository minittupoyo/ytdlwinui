[CmdletBinding()]
param(
    [string]$Destination = (Join-Path $env:LOCALAPPDATA 'ytdlgui\bin'),
    [ValidateSet('yt-dlp', 'deno', 'ffmpeg')]
    [string[]]$Tools = @('yt-dlp', 'deno', 'ffmpeg'),
    [ValidateSet('x64', 'arm64')]
    [string]$Architecture
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

function Get-NativeArchitecture {
    $reported = if ($env:PROCESSOR_ARCHITEW6432) { $env:PROCESSOR_ARCHITEW6432 } else { $env:PROCESSOR_ARCHITECTURE }
    switch ($reported.ToUpperInvariant()) {
        'AMD64' { return 'x64' }
        'ARM64' { return 'arm64' }
        default { throw "Unsupported architecture: $reported. Only x64 and ARM64 are supported." }
    }
}

function Get-PublishedHash {
    param([string]$ChecksumPath, [string]$AssetName)

    $document = Get-Content -LiteralPath $ChecksumPath -Raw
    foreach ($line in $document -split "`r?`n") {
        if ($line -match '^\s*([0-9a-fA-F]{64})\s+\*?(.+?)\s*$' -and
            [IO.Path]::GetFileName($Matches[2]) -ieq $AssetName) {
            return $Matches[1]
        }
    }

    $hashMatch = [regex]::Match($document, '(?im)^\s*Hash\s*:\s*([0-9a-fA-F]{64})\s*$')
    $pathMatch = [regex]::Match($document, '(?im)^\s*Path\s*:\s*(.+?)\s*$')
    if ($hashMatch.Success -and $pathMatch.Success -and
        [IO.Path]::GetFileName($pathMatch.Groups[1].Value) -ieq $AssetName) {
        return $hashMatch.Groups[1].Value
    }

    throw "SHA-256 checksum for $AssetName was not found."
}

function Save-VerifiedDownload {
    param(
        [string]$Name, [uri]$Source, [uri]$Checksums, [string]$AssetName,
        [string]$OutputPath, [string]$WorkingDirectory
    )

    $checksumPath = Join-Path $WorkingDirectory ("checksum-" + [guid]::NewGuid().ToString('N') + '.txt')
    Write-Host "[$Name] Downloading checksum..."
    Invoke-WebRequest -UseBasicParsing -Uri $Checksums -OutFile $checksumPath
    $expectedHash = Get-PublishedHash -ChecksumPath $checksumPath -AssetName $AssetName
    Write-Host "[$Name] Downloading $AssetName..."
    Invoke-WebRequest -UseBasicParsing -Uri $Source -OutFile $OutputPath
    Write-Host "[$Name] Verifying SHA-256..."
    $stream = [IO.File]::OpenRead($OutputPath)
    try {
        $sha256 = [Security.Cryptography.SHA256]::Create()
        try {
            $actualHash = [BitConverter]::ToString($sha256.ComputeHash($stream)).Replace('-', '')
        }
        finally {
            $sha256.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }
    if ($actualHash -ine $expectedHash) {
        throw "SHA-256 mismatch for $AssetName. The file was not installed."
    }
}

function Install-FileAtomically {
    param([string]$Source, [string]$Target)
    $pending = "$Target.new"
    try {
        Copy-Item -LiteralPath $Source -Destination $pending -Force
        Move-Item -LiteralPath $pending -Destination $Target -Force
    }
    finally {
        Remove-Item -LiteralPath $pending -Force -ErrorAction SilentlyContinue
    }
}

function Find-ArchiveFile {
    param([string]$Directory, [string]$FileName)
    $matches = @(Get-ChildItem -LiteralPath $Directory -Filter $FileName -File -Recurse)
    if ($matches.Count -ne 1) {
        throw "Expected exactly one $FileName in the downloaded archive; found $($matches.Count)."
    }
    return $matches[0].FullName
}

if (-not $Architecture) { $Architecture = Get-NativeArchitecture }
$workingDirectory = Join-Path ([IO.Path]::GetTempPath()) ("ytdlgui-tools-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $workingDirectory, $Destination | Out-Null

try {
    if ($Tools -contains 'yt-dlp') {
        $asset = if ($Architecture -eq 'arm64') { 'yt-dlp_arm64.exe' } else { 'yt-dlp.exe' }
        $download = Join-Path $workingDirectory $asset
        Save-VerifiedDownload -Name 'yt-dlp' -Source "https://github.com/yt-dlp/yt-dlp/releases/latest/download/$asset" `
            -Checksums 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/SHA2-256SUMS' `
            -AssetName $asset -OutputPath $download -WorkingDirectory $workingDirectory
        Install-FileAtomically -Source $download -Target (Join-Path $Destination 'yt-dlp.exe')
    }

    if ($Tools -contains 'deno') {
        $target = if ($Architecture -eq 'arm64') { 'aarch64' } else { 'x86_64' }
        $asset = "deno-$target-pc-windows-msvc.zip"
        $source = "https://github.com/denoland/deno/releases/latest/download/$asset"
        $download = Join-Path $workingDirectory $asset
        Save-VerifiedDownload -Name 'Deno' -Source $source -Checksums "$source.sha256sum" `
            -AssetName $asset -OutputPath $download -WorkingDirectory $workingDirectory
        $extractDirectory = Join-Path $workingDirectory 'deno'
        Expand-Archive -LiteralPath $download -DestinationPath $extractDirectory
        Install-FileAtomically -Source (Find-ArchiveFile $extractDirectory 'deno.exe') -Target (Join-Path $Destination 'deno.exe')
    }

    if ($Tools -contains 'ffmpeg') {
        $platform = if ($Architecture -eq 'arm64') { 'winarm64' } else { 'win64' }
        $asset = "ffmpeg-master-latest-$platform-gpl.zip"
        $source = "https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/$asset"
        $download = Join-Path $workingDirectory $asset
        Save-VerifiedDownload -Name 'FFmpeg' -Source $source `
            -Checksums 'https://github.com/yt-dlp/FFmpeg-Builds/releases/latest/download/checksums.sha256' `
            -AssetName $asset -OutputPath $download -WorkingDirectory $workingDirectory
        $extractDirectory = Join-Path $workingDirectory 'ffmpeg'
        Expand-Archive -LiteralPath $download -DestinationPath $extractDirectory
        Install-FileAtomically -Source (Find-ArchiveFile $extractDirectory 'ffmpeg.exe') -Target (Join-Path $Destination 'ffmpeg.exe')
        Install-FileAtomically -Source (Find-ArchiveFile $extractDirectory 'ffprobe.exe') -Target (Join-Path $Destination 'ffprobe.exe')
    }

    Write-Host "Installed tools to: $Destination"
}
finally {
    Remove-Item -LiteralPath $workingDirectory -Recurse -Force -ErrorAction SilentlyContinue
}
