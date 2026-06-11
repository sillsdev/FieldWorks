# Seam Domain Comparison

This note makes the seams concrete by comparing, per domain, four states:

1. **Before** — how it worked before this migration effort.
2. **Ideal** — the clean end-state after WinForms is removed.
3. **Now** — what actually exists on this branch (`010-advanced-entry-view-phase-1-2`).
4. **Recommended** — what to build during the coexistence year (Path 3).

It complements `seam-recommendations.md` and `architecture-diagrams.md`.

## Constraints (fixed)

- **Avalonia 11.x only** until WinForms is gone (no Avalonia 12 message filter / dispatcher work;
  cross-boundary tab/focus and popup-DPI are ours to work around; host coarsely).
- **~1-year coexist phase, then WinForms deleted.** Each UI *class* is wholly one framework, but
  different classes run **concurrently** and cooperate via **selection** and **copy/paste**.
- **XML-layout retirement is a separate effort** — keep XML→IR import; the typed IR is the runtime
  contract the Avalonia side consumes.

## What "throwaway" means here

Throwaway = wiring the new ports into **legacy internals** (e.g. threading `RefreshCoordinator` into
the live `DataTree`, or `LexicalEditorRegistry` into `SliceFactory`), because that code is deleted at
cutover. **Not** throwaway: the cross-framework **selection** and **copy/paste** bridges — they are
real, must-build, and bidirectional, and the selection concept outlives WinForms.

## A. Routing & view model

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Surface routing | `LexicalEditSurfaceResolver`/`Factory` + `LexicalEditSurfaceSelectionService` | `RecordEditView` always builds `DataTree` | No switch — only Avalonia | Resolver/factory built, tested, wired to `UIMode`; selection service added (3.9) | The one load-bearing clean seam. Route every host decision through the selection service |
| View definition / layout | typed IR (`ViewDefinitionModel`) | XML parsed straight into slices at runtime | Typed IR is authoring + runtime contract; XML gone | IR built, tested, **now consumed** by the region model (4.8) | Keep XML→IR import; defer XML retirement to its own effort |
| Editor/slice selection | `ILexicalEditorRegistry` | `SliceFactory` reads XML, picks WinForms slice | Registry resolves IR editor-kind → Avalonia control | Registry + fallback implemented; **not** wired into live `SliceFactory` | Wire registry on the **Avalonia** side only; leave legacy `SliceFactory` untouched |

## B. Editing core

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Edit session (commit/cancel/dirty) | `IEditSession` | Slices write LCModel inline, fenced ad hoc | One fenced LCModel session per edit | `PocEditSession` snapshots the **DTO**, not LCModel; real fenced session is on the prototype branch only | Reproduce the prototype's LCModel-fenced session behind `IEditSession` for the first product editor (6.x) |
| Undo/redo | (`IUndoRedoCoordinator`, not built) | LCModel `IActionHandler` stack authoritative | Global LCModel undo authoritative; control-local text undo as leaf | Local save/cancel only | Global undo = LCModel always; let Avalonia `TextBox` keep local text undo; commits route through the session |
| LCModel access / write-back | `IEditSession` + region builder | Slices read+write LCModel objects directly | Avalonia binds to a model-backed region VM; writes via session | Product route built a **read-only lossy** projection (`LexicalEditPocMapper`); replaced by typed-definition-backed region model (4.8) | Region model is the product contract; rich editing + write-back through the session is 6.x/7.x |
| Validation | validation seam | Inline in slices / native | Domain validation + Avalonia `INotifyDataErrorInfo` adapter | None on this branch (prototype-branch only) | Reproduce behind the seam when the first editable slice lands |

## C. The coexistence boundary (where 11.x bites)

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Selection sync ("current lexeme") | `IRecordNavigationContext` + `IPropertyStateStore` | WinForms views follow xCore `RecordClerk`/PropertyTable "current record" broadcast | Same bus; Avalonia is first-class publisher+subscriber | `IRecordNavigationContext` contract-only; `IPropertyStateStore` in-memory only | **Build it (not throwaway).** Bidirectional: the active surface *follows* the broadcast and *publishes* its own selection back. The bus already exists |
| Copy / paste | clipboard seam (not built) | Native Views clipboard (rich/structured TsString) | WS-aware framework-neutral clipboard | Unaddressed | **Build a shared FieldWorks clipboard format** (serialized multi-WS/TsString) both native-Views and Avalonia read/write, plus plain-text fallback; both hit the OS clipboard. Decide target fidelity early — rich Views formats won't round-trip natively |
| Focus / keyboard / tab | host edge | WinForms tab order across slices | Pure Avalonia focus within one host | Coarse hosting via `WinFormsAvaloniaControlHost` | Host coarsely — one big Avalonia view per host. Own focus *inside* the Avalonia view; don't fight cross-boundary Tab (open 11.x bug) |
| Command routing (menus/xCore) | `IXCoreCommandBridge` | xCore mediator routes to active target | Avalonia commands + thin xCore bridge at shell phase | Contract-only | Bridge only the commands this screen needs this year; defer the general bridge to the shell migration |

## D. Text & rendering

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| WS text / fonts / IME | `IWritingSystemTextService` | Native Views shaping + project WS fonts | Managed WS service feeds Avalonia text (HarfBuzz/OpenType) | POC bakes font into the DTO; region field now carries ws + (optional) font hints | Build a real WS→font/flow service from project settings; verify IME per-WS on 11.x |
| Choosers / popups | `IChooserService` | WinForms modal chooser dialogs | Avalonia flyouts + service-backed chooser model | POC `MorphTypePopupChooser` = hardcoded flyout | Real chooser **service** returning LCModel-sourced options; watch 11.x popup-DPI |
| Native rendering (RootSite/Views/Graphite) | audit test | Everything via native Views/C++ + Graphite | Excluded from Avalonia default path | POC audited to have zero Graphite/Views/RootSite refs | Keep the audit gate; legacy keeps native rendering until deleted |

## E. Infrastructure

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Refresh coordination | `ILexicalRefreshCoordinator` | `DataTree` `DoNotRefresh`/`RefreshPending` flags inline | One coordinator both surfaces honor | `RefreshCoordinator` models the gate, tested; **not** wired into live `DataTree` | Wire on the Avalonia side; leave legacy inline flags alone (throwaway) |
| Lifetime / disposal | `IRegionLifetime` | Slices dispose ad hoc | Explicit region ownership tree | Implemented + tested | Use for the Avalonia host/region |
| UI scheduling / threading | `IUiScheduler` | WinForms `Control.Invoke` | Single dispatcher, marshalled | `ImmediateUiScheduler` (tests) + dispatcher at edge | Keep thin; single UI thread on 11.x |
| Host/surface contract | `ILexicalEditHost`/`ILexicalEditSurface` | Implicit in `RecordEditView` | Explicit init/focus/context-menu/replacement contract | Contracts defined (3.5); `RecordEditView` conforms via the selection service | Formalize the active-host contract (3.10) as an audited invariant |
