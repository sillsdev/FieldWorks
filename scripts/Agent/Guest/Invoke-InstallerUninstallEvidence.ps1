param(
	[Parameter(Mandatory)]
	[ValidateSet('Wix3','Wix6')]
	[string]$Mode,

	[Parameter(Mandatory)]
	[ValidateNotNullOrEmpty()]
	[string]$InstallerPath,

	[Parameter()]
	[string]$WorkRoot = 'C:\FWInstallerTest',

	[Parameter()]
	[string[]]$UninstallArguments = @('/quiet', '/norestart'),

	[Parameter()]
	[int]$TimeoutSeconds = 1200
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $PSCommandPath
. (Join-Path $scriptRoot 'InstallerParityEvidence.Common.ps1')

function Get-ExePathFromUninstallString {
	param([Parameter(Mandatory)][string]$UninstallString)

	if ($UninstallString -match '^"(?<exe>[^"]+\.exe)"') {
		return $Matches['exe']
	}

	if ($UninstallString -match '^(?<exe>[^\s]+\.exe)\b') {
		return $Matches['exe']
	}

	return $null
}

function Get-InstalledBundleExePath {
	param([Parameter(Mandatory)][string]$RunRoot)

	$keys = @(
		'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
		'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
	)

	$raw = foreach ($key in $keys) {
		Get-ItemProperty -Path $key -ErrorAction SilentlyContinue |
			Where-Object {
				$_.PSObject.Properties.Match('DisplayName').Count -gt 0 -and
				-not [string]::IsNullOrWhiteSpace($_.DisplayName) -and
				$_.PSObject.Properties.Match('UninstallString').Count -gt 0 -and
				-not [string]::IsNullOrWhiteSpace($_.UninstallString)
			} |
			Select-Object DisplayName, DisplayVersion, Publisher, InstallDate, UninstallString
	}

	$raw |
		Sort-Object DisplayName, DisplayVersion |
		Format-Table -AutoSize |
		Out-String |
		Set-Content -Path (Join-Path $RunRoot 'installed-uninstall-entries.txt') -Encoding UTF8

	$candidates = $raw | Where-Object {
		$_.DisplayName -like 'FieldWorks*' -or $_.DisplayName -like '*FieldWorks*'
	}

	$preferred = $candidates | Where-Object { $_.UninstallString -match 'FieldWorksBundle\.exe' } | Select-Object -First 1
	if (-not $preferred) {
		$preferred = $candidates | Where-Object { $_.UninstallString -match '\.exe' } | Select-Object -First 1
	}

	if ($preferred) {
		$exe = Get-ExePathFromUninstallString -UninstallString $preferred.UninstallString
		if ($exe -and (Test-Path -LiteralPath $exe)) {
			return $exe
		}
	}

	return $null
}

$timeStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$runRoot = Join-Path $WorkRoot ('Evidence\\' + $Mode + '\\' + $timeStamp + '-uninstall')
New-Directory -Path $runRoot

"Mode=$Mode" | Set-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8
"Started=$(Get-Date -Format o)" | Add-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8
"InstallerPath=$InstallerPath" | Add-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8
"TimeoutSeconds=$TimeoutSeconds" | Add-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8

try {
	(Get-ComputerInfo | Out-String) | Set-Content -Path (Join-Path $runRoot 'computerinfo.txt') -Encoding UTF8
} catch {
	"Get-ComputerInfo failed: $($_.Exception.Message)" | Set-Content -Path (Join-Path $runRoot 'computerinfo.txt') -Encoding UTF8
}

Export-UninstallSnapshot -Path (Join-Path $runRoot 'uninstall-pre.txt')

$installedExe = Get-InstalledBundleExePath -RunRoot $runRoot
$exe = $installedExe
if (-not $exe) {
	$exe = (Resolve-Path -LiteralPath $InstallerPath).Path
}

$burnLogName = 'wix6-uninstall-bundle.log'
if ($Mode -eq 'Wix3') {
	$burnLogName = 'wix3-uninstall-bundle.log'
}
$burnLog = Join-Path $runRoot $burnLogName

$arguments = @('/uninstall')
$arguments += $UninstallArguments
$arguments += @('/log', $burnLog)

"UsingExe=$exe" | Add-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8
"Arguments=$($arguments -join ' ')" | Add-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8

$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru
$null = Wait-Process -Id $process.Id -Timeout $TimeoutSeconds -ErrorAction SilentlyContinue
$process.Refresh()

if (-not $process.HasExited) {
	"Timeout after $TimeoutSeconds seconds. ProcessId=$($process.Id)" | Set-Content -Path (Join-Path $runRoot 'timeout.txt') -Encoding UTF8

	try {
		Get-Process |
			Where-Object { $_.ProcessName -match 'FieldWorks|msiexec|setup|burn|wix' } |
			Select-Object Id, ProcessName, StartTime, CPU |
			Sort-Object ProcessName, Id |
			Format-Table -AutoSize |
			Out-String |
			Set-Content -Path (Join-Path $runRoot 'related-processes.txt') -Encoding UTF8
	} catch {
		"Process listing failed: $($_.Exception.Message)" | Set-Content -Path (Join-Path $runRoot 'related-processes.txt') -Encoding UTF8
	}

	try {
		Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
		Start-Sleep -Seconds 2
	} catch {
		"Stop-Process failed: $($_.Exception.Message)" | Set-Content -Path (Join-Path $runRoot 'stop-process-error.txt') -Encoding UTF8
	}

	$process.Refresh()
	if ($process.HasExited) {
		"ExitCode=$($process.ExitCode)" | Set-Content -Path (Join-Path $runRoot 'exitcode.txt') -Encoding UTF8
	} else {
		"ExitCode=UNKNOWN (still running)" | Set-Content -Path (Join-Path $runRoot 'exitcode.txt') -Encoding UTF8
	}
} else {
	"ExitCode=$($process.ExitCode)" | Set-Content -Path (Join-Path $runRoot 'exitcode.txt') -Encoding UTF8
}

Export-UninstallSnapshot -Path (Join-Path $runRoot 'uninstall-post.txt')

$tempOut = Join-Path $runRoot 'temp'
New-Directory -Path $tempOut
Copy-Item -Path (Join-Path $env:TEMP '*.log') -Destination $tempOut -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $env:TEMP '*.txt') -Destination $tempOut -Force -ErrorAction SilentlyContinue

$zipPath = Join-Path $WorkRoot ("evidence-$Mode-$timeStamp-uninstall.zip")
Compress-Archive -Path $runRoot -DestinationPath $zipPath -Force

Write-Output "EvidenceFolder=$runRoot"
Write-Output "EvidenceZip=$zipPath"
