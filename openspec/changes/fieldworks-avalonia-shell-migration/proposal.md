## Why

> **Status note (2026-06-20).** This change is **folded into and superseded by `avalonia-end-game`**
> (Phase 2 of the FieldWorks Avalonia migration). `avalonia-end-game` absorbs the Avalonia default
> shell, application/window lifetime, command bridge, screen registry, dialog/service abstractions, and
> final WinForms decommissioning specified here, and adds the net48 → .NET 10 retarget and true
> Windows/macOS/Linux cutover. This proposal is preserved as historical detail; the active Phase-2 plan
> lives in `openspec/changes/avalonia-end-game/`.

The Lexical Edit Avalonia migration proves the first high-risk regional replacement, but FieldWorks will still start and compose its application through a WinForms/XCore shell. A second phase is needed to replace application lifetime, windowing, navigation, menus, dialogs, and main-screen hosting with Avalonia so the final default product is a fully Avalonia app rather than an Avalonia island inside the old shell.

## What Changes

- Add an Avalonia desktop shell using explicit application lifetime, main-window ownership, active-window tracking, dialog ownership, and shutdown services.
- Extract framework-neutral shell/window contracts so `FwApp`, `FieldWorksManager`, xWorks windows, project startup, multi-window behavior, and modal ownership no longer require `System.Windows.Forms.Form` in the default path.
- Compile/import existing shell configuration such as `Language Explorer/Configuration/Main.xml` into typed shell definitions for commands, lists, menus, context menus, sidebars, toolbars, status panes, listeners, extension includes, shortcuts, localization metadata, and screen/tool registrations.
- Introduce Avalonia shell composition for navigation, content hosting, record/side panes, menus, context menus, toolbars, status/progress, diagnostics, accessibility, and theme resources.
- Bridge XCore mediator/property-table behavior into typed Avalonia commands and state services during migration, then retire WinForms UI adapters from the default path.
- Add an Avalonia main-screen registry and migrate screens area by area after Lexical Edit gates are proven.
- Move global dialogs and services behind abstractions: project open/create/backup/restore, writing systems, settings, import/export, find/replace, styles, help, feedback, progress, keyboarding, clipboard, browser/PDF, print, and accessibility.
- Retire WinForms shell, WinForms dynamic content host, WinForms-only default dialogs, FlexUIAdapter default behavior, Gecko Graphite assumptions, and native viewing/rendering from the final default app.

## Non-goals

- No LCModel rewrite or project data schema change.
- No one-shot migration of every screen before shell seams and parity gates exist.
- No permanent WinForms embedding in the final default app.
- No removal of native/external linguistics services such as XAmple, spelling, ICU, Encoding Converters, or parser tools when isolated behind non-UI service contracts.
- No Graphite or Gecko Graphite compatibility path in the final Avalonia default UI.

## Capabilities

### New Capability

- `fieldworks-avalonia-shell-migration`: Avalonia default shell, typed shell composition, application/window lifetime, command bridge, main-screen registry, validation gates, packaging, and final WinForms shell decommissioning.

### Architecture Areas Covered

- `architecture/layers/entry-points`: Avalonia host and dual-lifetime transition.
- `architecture/ui-framework/xcore-mediator`: Mediator compatibility bridge and final composition boundary.
- `architecture/ui-framework/winforms-patterns`: WinForms shell decommissioning gates and temporary adapter rules.
- `architecture/testing/test-strategy`: Shell contract snapshots, Avalonia.Headless shell tests, UIA baselines, and full-app smoke gates.
- `architecture/interop/native-boundary`: Retained native services outside Avalonia UI boundaries; no native viewing/rendering in the default shell.
- `architecture/build-deploy/installer`: Avalonia runtime/package harvest and WinForms/Gecko retirement gates.
- `architecture/build-deploy/localization`: Shell labels, shortcuts, tooltips, status text, and dialogs preserved through typed shell migration.

## Impact

- Managed shell/framework code: `Src/Common/FieldWorks/`, `Src/Common/Framework/`, `Src/XCore/`, `Src/xWorks/`, and main FLEx screens under `Src/LexText/`.
- Avalonia code: `Src/Common/FwAvalonia/`, `Src/Common/FwAvaloniaPreviewHost/`, and future FieldWorks Avalonia shell projects.
- Configuration/localization: `DistFiles/Language Explorer/Configuration/Main.xml`, area/tool XML includes, existing localization resources, and Crowdin integration.
- Native/interop: no new native UI surface; retained native/external services remain behind service boundaries; native Views/Graphite/Gecko Graphite are excluded from the default Avalonia UI.
- Build/deploy: traversal build, solution integration, installer/runtime packaging, app startup path, and dependency harvest.