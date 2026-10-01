<#
.SYNOPSIS
	Deletes the Obj folder of each nmake-built native module whose headers or makefiles
	are newer than its oldest object, so the next native build compiles it completely.

.DESCRIPTION
	Wraps Remove-NativeObjDirsWithStaleInputs for MSBuild, which runs it after package
	restore has copied its headers and before the first native module builds. Exits 1
	when a module's objects could not be removed.

.EXAMPLE
	Build/Agent/Remove-StaleNativeObjects.ps1 -RepoRoot . -Configuration Debug
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory)][string] $RepoRoot,
	[Parameter(Mandatory)][string] $Configuration
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'FwBuildHelpers.psm1') -Force

try {
	[void](Remove-NativeObjDirsWithStaleInputs -RepoRoot (Resolve-Path $RepoRoot).Path -Configuration $Configuration)
}
catch {
	Write-Host "[ERROR] $($_.Exception.Message)" -ForegroundColor Red
	exit 1
}
exit 0
