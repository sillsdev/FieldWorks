[CmdletBinding()]
param(
	[Parameter(Mandatory = $false)]
	[string]$RepoRoot,

	[Parameter(Mandatory = $false)]
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Release',

	[Parameter(Mandatory = $false)]
	[ValidateSet('x64')]
	[string]$Platform = 'x64',

	[Parameter(Mandatory = $false)]
	[string]$BuildLogPath,

	[Parameter(Mandatory = $false)]
	[string]$ReportPath,

	[Parameter(Mandatory = $false)]
	[string]$WixVersion = '6.0.2',

	[Parameter(Mandatory = $false)]
	[long]$MinimumOfflineBundleBytes = 50000000,

	[Parameter(Mandatory = $false)]
	[switch]$SkipBuildLogAudit,

	[Parameter(Mandatory = $false)]
	[switch]$RequireNoWix3ToolsOnPath,

	[Parameter(Mandatory = $false)]
	[switch]$AllowPatchableInstallerDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-DefaultRepoRoot {
	$repoRootPath = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..\..')
	return $repoRootPath.Path
}

function New-DirectoryIfMissing {
	param([Parameter(Mandatory = $true)][string]$Path)

	if (!(Test-Path -LiteralPath $Path)) {
		$null = New-Item -ItemType Directory -Path $Path -Force
	}
}

function Convert-ToRepoRelativePath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $true)]
		[string]$Root
	)

	$fullPath = [System.IO.Path]::GetFullPath($Path)
	$fullRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
	if ($fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
		return $fullPath.Substring($fullRoot.Length).TrimStart('\', '/')
	}

	return $fullPath
}

function Add-CheckResult {
	param(
		[Parameter(Mandatory = $true)]
		[bool]$Passed,
		[Parameter(Mandatory = $true)]
		[string]$Message
	)

	if ($Passed) {
		$script:ReportLines.Add(('[OK] {0}' -f $Message))
		return
	}

	$script:ReportLines.Add(('[FAIL] {0}' -f $Message))
	$script:Failures.Add($Message)
}

function Add-WarningResult {
	param([Parameter(Mandatory = $true)][string]$Message)

	$script:ReportLines.Add(('[WARN] {0}' -f $Message))
	$script:Warnings.Add($Message)
}

function Add-FileEvidence {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Label,
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $true)]
		[string]$Root,
		[Parameter(Mandatory = $false)]
		[switch]$Hash
	)

	if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
		Add-CheckResult -Passed $false -Message ("Missing {0}: {1}" -f $Label, (Convert-ToRepoRelativePath -Path $Path -Root $Root))
		return
	}

	$item = Get-Item -LiteralPath $Path
	$relativePath = Convert-ToRepoRelativePath -Path $item.FullName -Root $Root
	Add-CheckResult -Passed $true -Message ("{0}: {1}" -f $Label, $relativePath)
	$script:EvidenceLines.Add(("{0}`t{1}`t{2}" -f $Label, $relativePath, $item.Length))

	if ($Hash) {
		$fileHash = Get-FileHash -LiteralPath $item.FullName -Algorithm SHA256
		$script:EvidenceLines.Add(("{0} SHA256`t{1}`t{2}" -f $Label, $relativePath, $fileHash.Hash))
	}
}

function Test-BuildLogForForbiddenReferences {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path,
		[Parameter(Mandatory = $true)]
		[string]$ExpectedWixVersion
	)

	if (!(Test-Path -LiteralPath $Path -PathType Leaf)) {
		Add-CheckResult -Passed $false -Message ("Build log not found: {0}" -f $Path)
		return
	}

	Add-CheckResult -Passed $true -Message ("Build log found: {0}" -f (Convert-ToRepoRelativePath -Path $Path -Root $RepoRoot))

	$lines = Get-Content -LiteralPath $Path
	$legacyToolPattern = [regex]'(?i)(^|[\\/\s"''])(candle|light|insignia)\.exe(["''\s]|$)'
	$legacyPathPattern = [regex]'(?i)\b(PatchableInstaller|genericinstaller)\b'
	$heatPattern = [regex]'(?i)\bheat\.exe\b'
	$heatDiagnosticPattern = [regex]'(?i)^\s*heat\.exe\s*:\s*(warning|error)\s+HEAT\d+:'
	$expectedHeatPatternText = ('(?i)(packages|\.nuget[\\/]packages)[\\/]wixtoolset\.heat[\\/]{0}[\\/]tools[\\/]net472[\\/]x64[\\/]heat\.exe' -f [regex]::Escape($ExpectedWixVersion))
	$expectedHeatPattern = [regex]$expectedHeatPatternText
	$heatLines = New-Object 'System.Collections.Generic.List[string]'

	for ($i = 0; $i -lt $lines.Count; $i++) {
		$line = $lines[$i]
		$lineNumber = $i + 1

		if ($legacyPathPattern.IsMatch($line)) {
			Add-CheckResult -Passed $false -Message ("Build log references legacy generic installer path at line {0}: {1}" -f $lineNumber, $line.Trim())
		}

		if ($legacyToolPattern.IsMatch($line)) {
			Add-CheckResult -Passed $false -Message ("Build log references legacy WiX 3 tool at line {0}: {1}" -f $lineNumber, $line.Trim())
		}

		if ($heatPattern.IsMatch($line)) {
			if ($heatDiagnosticPattern.IsMatch($line)) {
				continue
			}

			$heatLines.Add($line.Trim())
			if (-not $expectedHeatPattern.IsMatch($line)) {
				Add-CheckResult -Passed $false -Message ("Heat invocation is not from WixToolset.Heat {0} at line {1}: {2}" -f $ExpectedWixVersion, $lineNumber, $line.Trim())
			}
		}
	}

	if ($heatLines.Count -eq 0) {
		Add-WarningResult -Message 'No heat.exe invocation was found in the build log. This can happen on incremental builds, but clean CI should show WixToolset.Heat usage.'
	} else {
		Add-CheckResult -Passed $true -Message ("Build log contains {0} WixToolset.Heat invocation/reference line(s)." -f $heatLines.Count)
	}
}

function Test-Wix3ToolsAbsentFromPath {
	$toolNames = @('candle.exe', 'light.exe', 'insignia.exe')
	$foundTools = New-Object 'System.Collections.Generic.List[string]'

	foreach ($toolName in $toolNames) {
		$commands = Get-Command -Name $toolName -CommandType Application -ErrorAction SilentlyContinue
		foreach ($command in $commands) {
			$foundTools.Add(('{0}: {1}' -f $toolName, $command.Source))
		}
	}

	if ($foundTools.Count -eq 0) {
		Add-CheckResult -Passed $true -Message 'No WiX 3 command-line tools are available on PATH.'
		return
	}

	foreach ($foundTool in $foundTools) {
		Add-CheckResult -Passed $false -Message ('WiX 3 command-line tool is available on PATH: {0}' -f $foundTool)
	}
}

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
	$RepoRoot = Resolve-DefaultRepoRoot
}

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
	$ReportPath = Join-Path $RepoRoot 'Output\InstallerEvidence\wix6-installer-build-evidence.txt'
}

$reportDir = Split-Path -Parent $ReportPath
New-DirectoryIfMissing -Path $reportDir

$script:Failures = New-Object 'System.Collections.Generic.List[string]'
$script:Warnings = New-Object 'System.Collections.Generic.List[string]'
$script:ReportLines = New-Object 'System.Collections.Generic.List[string]'
$script:EvidenceLines = New-Object 'System.Collections.Generic.List[string]'

$script:ReportLines.Add('WiX 6 installer build evidence')
$script:ReportLines.Add(('Started: {0:o}' -f (Get-Date)))
$script:ReportLines.Add(('RepoRoot: {0}' -f $RepoRoot))
$script:ReportLines.Add(('Configuration: {0}' -f $Configuration))
$script:ReportLines.Add(('Platform: {0}' -f $Platform))
$script:ReportLines.Add(('Expected WixToolset version: {0}' -f $WixVersion))
$script:ReportLines.Add(('Require no WiX 3 tools on PATH: {0}' -f [bool]$RequireNoWix3ToolsOnPath))
$script:ReportLines.Add('')

$patchableInstallerPath = Join-Path $RepoRoot 'PatchableInstaller'
if ($AllowPatchableInstallerDirectory) {
	Add-WarningResult -Message 'PatchableInstaller directory check is disabled for this run.'
} else {
	Add-CheckResult -Passed (!(Test-Path -LiteralPath $patchableInstallerPath)) -Message 'PatchableInstaller directory is absent from the WiX 6 build worktree.'
}

if ($RequireNoWix3ToolsOnPath) {
	Test-Wix3ToolsAbsentFromPath
}

$wixOutputDir = Join-Path $RepoRoot ('FLExInstaller\wix6\bin\{0}\{1}' -f $Platform, $Configuration)
$wixCultureDir = Join-Path $wixOutputDir 'en-US'

$expectedArtifacts = @(
	@{ Label = 'MSI'; Path = Join-Path $wixCultureDir 'FieldWorks.msi' },
	@{ Label = 'MSI wixpdb'; Path = Join-Path $wixCultureDir 'FieldWorks.wixpdb' },
	@{ Label = 'Online bundle'; Path = Join-Path $wixOutputDir 'FieldWorksBundle.exe' },
	@{ Label = 'Online bundle wixpdb'; Path = Join-Path $wixOutputDir 'FieldWorksBundle.wixpdb' },
	@{ Label = 'Offline bundle'; Path = Join-Path $wixOutputDir 'FieldWorksOfflineBundle.exe' },
	@{ Label = 'Offline bundle wixpdb'; Path = Join-Path $wixOutputDir 'FieldWorksOfflineBundle.wixpdb' }
)

foreach ($artifact in $expectedArtifacts) {
	Add-FileEvidence -Label $artifact.Label -Path $artifact.Path -Root $RepoRoot -Hash
}

$offlineBundlePath = Join-Path $wixOutputDir 'FieldWorksOfflineBundle.exe'
if ((Test-Path -LiteralPath $offlineBundlePath -PathType Leaf) -and $MinimumOfflineBundleBytes -gt 0) {
	$offlineBundle = Get-Item -LiteralPath $offlineBundlePath
	Add-CheckResult -Passed ($offlineBundle.Length -ge $MinimumOfflineBundleBytes) -Message ("Offline bundle is at least {0} bytes (actual: {1})." -f $MinimumOfflineBundleBytes, $offlineBundle.Length)
}

$offlineSourceDir = Join-Path $wixCultureDir 'SourceDir'
$offlinePayloads = @(
	'ndp48-x86-x64-allos-enu.exe',
	('vcredist_2008_{0}.exe' -f $Platform),
	('vcredist_2010_{0}.exe' -f $Platform),
	('vcredist_2012_{0}.exe' -f $Platform),
	('vcredist_2013_{0}.exe' -f $Platform),
	('vcredist_2015-19_{0}.exe' -f $Platform)
)

foreach ($payload in $offlinePayloads) {
	Add-FileEvidence -Label 'Offline prerequisite payload' -Path (Join-Path $offlineSourceDir $payload) -Root $RepoRoot
}

Add-FileEvidence -Label 'Offline FLEx Bridge payload' -Path (Join-Path $RepoRoot 'FLExInstaller\wix6\libs\FLExBridge_Offline.exe') -Root $RepoRoot

if (-not $SkipBuildLogAudit) {
	if ([string]::IsNullOrWhiteSpace($BuildLogPath)) {
		Add-WarningResult -Message 'No build log was supplied; skipping legacy tool/path log audit.'
	} else {
		$resolvedBuildLog = $BuildLogPath
		if (-not [System.IO.Path]::IsPathRooted($resolvedBuildLog)) {
			$resolvedBuildLog = Join-Path $RepoRoot $resolvedBuildLog
		}

		Test-BuildLogForForbiddenReferences -Path $resolvedBuildLog -ExpectedWixVersion $WixVersion
	}
}

$script:ReportLines.Add('')
$script:ReportLines.Add('File evidence')
$script:ReportLines.Add("Label`tPath`tValue")
$script:ReportLines.AddRange($script:EvidenceLines)
$script:ReportLines.Add('')
$script:ReportLines.Add(('Warnings: {0}' -f $script:Warnings.Count))
$script:ReportLines.Add(('Failures: {0}' -f $script:Failures.Count))
$script:ReportLines.Add(('Finished: {0:o}' -f (Get-Date)))

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($ReportPath, $script:ReportLines, $utf8NoBom)

Write-Output "WiX 6 build evidence report: $ReportPath"
foreach ($line in $script:ReportLines) {
	Write-Output $line
}

if ($script:Failures.Count -gt 0) {
	exit 1
}

exit 0