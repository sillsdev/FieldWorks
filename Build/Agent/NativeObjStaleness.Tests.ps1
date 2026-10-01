<#
.SYNOPSIS
	Fixture tests for Remove-NativeObjDirsWithStaleInputs in FwBuildHelpers.psm1.

.DESCRIPTION
	Builds a throwaway repo layout with object files and headers at chosen timestamps
	and checks which module folders the helper removes. Run directly:
	pwsh -File Build/Agent/NativeObjStaleness.Tests.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'FwBuildHelpers.psm1') -Force

$failures = New-Object System.Collections.ArrayList
$old = [datetime]::new(2026, 9, 2, 12, 0, 0, [System.DateTimeKind]::Utc)
$new = $old.AddDays(28)
$modules = @([pscustomobject]@{
	Name = 'Mod'
	InputDirs = @('Src\mod', 'Include')
	GeneratedDirs = @('Output\{0}\Common')
})

function New-Fixture {
	$root = Join-Path ([System.IO.Path]::GetTempPath()) ("NativeObjFixture_" + [System.Guid]::NewGuid().ToString('N'))
	New-Item -ItemType Directory -Path $root | Out-Null
	return $root
}

function Set-FixtureFile {
	param([string] $Root, [string] $RelativePath, [datetime] $WriteTimeUtc)

	$path = Join-Path $Root $RelativePath
	New-Item -ItemType Directory -Path (Split-Path $path -Parent) -Force | Out-Null
	Set-Content -LiteralPath $path -Value 'x' -Encoding ASCII
	(Get-Item -LiteralPath $path).LastWriteTimeUtc = $WriteTimeUtc
}

function Assert-Removal {
	param([string] $Name, [scriptblock] $Arrange, [bool] $ExpectRemoved)

	$root = New-Fixture
	try {
		& $Arrange $root
		$objDir = Join-Path $root 'Obj\Debug\Mod'
		$existedBefore = Test-Path -LiteralPath $objDir
		$removed = Remove-NativeObjDirsWithStaleInputs -RepoRoot $root -Configuration 'Debug' -Modules $modules 6>$null
		$existsAfter = Test-Path -LiteralPath $objDir
		$reportedRemoved = $removed -contains 'Mod'
		if ($ExpectRemoved -and ($existsAfter -or -not $reportedRemoved)) {
			[void]$script:failures.Add("FAIL [$Name]: expected Obj\Debug\Mod to be removed and reported")
		}
		if (-not $ExpectRemoved -and ($reportedRemoved -or ($existedBefore -and -not $existsAfter))) {
			[void]$script:failures.Add("FAIL [$Name]: expected Obj\Debug\Mod to be left alone")
		}
	}
	finally {
		Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
	}
}

Assert-Removal 'header-newer-than-oldest-object' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\Unedited.obj' $old
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\Edited.obj' $new.AddMinutes(5)
	Set-FixtureFile $r 'Src\mod\Layout.h' $new
} $true

Assert-Removal 'shared-include-header-newer' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $old
	Set-FixtureFile $r 'Include\Shared.h' $new
} $true

Assert-Removal 'makefile-newer' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $old
	Set-FixtureFile $r 'Src\mod\Mod.mak' $new
} $true

Assert-Removal 'all-objects-newer-than-headers' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $new.AddMinutes(1)
	Set-FixtureFile $r 'Obj\Debug\Mod\nopch\B.obj' $new.AddMinutes(2)
	Set-FixtureFile $r 'Src\mod\Layout.h' $new
} $false

# Source files are nmake's own job; only headers and makefiles count here.
Assert-Removal 'only-a-source-file-is-newer' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $old
	Set-FixtureFile $r 'Src\mod\Layout.h' $old.AddMinutes(-1)
	Set-FixtureFile $r 'Src\mod\Layout.cpp' $new
} $false

# Test headers sit in a subfolder the module never includes from.
Assert-Removal 'header-in-unlisted-subfolder' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $old
	Set-FixtureFile $r 'Src\mod\Test\TestLayout.h' $new
} $false

Assert-Removal 'no-object-folder' {
	param($r)
	Set-FixtureFile $r 'Src\mod\Layout.h' $new
} $false

Assert-Removal 'object-folder-without-objects' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\Mod.txt' $old
	Set-FixtureFile $r 'Src\mod\Layout.h' $new
} $false

Assert-Removal 'generated-header-newer' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $old
	Set-FixtureFile $r 'Output\Debug\Common\CellarConstants.h' $new
} $true

# Generated folders are per configuration; another configuration's output must not count.
Assert-Removal 'other-configuration-generated-header-newer' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $old
	Set-FixtureFile $r 'Output\Release\Common\CellarConstants.h' $new
} $false

Assert-Removal 'only-version-stamp-header-newer' {
	param($r)
	Set-FixtureFile $r 'Obj\Debug\Mod\autopch\A.obj' $old
	Set-FixtureFile $r 'Output\Debug\Common\bldinc.h' $new
} $false

function Invoke-WithLockedFile {
	param([string] $Name, [string] $LockedRelativePath, [bool] $ExpectThrow)

	$root = New-Fixture
	$stream = $null
	try {
		Set-FixtureFile $root 'Obj\Debug\Mod\autopch\A.obj' $old
		Set-FixtureFile $root 'Obj\Debug\Mod\autopch\B.obj' $old
		Set-FixtureFile $root $LockedRelativePath $old
		Set-FixtureFile $root 'Src\mod\Layout.h' $new
		$stream = [System.IO.File]::Open((Join-Path $root $LockedRelativePath), 'Open', 'Read', 'None')

		$threw = $false
		$removed = @()
		try {
			$removed = Remove-NativeObjDirsWithStaleInputs -RepoRoot $root -Configuration 'Debug' -Modules $modules 6>$null
		}
		catch {
			$threw = $true
		}

		$objDir = Join-Path $root 'Obj\Debug\Mod'
		$objsLeft = @(Get-ChildItem -LiteralPath $objDir -Recurse -Filter '*.obj' -ErrorAction SilentlyContinue |
			Where-Object { $_.FullName -ne (Join-Path $root $LockedRelativePath) })
		if ($ExpectThrow -and -not $threw) {
			[void]$script:failures.Add("FAIL [$Name]: expected a locked object file to fail the build")
		}
		if (-not $ExpectThrow -and ($threw -or -not ($removed -contains 'Mod') -or $objsLeft.Count -gt 0)) {
			[void]$script:failures.Add("FAIL [$Name]: expected a locked debug database to be skipped with every object removed")
		}
	}
	finally {
		if ($stream) { $stream.Dispose() }
		Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
	}
}

Invoke-WithLockedFile 'locked-debug-database-is-skipped' 'Obj\Debug\Mod\vc140.pdb' $false
Invoke-WithLockedFile 'locked-object-fails-the-build' 'Obj\Debug\Mod\autopch\Locked.obj' $true

# Each real module's input folders must exist, or a typo would silently skip them.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
foreach ($module in (Get-NativeMakefileModules)) {
	foreach ($dir in $module.InputDirs) {
		if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $dir) -PathType Container)) {
			[void]$failures.Add("FAIL [real-module-dirs]: $($module.Name) lists missing folder '$dir'")
		}
	}
}

# A path-qualified include such as "../Cellar/FwXml.h" bypasses the makefile's include list, so
# the folder it reaches must be listed for every module that scans the including file.
foreach ($module in (Get-NativeMakefileModules)) {
	$listed = @($module.InputDirs | ForEach-Object { [System.IO.Path]::GetFullPath((Join-Path $repoRoot $_)).TrimEnd('\') })
	foreach ($dir in $module.InputDirs) {
		$fullDir = Join-Path $repoRoot $dir
		foreach ($source in (Get-ChildItem -LiteralPath $fullDir -File -ErrorAction SilentlyContinue |
				Where-Object { $_.Extension -in @('.cpp', '.c', '.h', '.hpp', '.inl') })) {
			foreach ($match in (Select-String -LiteralPath $source.FullName -Pattern '^\s*#\s*include\s+"([^"]*[\\/][^"]*)"' -AllMatches)) {
				$includePath = $match.Matches[0].Groups[1].Value
				$target = Join-Path $source.DirectoryName $includePath
				if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
					# Leading ..\ segments may climb from a deeper include folder.
					$target = Join-Path $repoRoot ($includePath -replace '^([.][.][\\/])+', '')
				}
				if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
					continue
				}
				$targetDir = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetFullPath($target)).TrimEnd('\')
				if ($listed -notcontains $targetDir) {
					$relativeSource = $source.FullName.Substring($repoRoot.Length + 1)
					[void]$failures.Add("FAIL [real-module-includes]: $($module.Name) scans $relativeSource, which includes a header in unlisted folder '$targetDir'")
				}
			}
		}
	}
}

if ($failures.Count -gt 0) {
	Write-Host ''
	foreach ($f in $failures) { Write-Host $f -ForegroundColor Red }
	Write-Host ''
	Write-Host "$($failures.Count) test(s) failed." -ForegroundColor Red
	exit 1
}

Write-Host 'All NativeObjStaleness tests passed.' -ForegroundColor Green
exit 0
