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
$modules = @([pscustomobject]@{ Name = 'Mod'; InputDirs = @('Src\mod', 'Include') })

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

# Each real module's input folders must exist, or a typo would silently skip them.
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
foreach ($module in (Get-NativeMakefileModules)) {
	foreach ($dir in $module.InputDirs) {
		if (-not (Test-Path -LiteralPath (Join-Path $repoRoot $dir) -PathType Container)) {
			[void]$failures.Add("FAIL [real-module-dirs]: $($module.Name) lists missing folder '$dir'")
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
