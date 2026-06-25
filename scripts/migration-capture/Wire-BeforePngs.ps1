<#
.SYNOPSIS
  Wire captured "<classkebab>-before.png" images into their Docs/migration markdown files.

  Captured PNGs are named after the dialog CLASS (e.g. fw-proj-properties from FwProjPropertiesDlg),
  but docs are named after the SCREEN (e.g. project-properties.md). So match each PNG to the doc whose
  declared legacy class (the `ClassName` in the doc title / "Legacy class" row) kebabs to the same stem;
  fall back to filename-stem match. Move the PNG into that doc's images/ folder and insert/update a
  "## What it looks like (before / after)" section. Idempotent. Reports unmatched PNGs.
#>
[CmdletBinding()]
param([string]$Root)
if (-not $Root) { $Root = (Resolve-Path "$PSScriptRoot\..\..\Docs\migration").Path }

function Kebab([string]$cls) {
  if ($cls.EndsWith("Dlg")) { $cls = $cls.Substring(0, $cls.Length - 3) }
  elseif ($cls.EndsWith("Dialog")) { $cls = $cls.Substring(0, $cls.Length - 6) }
  $sb = New-Object System.Text.StringBuilder
  for ($i = 0; $i -lt $cls.Length; $i++) {
    $c = $cls[$i]
    if ([char]::IsUpper($c) -and $i -gt 0 -and ([char]::IsLower($cls[$i-1]) -or ($i+1 -lt $cls.Length -and [char]::IsLower($cls[$i+1])))) { [void]$sb.Append('-') }
    [void]$sb.Append([char]::ToLowerInvariant($c))
  }
  return $sb.ToString()
}

$docs = Get-ChildItem $Root -Recurse -Filter "*.md" | Where-Object { $_.Name -notmatch '^_|README|INVENTORY' }
$docByKey = @{}   # key (kebab) -> doc full path  (first wins)
foreach ($d in $docs) {
  $txt = Get-Content $d.FullName -Raw
  $keys = New-Object System.Collections.Generic.HashSet[string]
  [void]$keys.Add($d.BaseName)                                   # filename stem
  foreach ($m in [regex]::Matches($txt, '`([A-Za-z0-9_.]*?([A-Za-z0-9]+(Dlg|Dialog)))`')) {
    $cls = $m.Groups[2].Value                                    # bare class name (last segment)
    [void]$keys.Add((Kebab $cls))
  }
  foreach ($k in $keys) { if (-not $docByKey.ContainsKey($k)) { $docByKey[$k] = $d.FullName } }
}

$pngs = Get-ChildItem $Root -Recurse -Filter "*-before.png"
$wired = 0; $already = 0; $unmatched = @()
foreach ($png in $pngs) {
  $stem = $png.BaseName -replace '-before$',''
  $doc = if ($docByKey.ContainsKey($stem)) { $docByKey[$stem] } else { $null }
  if (-not $doc) { $unmatched += $png.Name; continue }

  $docDir = Split-Path $doc
  $imgDir = Join-Path $docDir "images"
  New-Item -ItemType Directory -Force -Path $imgDir | Out-Null
  # Gather per-tab snapshots for this dialog from the same source dir before moving anything.
  $srcDir = Split-Path $png.FullName
  $tabs = Get-ChildItem $srcDir -Filter "$stem-tab-*.png" -ErrorAction SilentlyContinue
  $destBefore = Join-Path $imgDir $png.Name
  if ($png.FullName -ne $destBefore) { Move-Item -Force $png.FullName $destBefore }
  foreach ($t in $tabs) { $td = Join-Path $imgDir $t.Name; if ($t.FullName -ne $td) { Move-Item -Force $t.FullName $td } }
  $afterName = ($png.BaseName -replace '-before$','') + "-after.png"

  $txt = Get-Content $doc -Raw
  $rel = "./images/$($png.Name)"
  # Regenerate the section every run (idempotent) so newly-added tab images get picked up even if the
  # doc was already wired with just the main "before" image in an earlier sweep.
  $hadIt = $txt -match [regex]::Escape($rel)
  $tabBlock = ""
  if ($tabs.Count) {
    $tabBlock = "`r`nTabs (legacy):`r`n`r`n" + (($tabs | Sort-Object Name | ForEach-Object {
      $tn = ($_.BaseName -replace "^$([regex]::Escape($stem))-tab-",'')
      "![$tn](./images/$($_.Name))"
    }) -join " ") + "`r`n"
  }
  $block = @"
## What it looks like (before / after)
Legacy "before" captured by the screenshot harness (ScreenshotHarnessTests, option 2). Avalonia "after"
comes from the surface's FwAvaloniaDialogs(Tests) visual test (same data); attach both to the JIRA ticket.

| Legacy (WinForms) — "before" | Avalonia (New) — "after" |
|---|---|
| ![$stem legacy]($rel) | ![$stem avalonia](./images/$afterName) |
$tabBlock
"@
  if ($txt -match '(?ms)^##\s+What it looks like.*?(?=^##\s)') {
    $txt = [regex]::Replace($txt, '(?ms)^##\s+What it looks like.*?(?=^##\s)', $block)
  } elseif ($txt -match '(?m)^##\s+What it is') {
    $txt = $txt -replace '(?m)^##\s+What it is', ($block + '## What it is')
  } else {
    $txt = $txt.TrimEnd() + "`r`n`r`n" + $block
  }
  Set-Content -Path $doc -Value $txt -NoNewline
  $wired++
}
Write-Output "wired=$wired  already=$already  unmatched=$($unmatched.Count)"
if ($unmatched.Count) { Write-Output "UNMATCHED (no doc):"; $unmatched | Sort-Object | ForEach-Object { Write-Output "  $_" } }
