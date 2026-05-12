# Adds <codeBase href="Newtonsoft.Json.dll"> under Newtonsoft.Json dependentAssembly in staged *.config
# so the private 13.0.x DLL wins over an older same-identity (13.0.0.0) GAC build. Param: StagedBinDir.
param([Parameter(Mandatory)][string]$StagedBinDir)
$ErrorActionPreference = 'Stop'
$bin = if ($StagedBinDir) { [IO.Path]::GetFullPath($StagedBinDir) }
if (-not $bin -or -not (Test-Path -LiteralPath $bin -PathType Container)) {
	Write-Verbose "Skip: staged bin missing: $StagedBinDir"; return
}
$nj = Join-Path $bin 'Newtonsoft.Json.dll'
if (-not (Test-Path -LiteralPath $nj -PathType Leaf)) {
	Write-Verbose "Skip: Newtonsoft.Json.dll missing under $bin"; return
}
$ver = [Reflection.AssemblyName]::GetAssemblyName($nj).Version.ToString()
$ns = 'urn:schemas-microsoft-com:asm.v1'
Get-ChildItem -LiteralPath $bin -Filter '*.config' -File | ForEach-Object {
	$path = $_.FullName
	$xml = New-Object System.Xml.XmlDocument
	try { $xml.Load($path) } catch { Write-Warning "Skip unreadable config $path : $_"; return }
	$m = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
	[void]$m.AddNamespace('ab', $ns)
	$deps = $xml.SelectNodes('//ab:dependentAssembly', $m)
	if (-not $deps -or $deps.Count -eq 0) { return }
	$changed = $false
	foreach ($d in $deps) {
		$id = $d.SelectSingleNode('ab:assemblyIdentity', $m)
		if (-not $id -or $id.GetAttribute('name') -cne 'Newtonsoft.Json') { continue }
		$ok = $false
		foreach ($c in @($d.SelectNodes('ab:codeBase', $m))) {
			if ($c.GetAttribute('href') -cne 'Newtonsoft.Json.dll') { continue }
			if ($c.GetAttribute('version') -ceq $ver) { $ok = $true; break }
			[void]$d.RemoveChild($c); $changed = $true
		}
		if ($ok) { continue }
		$cb = $xml.CreateElement('codeBase', $ns)
		$cb.SetAttribute('version', $ver); $cb.SetAttribute('href', 'Newtonsoft.Json.dll')
		[void]$d.InsertAfter($cb, $id); $changed = $true
	}
	if ($changed) { $xml.Save($path) }
}
