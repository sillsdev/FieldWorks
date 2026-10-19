<#
.SYNOPSIS
	Previews source-tree registry overrides that can redirect an installed FieldWorks.
.DESCRIPTION
	Dev builds resolve code and data from their own source tree. This preflight is
	read-only; use Build/Repair-DeveloperRegistry.ps1 explicitly to remove overrides.
.PARAMETER Force
	Accepted for existing callers. The preflight remains read-only.
.OUTPUTS
	RESULT=READ_ONLY after the preview completes.
#>
[CmdletBinding()]
param([switch]$Force)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')).Path
& (Join-Path $repoRoot 'Build/Repair-DeveloperRegistry.ps1') -WhatIf
Write-Host 'Dev code and data resolve from the running source tree.'
Write-Host 'RESULT=READ_ONLY'
