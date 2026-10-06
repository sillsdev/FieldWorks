<#
.SYNOPSIS
	Writes the nmake header dependencies for one native build product.

.DESCRIPTION
	Every compile rule in Bld/_rule.mak writes a cl /sourceDependencies sidecar
	(<object>.source-dependencies.json) next to its object file. This script reads
	those sidecars for one product and writes header-dependencies.mak, which the
	end of Bld/_targ.mak includes, so nmake rebuilds an object whenever a header it
	includes changes. nmake still makes every up-to-date decision; this script only
	supplies the edges.

	An object with no sidecar (compiled before header tracking existed, or its
	sidecar was deleted) depends on a target that is always out of date, so it is
	rebuilt once and gains a sidecar. A missing precompiled header does the same
	for the objects that produce and use it.

.PARAMETER RepositoryRoot
	Repository root. Headers outside it (SDK, toolset) are not tracked.

.PARAMETER IntermediateDirectory
	The product's intermediate directory, $(INT_DIR). The include file is written
	here.
#>
param(
	[Parameter(Mandatory = $true)]
	[string] $RepositoryRoot,
	[Parameter(Mandatory = $true)]
	[string] $IntermediateDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sidecarSuffix = '.source-dependencies.json'
$forceTarget = 'header-dependencies-force'
$repositoryPrefix = [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/') + '\'
$intermediateDirectory = [IO.Path]::GetFullPath($IntermediateDirectory)

if (-not [IO.Directory]::Exists($intermediateDirectory)) {
	exit 0
}

# nmake expands $ as a macro even inside quotes, so it is doubled. A # cannot be
# escaped inside a quoted name, so such a path fails the build rather than
# producing an edge to a file that does not exist.
function ConvertTo-NmakePath([string] $path) {
	if ($path.Contains('#')) {
		throw "Header dependency path contains '#', which nmake cannot quote: $path"
	}
	return '"' + $path.Replace('$', '$$') + '"'
}

# Returns the repository headers and PCH the sidecar records, and whether its PCH
# is missing. Deleted or renamed headers are dropped so they never become targets
# nmake cannot make.
function Get-TrackedFile([string] $sidecarPath) {
	$result = @{
		Paths = New-Object 'System.Collections.Generic.List[string]'
		MissingPch = $false
	}
	$data = ([IO.File]::ReadAllText($sidecarPath) | ConvertFrom-Json).Data
	$candidates = New-Object 'System.Collections.Generic.List[object]'
	$includes = $data.PSObject.Properties['Includes']
	if ($null -ne $includes) {
		foreach ($include in @($includes.Value)) {
			$candidates.Add(@{ Value = $include; IsPch = $false })
		}
	}
	$pch = $data.PSObject.Properties['PCH']
	if ($null -ne $pch -and $null -ne $pch.Value) {
		$candidates.Add(@{ Value = $pch.Value; IsPch = $true })
	}
	foreach ($candidate in $candidates) {
		$value = $candidate.Value
		$path = $null
		if ($value -is [string]) {
			$path = $value
		}
		elseif ($null -ne $value -and $null -ne $value.PSObject.Properties['Path']) {
			$path = [string]$value.Path
		}
		if ([string]::IsNullOrWhiteSpace($path)) {
			continue
		}
		$path = [IO.Path]::GetFullPath($path)
		if (-not $path.StartsWith($repositoryPrefix, [StringComparison]::OrdinalIgnoreCase)) {
			continue
		}
		if ([IO.File]::Exists($path)) {
			$result.Paths.Add($path)
		}
		elseif ($candidate.IsPch) {
			$result.MissingPch = $true
		}
	}
	return $result
}

$edges = New-Object 'System.Collections.Generic.List[string]'
$forced = New-Object 'System.Collections.Generic.SortedSet[string]' ([StringComparer]::OrdinalIgnoreCase)
$pchMissing = $false
$objects = [IO.Directory]::GetFiles($intermediateDirectory, '*.obj', [IO.SearchOption]::AllDirectories)
[Array]::Sort($objects, [StringComparer]::OrdinalIgnoreCase)
foreach ($object in $objects) {
	$sidecar = $object + $sidecarSuffix
	if (-not [IO.File]::Exists($sidecar)) {
		[void]$forced.Add($object)
		continue
	}
	$tracked = Get-TrackedFile $sidecar
	if ($tracked.MissingPch) {
		[void]$forced.Add($object)
		$pchMissing = $true
	}
	$tracked.Paths.Sort([StringComparer]::OrdinalIgnoreCase)
	$target = ConvertTo-NmakePath $object
	foreach ($path in $tracked.Paths) {
		$edges.Add($target + ' : ' + (ConvertTo-NmakePath $path))
	}
}
# A missing PCH is only recreated when its /Yc producer compiles again.
if ($pchMissing) {
	foreach ($object in $objects) {
		if ([IO.Path]::GetFileName([IO.Path]::GetDirectoryName($object)) -ieq 'genpch') {
			[void]$forced.Add($object)
		}
	}
}

$lines = New-Object 'System.Collections.Generic.List[string]'
$lines.AddRange($edges)
foreach ($object in $forced) {
	$lines.Add((ConvertTo-NmakePath $object) + ' : ' + $forceTarget)
}
if ($forced.Count -gt 0) {
	$lines.Add($forceTarget + ' :')
	$lines.Add("`t@rem")
}

$content = [string]::Join("`r`n", $lines.ToArray())
if ($content.Length -gt 0) {
	$content += "`r`n"
}
$makeFile = Join-Path $intermediateDirectory 'header-dependencies.mak'
if ([IO.File]::Exists($makeFile) -and [IO.File]::ReadAllText($makeFile) -ceq $content) {
	exit 0
}

# Write beside the target, then replace it, so nmake never reads a partial file.
$temporaryFile = $makeFile + '.' + [Diagnostics.Process]::GetCurrentProcess().Id + '.tmp'
try {
	[IO.File]::WriteAllText($temporaryFile, $content, (New-Object System.Text.UTF8Encoding($false)))
	$moved = $false
	if (-not [IO.File]::Exists($makeFile)) {
		try {
			[IO.File]::Move($temporaryFile, $makeFile)
			$moved = $true
		}
		catch [IO.IOException] {
			# Another build created the file first; replace it below instead.
		}
	}
	if (-not $moved) {
		# Windows PowerShell passes $null to a string parameter as "", which Replace rejects.
		[IO.File]::Replace($temporaryFile, $makeFile, [NullString]::Value)
	}
}
finally {
	if ([IO.File]::Exists($temporaryFile)) {
		[IO.File]::Delete($temporaryFile)
	}
}
