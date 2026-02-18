#!/usr/bin/env pwsh
# Shared Git helpers for Build/Agent PowerShell scripts
$ErrorActionPreference = 'Stop'

function Get-DefaultBranchName {
	$line = git remote show origin 2>$null | Select-String 'HEAD branch:' | Select-Object -First 1
	if ($line) { return ($line -split ':')[1].Trim() }
	return $null
}

function Get-DefaultBranchRef {
	$name = Get-DefaultBranchName
	if ($name) { return "origin/$name" }
	return 'origin/develop'
}
