# Localization Review — Avalonia / UI-Mode Changes (Task 10.13)

Date: 2026-06-09

Scope: every product-facing string introduced by this change — the FwAvalonia surfaces
(`Src/Common/FwAvalonia`, excluding `FwAvaloniaTests`), their xWorks consumption
(`RecordEditView`, `LexicalEditRegionEditContext`), and the UI-mode controls in
`LexOptionsDlg`. Reviewed against the `fieldworks-localization-review` skill checklist:
`.resx` coverage, Crowdin compatibility, stable automation IDs, localized user messages, and
explicit evidence for remaining prototype strings.

---

## 1. `.resx` coverage — PASS

- `Src/Common/FwAvalonia/FwAvaloniaStrings.resx` carries all 8 product-facing Avalonia strings
  (placeholder `ksNoEntrySelected`, unsupported-record `ksEntryTypeUnsupported`,
  unsupported-editor `ksUnsupportedEditor`, `ksSave`, `ksCancel`, `ksUndoEditEntry`,
  `ksRedoEditEntry`, validation `ksLexemeFormRequired`), each with a translator comment.
- `Src/Common/FwAvalonia/FwAvaloniaStrings.cs` is the resx-backed accessor
  (`ResourceManager("FwAvalonia.FwAvaloniaStrings", ...)`); the SDK-style csproj embeds the
  resx by default globbing with manifest name `FwAvalonia.FwAvaloniaStrings.resources`
  (default `RootNamespace` = project name `FwAvalonia`), which the accessor's base name
  matches.
- All 8 keys have live consumers: `LexicalEditRegionView` (Save/Cancel/UnsupportedEditor),
  `PocWinFormsHostControl.ShowNoEntry` (NoEntrySelected), `RecordEditView.cs:419`
  (EntryTypeUnsupported), `LexicalEditRegionEditContext.cs:110/130` (LexemeFormRequired,
  Undo/RedoEditEntry).
- `FwAvaloniaStringsTests` (`FwAvaloniaTests/RegionEditingTests.cs:217-230`) proves every key
  resolves non-empty from resources.

## 2. Crowdin compatibility — PARTIAL (one registration gap found)

How Crowdin discovers resx in this repo (facts):

- Root `crowdin.json` maps `"source": "Src/**/*.resx"` with ignores `Src/**/*Tests/**/*` and
  `Src/**/HelpTopicPaths.resx`. `Src/Common/FwAvalonia/FwAvaloniaStrings.resx` matches the
  glob and no ignore, so **upload to Crowdin needs no registration** — it is picked up
  automatically, like every other project resx. The UI-mode keys live in the long-established
  `Src/LexText/LexTextControls/LexTextControls.resx`, also covered.
- Translation download/build is `Build/Localize.targets` → `LocalizeFieldWorks` task →
  `Localizer`/`ProjectLocalizer` (`Build/Src/FwBuildTasks/Localization/`). `Localizer`
  collects every folder under `Src/` containing exactly one csproj, skipping folders ending
  in `Tests` — so `FwAvalonia` is collected and `FwAvaloniaTests` correctly skipped.

**GAP-L1 (build-side, must fix before claiming localized output):**
`ProjectLocalizer.GetResourceInfo` (`Build/Src/FwBuildTasks/Localization/ProjectLocalizer.cs:84-93`)
**requires an explicit `<RootNamespace>` element in the csproj** and logs an error
("Can't find RootNamespace...") and returns null when absent. `FwAvalonia.csproj` is SDK-style
and declares no `<RootNamespace>` (it relies on the SDK default). By static reading, the
localization build will therefore error/skip FwAvalonia and **no `FwAvalonia.resources.dll`
satellite assemblies will be produced** even though Crowdin returns translations.
Fix: add `<RootNamespace>FwAvalonia</RootNamespace>` to `Src/Common/FwAvalonia/FwAvalonia.csproj`.
Caveat/what to verify: this finding is from code reading, not an executed localization build —
confirm by running the `Localize.targets` lane (requires `CROWDIN_API_KEY`; runs in the
installer CD workflows, which install `overcrowdin`) or by unit-driving `ProjectLocalizer`
against the FwAvalonia folder, and also confirm whether the logged error fails the whole
localization build rather than just skipping the project. Installer packaging of the new
satellite dll is a follow-on check once it exists.

## 3. Stable automation IDs — PASS

- All automation selectors are nonlocalized code constants, never resource lookups:
  `LexicalEditRegionView`, `RegionEditor.Save`, `RegionEditor.Cancel`,
  `RegionEditor.ValidationErrors`, per-field ids from the IR (`field.AutomationId` falling
  back to `StableId`, plus `.Label`/`.{wsAbbrev}` suffixes), `PocWinFormsHostControl`/
  `AvaloniaHost`/`RecordEditView.AvaloniaPoc` on the WinForms host.
- Locked by tests: `PocLexEntrySliceTests` (stable Avalonia automation metadata),
  `WinFormsUiaSmokeTests` (nonlocalized filter-combo ids), and the region editing suites
  (tasks 2.12, 6.8, 6.9).

## 4. Localized user messages — PASS for messages, with label-lane gap (see GAP-L3)

- Every user-facing *message* on the product Avalonia path is resx-backed (section 1):
  placeholder, unsupported-record, unsupported-editor, validation error, undo/redo labels,
  Save/Cancel. Validation errors surface through `IRegionEditContext.Validate()` →
  `LexicalEditRegionView` in place; the unsupported-surface behavior under the global switch
  (6.12) uses `ksEntryTypeUnsupported`.
- Non-migrated consumers fall back to the legacy surface (no new fallback message exists to
  localize — recorded in task 2.12).

## 5. UI-mode labels in LexOptionsDlg — PASS

- `Src/LexText/LexTextControls/LexOptionsDlg.cs` reads all five UI-mode strings through
  `GetOptionString(...)` → `LexTextControls.ResourceManager`; the keys exist in
  `Src/LexText/LexTextControls/LexTextControls.resx:1316-1335` with translator comments:
  `UiModeGroupTitle` ("Lexical Edit UI:"), `UiModeLabel` ("Mode:"), `UiModeLegacy` ("Legacy"),
  `UiModeNew` ("New"), `UiModeRestartToApply` ("Restart to apply").
- The English literals in `LexOptionsDlg.cs:451-482` are fallback defaults used only if the
  resource is missing; the resx is authoritative, locked by
  `LexOptionsDlgTests.UIModeControls_ReadDisplayTextFromResx`
  (`Src/LexText/LexTextControls/LexTextControlsTests/LexOptionsDlgTests.cs`).
- Mode values persisted/broadcast (`"Legacy"`/`"New"`, `UIMode` property name) are
  nonlocalized identifiers, correctly separate from the display text.

## 6. Remaining hardcoded string literals in FwAvalonia production source — classified

Census method: grep of multi-word string literals over `Src/Common/FwAvalonia` excluding
`FwAvaloniaTests`, plus file-by-file reading of the product render path. Every hit classified:

### Resx-backed (correct)
- All consumers listed in section 1.

### Preview-host-only (acceptable by contract; `Poc*` files are preview-only — task 4.8/7.9)
- `Poc/PocLexEntrySlice.cs:24,47,48` — "Lexical Edit POC Slice", "Lexeme Form", "Morph Type".
- `Poc/MorphTypePopupChooser.cs:35,56` — "Morph type options", "Morph Type" (reached only via
  `PocLexEntrySlice`/`ShowEntry`, which is preview-host-only).
- `Poc/PocPreviewWindow.cs:22`, `Poc/PocEntryDto.cs` sample data,
  `Poc/PocPreviewDataProvider.cs` ws/font literals, `Preview/AssemblyPreviewModules.cs:6`
  ("Lexical Edit POC" module name).
- **Note:** `Poc/PocWinFormsHostControl.cs` is *not* preview-only despite the prefix — it is
  the product WinForms host (`RecordEditView.cs:69,98,520`). Its strings are classified below.

### Automation/accessibility identifiers (correctly nonlocalized)
- `LexicalEditRegionView` ids, `RegionEditor.*` ids, field ids (section 3).
- `PocWinFormsHostControl.cs:28,29,36` — `Name`/`AccessibleName` values
  "PocWinFormsHostControl", "RecordEditView.AvaloniaPoc", "AvaloniaHost" (selector-shaped,
  used by host contract tests).

### Developer/diagnostic strings (nonlocalized by design, not user-surfaced)
- `XmlLayoutImporter.cs` / `LayoutImportCoverage.cs` import diagnostics (shown in coverage
  reports and region diagnostics, not rendered to end users by `LexicalEditRegionView`).
- `LexicalEditSurfaceSelectionService.cs:69,84` `SurfaceDecision.Reason` strings (not rendered
  by `RecordEditView`; logged/tested only).
- `MorphTypeSwapLogic.cs:73` decision reason; `SeamImplementations.cs:29` exception message.
- `LexicalEditFirstSlice.cs:122-124` `authored-fallback` diagnostic message.

### GAP — needs migration before broader rollout
- **GAP-L2 (screen-reader-visible English on product controls):**
  `Region/LexicalEditRegionView.cs:42` — `AutomationProperties.SetName(this, "Lexical Edit
  Region")`; `Poc/PocWinFormsHostControl.cs:37` — `AccessibleName = "Avalonia Host"`.
  `AutomationId` must stay nonlocalized, but UIA *Name* is announced by screen readers and
  should be resx-backed (the Save/Cancel buttons already set localized Names — these two
  container names are the stragglers). Low severity, two strings.
- **GAP-L3 (field-label localization lane not wired):** the region renders `field.Label` raw
  (`LexicalEditRegionView.cs:178,185`). Labels originate in shipped layout XML (English) via
  the importer, and `LexicalEditFirstSlice.cs:90` additionally stamps the literal
  "Lexeme Form" (the `AuthoredFallback` at `:115-117` stamps "Lexeme Form"/"Morph Type"/
  "Gloss"). Legacy `DataTree` localizes these same layout labels at display time through
  `XmlUtils.GetLocalizedAttributeValue`/`StringTable` (`DataTree.cs:3349-3350`), whose
  `strings-{locale}.xml` lane is Crowdin entry #1 in `crowdin.json`. The Avalonia path has no
  equivalent pass, so in a non-English UI the new view shows English field labels where the
  legacy view shows translated ones. The IR already carries `LocalizationKey` (task 4.7) and
  the mapper propagates it (`LexicalEditRegionMapper.cs:66-67`) — what is missing is resolving
  labels through the StringTable lane (or populating `LocalizationKey`) at compile or render
  time. This is the substantive localization-parity gap; it should be fixed (or explicitly
  waived with owner sign-off) before any region claims localization parity under 10.8.

## Verdict per checklist item

| Checklist item | Verdict |
|---|---|
| `.resx` coverage of product-facing Avalonia strings | **PASS** (8/8 keys, all consumed, test-locked) |
| Crowdin compatibility | **PARTIAL** — upload auto-discovered via `crowdin.json` glob; **GAP-L1**: missing `<RootNamespace>` in `FwAvalonia.csproj` likely breaks satellite-assembly build; verify via the Localize lane |
| Stable automation IDs (nonlocalized) | **PASS** (code constants, test-locked) |
| Localized user messages (placeholder/unsupported/validation/undo-redo) | **PASS** |
| UI-mode labels in `LexOptionsDlg` use resx | **PASS** (`LexTextControls.resx`, test-locked; in-code literals are fallbacks only) |
| Explicit evidence for remaining prototype strings | **DONE** — census above; preview-only strings confined to `Poc*` preview paths; **GAP-L2** (2 UIA names) and **GAP-L3** (field-label localization lane) are the open items |

Open actions: (1) add `<RootNamespace>FwAvalonia</RootNamespace>` and verify the localization
build produces `FwAvalonia.resources.dll` (GAP-L1); (2) move the two container UIA names to
`FwAvaloniaStrings.resx` (GAP-L2); (3) decide and implement the field-label localization lane
— StringTable pass or `LocalizationKey` population — before localization parity is claimed for
the region (GAP-L3).
