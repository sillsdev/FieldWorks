---
name: fieldworks-avalonia-ui
description: Use when creating, reviewing, or fixing Avalonia UI modules in FieldWorks, especially XAML, MVVM, preview-host, localization, accessibility, or net8 Avalonia test changes.
---

# FieldWorks Avalonia UI

## Use This For
- Avalonia XAML, view models, commands, lifetimes, dispatching, and resource/style changes.
- New or changed projects under `Src/**/**/*.Avalonia/`, `Src/Common/FwAvalonia/`, and `Src/Common/FwAvaloniaPreviewHost/`.
- Preview Host module registration, sample data providers, and UI diagnostics.

## Required Checks
- Use current Avalonia docs for uncertain APIs; do not guess dispatcher, headless, automation, or binding behavior.
- Keep product UI strings localizable; prototype hardcoded strings must be called out as gaps.
- Stable accessibility identity belongs on user-facing controls via Avalonia automation properties.
- UI work should stay in bindings/view models where practical; avoid logic-heavy code-behind.
- Keep module preview data lightweight unless the change explicitly opts into LCModel/project data.
- Preserve repo build/test entry points: `./build.ps1` and `./test.ps1`.
- For Path 3 visual parity, remember the official Avalonia behavior: headless tests can simulate keyboard/mouse/text input on `Window`, `Dispatcher.UIThread.RunJobs()` flushes deferred UI work, and visual regression capture requires Skia + `UseHeadlessDrawing=false` with `CaptureRenderedFrame()`.
- Stamp stable `AutomationProperties.Name` and `AutomationProperties.AutomationId` on user-facing controls that participate in parity bundles so the UIA/accessibility lane can identify them reliably.

## Review Red Flags
- A Common project directly references a feature module without an explicit architecture decision.
- Preview-only code is launched from product UI without a feature gate and real-project behavior story.
- Sleep-based or timing-sensitive UI tests.
- Claims of accessibility, localization, IME, or keyboard parity without executable evidence.

## Handoff
Report exact Avalonia docs consulted, tests run, remaining prototype gaps, and whether the change is product-facing or preview-only. For Path 3 work, say whether the visual evidence is control-level headless capture or live desktop capture, and which accessibility identities were assigned via `AutomationProperties`.