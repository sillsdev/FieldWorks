[CmdletBinding()]
param(
	[string]$SearchRoot,
	[string]$OutputDirectory,
	[string]$GitHubOutputPath = $env:GITHUB_OUTPUT
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrWhiteSpace($SearchRoot)) {
	$SearchRoot = $repoRoot
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
	$OutputDirectory = Join-Path (Join-Path $repoRoot 'Output') 'RenderArtifacts'
}

function Get-FullPath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Path
	)

	return [System.IO.Path]::GetFullPath($Path)
}

function Format-RelativePath {
	param(
		[Parameter(Mandatory = $true)]
		[string]$BasePath,
		[Parameter(Mandatory = $true)]
		[string]$TargetPath
	)

	$baseFullPath = Get-FullPath $BasePath
	$targetFullPath = Get-FullPath $TargetPath
	if (-not $baseFullPath.EndsWith([System.IO.Path]::DirectorySeparatorChar.ToString(), [System.StringComparison]::Ordinal)) {
		$baseFullPath += [System.IO.Path]::DirectorySeparatorChar
	}

	$baseUri = New-Object System.Uri($baseFullPath)
	$targetUri = New-Object System.Uri($targetFullPath)
	return [System.Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
}

function Format-ArtifactPath {
	param(
		[string]$Path
	)

	if ([string]::IsNullOrWhiteSpace($Path)) {
		return $null
	}

	return $Path.Replace([System.IO.Path]::DirectorySeparatorChar, '/')
}

function Copy-RenderArtifactFile {
	param(
		[Parameter(Mandatory = $true)]
		[string]$SourcePath,
		[Parameter(Mandatory = $true)]
		[string]$DestinationPath,
		[Parameter(Mandatory = $true)]
		[string]$OutputRoot
	)

	if (-not (Test-Path -LiteralPath $SourcePath)) {
		return $null
	}

	$destinationDirectory = Split-Path -Parent $DestinationPath
	if (-not (Test-Path -LiteralPath $destinationDirectory)) {
		New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
	}

	Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
	return Format-ArtifactPath (Format-RelativePath -BasePath $OutputRoot -TargetPath $DestinationPath)
}

function Write-GitHubOutputValue {
	param(
		[Parameter(Mandatory = $true)]
		[string]$Name,
		[Parameter(Mandatory = $true)]
		[string]$Value
	)

	if ([string]::IsNullOrWhiteSpace($GitHubOutputPath)) {
		return
	}

	$encoding = New-Object System.Text.UTF8Encoding($false)
	[System.IO.File]::AppendAllText($GitHubOutputPath, "$Name=$Value$([System.Environment]::NewLine)", $encoding)
}

function New-MarkdownReport {
	param(
		[Parameter(Mandatory = $true)]
		[object[]]$Entries,
		[Parameter(Mandatory = $true)]
		[string]$OutputRoot
	)

	$readmePath = Join-Path $OutputRoot 'README.md'
	$lines = @(
		'# Render comparison artifacts',
		'',
		"Render snapshot failures: $($Entries.Count)",
		'',
		'Open `index.html` after downloading this artifact to inspect expected, actual, and diff images.',
		'',
		'| Snapshot | Expected | Actual | Diff |',
		'|---|---|---|---|'
	)

	foreach ($entry in $Entries) {
		$expected = if ($entry.ExpectedPath) { "[expected]($($entry.ExpectedPath))" } else { 'missing' }
		$actual = if ($entry.ActualPath) { "[actual]($($entry.ActualPath))" } else { 'missing' }
		$diff = if ($entry.DiffPath) { "[diff]($($entry.DiffPath))" } else { 'missing' }
		$lines += "| $($entry.SourcePath) | $expected | $actual | $diff |"
	}

	Set-Content -Path $readmePath -Value $lines -Encoding UTF8
}

function Get-DiffStat {
	param(
		[string]$DiffMetadataSourcePath
	)

	if ([string]::IsNullOrWhiteSpace($DiffMetadataSourcePath) -or -not (Test-Path -LiteralPath $DiffMetadataSourcePath)) {
		return $null
	}

	try {
		$diffJson = Get-Content -LiteralPath $DiffMetadataSourcePath -Raw | ConvertFrom-Json
		$diffNode = $diffJson.PSObject.Properties['Diff']
		if ($null -eq $diffNode -or $null -eq $diffNode.Value) {
			return $null
		}

		$diff = $diffNode.Value
		$allowedNode = $diffJson.PSObject.Properties['AllowedDifferentPixelCount']
		return [pscustomobject]@{
			DifferentPixelCount = $diff.DifferentPixelCount
			AllowedDifferentPixelCount = if ($null -ne $allowedNode) { $allowedNode.Value } else { $null }
			RegionWidth = $diff.DiffRegionWidth
			RegionHeight = $diff.DiffRegionHeight
			MinX = $diff.MinX
			MinY = $diff.MinY
		}
	}
	catch {
		return $null
	}
}

function New-HtmlReport {
	param(
		[Parameter(Mandatory = $true)]
		[object[]]$Entries,
		[Parameter(Mandatory = $true)]
		[string]$OutputRoot
	)

	$indexPath = Join-Path $OutputRoot 'index.html'

	$style = @'
<style>
	:root{color-scheme:light}
	body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#1f2328;background:#fff}
	h1{margin:0 0 4px}
	.lede{color:#57606a;margin:0 0 20px}
	.hint{font-size:12px;color:#57606a;margin:6px 0 20px}
	table{border-collapse:collapse;width:100%}
	th,td{border:1px solid #d0d7de;padding:8px;vertical-align:top}
	th{background:#f6f8fa;text-align:left}
	td.thumb{padding:4px;text-align:center;width:330px}
	img.thumb{max-width:320px;max-height:240px;border:1px solid #d0d7de;background:#fff;cursor:zoom-in;display:block;margin:auto}
	.path{font-family:Consolas,monospace;font-size:12px;word-break:break-all}
	.stat{font-family:Consolas,monospace;font-size:12px;color:#57606a;margin-top:6px}
	.stat b{color:#1f2328}
	kbd{font-family:Consolas,monospace;background:#f6f8fa;border:1px solid #d0d7de;border-bottom-width:2px;border-radius:4px;padding:1px 5px;font-size:11px}

	/* Comparison viewer */
	#lb{position:fixed;inset:0;background:rgba(13,17,23,.92);display:none;flex-direction:column;z-index:1000;color:#adbac7;font-size:13px}
	#lb.open{display:flex}
	#lb header{display:flex;align-items:center;gap:14px;padding:10px 16px;color:#e6edf3;font-size:14px}
	#lb header .name{font-family:Consolas,monospace;font-weight:600}
	#lb header .spacer{flex:1}
	#lb button{cursor:pointer;color:#adbac7;background:transparent;border:1px solid #444c56;border-radius:6px;padding:4px 10px;font:inherit}
	#lb button:hover{background:#21262d;color:#e6edf3}
	.modebar{display:flex;gap:6px;justify-content:center;padding-bottom:8px}
	.modebar button{border-radius:999px;padding:4px 14px}
	.modebar button.active{background:#1f6feb;border-color:#1f6feb;color:#fff;font-weight:600}
	.stage{flex:1;display:flex;align-items:center;justify-content:center;overflow:hidden;padding:0 16px 8px}
	.frame{position:relative;cursor:pointer;box-shadow:0 0 0 1px #444c56;
		/* checkerboard so transparent/white renders stay visible on the dark backdrop */
		background:linear-gradient(45deg,#888 25%,transparent 25%,transparent 75%,#888 75%),
		linear-gradient(45deg,#888 25%,#bbb 25%,#bbb 75%,#888 75%) #aaa;
		background-size:16px 16px;background-position:0 0,8px 8px}
	.frame img{position:absolute;top:0;left:0;display:block;opacity:0;image-rendering:pixelated}
	.frame img.show{opacity:1}
	#lb footer{padding:8px 16px 14px;text-align:center}
	#lb footer .nav{margin-top:4px;color:#768390}
	#lb footer kbd{background:#21262d;border-color:#444c56;color:#e6edf3}
</style>
'@

	$viewer = @'
<div id="lb" role="dialog" aria-modal="true" aria-label="Render comparison viewer">
	<header>
		<span class="name" id="lbName"></span>
		<span class="stat" id="lbStat"></span>
		<span class="spacer"></span>
		<button id="lbClose" title="Close (Esc)">&#10005; Close</button>
	</header>
	<div class="modebar" id="lbModes"></div>
	<div class="stage" id="lbStage">
		<div class="frame" id="lbFrame" title="Click to flip Expected &#8596; Actual">
			<img id="img-expected" alt="Expected">
			<img id="img-actual" alt="Actual">
			<img id="img-diff" alt="Diff">
		</div>
	</div>
	<footer>
		<span id="lbModeLabel"></span>
		<div class="nav"><kbd>Click</kbd> flip Expected/Actual &nbsp;&middot;&nbsp; <kbd>&#8592;</kbd> <kbd>&#8594;</kbd> change view &nbsp;&middot;&nbsp; <kbd>Esc</kbd> close</div>
	</footer>
</div>
'@

	$script = @'
<script>
const MODES = ["expected", "actual", "diff"];
const LABEL = { expected: "Expected (baseline)", actual: "Actual (this run)", diff: "Diff (changed pixels)" };

const lb = document.getElementById("lb");
const stage = document.getElementById("lbStage");
const frame = document.getElementById("lbFrame");
const modeBar = document.getElementById("lbModes");
const imgs = { expected: byId("expected"), actual: byId("actual"), diff: byId("diff") };
function byId(m){ return document.getElementById("img-" + m); }

let sources = {};      // mode -> image src for the open snapshot
let mode = "actual";   // current view
let lastAB = "actual"; // last Expected/Actual side, so Diff can flip back

// Open the viewer for whichever thumbnail was clicked, starting on its mode.
document.querySelector("table").addEventListener("click", e => {
	const img = e.target.closest("img.thumb");
	if (img) openRow(img.closest("tr"), img.dataset.mode);
});

function openRow(tr, startMode) {
	sources = {};
	tr.querySelectorAll("img.thumb").forEach(img => sources[img.dataset.mode] = img.getAttribute("src"));
	document.getElementById("lbName").textContent = tr.dataset.name || "";
	document.getElementById("lbStat").textContent = tr.dataset.stat || "";

	for (const m of MODES) imgs[m].src = sources[m] || "";
	imgs.expected.onload = sizeFrame;
	if (imgs.expected.complete) sizeFrame();

	buildModeBar();
	setMode(sources[startMode] ? startMode : "actual");
	lb.classList.add("open");
}

// Lock the frame to the render's natural size, then scale it to fill the stage.
function sizeFrame() {
	frame.style.width = imgs.expected.naturalWidth + "px";
	frame.style.height = imgs.expected.naturalHeight + "px";
	fitFrame();
}
function fitFrame() {
	const w = imgs.expected.naturalWidth, h = imgs.expected.naturalHeight;
	if (!w || !h) return;
	const k = Math.min((stage.clientWidth - 24) / w, (stage.clientHeight - 24) / h);
	frame.style.transform = `scale(${Math.max(k, 0.1)})`;
}
window.addEventListener("resize", () => { if (lb.classList.contains("open")) fitFrame(); });

function buildModeBar() {
	modeBar.innerHTML = "";
	for (const m of MODES) {
		if (!sources[m]) continue;
		const b = document.createElement("button");
		b.textContent = m[0].toUpperCase() + m.slice(1);
		b.dataset.mode = m;
		b.onclick = () => setMode(m);
		modeBar.appendChild(b);
	}
}

function setMode(m) {
	mode = m;
	if (m !== "diff") lastAB = m;
	for (const k of MODES) imgs[k].classList.toggle("show", k === m);
	[...modeBar.children].forEach(b => b.classList.toggle("active", b.dataset.mode === m));
	document.getElementById("lbModeLabel").textContent = LABEL[m];
}

function step(dir) {
	const avail = MODES.filter(m => sources[m]);
	setMode(avail[(avail.indexOf(mode) + dir + avail.length) % avail.length]);
}

function close() { lb.classList.remove("open"); }

// Click image: flip Expected <-> Actual; from Diff, flip back to the last A/B side.
frame.addEventListener("click", () => setMode(mode === "diff" ? lastAB : mode === "expected" ? "actual" : "expected"));
document.getElementById("lbClose").onclick = close;
lb.addEventListener("click", e => { if (e.target === lb) close(); });
document.addEventListener("keydown", e => {
	if (!lb.classList.contains("open")) return;
	if (e.key === "Escape") close();
	else if (e.key === "ArrowRight") { step(1); e.preventDefault(); }
	else if (e.key === "ArrowLeft") { step(-1); e.preventDefault(); }
});
</script>
'@

	$builder = New-Object System.Text.StringBuilder
	[void]$builder.AppendLine('<!doctype html>')
	[void]$builder.AppendLine('<html lang="en">')
	[void]$builder.AppendLine('<head>')
	[void]$builder.AppendLine('<meta charset="utf-8">')
	[void]$builder.AppendLine('<title>Render comparison artifacts</title>')
	[void]$builder.AppendLine($style)
	[void]$builder.AppendLine('</head>')
	[void]$builder.AppendLine('<body>')
	[void]$builder.AppendLine('<h1>Render comparison artifacts</h1>')
	[void]$builder.AppendLine("<p class=`"lede`">Render snapshot failures: $($Entries.Count)</p>")
	[void]$builder.AppendLine('<p class="hint">Click any image to open the comparison viewer. There, <b>click the image to flip Expected&nbsp;&#8596;&nbsp;Actual</b>, or use <kbd>&#8592;</kbd>&nbsp;<kbd>&#8594;</kbd> to step through Expected / Actual / Diff. <kbd>Esc</kbd> closes.</p>')
	[void]$builder.AppendLine('<table>')
	[void]$builder.AppendLine('<thead><tr><th>Snapshot</th><th>Expected</th><th>Actual</th><th>Diff</th></tr></thead>')
	[void]$builder.AppendLine('<tbody>')

	foreach ($entry in $Entries) {
		$source = $entry.SourcePath
		$encodedSource = [System.Net.WebUtility]::HtmlEncode($source)
		$encodedName = [System.Net.WebUtility]::HtmlEncode($entry.SnapshotName)

		$statAttr = ''
		$statCell = ''
		if ($null -ne $entry.DiffStat) {
			$d = $entry.DiffStat
			# numeric entities keep the &middot; and &times; independent of source encoding
			$statAttr = '{0} px differ &#183; region {1}&#215;{2} @ ({3},{4})' -f $d.DifferentPixelCount, $d.RegionWidth, $d.RegionHeight, $d.MinX, $d.MinY
			$statCell = '<div class="stat"><b>{0}</b> px differ (allowed {1})</div>' -f $d.DifferentPixelCount, $d.AllowedDifferentPixelCount
		}

		[void]$builder.AppendLine(('<tr data-name="{0}" data-stat="{1}">' -f $encodedName, $statAttr))
		[void]$builder.AppendLine(('<td class="path">{0}{1}</td>' -f $encodedSource, $statCell))
		foreach ($artifact in @(
			@{ Mode = 'expected'; PropertyName = 'ExpectedPath'; Description = 'Expected render' },
			@{ Mode = 'actual'; PropertyName = 'ActualPath'; Description = 'Actual render' },
			@{ Mode = 'diff'; PropertyName = 'DiffPath'; Description = 'Diff image' }
		)) {
			$path = $entry.($artifact.PropertyName)
			if ([string]::IsNullOrWhiteSpace($path)) {
				[void]$builder.AppendLine('<td>missing</td>')
				continue
			}

			$encodedPath = [System.Net.WebUtility]::HtmlEncode($path)
			$encodedAltText = [System.Net.WebUtility]::HtmlEncode(('{0} for {1}' -f $artifact.Description, $source))
			[void]$builder.AppendLine(('<td class="thumb"><img class="thumb" data-mode="{0}" src="{1}" alt="{2}"></td>' -f $artifact.Mode, $encodedPath, $encodedAltText))
		}
		[void]$builder.AppendLine('</tr>')
	}

	[void]$builder.AppendLine('</tbody>')
	[void]$builder.AppendLine('</table>')
	[void]$builder.AppendLine($viewer)
	[void]$builder.AppendLine($script)
	[void]$builder.AppendLine('</body>')
	[void]$builder.AppendLine('</html>')

	Set-Content -Path $indexPath -Value $builder.ToString() -Encoding UTF8
}

$resolvedSearchRoot = Get-FullPath $SearchRoot
$resolvedOutputDirectory = Get-FullPath $OutputDirectory

if (-not (Test-Path -LiteralPath $resolvedSearchRoot)) {
	throw "Search root not found: $resolvedSearchRoot"
}

if (Test-Path -LiteralPath $resolvedOutputDirectory) {
	Remove-Item -LiteralPath $resolvedOutputDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

$receivedFiles = @(Get-ChildItem -Path $resolvedSearchRoot -Filter '*.received.png' -File -Recurse | Where-Object {
	-not $_.FullName.StartsWith($resolvedOutputDirectory, [System.StringComparison]::OrdinalIgnoreCase)
} | Sort-Object FullName)

$entries = @()
foreach ($receivedFile in $receivedFiles) {
	$receivedPath = $receivedFile.FullName
	$snapshotBasePath = $receivedPath.Substring(0, $receivedPath.Length - '.received.png'.Length)
	$snapshotName = [System.IO.Path]::GetFileName($snapshotBasePath)
	$relativeDirectory = Format-RelativePath -BasePath $resolvedSearchRoot -TargetPath (Split-Path -Parent $receivedPath)
	$destinationDirectory = Join-Path $resolvedOutputDirectory $relativeDirectory

	$expectedPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.verified.png') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.expected.png')) -OutputRoot $resolvedOutputDirectory
	$actualPath = Copy-RenderArtifactFile -SourcePath $receivedPath -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.actual.png')) -OutputRoot $resolvedOutputDirectory
	$diffPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.diff.png') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.diff.png')) -OutputRoot $resolvedOutputDirectory
	$expectedMetadataPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.verified.json') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.expected.json')) -OutputRoot $resolvedOutputDirectory
	$actualMetadataPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.received.json') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.actual.json')) -OutputRoot $resolvedOutputDirectory
	$diffMetadataPath = Copy-RenderArtifactFile -SourcePath ($snapshotBasePath + '.diff.json') -DestinationPath (Join-Path $destinationDirectory ($snapshotName + '.diff.json')) -OutputRoot $resolvedOutputDirectory
	$diffStat = Get-DiffStat -DiffMetadataSourcePath ($snapshotBasePath + '.diff.json')

	$entries += [pscustomobject]@{
		SnapshotName = $snapshotName
		SourcePath = Format-ArtifactPath (Format-RelativePath -BasePath $resolvedSearchRoot -TargetPath $receivedPath)
		ExpectedPath = $expectedPath
		ActualPath = $actualPath
		DiffPath = $diffPath
		ExpectedMetadataPath = $expectedMetadataPath
		ActualMetadataPath = $actualMetadataPath
		DiffMetadataPath = $diffMetadataPath
		DiffStat = $diffStat
	}
}

$hasArtifacts = $entries.Count -gt 0
Write-GitHubOutputValue -Name 'has_artifacts' -Value ($hasArtifacts.ToString().ToLowerInvariant())
Write-GitHubOutputValue -Name 'failure_count' -Value $entries.Count.ToString([System.Globalization.CultureInfo]::InvariantCulture)
Write-GitHubOutputValue -Name 'artifact_path' -Value $resolvedOutputDirectory

if (-not $hasArtifacts) {
	Write-Output ([pscustomobject]@{
		HasArtifacts = $false
		FailureCount = 0
		OutputDirectory = $resolvedOutputDirectory
	})
	return
}

$manifest = [pscustomobject]@{
	GeneratedAtUtc = [System.DateTime]::UtcNow.ToString('O', [System.Globalization.CultureInfo]::InvariantCulture)
	FailureCount = $entries.Count
	Artifacts = $entries
}

$manifest | ConvertTo-Json -Depth 6 | Set-Content -Path (Join-Path $resolvedOutputDirectory 'manifest.json') -Encoding UTF8
New-MarkdownReport -Entries $entries -OutputRoot $resolvedOutputDirectory
New-HtmlReport -Entries $entries -OutputRoot $resolvedOutputDirectory

Write-Output ([pscustomobject]@{
	HasArtifacts = $true
	FailureCount = $entries.Count
	OutputDirectory = $resolvedOutputDirectory
})

Write-Host "::notice title=Render comparison artifacts::$($entries.Count) render comparison artifact set(s) collected."