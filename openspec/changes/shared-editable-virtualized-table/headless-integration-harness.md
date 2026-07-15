# Headless integration-test harness (scenarios & workflows)

**Status:** built (F1) · **Date:** 2026-06-16

Avalonia **headless integration tests that walk real scenarios and workflows** are the front-and-center
verification style for the whole WinForms→Avalonia program — not one-off unit tests, and not deferred
"live verification." This doc records the harness built in F1 and how it expands across the 13 phases.

## Two layers (both green in F1)

| Layer | Home | Proves | Fidelity |
| --- | --- | --- | --- |
| **Avalonia surface workflows** | `FwAvaloniaTests` (already an Avalonia-headless project) | the owned controls + seam contracts interoperate through real user gestures — filter, clear, select, edit, refresh — co-hosting multiple surfaces in one headless window | LCModel-free; surfaces driven over a real production adapter or an in-memory scenario store |
| **Real-domain (clerk) integration** | `xWorksTests` (real in-memory LCModel cache + real `RecordClerk`/adapters) | the real domain behaviour the cutover relies on — `Clerk.OnChangeFilter`/`OnSorterChanged` actually narrow/reorder/restore the real list | full domain; no app window, no on-disk project |

Crucially, **Avalonia hosting stays out of `xWorksTests`**: adding `[assembly: AvaloniaTestApplication]`
there would change the test host for its ~1400 existing tests. So view-hosting lives in dedicated
Avalonia-headless assemblies; the domain layer asserts on the clerk directly (no view needed).

## The reusable pieces

**Surface workflow harness** — `Src/Common/FwAvalonia/FwAvaloniaTests/Workflows/HeadlessWorkflowHarness.cs`:
- `HeadlessStage` — hosts one or two Avalonia controls in a headless window and pumps the dispatcher
  (`Show`, `ShowSideBySide`, `Pump`), so a test acts on a realized visual tree like a user.
- **Page-object drivers** expose intent-level verbs so scenario tests read like a script and survive
  control-internal churn:
  - `BrowseTableDriver` — `RowCount`, `CellText(r,c)`, `SelectRow`, `Filter`/`ClearFilter`/`FilterPreset`,
    `Sort`, `CheckAll`/`CheckedRows`, `Refresh`.
  - `LexicalEditorDriver` — `FieldText(field)`, `Type(field, value)`.
- Exemplar: `BrowseEditorIntegrationTests` co-hosts the browse table + the lexical-edit view and walks
  filter→narrow→clear, select-row→detail-follows, edit-in-detail→table-cell-refreshes.

**Real-domain harness** — `Src/xWorks/xWorksTests/ClerkRoutedFilterTests.cs` (the reusable setup other
phases copy):
- `MockFwXApp`/`MockFwXWindow` bootstrap over a `MemoryOnlyBackendProviderRestoredForEachTestTestBase`
  in-memory cache (the proven `ConfiguredXHTMLGeneratorTests` recipe); create domain objects directly
  (the restored base holds the undoable task open — no nested `NonUndoableUnitOfWorkHelper`).
- A real Entries `RecordClerk` (`<recordList owner='LexDb' property='Entries'/>`, `ActivateUI` +
  `SetSuppressingLoadList(false)` + `ReloadList`).
- Asserts on `clerk.ListSize` / `SortItemProvider` order through the real `OnChangeFilter`/`OnSorterChanged`.

## Conventions (apply to every new scenario test)

1. **Drive through a driver, not the visual tree.** Add a driver per surface; tests stay readable and
   stable. New surfaces (grid/tree, choosers, interlinear, dialogs) get a new driver beside the existing two.
2. **Script the workflow.** A test is a sequence of user verbs + assertions on observable state
   (`RowCount`, `CellText`, `FieldText`), not internal calls.
3. **Pump after every acting verb** (drivers do this) so layout/handlers settle before the next assertion.
4. **Pick the fidelity the claim needs.** View-contract round-trips → surface layer (fast, LCModel-free).
   "The real list narrows / undo works / persistence survives" → real-domain layer.

## Expansion across the 13 phases

- **Per surface migrated (Stages 4–8):** add a page-object driver next to `BrowseTableDriver`, and a
  workflow fixture co-hosting it with its collaborators (e.g. browse + detail, chooser + host).
- **Per domain behaviour (filter/sort/bulk-edit/undo/navigation):** add a real-clerk fixture asserting the
  effect on the clerk/model, reusing the `MockFwXWindow` setup.
- **Dedicated full-stack project (recommended next infra step):** a single new Avalonia-headless test
  assembly that references **both** xWorks (real clerk/adapters) **and** the surface drivers, with its own
  `[assembly: AvaloniaTestApplication]`, so the *production* graph (real `RecordClerk` → real
  `ClerkBrowseRowSource`/`BrowseViewer` → real `LexicalBrowseView`) can be co-hosted and driven end to
  end. Kept separate so it never destabilizes the large `xWorksTests` suite. When it lands, promote the
  drivers + `HeadlessStage` into a shared test-support library (model on `RenderTestInfrastructure`) so
  both `FwAvaloniaTests` and the integration project share one copy.
- **Gate:** each phase's "definition of done" includes its headless workflow fixtures, run on the normal
  `./test.ps1` path — the same place the parity gates already live.
