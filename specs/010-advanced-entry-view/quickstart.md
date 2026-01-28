# Quickstart (Phase 1)

This quickstart explains how to iterate on the Advanced New Entry Avalonia view using **Path 3** (Parts/Layout as contract → Presentation IR → Avalonia), with detached staged state and a single-transaction Save.

## Prerequisites
- Visual Studio 2022 (Desktop development with C++ and .NET workloads installed)
- FieldWorks build prereqs as in `.github/instructions/build.instructions.md`
- Avalonia tooling (installed implicitly by NuGet restore)
 - NuGet packages: `Avalonia`, `Avalonia.PropertyGrid`, `CommunityToolkit.Mvvm`

## Build and test
- Build: `./build.ps1`
- Test: `./test.ps1`

## Create the projects (proposed)
## Preview the UI (fast loop)
Run the shared Preview Host:

- `./scripts/Agent/Run-AvaloniaPreview.ps1 -Module advanced-entry -Data sample`

This builds and launches the Advanced Entry module without running the full FieldWorks app.

### Preview Host logs
- Default log file (next to the Preview Host executable): `Output/Debug/FieldWorks.trace.log`
- Override log path by setting environment variable `FW_PREVIEW_TRACE_LOG` to a full file path.

## Recommended folders
- Layout/: Parts/Layout loading + compilation
- Presentation/: Presentation IR types
- Staging/: detached staged state keyed by IR nodes
- Materialization/: staged state → LCModel mapping
- Services/: `ValidationService`, `LcmTransactionService`, `TemplateService`
- Editors/: `WsTextEditor`, `PossibilityTreeEditor`, `FeatureStructureEditor`, `ReferencePickerEditor`
- Views/: PropertyGrid host view (`AdvancedEntryView`), `EntrySummaryView`
- Themes/: XAML styles/templates

## Inner loop
1. Update the Parts/Layout contract selection (root view id(s)) as documented in `specs/010-advanced-entry-view/plan.md`.
2. Implement/adjust Parts/Layout loading and compilation → Presentation IR.
3. Launch the module in the Preview Host and verify the PropertyGrid renders per IR.
4. Verify editor selection (WS text, possibility pickers, references) and dynamic visibility rules are driven by IR.
5. Run `ValidationService` preflight on staged data.
6. Exercise materialization by saving via an integration harness (single transaction + single undo).

## Parity references
- Parity checklist (source of truth for scope): `specs/010-advanced-entry-view/parity-lcmodel-ui.md`
- Presentation IR research (grounding + reuse points): `specs/010-advanced-entry-view/presentation-ir-research.md`
- Legacy behavior references: `Src/LexText/LexTextControls/PatternView.cs`, `PatternVcBase.cs`, `PopupTreeManager.cs`, `POSPopupTreeManager.cs`, `FeatureStructureTreeView.cs`
- Avalonia.PropertyGrid: `PropertyGrid.axaml.cs` (operations menu), `PropertyGrid.axaml` (templating)

## Non-functional checks
- i18n/script: test RTL and combining marks in `WsTextEditor`
- Performance: enforce Path 3 acceptance checklist from `specs/010-advanced-entry-view/plan.md`:
	- compilation cache key + invalidation
	- compilation runs off the UI thread (cancellable)
	- sequences are virtualized (don’t create 100s of live editors)
- Observability: ensure logs for validation, Save attempts/results, and durations
