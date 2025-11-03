# Quickstart (Phase 1)

This quickstart explains how to iterate on the Advanced New Entry Avalonia view using Option A (DTO staging + single-transaction Save).

## Prerequisites
- Visual Studio 2022 (Desktop development with C++ and .NET workloads installed)
- FieldWorks build prereqs as in `.github/instructions/build.instructions.md`
- Avalonia tooling (installed implicitly by NuGet restore)
 - NuGet packages: `Avalonia`, `Avalonia.PropertyGrid`, `CommunityToolkit.Mvvm`

## Create the projects (proposed)
- New project: `Src/LexText/AdvancedEntry.Avalonia` (Managed, Avalonia)
- New test project: `Src/LexText/AdvancedEntry.Avalonia/AdvancedEntry.Avalonia.Tests` (NUnit)

## Recommended folders
- Models/ (DTOs): `EntryModel`, `SenseModel`, `PronunciationModel`, `VariantModel`, `ExampleModel`, `TemplateProfileModel`
- Mappers/: `EntryMapper`, `SenseMapper`, `PronunciationMapper`, `VariantMapper`
- Services/: `ValidationService`, `LcmTransactionService`, `TemplateService`
- Editors/: `WsTextEditor`, `PossibilityTreeEditor`, `FeatureStructureEditor`, `ReferencePickerEditor`
- Views/: PropertyGrid host view (`AdvancedEntryView`), `EntrySummaryView`
- Themes/: XAML styles/templates

## Inner loop
1. Launch the PropertyGrid host (`AdvancedEntryView`) with a seeded `EntryModel`.
2. Verify editors and dynamic visibility rules (attributes + filters).
3. Run `ValidationService` preflight on sample data.
4. Exercise `EntryMapper` by saving to a test project DB (integration test harness).
5. Confirm undo stack presents a single undoable Save operation.

## Parity references
- Legacy: `Src/LexText/LexTextControls/PatternView.cs`, `PatternVcBase.cs`, `PopupTreeManager.cs`, `POSPopupTreeManager.cs`, `FeatureStructureTreeView.cs`
- Avalonia.PropertyGrid: `PropertyGrid.axaml.cs` (operations menu), `PropertyGrid.axaml` (templating)

## Non-functional checks
- i18n/script: test RTL and combining marks in `WsTextEditor`
- Performance: measure Save (DTO→LCM) duration; tune mappers
- Observability: ensure logs for validation, Save attempts/results, and durations
