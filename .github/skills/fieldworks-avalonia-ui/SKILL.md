---
name: fieldworks-avalonia-ui
description: Use when creating, reviewing, or fixing Avalonia UI modules in FieldWorks, especially XAML, MVVM, preview-host, localization, accessibility, product-vs-preview wiring, or net48/net8 Avalonia test changes.
---

# FieldWorks Avalonia UI

## Use This For
- Avalonia XAML, view models, commands, lifetimes, dispatching, and resource/style changes.
- New or changed projects under `Src/**/**/*.Avalonia/`, `Src/Common/FwAvalonia/`, and `Src/Common/FwAvaloniaPreviewHost/`.
- Preview Host module registration, sample data providers, and UI diagnostics.
- Global or per-screen UI host wiring that selects between Avalonia and legacy UI, including app-setting, `PropertyTable`, mediator, and product-vs-preview routing changes.

## Required Checks
- Use current Avalonia docs for uncertain APIs; do not guess dispatcher, headless, automation, or binding behavior.
- Keep product UI strings localizable; prototype hardcoded strings must be called out as gaps.
- Stable accessibility identity belongs on user-facing controls via Avalonia automation properties.
- UI work should stay in bindings/view models where practical; avoid logic-heavy code-behind.
- Keep module preview data lightweight unless the change explicitly opts into LCModel/project data.
- If a change touches a UI mode or host switch, trace the full wiring path: setting source, `PropertyTable`/mediator broadcast, listener registration, host reload path, focus/command routing, and explicit fallback state for every affected consumer.
- Global runtime switches are product behavior. Audit every affected host/consumer, not only the first lexical surface.
- Product-facing Avalonia paths must use real edit-session/domain contracts; detached DTO-only models remain preview-only.
- Preserve repo build/test entry points: `./build.ps1` and `./test.ps1`, and make sure Avalonia projects/tests are covered through the normal repo graph rather than only through optional branch-specific lanes.
- For Path 3 visual parity, remember the official Avalonia behavior: headless tests can simulate keyboard/mouse/text input on `Window`, `Dispatcher.UIThread.RunJobs()` flushes deferred UI work, and visual regression capture requires Skia + `UseHeadlessDrawing=false` with `CaptureRenderedFrame()`.
- Stamp stable `AutomationProperties.Name` and `AutomationProperties.AutomationId` on user-facing controls that participate in parity bundles so the UIA/accessibility lane can identify them reliably.

## Review Red Flags
- A Common project directly references a feature module without an explicit architecture decision.
- Preview-only code is launched from product UI without a feature gate and real-project behavior story.
- Tests manually call `OnPropertyChanged(...)`, `ShowRecord()`, or similar handlers instead of proving the real runtime broadcast/wiring path.
- The active Avalonia path still initializes or drives hidden legacy rendering/menu infrastructure without an explicit approved baseline-only reason.
- A product-facing route uses preview host code, preview DTOs, or a lossy mapper as if it were a migrated surface.
- Optional or branch-specific Avalonia build/test lanes are treated as the only integration evidence.
- Sleep-based or timing-sensitive UI tests.
- Claims of accessibility, localization, IME, or keyboard parity without executable evidence.

## Handoff
Report exact Avalonia docs consulted, tests run, remaining prototype gaps, whether the change is product-facing or preview-only, and how the live wiring path was validated for each affected host. For Path 3 work, say whether the visual evidence is control-level headless capture or live desktop capture, and which accessibility identities were assigned via `AutomationProperties`.