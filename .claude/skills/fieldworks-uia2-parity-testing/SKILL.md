---
name: fieldworks-uia2-parity-testing
description: "Design or review FieldWorks UI automation and accessibility tests: UIA2, FlaUI, Appium, WinAppDriver, Avalonia.Headless, keyboard, focus, IME, and automation-id strategy. Use whenever a task adds, changes, or evaluates automated UI tests or accessibility/workflow parity claims for WinForms or Avalonia surfaces — including deciding whether a test belongs in the headless or desktop environment."
---

# FieldWorks UIA2 Parity Testing

## Environment Separation

- Avalonia.Headless is for fast in-process control, layout, view-model,
  binding, and input tests.
- UIA2/FlaUI/Appium/WinAppDriver tests require realized desktop windows and
  validate native accessibility trees, focus, invoke patterns, and product
  integration.
- Do not call a headless smoke test a UIA2 baseline — see
  parity-evidence.md §2.

## Role in the Parity Bundle

In a Path 3 parity bundle (defined in
`fieldworks-winforms-to-avalonia-migration/references/parity-evidence.md`),
desktop automation contributes the workflow/accessibility evidence only:
launcher/chooser reachability, focus movement and return, invoke/cancel/
accept paths, native automation tree identity, and shell-level keyboard
behavior. It does not replace semantic snapshots or visual/render evidence;
report it alongside those artifacts for the same scenario id.

## Canonical Examples

- Headless app/test setup:
  `Src/Common/FwAvalonia/FwAvaloniaTests/TestAppBuilder.cs`; input and
  focus patterns in `RegionEditingTests.cs`, `RegionFocusMemoryTests.cs`
- Realized-window UIA smoke on the legacy product path:
  `Src/xWorks/xWorksTests/WinFormsUiaSmokeTests.cs`
- Preview-host UIA:
  `Src/Common/FwAvalonia/FwAvaloniaPreviewHostTests/PreviewHostUiaTests.cs`
- Automation-id locking: `Src/Common/FwAvalonia/FwAvaloniaTests/OwnedControlAutomationConventionTests.cs`

## Automation Identity

Derive AutomationIds from the IR `StableId` (`{StableId}`,
`{StableId}.Label`, `{StableId}.{WsAbbrev}`), defined as code constants —
never resource keys, never localized text. Localized names/tooltips go on
`AutomationProperties.Name`. Owned controls need custom automation peers
when stock peers do not expose the required patterns.

## Required Evidence

- Stable automation IDs or accessible names for controls under test.
- Explicit coverage of focus movement, invoke/click path, popup/chooser
  reachability, keyboard shortcuts, and failure artifacts.
- When UI mode or host wiring changes, desktop automation must cover the
  real switch-driven host refresh or fallback behavior on realized windows;
  manual handler calls or headless-only assertions do not prove product
  wiring.
- Clear CI placement: headless tests can run broadly; desktop automation
  needs an interactive Windows desktop or a configured automation host.

## Review Red Flags

- "Runs in the background" used for UIA2/Appium without explaining the
  required desktop/session.
- Manual `OnPropertyChanged(...)` or similar handler invocation presented
  as proof of live UI-mode wiring.
- Tests assert implementation internals instead of user-observable
  accessibility behavior.
- Automation selectors rely on localized labels when stable IDs are
  available or required.
- IME coverage claimed without a real text editor/control surface and
  input-method evidence. (IME composition/commit is a known open gap for
  rich-text scope — do not let a checkbox claim it implicitly.)
- Sleep-based waits instead of event-driven synchronization.

## Handoff

Classify each test as headless, native desktop automation, or smoke
substitute, and state what parity claim it can and cannot support. For
bundle work, say which workflow/accessibility assertions the desktop
environment proved, whether switch wiring/fallback was exercised on a
realized window, and which claims still need another environment.

## Keep This Skill Current

When a new automation pattern, peer implementation, CI-placement constraint, or
flakiness fix proves out, add it here in the same PR; route durable lessons
through `fieldworks-winforms-to-avalonia-migration/references/lessons-learned.md`.
