## 1. Project Skeleton And Package Setup

- [ ] 1.1 Add central package entries for `FlaUI.Core` and `FlaUI.UIA3` (v5.0.0+) in `Directory.Packages.props`. FlaUI v5.0.0 targets `net48` and `.NET 6.0`; compatibility is confirmed and no spike is needed. [Managed C#, ~30 min]
- [ ] 1.2 Create `Src/Common/FieldWorks/FieldWorksUiAutomationTests/FieldWorksUiAutomationTests.csproj` as an SDK-style `net48` test project with a project reference to `Src/Common/FieldWorks/FieldWorks.csproj`. [Managed C#, ~45 min]
- [ ] 1.3 Update `Src/Common/FieldWorks/FieldWorks.csproj` to exclude `FieldWorksUiAutomationTests/**` from app compilation and add the new project to `FieldWorks.sln`. [Managed C#, ~30 min]

## 2. Dedicated UI Runner Script

- [ ] 2.1 Add a top-level `testui.ps1` script that owns all desktop UI automation invocation and stays operationally separate from `test.ps1`. [PowerShell, ~1 hr]
- [ ] 2.2 Add a warm-sandbox setup script (invoked by `testui.ps1` on first use or via a setup subcommand) that generates a `.wsb` configuration mapping the host's `Output/Debug/` read-only and a writable results folder, includes a `LogonCommand` that installs VSTest prerequisites inside the sandbox, and launches the sandbox minimized on the host side. The sandbox stays alive between test invocations within a session; `testui.ps1` re-uses it for subsequent runs. Only the main repo checkout is supported for sandbox mapping (not worktrees). [PowerShell, ~2 hr]
- [ ] 2.3 Implement sandbox↔host communication using mapped-folder + file-based signaling: the sandbox-side runner writes `_status.json` (phase and pass/fail counts) and a `_done` sentinel to the writable results folder; the host-side `testui.ps1` polls the status file and treats the sentinel as the completion signal. [PowerShell, ~1 hr]
- [ ] 2.4 Add an explicit non-sandbox mode to `testui.ps1` for direct desktop debugging and CI runner execution, while preserving the same test assembly contract and artifact layout. [PowerShell, ~45 min]

## 3. Process-Scoped Isolation For Launched FieldWorks.exe

- [ ] 3.1 Add process-scoped project-root override support in `Src/Common/FwUtils/FwDirectoryFinder.cs` so launched automation runs can point `ProjectsDirectory` at a temp root without mutating the developer's normal HKCU value. [Managed C#, ~1 hr]
- [ ] 3.2 Add process-scoped application-settings override support in `Src/Common/FwUtils/FwApplicationSettings.cs` and any supporting settings/bootstrap code needed to keep user config under an isolated temp root during automation runs. [Managed C#, ~1.5 hr]
- [ ] 3.3 Add focused tests for the new override behavior in `Src/Common/FwUtils/FwUtilsTests/**` and/or `Src/Common/FieldWorks/FieldWorksTests/**`, including restoration/fallback behavior when overrides are absent. [Managed C#, ~1 hr]

## 4. Shared UI Automation Harness

- [ ] 4.1 Add `Infrastructure/FwUiTestBase.cs`, `Infrastructure/FwProjectSetup.cs`, `Infrastructure/FwScreenshot.cs`, and `Infrastructure/TestSteps.cs` under `Src/Common/FieldWorks/FieldWorksUiAutomationTests/` to encapsulate launch, focus recovery, popup/menu traversal, screenshot capture, and safe shutdown. [Managed C#, ~1.5 hr]
- [ ] 4.2 Wire the harness to create a temp project workspace from `TestLangProj/`, pre-seed deterministic settings, set `FEEDBACK=false`, disable assertion UI, and resolve `Output/<Configuration>/FieldWorks.exe`. [Managed C#, ~1.5 hr]
- [ ] 4.3 Mark the UI automation assembly or fixtures as non-parallel and expose a single automation-backend selection point so UIA3 can be swapped if WinForms controls require UIA2. [Managed C#, ~45 min]

## 5. First Smoke And Navigation Coverage

- [ ] 5.1 Add a launch smoke fixture in `SmokeTests/ApplicationLaunchTests.cs` that verifies the app opens `TestLangProj`, the main window is visible, and core shell/menu elements are discoverable. [Managed C#, ~1 hr]
- [ ] 5.2 Add a small navigation fixture in `NavigationTests/AreaSwitchingTests.cs` covering a stable subset of area/view transitions and collecting screenshot evidence for each checkpoint. [Managed C#, ~1.5 hr]
- [ ] 5.3 Capture screenshots on failure to a deterministic output folder and ensure failures surface useful diagnostics in TRX output. [Managed C#, ~45 min]

## 6. CI And Local Execution Path

- [ ] 6.1 Add `.github/workflows/ui-tests.yml` that runs on `windows-latest`, builds with `./build.ps1 -Configuration Debug -Platform x64 -BuildTests`, runs the new test project through `./testui.ps1`, and uploads TRX/screenshot artifacts. [Managed C# + PowerShell, ~1 hr]
- [ ] 6.2 Keep the default managed CI lane unchanged, but tag UI automation tests with both `UiAutomation` and `DesktopRequired` so they stay discoverable while remaining out of the standard `CI.yml` filter path. [Managed C#, ~30 min]
- [ ] 6.3 Add a short local execution guide in `Src/Common/FieldWorks/FieldWorksUiAutomationTests/README.md` covering Sandbox-first local runs, direct desktop fallback mode, prerequisites, and Windows developer/agent assumptions. [Docs, ~45 min]

## 7. Validation

- [ ] 7.1 Run `./build.ps1 -Configuration Debug -Platform x64 -BuildTests` and fix any managed build issues introduced by the new project or overrides. [Validation, ~30 min]
- [ ] 7.2 Run `./testui.ps1` locally in its default warm-Sandbox mode and verify smoke/navigation stability plus host-side artifact retrieval through the mapped results folder and status-file polling. [Validation, ~45 min]
- [ ] 7.3 Run `./testui.ps1` in direct desktop mode on a developer workstation to verify the debugging fallback path still works when Sandbox is not desired. [Validation, ~30 min]
- [ ] 7.4 Run the dedicated GitHub Actions workflow, confirm screenshot/TRX artifacts are published, and capture any runner-specific gaps before promoting the lane beyond manual/scheduled use. [Validation, ~45 min]