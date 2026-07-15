---
name: fieldworks-ui-wiring-review
description: "Review or change FieldWorks UI wiring — app-setting and PropertyTable routing, mediator notifications, current-content switching, host replacement, preview-vs-product boundaries, and the global legacy-vs-Avalonia UI selection. Use whenever a change touches which UI host is active, how a setting reaches a screen, RecordEditView/currentContentControl routing, save/PrepareToGoAway paths, or fallback behavior — even if the diff looks like a one-line settings change."
---

# FieldWorks UI Wiring Review

## Use This For

- Global or screen-level UI mode selection.
- `PropertyTable`, app-setting, mediator, or listener changes that affect
  which UI host is active.
- `RecordEditView`, `currentContentControl`, host replacement, save or
  `PrepareToGoAway()` routing, focus or command target routing, and
  preview-to-product promotion work.

## Canonical Wiring

The decided routing model is explicit per-host behavior — supported
Avalonia, explicit legacy fallback, or blocked — never silent fallback:

- Surface selection: `Src/Common/FwAvalonia/LexicalEditSurfaceSelectionService.cs`,
  `LexicalEditSurfaceResolver.cs` (behavior enum + routing logic)
- Approved legacy adapters: `Src/Common/FwAvalonia/Seams/ActiveHostContract.cs`
- Contract tests to imitate:
  `Src/xWorks/xWorksTests/RecordEditViewActiveHostContractTests.cs`,
  `Src/Common/FwAvalonia/FwAvaloniaTests/SurfaceAndHostContractTests.cs`,
  `LexicalEditSurfaceResolverTests.cs`

## Required Checks

- Review scope against the branch-only diff (`main..HEAD`) and list every
  host or consumer affected.
- Trace the full wiring path end to end: setting source, persisted state,
  `PropertyTable` key, mediator or property broadcast, listener
  registration, host reload path, focus or command target routing, save or
  `PrepareToGoAway()` path, and fallback or blocked state.
- For global switches, verify each current consumer has an explicit
  contract: supported Avalonia surface, explicit legacy fallback, or
  resource-backed unsupported state.
- The active Avalonia route must not instantiate or drive hidden legacy
  rendering or menu infrastructure except through `ActiveHostContract`
  approved adapters — and prove the negative with a contract test, not by
  inspection alone.
- Product wiring and preview wiring are reviewed separately; preview DTOs,
  preview hosts, and spike-only semantics do not satisfy product routing.
- Validation uses the normal repo build and test path (`./build.ps1`,
  `./test.ps1`) plus host-specific tests when wiring changes.

## Review Red Flags

- Tests manually call `OnPropertyChanged(...)`, `ShowRecord()`, or similar
  handlers instead of driving the real setting and broadcast path.
- A preview-only mapper or detached DTO model sits on a product-facing
  route.
- Hidden legacy `DataTree`, menu handler, or renderer is still initialized
  and driven while Avalonia is the active host.
- A global setting changes unrelated screens without a manifest or
  explicit fallback story.
- Build or test evidence relies mainly on branch-only optional lanes or ad
  hoc commands.

## Handoff

Report the setting source, listeners, affected hosts, per-host fallback
state, executable proof of the live wiring path, and any remaining hidden
legacy dependencies.

## Keep This Skill Current

When a new host type, routing pattern, or wiring failure mode appears in a
migration, add it here (and a red flag if it is a review smell) in the same
PR. Durable lessons also go through
`fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
