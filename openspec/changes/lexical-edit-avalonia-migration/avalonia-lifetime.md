# Avalonia Lifetime and Disposal Plan

The migrated Lexical Edit region must release sessions, loaders, subscriptions, and UI resources deterministically across save, cancel, close, navigation, and shell unload.

## Current State

| Item | Source | Current Behavior |
|---|---|---|
| View model lifetime | [Src/LexText/AdvancedEntry.Avalonia/ViewModels/MainWindowViewModel.cs](Src/LexText/AdvancedEntry.Avalonia/ViewModels/MainWindowViewModel.cs) | Owns the currently loaded entry lifetime and disposes it on save/cancel. |
| Window close | [Src/LexText/AdvancedEntry.Avalonia/Views/MainWindow.axaml.cs](Src/LexText/AdvancedEntry.Avalonia/Views/MainWindow.axaml.cs) | Wires close behavior for the preview-host window. |
| Existing coverage | `MainWindowViewModelLifetimeTests` | Save/cancel disposal and close request basics. |
| Proposed seam | `ILexicalLifetimeManager` | Not implemented. |

## Lifetime Contract

Each migrated region must define owners for:

- Active edit session.
- Loaded presentation snapshot and lazy sequence materializers.
- Validation subscriptions and async validation runs.
- UI scheduler callbacks.
- LCModel event subscriptions / `PropChanged` listeners.
- Shell command bridge registrations.
- Popup/chooser/dialog resources.

Disposal must be idempotent, must detach event handlers, and must not allow late callbacks to mutate a closed region.

## Close and Navigation Semantics

| Scenario | Required Behavior |
|---|---|
| Save then close | Save completes or reports failure; successful save disposes session and closes/returns to shell once. |
| Cancel then close | Cancel rolls back, disposes session, and closes/returns once. |
| Window close while dirty | Policy must be explicit: prompt, cancel, save, or block. First slice can choose a narrow policy but must test it. |
| Navigation to another entry | Old session is saved/canceled/disposed before new session becomes active. Late loader results from old entry are ignored/disposed. |
| External shell unload | Unregister commands/events and dispose region without depending on visual tree finalizers. |
| Fault during save/cancel | Region remains in documented state with rollback/diagnostic path; no double close. |

## Required Tests

| Test Area | Cases |
|---|---|
| Idempotent disposal | Dispose twice, save then dispose, cancel then dispose. |
| Late loader | Loader completes after cancel/navigation; result is disposed or ignored and does not overwrite current entry. |
| Event unsubscribe | LCModel/mediator/scheduler callbacks do not fire into disposed view model. |
| Close ordering | Close requested once after save/cancel; failure does not close unless policy says so. |
| Dirty close | Dirty close policy is tested, including prompt/dialog seam when introduced. |
| Popup/chooser | Open popup resources are closed/disposed on region unload. |
| Leak smoke | Weak-reference or subscription-count smoke test for common lifetime leaks when feasible. |

## Proposed Lifetime Manager

If the view-model lifetime logic grows beyond first slice, extract a seam with these responsibilities:

- Track region lifecycle state.
- Own cancellation tokens for async work.
- Dispose old sessions before loading new roots.
- Coordinate close/navigation decisions with edit session, validation, scheduler, and shell bridge.
- Expose test hooks for active subscriptions/resources.

## Phase Gates

| Phase | Gate |
|---|---|
| Phase 2 | Current save/cancel lifetime tests pass. |
| Phase 3 | Lifetime extraction starts only with late-loader and idempotent-disposal tests. |
| Phase 5 | First editable slice has dirty close/navigation behavior tested. |
| Phase 8 | Shell unload and command bridge registrations are disposed in integration tests. |