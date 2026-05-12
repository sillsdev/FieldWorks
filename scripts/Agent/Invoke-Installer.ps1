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
	[ValidateSet('Wix3', 'Wix6')]
	[string]$InstallerToolset = 'Wix3',

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
	[switch]$CaptureScreenshots,

	[Parameter(Mandatory = $false)]
	[int]$ScreenshotDurationSeconds = 20,

	[Parameter(Mandatory = $false)]
	[int]$ScreenshotIntervalSeconds = 2,

	[Parameter(Mandatory = $false)]
	[switch]$StopAfterScreenshots,

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

function New-DirectoryIfMissing {
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

	if ($InstallerToolset -eq 'Wix6') {
		$installerDir = Join-Path $RepoRoot ('FLExInstaller\wix6\bin\{0}\{1}' -f $Platform, $Configuration)
	} else {
		$installerDir = Join-Path $RepoRoot ('FLExInstaller\bin\{0}\{1}' -f $Platform, $Configuration)
	}

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

function Add-ScreenshotSupport {
	Add-Type -AssemblyName System.Drawing
	Add-Type -AssemblyName System.Windows.Forms

	if ('InstallerScreenshotNativeMethods' -as [type]) {
		return
	}

	Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;

public static class InstallerScreenshotNativeMethods
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
}
'@
}

function Show-WindowForScreenshot {
	param(
		[Parameter(Mandatory = $true)]
		[IntPtr]$WindowHandle
	)

	Add-ScreenshotSupport

	if ($WindowHandle -eq [IntPtr]::Zero) {
		return $false
	}

	$null = [InstallerScreenshotNativeMethods]::ShowWindow($WindowHandle, 9)
	$null = [InstallerScreenshotNativeMethods]::SetForegroundWindow($WindowHandle)
	Start-Sleep -Milliseconds 300
	return [InstallerScreenshotNativeMethods]::IsWindowVisible($WindowHandle)
}

function Get-SafeFileName {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Value
	)

	$fileName = $Value
	foreach ($invalidChar in [System.IO.Path]::GetInvalidFileNameChars()) {
		$fileName = $fileName.Replace($invalidChar, '_')
	}

	if ([string]::IsNullOrWhiteSpace($fileName)) {
		return 'window'
	}

	return $fileName
}

function Get-InstallerWindowCandidates {
	param(
		[Parameter(Mandatory = $true)]
		[int]$RootProcessId,
		[Parameter(Mandatory = $true)]
		[string]$InstallerPath
	)

	$processNames = New-Object 'System.Collections.Generic.HashSet[string]' ([System.StringComparer]::OrdinalIgnoreCase)
	$installerProcessName = [System.IO.Path]::GetFileNameWithoutExtension($InstallerPath)
	$null = $processNames.Add($installerProcessName)
	$null = $processNames.Add('FieldWorksBundle')
	$null = $processNames.Add('FieldWorksOfflineBundle')
	$null = $processNames.Add('msiexec')

	try {
		$rootProcess = Get-Process -Id $RootProcessId -ErrorAction Stop
		$null = $processNames.Add($rootProcess.ProcessName)
	} catch { }

	$windows = New-Object 'System.Collections.Generic.List[object]'
	foreach ($process in Get-Process -ErrorAction SilentlyContinue) {
		if (-not $processNames.Contains($process.ProcessName)) { continue }
		if ($process.MainWindowHandle -eq [IntPtr]::Zero) { continue }

		$title = $process.MainWindowTitle
		if ([string]::IsNullOrWhiteSpace($title)) {
			$title = $process.ProcessName
		}

		$windows.Add([pscustomobject]@{
			ProcessId = $process.Id
			ProcessName = $process.ProcessName
			WindowHandle = $process.MainWindowHandle
			Title = $title
		})
	}

	return $windows
}

function Save-WindowScreenshot {
	param(
		[Parameter(Mandatory = $true)]
		[IntPtr]$WindowHandle,
		[Parameter(Mandatory = $true)]
		[string]$Path
	)

	Add-ScreenshotSupport
	$null = Show-WindowForScreenshot -WindowHandle $WindowHandle

	$rect = New-Object InstallerScreenshotNativeMethods+RECT
	if (-not [InstallerScreenshotNativeMethods]::GetWindowRect($WindowHandle, [ref]$rect)) {
		return $false
	}

	$width = $rect.Right - $rect.Left
	$height = $rect.Bottom - $rect.Top
	if ($width -le 0 -or $height -le 0) {
		return $false
	}

	$bitmap = New-Object System.Drawing.Bitmap $width, $height
	$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
	try {
		$graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, $bitmap.Size)
		$bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
		return $true
	} catch {
		return $false
	} finally {
		$graphics.Dispose()
		$bitmap.Dispose()
	}
}

function Save-DesktopScreenshot {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $false)]
		[IntPtr]$WindowHandle = [IntPtr]::Zero
	)

	Add-ScreenshotSupport

	$bounds = $null
	if ($WindowHandle -ne [IntPtr]::Zero) {
		$null = Show-WindowForScreenshot -WindowHandle $WindowHandle
		$rect = New-Object InstallerScreenshotNativeMethods+RECT
		if ([InstallerScreenshotNativeMethods]::GetWindowRect($WindowHandle, [ref]$rect)) {
			$width = $rect.Right - $rect.Left
			$height = $rect.Bottom - $rect.Top
			if ($width -gt 0 -and $height -gt 0) {
				$bounds = New-Object System.Drawing.Rectangle $rect.Left, $rect.Top, $width, $height
			}
		}
	}

	if ($null -eq $bounds) {
		$bounds = [System.Windows.Forms.SystemInformation]::VirtualScreen
	}

	if ($bounds.Width -le 0 -or $bounds.Height -le 0) {
		return $false
	}

	$bitmap = New-Object System.Drawing.Bitmap $bounds.Width, $bounds.Height
	$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
	try {
		$graphics.CopyFromScreen($bounds.Left, $bounds.Top, 0, 0, $bitmap.Size)
		$bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
		return $true
	} catch {
		return $false
	} finally {
		$graphics.Dispose()
		$bitmap.Dispose()
	}
}

function Save-InstallerScreenshots {
	param(
		[Parameter(Mandatory = $true)]
		[int]$ProcessId,
		[Parameter(Mandatory = $true)]
		[string]$InstallerPath,
		[Parameter(Mandatory = $true)]
		[string]$EvidenceDir,
		[Parameter(Mandatory = $true)]
		[int]$DurationSeconds,
		[Parameter(Mandatory = $true)]
		[int]$IntervalSeconds
	)

	if ($DurationSeconds -lt 1) { $DurationSeconds = 1 }
	if ($IntervalSeconds -lt 1) { $IntervalSeconds = 1 }

	$screenshotDir = Join-Path $EvidenceDir 'screenshots'
	New-DirectoryIfMissing -Path $screenshotDir

	$manifestPath = Join-Path $screenshotDir 'manifest.txt'
	$manifest = New-Object 'System.Collections.Generic.List[string]'
	$manifest.Add(('Started: {0:o}' -f (Get-Date)))
	$manifest.Add(('InstallerPath: {0}' -f $InstallerPath))
	$manifest.Add(('RootProcessId: {0}' -f $ProcessId))
	$manifest.Add(('DurationSeconds: {0}' -f $DurationSeconds))
	$manifest.Add(('IntervalSeconds: {0}' -f $IntervalSeconds))
	$manifest.Add('')

	$deadline = (Get-Date).AddSeconds($DurationSeconds)
	$captureIndex = 0

	while ((Get-Date) -lt $deadline) {
		$windows = @(Get-InstallerWindowCandidates -RootProcessId $ProcessId -InstallerPath $InstallerPath)
		if ($windows.Count -eq 0) {
			$manifest.Add(('{0:o} No visible installer windows found.' -f (Get-Date)))
		} else {
			foreach ($window in $windows) {
				$captureIndex++
				$safeTitle = Get-SafeFileName -Value $window.Title
				$fileName = ('{0:0000}-{1}-{2}.png' -f $captureIndex, $window.ProcessName, $safeTitle)
				$screenshotPath = Join-Path $screenshotDir $fileName
				if (Save-WindowScreenshot -WindowHandle $window.WindowHandle -Path $screenshotPath) {
					$manifest.Add(('{0:o} {1} PID={2} Title="{3}" Path={4}' -f (Get-Date), $window.ProcessName, $window.ProcessId, $window.Title, $screenshotPath))
				} elseif (Save-DesktopScreenshot -Path $screenshotPath -WindowHandle $window.WindowHandle) {
					$manifest.Add(('{0:o} {1} PID={2} Title="{3}" Path={4} Capture=DesktopFallback' -f (Get-Date), $window.ProcessName, $window.ProcessId, $window.Title, $screenshotPath))
				} else {
					$manifest.Add(('{0:o} Failed to capture {1} PID={2} Title="{3}"' -f (Get-Date), $window.ProcessName, $window.ProcessId, $window.Title))
				}
			}
		}

		Start-Sleep -Seconds $IntervalSeconds
	}

	$manifest.Add('')
	$manifest.Add(('Finished: {0:o}' -f (Get-Date)))
	$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::WriteAllLines($manifestPath, $manifest, $utf8NoBom)

	return $screenshotDir
}

function Stop-InstallerProcessAfterScreenshots {
	param(
		[Parameter(Mandatory = $true)]
		[System.Diagnostics.Process]$Process
	)

	$Process.Refresh()
	if ($Process.HasExited) {
		return $false
	}

	Write-Output "Stopping installer process after screenshot capture: $($Process.Id)"
	Stop-Process -Id $Process.Id -Force -ErrorAction Stop
	try {
		Wait-Process -Id $Process.Id -Timeout 5 -ErrorAction SilentlyContinue
	} catch { }

	return $true
}

function Get-ProcessExitState {
	param(
		[Parameter(Mandatory = $true)]
		[System.Diagnostics.Process]$Process
	)

	$Process.Refresh()
	if ($Process.HasExited) {
		return [pscustomobject]@{
			ExitCode = [int]$Process.ExitCode
			IsRunning = $false
		}
	}

	return [pscustomobject]@{
		ExitCode = $null
		IsRunning = $true
	}
}

$repoRoot = Resolve-RepoRoot

$resolvedType = Resolve-InstallerType -InstallerType $InstallerType -InstallerPath $InstallerPath

if ([string]::IsNullOrWhiteSpace($InstallerPath)) {
	$InstallerPath = Resolve-DefaultInstallerPath -RepoRoot $repoRoot -ResolvedType $resolvedType -Configuration $Configuration -Platform $Platform
}

if (!(Test-Path -LiteralPath $InstallerPath)) {
	throw "Installer path not found: $InstallerPath. Build it first (e.g., .\build.ps1 -BuildInstaller -Configuration $Configuration -InstallerToolset $InstallerToolset)."
}

$InstallerPath = (Resolve-Path -LiteralPath $InstallerPath).Path

if ([string]::IsNullOrWhiteSpace($LogRoot)) {
	$LogRoot = Join-Path $repoRoot 'Output\InstallerEvidence'
}

if ([string]::IsNullOrWhiteSpace($RunId)) {
	$RunId = (Get-Date -Format 'yyyyMMdd-HHmmss')
}

$evidenceDir = Join-Path $LogRoot $RunId
New-DirectoryIfMissing -Path $evidenceDir

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
		$screenshotDir = $null
		$stoppedAfterScreenshots = $false

		if ($CaptureScreenshots) {
			$screenshotDir = Save-InstallerScreenshots -ProcessId $process.Id -InstallerPath $InstallerPath -EvidenceDir $evidenceDir -DurationSeconds $ScreenshotDurationSeconds -IntervalSeconds $ScreenshotIntervalSeconds
			Write-Output "Screenshots: $screenshotDir"
		}

		if ($CaptureScreenshots -and $StopAfterScreenshots) {
			$stoppedAfterScreenshots = Stop-InstallerProcessAfterScreenshots -Process $process
		}

		if (-not $NoWait) {
			if ($TimeoutSeconds -gt 0) {
				$null = Wait-Process -Id $process.Id -Timeout $TimeoutSeconds
			} else {
				$process.WaitForExit()
			}
		}

		$exitState = Get-ProcessExitState -Process $process
		$exitCode = $exitState.ExitCode
		$finished = Get-Date

		if ($stoppedAfterScreenshots) {
			$exitCode = 0
			$global:LASTEXITCODE = 0
			Write-Output "ExitCode: 0 (stopped after screenshots)"
		} elseif ($null -eq $exitCode) {
			$global:LASTEXITCODE = 0
			Write-Output "ExitCode: <process still running; NoWait=$NoWait>"
		} else {
			$global:LASTEXITCODE = $exitCode
			Write-Output "ExitCode: $exitCode"
		}

		Write-Output "ProcessId: $($process.Id)"

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
				ProcessExitCode = $exitState.ExitCode
				ProcessId = $process.Id
				IsRunning = $exitState.IsRunning
				StoppedAfterScreenshots = $stoppedAfterScreenshots
				ScreenshotDir = $screenshotDir
			}
		}

		if ($NoExit -or $PassThru) {
			return $result
		}

		if ($null -eq $exitCode) { exit 0 }
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
		$screenshotDir = $null
		$stoppedAfterScreenshots = $false

		if ($CaptureScreenshots) {
			$screenshotDir = Save-InstallerScreenshots -ProcessId $process.Id -InstallerPath $InstallerPath -EvidenceDir $evidenceDir -DurationSeconds $ScreenshotDurationSeconds -IntervalSeconds $ScreenshotIntervalSeconds
			Write-Output "Screenshots: $screenshotDir"
		}

		if ($CaptureScreenshots -and $StopAfterScreenshots) {
			$stoppedAfterScreenshots = Stop-InstallerProcessAfterScreenshots -Process $process
		}

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

		$exitState = Get-ProcessExitState -Process $process
		$exitCode = $exitState.ExitCode
		$finished = Get-Date

		if ($stoppedAfterScreenshots) {
			$exitCode = 0
			$global:LASTEXITCODE = 0
			Write-Output "ExitCode: 0 (stopped after screenshots)"
		} elseif ($null -eq $exitCode) {
			$global:LASTEXITCODE = 0
			Write-Output "ExitCode: <process still running; NoWait=$NoWait>"
		} else {
			$global:LASTEXITCODE = $exitCode
			Write-Output "ExitCode: $exitCode"
		}

		Write-Output "ProcessId: $($process.Id)"

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
				ProcessExitCode = $exitState.ExitCode
				ProcessId = $process.Id
				IsRunning = $exitState.IsRunning
				StoppedAfterScreenshots = $stoppedAfterScreenshots
				ScreenshotDir = $screenshotDir
			}
		}

		if ($NoExit -or $PassThru) {
			return $result
		}

		if ($null -eq $exitCode) { exit 0 }
		exit $exitCode
	}
}
