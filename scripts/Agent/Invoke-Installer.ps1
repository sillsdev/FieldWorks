[CmdletBinding(SupportsShouldProcess = $true)]
param(
	[Parameter(Mandatory = $false)]
	[ValidateSet('Auto', 'Bundle', 'Msi')]
	[string]$InstallerType = 'Auto',

	[Parameter(Mandatory = $false)]
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',

	[Parameter(Mandatory = $false)]
	[ValidateSet('x64', 'x86')]
	[string]$Platform = 'x64',

	[Parameter(Mandatory = $false)]
	[string]$InstallerPath,

	[Parameter(Mandatory = $false)]
	[string]$LogRoot,

	[Parameter(Mandatory = $false)]
	[string]$RunId,

	[Parameter(Mandatory = $false)]
	[string[]]$Arguments = @(),

	[Parameter(Mandatory = $false)]
	[switch]$NoWait,

	[Parameter(Mandatory = $false)]
	[int]$TimeoutSeconds = 0,

	[Parameter(Mandatory = $false)]
	[switch]$IncludeTempLogs,

	[Parameter(Mandatory = $false)]
	[switch]$SummarizeMsiFileAccess,

	[Parameter(Mandatory = $false)]
	[switch]$PassThru,

	[Parameter(Mandatory = $false)]
	[switch]$NoExit
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-RepoRoot {
	$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
	return $repoRoot.Path
}

function Ensure-Directory {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path
	)

	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Resolve-DefaultInstallerPath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$RepoRoot,
		[Parameter(Mandatory = $true)]
		[string]$ResolvedType,
		[Parameter(Mandatory = $true)]
		[string]$Configuration,
		[Parameter(Mandatory = $true)]
		[string]$Platform
	)

	$installerDir = Join-Path $RepoRoot ('FLExInstaller\bin\{0}\{1}' -f $Platform, $Configuration)

	if ($ResolvedType -eq 'Msi') {
		return Join-Path $installerDir 'en-US\FieldWorks.msi'
	}

	return Join-Path $installerDir 'FieldWorksBundle.exe'
}

function Resolve-InstallerType {
	param(
		[Parameter(Mandatory = $true)]
		[string]$InstallerType,
		[Parameter(Mandatory = $false)]
		[string]$InstallerPath
	)

	if ($InstallerType -ne 'Auto') {
		return $InstallerType
	}

	if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
		return 'Bundle'
	}

	$ext = [IO.Path]::GetExtension($InstallerPath)
	if ($ext -ieq '.msi') { return 'Msi' }
	if ($ext -ieq '.exe') { return 'Bundle' }

	throw "Unable to infer installer type from path '$InstallerPath'. Use -InstallerType Bundle|Msi."
}

function Copy-TempLogs {
	param(
		[Parameter(Mandatory = $true)]
		[datetime]$Started,
		[Parameter(Mandatory = $true)]
		[datetime]$Finished,
		[Parameter(Mandatory = $true)]
		[string]$DestinationDir
	)

	$patterns = @(
		'WixBundleLog*.log',
		'WixBundleLog*.txt',
		'*.msi.log',
		'*.burn.log'
	)

	foreach ($pattern in $patterns) {
		$items = Get-ChildItem -LiteralPath $env:TEMP -File -Filter $pattern -ErrorAction SilentlyContinue
		foreach ($item in $items) {
			if ($item.LastWriteTime -lt $Started) { continue }
			if ($item.LastWriteTime -gt $Finished) { continue }

			$dest = Join-Path $DestinationDir $item.Name
			Copy-Item -LiteralPath $item.FullName -Destination $dest -Force
		}
	}
}

function Write-MsiFileAccessSummary {
	param(
		[Parameter(Mandatory = $true)]
		[string]$MsiLogPath,
		[Parameter(Mandatory = $true)]
		[string]$SummaryPath
	)

	if (!(Test-Path -LiteralPath $MsiLogPath)) {
		return
	}

	$seen = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
	$paths = New-Object 'System.Collections.Generic.List[string]'
	$regex = [regex]'^.*\bFile:\s+(?<path>[^;]+);'

	foreach ($line in Get-Content -LiteralPath $MsiLogPath -ErrorAction Stop) {
		$match = $regex.Match($line)
		if (-not $match.Success) { continue }

		$filePath = $match.Groups['path'].Value.Trim()
		if ([string]::IsNullOrWhiteSpace($filePath)) { continue }

		if ($seen.Add($filePath)) {
			$paths.Add($filePath)
		}
	}

	$paths.Sort([System.StringComparer]::OrdinalIgnoreCase)

	$lines = New-Object 'System.Collections.Generic.List[string]'
	$lines.Add(('MSI file-access summary for: {0}' -f $MsiLogPath))
	$lines.Add(('Unique file paths: {0}' -f $paths.Count))
	$lines.Add('')
	$lines.AddRange($paths)

	$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::WriteAllLines($SummaryPath, $lines, $utf8NoBom)
}

$repoRoot = Resolve-RepoRoot

$resolvedType = Resolve-InstallerType -InstallerType $InstallerType -InstallerPath $InstallerPath

if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
	$InstallerPath = Resolve-DefaultInstallerPath -RepoRoot $repoRoot -ResolvedType $resolvedType -Configuration $Configuration -Platform $Platform
}

if (!(Test-Path -LiteralPath $InstallerPath)) {
	throw "Installer path not found: $InstallerPath. Build it first (e.g., .\\build.ps1 -BuildInstaller -Configuration $Configuration)."
}

$InstallerPath = (Resolve-Path -LiteralPath $InstallerPath).Path

if ([string]::IsNullOrWhiteSpace($LogRoot)) {
	$LogRoot = Join-Path $repoRoot 'Output\InstallerEvidence'
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
	$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss')
}

$evidenceDir = Join-Path $LogRoot $RunId
Ensure-Directory -Path $evidenceDir

$started = Get-Date

if ($resolvedType -eq 'Msi') {
	$msiLog = Join-Path $evidenceDir 'msi-install.log'
	$msiFileSummary = Join-Path $evidenceDir 'msi-files.txt'

	$msiArgs = @('/i', $InstallerPath, '/l*v', $msiLog)
	$msiArgs += $Arguments

	Write-Output "Running MSI: $InstallerPath"
	Write-Output "Evidence dir: $evidenceDir"
	Write-Output "MSI log: $msiLog"
	Write-Output ("Command: msiexec {0}" -f ($msiArgs -join ' '))

	if ($PSCmdlet.ShouldProcess($InstallerPath, 'Run MSI')) {
		$process = Start-Process -FilePath 'msiexec.exe' -ArgumentList $msiArgs -PassThru

		if (-not $NoWait) {
			if ($TimeoutSeconds -gt 0) {
				$null = Wait-Process -Id $process.Id -Timeout $TimeoutSeconds
			} else {
				$process.WaitForExit()
			}
		}

		$exitCode = $process.ExitCode
		$finished = Get-Date

		$global:LASTEXITCODE = $exitCode

		Write-Output "ExitCode: $exitCode"

		if ($IncludeTempLogs) {
			Copy-TempLogs -Started $started -Finished $finished -DestinationDir $evidenceDir
		}

		if ($SummarizeMsiFileAccess) {
			Write-MsiFileAccessSummary -MsiLogPath $msiLog -SummaryPath $msiFileSummary
		}

		$result = $null
		if ($PassThru) {
			$result = [pscustomobject]@{
				InstallerType = 'Msi'
				InstallerPath = $InstallerPath
				EvidenceDir = $evidenceDir
				PrimaryLogPath = $msiLog
				ExitCode = $exitCode
			}
		}

		if ($NoExit -or $PassThru) {
			return $result
		}

		exit $exitCode
	}
} else {
	$bundleLog = Join-Path $evidenceDir 'bundle.log'
	$bundleMsiFileSummary = Join-Path $evidenceDir 'bundle-msi-files.txt'

	# Burn bundles typically require either:
	# - interactive UI (no args; user clicks Install/Uninstall), or
	# - a non-interactive mode like /passive or /quiet.
	# If callers run with no args, it can look like the bundle "hangs" after Detect.
	if ($Arguments.Count -eq 0) {
		Write-Output 'NOTE: No bundle arguments provided. The UI will wait for user action after Detect.'
		Write-Output '      For automated installs, pass /passive (or /quiet) explicitly.'
	}

	$bundleArgs = @('/log', $bundleLog)
	$bundleArgs += $Arguments

	Write-Output "Running bundle: $InstallerPath"
	Write-Output "Evidence dir: $evidenceDir"
	Write-Output "Bundle log: $bundleLog"
	Write-Output ("Command: {0} {1}" -f $InstallerPath, ($bundleArgs -join ' '))

	if ($PSCmdlet.ShouldProcess($InstallerPath, 'Run bundle')) {
		$process = Start-Process -FilePath $InstallerPath -ArgumentList $bundleArgs -PassThru

		if (-not $NoWait) {
			try {
				if ($TimeoutSeconds -gt 0) {
					$null = Wait-Process -Id $process.Id -Timeout $TimeoutSeconds
				} else {
					$process.WaitForExit()
				}
			} catch {
				Write-Error "Timed out after $TimeoutSeconds seconds waiting for bundle to exit."
				try { Stop-Process -Id $process.Id -Force -ErrorAction Stop } catch { }
				throw
			}
		}

		$exitCode = $process.ExitCode
		$finished = Get-Date

		$global:LASTEXITCODE = $exitCode

		Write-Output "ExitCode: $exitCode"

		if ($IncludeTempLogs) {
			Copy-TempLogs -Started $started -Finished $finished -DestinationDir $evidenceDir
		}

		if ($SummarizeMsiFileAccess) {
			# Look for the MSI package log captured alongside bundle.log.
			$msiLogs = @(Get-ChildItem -LiteralPath $evidenceDir -File -Filter '*AppMsiPackage*.log' -ErrorAction SilentlyContinue)
			if ($msiLogs.Count -gt 0) {
				$msiLogPath = ($msiLogs | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
				Write-MsiFileAccessSummary -MsiLogPath $msiLogPath -SummaryPath $bundleMsiFileSummary
			}
		}

		$result = $null
		if ($PassThru) {
			$result = [pscustomobject]@{
				InstallerType = 'Bundle'
				InstallerPath = $InstallerPath
				EvidenceDir = $evidenceDir
				PrimaryLogPath = $bundleLog
				ExitCode = $exitCode
			}
		}

		if ($NoExit -or $PassThru) {
			return $result
		}

		exit $exitCode
	}
}
