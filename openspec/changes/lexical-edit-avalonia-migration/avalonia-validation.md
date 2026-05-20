# Avalonia Validation Plan

Validation must protect LCModel data, expose useful user feedback, and remain testable without assuming UI behavior that is not yet implemented.

## Current State

| Item | Source | Current Behavior |
|---|---|---|
| Validation service | [Src/LexText/AdvancedEntry.Avalonia/Services/ValidationService.cs](Src/LexText/AdvancedEntry.Avalonia/Services/ValidationService.cs) | Evaluates required Presentation IR nodes, skips unmaterialized lazy sequence items, returns deterministic errors in layout order. |
| Coverage | [ValidationServiceTests.cs](Src/LexText/AdvancedEntry.Avalonia/AdvancedEntry.Avalonia.Tests/ValidationServiceTests.cs) | Required-field ordering and lazy skip behavior. |
| UI binding | Current property-grid prototype | No complete production binding adapter yet. |

Avalonia supports validation through binding mechanisms such as `INotifyDataErrorInfo` and `DataValidationErrors`, but the current spike has not wired a full production validation presentation layer.

## Required Validation Model

| Field | Requirement |
|---|---|
| `NodeId` | Stable presentation node ID for focus and error placement. |
| `ObjectId` / class / flid | Enough LCModel context for diagnostics and refresh. |
| `Severity` | Error, warning, info. Save-blocking behavior depends on severity. |
| `ResourceKey` | Localizable message key, with formatted parameters separated from text. |
| `Message` | Localized display text at presentation time. |
| `AccessibilityText` | Screen-reader-friendly error summary where Avalonia supports exposing it. |
| `Version` | Validation run/version used to ignore stale async results. |

## Architecture

1. Validation rules operate on immutable presentation/edit-session snapshots where possible.
2. LCModel-dependent rules run on the approved thread or use immutable metadata/value snapshots.
3. The view model exposes validation through an Avalonia-friendly adapter, preferably `INotifyDataErrorInfo` when it maps cleanly to the control surface.
4. UI controls display validation state without hardcoding production strings in XAML or code.
5. Save command enablement depends on save-blocking validation errors, not on visual state alone.

## Required Tests

| Test Area | Cases |
|---|---|
| Determinism | Errors ordered by layout/focus order, independent of dictionary enumeration. |
| Lazy data | Unmaterialized sequences skipped; materialized invalid child reports correct node. |
| Localization | Error carries resource key and arguments; localized message resolves in presentation layer. |
| Severity | Warnings do not block save; errors block save; policy is explicit. |
| Async/stale results | Slow validation result from older snapshot is ignored after newer edit. |
| Accessibility | Error summary/automation metadata is exposed for focused invalid controls where supported. |
| Save interaction | Save refuses blocking errors and does not partially commit; cancel remains available. |
| External mutation | Deleted or replaced LCModel objects produce deterministic stale-object diagnostics. |

## Open Decisions

| Decision | Notes |
|---|---|
| `INotifyDataErrorInfo` vs direct `DataValidationErrors` | Prefer the smallest adapter that lets controls and tests observe the same errors. |
| Sync vs async rules | First slice can stay synchronous for required-field rules; async rules need cancellation/versioning first. |
| Error placement for virtualized nodes | Non-materialized invalid data needs region-level summary and a way to materialize/focus the node. |

## Phase Gates

| Phase | Gate |
|---|---|
| Phase 2 | Core service tests pass and known UI-binding gaps are recorded. |
| Phase 5 | First editable slice exposes validation in the view model and blocks save only by explicit severity policy. |
| Phase 6 | Accessibility and keyboard navigation can reach validation feedback. |
| Phase 8 | Shell dirty/save state reflects validation state. |