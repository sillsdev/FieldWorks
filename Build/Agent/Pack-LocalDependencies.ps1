[CmdletBinding()]
param(
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Debug',
	[switch]$LocalPalaso,
	[switch]$LocalLcm,
	[switch]$LocalChorus,
	[string]$LocalPackageVersion = '99.0.0-local'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$helpersPath = Join-Path $PSScriptRoot 'FwBuildHelpers.psm1'
if (Test-Path -LiteralPath $helpersPath) {
	Import-Module $helpersPath -Force
}

$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$feedDir = Join-Path $repoRoot 'Output\LocalNuGetFeed'
$overridePath = Join-Path $repoRoot 'Build\SilVersions.Local.props'
$nugetSources = @(
	$feedDir,
	'https://api.nuget.org/v3/index.json'
)

$selectedDependencies = @()
if ($LocalPalaso) {
	$selectedDependencies += 'Palaso'
}
if ($LocalLcm) {
	$selectedDependencies += 'Lcm'
}
if ($LocalChorus) {
	$selectedDependencies += 'Chorus'
}

if ($selectedDependencies.Count -eq 0) {
	if (Test-Path $overridePath) {
		Remove-Item -LiteralPath $overridePath -Force
		Write-Output 'Removed Build\SilVersions.Local.props; using pinned package versions.'
	}
	return
}

$repoEnvVarByDependency = @{
	Palaso = 'FW_LOCAL_PALASO'
	Lcm = 'FW_LOCAL_LCM'
	Chorus = 'FW_LOCAL_CHORUS'
}

$projectPathsByDependency = @{
	Palaso = @(
		'SIL.Archiving\SIL.Archiving.csproj',
		'SIL.Core\SIL.Core.csproj',
		'SIL.Core.Desktop\SIL.Core.Desktop.csproj',
		'SIL.Lexicon\SIL.Lexicon.csproj',
		'SIL.Lift\SIL.Lift.csproj',
		'SIL.Media\SIL.Media.csproj',
		'SIL.Scripture\SIL.Scripture.csproj',
		'SIL.TestUtilities\SIL.TestUtilities.csproj',
		'SIL.Windows.Forms\SIL.Windows.Forms.csproj',
		'SIL.Windows.Forms.Archiving\SIL.Windows.Forms.Archiving.csproj',
		'SIL.Windows.Forms.GeckoBrowserAdapter\SIL.Windows.Forms.GeckoBrowserAdapter.csproj',
		'SIL.Windows.Forms.Keyboarding\SIL.Windows.Forms.Keyboarding.csproj',
		'SIL.Windows.Forms.WritingSystems\SIL.Windows.Forms.WritingSystems.csproj',
		'SIL.WritingSystems\SIL.WritingSystems.csproj'
	)
	Lcm = @(
		'src\CSTools\Tools\Tools.csproj',
		'src\SIL.LCModel\SIL.LCModel.csproj',
		'src\SIL.LCModel.Build.Tasks\SIL.LCModel.Build.Tasks.csproj',
		'src\SIL.LCModel.Core\SIL.LCModel.Core.csproj',
		'src\SIL.LCModel.FixData\SIL.LCModel.FixData.csproj',
		'src\SIL.LCModel.Utils\SIL.LCModel.Utils.csproj',
		'tests\SIL.LCModel.Core.Tests\SIL.LCModel.Core.Tests.csproj',
		'tests\SIL.LCModel.Tests\SIL.LCModel.Tests.csproj',
		'tests\SIL.LCModel.Utils.Tests\SIL.LCModel.Utils.Tests.csproj'
	)
	Chorus = @(
		'src\Chorus\Chorus.csproj',
		'src\LibChorus\LibChorus.csproj'
	)
}

$switchNameByDependency = @{
	Palaso = 'LocalPalaso'
	Lcm = 'LocalLcm'
	Chorus = 'LocalChorus'
}

$packageIdsByDependency = @{
	Palaso = @(
		'sil.archiving',
		'sil.core',
		'sil.core.desktop',
		'sil.lexicon',
		'sil.lift',
		'sil.media',
		'sil.scripture',
		'sil.testutilities',
		'sil.windows.forms',
		'sil.windows.forms.archiving',
		'sil.windows.forms.geckobrowseradapter',
		'sil.windows.forms.keyboarding',
		'sil.windows.forms.writingsystems',
		'sil.writingsystems'
	)
	Lcm = @(
		'sil.lcmodel.tools',
		'sil.lcmodel',
		'sil.lcmodel.build.tasks',
		'sil.lcmodel.core',
		'sil.lcmodel.fixdata',
		'sil.lcmodel.utils',
		'sil.lcmodel.core.tests',
		'sil.lcmodel.tests',
		'sil.lcmodel.utils.tests'
	)
	Chorus = @(
		'sil.chorus.app',
		'sil.chorus.libchorus'
	)
}

$projectsWithoutSymbolPackages = @(
	'src\CSTools\Tools\Tools.csproj',
	'src\SIL.LCModel.Build.Tasks\SIL.LCModel.Build.Tasks.csproj',
	'tests\SIL.LCModel.Core.Tests\SIL.LCModel.Core.Tests.csproj',
	'tests\SIL.LCModel.Tests\SIL.LCModel.Tests.csproj',
	'tests\SIL.LCModel.Utils.Tests\SIL.LCModel.Utils.Tests.csproj'
)

function Get-LocalDependencyStampDirectory {
	return (Join-Path $feedDir '.stamp')
}

function Get-LocalDependencyStampPath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName
	)

	return (Join-Path (Get-LocalDependencyStampDirectory) ("{0}.json" -f $DependencyName))
}

function Get-StringHash {
	param(
		[AllowEmptyString()]
		[string]$Value
	)

	$sha256 = [System.Security.Cryptography.SHA256]::Create()
	try {
		$bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
		$hashBytes = $sha256.ComputeHash($bytes)
		return ([System.BitConverter]::ToString($hashBytes).Replace('-', '').ToLowerInvariant())
	}
	finally {
		$sha256.Dispose()
	}
}

function Get-DependencyRepoFingerprint {
	param(
		[Parameter(Mandatory = $true)]
		[string]$RepoPath
	)

	$resolvedRepoPath = [System.IO.Path]::GetFullPath($RepoPath)
	$gitHeadOutput = @(& git -c core.safecrlf=false -c core.autocrlf=false -C $resolvedRepoPath rev-parse HEAD 2>$null)
	if ($LASTEXITCODE -ne 0 -or $gitHeadOutput.Count -eq 0) {
		throw "Could not determine git HEAD for local dependency repo '$resolvedRepoPath'."
	}

	$gitHead = ($gitHeadOutput -join "`n").Trim()
	$statusLines = @(& git -c core.safecrlf=false -c core.autocrlf=false -C $resolvedRepoPath status --porcelain=v1 --untracked-files=all 2>$null)
	if ($LASTEXITCODE -ne 0) {
		throw "Could not determine git status for local dependency repo '$resolvedRepoPath'."
	}

	if ($statusLines.Count -eq 0) {
		return [pscustomobject]@{
			RepoPath = $resolvedRepoPath
			GitHead = $gitHead
			IsDirty = $false
			Fingerprint = "clean:$gitHead"
		}
	}

	$diffText = (@(& git -c core.safecrlf=false -c core.autocrlf=false -C $resolvedRepoPath diff --no-ext-diff --binary HEAD -- . 2>$null) -join "`n")
	if ($LASTEXITCODE -ne 0) {
		throw "Could not determine git diff for local dependency repo '$resolvedRepoPath'."
	}

	$untrackedFileDescriptors = foreach ($statusLine in $statusLines | Where-Object { $_.StartsWith('?? ') }) {
		if ($statusLine.Length -lt 4) {
			continue
		}

		$relativePath = $statusLine.Substring(3)
		$fullPath = Join-Path $resolvedRepoPath $relativePath
		if (Test-Path -LiteralPath $fullPath -PathType Leaf) {
			$fileInfo = Get-Item -LiteralPath $fullPath
			$fileHash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
			"$relativePath|$($fileInfo.Length)|$($fileInfo.LastWriteTimeUtc.Ticks)|$fileHash"
		}
		else {
			"$relativePath|missing"
		}
	}

	$fingerprintSource = @(
		$gitHead,
		($statusLines -join "`n"),
		$diffText,
		($untrackedFileDescriptors -join "`n")
	) -join "`n---`n"

	return [pscustomobject]@{
		RepoPath = $resolvedRepoPath
		GitHead = $gitHead
		IsDirty = $true
		Fingerprint = "dirty:$(Get-StringHash -Value $fingerprintSource)"
	}
}

function Read-DependencyStamp {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName
	)

	$stampPath = Get-LocalDependencyStampPath -DependencyName $DependencyName
	if (-not (Test-Path -LiteralPath $stampPath -PathType Leaf)) {
		return $null
	}

	return (Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json)
}

function Test-DependencyFeedArtifactsExist {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName,
		[Parameter(Mandatory = $true)]
		[string]$PackageVersion
	)

	foreach ($packageId in $packageIdsByDependency[$DependencyName]) {
		$packageArtifact = Get-ChildItem -LiteralPath $feedDir -Filter "$packageId.$PackageVersion.nupkg" -File -ErrorAction SilentlyContinue |
			Where-Object { $_.Name -notlike '*.snupkg' } |
			Select-Object -First 1

		if ($null -eq $packageArtifact) {
			return $false
		}
	}

	return $true
}

function Write-DependencyStamp {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName,
		[Parameter(Mandatory = $true)]
		[string]$RepoPath,
		[Parameter(Mandatory = $true)]
		[pscustomobject]$FingerprintInfo,
		[Parameter(Mandatory = $true)]
		[string]$PackageVersion
	)

	$stampDir = Get-LocalDependencyStampDirectory
	New-Item -Path $stampDir -ItemType Directory -Force | Out-Null

	$stampObject = [pscustomobject]@{
		DependencyName = $DependencyName
		RepoPath = [System.IO.Path]::GetFullPath($RepoPath)
		Configuration = $Configuration
		RequestedLocalPackageVersion = $LocalPackageVersion
		PackageVersion = $PackageVersion
		GitHead = $FingerprintInfo.GitHead
		IsDirty = $FingerprintInfo.IsDirty
		Fingerprint = $FingerprintInfo.Fingerprint
		PackageIds = @($packageIdsByDependency[$DependencyName])
	}

	$stampPath = Get-LocalDependencyStampPath -DependencyName $DependencyName
	$stampObject | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $stampPath -Encoding UTF8
}

function Get-DependencyReuseState {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName,
		[Parameter(Mandatory = $true)]
		[string]$RepoPath
	)

	$fingerprintInfo = Get-DependencyRepoFingerprint -RepoPath $RepoPath
	$stamp = Read-DependencyStamp -DependencyName $DependencyName
	if ($null -eq $stamp) {
		return [pscustomobject]@{
			DependencyName = $DependencyName
			CanReuse = $false
			Reason = 'no stamp'
			FingerprintInfo = $fingerprintInfo
			PackageVersion = ''
		}
	}

	$resolvedRepoPath = [System.IO.Path]::GetFullPath($RepoPath)
	if ($stamp.RepoPath -ne $resolvedRepoPath) {
		return [pscustomobject]@{
			DependencyName = $DependencyName
			CanReuse = $false
			Reason = 'repo path changed'
			FingerprintInfo = $fingerprintInfo
			PackageVersion = ''
		}
	}

	if ($stamp.Configuration -ne $Configuration) {
		return [pscustomobject]@{
			DependencyName = $DependencyName
			CanReuse = $false
			Reason = 'configuration changed'
			FingerprintInfo = $fingerprintInfo
			PackageVersion = ''
		}
	}

	if ($stamp.RequestedLocalPackageVersion -ne $LocalPackageVersion) {
		return [pscustomobject]@{
			DependencyName = $DependencyName
			CanReuse = $false
			Reason = 'requested package version changed'
			FingerprintInfo = $fingerprintInfo
			PackageVersion = ''
		}
		}

	if ($stamp.Fingerprint -ne $fingerprintInfo.Fingerprint) {
		return [pscustomobject]@{
			DependencyName = $DependencyName
			CanReuse = $false
			Reason = 'repo contents changed'
			FingerprintInfo = $fingerprintInfo
			PackageVersion = ''
		}
	}

	$packageVersion = [string]$stamp.PackageVersion
	if ([string]::IsNullOrWhiteSpace($packageVersion)) {
		return [pscustomobject]@{
			DependencyName = $DependencyName
			CanReuse = $false
			Reason = 'stamp missing package version'
			FingerprintInfo = $fingerprintInfo
			PackageVersion = ''
		}
	}

	if (-not (Test-DependencyFeedArtifactsExist -DependencyName $DependencyName -PackageVersion $packageVersion)) {
		return [pscustomobject]@{
			DependencyName = $DependencyName
			CanReuse = $false
			Reason = 'feed artifacts missing'
			FingerprintInfo = $fingerprintInfo
			PackageVersion = ''
		}
	}

	return [pscustomobject]@{
		DependencyName = $DependencyName
		CanReuse = $true
		Reason = 'stamp matched'
		FingerprintInfo = $fingerprintInfo
		PackageVersion = $packageVersion
	}
}

function Get-DependencyRepoPath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName
	)

	$envVarName = $repoEnvVarByDependency[$DependencyName]
	$repoPath = [Environment]::GetEnvironmentVariable($envVarName)
	if ([string]::IsNullOrWhiteSpace($repoPath)) {
		throw "-$($switchNameByDependency[$DependencyName]) requires environment variable $envVarName to point to the $DependencyName repo checkout."
	}

	if (-not (Test-Path -LiteralPath $repoPath -PathType Container)) {
		throw "$envVarName points to '$repoPath', but that directory does not exist."
	}

	foreach ($relativeProjectPath in $projectPathsByDependency[$DependencyName]) {
		$projectPath = Join-Path $repoPath $relativeProjectPath
		if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) {
			throw "$envVarName points to '$repoPath', but required project '$relativeProjectPath' was not found there."
		}
	}

	return $repoPath
}

function Remove-PackageCacheDirectory {
	param(
		[Parameter(Mandatory = $true)]
		[string]$PackageDir
	)

	for ($attempt = 1; $attempt -le 2; $attempt++) {
		try {
			Remove-Item -LiteralPath $PackageDir -Recurse -Force
			break
		}
		catch {
			$fileLockDetected = (Get-Command Test-IsFileLockError -ErrorAction SilentlyContinue) -and (Test-IsFileLockError -ErrorRecord $_)
			if ($attempt -lt 2 -and $fileLockDetected) {
				Write-Warning "Package cache cleanup hit a file lock for '$PackageDir'. Stopping stale build processes and retrying."
				if (Get-Command Stop-ConflictingProcesses -ErrorAction SilentlyContinue) {
					Stop-ConflictingProcesses -IncludeOmniSharp -RepoRoot $repoRoot
				}
				Start-Sleep -Seconds 2
				continue
			}

			if ($fileLockDetected) {
				Write-Warning "Package cache cleanup could not remove '$PackageDir' because files are still locked. Continuing with the existing extracted package cache."
				break
			}

			throw
		}
	}
}

function Test-IsPathAlreadyGoneError {
	param(
		[Parameter(Mandatory = $true)]
		[System.Management.Automation.ErrorRecord]$ErrorRecord
	)

	if ($ErrorRecord.Exception -is [System.IO.FileNotFoundException] -or
		$ErrorRecord.Exception -is [System.IO.DirectoryNotFoundException] -or
		$ErrorRecord.Exception -is [System.Management.Automation.ItemNotFoundException]) {
		return $true
	}

	$message = $ErrorRecord.Exception.Message
	return ($message -like 'Could not find file*' -or $message -like 'Could not find a part of the path*' -or $message -like 'Cannot find path*')
}

function Get-LocalNuGetCacheRoot {
	return (Join-Path $repoRoot 'Output\LocalNuGetCache')
}

function Test-IsDependencyTempCacheDirectory {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DirectoryName,
		[Parameter(Mandatory = $true)]
		[string[]]$Dependencies
	)

	foreach ($dependency in $Dependencies) {
		if ($DirectoryName -match ("^{0}(?:-l10n)?-[0-9a-f]{{32}}$" -f [regex]::Escape($dependency))) {
			return $true
		}
	}

	return $false
}

function Remove-TempNuGetCacheDirectory {
	param(
		[Parameter(Mandatory = $true)]
		[string]$CacheDir,
		[switch]$SkipIfLocked
	)

	if (-not (Test-Path -LiteralPath $CacheDir -PathType Container)) {
		return
	}

	for ($attempt = 1; $attempt -le 3; $attempt++) {
		try {
			Remove-Item -LiteralPath $CacheDir -Recurse -Force
			return
		}
		catch {
			if (Test-IsPathAlreadyGoneError -ErrorRecord $_) {
				if (-not (Test-Path -LiteralPath $CacheDir -PathType Container)) {
					return
				}

				if ($attempt -lt 3) {
					Start-Sleep -Milliseconds 500
					continue
				}

				return
			}

			$fileLockDetected = (Get-Command Test-IsFileLockError -ErrorAction SilentlyContinue) -and (Test-IsFileLockError -ErrorRecord $_)
			if ($SkipIfLocked -and $fileLockDetected) {
				Write-Output "Skipping in-use temp NuGet cache '$CacheDir'."
				return
			}

			if ($attempt -lt 3 -and $fileLockDetected) {
				Start-Sleep -Seconds 2
				continue
			}

			throw
		}
	}
}


function Clear-LocalNuGetCacheRoot {
	param(
		[switch]$SkipIfLocked
	)

	$cacheRoot = Get-LocalNuGetCacheRoot
	if (-not (Test-Path -LiteralPath $cacheRoot -PathType Container)) {
		return
	}

	Remove-TempNuGetCacheDirectory -CacheDir $cacheRoot -SkipIfLocked:$SkipIfLocked
}

function Clear-FieldWorksPackageCache {
	param(
		[Parameter(Mandatory = $true)]
		[string[]]$Dependencies
	)

	$packagesRoot = Join-Path $repoRoot 'packages'
	if (-not (Test-Path -LiteralPath $packagesRoot -PathType Container)) {
		return
	}

	foreach ($dependency in $Dependencies) {
		foreach ($packageId in $packageIdsByDependency[$dependency]) {
			$packageDir = Join-Path $packagesRoot $packageId
			if (Test-Path -LiteralPath $packageDir) {
				Remove-PackageCacheDirectory -PackageDir $packageDir
			}
		}
	}
}

function Write-LocalOverrideFile {
	param(
		[Parameter(Mandatory = $true)]
		[hashtable]$DependencyVersions,
		[string]$L10NSharpVersion
	)

	$overrideLines = @(
		'<Project>',
		'  <PropertyGroup Label="Local SIL dependency overrides">'
	)

	if ($LocalPalaso) {
		$overrideLines += "    <SilLibPalasoVersion>$($DependencyVersions['Palaso'])</SilLibPalasoVersion>"
		if (-not [string]::IsNullOrWhiteSpace($L10NSharpVersion)) {
			$overrideLines += "    <L10NSharpVersion>$L10NSharpVersion</L10NSharpVersion>"
		}
	}

	if ($LocalLcm) {
		$overrideLines += "    <SilLcmVersion>$($DependencyVersions['Lcm'])</SilLcmVersion>"
	}

	if ($LocalChorus) {
		$overrideLines += "    <SilChorusVersion>$($DependencyVersions['Chorus'])</SilChorusVersion>"
	}

	$overrideLines += @(
		'  </PropertyGroup>',
		'</Project>'
	)

	Set-Content -LiteralPath $overridePath -Value $overrideLines -Encoding UTF8
	Write-Output "Wrote local dependency version overrides to $overridePath."
}

function Sync-FieldWorksPackageCache {
	param(
		[Parameter(Mandatory = $true)]
		[hashtable]$DependencyVersions,
		[Parameter(Mandatory = $true)]
		[string[]]$Dependencies
	)

	$packagesRoot = Join-Path $repoRoot 'packages'
	New-Item -Path $packagesRoot -ItemType Directory -Force | Out-Null
	Add-Type -AssemblyName System.IO.Compression.FileSystem

	foreach ($dependency in $Dependencies) {
		$packageVersion = $DependencyVersions[$dependency]
		foreach ($packageId in $packageIdsByDependency[$dependency]) {
			$packageArtifact = Get-ChildItem -LiteralPath $feedDir -Filter "$packageId.$packageVersion.nupkg" -File -ErrorAction SilentlyContinue |
				Where-Object { $_.Name -notlike '*.snupkg' } |
				Select-Object -First 1

			if ($null -eq $packageArtifact) {
				continue
			}

			$packageDir = Join-Path (Join-Path $packagesRoot $packageId) $packageVersion
			New-Item -Path $packageDir -ItemType Directory -Force | Out-Null

			$zipArchive = [System.IO.Compression.ZipFile]::OpenRead($packageArtifact.FullName)
			try {
				foreach ($entry in $zipArchive.Entries) {
					if ([string]::IsNullOrWhiteSpace($entry.FullName)) {
						continue
					}

					$destinationPath = Join-Path $packageDir ($entry.FullName -replace '/', '\\')
					if ($entry.FullName.EndsWith('/')) {
						New-Item -Path $destinationPath -ItemType Directory -Force | Out-Null
						continue
					}

					$destinationDir = Split-Path $destinationPath -Parent
					if (-not (Test-Path -LiteralPath $destinationDir)) {
						New-Item -Path $destinationDir -ItemType Directory -Force | Out-Null
					}

					$shouldCopy = $true
					if (Test-Path -LiteralPath $destinationPath -PathType Leaf) {
						$existingFile = Get-Item -LiteralPath $destinationPath
						$entryTimestampUtc = $entry.LastWriteTime.UtcDateTime
						$existingTimestampUtc = $existingFile.LastWriteTimeUtc
						$shouldCopy = ($existingFile.Length -ne $entry.Length) -or ($existingTimestampUtc -ne $entryTimestampUtc)
					}

					if (-not $shouldCopy) {
						continue
					}

					try {
						$entryStream = $entry.Open()
						try {
							$fileStream = [System.IO.File]::Open($destinationPath, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write, [System.IO.FileShare]::Read)
							try {
								$entryStream.CopyTo($fileStream)
							}
							finally {
								$fileStream.Dispose()
							}
						}
						finally {
							$entryStream.Dispose()
						}

						[System.IO.File]::SetLastWriteTimeUtc($destinationPath, $entry.LastWriteTime.UtcDateTime)
					}
					catch {
						Write-Warning "Could not refresh extracted package file '$destinationPath'. Continuing with the existing file."
					}
				}
			}
			finally {
				$zipArchive.Dispose()
			}
		}
	}
}

function Get-PackageReferenceVersion {
	param(
		[Parameter(Mandatory = $true)]
		[string]$ProjectPath,
		[Parameter(Mandatory = $true)]
		[string]$PackageId
	)

	[xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
	$packageReference = Select-Xml -Xml $projectXml -XPath "//*[local-name()='PackageReference' and @Include='$PackageId']" |
		Select-Object -First 1

	if ($null -eq $packageReference) {
		throw "PackageReference '$PackageId' was not found in $ProjectPath."
	}

	return $packageReference.Node.Version
}

function Get-ProjectPackageId {
	param(
		[Parameter(Mandatory = $true)]
		[string]$ProjectPath
	)

	[xml]$projectXml = Get-Content -LiteralPath $ProjectPath -Raw
	$propertyGroups = @($projectXml.Project.PropertyGroup)

	foreach ($propertyGroup in $propertyGroups) {
		if (($propertyGroup.PSObject.Properties.Name -contains 'PackageId') -and -not [string]::IsNullOrWhiteSpace($propertyGroup.PackageId)) {
			return $propertyGroup.PackageId
		}
	}

	foreach ($propertyGroup in $propertyGroups) {
		if (($propertyGroup.PSObject.Properties.Name -contains 'AssemblyName') -and -not [string]::IsNullOrWhiteSpace($propertyGroup.AssemblyName)) {
			return $propertyGroup.AssemblyName
		}
	}

	return (Split-Path $ProjectPath -LeafBase)
}

function Clear-LocalFeedPackages {
	param(
		[Parameter(Mandatory = $true)]
		[string[]]$Dependencies
	)

	if (-not (Test-Path -LiteralPath $feedDir -PathType Container)) {
		return
	}

	foreach ($dependency in $Dependencies) {
		foreach ($packageId in $packageIdsByDependency[$dependency]) {
			Get-ChildItem -LiteralPath $feedDir -File -ErrorAction SilentlyContinue |
				Where-Object { $_.Name -like "$packageId.*.nupkg" -or $_.Name -like "$packageId.*.snupkg" } |
				Remove-Item -Force
		}
	}
}

function Get-DependencyPackageVersion {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName
	)

	$versions = New-Object System.Collections.Generic.HashSet[string]([System.StringComparer]::OrdinalIgnoreCase)

	foreach ($packageId in $packageIdsByDependency[$DependencyName]) {
		$packageFile = Get-ChildItem -LiteralPath $feedDir -Filter "$packageId.*.nupkg" -File -ErrorAction SilentlyContinue |
			Where-Object { $_.Name -notlike '*.snupkg' } |
			Select-Object -First 1

		if ($null -eq $packageFile) {
			throw "Packed package '$packageId' was not found in $feedDir."
		}

		$pattern = '^{0}\.(.+)\.nupkg$' -f [regex]::Escape($packageId)
		$match = [regex]::Match($packageFile.Name, $pattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
		if (-not $match.Success) {
			throw "Could not determine the package version from '$($packageFile.Name)'."
		}

		[void]$versions.Add($match.Groups[1].Value)
	}

	$resolvedVersions = @($versions)
	if ($resolvedVersions.Count -ne 1) {
		throw "Expected a single package version for $DependencyName, but found: $($resolvedVersions -join ', ')."
	}

	return $resolvedVersions[0]
}

function New-TempNuGetConfig {
	param(
		[Parameter(Mandatory = $true)]
		[string]$ConfigPath,
		[Parameter(Mandatory = $true)]
		[string[]]$Sources
	)

	$configLines = @(
		'<?xml version="1.0" encoding="utf-8"?>',
		'<configuration>',
		'  <packageSources>',
		'    <clear />'
	)

	for ($index = 0; $index -lt $Sources.Count; $index++) {
		$configLines += ('    <add key="source{0}" value="{1}" />' -f $index, $Sources[$index])
	}

	$configLines += @(
		'  </packageSources>',
		'</configuration>'
	)

	Set-Content -LiteralPath $ConfigPath -Value $configLines -Encoding UTF8
	return $ConfigPath
}

function New-DependencyCacheDir {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName
	)

	$cacheRoot = Get-LocalNuGetCacheRoot
	New-Item -Path $cacheRoot -ItemType Directory -Force | Out-Null

	$cacheDir = Join-Path $cacheRoot ("{0}-{1}" -f $DependencyName, [guid]::NewGuid().ToString('N'))
	New-Item -Path $cacheDir -ItemType Directory -Force | Out-Null
	return $cacheDir
}

function Test-PackedProjectArtifactExists {
	param(
		[Parameter(Mandatory = $true)]
		[string]$ProjectPath,
		[Parameter(Mandatory = $true)]
		[string]$PackageFeedDir
	)

	$packageIdPrefix = Get-ProjectPackageId -ProjectPath $ProjectPath
	$packageArtifact = Get-ChildItem -LiteralPath $PackageFeedDir -Filter "$packageIdPrefix.*.nupkg" -File -ErrorAction SilentlyContinue |
		Where-Object { $_.Name -notlike '*.snupkg' } |
		Select-Object -First 1

	return ($null -ne $packageArtifact)
}

function Invoke-Pack {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName,
		[Parameter(Mandatory = $true)]
		[string]$RepoPath
	)

	$cacheDir = New-DependencyCacheDir -DependencyName $DependencyName
	$nugetConfigPath = Join-Path $cacheDir 'nuget.config'

	Write-Output "Packing $DependencyName from $RepoPath"

	$previousNugetPackages = [Environment]::GetEnvironmentVariable('NUGET_PACKAGES')
	try {
		$env:NUGET_PACKAGES = $cacheDir
		New-TempNuGetConfig -ConfigPath $nugetConfigPath -Sources $nugetSources | Out-Null

		foreach ($relativeProjectPath in $projectPathsByDependency[$DependencyName]) {
			$projectPath = Join-Path $RepoPath $relativeProjectPath
			$restoreArgs = @(
				'restore',
				$projectPath,
				'--configfile',
				$nugetConfigPath,
				'--disable-build-servers',
				'--verbosity',
				'minimal'
			)

			& dotnet @restoreArgs
			if ($LASTEXITCODE -ne 0) {
				throw "dotnet restore failed for $DependencyName project $projectPath."
			}

			$packArgs = @(
				'pack',
				$projectPath,
				'-c',
				$Configuration,
				'--output',
				$feedDir,
				'--no-restore',
				'--disable-build-servers',
				'--verbosity',
				'minimal',
				"-p:PackageVersion=$LocalPackageVersion"
			)

			if ($projectsWithoutSymbolPackages -contains $relativeProjectPath) {
				$packArgs += '-p:DebugSymbols=false'
			}
			else {
				$packArgs += @(
					'-p:DebugType=embedded',
					'-p:DebugSymbols=true'
				)
			}

			& dotnet @packArgs
			if ($LASTEXITCODE -ne 0) {
				if (($projectsWithoutSymbolPackages -contains $relativeProjectPath) -and (Test-PackedProjectArtifactExists -ProjectPath $projectPath -PackageFeedDir $feedDir)) {
					Write-Warning "$DependencyName project $projectPath returned a non-zero exit code after creating its main package artifact. Continuing without a symbol package."
					continue
				}

				throw "dotnet pack failed for $DependencyName project $projectPath."
			}
		}
	}
	finally {
		if ([string]::IsNullOrWhiteSpace($previousNugetPackages)) {
			Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
		}
		else {
			$env:NUGET_PACKAGES = $previousNugetPackages
		}

		Remove-TempNuGetCacheDirectory -CacheDir $cacheDir
	}
}

function Start-PackJob {
	param(
		[Parameter(Mandatory = $true)]
		[string]$DependencyName,
		[Parameter(Mandatory = $true)]
		[string]$RepoPath
	)

	$cacheDir = New-DependencyCacheDir -DependencyName $DependencyName

	return Start-Job -Name $DependencyName -ScriptBlock {
		param($JobDependencyName, $JobRepoPath, $JobProjectPaths, $JobProjectsWithoutSymbolPackages, $JobConfiguration, $JobFeedDir, $JobLocalPackageVersion, $JobCacheDir, $JobSources)

		Set-StrictMode -Version Latest
		$ErrorActionPreference = 'Stop'

		function Remove-JobTempNuGetCacheDirectory {
			param(
				[Parameter(Mandatory = $true)]
				[string]$CacheDir
			)

			if (-not (Test-Path -LiteralPath $CacheDir -PathType Container)) {
				return
			}

			for ($attempt = 1; $attempt -le 3; $attempt++) {
				try {
					Remove-Item -LiteralPath $CacheDir -Recurse -Force
					return
				}
				catch {
					if ($attempt -lt 3) {
						Start-Sleep -Seconds 2
						continue
					}

					throw
				}
			}
		}

		$env:NUGET_PACKAGES = $JobCacheDir
		$jobNugetConfigPath = Join-Path $JobCacheDir 'nuget.config'
		$configLines = @(
			'<?xml version="1.0" encoding="utf-8"?>',
			'<configuration>',
			'  <packageSources>',
			'    <clear />'
		)

		for ($index = 0; $index -lt $JobSources.Count; $index++) {
			$configLines += ('    <add key="source{0}" value="{1}" />' -f $index, $JobSources[$index])
		}

		$configLines += @(
			'  </packageSources>',
			'</configuration>'
		)

		try {
			Set-Content -LiteralPath $jobNugetConfigPath -Value $configLines -Encoding UTF8
			foreach ($relativeProjectPath in $JobProjectPaths) {
				$projectPath = Join-Path $JobRepoPath $relativeProjectPath
				$restoreArgs = @(
					'restore',
					$projectPath,
					'--configfile',
					$jobNugetConfigPath,
					'--disable-build-servers',
					'--verbosity',
					'minimal'
				)

				& dotnet @restoreArgs
				if ($LASTEXITCODE -ne 0) {
					throw "dotnet restore failed for $JobDependencyName project $projectPath."
				}

				$packArgs = @(
					'pack',
					$projectPath,
					'-c',
					$JobConfiguration,
					'--output',
					$JobFeedDir,
					'--no-restore',
					'--disable-build-servers',
					'--verbosity',
					'minimal',
					"-p:PackageVersion=$JobLocalPackageVersion"
				)

				if ($JobProjectsWithoutSymbolPackages -contains $relativeProjectPath) {
					$packArgs += '-p:DebugSymbols=false'
				}
				else {
					$packArgs += @(
						'-p:DebugType=embedded',
						'-p:DebugSymbols=true'
					)
				}

				& dotnet @packArgs
				if ($LASTEXITCODE -ne 0) {
					if ($JobProjectsWithoutSymbolPackages -contains $relativeProjectPath) {
						[xml]$jobProjectXml = Get-Content -LiteralPath $projectPath -Raw
						$jobPropertyGroups = @($jobProjectXml.Project.PropertyGroup)
						$packageIdPrefix = $null
						foreach ($jobPropertyGroup in $jobPropertyGroups) {
							if (($jobPropertyGroup.PSObject.Properties.Name -contains 'PackageId') -and -not [string]::IsNullOrWhiteSpace($jobPropertyGroup.PackageId)) {
								$packageIdPrefix = $jobPropertyGroup.PackageId
								break
							}
						}
						if ([string]::IsNullOrWhiteSpace($packageIdPrefix)) {
							foreach ($jobPropertyGroup in $jobPropertyGroups) {
								if (($jobPropertyGroup.PSObject.Properties.Name -contains 'AssemblyName') -and -not [string]::IsNullOrWhiteSpace($jobPropertyGroup.AssemblyName)) {
									$packageIdPrefix = $jobPropertyGroup.AssemblyName
									break
								}
							}
						}
						if ([string]::IsNullOrWhiteSpace($packageIdPrefix)) {
							$packageIdPrefix = Split-Path $projectPath -LeafBase
						}
						$packageArtifact = Get-ChildItem -LiteralPath $JobFeedDir -Filter "$packageIdPrefix.*.nupkg" -File -ErrorAction SilentlyContinue |
							Where-Object { $_.Name -notlike '*.snupkg' } |
							Select-Object -First 1

						if ($null -ne $packageArtifact) {
							Write-Warning "$JobDependencyName project $projectPath returned a non-zero exit code after creating its main package artifact. Continuing without a symbol package."
							continue
						}
					}

					throw "dotnet pack failed for $JobDependencyName project $projectPath."
				}
			}
		}
		finally {
			if (Test-Path Env:NUGET_PACKAGES) {
				Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
			}

			Remove-JobTempNuGetCacheDirectory -CacheDir $JobCacheDir
		}
	} -ArgumentList $DependencyName, $RepoPath, $projectPathsByDependency[$DependencyName], $projectsWithoutSymbolPackages, $Configuration, $feedDir, $LocalPackageVersion, $cacheDir, $nugetSources
}

New-Item -Path $feedDir -ItemType Directory -Force | Out-Null
Clear-LocalNuGetCacheRoot

$repoPaths = @{}
foreach ($dependency in $selectedDependencies) {
	$repoPaths[$dependency] = Get-DependencyRepoPath -DependencyName $dependency
}

$dependencyReuseStates = @{}
$dependenciesToPack = New-Object System.Collections.Generic.List[string]
$dependenciesToSync = New-Object System.Collections.Generic.List[string]
$resolvedDependencyVersions = @{}

foreach ($dependency in $selectedDependencies) {
	$reuseState = Get-DependencyReuseState -DependencyName $dependency -RepoPath $repoPaths[$dependency]
	$dependencyReuseStates[$dependency] = $reuseState
	[void]$dependenciesToSync.Add($dependency)

	if ($reuseState.CanReuse) {
		$resolvedDependencyVersions[$dependency] = $reuseState.PackageVersion
		Write-Output "Reusing local $dependency packages from $feedDir ($($reuseState.PackageVersion); $($reuseState.FingerprintInfo.Fingerprint))."
	}
	else {
		[void]$dependenciesToPack.Add($dependency)
		Write-Output "Packing local $dependency packages because $($reuseState.Reason)."
	}
}

if ($dependenciesToPack.Count -gt 0) {
	Clear-LocalFeedPackages -Dependencies @($dependenciesToPack)
}

Write-Output "Using local dependency feed at $feedDir"
Write-Output "Selected local dependencies: $($selectedDependencies -join ', ')"

if ($LocalPalaso -and $dependenciesToPack.Contains('Palaso')) {
	Invoke-Pack -DependencyName 'Palaso' -RepoPath $repoPaths['Palaso']
}

$parallelJobs = @()
if ($LocalLcm -and $dependenciesToPack.Contains('Lcm')) {
	$parallelJobs += Start-PackJob -DependencyName 'Lcm' -RepoPath $repoPaths['Lcm']
}
if ($LocalChorus -and $dependenciesToPack.Contains('Chorus')) {
	$parallelJobs += Start-PackJob -DependencyName 'Chorus' -RepoPath $repoPaths['Chorus']
}

foreach ($job in $parallelJobs) {
	Wait-Job -Job $job | Out-Null
	try {
		Receive-Job -Job $job -ErrorAction Stop | Write-Output
	}
	finally {
		Remove-Job -Job $job -Force
	}
}
foreach ($dependency in $selectedDependencies) {
	if ($dependenciesToPack.Contains($dependency)) {
		$resolvedDependencyVersions[$dependency] = Get-DependencyPackageVersion -DependencyName $dependency
		Write-DependencyStamp -DependencyName $dependency -RepoPath $repoPaths[$dependency] -FingerprintInfo $dependencyReuseStates[$dependency].FingerprintInfo -PackageVersion $resolvedDependencyVersions[$dependency]
	}
}

$resolvedL10NSharpVersion = ''
if ($LocalPalaso) {
	$resolvedL10NSharpVersion = Get-PackageReferenceVersion -ProjectPath (Join-Path $repoPaths['Palaso'] 'SIL.Core.Desktop\SIL.Core.Desktop.csproj') -PackageId 'L10NSharp'
}

if ($dependenciesToPack.Count -gt 0) {
	Clear-FieldWorksPackageCache -Dependencies @($dependenciesToPack)
}
Sync-FieldWorksPackageCache -DependencyVersions $resolvedDependencyVersions -Dependencies @($dependenciesToSync)
Write-LocalOverrideFile -DependencyVersions $resolvedDependencyVersions -L10NSharpVersion $resolvedL10NSharpVersion

foreach ($dependency in $selectedDependencies) {
	$resolvedVersion = $resolvedDependencyVersions[$dependency]
	if ($dependencyReuseStates[$dependency].CanReuse) {
		Write-Output "$dependency packages were reused at version $resolvedVersion."
	}
	elseif ($resolvedVersion -ne $LocalPackageVersion) {
		Write-Warning "$dependency packages were requested at version $LocalPackageVersion, but the packed artifacts use version $resolvedVersion. Using the actual packed version in Build\\SilVersions.Local.props."
	}
	else {
		Write-Output "$dependency packages were packed at version $resolvedVersion."
	}
}

Clear-LocalNuGetCacheRoot
