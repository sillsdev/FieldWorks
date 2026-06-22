# Seam Catalog

The seams that separate Avalonia UI from LCModel/xCore/WinForms. All
contracts live in `Src/Common/FwAvalonia/Seams/ISeams.cs` with
implementations in `SeamImplementations.cs` and tests in
`Src/Common/FwAvalonia/FwAvaloniaTests/SeamTests.cs`. Per-seam design docs
(current state, alternatives considered, required tests) live in
`openspec/changes/lexical-edit-avalonia-migration/avalonia-*.md`.

Before inventing a new abstraction for a migration, check this table. If a
seam fits, reuse it. If none fits, add the new seam here (name, purpose,
rules, pivot trigger) in the same PR that introduces it.

Contents:

1. Seam table
2. Supporting seams
3. Pivot triggers (when to revisit a decision)

## 1. Seam table

| Seam | Purpose | Key rules |
| --- | --- | --- |
| `IEditSession` | Fenced LCModel undo-task lifecycle: Active → Saved/Canceled → Disposed | One undoable action per save; cancel rolls back without creating an undo action; writes outside a session are a bug |
| `IUndoRedoCoordinator` | Routes global undo/redo through the LCModel action handler | Control-local text undo stays local until commit; never a parallel committed-state history; refresh region after global undo/redo |
| `IValidationService` | Deterministic validation over immutable presentation snapshots | Focus-order error ordering; skip unmaterialized lazy items; localized message keys; only severity=Error blocks save; discard stale async results |
| `IXCoreCommandBridge` | Bridges xCore mediator command routing to Avalonia commands | Region-local commands first; shell-scope wiring happens in the shell phase, not per region |
| `IUiScheduler` | Thin UI-thread marshalling (`IsOnUiThread`, `Post`) | No hidden `Task.Run`; fakeable in tests; keeps threading visible at the seam |
| `IRegionLifetime` | Region disposal discipline | Idempotent disposal, late-callback suppression, event-handler cleanup; protects against async work completing after close |
| `ILexicalRefreshCoordinator` | Mirrors legacy `DoNotRefresh`/`RefreshListNeeded` gating (LT-22414) | Defer PropChanged fan-out during multi-field edits until commit/cancel; characterize legacy behavior before extending (`RefreshCoordinator.cs`) |
| `IRecordNavigationContext` | Bidirectional selection bridge with the xCore "current record" bus | Follow external navigation and publish selection back; never reach into PropertyTable directly from a region |
| `IFwClipboard` | Clipboard access without WinForms dependency | See `FwClipboardSeamTests.cs` |
| `IHostSurface` (focus API) | Host-side focus save/restore around WinForms dialogs | Pairs with the dialog-ownership rules (architecture-patterns.md §7) |

## 2. Supporting seams

- **View definition pipeline:** `IViewDefinitionImporter` /
  `ViewDefinitionCompiler` / cache keyed by immutable source snapshot
  (`ViewDefinitionCacheKey.cs`). Off-thread compilation, deterministic
  output.
- **Region value provision:** the composer consumes value-provider style
  seams so region models can be built and tested without LCModel or
  WinForms.
- **Active-host contract:** `ActiveHostContract.cs` whitelists the only
  approved adapters through which an active Avalonia host may touch legacy
  infrastructure.
- **Drag/drop and sync-context hygiene:** `FwDragDrop.cs`,
  `FinalizerSafeSynchronizationContext.cs`.

## 3. Pivot triggers (when to revisit a decision)

A decision below stands until its trigger fires. When a trigger fires,
record the re-evaluation outcome here and in the lessons ledger.

- **Edit sessions:** adopt a staged-draft model only if fenced direct
  LCModel sessions prove unacceptably complex or risky in practice.
- **Undo/redo:** add richer document-local undo only for a specific owned
  control that needs it, still committing through LCModel.
- **Validation:** collapse to Avalonia-native validation only for isolated
  dialogs with no LCModel/cross-object semantics.
- **UI scheduler / lifetime:** collapse wrappers that demonstrably provide
  no test or architecture value.
- **TreeDataGrid:** re-evaluate for browse surfaces if it is relicensed
  permissively (or SIL accepts a commercial license) AND upstream closes
  the editing/automation gaps.
- **VirtualizingStackPanel:** escalate to a fully owned realization-window
  virtualizer if scroll/expand or open-time budgets fail on the production
  fixtures (253-slice detail, 10k-row browse).
- **TreeView ceiling (≤500 items):** raise/remove if a consumable Avalonia
  release ships TreeView virtualization.
- **ItemsRepeater:** reconsider as the owned-control substrate if it is
  un-deprecated with maintained virtualization.
- **Owned-control cost:** if owned controls overrun, re-open the
  TreeDataGrid commercial option with measured cost as the baseline.
- **Stock-control accessibility:** if any adopted stock control fails an
  accessibility gate, owning its automation peers becomes mandatory.
