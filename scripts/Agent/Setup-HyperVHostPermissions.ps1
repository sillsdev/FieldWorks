[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '', Justification = 'Work around a persistent false-positive diagnostic referencing a removed function name.')]
[CmdletBinding()]
param(
	# User to ensure is in the required local groups. Defaults to the current Windows identity.
	[Parameter(Mandatory = $false)]
	[string]$UserName = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name,

	# Ensure the user is a member of the local 'Hyper-V Administrators' group.
	[Parameter(Mandatory = $false)]
	[bool]$EnsureHyperVAdministrators = $true,

	# If set, do not make changes; only report status.
	[Parameter(Mandatory = $false)]
	[switch]$ValidateOnly,

	# Internal: prevents infinite recursion when self-elevating.
	[Parameter(Mandatory = $false)]
	[switch]$NoElevate
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-IsAdministrator {
	$identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
	$principal = New-Object System.Security.Principal.WindowsPrincipal($identity)
	return $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Test-HyperVModuleAvailable {
	try {
		$null = Get-Module -ListAvailable -Name Hyper-V -ErrorAction Stop
		return $true
	} catch {
		return $false
	}
}

function Test-IsLocalGroupMember {
	param(
		[Parameter(Mandatory = $true)][string]$GroupName,
		[Parameter(Mandatory = $true)][string]$MemberName
	)
	try {
		$members = Get-LocalGroupMember -Group $GroupName -ErrorAction Stop
		foreach ($m in $members) {
			if ([string]::Equals([string]$m.Name, $MemberName, [StringComparison]::OrdinalIgnoreCase)) {
				return $true
			}
		}
		return $false
	} catch {
		# If the group doesn't exist or the query fails, treat as not a member.
		return $false
	}
}

$isAdmin = Test-IsAdministrator
$hasHyperVModule = Test-HyperVModuleAvailable

if (-not $isAdmin -and -not $ValidateOnly -and -not $NoElevate) {
	Write-Output "[INFO] Not running elevated; attempting to re-run as Administrator (UAC prompt expected)."

	$argList = @(
		'-NoProfile',
		'-ExecutionPolicy', 'Bypass',
		'-File', $PSCommandPath,
		'-NoElevate',
		'-UserName', $UserName,
		"-EnsureHyperVAdministrators:$EnsureHyperVAdministrators"
	)
	if ($ValidateOnly) { $argList += '-ValidateOnly' }

	$proc = Start-Process -FilePath 'powershell.exe' -Verb RunAs -ArgumentList $argList -Wait -PassThru
	exit $proc.ExitCode
}

if (-not $isAdmin -and -not $ValidateOnly) {
	Write-Error "ERROR: This script must run elevated to change local group membership. Re-run in an Administrator PowerShell (or allow the UAC prompt)."
	exit 1
}

Write-Output "UserName: $UserName"
Write-Output ("IsElevated: " + $isAdmin)
Write-Output ("HyperVModuleAvailable: " + $hasHyperVModule)

$changed = $false
if ($EnsureHyperVAdministrators) {
	$groupName = 'Hyper-V Administrators'
	if (Test-IsLocalGroupMember -GroupName $groupName -MemberName $UserName) {
		Write-Output "[OK] '$UserName' is already in '$groupName'."
	} elseif ($ValidateOnly) {
		Write-Output "[INFO] Would add '$UserName' to '$groupName' (ValidateOnly)."
	} else {
		Add-LocalGroupMember -Group $groupName -Member $UserName -ErrorAction Stop
		Write-Output "[OK] Added '$UserName' to '$groupName'."
		$changed = $true
	}
}

if ($changed) {
	Write-Output "[WARN] Group membership changes require log off/on (or reboot) to take effect in existing processes."
	Write-Output "[WARN] After logon, start VS Code normally for parity runs; start VS Code as Administrator only when you need host disk operations like Mount-VHD."
}

# Emit a structured summary at the end for scripting.
$inHvAdmins = Test-IsLocalGroupMember -GroupName 'Hyper-V Administrators' -MemberName $UserName
return [pscustomobject]@{
	UserName = $UserName
	IsElevated = $isAdmin
	HyperVModuleAvailable = $hasHyperVModule
	InHyperVAdministrators = $inHvAdmins
	ChangedGroupMembership = $changed
}
