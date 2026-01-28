[CmdletBinding(SupportsShouldProcess = $true)]
[Diagnostics.CodeAnalysis.SuppressMessageAttribute(
	'PSAvoidUsingPlainTextForPassword',
	'',
	Justification = 'This script does not accept passwords as strings. It prompts via Get-Credential/Read-Host -AsSecureString and stores credentials DPAPI-encrypted via Export-Clixml.'
)]
param(
	# Hyper-V VM name. Used to pick a stable cache filename.
	[Parameter(Mandatory = $true)]
	[string]$VMName,

	# Optional username to prefill (you can still change it in the prompt).
	[Parameter(Mandatory = $false)]
	[string]$UserName,

	# Overwrite an existing cached credential.
	[Parameter(Mandatory = $false)]
	[switch]$Force,

	# Delete the cached credential instead of setting it.
	[Parameter(Mandatory = $false)]
	[switch]$Clear
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-DirectoryIfMissing {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Get-DefaultCredentialPath {
	param([Parameter(Mandatory = $true)][string]$VMName)
	$root = if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) { $env:LOCALAPPDATA } else { $env:TEMP }
	$dir = Join-Path $root 'FieldWorks\HyperV\Secrets'
	New-DirectoryIfMissing -Path $dir
	return (Join-Path $dir ("{0}.guestCredential.clixml" -f $VMName))
}

$cachePath = Get-DefaultCredentialPath -VMName $VMName

if ($Clear) {
	if (Test-Path -LiteralPath $cachePath -PathType Leaf) {
		if ($PSCmdlet.ShouldProcess($cachePath, 'Remove cached guest credential')) {
			Remove-Item -LiteralPath $cachePath -Force -ErrorAction Stop
			Write-Output "Removed cached guest credential: $cachePath"
		}
	} else {
		Write-Output "No cached guest credential found at: $cachePath"
	}
	return
}

if ((Test-Path -LiteralPath $cachePath -PathType Leaf) -and -not $Force) {
	throw "Credential cache already exists. Re-run with -Force to overwrite, or use -Clear to remove it first. Path: $cachePath"
}

$prompt = "Enter local admin credentials for VM '$VMName' (PowerShell Direct)"
$cred = $null

if (-not [string]::IsNullOrWhiteSpace($UserName)) {
	$secure = Read-Host -Prompt "Password for '$UserName'" -AsSecureString
	$cred = New-Object System.Management.Automation.PSCredential($UserName, $secure)
} else {
	$cred = Get-Credential -Message $prompt
}

if ($null -eq $cred) {
	throw 'Credential prompt was cancelled.'
}

New-DirectoryIfMissing -Path (Split-Path -Parent $cachePath)

if ($PSCmdlet.ShouldProcess($cachePath, 'Save cached guest credential (DPAPI encrypted, CurrentUser)')) {
	$cred | Export-Clixml -LiteralPath $cachePath -Force -ErrorAction Stop
}

if (!(Test-Path -LiteralPath $cachePath -PathType Leaf)) {
	throw "Failed to write credential cache: $cachePath"
}

Write-Output "Saved cached guest credential: $cachePath"
