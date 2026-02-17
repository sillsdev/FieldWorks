param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function New-Directory {
	param([Parameter(Mandatory)][string]$Path)
	$null = New-Item -ItemType Directory -Force -Path $Path
}

function Export-UninstallSnapshot {
	param([Parameter(Mandatory)][string]$Path)

	$keys = @(
		'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*',
		'HKLM:\Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
	)

	$items = foreach ($key in $keys) {
		Get-ItemProperty -Path $key -ErrorAction SilentlyContinue |
			Where-Object {
				$_.PSObject.Properties.Match('DisplayName').Count -gt 0 -and
				-not [string]::IsNullOrWhiteSpace($_.DisplayName)
			} |
			Select-Object DisplayName, DisplayVersion, Publisher, InstallDate
	}

	$items |
		Sort-Object DisplayName, DisplayVersion |
		Format-Table -AutoSize |
		Out-String |
		Set-Content -Path $Path -Encoding UTF8
}

function Invoke-InstallerParityEvidenceRun {
	[CmdletBinding()]
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
		[string[]]$InstallArguments = @('/quiet','/norestart'),

		[Parameter()]
		[int]$SleepSecondsAfterInstall = 10
	)

	$timeStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
	$runRoot = Join-Path $WorkRoot ('Evidence\\' + $Mode + '\\' + $timeStamp)
	New-Directory -Path $runRoot

	"Mode=$Mode" | Set-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8
	"Started=$(Get-Date -Format o)" | Add-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8
	"InstallerPath=$InstallerPath" | Add-Content -Path (Join-Path $runRoot 'run-info.txt') -Encoding UTF8

	try {
		(Get-ComputerInfo | Out-String) | Set-Content -Path (Join-Path $runRoot 'computerinfo.txt') -Encoding UTF8
	} catch {
		"Get-ComputerInfo failed: $($_.Exception.Message)" | Set-Content -Path (Join-Path $runRoot 'computerinfo.txt') -Encoding UTF8
	}

	Export-UninstallSnapshot -Path (Join-Path $runRoot 'uninstall-pre.txt')

	$burnLogName = 'wix6-bundle.log'
	if ($Mode -eq 'Wix3') {
		$burnLogName = 'wix3-bundle.log'
	}
	$burnLog = Join-Path $runRoot $burnLogName
	$exe = (Resolve-Path -LiteralPath $InstallerPath).Path

	$arguments = @($InstallArguments + @('/log', $burnLog))
	"Running: $exe $($arguments -join ' ')" | Set-Content -Path (Join-Path $runRoot 'command.txt') -Encoding UTF8

	$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru -Wait
	"ExitCode=$($process.ExitCode)" | Set-Content -Path (Join-Path $runRoot 'exitcode.txt') -Encoding UTF8

	Start-Sleep -Seconds $SleepSecondsAfterInstall

	Export-UninstallSnapshot -Path (Join-Path $runRoot 'uninstall-post.txt')

	$tempOut = Join-Path $runRoot 'temp'
	New-Directory -Path $tempOut
	Copy-Item -Path (Join-Path $env:TEMP '*.log') -Destination $tempOut -Force -ErrorAction SilentlyContinue
	Copy-Item -Path (Join-Path $env:TEMP '*.txt') -Destination $tempOut -Force -ErrorAction SilentlyContinue

	$zipPath = Join-Path $WorkRoot ("evidence-$Mode-$timeStamp.zip")
	Compress-Archive -Path $runRoot -DestinationPath $zipPath -Force

	Write-Output "EvidenceFolder=$runRoot"
	Write-Output "EvidenceZip=$zipPath"
	Write-Output "ExitCode=$($process.ExitCode)"
}
