# POC Spike Evidence

Change: `lexical-edit-avalonia-poc-spike`
Date: 2026-06-08
Branch: `010-advanced-entry-view-phase-1-2`

This report records what the spike actually proved with executed evidence, and what remains. It is
the artifact required by tasks 5.1–5.2 and the spec requirement "The spike ends with evidence and a
go/no-go."

## Environment

- OS: Windows, x64
- .NET SDK: 9.0.314
- Target framework under test: **net48** (.NET Framework 4.8)
- Avalonia: **11.3.17** (pinned in `Directory.Packages.props`)

## Host-bridge package decision (executed analysis)

- **Avalonia 12.x dropped `netstandard2.0`** (net8+ only) and therefore cannot load on net48.
- **Avalonia 11.3.17** core/desktop/headless ship `netstandard2.0`, so they load on net48.
- `Avalonia.Win32.Interoperability` (which provides `WinFormsAvaloniaControlHost` for in-process
  embedding) targets `.NETFramework4.6.1` — a strong positive signal for in-process net48 hosting.
- Conclusion: the net48 in-process strategy is viable **only on the Avalonia 11.3.x line**. This is
  now pinned and documented in CPM.

## What was built and executed

Two isolated projects were created under `Src/Common/FwAvalonia/` (intentionally **not** added to
`FieldWorks.proj` or `FieldWorks.sln` yet, so the spike cannot break the default build):

- `FwAvalonia.csproj` — net48 library: flag resolver/factory, POC DTOs, fenced edit session, and
  pure-C# Avalonia controls (multi-WS text editor, morph-type popup chooser, assembled slice).
- `FwAvaloniaTests.csproj` — net48 headless test project (Avalonia.Headless.NUnit).

### Executed results

| Step | Command | Result |
|------|---------|--------|
| Restore (Avalonia 11.3.17 on net48) | `dotnet restore FwAvalonia.csproj` | **Succeeded** (3.4s) |
| Build library on net48 | `dotnet build FwAvalonia.csproj -c Debug` | **Build succeeded, 0 errors** |
| Build headless tests on net48 | `dotnet build FwAvaloniaTests.csproj -c Debug` | **Build succeeded, 0 errors** |
| Run headless tests on net48 | `dotnet test FwAvaloniaTests.csproj` | **Passed! 20 passed, 0 failed (1s)** |

The 20 passing tests cover:

- **Flag / dual-run (8):** resolver defaults to WinForms; selects Avalonia only on a truthy flag
  (`1/true/on/yes`); stays WinForms on falsy/unknown; explicit override beats the environment;
  the factory **does not construct any Avalonia runtime when the flag is off**, and constructs it
  exactly once when on.
- **No native/Graphite (1):** the POC assembly references no `Graphite`, `ViewsInterfaces`, `Views`,
  `RootSite`, `Gecko`, or `Geckofx` assembly.
- **Headless Avalonia slice on net48 (5):** renders the three editors (lexeme form, morph-type
  chooser, sense gloss); writing-system text uses the configured font per alternative; an edit writes
  through to the entry and survives **commit** while a later edit is rolled back by **cancel**; the
  morph-type chooser updates the entry and **returns focus to the host button**; the semantic
  snapshot is deterministic.

## Mapping to spike tasks

| Task group | Status | Evidence |
|------------|--------|----------|
| 1. Feature flag and two-adapter selection (default off) | **Done** | `LexicalEditSurfaceResolver`, `LexicalEditSurfaceFactory`, 8 passing tests. |
| 2. In-process host bridge | **Partial — foundation proven** | Avalonia 11.3.17 restores/builds/runs **headless on net48**; `Avalonia.Win32.Interoperability` (net461) restores/builds on net48. Embedding `WinFormsAvaloniaControlHost` into the live `RecordEditView` is **not yet done** (needs the running app) — see Pending. |
| 3. Owned Avalonia POC slice (three editors) | **Done (headless)** | `PocLexEntrySlice` + 5 passing headless tests, incl. commit/cancel and popup focus return. |
| 4. Parity and dual-run evidence | **Partial** | Deterministic semantic snapshot captured in headless tests; DPI density measurement and side-by-side screenshots require the running app — see Pending. |
| 5. Spike report and handoff | **This document** | Go/no-go below. |

## Pending (honest gaps — not yet executed)

These require the full FieldWorks app to build and run, which is heavier than this isolated spike and
was intentionally deferred to keep the default build safe:

1. ~~**In-process embedding into `RecordEditView`** via `WinFormsAvaloniaControlHost` under the live
   net48 message loop and DPI settings (task 2.3).~~ **CLOSED (2026-06-09).** The live embedding now
   exists in product code: `RecordEditView` routes the lexicon edit tool through
   `LexicalEditSurfaceSelectionService` to an in-process Avalonia host (`PocWinFormsHostControl` /
   `LexicalEditRegionView`) under the real net48 message loop, with preview-host UIA smoke tests and
   `RecordEditViewActiveHostContractTests` driving the real mediator/idle path. Note: the embedding
   was delivered through the lexical-edit program's region-model boundary, not the
   `datatree-model-view-separation` route this report anticipated — see lexical-edit task 1.13 for
   the roadmap reconciliation.
2. **DPI density measurement** at 100% and 150% and **side-by-side screenshots** of flag-off vs
   flag-on in the running app (tasks 0.2, 4.2, 4.3). Still open.
3. **Avalonia.Headless render-frame** native/Graphite runtime assertion beyond the reference audit
   (task 4.4 is covered at the reference level; a rendered-frame assertion is not added). Still open;
   tracked by lexical-edit tasks 6.9/8.4.

## Go / No-Go

**GO** for the regional migration, with the in-app embedding step (Pending #1) as the first task of
the DataTree region (`datatree-model-view-separation`).

Rationale: the dominant unknown — *can Avalonia render editable FieldWorks-owned controls in-process
on .NET Framework 4.8?* — is answered **yes** at the framework/build/headless level, on a pinned,
netstandard2.0-compatible Avalonia line, with the WinForms interop assembly confirmed to restore and
build on net48. The two-adapter flag works and keeps WinForms the safe default. The remaining risk is
concentrated in the live-embedding and density-measurement steps, which are now small, well-scoped,
and gated by Gate 0.

## Handoff to the regional migration

- `SliceSpec` (Plan A) ⊂ typed view-definition node (Plan B); `IDataTreeView` (Plan A) is selected by
  this spike's two-adapter flag. The first regional task is to embed the proven slice into
  `RecordEditView` behind the flag and capture the DPI density evidence.
- Keep the spike projects isolated until Gate 0's live-embedding evidence is captured, then add them
  to `FieldWorks.proj` and `FieldWorks.sln` per `avalonia.instructions.md`. **(2026-06-09: the
  live-embedding evidence now exists, so this integration is due — tracked as lexical-edit task 1.11.)**
