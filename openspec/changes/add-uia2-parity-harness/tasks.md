## 1. Harness Foundation

- [ ] 1.1 Managed C#: Create shared parity-smoke contracts under `Src/Common/UiParity/` or an approved existing test-support location for scenario IDs, scope classification, backend capabilities, actions, observations, comparison results, and artifact sinks.
- [ ] 1.2 Managed C#: Add contract tests proving one scenario definition can select `HeadlessOnly`, `DesktopOnly`, and `Paired` modes without referencing concrete UI frameworks.
- [ ] 1.3 Managed C#: Add normalized observation models for stable node ID, accessible ID/name, role/editor kind, enabled/visible state, focus owner, available actions, selected value/display text, table header order, filter affordances, popup/dialog state, and diagnostics.
- [ ] 1.4 Managed C#: Add paired comparison classification for missing node, wrong order, missing action, focus mismatch, accessibility mismatch, value mismatch, unsupported backend behavior, and infrastructure failure.
- [ ] 1.5 Managed C#: Add any new harness project to `FieldWorks.proj` and `FieldWorks.sln` so `./build.ps1` and `./test.ps1` include it through the normal traversal flow.

## 2. Headless Slice Harness

- [ ] 2.1 Managed C#: Implement the Avalonia.Headless slice backend using the existing `Src/LexText/AdvancedEntry.Avalonia/AdvancedEntry.Avalonia.Tests` setup.
- [ ] 2.2 Managed C#: Support headless scenario actions for focus, text input, keyboard shortcuts, mouse/click activation, flyout/context-menu activation where available, and dispatcher flushing.
- [ ] 2.3 Managed C#: Emit normalized observations from Avalonia controls, including accessibility metadata, focus owner, visible text/value, available actions, validation state, and disposal/subscription cleanup where observable.
- [ ] 2.4 Managed C#: Add headless backend self-tests proving it runs without desktop UIA2, Appium, WinAppDriver, or a visible app window.

## 3. Desktop Automation Harness

- [ ] 3.1 Managed C#: Decide and document the adapter packages for the first implementation: prefer `FlaUI.UIA2` for focused legacy WinForms surfaces and `Appium.WebDriver` with WinAppDriver for full-app journeys unless a proof spike rejects either choice.
- [ ] 3.2 Managed C#: Implement desktop environment detection for interactive desktop availability, UIA2/FlaUI availability, Appium/WinAppDriver availability, screen/session state, timeout policy, and not-runnable diagnostics.
- [ ] 3.3 Managed C#: Implement the focused WinForms UIA2 backend behind a single adapter layer with deterministic window ownership/placement and no global COM registration.
- [ ] 3.4 Managed C#: Implement an Appium/WinAppDriver backend for full-app journeys that can launch or attach to the approved FieldWorks application host and navigate by stable automation IDs/names.
- [ ] 3.5 Managed C#: Add desktop backend self-tests that verify unavailable desktop automation environments produce explicit skipped/not-runnable capability results instead of product failures.
- [ ] 3.6 Managed C#: Add desktop backend self-tests that verify offscreen placement is used only after the automation root and required children are discoverable.

## 4. Artifacts and Diagnostics

- [ ] 4.1 Managed C#: Add artifact writing under the selected test artifact path with scenario ID, scope, backend, mode, fixture, failed step, normalized observations, capability report, raw tree dump where available, screenshot where available, and comparison summary.
- [ ] 4.2 Managed C#: Add tests proving artifact bundles distinguish product behavior mismatches from backend infrastructure failures.
- [ ] 4.3 Documentation: Add short troubleshooting notes for missing automation IDs, unavailable desktop sessions, locked/disconnected RDP sessions, missing WinAppDriver server, and failed offscreen discovery.

## 5. First Smoke Scenarios

- [ ] 5.1 Managed C#: Convert the existing morph-type launcher smoke baseline into a shared scenario definition while keeping `Src/Common/Controls/DetailControls/DetailControlsTests/MorphTypeAtomicLauncherTests.cs` green.
- [ ] 5.2 Managed C#: Add a focused WinForms UIA2 execution path for the morph-type launcher scenario that focuses the launcher, invokes it, observes the chooser or chooser-decision surface, and cancels/closes deterministically.
- [ ] 5.3 Managed C#: Convert the existing XMLViews table header/filter reachability baseline into a shared scenario definition while keeping `Src/xWorks/xWorksTests/BulkEditBarTests.cs` green.
- [ ] 5.4 Managed C#: Add a focused WinForms UIA2 execution path for XMLViews table/filter reachability covering stable header order and show-all/filter-for/choose affordances.
- [ ] 5.5 Managed C#: Add the first Avalonia.Headless counterpart scenario when the migrated launcher, chooser, table, or filter control exists; until then, record the scenario as WinForms-only with an explicit migration-gated reason.
- [ ] 5.6 Managed C#: Choose and document the first Appium/WinAppDriver full-app pilot journey, such as app startup to Lexical Edit, project open to AdvancedEntry preview, or a narrower full-app launcher/chooser path.

## 6. CI and Agent Entry Points

- [ ] 6.1 Managed C#/PowerShell: Add repo-approved wrapper or VS Code task entry points for `HeadlessOnly`, `DesktopOnly`, and `Paired` parity-smoke modes under `Build/Agent/` or `scripts/Agent/`.
- [ ] 6.2 Managed C#/PowerShell: Ensure the default CI-safe mode runs headless-capable scenarios without requiring an interactive desktop, Appium, WinAppDriver, FlaUI, or UIA2.
- [ ] 6.3 Managed C#/PowerShell: Ensure desktop mode writes clear prerequisites and reports desktop automation capability before running scenario actions.
- [ ] 6.4 Managed C#/PowerShell: Ensure paired mode can compare one headless Avalonia slice run with one desktop legacy or full-app run when both backends are eligible.
- [ ] 6.5 Documentation: Add short usage notes for running the harness locally, in CI-safe headless mode, in focused WinForms UIA2 mode, and in full-app Appium/WinAppDriver mode.

## 7. Validation

- [ ] 7.1 Run `./test.ps1` for the new harness tests and the touched DetailControls, xWorks, and AdvancedEntry Avalonia test projects.
- [ ] 7.2 Run the parity-smoke wrapper in `HeadlessOnly` mode and confirm it passes without desktop automation.
- [ ] 7.3 Run the parity-smoke wrapper in `DesktopOnly` mode on a Windows desktop automation environment and capture pass/skip/failure artifacts.
- [ ] 7.4 Run the parity-smoke wrapper in `Paired` mode for at least one eligible scenario or record why no paired scenario is eligible yet.
- [ ] 7.5 Run `./build.ps1` before the implementation is considered ready for review.
- [ ] 7.6 Run `openspec validate add-uia2-parity-harness --strict` and `git diff --check` after task/spec updates.