# Avalonia UI Scheduler Plan

Avalonia UI work must use the UI dispatcher deliberately, and LCModel work must not be pushed onto background threads unless it operates on immutable snapshots. This plan prevents the migration from hiding threading bugs behind `Task.Run`.

## Current State

| Item | Source | Current Behavior |
|---|---|---|
| Dispatcher use | [Src/LexText/AdvancedEntry.Avalonia/ViewModels/MainWindowViewModel.cs](Src/LexText/AdvancedEntry.Avalonia/ViewModels/MainWindowViewModel.cs) | Uses Avalonia dispatcher calls directly for UI-bound operations. |
| Tests | Avalonia headless tests | Tests can flush dispatcher work, but there is no scheduler seam yet. |
| Proposed seam | `IUiScheduler` | Not implemented. |

Avalonia documentation distinguishes fire-and-forget dispatcher posting from awaited invocation. Use the awaited path when tests and lifecycle code need completion, result, cancellation, or exception propagation.

## Rules

| Rule | Rationale |
|---|---|
| UI-bound mutations run on Avalonia UI thread. | Keeps visual tree/view-model notifications predictable. |
| LCModel writes run through approved edit-session/main-thread path. | LCModel and native boundaries are not safe targets for casual background work. |
| Background work uses immutable snapshots. | Layout compilation and validation can be parallelized only after data is copied into immutable inputs. |
| Await completion for lifecycle-critical work. | Save, cancel, close, validation, and loader disposal need exception/cancellation visibility. |
| Fire-and-forget must be rare and logged/owned. | `Post` can hide failure and create late callbacks after disposal. |

## Proposed Scheduler Seam

Introduce a thin seam only when tests need it:

```csharp
public interface IUiScheduler
{
    bool CheckAccess();
    Task InvokeAsync(Func<Task> action, CancellationToken cancellationToken);
    Task<T> InvokeAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
    void Post(Action action);
}
```

Contract details:

- `InvokeAsync` propagates exceptions and observes cancellation before starting work when possible.
- `Post` is for non-critical notifications only; tests must not rely on it as completed work.
- The scheduler does not own LCModel threading policy; edit-session services do.

## Required Tests

| Test Area | Cases |
|---|---|
| Access | `CheckAccess` returns true on UI thread and false in fake/background contexts. |
| Exception | Exception thrown in invoked action reaches caller and does not leave session half-disposed. |
| Cancellation | Canceled token prevents queued lifecycle work or reports cancellation deterministically. |
| Disposal | Late scheduled loader result is disposed/ignored after cancel/close. |
| Ordering | Save/cancel/close ordering is deterministic under queued UI work. |
| Snapshot work | Background layout/validation tests prove inputs are immutable and no live `LcmCache` mutation occurs. |

## Phase Gates

| Phase | Gate |
|---|---|
| Phase 3 | Add scheduler seam only with fake-scheduler tests that prove lifecycle behavior. |
| Phase 4 | Layout compiler background work consumes immutable snapshots. |
| Phase 5 | Save/cancel validation paths use awaited scheduling where completion matters. |
| Phase 8 | Shell integration has cancellation and disposal tests for region unload/navigation. |