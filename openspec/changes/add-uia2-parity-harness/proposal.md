## Why

The Lexical Edit Avalonia migration now has focused in-repo smoke baselines, but no reusable harness that can describe one user workflow and run it against isolated Avalonia slices, legacy WinForms surfaces, or full FieldWorks startup journeys. We need that hybrid harness before incremental Avalonia slices can be trusted in CI or by agents as parity evidence against the existing UI.

## What Changes

- Add a shared parity-smoke harness contract that describes a scenario once and runs it against one or more backends.
- Add a truly headless Avalonia slice harness for focused partial-integration tests that do not need full app startup.
- Add a desktop automation harness for realized-window tests, including legacy WinForms UIA2/FlaUI-style reachability and full-app Appium/WinAppDriver journeys.
- Allow the same smoke test to run headless-only, desktop-only, or paired comparisons depending on scenario scope and what is available in CI or an agent workstation.
- Define normalized observation artifacts so parity reports compare semantic behavior rather than raw control implementation details.
- Add CI/agent entry points that run a stable headless subset by default and broader interactive desktop automation when a Windows automation environment is available.

## Non-goals

- Replacing the existing unit, integration, render, or Avalonia.Headless tests.
- Requiring all CI machines to run desktop automation before the harness has environment detection and skip/report semantics.
- Treating UIA2 as a deep inspector for owner-drawn Views content; those paths still need render, semantic, and model assertions.
- Migrating Lexical Edit controls in this change. This change only creates the reusable parity harness and scenario contract.

## Capabilities

### New Capabilities

- `uia2-parity-harness`: Shared parity-smoke scenario contract, headless slice harness, desktop automation harness, WinForms/UIA2 backend, Appium/WinAppDriver full-app backend, Avalonia.Headless backend, normalized observations, artifacts, and CI/agent execution rules.

### Modified Capabilities

None. This proposal adds a focused harness capability that supports the existing `lexical-edit-parity-automation` direction without changing that capability's requirement text.

## Impact

- Managed C# test infrastructure under shared test-support projects or a new focused harness project.
- Legacy WinForms surfaces under `Src/Common/Controls/DetailControls/`, `Src/Common/Controls/XMLViews/`, and `Src/xWorks/` through test-only UIA2 adapters or focused realized-window test hosts.
- Avalonia test surfaces under `Src/LexText/AdvancedEntry.Avalonia/` and shared `Src/Common/FwAvalonia/` test infrastructure through Avalonia.Headless for slice tests and Appium/WinAppDriver for future real-window/full-app checks.
- CI and agent scripts/tasks that select headless-only, desktop-only, or paired parity-smoke modes.
- No native C++ behavior changes are expected, though migrated-region evidence may later use this harness alongside native/render dependency audits.