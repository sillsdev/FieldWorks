# UI Wiring Review Checklist (Task 3.11)

A repeatable checklist for every feature-flag or host-routing change that affects which UI surface is
active. It operationalizes the `fieldworks-ui-wiring-review` skill for this change. Attach a filled copy
to each PR that touches surface selection, `PropertyTable`/mediator routing, or host replacement.

## Scope

- [x] Reviewed against the branch-only diff (`main..HEAD`), not a calendar-day commit list. — `git diff --name-only main...HEAD`: 713 files reviewable on the branch (480 outside `.claude/` skills + `openspec/`; the `Src/` product/test surface is the wiring-relevant subset).
- [x] Listed **every** `RecordEditView` consumer / host affected (Lexicon, Grammar, Notebook, Lists,
      Words), and each has a deliberate behavior under both UI modes: supported Avalonia, explicit
      legacy fallback, or resource-backed blocked state. — RecordEditViewSwitchTests.cs:94-113: `lexiconEdit` → Avalonia; `posEdit`/`notebookEdit`/`domainTypeEdit`/`Analyses` (Grammar/Notebook/Lists/Words) assert `LexicalEditSurface.WinForms` ExplicitLegacyFallback under New mode; behaviors enumerated in `HostUiBehavior` (LexicalEditSurfaceSelectionService.cs:12-25).

## Wiring path (trace end to end)

- [x] **Setting source** — where the mode is read (app setting / persisted `UIMode` preference). — Settings.settings:14 (`FwApplicationSettings.UIMode`, User scope) seeded into the PropertyTable at FieldWorks.cs:2916; host reads it via `m_propertyTable.GetStringProperty(UIModePropertyName, LegacyUIMode)` (RecordEditView.cs:570).
- [x] **Persisted state** — persistence flag set/cleared correctly; no accidental global persistence. — durable persistence is the app setting (`settings.UIMode = norm`, AvaloniaOptionsDialogLauncher.cs:497); PropertyTable persistence is deliberately disabled everywhere it is written (FieldWorks.cs:2917, AvaloniaOptionsDialogLauncher.cs:501, LexOptionsDlg.cs:151) so the mode is not double-persisted into the global property table.
- [x] **`PropertyTable` key** — `UIMode` (and `currentContentControl` for per-tool resolution). — `UIModePropertyName = "UIMode"` (LexicalEditSurfaceResolver.cs:49); per-tool resolution reads `currentContentControl` (RecordEditView.cs:573, RecordEditView.cs:745).
- [x] **Broadcast** — change is delivered via the real mediator/property broadcast, not a manual
      `OnPropertyChanged(...)` call in production or tests. — production flips with `SetProperty(UIModePropertyName, norm, true)` (AvaloniaOptionsDialogLauncher.cs:500, LexOptionsDlg.cs:150); `doBroadcastIfChanged` drives the real broadcast (PropertyTable.cs:563). Switch test uses the same `SetProperty("UIMode","New",true)` real path (RecordEditViewSwitchTests.cs:85), not a direct `OnPropertyChanged` call.
- [ ] **Listener registration** — the host subscribes/unsubscribes symmetrically (no leak). — OPEN: the `UIMode` reaction is `RecordEditView.OnPropertyChanged` (RecordEditView.cs:363), an `IxCoreColleague` mediator callback, not an explicit subscribe/unsubscribe pair, so "symmetric subscribe/unsubscribe" does not apply to it. The only explicit pub/sub in the host is `Subscriber.Subscribe(ConsideringClosing)` / `Unsubscribe(...)` (RecordEditView.cs:180 / :200) which is symmetric — but that is the close hook, not the UIMode listener. No code-level test proves colleague unregistration on dispose.
- [x] **Resolution** — routed through `LexicalEditSurfaceSelectionService` (3.9), not ad-hoc reads of
      settings/property-table state scattered in the host. — `m_surfaceSelectionService.Decide(uiMode, toolName).Surface` is the single resolution point (RecordEditView.cs:72, :576); the service is the only place mode+tool→surface logic lives (LexicalEditSurfaceSelectionService.cs:61-85).
- [x] **Host reload path** — current content is re-shown on switch without a tool reload. — OnPropertyChanged flips the surface then calls `ShowRecord(new RecordNavigationInfo(...))` in place (RecordEditView.cs:387-388); switch test asserts the live control instance is reused (`sameControl Is.SameAs(control)`, RecordEditViewSwitchTests.cs:88-91).
- [x] **Focus / command target routing** — active surface added to Ctrl+Tab/message targets; inactive
      surface removed. — Ctrl+Tab adds the Avalonia host when active else the legacy CurrentSlice (PopulateCtrlTabTargetCandidateList, RecordEditView.cs:1564-1577); message targets add the legacy DataTree/menu only while `m_legacySurfaceInitialized` (GetMessageAdditionalTargets, RecordEditView.cs:1550-1554).
- [x] **Save / `PrepareToGoAway()`** — routes to the active surface only. — PrepareToGoAway settles the fenced Avalonia session unconditionally and calls `m_dataEntryForm.PrepareToGoAway()` ONLY when `!ShouldUseAvaloniaLexicalEdit` (RecordEditView.cs:343-347).
- [x] **Fallback / blocked state** — non-migrated hosts fall back explicitly. — service returns `HostUiBehavior.ExplicitLegacyFallback` for New mode + unmigrated tool (LexicalEditSurfaceSelectionService.cs:77-81); proven for posEdit/notebookEdit/domainTypeEdit/Analyses (RecordEditViewSwitchTests.cs:98-112); `Blocked` reserved (HostUiBehavior, LexicalEditSurfaceSelectionService.cs:24).

## Active-host contract (3.10)

- [x] The active Avalonia path does **not** instantiate or drive a hidden legacy `DataTree`/menu
      infrastructure (no `EnsureLegacySurfaceInitialized` / `DataTree.ShowObject` while Avalonia is
      active), except through an adapter explicitly declared in `ActiveHostContract.AllowedBaselineAdapters`. — Avalonia start initializes only the Avalonia surface (SetupDataContext, RecordEditView.cs:1473-1481); ShowRecord branches to `EnsureAvaloniaSurfaceActive()` (RecordEditView.cs:494-497) and never `EnsureLegacySurfaceInitialized` while Avalonia is active; the sole legacy-drive site (EnsureMenuCommandAdapter, RecordEditView.cs:1182-1203) first calls `AssertLegacyDataTreeDriveAllowed(CommandMenuRoutingAdapterId)` against the contract built from `ApprovedBaselineAdapters` (RecordEditView.cs:95-96, :1375-1376).
- [x] An audit test proves it (e.g. `RecordEditViewActiveHostContractTests`). — RecordEditViewActiveHostContractTests.cs:67-109 loads `lexiconEdit` in New mode, asserts `m_legacySurfaceInitialized == false`, the dormant DataTree is unparented, and the contract permits only `CommandMenuRoutingAdapterId` while an unlisted id throws (RecordEditViewActiveHostContractTests.cs:85-104).

## Product vs preview boundary

- [x] Product route uses a **typed-definition-backed region model** (`LexicalEditRegionModel`), not a
      lossy `LexicalEditPocMapper` DTO or preview host code. — product ShowAvaloniaEntry builds a `LexicalEditRegionModel` via `FullEntryRegionComposer.Compose(...)` from compiled view definitions (RecordEditView.cs:749-758); field kinds are derived from the typed view-definition editor classification (LexicalEditRegionModel.cs:14-18). `LexicalEditPocMapper` does not exist in `Src` (only named in openspec docs), so it cannot be on any route.
- [x] Preview-only artifacts (`LexicalEditPreviewDataProvider`, `LexicalEditPreviewScenario`, sample data) are not on any
      product route. — these types live only under `Src/Common/FwAvalonia/Preview/` (LexicalEditPreviewSupport.cs:41-46) and are referenced only by the preview module registration (AssemblyPreviewModules.cs) and LexicalEditPreviewTests.cs; no reference from RecordEditView/RecordBrowseView or the composer (grep: 6 files, all Preview/tests/openspec).
- [x] Product-facing strings are localizable; remaining prototype strings are called out as gaps. — §19 owned controls source user-facing text from the resx-backed `FwAvaloniaStrings` (FwStructuredTextField.cs:97,155,166,276,287,297-299,405,415-416,477-478; host warnings FwAvaloniaStrings.EditDiscarded*, RecordEditView.cs:248-250). GAP: a constructed accessibility Name concatenates a non-localized " paragraph " literal (FwStructuredTextField.cs:94) — accessibility name only, visible label uses the localized field label; called out as a remaining gap.
- [x] Stable, nonlocalized `AutomationId`s on user-facing controls; localized names/tooltips allowed. — §19 controls stamp stable nonlocalized AutomationIds with localized Name/tooltip (FwStructuredTextField.cs:53-54,93-94,297-299); the convention is enforced executably by OwnedControlAutomationConventionTests.cs:40-70 (AutomationId == stable field id, Name == localized label, per-WS box ids suffixed) so a future control that omits an id fails CI.

## Build / test graph

- [x] Validated through the normal repo path (`./build.ps1`, `./test.ps1`) plus host-specific tests —
      not a branch-only `-BuildAvalonia` lane as the primary evidence. — `./build.ps1` is the sanctioned build entry (AGENTS.md) and succeeded this session; the managed `./test.ps1` sweep was 1621 green, including the host-specific RecordEditViewSwitchTests / RecordEditViewActiveHostContractTests / LexicalEditSurfaceResolverTests / OwnedControlAutomationConventionTests cited above (not re-run here; the scripts ARE the validated path).
- [x] Tests drive the real setting + broadcast path; none simulate wiring via direct handler calls. — RecordEditViewSwitchTests.cs:85 flips via `SetProperty("UIMode","New",true)` (real broadcast) and the harness drains the real mediator/idle queues (DrainMediatorAndIdleQueues, RecordEditViewSwitchTests.cs:169-186); no test calls `control.OnPropertyChanged(...)` directly.
