param(
	[Parameter(Mandatory = $true)]
	[string]$Wix3Installer,

	[Parameter(Mandatory = $true)]
	[string]$Wix6Installer,

	[Parameter(Mandatory = $true)]
	[string]$OutputRoot,

	[string[]]$InstallArgs = @('/quiet', '/norestart')
)

$ErrorActionPreference = 'Stop'

function Ensure-Directory([string]$Path)
{
	New-Item -ItemType Directory -Force -Path $Path | Out-Null
}

function Write-TextFile([string]$Path, [string]$Text)
{
	$dir = Split-Path -Parent $Path
	if ($dir) { Ensure-Directory $dir }
	$Text | Out-File -FilePath $Path -Encoding UTF8
}

function Export-UninstallSnapshot([string]$Path)
{
	$items = @()

	$items += Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*' -ErrorAction SilentlyContinue
	$items += Get-ItemProperty 'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*' -ErrorAction SilentlyContinue

	$items |
		Where-Object {
			$_.PSObject.Properties.Match('DisplayName').Count -gt 0 -and
			-not [string]::IsNullOrWhiteSpace($_.DisplayName)
		} |
		Select-Object DisplayName, DisplayVersion, Publisher, InstallDate, UninstallString |
		Sort-Object DisplayName |
		Format-Table -AutoSize |
		Out-String -Width 4096 |
		Out-File -FilePath $Path -Encoding UTF8
}

function Run-Installer([string]$InstallerPath, [string]$LogPath)
{
	if (-not (Test-Path -LiteralPath $InstallerPath))
	{
		throw "Installer not found: $InstallerPath"
	}

	$logDir = Split-Path -Parent $LogPath
	if ($logDir) { Ensure-Directory $logDir }

	$args = @()
	$args += $InstallArgs
	$args += '/log'
	$args += $LogPath

	$startInfo = @{
		FilePath = $InstallerPath
		ArgumentList = $args
		Wait = $true
		PassThru = $true
	}

	$proc = Start-Process @startInfo
	return $proc.ExitCode
}

$resolvedOutputRoot = (Resolve-Path -LiteralPath $OutputRoot -ErrorAction SilentlyContinue)
if (-not $resolvedOutputRoot)
{
	Ensure-Directory $OutputRoot
}

$timestamp = (Get-Date).ToString('yyyyMMdd_HHmmss')
$runRoot = Join-Path $OutputRoot $timestamp
Ensure-Directory $runRoot

Write-TextFile (Join-Path $runRoot 'started.txt') (Get-Date).ToString('o')
try
{
	(Get-ComputerInfo | Out-String -Width 4096) | Out-File -FilePath (Join-Path $runRoot 'computerinfo.txt') -Encoding UTF8
}
catch
{
	Write-TextFile (Join-Path $runRoot 'computerinfo-error.txt') $_.ToString()
}

Export-UninstallSnapshot (Join-Path $runRoot 'uninstall-pre.txt')

$wix3Log = Join-Path $runRoot 'wix3-burn.log'
$wix6Log = Join-Path $runRoot 'wix6-burn.log'

$wix3Exit = Run-Installer -InstallerPath $Wix3Installer -LogPath $wix3Log
Write-TextFile (Join-Path $runRoot 'wix3-exitcode.txt') $wix3Exit

$wix6Exit = Run-Installer -InstallerPath $Wix6Installer -LogPath $wix6Log
Write-TextFile (Join-Path $runRoot 'wix6-exitcode.txt') $wix6Exit

Export-UninstallSnapshot (Join-Path $runRoot 'uninstall-post.txt')

# Best-effort extra logs
try
{
	$extraDir = Join-Path $runRoot 'temp-logs'
	Ensure-Directory $extraDir
	Copy-Item -Path (Join-Path $env:TEMP '*.log') -Destination $extraDir -Force -ErrorAction SilentlyContinue
}
catch
{
	Write-TextFile (Join-Path $runRoot 'temp-logs-error.txt') $_.ToString()
}

$zipPath = Join-Path $OutputRoot ("evidence-$timestamp.zip")
Compress-Archive -Path $runRoot -DestinationPath $zipPath -Force
Write-Output $zipPath
