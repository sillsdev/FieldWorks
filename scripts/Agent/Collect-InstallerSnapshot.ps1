[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$OutputPath,

	[Parameter(Mandatory = $false)]
	[string]$Name = 'snapshot',

	[Parameter(Mandatory = $false)]
	[string[]]$UninstallNamePatterns = @('FieldWorks', 'FLEx', 'SIL'),

	[Parameter(Mandatory = $false)]
	[string[]]$RegistryRoots = @(
		'HKLM:\SOFTWARE\SIL',
		'HKLM:\SOFTWARE\WOW6432Node\SIL',
		'HKCU:\SOFTWARE\SIL'
	),

	[Parameter(Mandatory = $false)]
	[string[]]$FileRoots = @(
		'$env:ProgramFiles\SIL',
		'$env:ProgramFiles(x86)\SIL',
		'$env:ProgramData\SIL',
		'$env:APPDATA\SIL',
		'$env:LOCALAPPDATA\SIL',
		'$env:ProgramData\Microsoft\Windows\Start Menu\Programs',
		'$env:PUBLIC\Desktop'
	),

	[Parameter(Mandatory = $false)]
	[int]$MaxFileCount = 20000
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Ensure-Directory {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path
	)

	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Resolve-ExpandedPath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path
	)

	# Expand $env:... expressions that appear as literal strings in defaults.
	return $ExecutionContext.InvokeCommand.ExpandString($Path)
}

function Get-UninstallEntries {
	param(
		[Parameter(Mandatory = $true)]
		[string[]]$NamePatterns
	)

	$uninstallRoots = @(
		'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall',
		'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall',
		'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall'
	)

	$results = New-Object System.Collections.Generic.List[object]

	foreach ($root in $uninstallRoots) {
		if (!(Test-Path -LiteralPath $root)) { continue }

		$subKeys = Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue
		foreach ($subKey in $subKeys) {
			try {
				$props = Get-ItemProperty -LiteralPath $subKey.PSPath -ErrorAction Stop
				$displayName = [string]$props.DisplayName
				if ([string]::IsNullOrWhiteSpace($displayName)) { continue }

				$matches = $false
				foreach ($pattern in $NamePatterns) {
					if ($displayName -like ('*' + $pattern + '*')) { $matches = $true; break }
				}
				if (-not $matches) { continue }

				$results.Add([pscustomobject]@{
					KeyPath = $subKey.Name
					DisplayName = $displayName
					DisplayVersion = [string]$props.DisplayVersion
					Publisher = [string]$props.Publisher
					InstallLocation = [string]$props.InstallLocation
					UninstallString = [string]$props.UninstallString
				})
			} catch {
				# Ignore unreadable keys
			}
		}
	}

	return $results
}

function Get-RegistrySnapshot {
	param(
		[Parameter(Mandatory = $true)]
		[string[]]$Roots
	)

	$results = New-Object System.Collections.Generic.List[object]

	foreach ($root in $Roots) {
		if (!(Test-Path -LiteralPath $root)) { continue }

		try {
			$props = Get-ItemProperty -LiteralPath $root -ErrorAction Stop
			$values = @{}
			foreach ($p in $props.PSObject.Properties) {
				if ($p.Name -in @('PSPath', 'PSParentPath', 'PSChildName', 'PSDrive', 'PSProvider')) { continue }
				$values[$p.Name] = [string]$p.Value
			}

			$results.Add([pscustomobject]@{
				Path = $root
				Values = $values
			})
		} catch {
			# Ignore unreadable roots
		}
	}

	return $results
}

function Get-EnvironmentSnapshot {
	$machine = [Environment]::GetEnvironmentVariables('Machine')
	$user = [Environment]::GetEnvironmentVariables('User')

	$interesting = @{}
	foreach ($key in $machine.Keys) {
		$name = [string]$key
		if ($name -eq 'Path' -or $name -like 'FW*' -or $name -like 'FLEX*' -or $name -like 'SIL*') {
			$interesting['Machine:' + $name] = [string]$machine[$key]
		}
	}
	foreach ($key in $user.Keys) {
		$name = [string]$key
		if ($name -eq 'Path' -or $name -like 'FW*' -or $name -like 'FLEX*' -or $name -like 'SIL*') {
			$interesting['User:' + $name] = [string]$user[$key]
		}
	}

	return $interesting
}

function Get-FileSnapshot {
	param(
		[Parameter(Mandatory = $true)]
		[string[]]$Roots,
		[Parameter(Mandatory = $true)]
		[int]$MaxFileCount
	)

	$results = New-Object System.Collections.Generic.List[object]

	foreach ($rootExpr in $Roots) {
		$root = Resolve-ExpandedPath -Path $rootExpr
		if ([string]::IsNullOrWhiteSpace($root)) { continue }
		if (!(Test-Path -LiteralPath $root)) { continue }

		# Heuristic filters to keep snapshot size reasonable.
		$filterStartMenu = $root -like '*\\Start Menu\\Programs'
		$filterDesktop = $root -like '*\\Desktop'

		$files = Get-ChildItem -LiteralPath $root -Recurse -File -ErrorAction SilentlyContinue
		foreach ($file in $files) {
			if ($results.Count -ge $MaxFileCount) { break }

			if ($filterStartMenu -or $filterDesktop) {
				if ($file.Extension -ne '.lnk') { continue }
				if ($file.Name -notmatch '(?i)(fieldworks|flex|sil)') { continue }
			}

			$results.Add([pscustomobject]@{
				Path = $file.FullName
				Length = [long]$file.Length
				LastWriteTimeUtc = $file.LastWriteTimeUtc.ToString('o')
			})
		}
	}

	return $results
}

# Resolve output file path
$outPath = Resolve-ExpandedPath -Path $OutputPath

if (Test-Path -LiteralPath $outPath -PathType Container) {
	$outPath = Join-Path $outPath ('snapshot-{0}.json' -f $Name)
} else {
	$parent = Split-Path -Parent $outPath
	if (-not [string]::IsNullOrWhiteSpace($parent)) {
		Ensure-Directory -Path $parent
	}
}

$parentDir = Split-Path -Parent $outPath
if (-not [string]::IsNullOrWhiteSpace($parentDir)) {
	Ensure-Directory -Path $parentDir
}

$snapshot = [pscustomobject]@{
	SnapshotVersion = 1
	Name = $Name
	CreatedUtc = (Get-Date).ToUniversalTime().ToString('o')
	MachineName = $env:COMPUTERNAME
	UserName = $env:USERNAME
	OSVersion = [System.Environment]::OSVersion.VersionString
	UninstallEntries = (Get-UninstallEntries -NamePatterns $UninstallNamePatterns)
	Registry = (Get-RegistrySnapshot -Roots $RegistryRoots)
	Environment = (Get-EnvironmentSnapshot)
	Files = (Get-FileSnapshot -Roots $FileRoots -MaxFileCount $MaxFileCount)
}

$json = $snapshot | ConvertTo-Json -Depth 10
Set-Content -LiteralPath $outPath -Value $json -Encoding UTF8

Write-Output "Wrote snapshot: $outPath"
Write-Output ("UninstallEntries: {0}, RegistryRoots: {1}, Files: {2}" -f $snapshot.UninstallEntries.Count, $snapshot.Registry.Count, $snapshot.Files.Count)
