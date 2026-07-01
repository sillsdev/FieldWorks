---
name: fieldworks-winapp
description: >
  Control and document the FieldWorks desktop application with WinForms MCP or WinApp MCP.
  Use this skill whenever a task requires launching FieldWorks, restoring or
  opening a FieldWorks project, walking WinForms UI flows, collecting manual
  screenshots, reproducing UI bugs, or verifying a fix inside the live FLEx
  application.
license: MIT
compatibility: >
  Requires a Windows/x64 FieldWorks workspace. Prefer WinForms MCP
  for hidden-desktop UIA2 automation when available; use WinApp MCP as the UIA3
  fallback and for visible desktop/window diagnostics.
metadata:
  author: FieldWorks team
  version: "1.0"
---

# FieldWorks WinForms/WinApp Automation

Use this skill to launch, inspect, navigate, and capture evidence from the
FieldWorks Language Explorer desktop app through WinForms MCP or WinApp MCP.
Keep the workflow grounded in the live UI tree: inspect first, interact second,
capture evidence after the target state is visible.

**Scope: legacy WinForms only.** This skill always runs FieldWorks in **Legacy
(WinForms) UI mode** to scrape the legacy surface for "truth" screenshots,
workflows, and behaviour (the migration parity baseline). The WinForms UIA2 MCP
can only see WinForms — the Avalonia (New) surface is captured by a SEPARATE
headless skill, never through this MCP. ALWAYS run
`scripts/Set-FieldWorksLegacyMode.ps1` before launching (see Setup First).

This skill is intentionally organized as a small index plus route-specific
navigation files. Read only the route files needed for the task.

**Migration-doc screenshots.** A common task is capturing the legacy "truth" PNGs for the
Phase-1 migration docs (see the hub skill's "Phase-1 Landing Strategy"). Navigate to the
legacy screen in Legacy mode, capture into `Docs/migration/images/<screen>-NN.png`, and
reference it from `Docs/migration/<screen>.md` (template at `Docs/migration/_TEMPLATE.md`).
Capture the important states (initial, filled, error, multi-select), not just the empty form.

**Capture limits observed with winforms-mcp (UIA2) on FLEx — read before a bulk campaign:**
- **Main-window / dialog-window capture works** — `winforms_take_screenshot` via PrintWindow
  renders the active view and any open modal dialog correctly (verified on Sena 3).
- **The left area/tool navigator is a custom `SilSidePane`/`OutlookBar`** and is NOT exposed to
  UIA2 — `find_element` by area/tool name ("Lexicon", "Concordance", …) returns "not found",
  and `click_element` needs a cached UIA elementId (there is no coordinate-click). So **you
  cannot switch areas or tools through winforms-mcp.** Use WinApp MCP (UIA3) or a visible
  desktop + manual clicks for tool/list screens.
- **Menu popups are flaky:** dropdowns don't appear in PrintWindow captures (rendered on a
  separate layer), submenu enumeration via `get_element_tree` is partial/lazy, and menu state
  is fragile (a top-level item can intermittently report "not found" after a prior open). Open
  *dialogs* by passing the full `menuPath` to `click_menu_item` and screenshot the resulting
  window — don't rely on screenshotting the menu itself.
- **Consequence for the migration-doc campaign:** bulk PNG capture of every screen is not
  feasible through winforms-mcp navigation alone. Two non-UIA routes solve it (built under the
  `legacy-screenshot-capture` OpenSpec change) — prefer these over fighting the UIA tree:

  **Tool/list screens — launch-per-tool link (no UIA navigation).** Launch FLEx pointed straight
  at a tool via a guid-less silfw link, then PrintWindow the main window:
  `FieldWorks.exe "silfw://localhost/link?app=flex&database=<proj>&server=&tool=<toolId>"`
  (sole arg; **omit `guid`** — an empty `guid=` crashes `FwLinkArgs` parsing; `LinkListener`
  switches tools regardless of guid). Script: `scripts/migration-capture/Capture-LegacyTools.ps1`
  (run under Windows PowerShell 5.1 — `System.Drawing` `Bitmap` isn't available in pwsh 7; relaunch
  per tool + graceful `CloseMainWindow` to release the project lock). Captured 67/67 tool/list screens.

  **Dialogs — piggyback an in-memory-cache test fixture (no standalone exe).** A standalone capture
  exe needs its own reg-free COM manifest + cache bootstrap and per-dialog `Mediator`/`PropertyTable`/
  `CmObjectUi`. Instead add an `[Explicit]` NUnit fixture to a test project whose
  `MemoryOnlyBackendProviderRestoredForEachTestTestBase` already bootstraps reg-free COM + `LcmCache`:
  each capture is a `[Test]` that seeds objects, constructs the dialog (ctor + `SetDlgInfo`), and
  renders with `Control.DrawToBitmap`. Example: `LexTextControlsTests/ScreenshotHarnessTests.cs`; run
  `.\test.ps1 -SkipNative -TestProject <Proj> -TestFilter "FullyQualifiedName~ScreenshotHarness"`.
  Per-dialog wiring is bespoke (each `SetDlgInfo` differs), so grow it as each dialog's ticket is worked.

  Conditional dialogs (delete/restore/warning) and Views/Gecko-coupled dialogs (need a live
  `IVwSelection`) are still captured on the workflow that triggers them — i.e. on JIRA pickup.

  **Harness reality (encode this — verified to root cause):** the harness renders **simple
  tree/list/feature dialogs** headless (proven: feature-chooser, MSA-inflection-feature-list, across
  both an in-memory and a Sena-3-copy data flavor via `CaptureContext`). **Dialogs that embed a live
  "matching entries" / search-browse (or any live XMLView)** — InsertEntryDlg, AddNewSenseDlg, the
  GoDlg family (Merge/Link/EntryGo) — **cannot be captured headless, and a full real project does NOT
  fix them** (data/stylesheet is never the blocker): `EntryGoDlg.InitializeMatchingObjects` reads the
  app's `"WindowConfiguration"` XML and builds an XMLView + `SearchEngine` that needs a running message
  loop, so headless they NRE or show a modal off-screen and HANG. **Do not attempt them in the
  harness** — capture them **live** from the running app (they're reachable) or on JIRA pickup. Rule
  of thumb: feature/list/tree dialog → harness; matching-entries/search-browse dialog → live. Each
  `Cap(...)` is wrapped so one failure never blocks the batch.
- **Two durable harness mechanics (encode):** (1) **Stop `Debug.Assert` modal popups** during capture
  by clearing `System.Diagnostics.Trace.Listeners` (save + restore in `finally`) — this also makes
  asserts no-op so dialogs proceed past benign ones, and real failures surface as catchable exceptions
  you log (never popups). (2) Provide `WindowConfiguration` (the `<window>` node) via
  `XWindow.LoadConfigurationWithIncludes(GetCodeFile("Language Explorer/Configuration/Main.xml"), true)`
  and a minimal `IApp` stub under the `"App"` key — these clear the first NRE/assert in any dialog
  that touches the layout/XmlVc path. (3) A message-loop `CapLoop` (`Application.Run` + a Forms.Timer
  that captures-then-closes + a watchdog) handles app-coupled dialogs that init asynchronously — but
  **does NOT** help ones (matching-entries family) whose `SetDlgInfo` builds a `BrowseViewer`
  synchronously and blocks the thread; those are an unfixable headless wall → live-capture.

  **Before/after pairing (the deliverable shape).** Each migration doc shows the legacy "before"
  and the Avalonia "after" of the SAME seeded data, side by side, and **both PNGs attach to the
  surface's JIRA ticket** (`Docs/migration/_TEMPLATE.md` layout: `<name>-before.png` /
  `<name>-after.png` in the doc's `images/`). The **after** comes from the Avalonia visual test for
  that surface (`FwAvaloniaDialogsTests`/`FwAvaloniaTests`) — the `fieldworks-semantic-render-parity`
  lane — rendered from the same data the harness seeds; it is added when the Avalonia surface exists
  (during the ticket's implementation). To attach to JIRA, use the `atlassian-skills` scripts.

## Setup First (if `winforms_*` tools are missing)

If `ToolSearch "winforms"` finds no tools, the MCP server is not registered for
**Claude Code** — most often because it is only in `.vscode/mcp.json` (VS Code's
file), not the repo-root `.mcp.json` (Claude Code's file). Read
`references/mcp-setup.md`, then:

1. Ensure repo-root `.mcp.json` registers `winforms-mcp` (committed here; the
   Windows-robust `cmd /c npx` form).
2. Run `scripts/Preflight-WinFormsMcp.ps1` to confirm node/npx, the package, and
   a built `Output/Debug/FieldWorks.exe`.
3. **Reconnect/restart Claude Code** (MCP servers load at session start) and
   **approve** the project server when prompted. Confirm with `claude mcp list`
   and `ToolSearch "winforms"`.

The MCP **client** launches the server — no script starts it; the preflight only
verifies that launch will succeed.

## Core Rules

- Prefer WinForms MCP UIA2 tools for fresh FieldWorks runtime automation because
  FieldWorks is currently WinForms and the workspace config runs WinForms MCP in
  hidden-desktop mode.
- Use WinApp MCP UIA3 tools when WinForms MCP is unavailable, when a task needs
  visible desktop/window diagnostics, or when UIA2 cannot see the target
  surface.
- Prefer UI Automation IDs and names over coordinates. Use coordinates only
  after snapshots and element searches fail.
- Always inspect the current tree with `winforms_get_element_tree`,
  `mcp_winapp_get_snapshot`, or targeted element search before assuming a
  dialog structure.
- Treat FieldWorks UI automation as stateful. After opening menus, dialogs, or
  popups, re-query the snapshot or element list.
- Capture screenshots into a task-specific evidence folder before closing the
  app or dialog.
- Close evidence-only dialogs with `Cancel` or `Escape` unless the user asked
  to change project data.
- Do not globally register COM or add registry hacks. FieldWorks uses reg-free
  COM from the build output.

## MCP Selection

- Default to WinForms MCP for launch/test/screenshot flows. Use
  `winforms_launch_app`, `winforms_wait_for_element`,
  `winforms_get_element_tree`, `winforms_find_element`,
  `winforms_click_element`, `winforms_type_text`, `winforms_set_value`,
  `winforms_select_item`, `winforms_click_menu_item`, and
  `winforms_take_screenshot`.
- Use WinForms MCP headless-safe operations only. Avoid `winforms_send_keys`,
  drag/drop, and double-clicks on hidden-desktop processes.
- Use WinApp MCP for visible desktop/window diagnostics, UIA3 comparison,
  fallback screenshots, or controls that WinForms MCP cannot operate.
- Read `references/mcp-selection.md` before changing a route from one MCP driver
  to the other.

## How This Skill Is Organized

- `SKILL.md`: trigger metadata, global safety rules, and route index.
- `navigation/*.md`: one goal-oriented navigation path per file. These are the
  FieldWorks equivalent of Page Objects: each file owns the menu path,
  automation IDs, verification cues, and safe exit path for one user-facing
  destination or workflow.
- `references/how-to-update.md`: rules for adding or revising navigation paths.
- `references/mcp-selection.md`: rules for choosing WinForms MCP or WinApp MCP.
- `references/mcp-setup.md`: enabling the `winforms_*` tools for Claude Code (`.mcp.json`, reconnect).
- `references/research.md`: source-backed rationale for this structure.
- `scripts/Preflight-WinFormsMcp.ps1`: pre-session check (node/npx, package, build, `.mcp.json`).
- `scripts/Set-FieldWorksLegacyMode.ps1`: forces `UIMode=Legacy` — run before EVERY launch.
- `scripts/Resolve-FieldWorksDevRegistry.ps1`: aligns the dev registry (`RootCodeDir`/`RootDataDir`) to
  this worktree before launch; auto-realigns when the other worktree is idle, else prints `RESULT=ASK_USER`.
- `scripts/Install-VirtualDisplayDriver.ps1`: bootstraps the Virtual Display Driver (for the invisible
  off-screen-monitor path; see `references/headless-rendering.md`).
- `scripts/Launch-FieldWorksInvisible.ps1`: the default launcher for FieldWorks WinForms capture on this
  dev machine — launches on the console desktop and moves the window onto the VDD virtual monitor so it
  renders but stays invisible. Wraps `Set-FieldWorksLegacyMode.ps1` + `Resolve-FieldWorksDevRegistry.ps1`;
  requires `Install-VirtualDisplayDriver.ps1` to have been run once.
- `references/headless-rendering.md`: why FieldWorks needs a display-bound desktop; what works/doesn't for
  invisible capture (winforms-mcp HEADLESS does NOT render FieldWorks; use visible, or VDD off-screen, or RDP).

When a task names a destination, read the matching navigation file. When a task
discovers a better route or a fragile selector, read and update
`references/how-to-update.md` before changing the route file.

## Navigation Path Index

- Launch or attach to FieldWorks: `navigation/launch-or-attach.md`
- Confirm or restore a sample project: `navigation/project-loading.md`
- Writing System Properties > Font tab: `navigation/writing-system-font-options.md`
- Styles dialog > Font tab: `navigation/styles-font-tab.md`
- Manual screenshot evidence: `navigation/screenshot-evidence.md`
- WinForms ↔ Avalonia parity capture: `navigation/winforms-avalonia-parity.md`
- Enable the MCP tools for Claude Code: `references/mcp-setup.md`
- MCP driver selection: `references/mcp-selection.md`

## Screenshot Evidence

For evidence tasks, read `navigation/screenshot-evidence.md`. Prefer committed
OpenSpec evidence only when the screenshot should be part of review history;
otherwise use `Output/ManualEvidence/<ticket-or-change-id>/`.

For target detection, descriptive filenames, inline review, annotation, and
retake quality gates, also use `../smart-screenshot-capture/SKILL.md`.

## How To Update This Skill

Keep improving this skill as you explore FieldWorks and discover the most
efficient ways to do things. When you find a reliable automation ID, keyboard
route, menu path, restore flow, modal-dialog workaround, or screenshot trick,
update the relevant route file in the same change set or propose the update to
the user. Prefer short, verified notes over broad guesses.

Use `references/how-to-update.md` for the exact update checklist. In short:

- add one navigation file per distinct destination or workflow;
- keep each file task-focused and action-oriented;
- record stable automation IDs, entry state, verification cues, and exit path;
- move shared rules back to this index only when at least two route files need
  the same guidance;
- remove or revise stale routes when WinApp snapshots prove they changed.
