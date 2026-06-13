## 1. OpenSpec Alignment

- [ ] 1.1 Update `CONTEXT.md` and the change docs under `openspec/changes/avalonia-multi-writing-system-text-foundation/` to use the repo terms `ITsString`, multi-writing-system text, and IME composition consistently. [Docs]
- [ ] 1.2 Update `openspec/changes/lexical-edit-avalonia-migration/tasks.md` task 6.13 and related gate references to point at `openspec/changes/avalonia-multi-writing-system-text-foundation/` as the owning change. [OpenSpec]

## 2. Managed Text Model

- [ ] 2.1 Add an `ITsString` to managed-run projection in `Src/Common/FwAvalonia/Region/` that preserves text, writing-system assignments, and supported run properties without flattening to plain text. [Managed C#]
- [ ] 2.2 Add the inverse write-back path in `Src/Common/FwAvalonia/Region/` so edited runs rebuild managed `ITsString` values through LCModel APIs. [Managed C#]
- [ ] 2.3 Add Unicode cluster handling for combining marks, surrogate pairs, and zero-width joiner sequences in the editor model so caret movement and deletion operate on user-visible grapheme clusters. [Managed C#]

## 3. Owned Avalonia Editor Control

- [ ] 3.1 Add a FieldWorks-owned Avalonia text editor control under `Src/Common/FwAvalonia/Region/` that renders the managed run model without native Views dependencies. [Managed C#]
- [ ] 3.2 Wire the control into the existing lexical-edit region path in `Src/Common/FwAvalonia/` and `Src/xWorks/` for `StringSlice` and `MultiStringSlice` replacement candidates. [Managed C#]
- [ ] 3.3 Preserve supported run formatting on no-op saves and text edits without adding a separate style-authoring UI. [Managed C#]

## 4. Writing-System and IME Behavior

- [ ] 4.1 Project per-writing-system default font, size, direction, culture or script metadata, and keyboard activation from the language project into the new editor path in `LexicalEditRegionBuilder` or related region-model code. [Managed C#]
- [ ] 4.2 Add explicit IME composition state handling in `Src/Common/FwAvalonia/Region/` for compose, cancel, and commit behavior that stays local until commit. [Managed C#]
- [ ] 4.3 Add backspace-within-composition behavior so Backspace edits the active composition before it edits committed text. [Managed C#]

## 5. RTL and Selection Fidelity

- [ ] 5.1 Add mixed-direction and RTL caret-movement logic in the Avalonia editor so arrow movement and selection honor active run direction. [Managed C#]
- [ ] 5.2 Add hit-testing and selection-range behavior for mixed-direction runs without reintroducing native Views services. [Managed C#]
- [ ] 5.3 Add focused regression cases for mirrored punctuation, mixed-direction numbers, and run-boundary movement in the editor test suite. [Managed C# Tests]

## 6. Coexistence Integration

- [ ] 6.1 Reuse `IFwClipboard`, `FwTsStringClipboard`, and `FwDragDropData` in `Src/Common/FwAvalonia/` and `Src/xWorks/` for Avalonia rich-text copy, paste, and drag/drop round-trips. [Managed C#]
- [ ] 6.2 Replace ghost-text realization for the migrated string fields so first-commit creation no longer depends on legacy `GhostStringSlice`. [Managed C#]
- [ ] 6.3 Add coexistence coverage in `Src/xWorks/xWorksTests/` proving styled `ITsString` commits refresh legacy surfaces and remain one shared undo step. [Managed C# Tests]

## 7. Automated Evidence

- [ ] 7.1 Add headless tests in `Src/Common/FwAvalonia/FwAvaloniaTests/` for run round-trip fidelity, no-op save preservation, and Unicode cluster editing. [Managed C# Tests]
- [ ] 7.2 Add headless tests for IME state transitions and mixed-direction caret or selection behavior in `Src/Common/FwAvalonia/FwAvaloniaTests/`. [Managed C# Tests]
- [ ] 7.3 Add clipboard or drag/drop integration tests in `Src/xWorks/xWorksTests/` covering Avalonia to legacy and legacy to Avalonia `TsString` interchange. [Managed C# Tests]

## 8. Manual and Performance Evidence

- [ ] 8.1 Extend `TestLangProj/` or companion evidence fixtures with one RTL and one complex-script writing-system scenario suitable for both automated and manual runs. [Managed C# Tests + Data]
- [ ] 8.2 Capture realized-window manual evidence for one RTL and one complex-script editing path using the FieldWorks host and record the artifact locations in this change. [Manual]
- [ ] 8.3 Add a typing-latency harness and record thresholds at 100% and 150% DPI for the new editor in the lexical-edit performance docs or manifests. [Performance]

## 9. Localization and Handoffs

- [ ] 9.1 Verify that field labels still resolve through the StringTable lane and that product-facing messages stay in `FwAvaloniaStrings.resx` when the rich-text editor replaces plain-text rows. [Managed C# + Localization]
- [ ] 9.2 Add Graphite-only diagnostic hooks or explicit handoff notes to `graphite-transition-support` without redefining its fallback policy in this change. [Managed C# + Docs]
- [ ] 9.3 Update `native-views-audit.md`, `xml-retirement-blockers.md`, and related lexical-edit evidence docs to record which text blockers this change closes and what remains deferred, including `StText` and any unsupported object-content cases. [OpenSpec]

## 10. Validation

- [ ] 10.1 Run targeted `./test.ps1` filters for the new `FwAvaloniaTests` and `xWorksTests` text-foundation suites after each implementation slice. [Validation]
- [ ] 10.2 Run `./build.ps1` after the managed changes are complete and confirm the normal repo build graph still exercises the Avalonia projects and tests. [Validation]
- [ ] 10.3 Run `CI: Full local check` and review the updated automated plus manual evidence bundle before using this change as a parity gate. [Validation]