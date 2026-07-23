<#
.SYNOPSIS
  Option 2 — launch-per-tool legacy screenshot capture for the migration docs.

  For each tool in the manifest, launches legacy FLEx pointed at that tool via a
  guid-less silfw link (FwLinkArgs / FieldWorks.cs FwAppArgs), waits for the project
  to load, captures the main window with PrintWindow, and closes gracefully (releasing
  the project lock) before the next tool.

  Capture is read-only: it never edits the language project. UIMode is left at its
  default (Legacy). Runs unattended; no MCP required.

.NOTES
  Proven recipe: FieldWorks.exe "silfw://localhost/link?app=flex&database=<proj>&server=&tool=<toolId>"
  (sole argument, NO guid param — an empty guid crashes FwLinkArgs parsing).
#>
[CmdletBinding()]
param(
  [string]$Worktree,
  [string]$Project  = "Sena 3",
  [string]$Manifest,
  [string]$Only,                 # capture only this toolId (proof runs)
  [int]$TimeoutSec  = 150,        # max wait for project load
  [int]$PaintDelayMs = 4000,      # let the tool finish painting before capture
  [switch]$Force                  # recapture even if the PNG already exists
)
$ErrorActionPreference = 'Stop'
# Resolve defaults in the body where $PSScriptRoot is reliably bound.
if (-not $Worktree) { $Worktree = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
if (-not $Manifest) { $Manifest = Join-Path $Worktree 'openspec\changes\legacy-screenshot-capture\manifests\tools.csv' }
$exe = Join-Path $Worktree 'Output\Debug\FieldWorks.exe'
if (-not (Test-Path $exe)) { throw "FieldWorks.exe not found: $exe (build the worktree first)" }

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;using System.Runtime.InteropServices;using System.Drawing;using System.Drawing.Imaging;
public struct CapRect { public int Left, Top, Right, Bottom; }
public static class Cap {
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out CapRect r);
  public static void Save(IntPtr hwnd, string path) {
    CapRect r; GetWindowRect(hwnd, out r);
    int w = r.Right - r.Left, h = r.Bottom - r.Top;
    if (w < 1 || h < 1) throw new Exception("zero-size window");
    using (var bmp = new Bitmap(w, h))
    using (var g = Graphics.FromImage(bmp)) {
      IntPtr hdc = g.GetHdc();
      PrintWindow(hwnd, hdc, 2u); // PW_RENDERFULLCONTENT
      g.ReleaseHdc(hdc);
      bmp.Save(path, ImageFormat.Png);
    }
  }
}
"@

function Wait-Loaded([System.Diagnostics.Process]$p, [int]$timeoutSec) {
  $sw = [Diagnostics.Stopwatch]::StartNew()
  while ($sw.Elapsed.TotalSeconds -lt $timeoutSec) {
    Start-Sleep -Milliseconds 600
    if ($p.HasExited) { return "exited" }
    $p.Refresh(); $t = $p.MainWindowTitle
    if ($t -like '*FieldWorks Language Explorer*') { return "loaded" }
    if ($t -eq 'An error has occurred') { return "error-dialog" }
  }
  return "timeout"
}

function Close-Flex([System.Diagnostics.Process]$p) {
  if ($p -and -not $p.HasExited) {
    $null = $p.CloseMainWindow()
    if (-not $p.WaitForExit(8000)) { try { $p.Kill() } catch {} ; $p.WaitForExit(5000) | Out-Null }
  }
  # ensure no stray instance holds the project lock before the next relaunch
  Get-Process FieldWorks -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Kill(); $_.WaitForExit(5000) } catch {} }
}

$rows = Import-Csv $Manifest
if ($Only) { $rows = $rows | Where-Object { $_.toolId -eq $Only } }
$results = @()
foreach ($row in $rows) {
  $img = Join-Path $Worktree $row.imagePath
  if ((Test-Path $img) -and -not $Force) { $results += [pscustomobject]@{tool=$row.toolId; status='skip-exists'}; continue }
  New-Item -ItemType Directory -Force -Path (Split-Path $img) | Out-Null
  $enc = [uri]::EscapeDataString($Project)
  $link = "silfw://localhost/link?app=flex&database=$enc&server=&tool=$($row.toolId)"
  Get-Process FieldWorks -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Kill(); $_.WaitForExit(5000) } catch {} }
  $p = Start-Process -FilePath $exe -ArgumentList $link -PassThru
  $state = Wait-Loaded $p $TimeoutSec
  $status = $state
  if ($state -eq 'loaded') {
    Start-Sleep -Milliseconds $PaintDelayMs
    $p.Refresh()
    try { [Cap]::Save($p.MainWindowHandle, $img); $status = 'captured' }
    catch { $status = "capture-failed: $($_.Exception.Message)" }
  }
  Close-Flex $p
  $results += [pscustomobject]@{tool=$row.toolId; status=$status}
  Write-Host ("{0,-40} {1}" -f $row.toolId, $status)
}
Write-Host "`n=== summary ==="
$results | Group-Object status | ForEach-Object { Write-Host ("{0,5}  {1}" -f $_.Count, $_.Name) }
