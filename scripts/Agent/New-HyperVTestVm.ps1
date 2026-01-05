[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$VMName,

	# ISO file path. Optional if -IsoUrl is provided.
	[Parameter(Mandatory = $false)]
	[string]$IsoPath,

	# Direct ISO download URL. Used only when the VM does not already exist and -IsoPath is missing.
	# NOTE: Microsoft ISO links are often time-limited; expect to refresh this URL occasionally.
	[Parameter(Mandatory = $false)]
	[string]$IsoUrl,

	# Use Fido to obtain a Microsoft ISO URL (Fido is GPLv3; not vendored here).
	# This uses scripts/Agent/Get-WindowsIso.ps1 -AutoDownloadFido and is intended for local/dev use only.
	[Parameter(Mandatory = $false)]
	[switch]$UseFido,

	# Explicit acknowledgement required when using Fido (GPLv3).
	[Parameter(Mandatory = $false)]
	[switch]$AllowGplFido,

	[Parameter(Mandatory = $false)]
	[ValidateSet('Windows 11', 'Windows 10')]
	[string]$FidoWin = 'Windows 11',

	[Parameter(Mandatory = $false)]
	[string]$FidoRel = 'Latest',

	[Parameter(Mandatory = $false)]
	[string]$FidoEd = 'Windows 11 Home/Pro/Edu',

	[Parameter(Mandatory = $false)]
	[string]$FidoLang = 'English',

	[Parameter(Mandatory = $false)]
	[ValidateSet('x64', 'ARM64', 'x86')]
	[string]$FidoArch = 'x64',

	# Optional cache root for ISO downloads (defaults to %ProgramData%\FieldWorks\HyperV\ISOs).
	[Parameter(Mandatory = $false)]
	[string]$IsoCacheRoot,

	[Parameter(Mandatory = $false)]
	[string]$SwitchName,

	[Parameter(Mandatory = $false)]
	[string]$VmRoot,

	[Parameter(Mandatory = $false)]
	[Alias('VhdSizeGB')]
	[int]$VhdSizeGiB = 80,

	[Parameter(Mandatory = $false)]
	[ValidateRange(1, 64)]
	[int]$ProcessorCount = 2,

	[Parameter(Mandatory = $false)]
	# MemoryStartupBytes in bytes.
	[ValidateRange(536870912, 68719476736)]
	[long]$MemoryStartupBytes = 4294967296,

	[Parameter(Mandatory = $false)]
	[switch]$StartVm,

	[Parameter(Mandatory = $false)]
	[switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
	$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\\..')
	return $repoRoot.Path
}

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

function Get-IsoFromUrlIfNeeded {
	param(
		[Parameter(Mandatory = $true)][string]$IsoUrl,
		[Parameter(Mandatory = $false)][string]$IsoCacheRoot
	)

	$scriptPath = Join-Path $PSScriptRoot 'Get-WindowsIso.ps1'
	if (!(Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
		throw "Missing helper script: $scriptPath"
	}

	$isoArgs = @{
		IsoUrl = $IsoUrl
	}
	if (-not [string]::IsNullOrWhiteSpace($IsoCacheRoot)) {
		$isoArgs.CacheRoot = $IsoCacheRoot
	}

	$downloadedIsoPath = & $scriptPath @isoArgs
	if ([string]::IsNullOrWhiteSpace($downloadedIsoPath)) {
		throw 'ISO download helper returned an empty path.'
	}
	return [string]$downloadedIsoPath
}

function Get-IsoFromFidoIfNeeded {
	param(
		[Parameter(Mandatory = $false)][string]$IsoCacheRoot,
		[Parameter(Mandatory = $true)][switch]$AllowGplFido,
		[Parameter(Mandatory = $true)][string]$Win,
		[Parameter(Mandatory = $true)][string]$Rel,
		[Parameter(Mandatory = $true)][string]$Ed,
		[Parameter(Mandatory = $true)][string]$Lang,
		[Parameter(Mandatory = $true)][string]$Arch
	)

	# If we've already downloaded a matching ISO, prefer reusing it without invoking Fido again.
	# (Fido/Microsoft endpoints can be flaky; cache reuse makes this lane far more reliable.)
	$effectiveCacheRoot = $IsoCacheRoot
	if ([string]::IsNullOrWhiteSpace($effectiveCacheRoot)) {
		if (-not [string]::IsNullOrWhiteSpace($env:ProgramData)) {
			$effectiveCacheRoot = (Join-Path $env:ProgramData 'FieldWorks\HyperV\ISOs')
		} else {
			$effectiveCacheRoot = (Join-Path $env:LOCALAPPDATA 'FieldWorks\HyperV\ISOs')
		}
	}

	$winToken = 'Win11'
	if ($Win -eq 'Windows 10') {
		$winToken = 'Win10'
	}
	$langPattern = '*'
	if (-not [string]::IsNullOrWhiteSpace($Lang)) {
		$langParts = $Lang.Trim().Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
		if ($langParts.Count -gt 0) {
			$langPattern = ($langParts -join '*')
		}
	}

	try {
		if (Test-Path -LiteralPath $effectiveCacheRoot -PathType Container) {
			$existingIso = Get-ChildItem -LiteralPath $effectiveCacheRoot -Filter "${winToken}_*_${langPattern}_${Arch}.iso" -ErrorAction SilentlyContinue |
				Sort-Object -Property LastWriteTime -Descending |
				Select-Object -First 1
			if ($null -ne $existingIso) {
				Write-Output "Using cached ISO (skipping Fido): $($existingIso.FullName)"
				return [string]$existingIso.FullName
			}
		}
	} catch {
		# Ignore cache lookup failures and fall back to normal flow.
	}

	$scriptPath = Join-Path $PSScriptRoot 'Get-WindowsIso.ps1'
	if (!(Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
		throw "Missing helper script: $scriptPath"
	}

	$isoArgs = @{
		AutoDownloadFido = $true
		AllowGplFido = $AllowGplFido
		Win = $Win
		Rel = $Rel
		Ed = $Ed
		Lang = $Lang
		Arch = $Arch
	}
	if (-not [string]::IsNullOrWhiteSpace($IsoCacheRoot)) {
		$isoArgs.CacheRoot = $IsoCacheRoot
	}

	$downloadedIsoPath = & $scriptPath @isoArgs
	if ([string]::IsNullOrWhiteSpace($downloadedIsoPath)) {
		throw 'ISO download helper returned an empty path.'
	}
	return [string]$downloadedIsoPath
}

try {
	Import-Module Hyper-V -ErrorAction Stop
} catch {
	throw "Hyper-V PowerShell module is required. Enable Hyper-V and ensure the Hyper-V module is installed. Error: $_"
}

$existing = $null
try {
	$existing = Get-VM -Name $VMName -ErrorAction SilentlyContinue
} catch {
	$existing = $null
}

if ($null -ne $existing) {
	if (-not $Force) {
		Write-Output "VM already exists: $VMName"
		return $existing
	}

	Write-Output "Removing existing VM (Force): $VMName"
	if ($existing.State -ne 'Off') {
		Stop-VM -Name $VMName -Force -TurnOff -ErrorAction Stop
	}
	Remove-VM -Name $VMName -Force -ErrorAction Stop
}

# VM does not exist. Resolve ISO path now.
if (-not [string]::IsNullOrWhiteSpace($IsoPath)) {
	Assert-FileExists -Path $IsoPath
	$IsoPath = (Resolve-Path -LiteralPath $IsoPath).Path
} elseif (-not [string]::IsNullOrWhiteSpace($IsoUrl)) {
	Write-Output "ISO not provided; downloading from IsoUrl into cache"
	$IsoPath = Get-IsoFromUrlIfNeeded -IsoUrl $IsoUrl -IsoCacheRoot $IsoCacheRoot
	Assert-FileExists -Path $IsoPath
	$IsoPath = (Resolve-Path -LiteralPath $IsoPath).Path

} elseif ($UseFido) {
	if (-not $AllowGplFido) {
		throw 'Using -UseFido requires -AllowGplFido (Fido is GPLv3; not vendored into this repo).'
	}
	Write-Output "ISO not provided; using Fido to obtain an ISO URL and download into cache"
	$IsoPath = Get-IsoFromFidoIfNeeded -IsoCacheRoot $IsoCacheRoot -AllowGplFido:$AllowGplFido -Win $FidoWin -Rel $FidoRel -Ed $FidoEd -Lang $FidoLang -Arch $FidoArch
	Assert-FileExists -Path $IsoPath
	$IsoPath = (Resolve-Path -LiteralPath $IsoPath).Path
} else {
	throw "No ISO provided. Specify -IsoPath, specify -IsoUrl to download an ISO into the cache, or specify -UseFido -AllowGplFido to obtain a URL via Fido and download the ISO."
}

$repoRoot = Resolve-RepoRoot
if ([string]::IsNullOrWhiteSpace($VmRoot)) {
	$VmRoot = Join-Path $repoRoot 'Output\\HyperV\\VMs'
}
New-DirectoryIfMissing -Path $VmRoot

$vmDir = Join-Path $VmRoot $VMName
New-DirectoryIfMissing -Path $vmDir

$vhdPath = Join-Path $vmDir ("{0}.vhdx" -f $VMName)

Write-Output ("Creating VHDX: {0} (SizeGiB={1})" -f $vhdPath, $VhdSizeGiB)
$null = New-VHD -Path $vhdPath -SizeBytes ([int64]$VhdSizeGiB * 1073741824) -Dynamic -ErrorAction Stop

$vmParams = @{
	Name = $VMName
	Generation = 2
	VHDPath = $vhdPath
	MemoryStartupBytes = $MemoryStartupBytes
}
if (-not [string]::IsNullOrWhiteSpace($SwitchName)) {
	$vmParams.SwitchName = $SwitchName
}

Write-Output "Creating VM: $VMName"
$vm = New-VM @vmParams -ErrorAction Stop

Write-Output "Setting CPU count: $ProcessorCount"
Set-VMProcessor -VMName $VMName -Count $ProcessorCount -ErrorAction Stop

Write-Output "Configuring firmware secure boot template"
Set-VMFirmware -VMName $VMName -EnableSecureBoot On -SecureBootTemplate 'MicrosoftWindows' -ErrorAction Stop

Write-Output "Attaching ISO: $IsoPath"
$null = Add-VMDvdDrive -VMName $VMName -Path $IsoPath -ErrorAction Stop

$dvd = Get-VMDvdDrive -VMName $VMName -ErrorAction Stop
Write-Output "Setting boot device to DVD"
Set-VMFirmware -VMName $VMName -FirstBootDevice $dvd -ErrorAction Stop

Write-Output "VM created. Next: install Windows in the VM, create a local admin account, then create a clean checkpoint."
Write-Output "Tip: use Hyper-V Manager -> Connect to finish OOBE."

if ($StartVm) {
	Write-Output "Starting VM: $VMName"
	$null = Start-VM -Name $VMName -ErrorAction Stop
}

return $vm
