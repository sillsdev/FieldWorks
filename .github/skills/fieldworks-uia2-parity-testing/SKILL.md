---
name: fieldworks-uia2-parity-testing
description: Use when designing or reviewing FieldWorks UI automation, UIA2, FlaUI, Appium, WinAppDriver, Avalonia.Headless, accessibility, keyboard, focus, or IME parity tests.
---

# FieldWorks UIA2 Parity Testing

## Lane Separation
- Avalonia.Headless is for fast in-process control, layout, view-model, binding, and input tests.
- UIA2/FlaUI/Appium/WinAppDriver tests require realized desktop windows and validate native accessibility trees, focus, invoke patterns, and product integration.
- Do not call a headless smoke test a UIA2 baseline.

## Path 3 Role
In a Path 3 parity bundle, UIA2/FlaUI/Appium contributes the workflow/accessibility lane only:

- launcher/chooser reachability,
- focus movement and focus return,
- invoke/cancel/accept paths,
- native automation tree identity,
- shell-level keyboard behavior.

It does not replace semantic snapshots or visual/render evidence. A desktop automation result should be reported alongside the semantic and visual artifacts for the same scenario id.

## Required Evidence
- Stable automation IDs or accessible names for controls under test.
- Explicit coverage of focus movement, invoke/click path, popup/chooser reachability, keyboard shortcuts, and failure artifacts.
- When UI mode or host wiring changes, desktop automation must cover the real switch-driven host refresh or fallback behavior on realized windows; manual handler calls or headless-only assertions do not prove product wiring.
- Clear CI lane: headless can run broadly; desktop automation needs an interactive Windows desktop or a configured automation host.

## Review Red Flags
- “Runs in the background” used for UIA2/Appium without explaining the required desktop/session.
- Manual `OnPropertyChanged(...)` or similar handler invocation is presented as proof of live UI-mode wiring.
- Tests assert implementation internals instead of user-observable accessibility behavior.
- Automation selectors rely on localized labels when stable IDs are available or required.
- IME coverage is claimed without a real text editor/control surface and input-method evidence.

## Handoff
Classify each test as headless, native desktop automation, or smoke substitute, and state what parity claim it can and cannot support. When used in a Path 3 bundle, say explicitly which workflow/accessibility assertions the desktop lane proved, whether switch wiring/fallback was exercised on a realized window, and which claims still need another lane.