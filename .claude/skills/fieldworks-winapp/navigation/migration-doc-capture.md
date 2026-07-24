# Migration-Doc Legacy Capture Campaign

Use this route when capturing the legacy WinForms "truth" PNGs for the Phase-1 migration docs
(see the hub skill's "Phase-1 Landing Strategy"). It records the verified UIA2 capture limits
on FLEx and the two non-UIA capture routes that make a bulk campaign feasible.

**Where the docs live.** Navigate to the legacy screen in Legacy mode, capture into
`Docs/migration/images/<screen>-NN.png`, and reference it from `Docs/migration/<screen>.md`
(template at `Docs/migration/_TEMPLATE.md`). Note: `Docs/migration/` lives on the separate,
never-merged `phase1-docs` branch, not in this checkout. Capture the important states
(initial, filled, error, multi-select), not just the empty form.

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
  on the `phase1-docs` branch (this capture tooling lives there alongside the `Docs/migration/` docs
  it produces, not on the migration code branches) (run under Windows PowerShell 5.1 —
  `System.Drawing` `Bitmap` isn't available in pwsh 7; relaunch per tool + graceful `CloseMainWindow`
  to release the project lock). Captured 67/67 tool/list screens.

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
  surface's JIRA ticket**. This route owns only the legacy half — the legacy baseline
  (`visual.legacy.png`). The Avalonia capture (`visual.avalonia.png`) and the rest of the
  before/after pipeline (semantic snapshots, visual diffs, workflow evidence) are owned by
  `../../fieldworks-semantic-render-parity/SKILL.md`; the Avalonia side is added when the
  Avalonia surface exists (during the ticket's implementation). To attach PNGs to JIRA, use the
  `atlassian-skills` scripts.
