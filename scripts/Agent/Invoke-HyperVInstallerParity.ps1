[CmdletBinding()]
param(
	[Parameter(Mandatory = $false)]
	[string]$ConfigPath,

	[Parameter(Mandatory = $false)]
	[string]$VMName,

	[Parameter(Mandatory = $false)]
	[string]$CheckpointName,

	[Parameter(Mandatory = $false)]
	[string]$GuestUserName,

	[Parameter(Mandatory = $false)]
	[string]$GuestPasswordEnvVar,

	[Parameter(Mandatory = $false)]
	[pscredential]$GuestCredential,

	[Parameter(Mandatory = $false)]
	[string]$GuestWorkDir = 'C:\\FWInstallerTest',

	[Parameter(Mandatory = $false)]
	[ValidateSet('Auto', 'Bundle', 'Msi')]
	[string]$InstallerType = 'Auto',

	[Parameter(Mandatory = $false)]
	[string[]]$InstallArguments = @(),

	[Parameter(Mandatory = $false)]
	[int]$MaxFileCount = 20000,

	# How long to wait for PowerShell Direct to come up after restoring the checkpoint and starting the VM.
	[Parameter(Mandatory = $false)]
	[int]$PowerShellDirectTimeoutSeconds = 900,

	[Parameter(Mandatory = $false)]
	[string]$Wix3InstallerPath,

	[Parameter(Mandatory = $false)]
	[string]$Wix6InstallerPath,

	[Parameter(Mandatory = $false)]
	[string]$HostEvidenceRoot,

	[Parameter(Mandatory = $false)]
	[switch]$PublishToSpecEvidence,

	[Parameter(Mandatory = $false)]
	[switch]$GenerateFixPlan,

	[Parameter(Mandatory = $false)]
	[switch]$FailOnDifferences,

	# If the baseline installer path does not exist, the script can auto-download a known WiX3 baseline installer.
	# This is intended for deterministic parity runs without requiring a separate worktree.
	[Parameter(Mandatory = $false)]
	[bool]$AutoDownloadWix3Baseline = $true,

	# Optional page URL that contains a link to the baseline installer EXE.
	[Parameter(Mandatory = $false)]
	[string]$Wix3BaselineDownloadPageUrl,

	# Optional direct URL to the baseline installer EXE.
	[Parameter(Mandatory = $false)]
	[string]$Wix3BaselineDownloadUrl,

	# Root directory for caching downloaded installers (defaults to %ProgramData%\FieldWorks\HyperV\Installers).
	[Parameter(Mandatory = $false)]
	[string]$InstallerCacheRoot,

	# Force re-download of the baseline installer even if it already exists in cache.
	[Parameter(Mandatory = $false)]
	[switch]$ForceDownloadWix3Baseline,

	# If set, the script will fail (instead of prompting) when guest credentials cannot be built from the configured env var.
	# Useful for CI/non-interactive runs.
	[Parameter(Mandatory = $false)]
	[switch]$RequireGuestPasswordEnvVar,

	# If set (default), the runner will cache the guest credential locally using DPAPI (CurrentUser)
	# so you only need to enter it once on a given machine/user profile.
	[Parameter(Mandatory = $false)]
	[bool]$RememberGuestCredential = $true,

	# Optional override for where to store the cached credential (DPAPI encrypted). Defaults under %LOCALAPPDATA%.
	[Parameter(Mandatory = $false)]
	[string]$GuestCredentialCachePath,

	[Parameter(Mandatory = $false)]
	[switch]$ValidateOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
	$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
	return $repoRoot.Path
}

function Assert-NotEmpty {
	param([string]$Value, [string]$Name)
	if ([string]::IsNullOrWhiteSpace($Value)) {
		throw "Missing required value: $Name"
	}
}

function Read-Config {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) { throw "Config not found: $Path" }
	(Get-Content -LiteralPath $Path -Raw -Encoding UTF8) | ConvertFrom-Json -ErrorAction Stop
}

function New-DirectoryIfMissing {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Get-DefaultInstallerCacheRoot {
	# Prefer a cross-worktree cache location.
	if (-not [string]::IsNullOrWhiteSpace($env:ProgramData)) {
		return (Join-Path $env:ProgramData 'FieldWorks\HyperV\Installers')
	}
	return (Join-Path $env:LOCALAPPDATA 'FieldWorks\HyperV\Installers')
}

function Get-FileNameFromUrl {
	param([Parameter(Mandatory = $true)][string]$Url)
	$uri = [System.Uri]::new($Url)
	return [System.IO.Path]::GetFileName($uri.AbsolutePath)
}

function Download-FileToCache {
	param(
		[Parameter(Mandatory = $true)][string]$Url,
		[Parameter(Mandatory = $true)][string]$DestinationPath,
		[Parameter(Mandatory = $false)][switch]$Force
	)

	if ((Test-Path -LiteralPath $DestinationPath -PathType Leaf) -and -not $Force) {
		return $DestinationPath
	}

	New-DirectoryIfMissing -Path (Split-Path -Parent $DestinationPath)

	Write-Verbose "Downloading baseline installer to cache: $DestinationPath"
	$downloaded = $false
	try {
		Start-BitsTransfer -Source $Url -Destination $DestinationPath -ErrorAction Stop
		$downloaded = $true
	} catch {
		Write-Verbose "BITS download failed, falling back to Invoke-WebRequest. Error: $_"
		Invoke-WebRequest -Uri $Url -OutFile $DestinationPath -UseBasicParsing -ErrorAction Stop
		$downloaded = $true
	}

	if (-not $downloaded -or !(Test-Path -LiteralPath $DestinationPath -PathType Leaf)) {
		throw "Failed to download baseline installer from '$Url'"
	}

	return $DestinationPath
}

function Resolve-Wix3BaselineDownloadUrlFromPage {
	param([Parameter(Mandatory = $true)][string]$PageUrl)

	Write-Verbose "Resolving baseline installer EXE URL from page: $PageUrl"
	$resp = Invoke-WebRequest -Uri $PageUrl -UseBasicParsing -ErrorAction Stop
	$html = [string]$resp.Content

	# Prefer the x64 Online installer link for 9.2.11, which is known to be WiX3-based.
	# Example: https://downloads.languagetechnology.org/fieldworks/9.2.11/1154/FieldWorks_9.2.11.1_Online_x64.exe
	$pattern = '(?i)https?://downloads\.languagetechnology\.org/fieldworks/9\.2\.11/[^\"\s>]+FieldWorks_9\.2\.11\.[^\"\s>]*_Online_x64\.exe'
	$m = [regex]::Match($html, $pattern)
	if ($m.Success) {
		return $m.Value
	}

	# Fallback: first matching EXE on the download host.
	$pattern2 = '(?i)https?://downloads\.languagetechnology\.org/fieldworks/9\.2\.11/[^\"\s>]+\.exe'
	$m2 = [regex]::Match($html, $pattern2)
	if ($m2.Success) {
		return $m2.Value
	}

	throw "Could not find a FieldWorks 9.2.11 Windows installer EXE link on page: $PageUrl"
}

function Ensure-Wix3BaselineInstallerPath {
	param(
		[Parameter(Mandatory = $true)][string]$CurrentInstallerPath,
		[Parameter(Mandatory = $false)][string]$DownloadPageUrl,
		[Parameter(Mandatory = $false)][string]$DownloadUrl,
		[Parameter(Mandatory = $true)][string]$CacheRoot,
		[Parameter(Mandatory = $false)][switch]$Force
	)

	if (Test-Path -LiteralPath $CurrentInstallerPath -PathType Leaf) {
		return (Resolve-Path -LiteralPath $CurrentInstallerPath).Path
	}

	if ([string]::IsNullOrWhiteSpace($DownloadUrl)) {
		if ([string]::IsNullOrWhiteSpace($DownloadPageUrl)) {
			return $CurrentInstallerPath
		}
		$DownloadUrl = Resolve-Wix3BaselineDownloadUrlFromPage -PageUrl $DownloadPageUrl
	}

	$leaf = Get-FileNameFromUrl -Url $DownloadUrl
	$dest = Join-Path (Join-Path $CacheRoot 'Wix3Baseline') $leaf
	$downloadedPath = Download-FileToCache -Url $DownloadUrl -DestinationPath $dest -Force:$Force
	return (Resolve-Path -LiteralPath $downloadedPath).Path
}

function Try-GetStringFromObject {
	param(
		[Parameter(Mandatory = $true)]$Object,
		[Parameter(Mandatory = $true)][string]$PropertyName
	)

	if ($null -eq $Object) { return $null }
	try {
		$val = $Object.$PropertyName
		if ($null -eq $val) { return $null }
		return [string]$val
	} catch {
		return $null
	}
}

function New-CredentialFromEnvVar {
	param(
		[Parameter(Mandatory = $true)][string]$UserName,
		[Parameter(Mandatory = $true)][string]$PasswordEnvVar
	)

	if ([string]::IsNullOrWhiteSpace($UserName)) { throw "Missing required value: GuestUserName" }
	if ([string]::IsNullOrWhiteSpace($PasswordEnvVar)) { throw "Missing required value: GuestPasswordEnvVar" }

	$plain = [Environment]::GetEnvironmentVariable($PasswordEnvVar)
	if ([string]::IsNullOrWhiteSpace($plain)) {
		throw "Environment variable '$PasswordEnvVar' is not set or is empty."
	}

	$secure = ConvertTo-SecureString -String $plain -AsPlainText -Force
	return New-Object System.Management.Automation.PSCredential($UserName, $secure)
}

function Get-DefaultGuestCredentialCachePath {
	param([Parameter(Mandatory = $true)][string]$VMName)
	$root = if (-not [string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) { $env:LOCALAPPDATA } else { $env:TEMP }
	$dir = Join-Path $root 'FieldWorks\HyperV\Secrets'
	New-DirectoryIfMissing -Path $dir
	# Avoid embedding the username/password env var name in the filename; just key off VM.
	return (Join-Path $dir ("{0}.guestCredential.clixml" -f $VMName))
}

function Try-LoadCachedCredential {
	param([Parameter(Mandatory = $true)][string]$Path)
	if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
	if (!(Test-Path -LiteralPath $Path -PathType Leaf)) { return $null }
	try {
		$cred = Import-Clixml -LiteralPath $Path -ErrorAction Stop
		if ($cred -is [pscredential]) {
			return $cred
		}
		return $null
	} catch {
		Write-Warning "Failed to load cached guest credential from '$Path': $_"
		return $null
	}
}

function Try-SaveCachedCredential {
	param(
		[Parameter(Mandatory = $true)][pscredential]$Credential,
		[Parameter(Mandatory = $true)][string]$Path
	)
	try {
		New-DirectoryIfMissing -Path (Split-Path -Parent $Path)
		$Credential | Export-Clixml -LiteralPath $Path -Force -ErrorAction Stop
		return $true
	} catch {
		Write-Warning "Failed to cache guest credential to '$Path': $_"
		return $false
	}
}

$repoRoot = Resolve-RepoRoot

if (-not [string]::IsNullOrWhiteSpace($ConfigPath)) {
	$config = Read-Config -Path $ConfigPath

	if ([string]::IsNullOrWhiteSpace($VMName)) { $VMName = [string]$config.vmName }
	if ([string]::IsNullOrWhiteSpace($CheckpointName)) { $CheckpointName = [string]$config.checkpointName }
	if ([string]::IsNullOrWhiteSpace($GuestWorkDir)) { $GuestWorkDir = [string]$config.guestWorkDir }
	if ($InstallerType -eq 'Auto' -and $null -ne $config.installerType) { $InstallerType = [string]$config.installerType }
	if (($null -eq $InstallArguments -or $InstallArguments.Count -eq 0) -and $null -ne $config.installArguments) { $InstallArguments = @($config.installArguments | ForEach-Object { [string]$_ }) }
	if ($MaxFileCount -eq 20000 -and $null -ne $config.maxFileCount) { $MaxFileCount = [int]$config.maxFileCount }
	$timeoutFromConfig = Try-GetStringFromObject -Object $config -PropertyName 'powerShellDirectTimeoutSeconds'
	if ($PowerShellDirectTimeoutSeconds -eq 900 -and -not [string]::IsNullOrWhiteSpace($timeoutFromConfig)) {
		$PowerShellDirectTimeoutSeconds = [int]$timeoutFromConfig
	}

	if ([string]::IsNullOrWhiteSpace($Wix3InstallerPath) -and $null -ne $config.baseline.installerPath) { $Wix3InstallerPath = [string]$config.baseline.installerPath }
	if ([string]::IsNullOrWhiteSpace($Wix6InstallerPath) -and $null -ne $config.candidate.installerPath) { $Wix6InstallerPath = [string]$config.candidate.installerPath }

	if ([string]::IsNullOrWhiteSpace($Wix3BaselineDownloadPageUrl)) {
		$Wix3BaselineDownloadPageUrl = Try-GetStringFromObject -Object $config.baseline -PropertyName 'downloadPageUrl'
	}
	if ([string]::IsNullOrWhiteSpace($Wix3BaselineDownloadUrl)) {
		$Wix3BaselineDownloadUrl = Try-GetStringFromObject -Object $config.baseline -PropertyName 'downloadUrl'
	}
	if ([string]::IsNullOrWhiteSpace($InstallerCacheRoot)) {
		$InstallerCacheRoot = Try-GetStringFromObject -Object $config.baseline -PropertyName 'cacheRoot'
	}

	if ([string]::IsNullOrWhiteSpace($HostEvidenceRoot) -and $null -ne $config.output.hostEvidenceRoot) { $HostEvidenceRoot = [string]$config.output.hostEvidenceRoot }
	if (-not $PublishToSpecEvidence -and $null -ne $config.output.publishToSpecEvidence) { $PublishToSpecEvidence = [bool]$config.output.publishToSpecEvidence }

	if ([string]::IsNullOrWhiteSpace($GuestUserName)) {
		$GuestUserName = Try-GetStringFromObject -Object $config.guestCredential -PropertyName 'username'
	}
	if ([string]::IsNullOrWhiteSpace($GuestPasswordEnvVar)) {
		$GuestPasswordEnvVar = Try-GetStringFromObject -Object $config.guestCredential -PropertyName 'passwordEnvVar'
	}
}

# Default baseline download page (WiX3): FieldWorks 9.2.11
if ([string]::IsNullOrWhiteSpace($Wix3BaselineDownloadPageUrl)) {
	$Wix3BaselineDownloadPageUrl = 'https://software.sil.org/fieldworks/download/fw-92/fw-9211/'
}

if ([string]::IsNullOrWhiteSpace($InstallerCacheRoot)) {
	$InstallerCacheRoot = Get-DefaultInstallerCacheRoot
}

if ([string]::IsNullOrWhiteSpace($GuestCredentialCachePath)) {
	$GuestCredentialCachePath = Get-DefaultGuestCredentialCachePath -VMName $VMName
}

Assert-NotEmpty -Value $VMName -Name 'VMName'
Assert-NotEmpty -Value $CheckpointName -Name 'CheckpointName'
Assert-NotEmpty -Value $Wix3InstallerPath -Name 'Wix3InstallerPath'
Assert-NotEmpty -Value $Wix6InstallerPath -Name 'Wix6InstallerPath'


# If the baseline installer is missing, optionally download it from the well-known release page.
# To keep -ValidateOnly lightweight, downloading is skipped unless -ForceDownloadWix3Baseline is specified.
if ($AutoDownloadWix3Baseline -and !(Test-Path -LiteralPath $Wix3InstallerPath -PathType Leaf) -and (-not $ValidateOnly -or $ForceDownloadWix3Baseline)) {
	$Wix3InstallerPath = Ensure-Wix3BaselineInstallerPath -CurrentInstallerPath $Wix3InstallerPath -DownloadPageUrl $Wix3BaselineDownloadPageUrl -DownloadUrl $Wix3BaselineDownloadUrl -CacheRoot $InstallerCacheRoot -Force:$ForceDownloadWix3Baseline
}

if ($ValidateOnly) {
	try {
		Import-Module Hyper-V -ErrorAction Stop
		$null = Get-VM -Name $VMName -ErrorAction Stop
		$null = Get-VMSnapshot -VMName $VMName -Name $CheckpointName -ErrorAction Stop
	} catch {
		throw "Hyper-V validation failed for VM '$VMName' / checkpoint '$CheckpointName': $_"
	}

	if (!(Test-Path -LiteralPath $Wix3InstallerPath -PathType Leaf)) { throw "Baseline installer not found: $Wix3InstallerPath" }
	if (!(Test-Path -LiteralPath $Wix6InstallerPath -PathType Leaf)) { throw "Candidate installer not found: $Wix6InstallerPath" }

	Write-Output "Validation OK. VM='$VMName', Checkpoint='$CheckpointName'"
	Write-Output "Baseline: $Wix3InstallerPath"
	Write-Output "Candidate: $Wix6InstallerPath"
	return [pscustomobject]@{
		VMName = $VMName
		CheckpointName = $CheckpointName
		Wix3InstallerPath = $Wix3InstallerPath
		Wix6InstallerPath = $Wix6InstallerPath
	}
}

if ($null -eq $GuestCredential) {
	# First try a locally cached credential (DPAPI encrypted, CurrentUser scope).
	if ($RememberGuestCredential) {
		$cached = Try-LoadCachedCredential -Path $GuestCredentialCachePath
		if ($null -ne $cached) {
			Write-Verbose "Loaded cached guest credential from: $GuestCredentialCachePath"
			$GuestCredential = $cached
		}
	}

	if (-not [string]::IsNullOrWhiteSpace($GuestUserName) -and -not [string]::IsNullOrWhiteSpace($GuestPasswordEnvVar)) {
		try {
			$GuestCredential = New-CredentialFromEnvVar -UserName $GuestUserName -PasswordEnvVar $GuestPasswordEnvVar
		} catch {
			if ($RequireGuestPasswordEnvVar) {
				throw
			}
			Write-Warning "Guest password env var '$GuestPasswordEnvVar' is missing/empty; falling back to interactive credential prompt."
			$GuestCredential = Get-Credential -Message "Enter local admin credentials for VM '$VMName' (PowerShell Direct)"
		}
	} else {
		$GuestCredential = Get-Credential -Message "Enter local admin credentials for VM '$VMName' (PowerShell Direct)"
	}

	# Cache for next time (best-effort).
	if ($RememberGuestCredential -and $null -ne $GuestCredential) {
		$null = Try-SaveCachedCredential -Credential $GuestCredential -Path $GuestCredentialCachePath
	}
}

$runStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$wix3RunId = "wix3-${runStamp}"
$wix6RunId = "wix6-${runStamp}"

$runScript = Join-Path $repoRoot 'scripts\\Agent\\Invoke-HyperVInstallerRun.ps1'
$wix3RunArgs = @{
	VMName = $VMName
	CheckpointName = $CheckpointName
	GuestCredential = $GuestCredential
	InstallerPath = $Wix3InstallerPath
	Mode = 'Wix3'
	InstallerType = $InstallerType
	RunId = $wix3RunId
	GuestWorkDir = $GuestWorkDir
	HostEvidenceRoot = $HostEvidenceRoot
	InstallArguments = $InstallArguments
	MaxFileCount = $MaxFileCount
	PowerShellDirectTimeoutSeconds = $PowerShellDirectTimeoutSeconds
}
$wix3Run = & $runScript @wix3RunArgs

$wix6RunArgs = @{
	VMName = $VMName
	CheckpointName = $CheckpointName
	GuestCredential = $GuestCredential
	InstallerPath = $Wix6InstallerPath
	Mode = 'Wix6'
	InstallerType = $InstallerType
	RunId = $wix6RunId
	GuestWorkDir = $GuestWorkDir
	HostEvidenceRoot = $HostEvidenceRoot
	InstallArguments = $InstallArguments
	MaxFileCount = $MaxFileCount
	PowerShellDirectTimeoutSeconds = $PowerShellDirectTimeoutSeconds
}
$wix6Run = & $runScript @wix6RunArgs

if ($PublishToSpecEvidence) {
	& (Join-Path $repoRoot 'scripts\\Agent\\Publish-HyperVEvidenceToSpec.ps1') -Mode 'Wix3' -RunId $wix3RunId
	& (Join-Path $repoRoot 'scripts\\Agent\\Publish-HyperVEvidenceToSpec.ps1') -Mode 'Wix6' -RunId $wix6RunId

	$specEvidenceRoot = Join-Path $repoRoot 'specs\\001-wix-v6-migration\\evidence'
	$wix3EvidenceDir = Join-Path (Join-Path $specEvidenceRoot 'Wix3') $wix3RunId
	$wix6EvidenceDir = Join-Path (Join-Path $specEvidenceRoot 'Wix6') $wix6RunId
} else {
	$wix3EvidenceDir = $wix3Run.HostEvidenceDir
	$wix6EvidenceDir = $wix6Run.HostEvidenceDir
}

$compareScript = Join-Path $repoRoot 'scripts\\Agent\\Compare-InstallerEvidenceRuns.ps1'
$reportPath = & $compareScript -Wix3EvidenceDir $wix3EvidenceDir -Wix6EvidenceDir $wix6EvidenceDir -FailOnDifferences:$FailOnDifferences

if ($GenerateFixPlan) {
	$planPath = & (Join-Path $repoRoot 'scripts\\Agent\\New-Wix6ParityFixPlan.ps1') -DiffReportPath $reportPath
	Write-Output "Generated fix plan: $planPath"
}

Write-Output "WiX3 RunId: $wix3RunId"
Write-Output "WiX6 RunId: $wix6RunId"
Write-Output "Diff report: $reportPath"

return [pscustomobject]@{
	Wix3RunId = $wix3RunId
	Wix6RunId = $wix6RunId
	DiffReportPath = $reportPath
}
