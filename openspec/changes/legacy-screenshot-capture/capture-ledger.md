# Capture ledger — legacy-screenshot-capture

Reconciles every reachable Phase-1 capture target to its capture status. Data: Sena 3, UIMode=Legacy.

## Option 2 — launch-per-tool (tool + list screens)
- Script: `scripts/migration-capture/Capture-LegacyTools.ps1` (run under Windows PowerShell 5.1).
- Manifest: `manifests/tools.csv` (67 targets). Log: `manifests/tools-capture.log`.
- **Result: 67 / 67 captured** (64 in the sweep + 3 captured during proof: lexiconBrowse, concordance,
  interlinearEdit). PNGs in `Docs/migration/tools/images/` and `Docs/migration/lists/images/`,
  wired into every tool/list doc.
- Recipe (proven): `FieldWorks.exe "silfw://localhost/link?app=flex&database=Sena%203&server=&tool=<toolId>"`
  — sole argument, **no `guid` param** (an empty guid crashes `FwLinkArgs` parsing; the link still
  switches tools because `LinkListener` publishes `SetToolFromName` regardless of guid).

## Option 3 — dialog screenshot harness (piggyback test fixture)
- Fixture: `Src/LexText/LexTextControls/LexTextControlsTests/ScreenshotHarnessTests.cs`
  (`[Explicit]` + `[Category("ScreenshotHarness")]`; in-memory cache base = reg-free COM + LcmCache
  already bootstrapped). Run: `.\test.ps1 -SkipNative -TestProject LexTextControlsTests -TestFilter "FullyQualifiedName~ScreenshotHarness"`.
- Output is `<name>-before.png`; the Avalonia `<name>-after.png` comes from the parity visual test
  (see the before/after requirement). Each `Cap(...)` is wrapped so one failure never blocks the batch.

**Captured headless (the feature-structure-tree family — no FwTextBox/main-window dependency):**
- `feature-chooser` → `Docs/migration/images/feature-chooser-before.png` (`FeatureSystemInflectionFeatureListDlg`;
  the same feature-tree control as the two production dialogs the feature-chooser doc covers).
- `msa-inflection-feature-list` → `Docs/migration/dialogs/images/msa-inflection-feature-list-before.png`
  (sibling of feature-chooser; same doc family).

  Both flavors proven: `feature-chooser` via the in-memory base, `msa-inflection-feature-list` via the
  Sena 3 temp copy. The `CaptureContext` builds the app context (Mediator, PropertyTable, a real
  stylesheet carried on a fake "window" form, a non-null help provider).

**LIVE-CAPTURE / on-pickup (investigated to root cause; NOT headless-capturable):** `insert-entry`,
`add-new-sense`, `merge-entry`, `link-msa`, `entry-go` (the GoDlg family). **The full Sena 3 project +
real stylesheet did NOT unblock them — data/stylesheet was never the cause.** They embed the live
"matching entries" search-browse: `EntryGoDlg.InitializeMatchingObjects` reads the app's
`"WindowConfiguration"` XML and builds an XMLView + `SearchEngine`, which needs a real message loop.
Headless they NRE (no `WindowConfiguration`) or, given more context, show a modal off-screen and HANG
(observed: the off-screen `Show()` blocked for minutes). Faking that is reconstructing the app —
out of scope for throwaway tooling. **Capture these live** (they are reachable in the running app) or
when their JIRA ticket is implemented. `delete-confirmation`/`merge-object`/`RelatedWords` additionally
need the **FdoUi** assembly (not referenced by LexTextControlsTests) — same fixture pattern in an
FdoUi-referencing test project.

**Rule of thumb (encode):** simple tree/list/feature dialogs capture headless via the harness;
**dialogs that embed a matching-entries/search-browse (or any live XMLView) must be captured live**.
Adding a headless-capturable dialog = one `Cap(...)` line (seed objects + ctor + `SetDlgInfo`).

**Message-loop harness attempt (option 2) — worked past 3 barriers, then hit a hard wall on the
matching-entries family:**
1. NRE `WindowConfiguration` null → FIXED: load it like `LexEntryUi.EnsureWindowConfiguration`
   (`XWindow.LoadConfigurationWithIncludes(Main.xml).SelectSingleNode("window")`), set in CaptureContext.
2. `Debug.Assert` **modal popups** on screen → FIXED: clear `Trace.Listeners` during capture
   (restored after). Stops the popups AND makes asserts no-op so dialogs proceed; real failures then
   log as catchable traces, never popups. (Also added a minimal `IApp` stub for the `app != null` assert.)
3. With asserts no-op'd, `MatchingObjectsBrowser` builds a full `BrowseViewer` **synchronously inside
   `SetDlgInfo`** and **blocks the thread** (the message loop never pumps, so the watchdog can't close
   it; one hang kills the batch). This needs the real app (RecordClerk, etc.). **HARD WALL — these stay
   live-capture/on-pickup.** `CapLoop` (message-loop + watchdog) remains in the harness for future
   app-coupled dialogs that DON'T block synchronously.
The harness is popup-free and hang-free (the matching-entries family is not executed). Captures so far:
feature-chooser, msa-inflection-feature-list.

**Coverage reality for "all dialogs":** the harness reliably captures **simple** dialogs (no
matching-entries/XMLView). Reaching all ~130 needs per-dialog `Cap(...)` wiring across the dialog
assemblies (LexTextControls done here; FdoUi/FwCoreDlgs/xWorks via the same fixture in their own test
projects — the shared-helper-lib rollout). The matching-entries family (~5) is permanently on-pickup.

**BREADTH ENGINE (reflection sweep) — 34 dialogs captured + 33 wired into docs.** Instead of
hand-wiring ~130 dialogs, `ReflectSweep(dll)` loads each dialog assembly
(`LexTextControls`/`FwCoreDlgs`/`FdoUi`/`xWorks`), finds every constructable `Form` ending Dlg/Dialog,
matches its ctor + `SetDlgInfo` params from the `CaptureContext`, and captures each on its own timed
STA thread (hang → timeout → logged, sweep continues). The matching-entries family (`BaseGoDlg`
subclasses + InsertEntry/AddNewSense) is auto-skipped → live-capture. `Wire-BeforePngs.ps1` then matches
each `<class-kebab>-before.png` to the doc whose declared **legacy class** kebabs the same (so
`fw-proj-properties` → `project-properties.md`), moves the PNG into that doc's `images/`, and inserts the
before/after block.
- **Sweep (80 Forms attempted): 35 captured, ~32 dialog docs wired** (project-properties, font, backup,
  find-replace, master lists, custom-field, import/export wizards, add-converter, conflicting-save,
  add-new-user, choose-lang-project, occurrence, …) — matched to docs by declared legacy class.
- **35 is the empirical ceiling of GENERIC reflection capture** (confirmed across 5 sweeps): pushing
  `ArgFor` harder — empty arrays, hvo-named ints → real hvo, `XmlNode` → WindowConfiguration — *regressed*
  ~35→24 (dialogs that worked with null/0 broke on the "smarter" value). Reverted; kept only the safe
  `CmObjectUi`/specific-LCModel-type matches. Beyond 35, each dialog needs **bespoke domain objects**
  (msa-creator→`SandboxGenericMSA`+`IPersistenceProvider`; restore-project→`BackupFileSettings`;
  phon-feature→`IPhRegularRule`; merge-ws→a ws + ws-list; fw-delete-project→on-disk project list) or the
  **live app** (matching-entries family; Gecko-backed dictionary-config/export) — not a common fix.
- **Hand-wired bespoke path proven:** `msa-creator` (needs `IPersistenceProvider` + `SandboxGenericMSA`)
  captured by a hand-written `Cap(...)` — so any remaining bespoke dialog CAN be added one-by-one with
  its specific domain objects (in its own assembly's test project), it's just per-dialog work.
- **Pumping-loop capture (8th barrier):** `Cap` now runs each dialog under `Application.Run` + a
  capture-then-close timer on its timed STA thread, so dialogs that init ASYNCHRONOUSLY finish painting
  (Show()+DoEvents left them half-built). Net +2 (e.g. `lex-options`, previously a hang). Dialogs that
  block SYNCHRONOUSLY in SetDlgInfo (matching-entries; Gecko dictionary-config; clerk-backed export)
  still hang → timeout-isolated → live-only.
- **Expanded definition (tabs + populated) + ctor-only chrome breadth — major jump:** **65 dialogs
  captured / 99 attempted, 59 dialog docs wired**, plus **18 per-tab snapshots** across 5 tabbed dialogs
  (writing-system-setup ×7, options ×4, project-properties ×3, add-converter ×3, find-replace).
  - **Two-pass**: Pass A full `SetDlgInfo` over **Sena 3** (populated lists/grids); Pass B **ctor-only
    chrome** for the rest (construct the dialog, skip the hanging `SetDlgInfo`, capture the layout) — this
    is what unlocked the formerly-hanging export/dictionary-config/backup/custom-list/confirm-delete set.
  - **Per-tab**: `Save` walks `TabControl`s, selects each `TabPage`, captures `<name>-tab-<tab>.png`.
  - **Popups fixed**: the WinForms "COM separated from RCW" unhandled-exception dialog (thrown when a
    Views dialog disposes cross-apartment, AFTER its shot) is swallowed via an `Application.ThreadException`
    handler + `CatchException` mode — no modal, no block, capture preserved.
  - Tuning: capture delay 1.2s, ctor-only timeout 6s / full 10s — keeps the run under the 15-min vstest cap.
### Uncaptured dialogs — per-dialog blocker (from constructor/SetDlgInfo exceptions; option 2 = headless construct, so these need the live app or bespoke objects)
| Dialog | Blocker (exact) | Disposition |
|---|---|---|
| ~~fw-styles~~ | ~~NRE `FillStyleTable`; ctor needs `IVwRootSite`~~ | **CAPTURED LIVE** (Format→Styles…) — option 2b below |
| ~~insert-record~~ | ~~"Failed to compute height of an FwTextBox"~~ | **CAPTURED LIVE** (Insert→Record) — option 2b below |
| ~~restore-project~~ | ~~ctor needs `BackupFileSettings`~~ | **CAPTURED LIVE** (File→Project Management→Restore a Project…) — option 2b |
| ~~fw-delete-project~~ | ~~NRE enumerating on-disk projects~~ | **CAPTURED LIVE** (File→Project Management→Delete Project…) — option 2b |
| ~~dictionary-configuration~~ | ~~ctor: "requires GeckoWebBrowser … Xpcom.IsInitialized=False"~~ | **CAPTURED LIVE** (Tools→Configure→[1st `{0}`]) — Gecko preview rendered real data — option 2b |
| xml-diagnostics | ctor takes `GeckoElement` | Gecko → live |
| sfm-to-texts-and-words-mapping | `MissingManifestResourceException` (designer resource) | live cascade (File→Import→…) — attemptable |
| fw-apply-style | greyed unless live text selection (verified live) | live; needs editing context |
| add-list / configure-list / picture-properties / valid-characters | ctor hangs (even ctor-only timed out) | live; sub-dialog or menu cascade |
| merge-writing-system | ctor wants `IEnumerable<WSListItemModel>` (internal model) | sub-dialog of WS-properties (captured as parent) |
| merge-object | `InitBrowseView` needs a populated candidate list (XMLView) | live/bespoke |
| missing-old-field-works | ctor needs `RestoreProjectSettings` | live; only appears mid-restore |
| phonological-feature-chooser | SetDlgInfo needs `IPhRegularRule` (none in Sena 3) | bespoke data |
| dictionary-config-mgr | NRE `LoadDataFromInventory(XmlNode)` (specific inventory node) | live cascade (Tools→Configure→…) — attemptable |

These were verified, not assumed — each fails in its **constructor** (or a Views/Gecko/XMLView init), so a
headless harness cannot produce them. Captured via per-dialog override this round: **fw-chooser, utility**.

## Option 2b — LIVE-APP menu-launched capture (native UIA + Win32) — unblocks the headless-impossible set
- Script: `scripts/migration-capture/Capture-MenuDialogs.ps1`; manifest `manifests/menu-dialogs.csv`
  (run under Windows PowerShell 5.1). Full technique recorded in memory `fieldworks-live-menu-dialog-capture`.
- **Mechanism (single-level)**: launch FLEx live at the row's tool (guid-less silfw link, same as option 2),
  find the top-level `MenuItem` via native `System.Windows.Automation`, `Invoke()` the leaf. Invoking a
  modal-opening item makes UIA `Invoke()` **time out (0x80131505)** because the UI never goes idle — that
  timeout is the success signal. Capture the modal with **Win32 `EnumWindows`+`PrintWindow`** (UIA hangs
  while the modal blocks the UI thread); close read-only with `WM_CLOSE` (= Cancel).
- **Mechanism (multi-level cascades — the part that took 7 attempts):** UIA `Expand`/`Invoke` does NOT open
  FLEx's nested dropdowns. The working recipe is **real synthesized mouse input** on a **genuinely
  foregrounded** window:
  1. **Defeat the foreground lock** — a background process's `SetForegroundWindow` is ignored; tapping a
     synthetic **Alt key** (`keybd_event` VK_MENU) first makes the OS honor it (verified: foreground match
     False→True). Without this the clicks land on whatever window is actually on top (e.g. VS Code).
  2. **Real mouse click** (`SetCursorPos`+`mouse_event`) at each item's screen rect opens a dropdown that
     genuinely renders (the popup appears as a new top-level window) — unlike UIA Expand.
  3. **Find items via Win32 `EnumWindows` → `AutomationElement.FromHandle`** per popup window
     (`root::Descendants` never lists the popups).
  4. **Match on a whitespace-normalized name** — FLEx sets each item's UIA `Name` to a DESPACED form
     ("Project Management" → `ProjectManagement`, "Send/Receive" → `SendReceive`); exact-name matching is why
     every multi-word label failed for 6 attempts.
- **CAPTURED — 14 dialogs, all previously headless-impossible or uncaptured** (manifests
  `menu-dialogs.csv` + `menu-dialogs-batch{2..5}.csv`):
  - `fw-styles` (Format→Styles…) — full styles list + 5 tabs. → `styles.md`.
  - `insert-record` (Insert→Record) — "New Record". → `insert-record-dlg.md`.
  - `restore-project` (File→Project Management→Restore a Project…). → `restore-project.md`.
  - `fw-delete-project` (File→Project Management→Delete Project…). → `delete-project.md`.
  - `dictionary-configuration` (Tools→Configure→[1st `{0}`]) — layout tree + **Gecko preview with real Sena 3
    data** ("kula V crecer"); the Gecko the headless ctor demanded is initialized in the running app. → `dictionary-configuration.md`.
  - `restore-defaults` (Tools→Configure→Restore Defaults…). → `restore-defaults-dlg.md`.
  - `configure-interlin` (Tools→Configure→Interlinear… "Configure Interlinear Lines"). → `configure-interlin-dialog.md`.
  - `try-a-word` (Parser→Try a Word…). → `try-a-word-dlg.md`.
  - `parser-parameters` (Parser→Edit Parser Parameters…). → `parser-parameters-dlg.md`.
  - `interlinear-import` (File→Import→FLExText Interlinear…). → `interlinear-import-dlg.md`.
  - `sfm-to-texts-and-words-mapping` (File→Import→Standard Format Words and Glosses…) — was the
    `MissingManifestResourceException` headless blocker; renders fine live. → `sfm-to-texts-and-words-mapping-dlg.md`.
  - `interlinear-export` (File→Export Interlinear…). → `interlinear-export-dialog.md`.
  - `export-dialog` (File→Export… — base lexicon export chooser). → `export-dialog.md`.
  - `help-about` (Help→About Language Explorer…). → `help-about.md`.
  - `dictionary-configuration-manager` — **in-modal sub-dialog**: open Configure Dictionary, then click the
    **Manage Layouts…** button (postClicks). Populated "Manage Dictionary Layouts" (layouts + publications). → `dictionary-configuration-manager.md`.
  - (`notebook-export` existing headless capture re-wired to `notebook-export.md`.)
- **KEY: FLEx dialog buttons are exposed to UIA as controlType `Pane`, not `Button`** (only combobox
  dropdowns are `Button`). The in-modal `postClicks` matcher must search Name across {Button, **Pane**,
  MenuItem, TabItem, Hyperlink, ListItem} — once it did, the button-launched sub-dialog set opened up
  (dict-config-manager captured via "Manage Layouts…"). This corrects the earlier (wrong) conclusion that
  "FLEx dialog buttons aren't UIA-discoverable" — they are, just as Pane.
- **Context-gated leaves detected & reported `IsEnabled=false`** (need a specific tool/selection/file —
  capturable live from the right context on pickup): `lingua-links-import` (needs a LinguaLinks file),
  `discourse-export` (needs an active chart), `configure-list`/`add-list` (custom-list tool),
  `merge-entry` (selected entry; already has a capture), `fw-apply-style` (text selection).
  `filter-texts`/`import-word-set` use `defaultVisible=false` items not present in the tools tried.
- **Read-only preserved**: each dialog opened and was closed with `WM_CLOSE` (= Cancel); no project was
  restored/deleted (verified: restore showed "No project backups found"; delete's Delete button stayed disabled).
- **Remaining floor (still genuinely uncaptured):**
  - **Parent→child sub-dialogs** (valid-characters, merge-writing-system are buttons *inside* the
    Writing-System-Properties dialog — itself captured headless as `writing-system-setup`, 7 tabs;
    overwrite-existing-project only appears mid-restore).
  - **Context-gated leaves** (greyed unless the right tool/selection is active, so unattended capture skips
    them — the script detects `IsEnabled=false` and reports it): `configure-list`/`add-list` (Tools→Configure→
    List… — needs a custom-list tool), `merge-entry` (Tools→Merge with entry… — needs an entry selected),
    `fw-apply-style` (Format→Apply Style — needs a text selection). Capturable live from the right context on pickup.
  - **Button sub-dialogs reachable via `postClicks`** (now working with Pane matching) but needing TAB
    navigation first: valid-characters & merge-writing-system are buttons on non-default tabs of the
    Writing-System-Properties dialog; the WS-Props tabs are a Win32 `SysTabControl` not cleanly exposed as
    UIA `TabItem`, so selecting the right tab needs a coordinate click — a per-dialog discovery step left for pickup.
  - **Flow-only dialogs**: overwrite-existing-project (appears only mid-restore), update-report (only on update),
    sfm-to-texts later wizard pages — require performing the actual (destructive/stateful) action.
  - **xml-diagnostics**: hidden Gecko diagnostic, no standard menu entry.

- **Total: ~150 legacy truth PNGs across 132 docs** (67 tool/list + 66 dialog "before" + 18 per-tab,
  wired into 5 tabbed-dialog docs). Canonical-kept legacy dialogs (insert-entry/entry-go/lex-options) got
  brief docs so their captured PNGs land in markdown too. Abstract base classes (BaseGoDlg/MasterListDlg)
  excluded — not real dialogs. **66 of 98 constructable dialog Forms captured (~67%);** the remaining ~32
  are the per-dialog-blocker table above (Gecko/IVwRootSite/ctor-hang/bespoke-object/internal-model) →
  live-capture / on-pickup, each with its exact blocking exception. Remaining un-captured ≈ abstract bases, dialogs whose CTOR itself
  NREs/hangs (some Gecko-in-ctor), and canonical-kept dialogs (no deferred doc: insert-entry/entry-go/
  options — their PNGs are on disk). The matching-entries family is captured as **chrome** (ctor-only);
  fully-**populated** matching-entries shots still need the live app.
- **Two build/run gotchas hit + cleared:** (a) a stale FieldWorks/testhost process locks `Output/Debug`
  audio DLLs → `MSB3021 Access denied`; kill strays before a sweep. (b) "smarter" generic `ArgFor`
  (empty arrays / hvo-ints / config XmlNode) REGRESSES captures — null/0 is correct for many dialogs.
- On-pickup categories among the 46: ~12 matching-entries (live), ~16 hang/timeout (need a clerk/Gecko
  or pop a sub-modal — dictionary-config, export*, valid-characters, lex-options, …), ~11 ctor/SetDlgInfo
  errors, ~5 arg type-mismatch (FIXED: `ArgFor` now supplies the right `ICmPossibilityList`/`ICmPicture`/
  `IFsFeatStruc` and passes null instead of a wrong type).
- Total legacy truth PNGs now in docs: **67 tool/list + 34 dialogs ≈ 100+.** Remaining dialogs are the
  matching-entries family (live) + the hang/ctor-error tail (per-dialog or live, on pickup).

## On-pickup / not captured (recorded, not silently skipped)
- **Views/Gecko-coupled dialogs** (need a live `IVwRootBox`/`IVwSelection`): e.g. `RelatedWords`,
  `SummaryDialogForm`, `MergeObjectDlg` (FwTextBox), dictionary HTML preview. Capture when their ticket
  is worked, from the workflow that supplies the selection.
- **Phase-2 non-visual internals** (12, `Docs/migration/phase2/`): Views engine, mediator/PropertyTable,
  buffered draw, etc. — not screens; no PNG.

## Verification
- Option 2: 3 tools spot-verified (lexiconBrowse, concordance, semanticDomainEdit) — correct tool, non-blank.
- Option 3: harness `[Explicit]` run green; PNG asserted non-blank (>2 KB) and visually verified.
- `./test.ps1 -TestProject LexTextControlsTests` build green with the fixture added.
