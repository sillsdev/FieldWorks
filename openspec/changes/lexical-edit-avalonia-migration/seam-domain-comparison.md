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
  different classes run **concurrently** and cooperate via **selection**, **copy/paste**, and
  **drag-and-drop** (product decision 2026-06-09), with **refresh propagation**, **one undo stack**,
  and **dialog ownership** as first-editable-slice gates.
- **XML-layout retirement is a separate effort** — keep XML→IR import; the typed IR is the runtime
  contract the Avalonia side consumes.

## What "throwaway" means here

Throwaway = wiring the new ports into **legacy internals** (e.g. threading `RefreshCoordinator` into
the live `DataTree`, or `LexicalEditorRegistry` into `SliceFactory`), because that code is deleted at
cutover. **Not** throwaway: the cross-framework **selection**, **copy/paste**, and **drag-and-drop**
bridges — they are real, must-build, and bidirectional, and the selection/interchange concepts
outlive WinForms.

## A. Routing & view model

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Surface routing | `LexicalEditSurfaceResolver`/`Factory` + `LexicalEditSurfaceSelectionService` | `RecordEditView` always builds `DataTree` | No switch — only Avalonia | Resolver/factory built, tested, wired to `UIMode`; selection service added (3.9) | The one load-bearing clean seam. Route every host decision through the selection service |
| View definition / layout | typed IR (`ViewDefinitionModel`) | XML parsed straight into slices at runtime | Typed IR is authoring + runtime contract; XML gone | IR built, tested, **now consumed** by the region model (4.8) | Keep XML→IR import; defer XML retirement to its own effort |
| Editor/slice selection | `ILexicalEditorRegistry` | `SliceFactory` reads XML, picks WinForms slice | Registry resolves IR editor-kind → Avalonia control | Registry + fallback implemented; **not** wired into live `SliceFactory` | Wire registry on the **Avalonia** side only; leave legacy `SliceFactory` untouched |

## B. Editing core

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Edit session (commit/cancel/dirty) | `IEditSession` | Slices write LCModel inline, fenced ad hoc | One fenced LCModel session per edit | The detached preview stub is retired; product editing rides LCModel-backed region edit contexts while `IEditSession` remains the seam contract | Reproduce the prototype's LCModel-fenced session behind `IEditSession` for the first product editor (6.x) |
| Undo/redo | (`IUndoRedoCoordinator`, not built) | LCModel `IActionHandler` stack authoritative | Global LCModel undo authoritative; control-local text undo as leaf | Local save/cancel only | Global undo = LCModel always; let Avalonia `TextBox` keep local text undo; commits route through the session |
| LCModel access / write-back | `IEditSession` + region builder | Slices read+write LCModel objects directly | Avalonia binds to a model-backed region VM; writes via session | Product route built a **read-only lossy** projection (`LexicalEditPocMapper`); replaced by typed-definition-backed region model (4.8) | Region model is the product contract; rich editing + write-back through the session is 6.x/7.x |
| Validation | validation seam | Inline in slices / native | Domain validation + Avalonia `INotifyDataErrorInfo` adapter | None on this branch (prototype-branch only) | Reproduce behind the seam when the first editable slice lands |

## C. The coexistence boundary (where 11.x bites)

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Selection sync ("current lexeme") | `IRecordNavigationContext` + `IPropertyStateStore` | WinForms views follow xCore `RecordClerk`/PropertyTable "current record" broadcast | Same bus; Avalonia is first-class publisher+subscriber | **Built (3.12, 2026-06-09):** `RecordClerkNavigationContext` in xWorks — publish via the clerk's real `OnJumpToRecord`/`OnNextRecord`/`OnPreviousRecord`, follow via the sponsoring `RecordEditView`'s real `OnRecordNavigation`; proven on the real mediator path (`RecordClerkNavigationContextTests`) | Bidirectional bridge done for the first host; extend to additional hosts as they gain Avalonia surfaces |
| Copy / paste | `IFwClipboard` seam | Native Views clipboard (rich/structured TsString) | WS-aware framework-neutral clipboard | **Built (3.13, 2026-06-09):** the shared format is the existing legacy `"TsString"` OS format (`TsStringWrapper` XML rep) + NFC `UnicodeText` fallback; `IFwClipboard`/`FwClipboardText` (LCModel-free, FwAvalonia) + `FwTsStringClipboard` (xWorks) round-trip with real `EditingHelper` writes/reads (`FwTsStringClipboardTests`) | Fidelity decided: multi-WS/styles round-trip; ORC object references and external consumers don't (documented in `IFwClipboard`). Wire into Avalonia editors at 6.1/6.2 |
| Drag & drop | DnD bridge (3.14, not built) | WinForms `DoDragDrop`, surface-internal only: `SliceTreeNode` (slice drag/reorder), `RecordBarTreeHandler` (record-bar tree moves) | Framework-neutral payloads over OS DnD | Unaddressed | **Build it — product decision (2026-06-09): cross-surface DnD IS supported.** Reuse the 3.13 clipboard payloads (legacy `"TsString"` format + record-key hvo/guid) over the OS DnD pipeline; in-surface reorder semantics stay surface-local; WinForms→Avalonia→WinForms round-trip test required (3.14) |
| Focus / keyboard / tab | host edge | WinForms tab order across slices | Pure Avalonia focus within one host | Coarse hosting via `WinFormsAvaloniaControlHost` | Host coarsely — one big Avalonia view per host. Own focus *inside* the Avalonia view; don't fight cross-boundary Tab (open 11.x bug) |
| Command routing (menus/xCore) | `IXCoreCommandBridge` | xCore mediator routes to active target | Avalonia commands + thin xCore bridge at shell phase | Contract-only | Bridge only the commands this screen needs this year; defer the general bridge to the shell migration |
| Dialog ownership / modality | host edge (3.16, not built) | WinForms dialogs owned by WinForms windows | Avalonia dialogs/flyouts own their own tree | Unaddressed | WinForms choosers/message boxes launched from an active Avalonia surface need correct owner, modality, z-order, and focus return through `WinFormsAvaloniaControlHost`; Avalonia popups inside WinForms hosts hit the 11.x popup-DPI quirk. Chooser-launch + focus-return smoke test (3.16); document unsupported combinations |

## D. Text & rendering

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| WS text / fonts / IME | `IWritingSystemTextService` | Native Views shaping + project WS fonts | Managed WS service feeds Avalonia text (HarfBuzz/OpenType) | Preview now uses shared region-model sample data; region fields already carry ws + (optional) font hints | Build a real WS→font/flow service from project settings; verify IME per-WS on 11.x |
| Choosers / popups | `IChooserService` | WinForms modal chooser dialogs | Avalonia flyouts + service-backed chooser model | Shared `FwChooserField` now serves both preview and product paths | Real chooser **service** returning LCModel-sourced options; watch 11.x popup-DPI |
| Native rendering (RootSite/Views/Graphite) | audit test | Everything via native Views/C++ + Graphite | Excluded from Avalonia default path | The phase-0 spike and current region are audited to have zero Graphite/Views/RootSite refs on the Avalonia path | Keep the audit gate; legacy keeps native rendering until deleted |

## E. Infrastructure

| Domain | Seam | Before | Ideal | Now | Recommended (coexist year) |
|---|---|---|---|---|---|
| Refresh coordination | `ILexicalRefreshCoordinator` | `DataTree` `DoNotRefresh`/`RefreshPending` flags inline | One coordinator both surfaces honor | `RefreshCoordinator` models the gate, tested; **not** wired into live `DataTree` | Wire on the Avalonia side; leave legacy inline flags alone (throwaway). **Coexistence gate (3.15):** Avalonia commits must raise `PropChanged` legacy repaints from, and legacy edits + F5/`RefreshAllViews` must reach active Avalonia hosts — shared-cache consistency stands or falls on this loop; gates the first editable slice with a two-surface test |
| Lifetime / disposal | `IRegionLifetime` | Slices dispose ad hoc | Explicit region ownership tree | Implemented + tested | Use for the Avalonia host/region |
| UI scheduling / threading | `IUiScheduler` | WinForms `Control.Invoke` | Single dispatcher, marshalled | `ImmediateUiScheduler` (tests) + dispatcher at edge | Keep thin; single UI thread on 11.x |
| Host/surface contract | `ILexicalEditHost`/`ILexicalEditSurface` | Implicit in `RecordEditView` | Explicit init/focus/context-menu/replacement contract | Contracts defined (3.5); `RecordEditView` conforms via the selection service | Formalize the active-host contract (3.10) as an audited invariant |
