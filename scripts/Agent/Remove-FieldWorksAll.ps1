[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
	[string]$NameLike = 'FieldWorks*',
	[string]$PublisherLike = '',
	[string[]]$ExcludeNameLike = @('FieldWorks Lite*'),
	[string]$LogRoot = "$PSScriptRoot\..\..\Output\InstallerEvidence",
	[switch]$IncludeHidden,
	[switch]$RemoveOrphanedArpEntries
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

trap {
	Write-Output ("ERROR: " + $_.Exception.Message)
	$info = $_.InvocationInfo
	if ($info)
	{
		Write-Output ("ERROR SCRIPT: " + $info.ScriptName)
		Write-Output ("ERROR LINE NUMBER: " + $info.ScriptLineNumber)
		Write-Output ("ERROR OFFSET IN LINE: " + $info.OffsetInLine)
		if ($info.Line)
		{
			Write-Output ("ERROR LINE: " + $info.Line)
		}
		if ($info.PositionMessage)
		{
			Write-Output ("ERROR POSITION: " + $info.PositionMessage)
		}
	}
	exit 1
}

function New-LogFolder {
	param(
		[string]$Root
	)

	$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
	$folder = Join-Path -Path $Root -ChildPath ("remove-fieldworks-" + $timestamp)
	$null = New-Item -ItemType Directory -Force -Path $folder
	return $folder
}

function Get-SafeObjectPropertyValue {
	param(
		[AllowNull()]
		[object]$InputObject,
		[Parameter(Mandatory = $true)]
		[string]$Name
	)

	if ($null -eq $InputObject)
	{
		return $null
	}

	$prop = $InputObject.PSObject.Properties[$Name]
	if ($null -eq $prop)
	{
		return $null
	}

	return $prop.Value
}

function Get-ArpEntries {
	param(
		[string]$DisplayNameLike,
		[string]$PublisherLike,
		[string[]]$ExcludeDisplayNameLike,
		[switch]$IncludeHidden
	)

	$roots = @(
		'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
		'HKCU:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
		'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
		'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall'
	)

	$results = New-Object System.Collections.Generic.List[object]

	foreach ($root in $roots)
	{
		if (-not (Test-Path -LiteralPath $root))
		{
			continue
		}

		foreach ($k in Get-ChildItem -LiteralPath $root)
		{
			$p = $null
			try { $p = Get-ItemProperty -LiteralPath $k.PSPath -ErrorAction Stop } catch { continue }
			if ($null -eq $p) { continue }

			$displayName = Get-SafeObjectPropertyValue -InputObject $p -Name 'DisplayName'
			if (-not $displayName) { continue }

			if ($ExcludeDisplayNameLike)
			{
				$excluded = $false
				foreach ($pattern in $ExcludeDisplayNameLike)
				{
					if ($pattern -and ($displayName -like $pattern))
					{
						$excluded = $true
						break
					}
				}
				if ($excluded) { continue }
			}

			if ($displayName -notlike $DisplayNameLike) { continue }

			$publisher = Get-SafeObjectPropertyValue -InputObject $p -Name 'Publisher'
			if ($PublisherLike)
			{
				# If a Publisher filter is provided, require it to match.
				if (-not $publisher) { continue }
				if ($publisher -notlike $PublisherLike) { continue }
			}

			$systemComponent = Get-SafeObjectPropertyValue -InputObject $p -Name 'SystemComponent'
			if (-not $IncludeHidden -and (($systemComponent -eq 1) -or ($systemComponent -eq '1'))) { continue }

			$displayVersion = Get-SafeObjectPropertyValue -InputObject $p -Name 'DisplayVersion'
			$windowsInstaller = Get-SafeObjectPropertyValue -InputObject $p -Name 'WindowsInstaller'
			$noRemove = Get-SafeObjectPropertyValue -InputObject $p -Name 'NoRemove'
			$uninstallString = Get-SafeObjectPropertyValue -InputObject $p -Name 'UninstallString'
			$quietUninstallString = Get-SafeObjectPropertyValue -InputObject $p -Name 'QuietUninstallString'
			$installDate = Get-SafeObjectPropertyValue -InputObject $p -Name 'InstallDate'
			$installSource = Get-SafeObjectPropertyValue -InputObject $p -Name 'InstallSource'
			$localPackage = Get-SafeObjectPropertyValue -InputObject $p -Name 'LocalPackage'

			$results.Add([pscustomobject]@{
				RegistryPath = $k.PSPath
				KeyName = $k.PSChildName
				DisplayName = $displayName
				DisplayVersion = $displayVersion
				Publisher = $publisher
				WindowsInstaller = $windowsInstaller
				NoRemove = $noRemove
				SystemComponent = $systemComponent
				UninstallString = $uninstallString
				QuietUninstallString = $quietUninstallString
				InstallDate = $installDate
				InstallSource = $installSource
				LocalPackage = $localPackage
			})
		}
	}

	# Return a plain object[] to avoid any edge-case enumeration behavior of the backing List object.
	return $results.ToArray()
}

function Test-IsAdministrator {
	try {
		$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
		$principal = New-Object Security.Principal.WindowsPrincipal($identity)
		return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
	} catch {
		return $false
	}
}

function Test-UninstallExitCodeSuccess {
	param([AllowNull()][int]$ExitCode)
	if ($null -eq $ExitCode) { return $true } # WhatIf paths return null
	# 0 = success, 3010/1641 = success but reboot required, 1605/1614 = not installed
	return ($ExitCode -eq 0 -or $ExitCode -eq 3010 -or $ExitCode -eq 1641 -or $ExitCode -eq 1605 -or $ExitCode -eq 1614)
}

function Test-GuidBraced {
	param([string]$Value)
	if (-not $Value) { return $false }
	try {
		$trim = $Value.Trim()
		if ($trim.StartsWith('{') -and $trim.EndsWith('}'))
		{
			$null = [Guid]::Parse($trim)
			return $true
		}
	} catch {
		return $false
	}
	return $false
}

function Invoke-MsiUninstall {
	param(
		[string]$ProductCode,
		[string]$LogFolder
	)

	$logPath = Join-Path -Path $LogFolder -ChildPath ("msi-uninstall-" + $ProductCode.Trim('{}') + ".log")
	$arguments = @(
		'/x', $ProductCode,
		'/qn',
		'/norestart',
		'/l*v', $logPath
	)

	$exe = Join-Path -Path $env:SystemRoot -ChildPath 'System32\msiexec.exe'

	if ($PSCmdlet.ShouldProcess("MSI $ProductCode", "Uninstall via msiexec"))
	{
		$proc = Start-Process -FilePath $exe -ArgumentList $arguments -Wait -PassThru
		return [pscustomobject]@{ ExitCode = $proc.ExitCode; LogPath = $logPath }
	}

	return [pscustomobject]@{ ExitCode = $null; LogPath = $logPath }
}

function Invoke-CommandLineUninstall {
	param(
		[string]$CommandLine,
		[string]$DisplayName
	)

	if (-not $CommandLine) { return $null }

	if ($PSCmdlet.ShouldProcess($DisplayName, "Run: $CommandLine"))
	{
		# Use cmd.exe so quoted exe paths and embedded arguments work reliably.
		$proc = Start-Process -FilePath (Join-Path $env:SystemRoot 'System32\cmd.exe') -ArgumentList @('/c', $CommandLine) -Wait -PassThru
		return [pscustomobject]@{ ExitCode = $proc.ExitCode }
	}

	return [pscustomobject]@{ ExitCode = $null }
}

$logFolder = New-LogFolder -Root $LogRoot
Write-Output "[INFO] Log folder: $logFolder"

$entries = Get-ArpEntries -DisplayNameLike $NameLike -PublisherLike $PublisherLike -ExcludeDisplayNameLike $ExcludeNameLike -IncludeHidden:$IncludeHidden
if (-not $entries -or $entries.Count -eq 0)
{
	$publisherMsg = if ($PublisherLike) { " (Publisher like '$PublisherLike')" } else { '' }
	Write-Output "[OK] No matching ARP entries found for DisplayName like '$NameLike'$publisherMsg."
	exit 0
}

Write-Output "[INFO] Found $($entries.Count) matching ARP entr$(if ($entries.Count -eq 1) { 'y' } else { 'ies' })."

$results = New-Object System.Collections.Generic.List[object]
$hadFailure = $false

if (-not (Test-IsAdministrator))
{
	if ($WhatIfPreference)
	{
		Write-Output "[WARN] Not running elevated. -WhatIf will still list candidates, but real uninstalls will require 'Run as Administrator'."
	}
	else
	{
		Write-Output "[ERROR] This script must be run from an elevated PowerShell session (Run as Administrator) to uninstall MSI-based FieldWorks products."
		Write-Output "[ERROR] Re-run the same command in an elevated console."
		exit 1
	}
}

foreach ($e in $entries)
{
	$displayNameProp = $e.PSObject.Properties['DisplayName']
	$keyNameProp = $e.PSObject.Properties['KeyName']
	$displayVersionProp = $e.PSObject.Properties['DisplayVersion']
	$windowsInstallerProp = $e.PSObject.Properties['WindowsInstaller']
	$quietUninstallStringProp = $e.PSObject.Properties['QuietUninstallString']
	$uninstallStringProp = $e.PSObject.Properties['UninstallString']
	$registryPathProp = $e.PSObject.Properties['RegistryPath']

	$displayName = if ($displayNameProp) { [string]$displayNameProp.Value } else { $null }
	$keyName = if ($keyNameProp) { [string]$keyNameProp.Value } else { $null }
	$displayVersion = if ($displayVersionProp) { [string]$displayVersionProp.Value } else { $null }
	$windowsInstaller = if ($windowsInstallerProp) { $windowsInstallerProp.Value } else { $null }
	$quietUninstallString = if ($quietUninstallStringProp) { [string]$quietUninstallStringProp.Value } else { $null }
	$uninstallString = if ($uninstallStringProp) { [string]$uninstallStringProp.Value } else { $null }
	$registryPath = if ($registryPathProp) { [string]$registryPathProp.Value } else { $null }

	if (-not $displayName)
	{
		$kind = if ($null -ne $e) { $e.GetType().FullName } else { '<null>' }
		Write-Output "[WARN] Skipping unexpected item without DisplayName (Type=$kind)."
		continue
	}

	Write-Output "[INFO] Candidate: $displayName $displayVersion (Key=$keyName)"

	$didAttempt = $false

	# Prefer MSI uninstall when the key is a product code.
	if ($windowsInstaller -eq 1 -and (Test-GuidBraced -Value $keyName))
	{
		$didAttempt = $true
		$r = Invoke-MsiUninstall -ProductCode $keyName -LogFolder $logFolder
		if (-not (Test-UninstallExitCodeSuccess -ExitCode $r.ExitCode)) { $hadFailure = $true }
		$results.Add([pscustomobject]@{
			DisplayName = $displayName
			KeyName = $keyName
			Method = 'msiexec'
			ExitCode = $r.ExitCode
			LogPath = $r.LogPath
			RegistryPath = $registryPath
		})

		# If MSI says product is unknown, offer to remove the orphaned ARP key.
		if ($RemoveOrphanedArpEntries -and ($r.ExitCode -eq 1605 -or $r.ExitCode -eq 1614) -and $registryPath)
		{
			if ($PSCmdlet.ShouldProcess($registryPath, 'Remove orphaned ARP registry key'))
			{
				Remove-Item -LiteralPath $registryPath -Recurse -Force
			}
		}

		continue
	}

	# Next, try quiet uninstall string (typically bundles / EXE uninstallers).
	if ($quietUninstallString)
	{
		$didAttempt = $true
		$r = Invoke-CommandLineUninstall -CommandLine $quietUninstallString -DisplayName $displayName
		if (-not (Test-UninstallExitCodeSuccess -ExitCode $r.ExitCode)) { $hadFailure = $true }
		$results.Add([pscustomobject]@{
			DisplayName = $displayName
			KeyName = $keyName
			Method = 'QuietUninstallString'
			ExitCode = $r.ExitCode
			LogPath = $null
			RegistryPath = $registryPath
		})
		continue
	}

	# Finally, try uninstall string.
	if ($uninstallString)
	{
		$didAttempt = $true
		$r = Invoke-CommandLineUninstall -CommandLine $uninstallString -DisplayName $displayName
		if (-not (Test-UninstallExitCodeSuccess -ExitCode $r.ExitCode)) { $hadFailure = $true }
		$results.Add([pscustomobject]@{
			DisplayName = $displayName
			KeyName = $keyName
			Method = 'UninstallString'
			ExitCode = $r.ExitCode
			LogPath = $null
			RegistryPath = $registryPath
		})
		continue
	}

	if (-not $didAttempt)
	{
		$results.Add([pscustomobject]@{
			DisplayName = $displayName
			KeyName = $keyName
			Method = 'None'
			ExitCode = $null
			LogPath = $null
			RegistryPath = $registryPath
		})
		Write-Output "[WARN] No uninstall command found for: $displayName (Key=$keyName)."
	}
}

$reportPath = Join-Path -Path $logFolder -ChildPath 'results.txt'
$results | Format-Table -AutoSize | Out-String | Set-Content -LiteralPath $reportPath
Write-Output "[INFO] Wrote report: $reportPath"

if ($hadFailure)
{
	Write-Output "[ERROR] One or more uninstall attempts failed. See results.txt and any MSI logs in: $logFolder"
	exit 1
}

Write-Output "[INFO] Done. Re-check Settings > Apps to confirm entries are gone."
