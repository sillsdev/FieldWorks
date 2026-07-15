## Why

> **Status update (2026-06-15).** This change has landed: 27 of 29 tasks in `tasks.md` are
> complete. Only 8.2 (realized-window manual RTL/Khmer evidence) and 10.3 (`CI: Full local
> check`) remain open. The framing below is written prospectively (as originally proposed) and
> should be read as the design record, not as an indication that implementation has not started.

The Avalonia lexical-edit path can already display and commit plain-text per-writing-system rows, but editing parity is still blocked by the legacy Views-owned `ITsString` editors that handle the most common Lexical Edit interactions. Splitting former task 6.13 into its own change isolates the long-pole work: managed `ITsString` editing, IME composition, RTL/bidi caret and selection behavior, and the evidence needed to prove that the Avalonia path can replace `MultiStringSlice`-family editing without falling back to native Views.

## What Changes

- Introduce a FieldWorks-owned Avalonia multi-writing-system text-editing foundation that reads and writes managed `ITsString` values without flattening them to plain text, preserving supported run-level writing-system and style data.
- Reuse the existing `TsStringWrapper`, `IFwClipboard`, and drag/drop interchange contracts instead of defining a new text serialization format.
- Define the owned control, run adapter, edit-session wiring, ghost-field realization path, refresh behavior, and undo boundaries needed to replace `MultiStringSlice`, `StringSlice`, and `GhostStringSlice` on the Avalonia path.
- Add explicit evidence lanes for IME composition, RTL/bidi caret and selection behavior, clipboard and drag/drop round-trips, manual RTL and complex-script validation, and typing-latency budgets at 100% and 150% DPI.
- Record the handoffs that stay outside this change: multi-paragraph `StText`, Graphite fallback policy, shell-global command/focus behavior, and general-purpose style-authoring UI.

## Non-goals

- Replace multi-paragraph `StText` editing or other non-lexicon text surfaces in this change.
- Define a new product text model or a new clipboard format; LCModel `ITsString` and `TsStringWrapper` remain the source-of-truth contracts.
- Decide Graphite warning or fallback UX; that remains owned by `graphite-transition-support`.
- Flip Avalonia to the default Lexical Edit UI mode.

## Capabilities

### New Capabilities
- `avalonia-multi-writing-system-text-editing`: Managed `ITsString` editing, IME, bidi, ghost realization, and parity evidence for the Avalonia lexical-edit path.

### Modified Capabilities

## Impact

- `Src/Common/FwAvalonia/Region/`, `Src/Common/FwAvalonia/Seams/`, and `Src/Common/FwAvalonia/FwAvaloniaTests/` gain the managed text control, run model, and headless evidence.
- `Src/xWorks/` and related lexical-edit tests gain coexistence wiring checks for legacy refresh, shared undo, and clipboard or drag/drop round-trips.
- `TestLangProj/`, OpenSpec evidence docs, and lexical-edit manifests gain RTL and complex-script fixtures, typing-latency budgets, and gate bookkeeping.
- The implementation is primarily managed C# across net48-hosted Avalonia code and tests. Native Views code remains the baseline and comparison surface, not a new runtime dependency.