[CmdletBinding()]
param(
	# Direct download URL to a Windows ISO.
	[Parameter(Mandatory = $false, ParameterSetName = 'Url')]
	[string]$IsoUrl,

	# Path to an external copy of Fido.ps1 (NOT vendored into this repo).
	# If omitted, you can set -AutoDownloadFido (and acknowledge GPL via -AllowGplFido).
	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[string]$FidoPath,

	# Auto-download Fido.ps1 to a local cache folder (NOT committed).
	# This is gated behind -AllowGplFido to force an explicit acknowledgement.
	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[switch]$AutoDownloadFido,

	# Source URL used when -AutoDownloadFido is specified.
	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[string]$FidoDownloadUrl = 'https://raw.githubusercontent.com/pbatard/Fido/master/Fido.ps1',

	# Required acknowledgement when using Fido (GPLv3). This repo does not vendor Fido.
	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[switch]$AllowGplFido,

	# Parameters passed to Fido when -FidoPath is used.
	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[ValidateSet('Windows 11', 'Windows 10')]
	[string]$Win = 'Windows 11',

	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[string]$Rel = 'Latest',

	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[string]$Ed = 'Windows 11 Home/Pro/Edu',

	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[string]$Lang = 'English',

	[Parameter(Mandatory = $false, ParameterSetName = 'Fido')]
	[ValidateSet('x64', 'ARM64', 'x86')]
	[string]$Arch = 'x64',

	# Root directory for caching downloaded ISOs.
	[Parameter(Mandatory = $false)]
	[string]$CacheRoot,

	[Parameter(Mandatory = $false)]
	[switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-DirectoryIfMissing {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Assert-FileExists {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
		throw "File not found: $Path"
	}
}

function Get-DefaultCacheRoot {
	# Prefer a cross-worktree cache location.
	if (-not [string]::IsNullOrWhiteSpace($env:ProgramData)) {
		return (Join-Path $env:ProgramData 'FieldWorks\HyperV\ISOs')
	}
	return (Join-Path $env:LOCALAPPDATA 'FieldWorks\HyperV\ISOs')
}

function Get-DefaultToolsRoot {
	# Prefer a cross-worktree cache location.
	if (-not [string]::IsNullOrWhiteSpace($env:ProgramData)) {
		return (Join-Path $env:ProgramData 'FieldWorks\HyperV\Tools')
	}
	return (Join-Path $env:LOCALAPPDATA 'FieldWorks\HyperV\Tools')
}

function Get-FidoPathFromCache {
	param(
		[Parameter(Mandatory = $true)][string]$FidoDownloadUrl,
		[Parameter(Mandatory = $true)][string]$ToolsRoot,
		[Parameter(Mandatory = $true)][switch]$AllowGplFido,
		[Parameter(Mandatory = $false)][switch]$Force
	)

	if (-not $AllowGplFido) {
		throw "Using Fido requires -AllowGplFido (Fido is GPLv3; this repo does not vendor it)."
	}

	New-DirectoryIfMissing -Path $ToolsRoot
	$fidoDir = Join-Path $ToolsRoot 'Fido'
	New-DirectoryIfMissing -Path $fidoDir
	$fidoPath = Join-Path $fidoDir 'Fido.ps1'

	if ((Test-Path -LiteralPath $fidoPath -PathType Leaf) -and -not $Force) {
		return $fidoPath
	}

	Write-Verbose "Downloading Fido.ps1 to local cache: $fidoPath"
	try {
		Invoke-WebRequest -Uri $FidoDownloadUrl -OutFile $fidoPath -UseBasicParsing -ErrorAction Stop
	} catch {
		throw "Failed to download Fido.ps1 from '$FidoDownloadUrl': $_"
	}

	Assert-FileExists -Path $fidoPath
	return $fidoPath
}

function Get-IsoFileNameFromUrl {
	param([Parameter(Mandatory = $true)][string]$Url)
	try {
		$uri = [System.Uri]::new($Url)
		$fileName = [System.IO.Path]::GetFileName($uri.AbsolutePath)
		if ([string]::IsNullOrWhiteSpace($fileName)) {
			throw "Could not determine a file name from URL: $Url"
		}
		if (-not $fileName.EndsWith('.iso')) {
			throw "URL does not look like an ISO download (expected .iso): $Url"
		}
		return $fileName
	} catch {
		throw "Invalid URL '$Url': $_"
	}
}

function Get-IsoUrlFromFido {
	param(
		[Parameter(Mandatory = $true)][string]$FidoPath,
		[Parameter(Mandatory = $true)][string]$Win,
		[Parameter(Mandatory = $true)][string]$Rel,
		[Parameter(Mandatory = $true)][string]$Ed,
		[Parameter(Mandatory = $true)][string]$Lang,
		[Parameter(Mandatory = $true)][string]$Arch
	)

	Assert-FileExists -Path $FidoPath
	$resolved = (Resolve-Path -LiteralPath $FidoPath).Path

	# Fido's accepted language values are human-readable (e.g., 'English') rather than locales.
	# Map a few common locale-style inputs to keep our wrapper ergonomic.
	$normalizedLang = $Lang
	if (-not [string]::IsNullOrWhiteSpace($normalizedLang)) {
		switch -Regex ($normalizedLang.Trim()) {
			'^en(-[A-Za-z]{2})?$' { $normalizedLang = 'English'; break }
			'^en-us$' { $normalizedLang = 'English'; break }
			'^en-gb$' { $normalizedLang = 'English International'; break }
			'^fr(-[A-Za-z]{2})?$' { $normalizedLang = 'French'; break }
			'^fr-ca$' { $normalizedLang = 'French Canadian'; break }
			'^es(-[A-Za-z]{2})?$' { $normalizedLang = 'Spanish'; break }
			'^es-mx$' { $normalizedLang = 'Spanish (Mexico)'; break }
			default { }
		}
	}

	# NOTE: We intentionally do NOT vendor or modify Fido.ps1 in this repo.
	# Fido is GPLv3, so shipping a derived version would impose GPL obligations.

	# Prefer Windows PowerShell for best compatibility with Fido.
	# (Fido has historically been most reliable under Windows PowerShell rather than pwsh.)
	$exe = 'powershell.exe'

	Write-Verbose "Requesting ISO download URL via Fido: Win='$Win' Rel='$Rel' Ed='$Ed' Lang='$normalizedLang' Arch='$Arch'"
	# Run in a child process and force TLS 1.2+ first (some environments default to older TLS).
	$escapedPath = $resolved.Replace("'", "''")
	$escapedWin = $Win.Replace("'", "''")
	$escapedRel = $Rel.Replace("'", "''")
	$escapedEd = $Ed.Replace("'", "''")
	$escapedLang = $normalizedLang.Replace("'", "''")
	$escapedArch = $Arch.Replace("'", "''")

	$cmd = @"
& {
  [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
  & '$escapedPath' -Win '$escapedWin' -Rel '$escapedRel' -Ed '$escapedEd' -Lang '$escapedLang' -Arch '$escapedArch' -GetUrl
}
"@

	$raw = & $exe -NoProfile -ExecutionPolicy Bypass -Command $cmd

	if ($null -eq $raw) {
		throw 'Fido did not return a download URL.'
	}

	# Fido may emit multiple lines. Normalize to the last non-empty line.
	$rawLines = @()
	if ($raw -is [System.Array]) {
		$rawLines = @($raw)
	} else {
		$rawLines = @($raw)
	}
	$last = ($rawLines | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) } | Select-Object -Last 1)
	$url = [string]$last
	$url = $url.Trim()

	if ([string]::IsNullOrWhiteSpace($url)) {
		throw 'Fido did not return a download URL.'
	}
	if ($url -match '^Error\b' -or $url -match 'Exception') {
		throw "Fido returned an error instead of a URL: $url"
	}

	return $url
}

function Write-Metadata {
	param(
		[Parameter(Mandatory = $true)][string]$MetaPath,
		[Parameter(Mandatory = $true)][string]$IsoPath,
		[Parameter(Mandatory = $true)][string]$IsoUrl
	)
	$meta = [pscustomobject]@{
		isoPath = $IsoPath
		isoUrl = $IsoUrl
		retrievedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
	}
	$meta | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $MetaPath -Encoding UTF8
}

if ([string]::IsNullOrWhiteSpace($CacheRoot)) {
	$CacheRoot = Get-DefaultCacheRoot
}
New-DirectoryIfMissing -Path $CacheRoot

if ($PSCmdlet.ParameterSetName -eq 'Fido') {
	if ($AutoDownloadFido) {
		$toolsRoot = Get-DefaultToolsRoot
		$FidoPath = Get-FidoPathFromCache -FidoDownloadUrl $FidoDownloadUrl -ToolsRoot $toolsRoot -AllowGplFido:$AllowGplFido -Force:$Force
	}

	if ([string]::IsNullOrWhiteSpace($FidoPath)) {
		throw 'Specify -FidoPath, or specify -AutoDownloadFido to download Fido.ps1 to a local cache.'
	}
	if (-not $AllowGplFido) {
		throw "Using Fido requires -AllowGplFido (Fido is GPLv3; this repo does not vendor it)."
	}
	$IsoUrl = Get-IsoUrlFromFido -FidoPath $FidoPath -Win $Win -Rel $Rel -Ed $Ed -Lang $Lang -Arch $Arch
}

if ([string]::IsNullOrWhiteSpace($IsoUrl)) {
	throw 'Specify -IsoUrl, or specify -FidoPath to obtain a URL via Fido.'
}

$fileName = Get-IsoFileNameFromUrl -Url $IsoUrl
$isoPath = Join-Path $CacheRoot $fileName
$metaPath = "$isoPath.meta.json"

if ((Test-Path -LiteralPath $isoPath -PathType Leaf) -and -not $Force) {
	Write-Verbose "Using cached ISO: $isoPath"
	return $isoPath
}

Write-Verbose "Downloading ISO to cache: $isoPath"

# Prefer BITS (resumable). Fall back to Invoke-WebRequest if BITS isn't available.
$downloaded = $false
try {
	Start-BitsTransfer -Source $IsoUrl -Destination $isoPath -ErrorAction Stop
	$downloaded = $true
} catch {
	Write-Verbose "BITS download failed, falling back to Invoke-WebRequest. Error: $_"
	Invoke-WebRequest -Uri $IsoUrl -OutFile $isoPath -UseBasicParsing -ErrorAction Stop
	$downloaded = $true
}

if (-not $downloaded) {
	throw 'Failed to download ISO.'
}

Write-Metadata -MetaPath $metaPath -IsoPath $isoPath -IsoUrl $IsoUrl
Write-Verbose "Downloaded: $isoPath"
return $isoPath
