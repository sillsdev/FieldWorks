<#
.SYNOPSIS
  Option 2 (live-app) capture for MENU-LAUNCHED legacy dialogs that cannot be
  constructed headless (Gecko / live IVwRootSite / clerk-backed / bespoke objects).

  For each manifest row: launch legacy FLEx live at the row's tool (silfw link),
  drive the real WinForms menu via native System.Windows.Automation (UIA) -- expand
  each parent menu, Invoke the leaf -- then detect the modal dialog with Win32
  EnumWindows, PrintWindow-capture it, and close it read-only (WM_CLOSE = Cancel).

  Why native UIA (not winforms-mcp, not keystrokes):
   * winforms-mcp keystrokes are window-station isolated (Alt+mnemonic never lands);
   * winforms-mcp click_menu_item only walks the main-window tree, so it can't see
     the dropdown's separate top-level popup window (FLEx populates items lazily);
   * UIA InvokePattern.Invoke() is a direct accessibility call -- no focus needed.

  Invoking a control that opens a MODAL dialog makes UIA Invoke() block until the UI
  goes idle (which a modal never does) -> it times out (0x80131505). That timeout is
  EXPECTED and means the dialog opened; we then capture via Win32 (reliable while the
  modal blocks the UI thread, unlike UIA).

  Read-only: never edits the project; closes each dialog with WM_CLOSE (Cancel).
#>
[CmdletBinding()]
param(
  [string]$Worktree,
  [string]$Project   = "Sena 3",
  [string]$Manifest,
  [string]$Only,                  # capture only this row name (proof runs)
  [int]$TimeoutSec   = 150,        # max wait for project load
  [int]$PaintDelayMs = 4000,       # let the tool paint before driving menus
  [int]$DialogWaitMs = 8000,       # max wait for the modal window to appear (post-invoke)
  [switch]$Force                   # recapture even if the PNG already exists
)
$ErrorActionPreference = 'Stop'
if (-not $Worktree) { $Worktree = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
if (-not $Manifest) { $Manifest = Join-Path $Worktree 'openspec\changes\legacy-screenshot-capture\manifests\menu-dialogs.csv' }
$exe = Join-Path $Worktree 'Output\Debug\FieldWorks.exe'
if (-not (Test-Path $exe)) { throw "FieldWorks.exe not found: $exe" }

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Drawing;using System.Drawing.Imaging;using System.Text;
public struct R{public int L,T,Rt,B;}
public static class W{
  public delegate bool EnumProc(IntPtr h,IntPtr p);
  [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb,IntPtr p);
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
  [DllImport("user32.dll")] public static extern int GetWindowText(IntPtr h,StringBuilder s,int n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h,out R r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h,IntPtr hdc,uint f);
  [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h,uint msg,IntPtr w,IntPtr l);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h,int n);
  [DllImport("user32.dll")] public static extern bool AttachThreadInput(uint a,uint b,bool f);
  [DllImport("kernel32.dll")] public static extern uint GetCurrentThreadId();
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x,int y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f,uint dx,uint dy,uint d,IntPtr e);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk,byte scan,uint f,IntPtr e);
  public const uint WM_CLOSE=0x0010;
  // Real synthesized mouse input — the actual path WinForms menus expect. Unlike UIA Expand/Invoke
  // (which never truly opens FLEx's nested submenus), a real click opens a dropdown that renders and
  // stays open, so cascades work and the items become UIA-readable for the next level.
  public static void Move(int x,int y){ SetCursorPos(x,y); }
  public static void Click(int x,int y){ SetCursorPos(x,y); System.Threading.Thread.Sleep(60); mouse_event(0x0002,0,0,0,IntPtr.Zero); mouse_event(0x0004,0,0,0,IntPtr.Zero); }
  public static void RightClick(int x,int y){ SetCursorPos(x,y); System.Threading.Thread.Sleep(60); mouse_event(0x0008,0,0,0,IntPtr.Zero); mouse_event(0x0010,0,0,0,IntPtr.Zero); }
  // Defeat the Windows foreground lock so the FLEx window becomes ACTIVE -> its WinForms menu
  // dropdowns/flyouts stay open through a multi-level cascade (a non-active window's dropdowns
  // collapse instantly, which is why background-driven cascades failed).
  public static bool Foreground(IntPtr h){
    // Tapping Alt makes the OS treat our process as having just received input, which lifts the
    // foreground lock so SetForegroundWindow is honored (the documented workaround).
    keybd_event(0x12,0,0,IntPtr.Zero);        // VK_MENU down
    keybd_event(0x12,0,0x0002,IntPtr.Zero);   // VK_MENU up (KEYEVENTF_KEYUP)
    uint pp; uint tgt=GetWindowThreadProcessId(h,out pp);
    uint cur=GetCurrentThreadId();
    AttachThreadInput(cur,tgt,true);
    ShowWindow(h,5); BringWindowToTop(h); SetForegroundWindow(h);
    AttachThreadInput(cur,tgt,false);
    return GetForegroundWindow()==h;
  }
  public static List<IntPtr> Wins(uint target){var L=new List<IntPtr>();EnumWindows((h,p)=>{uint pp;GetWindowThreadProcessId(h,out pp);if(pp==target&&IsWindowVisible(h))L.Add(h);return true;},IntPtr.Zero);return L;}
  public static string Title(IntPtr h){var sb=new StringBuilder(512);GetWindowText(h,sb,512);return sb.ToString();}
  public static void Save(IntPtr h,string path){R r;GetWindowRect(h,out r);int w=r.Rt-r.L,ht=r.B-r.T;if(w<1||ht<1)throw new Exception("zero-size window");using(var b=new Bitmap(w,ht))using(var g=Graphics.FromImage(b)){IntPtr hdc=g.GetHdc();PrintWindow(h,hdc,2u);g.ReleaseHdc(hdc);b.Save(path,ImageFormat.Png);}}
  public static void Close(IntPtr h){SendMessage(h,WM_CLOSE,IntPtr.Zero,IntPtr.Zero);}
}
"@

$AE   = [System.Windows.Automation.AutomationElement]
$TS   = [System.Windows.Automation.TreeScope]
$CTRL = [System.Windows.Automation.ControlType]
$root = $AE::RootElement

function New-PidCond([int]$processId) { [System.Windows.Automation.PropertyCondition]::new($AE::ProcessIdProperty, $processId) }
function New-MenuItemCond([string]$name) {
  $cn = [System.Windows.Automation.PropertyCondition]::new($AE::NameProperty, $name)
  $ct = [System.Windows.Automation.PropertyCondition]::new($AE::ControlTypeProperty, $CTRL::MenuItem)
  [System.Windows.Automation.AndCondition]::new($cn, $ct)
}
function Get-AllTopWindows {
  for ($a = 0; $a -lt 5; $a++) { try { return $root.FindAll($TS::Children, [System.Windows.Automation.Condition]::TrueCondition) } catch { Start-Sleep -Milliseconds 150 } }
  return @()
}
# Menu-bar items live in the main window; dropdown/flyout items live in separate top-level POPUP
# windows that aren't enumerable as process children. Search popups FIRST (small/fast, before the
# flyout collapses), main window LAST (its huge subtree is the slow fallback for menu-bar items).
function Find-MenuItemAnywhere([IntPtr]$mainHandle, [string]$name) {
  $and = New-MenuItemCond $name
  $wins = Get-AllTopWindows
  $popups = @(); $mains = @()
  foreach ($w in $wins) {
    $h = [IntPtr]::Zero
    try { $h = [IntPtr]$w.Current.NativeWindowHandle } catch {}
    if ($h -eq $mainHandle) { $mains += $w } else { $popups += $w }
  }
  foreach ($w in ($popups + $mains)) { try { $hit = $w.FindFirst($TS::Descendants, $and); if ($hit) { return $hit } } catch {} }
  return $null
}
# WinForms MenuStrip items expose ExpandCollapse (top-level) / Invoke; expand is idempotent.
function Expand-Item([System.Windows.Automation.AutomationElement]$el) {
  try { $el.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand(); return $true } catch {}
  try { $el.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke(); return $true } catch {}
  return $false
}
function Get-ItemCenter([System.Windows.Automation.AutomationElement]$el) {
  try { $r = $el.Current.BoundingRectangle; if ($r.Width -lt 1 -or $r.Height -lt 1) { return $null }; return @([int]($r.X + $r.Width/2), [int]($r.Y + $r.Height/2)) } catch { return $null }
}
# Find a clickable element by normalized name inside one window (a dialog), via FromHandle.
# IMPORTANT: FLEx exposes its named buttons (OK/Cancel/"Change..."/"Manage...") as controlType **Pane**,
# not Button (only combobox dropdowns are Button) — so match by name across clickable-ish controlTypes
# (Button, Pane, MenuItem, TabItem, Hyperlink, ListItem), not just Button.
function Find-ClickableCenter([IntPtr]$hwnd, [string]$name) {
  $target = Norm $name
  $types = @($CTRL::Button, $CTRL::Pane, $CTRL::MenuItem, $CTRL::TabItem, $CTRL::Hyperlink, $CTRL::ListItem)
  try {
    $el = $AE::FromHandle($hwnd)
    foreach ($ct in $types) {
      $c = [System.Windows.Automation.PropertyCondition]::new($AE::ControlTypeProperty, $ct)
      foreach ($it in $el.FindAll($TS::Descendants, $c)) {
        try { if ((Norm $it.Current.Name) -eq $target -and $it.Current.IsEnabled) { return (Get-ItemCenter $it) } } catch {}
      }
    }
  } catch {}
  return $null
}
# topmost FLEx process window that is new (not in $seen) and has a real (non-empty, non-main) title
function Find-NewDialog([int]$processId, [hashtable]$seen, [string]$mainTitle) {
  foreach ($h in [W]::Wins([uint32]$processId)) {
    if (-not $seen.ContainsKey([int64]$h)) {
      $t = [W]::Title($h)
      if ($t -and $t.Trim().Length -gt 0 -and $t -ne $mainTitle) { return @($h, $t) }
    }
  }
  return $null
}
# FLEx sets each menu item's UIA Name to a DESPACED form ("Project Management" -> "ProjectManagement",
# "Send/Receive" -> "SendReceive"), so match on a whitespace-stripped, case-insensitive comparison rather
# than an exact Name condition (that despacing is why every multi-word menu label failed to match).
function Norm([string]$s) { if (-not $s) { return '' } ; return ($s -replace '\s', '').ToLowerInvariant() }
# Walk each of the process's visible top-level windows (main window for menu-bar items; the dropdown/flyout
# POPUP windows for nested items). Win32 EnumWindows reliably lists the popups (root::Descendants does not);
# AutomationElement.FromHandle gives each popup's small, fast UIA subtree.
function Find-MenuItemInProcWins([int]$processId, [string]$name) {
  $target = Norm $name
  $ct = [System.Windows.Automation.PropertyCondition]::new($AE::ControlTypeProperty, $CTRL::MenuItem)
  foreach ($h in [W]::Wins([uint32]$processId)) {
    try {
      $el = $AE::FromHandle([IntPtr]$h)
      foreach ($it in $el.FindAll($TS::Descendants, $ct)) {
        try { if ((Norm $it.Current.Name) -eq $target) { return $it } } catch {}
      }
    } catch {}
  }
  return $null
}
# Drive the menu with REAL mouse clicks: foreground FLEx (Alt-tap defeats the foreground lock), click the
# top-level item (opens dropdown), click each nested submenu parent (opens its flyout), click the leaf
# (invokes). Real clicks open dropdowns that genuinely render — unlike UIA Expand. Returns 'ok' or "stage:label".
function Invoke-MenuViaMouse([int]$processId, [IntPtr]$mainHandle, [string[]]$menu, [bool]$skipForeground = $false) {
  if (-not $skipForeground) {
    if (-not ([W]::Foreground($mainHandle))) { Start-Sleep -Milliseconds 200; [void][W]::Foreground($mainHandle) }
    Start-Sleep -Milliseconds 350
  }
  for ($i = 0; $i -lt $menu.Count; $i++) {
    $label = $menu[$i]
    $el = $null; $t = 0
    while (-not $el -and $t -lt 30) { $el = Find-MenuItemInProcWins $processId $label; if (-not $el) { Start-Sleep -Milliseconds 120; $t++ } }
    if (-not $el) { return "not-found:$label" }
    if ($i -eq ($menu.Count - 1)) { try { if (-not $el.Current.IsEnabled) { return "disabled:$label" } } catch {} }
    $c = Get-ItemCenter $el
    if (-not $c) { return "no-rect:$label" }
    [W]::Click($c[0], $c[1])
    Start-Sleep -Milliseconds 500
  }
  return 'ok'
}
# Find the first data row in the main window's browse/list view, to right-click for a context menu.
function Find-FirstRowCenter([IntPtr]$mainHandle) {
  $types = @($CTRL::ListItem, $CTRL::DataItem, $CTRL::TreeItem, $CTRL::Custom)
  try {
    $el = $AE::FromHandle($mainHandle)
    foreach ($ct in $types) {
      $cond = [System.Windows.Automation.PropertyCondition]::new($AE::ControlTypeProperty, $ct)
      foreach ($it in $el.FindAll($TS::Descendants, $cond)) {
        try {
          $r = $it.Current.BoundingRectangle
          if ($r.Width -gt 20 -and $r.Height -gt 5 -and $r.Y -gt 80) { return @([int]($r.X + 30), [int]($r.Y + $r.Height/2)) }
        } catch {}
      }
    }
  } catch {}
  return $null
}
function Expand-Chain([IntPtr]$mainHandle, [string[]]$labels) {
  try { [W]::Foreground($mainHandle) } catch {}   # make FLEx active so the cascade stays open
  foreach ($lbl in $labels) {
    $node = $null; $t = 0
    while (-not $node -and $t -lt 12) { $node = Find-MenuItemAnywhere $mainHandle $lbl; if (-not $node) { Start-Sleep -Milliseconds 120; $t++ } }
    if (-not $node) { return $false }
    [void](Expand-Item $node)
    Start-Sleep -Milliseconds 200
  }
  return $true
}

function Close-Flex([System.Diagnostics.Process]$p) {
  if ($p -and -not $p.HasExited) { try { $p.Kill(); [void]$p.WaitForExit(5000) } catch {} }
  Get-Process FieldWorks -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Kill(); [void]$_.WaitForExit(5000) } catch {} }
}

function Capture-One($row) {
  $img = Join-Path $Worktree $row.imagePath
  if ((Test-Path $img) -and -not $Force) { return 'skip-exists' }
  New-Item -ItemType Directory -Force -Path (Split-Path $img) | Out-Null
  $menu = @($row.menuPath -split '\|')
  $tool = $row.tool; if (-not $tool) { $tool = 'lexiconEdit' }

  Get-Process FieldWorks -ErrorAction SilentlyContinue | ForEach-Object { try { $_.Kill(); [void]$_.WaitForExit(5000) } catch {} }
  $enc  = [uri]::EscapeDataString($Project)
  $link = "silfw://localhost/link?app=flex&database=$enc&server=&tool=$tool"
  $p = Start-Process -FilePath $exe -ArgumentList $link -PassThru

  $sw = [Diagnostics.Stopwatch]::StartNew(); $loaded = $false
  while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
    Start-Sleep -Milliseconds 600
    if ($p.HasExited) { Close-Flex $p; return 'exited-during-load' }
    $p.Refresh(); if ($p.MainWindowTitle -like '*FieldWorks Language Explorer*') { $loaded = $true; break }
  }
  if (-not $loaded) { Close-Flex $p; return 'load-timeout' }
  Start-Sleep -Milliseconds $PaintDelayMs
  $mainTitle = $p.MainWindowTitle
  $mainHandle = [IntPtr]$p.MainWindowHandle

  # the snapshot of top-level windows BEFORE opening the dialog (Win32, reliable)
  $before = @{}; foreach ($h in [W]::Wins([uint32]$p.Id)) { $before[[int64]$h] = $true }

  # Optional: LEFT-click a data row first to establish a selection, so selection-gated menu items
  # (e.g. Tools->Spelling->Change Spelling on a selected wordform) become visible/enabled.
  $preClick = $null
  if ($row.PSObject.Properties['preClick']) { $preClick = $row.preClick }
  if ($preClick) {
    [void][W]::Foreground($mainHandle); Start-Sleep -Milliseconds 400
    $pc = $null; $pt = 0
    while (-not $pc -and $pt -lt 15) {
      if ($preClick -eq 'firstrow') { $pc = Find-FirstRowCenter $mainHandle } else { $pc = Find-ClickableCenter $mainHandle $preClick }
      if (-not $pc) { Start-Sleep -Milliseconds 200; $pt++ }
    }
    if ($pc) { [W]::Click($pc[0], $pc[1]); Start-Sleep -Milliseconds 600 }
  }

  # Optional: right-click a data row first to open a CONTEXT menu, then navigate $menu within it.
  $rightClick = $null
  if ($row.PSObject.Properties['rightClick']) { $rightClick = $row.rightClick }
  if ($rightClick) {
    $rc = $null; $rt = 0
    [void][W]::Foreground($mainHandle); Start-Sleep -Milliseconds 400
    while (-not $rc -and $rt -lt 15) {
      if ($rightClick -eq 'firstrow') { $rc = Find-FirstRowCenter $mainHandle }
      else { $rc = Find-ClickableCenter $mainHandle $rightClick }
      if (-not $rc) { Start-Sleep -Milliseconds 200; $rt++ }
    }
    if (-not $rc) { Close-Flex $p; return "rightclick-target-not-found: $rightClick" }
    $invoked = $false; $lastMouse = ''
    for ($attempt = 1; $attempt -le 4 -and -not $invoked; $attempt++) {
      [void][W]::Foreground($mainHandle); Start-Sleep -Milliseconds 200
      [W]::RightClick($rc[0], $rc[1]); Start-Sleep -Milliseconds 700
      $lastMouse = Invoke-MenuViaMouse $p.Id $mainHandle $menu $true   # context menu already open; don't re-foreground
      if ($lastMouse -eq 'ok') { $invoked = $true }
      elseif ($lastMouse -like 'disabled:*') { Close-Flex $p; return "context-leaf-disabled: $($menu -join '>')" }
      else { Start-Sleep -Milliseconds 300 }
    }
    if (-not $invoked) { Close-Flex $p; return "context-menu-failed ($lastMouse): $($menu -join '>')" }
  } else {
    # Drive the MAIN menu with REAL mouse clicks (opens dropdowns that genuinely render + cascade, unlike UIA
    # Expand which never opens FLEx's nested submenus). Retry the whole path a few times.
    $invoked = $false; $lastMouse = ''
    for ($attempt = 1; $attempt -le 4 -and -not $invoked; $attempt++) {
      $lastMouse = Invoke-MenuViaMouse $p.Id $mainHandle $menu
      if ($lastMouse -eq 'ok') { $invoked = $true }
      elseif ($lastMouse -like 'disabled:*') { Close-Flex $p; return "leaf-disabled (needs context): $($menu -join '>')" }
      else { Start-Sleep -Milliseconds 300 }
    }
    if (-not $invoked) { Close-Flex $p; return "menu-invoke-failed ($lastMouse): $($menu -join '>')" }
  }

  # find the NEW top-level window (the modal dialog) via Win32 -- works while modal blocks UI thread
  $dlg = [IntPtr]::Zero; $dtitle = ''
  $sw2 = [Diagnostics.Stopwatch]::StartNew()
  while ($sw2.Elapsed.TotalMilliseconds -lt $DialogWaitMs -and $dlg -eq [IntPtr]::Zero) {
    Start-Sleep -Milliseconds 300
    foreach ($h in [W]::Wins([uint32]$p.Id)) {
      if (-not $before.ContainsKey([int64]$h)) {
        $t = [W]::Title($h)
        # a real dialog has a non-empty title that isn't the main window; menu/dropdown
        # popups have empty titles -> reject them so we never capture a leftover dropdown.
        if ($t -and $t.Trim().Length -gt 0 -and $t -ne $mainTitle) { $dlg = $h; $dtitle = $t; break }
      }
    }
  }
  if ($dlg -eq [IntPtr]::Zero) { Close-Flex $p; return 'no-dialog' }

  # Optional: reach a SUB-dialog by clicking button(s) INSIDE the opened dialog (pipe-separated names).
  # e.g. Configure Dictionary -> "Manage Layouts..." opens the configuration-manager sub-dialog.
  $postClicks = $null
  if ($row.PSObject.Properties['postClicks']) { $postClicks = $row.postClicks }
  if ($postClicks) {
    $seen = @{}; foreach ($h in [W]::Wins([uint32]$p.Id)) { $seen[[int64]$h] = $true }  # parent dialog now counts as seen
    $cur = $dlg
    foreach ($btn in @($postClicks -split '\|')) {
      Start-Sleep -Milliseconds 600
      [void][W]::Foreground($cur)
      $c = $null; $bt = 0
      while (-not $c -and $bt -lt 20) { $c = Find-ClickableCenter $cur $btn; if (-not $c) { Start-Sleep -Milliseconds 150; $bt++ } }
      if (-not $c) { try { [W]::Close($dlg) } catch {} ; Close-Flex $p; return "subbutton-not-found: $btn" }
      [W]::Click($c[0], $c[1])
      $sub = $null; $st3 = [Diagnostics.Stopwatch]::StartNew()
      while ($st3.Elapsed.TotalMilliseconds -lt $DialogWaitMs -and -not $sub) {
        Start-Sleep -Milliseconds 300
        $sub = Find-NewDialog $p.Id $seen $mainTitle
      }
      if (-not $sub) { try { [W]::Close($dlg) } catch {} ; Close-Flex $p; return "subdialog-not-opened after: $btn" }
      $cur = [IntPtr]$sub[0]; $dtitle = $sub[1]; $seen[[int64]$cur] = $true
    }
    $dlg = $cur
  }

  Start-Sleep -Milliseconds 1200   # let it finish painting
  $status = 'no-dialog'
  try { [W]::Save($dlg, $img); $status = "captured: '$dtitle'" } catch { $status = "capture-failed: $($_.Exception.Message)" }
  try { [W]::Close($dlg) } catch {}
  Start-Sleep -Milliseconds 400
  Close-Flex $p
  return $status
}

$rows = Import-Csv $Manifest
if ($Only) { $rows = $rows | Where-Object { $_.name -eq $Only } }
$results = @()
foreach ($row in $rows) {
  $st = 'error'
  try { $st = Capture-One $row } catch { $st = "exception: $($_.Exception.Message)" }
  $results += [pscustomobject]@{ name = $row.name; status = $st }
  Write-Host ("{0,-32} {1}" -f $row.name, $st)
}
Write-Host "`n=== summary ==="
$results | Group-Object { ($_.status -split ':')[0] } | ForEach-Object { Write-Host ("{0,4}  {1}" -f $_.Count, $_.Name) }
