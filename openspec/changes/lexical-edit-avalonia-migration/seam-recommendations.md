# Avalonia Seam Recommendations

This note records the accepted seam recommendations for the Lexical Edit Avalonia migration, the main alternatives considered, why the current recommendation won, and the conditions under which FieldWorks should revisit that choice.

These notes support the normative capability specs:

- `avalonia-edit-sessions`
- `avalonia-undo-redo`
- `avalonia-validation`
- `avalonia-command-focus`
- `avalonia-ui-scheduler`
- `avalonia-lifetime`

## Edit Sessions

**Accepted recommendation:** Hybrid staged drafts plus a FieldWorks-owned commit boundary.

**Why this won:** Avalonia does not provide an application-level edit-session abstraction. FieldWorks needs staged values, explicit save and cancel, LCModel transaction integrity, and commit fencing. The current worktree already points this way with `AdvancedEntryEditSession`, `AdvancedEntryCommitFence`, and staged entry state.

**Options considered:**

1. FieldWorks-owned domain edit session from day one.
Pros: strongest LCModel alignment, explicit rollback, easiest legacy bridge.
Cons: most custom plumbing, highest up-front design cost.

2. Package-led draft editing using ReactiveUI or similar.
Pros: strong draft-state ergonomics, strong testability, good async support.
Cons: still does not solve LCModel commit semantics, introduces a larger framework commitment, risks duplicate state models.

3. Hybrid staged drafts plus FieldWorks-owned commit boundary.
Pros: keeps transaction authority in FieldWorks, reuses standard MVVM helpers locally, preview-host friendly, lower migration risk.
Cons: requires discipline to avoid duplicated validation and save logic.

**Stage / invocation:** Apply on the first editable slice, before broad editor rollout. Preview-only and sample-data surfaces may stay lighter until they commit live data.

**Change-gears trigger:** Revisit only if staged diff/apply complexity becomes disproportionate for a surface and that surface demonstrably behaves more like a simple form than a lexical editing workflow.

**Key references:**

- [Avalonia data validation](https://docs.avaloniaui.net/docs/app-development/data-validation)
- [MVVM Toolkit overview](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [ReactiveUI commands](https://www.reactiveui.net/documentation/handbook/commands/)
- [ReactiveUI validation](https://www.reactiveui.net/documentation/handbook/user-input-validation/)

## Undo and Redo

**Accepted recommendation:** Hybrid rollout with control-local undo allowed as leaf history while global undo and redo remain authoritative at the FieldWorks or LCModel transaction layer.

**Why this won:** Avalonia controls have local undo behavior, but FieldWorks already owns the harder part: grouped edits, chooser confirmation, nested tasks, selection preservation, and persisted project history.

**Options considered:**

1. Pure FieldWorks global transaction stack.
Pros: correct semantics for project state, chooser dialogs, grouped edits, and menu labels.
Cons: more custom control integration work.

2. Package-first object or view-model history.
Pros: fast proof of concept for simple forms.
Cons: wrong source of truth for LCModel-backed editing, high risk of multiple competing histories.

3. Hybrid control-local undo plus global LCModel undo routing.
Pros: best desktop UX, allows native text editing behavior without giving up domain-authoritative history.
Cons: requires explicit routing rules when focus is inside a control.

**Stage / invocation:** Allow leaf-control undo from the first render; require authoritative routing on the first editable slice.

**Change-gears trigger:** Revisit only for a specific owned control that needs document-local undo semantics richer than FieldWorks currently provides, and only if that control still hands persisted changes back through the domain-authoritative undo boundary.

**Key references:**

- [Avalonia TextBox API](https://api-docs.avaloniaui.net/docs/T_Avalonia_Controls_TextBox)
- [Avalonia commanding](https://docs.avaloniaui.net/docs/input-interaction/commanding)
- [Avalonia keyboard and hotkeys](https://docs.avaloniaui.net/docs/input-interaction/keyboard-and-hotkeys)
- [Avalonia Rich Text Editor docs](https://docs.avaloniaui.net/controls/input/text-input/richtexteditor)

## Validation

**Accepted recommendation:** FieldWorks-owned validation seam with Avalonia-native presentation and optional package-backed validators behind the seam.

**Why this won:** Avalonia is good at surfacing errors, but FieldWorks needs durable rules that can run without forcing UI materialization, survive preview host and headless tests, and stay independent of whichever MVVM helper package a given screen uses.

**Options considered:**

1. Native Avalonia validation plus `INotifyDataErrorInfo` or `ObservableValidator`.
Pros: simple, lightweight, accessible, good for dialogs.
Cons: weaker for cross-field, cross-object, async, and localized rule sets.

2. Package-led validation with FluentValidation or ReactiveUI.Validation.
Pros: strong rule composition, strong async and collection support, excellent tests.
Cons: still needs an Avalonia adapter, and should not become the public migration contract.

3. Hybrid domain validation seam with Avalonia adapters.
Pros: best fit for lexical editing, reusable across Avalonia and tests, keeps UI and domain responsibilities distinct.
Cons: needs structured issue paths, severity, and localization contracts.

**Stage / invocation:** Simple shell dialogs may use option 1 immediately; migrated lexical editors must use the hybrid seam by the first editable slice.

**Change-gears trigger:** If rule complexity stays low and no cross-object or async validation emerges for a surface, that surface may collapse back toward option 1. If rule composition expands sharply, add more FluentValidation behind the seam instead of moving the seam itself.

**Key references:**

- [Avalonia binding validation](https://docs.avaloniaui.net/docs/data-binding/binding-validation)
- [Avalonia compiled bindings](https://docs.avaloniaui.net/docs/data-binding/compiled-bindings)
- [ObservableValidator](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/observablevalidator)
- [FluentValidation docs](https://docs.fluentvalidation.net/en/latest/)
- [ReactiveUI user input validation](https://www.reactiveui.net/documentation/handbook/user-input-validation/)
- [AvaloniaUI - Validation in ViewModel](https://andydunkel.net/2023/02/02/avaloniaui-validation-in-viewmodel/)

## Command and Focus

**Accepted recommendation:** FieldWorks-owned global command and focus bridge for shell and popup behavior, while still allowing screen-local Avalonia commands inside migrated regions.

**Why this won:** Avalonia built-ins are enough for local `ICommand`, shortcuts, and focus navigation, but FieldWorks needs stable command IDs, active-target resolution, menu or toolbar parity, popup focus return, and compatibility with existing XCore state.

**Options considered:**

1. Mostly Avalonia built-ins with minimal wrappers.
Pros: fast and idiomatic for isolated screens.
Cons: not enough for shell-wide target routing or XCore parity.

2. Package-oriented MVVM command layer using CommunityToolkit or ReactiveUI.
Pros: great local ergonomics, async command support, strong tests.
Cons: improves local code more than architecture, still needs a shell-level router.

3. Custom hybrid bridge to XCore property state and navigation services.
Pros: best shell fit, shared command descriptors, explicit target resolution, strongest popup-focus story.
Cons: highest design cost, easy to over-preserve legacy quirks if left unchecked.

**Stage / invocation:** Use screen-local command helpers immediately where safe; require the bridge for the first editable slice where popup focus return matters, and promote it to shell-global use in `fieldworks-avalonia-shell-migration`.

**Change-gears trigger:** If shell-global needs remain much simpler than expected, more surfaces may collapse toward local built-ins or Toolkit commands. If the bridge starts preserving legacy quirks without user value, narrow it rather than expanding it.

**Key references:**

- [Avalonia commanding](https://docs.avaloniaui.net/docs/input-interaction/commanding)
- [Avalonia keyboard and hotkeys](https://docs.avaloniaui.net/docs/input-interaction/keyboard-and-hotkeys)
- [Avalonia focus](https://docs.avaloniaui.net/docs/input-interaction/focus)
- [Avalonia main window guidance](https://docs.avaloniaui.net/docs/fundamentals/main-window)
- [MVVM Toolkit RelayCommand](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/relaycommand)
- [ReactiveUI commands](https://www.reactiveui.net/documentation/handbook/commands/)
- [Eric Sink on ReactiveUI complexity tradeoffs](https://ericsink.com/entries/dont_use_rxui.html)

## UI Scheduler

**Accepted recommendation:** Thin FieldWorks-owned UI scheduling seam for non-view layers, with direct Avalonia dispatcher usage allowed only at the UI edge.

**Why this won:** Avalonia already has the dispatcher primitives. The real problem is keeping presenters, edit sessions, and services from taking a hard dependency on Avalonia when they only need a testable way to marshal back to the UI thread.

**Options considered:**

1. Direct Avalonia dispatcher use everywhere.
Pros: simplest possible code.
Cons: leaks Avalonia into non-view layers and weakens unit tests.

2. Thin wrapper around Avalonia built-ins.
Pros: best balance, easy to fake in tests, keeps Avalonia at the edge.
Cons: adds a seam that can become pointless if it only renames the API.

3. Reactive or heavier scheduler abstraction.
Pros: strong for explicitly reactive screens and test schedulers.
Cons: unnecessary as a global default if most needs are ordinary UI marshalling.

**Stage / invocation:** Apply up front in non-view code. Direct dispatcher use remains acceptable in `App`, `Program`, windows, preview host, and headless adapters.

**Change-gears trigger:** If the thin seam becomes a pure pass-through with no test or portability value, collapse low-value layers back to direct edge use. If a screen becomes fully ReactiveUI-driven, it may adopt reactive schedulers locally without changing the shared contract.

**Key references:**

- [Avalonia threading model](https://docs.avaloniaui.net/docs/app-development/threading)
- [Avalonia Dispatcher API](https://api-docs.avaloniaui.net/docs/T_Avalonia_Threading_Dispatcher)
- [Avalonia headless testing](https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform)
- [ReactiveUI testing and schedulers](https://www.reactiveui.net/documentation/handbook/testing/)

## Lifetime

**Accepted recommendation:** Thin FieldWorks-owned app, window, dialog, and shutdown seam for non-view code; direct Avalonia lifetime remains at the concrete UI edge; heavy region or document lifetime frameworks are deferred.

**Why this won:** Avalonia already exposes concrete app and window lifetime primitives. FieldWorks needs a place to centralize owner lookup, dialog coordination, shutdown requests, preview host substitution, and non-view window coordination without immediately inventing a large region framework.

**Options considered:**

1. Direct Avalonia lifetime everywhere.
Pros: fastest to implement, aligns with samples.
Cons: spreads ownership and shutdown policy through non-view code.

2. Thin wrapper around app lifetime, window ownership, dialogs, and shutdown.
Pros: best balance, helps tests, preview host, and shell migration.
Cons: another seam to maintain.

3. Heavy region or document lifetime framework.
Pros: strongest explicit lifetime tree.
Cons: highest cost and easiest to overdesign before repeated need is proven.

**Stage / invocation:** Apply the thin seam up front for non-view code, then expand it during `fieldworks-avalonia-shell-migration` when multiple windows, dialogs, and startup or shutdown paths converge.

**Change-gears trigger:** Introduce a heavier region or document lifetime framework only if repeated cross-screen or cross-window lifetime failures prove the thin seam is insufficient.

**Key references:**

- [Avalonia application lifetimes](https://docs.avaloniaui.net/docs/fundamentals/application-lifetimes)
- [Avalonia window management](https://docs.avaloniaui.net/docs/app-development/window-management)
- [Avalonia dialogs](https://docs.avaloniaui.net/docs/how-to/dialogs-how-to)
- [Avalonia top-level guidance](https://docs.avaloniaui.net/docs/fundamentals/top-level)
- [Avalonia MVVM pattern guidance](https://docs.avaloniaui.net/docs/fundamentals/the-mvvm-pattern)
