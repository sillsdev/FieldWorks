---
name: fieldworks-uia2-parity-testing
description: Use when designing or reviewing FieldWorks UI automation, UIA2, FlaUI, Appium, WinAppDriver, Avalonia.Headless, accessibility, keyboard, focus, or IME parity tests.
---

# FieldWorks UIA2 Parity Testing

## Lane Separation
- Avalonia.Headless is for fast in-process control, layout, view-model, binding, and input tests.
- UIA2/FlaUI/Appium/WinAppDriver tests require realized desktop windows and validate native accessibility trees, focus, invoke patterns, and product integration.
- Do not call a headless smoke test a UIA2 baseline.

## Required Evidence
- Stable automation IDs or accessible names for controls under test.
- Explicit coverage of focus movement, invoke/click path, popup/chooser reachability, keyboard shortcuts, and failure artifacts.
- Clear CI lane: headless can run broadly; desktop automation needs an interactive Windows desktop or a configured automation host.

## Review Red Flags
- “Runs in the background” used for UIA2/Appium without explaining the required desktop/session.
- Tests assert implementation internals instead of user-observable accessibility behavior.
- Automation selectors rely on localized labels when stable IDs are available or required.
- IME coverage is claimed without a real text editor/control surface and input-method evidence.

## Handoff
Classify each test as headless, native desktop automation, or smoke substitute, and state what parity claim it can and cannot support.