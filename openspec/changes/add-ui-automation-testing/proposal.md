## Why

FieldWorks has strong unit and integration coverage, but it does not have a maintained desktop UI automation lane for launch, shell navigation, and screenshot-backed regression checks. The default CI workflow already excludes `DesktopRequired` tests, which keeps routine validation fast but leaves core UI regressions dependent on manual verification.

## What Changes

- Add a managed desktop UI automation test project under `Src/Common/FieldWorks/` that follows current repo conventions: SDK-style `net48`, central PackageReference management, VSTest-compatible execution through a dedicated `testui.ps1` path, and `Output/<Configuration>` binaries.
- Add a separate top-level `testui.ps1` script for desktop UI automation so it remains operationally isolated from the existing `test.ps1` unit/integration path.
- Build a shared FlaUI-based harness for launching `FieldWorks.exe`, acquiring the main window, restoring focus, navigating menus, capturing screenshots, and shutting down safely.
- Isolate launched-app state for automation runs by giving the child process a dedicated project-data root and dedicated application-settings root, instead of relying on in-process test doubles or the developer's normal profile.
- Add initial smoke and navigation scenarios against `TestLangProj`, covering app launch, project open, shell/menu visibility, and stable transitions into a small set of core views.
- Make local UI automation default to an isolated Windows Sandbox launch path, while keeping a direct desktop-host path available when explicitly requested for debugging.
- Add a dedicated UI automation workflow for `windows-latest` that still uses `build.ps1` plus `testui.ps1`, publishes TRX output and screenshots, and does not slow or destabilize the default managed CI lane.
- Document the execution model, environment requirements, and fallback decisions so future work can add deeper workflows or optional WebDriver/Appium adapters without re-deciding the architecture.

## Non-goals

- Replatforming FieldWorks tests to `net9.0` or `dotnet test`.
- Folding desktop UI automation into the existing `test.ps1` script or default managed CI lane.
- Replacing the existing unit/integration test strategy or making UI automation part of the default PR gate immediately.
- Introducing Appium or FlaUI.WebDriver as a required runtime dependency for the first implementation.
- Building pixel-perfect screenshot diffing, AI vision checks, or broad end-to-end editing workflows in the first slice.
- Making native C++ or installer changes beyond what is required to execute managed UI tests.

## Capabilities

### New Capabilities
- `ui-automation-testing`: Direct FlaUI-based desktop automation for launching FieldWorks, isolating test state, navigating the shell, and collecting screenshot evidence locally through Windows Sandbox by default and on GitHub Actions through a dedicated script.

### Modified Capabilities
- `architecture/testing/test-strategy`: Add a dedicated desktop UI automation lane, category guidance, and explicit separation from the default managed CI test invocation.

## Impact

- **Affected code:** `testui.ps1`, `Src/Common/FieldWorks/FieldWorks.csproj`, new `Src/Common/FieldWorks/FieldWorksUiAutomationTests/**`, `Src/Common/FwUtils/**`, `Directory.Packages.props`, `FieldWorks.sln`, `.github/workflows/**`
- **Execution model:** Managed C# only; build via `./build.ps1`, run UI automation via `./testui.ps1`; standard CI and `./test.ps1` remain unchanged while UI automation runs in a separate local/CI path.
- **Dependencies:** FlaUI v5.0.0 packages (`FlaUI.Core`, `FlaUI.UIA3`), pinned through central package management. FlaUI v5.0.0 explicitly targets `net48` and `.NET 6.0`; compatibility is confirmed.
- **Risk:** Medium. The main risks are WinForms automation quirks, first-run dialogs, focus flakiness, Sandbox artifact handoff, and Windows runner desktop behavior; the design keeps these risks in a dedicated lane and addresses them with process-scoped isolation and screenshot evidence.