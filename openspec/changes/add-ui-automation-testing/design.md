## Context

The attached plan is directionally right about the value of snapshot-style UI automation, but several implementation assumptions do not match the current FieldWorks repository:

- `FieldWorks.exe` and its adjacent tests are still `net48`, not `net9.0`.
- Managed tests run through `test.ps1`, which shells out to `vstest.console.exe`, not `dotnet test`.
- The user wants desktop UI automation operationally isolated from the rest of the test system, with its own entrypoint script and its own local execution model.
- Package versions are centrally managed in `Directory.Packages.props` and the `Src/` tree inherits NUnit and `Microsoft.NET.Test.Sdk` automatically from `Src/Directory.Build.props`.
- FlaUI v5.0.0 explicitly targets both `.NET Framework 4.8` and `.NET 6.0`; compatibility with the repo's `net48` target is confirmed.
- The app output layout is flattened into `Output/<Configuration>/`, so UI tests should discover `FieldWorks.exe` there rather than in a target-framework-specific `bin/...` path.
- `DummyFwRegistryHelper` is useful for in-process unit tests, but it does not isolate a separately launched `FieldWorks.exe` process.

Repo evidence also shows two environment hazards that the automation harness must neutralize before the first useful smoke test:

- `FieldWorks.cs` can show first-run reporting or update prompts when user settings are missing.
- `FwApplicationSettings` stores user-scoped data under `%LOCALAPPDATA%`, so naïve launched-app tests can pollute or depend on a developer profile.

External grounding points to a pragmatic implementation shape:

- `LordOfMyatar/Radoub` shows the direct-FlaUI harness patterns that matter most here: fixture-scoped launch, aggressive focus recovery, popup-menu search from the desktop tree, isolated settings, sequential execution, and safe shutdown through `App.Close()`.
- `FlaUI/FlaUI.WebDriver` proves that screenshot capture works on stock Windows runners and that a root-desktop session can be automated on GitHub Actions without special display setup.
- `anpin/ContextMenuContainer` shows the cost of the WebDriver/Appium variant: an extra local service bootstrap, extra capabilities wiring, and more moving parts to manage on local machines and agent-hosted Windows sessions.
- Windows Sandbox documentation plus PowerShell `Start-Process` documentation support a practical local-host model where a `.wsb` launch can be initiated from a dedicated script and the host-side launcher window can be started minimized, while the sandbox itself remains an interactive desktop environment.

## Goals / Non-Goals

**Goals**

- Add a first-class, repo-conformant UI automation test project for FieldWorks.
- Keep desktop UI automation fully separate from the existing `test.ps1` unit/integration path by introducing a dedicated `testui.ps1` runner.
- Keep the first slice focused on deterministic shell-level validation: launch, project open, visible menus, basic area navigation, and screenshot evidence.
- Isolate project data and user settings so tests are safe for local developers, GitHub Actions runners, and Copilot-driven Windows sessions.
- Make local execution default to Windows Sandbox isolation instead of reusing the developer's active desktop session.
- Keep the implementation executable with the repository’s existing scripts and CI conventions.

**Non-Goals**

- End-to-end editing coverage for every FieldWorks workflow.
- Pixel-diff or visual-baseline approval testing in the first slice.
- A cross-language automation API or mandatory Appium/WebDriver service.
- Requiring UI automation to pass in the default `CI.yml` lane before it proves stable.
- Making Windows Sandbox a CI dependency for GitHub Actions.

## Decisions

### 1. Use direct FlaUI as the primary automation model

**Decision:** The first implementation will use FlaUI directly from a managed test project instead of FlaUI.WebDriver or Appium.

**Rationale:** FieldWorks is Windows-only and the test code will live in a C# repo that already uses NUnit/VSTest. Direct FlaUI avoids running a second long-lived process, avoids WebDriver capability plumbing, and is easier to execute from local shells, GitHub Actions, and Copilot-managed Windows servers. Radoub demonstrates the exact harness patterns we need, while FlaUI.WebDriver remains a useful fallback reference for screenshot semantics and root-desktop behavior.

**Alternatives considered:**

- **FlaUI.WebDriver / Appium:** viable, but adds service startup, port management, and extra moving parts that do not buy us much for a Windows-only C# test suite.
- **WinAppDriver:** rejected due to maintenance status and poorer fit with the cited repos.

### 1a. Use a separate `testui.ps1` entrypoint for desktop automation

**Decision:** Desktop UI automation will run through a new top-level `testui.ps1` script instead of being folded into `test.ps1`.

**Rationale:** The user wants UI automation treated as a separate beast operationally. That separation also matches the repo reality that desktop automation has different environment assumptions, artifact needs, and isolation behavior than standard unit/integration tests. A dedicated script keeps the existing test surface stable while allowing UI-specific setup, sandbox orchestration, logging, and cleanup.

**Alternatives considered:**

- **Extend `test.ps1` with a `-Ui` mode:** possible, but it would blur operational boundaries and make the main test script responsible for sandbox and desktop-session concerns it otherwise does not have.

### 1b. Default local runs to a warm Windows Sandbox

**Decision:** Local UI automation will default to launching a "warm" Windows Sandbox instance from `testui.ps1`. The sandbox is pre-provisioned with prerequisites (FlaUI runtimes, VSTest, test adapter assemblies) and maps the host's `Output/Debug/` folder read-only so the sandbox sees the latest build without a full copy step. The sandbox stays alive between test invocations within a session and is only torn down when the developer explicitly stops it or closes the window. A non-sandbox host-desktop mode is retained as an explicit opt-out for debugging.

**Rationale:** Same-session desktop automation can steal focus and is unsafe to run concurrently across worktrees. A warm sandbox avoids the cold-start cost of reinstalling prerequisites on every invocation while still providing an isolated desktop. Mapping `Output/Debug/` read-only keeps the feedback loop fast: rebuild on the host, re-run `testui.ps1`, and the sandbox picks up the new binaries immediately.

**Constraints:**

- Only the main repo checkout is mapped into the sandbox. Worktrees are not supported for sandbox-mode runs because Windows Sandbox supports a single `.wsb` configuration at a time and mapping multiple independent output trees would create ambiguous state.
- The minimized launch behavior is a host-side convenience of `Start-Process`, not a claim that Windows Sandbox itself is headless or hidden.

**Alternatives considered:**

- **Host desktop by default:** simpler bootstrap, but more intrusive for local developers and too easy to collide with active work.
- **Full VM-per-worktree by default:** stronger isolation, but too heavy as the default path for the first implementation.
- **Fresh sandbox per invocation:** maximally clean slate, but prerequisite installation on every run adds unacceptable startup latency for local iteration.

### 2. Place the new project beside the FieldWorks app and keep it `net48`

**Decision:** Add `Src/Common/FieldWorks/FieldWorksUiAutomationTests/FieldWorksUiAutomationTests.csproj` as an SDK-style `net48` test project, add it to `FieldWorks.sln`, and exclude that subfolder from `FieldWorks.csproj` item globs.

**Rationale:** This matches the repository’s established component-local test layout, keeps the new project in the same solution/build graph as the app it launches, and aligns with the current `Output/<Configuration>` conventions and `test.ps1` behavior.

**Alternatives considered:**

- **Top-level `Src/FwUiAutomationTests/`:** mechanically possible, but inconsistent with the repo’s current structure.
- **A newer target framework:** conflicts with the live app/test framework and would force an unnecessary split execution model.

### 3. Add process-scoped overrides for project and settings roots

**Decision:** The implementation will add explicit process-scoped overrides for automation runs, rather than mutating HKCU and `%LOCALAPPDATA%` directly or relying on `DummyFwRegistryHelper`.

**Rationale:** The launched `FieldWorks.exe` process must see isolated project data and isolated application settings. Using explicit overrides is safer for local developer machines, more deterministic on CI, and easier to reason about on ephemeral Windows servers. The harness will create a temp root, copy `TestLangProj` into it, pre-seed update/reporting settings to suppress first-run prompts, and launch `FieldWorks.exe` with those overrides plus `FEEDBACK=false` and assertion UI disabled.

**Alternatives considered:**

- **Temporarily rewrite real HKCU `ProjectsDir`:** easy, but risky when tests crash and awkward on shared developer profiles.
- **Use `DummyFwRegistryHelper`:** only affects the host test process, not the launched app.

### 4. Keep the automation backend switchable between UIA3 and UIA2

**Decision:** The harness will default to UIA3 but centralize automation creation so the backend can be switched if WinForms-specific controls prove more reliable under UIA2.

**Rationale:** The cited repos use UIA3 successfully, especially for screenshoting and popup menus, but FlaUI’s own guidance notes that some WinForms surfaces behave better under UIA2. FieldWorks is still WinForms-heavy, so the harness should not hard-wire that decision too deeply.

### 5. Run UI automation sequentially with fixture-scoped app lifetime

**Decision:** UI automation tests will run non-parallel, with one app launch per fixture and shared helpers for focus restoration, menu traversal, popup discovery, wait loops, and screenshot-on-failure capture.

**Rationale:** This follows the most stable Radoub patterns and avoids profile/process contention. Launching once per test is too slow; launching once for the entire assembly makes state drift harder to control.

### 6. Keep UI automation in a dedicated CI lane

**Decision:** Add `.github/workflows/ui-tests.yml` that builds with `./build.ps1 -BuildTests` and runs the new project through `./testui.ps1`, publishing TRX and screenshots as artifacts. The default `CI.yml` managed-test invocation remains unchanged.

**Rationale:** The existing repo already filters `DesktopRequired` tests out of the default lane. A dedicated workflow plus dedicated runner script keeps UI automation opt-in while stability is being established and gives Windows-desktop failures their own logs and screenshots.

### 7. Use one execution contract with two environments

**Decision:** `testui.ps1` will own one execution contract for the UI automation assembly, but it will support two environments: local Sandbox by default and direct Windows runner execution for GitHub Actions.

**Rationale:** The tests, categories, output layout, and artifact conventions should stay the same regardless of environment. What changes is only how the desktop session is provisioned. This keeps the suite coherent and avoids creating effectively different test systems for local versus CI use.

### 8. Use mapped-folder + file-based signaling for sandbox↔host communication

**Decision:** The first implementation will use a mapped folder with write access combined with file-based signaling as the primary sandbox↔host communication channel. Two alternatives are documented below; the implementation may switch approaches if experience reveals a better fit.

**Three documented approaches:**

1. **Mapped writable results folder (baseline).** Map a host-side results directory into the sandbox with `ReadOnly=false`. The `LogonCommand` script inside the sandbox runs `vstest.console.exe`, writes TRX output and screenshots into the mapped folder, and exits. The host observes results by reading the mapped folder after the sandbox process terminates. *Pros:* simplest, uses only documented `.wsb` features, zero polling. *Cons:* host has no progress visibility until the sandbox shuts down, and if the sandbox crashes the results folder may be incomplete.

2. **Mapped folder + sentinel file polling (recommended first approach).** Same mapped folder as #1, but the sandbox-side script also writes a structured status file (e.g. `_status.json` with `{"phase": "running"}`, then `{"phase": "complete", "passed": N, "failed": M}`) that the host-side `testui.ps1` can poll at intervals. A final sentinel file (e.g. `_done`) signals completion. *Pros:* host gets incremental progress, can detect sandbox hangs via timeout, graceful and ungraceful exits are distinguishable. *Cons:* requires a polling loop and a status-file contract.

3. **WindowsSandbox.exe process-exit signaling.** The host launches `WindowsSandbox.exe` via `Start-Process -Wait` and treats its exit as "tests finished." The mapped results folder provides the actual outcomes. *Pros:* no polling needed, very simple host code. *Cons:* sandbox exit may not correlate with test completion if the user interacts with the sandbox window, and `WindowsSandbox.exe` exit codes are not documented as meaningful.

**Rationale for starting with #2:** It gives the host real-time progress feedback, supports timeout-based hang detection, and degrades gracefully into #1 if the polling proves unnecessary. Switching to #3 later is trivial since mapped-folder output is common to all three approaches.

### 9. Sandbox mapping is limited to the main repo checkout

**Decision:** Only the main repository checkout's `Output/Debug/` is mapped into the sandbox. Worktree output directories are explicitly not supported for sandbox-mode runs.

**Rationale:** Windows Sandbox runs a single `.wsb` configuration at a time. Mapping outputs from multiple worktrees into the same sandbox would create ambiguous state. Worktree users who want UI automation should use the direct-desktop mode or maintain separate sandbox invocations manually.

## Architecture

The first implementation should be built from six layers:

1. **Project shell**
   `FieldWorksUiAutomationTests.csproj` under `Src/Common/FieldWorks/`, using central PackageReference management and a project reference to `FieldWorks.csproj`.

2. **UI runner orchestration**
   `testui.ps1` as the only supported entrypoint for desktop UI automation, responsible for mode selection, Sandbox launch/bootstrap for local runs, direct host execution for CI, and artifact collection.

3. **Process/state isolation**
   Helpers that create a temp workspace, copy `TestLangProj`, provision deterministic settings, and launch `FieldWorks.exe` with process-scoped overrides.

4. **Automation harness**
   A base fixture that resolves `Output/<Configuration>/FieldWorks.exe`, creates the FlaUI automation object, waits for the main window, restores focus, finds popup menus via desktop search, captures screenshots, and disposes safely.

5. **Smoke/navigation tests**
   A small initial suite that verifies launch, visible shell/menu elements, and navigation into a few stable areas.

6. **Dedicated CI workflow**
   A Windows-only workflow that runs only the UI automation assembly, uploads TRX files and screenshots, and stays separate from routine managed CI.

The topology is shown in `ui-automation-topology.mmd`.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| FlaUI package updates could introduce breaking changes | Pin FlaUI.Core and FlaUI.UIA3 versions in `Directory.Packages.props` and validate against the repo's `net48` target; FlaUI v5.0.0 is confirmed compatible |
| First-run dialogs block smoke tests on clean agents | Pre-seed isolated settings and disable analytics/assert UI via process-scoped configuration |
| UIA3 behaves poorly on specific WinForms controls | Keep automation creation behind a small factory so UIA2 can be trialed without rewriting tests |
| Focus flakiness causes keystrokes or clicks to hit the wrong window | Use Radoub-style `SetForeground()` + `Focus()` retries, minimal waits, and safe close via `App.Close()` |
| Sandbox startup adds friction to local runs | Use a warm sandbox model with pre-provisioned prerequisites and mapped `Output/Debug/` folder so startup cost is limited to sandbox boot, not full environment setup; keep host-desktop fallback for debugging |
| GitHub Actions or Windows server sessions lack a usable desktop | Validate on stock `windows-latest` first and keep any session-bootstrap fallback out of the initial critical path |
| UI tests corrupt developer state | Use temp project copies plus process-scoped overrides instead of test doubles or global profile mutation |

## Open Questions

1. Should the process-scoped overrides be named as general developer knobs (`FW_PROJECTS_DIR`, `FW_SETTINGS_DIR`) or explicitly test-only knobs?
2. Should the first harness ship with both `FlaUI.UIA3` and `FlaUI.UIA2`, or start with UIA3 only and add UIA2 if smoke tests expose WinForms gaps?
3. Which initial navigation targets are stable enough for the first suite: `Lexicon Edit`, `Grammar`, `Texts`, or a smaller subset?
4. Should the dedicated workflow begin as `workflow_dispatch` plus schedule only, or also run automatically for PRs touching `Src/Common/FieldWorks/**` once the lane stabilizes?
5. ~~Should `testui.ps1` create a fresh sandbox per invocation, or support reusing a long-lived sandbox during local iteration once the bootstrap path is stable?~~ **Resolved:** warm sandbox model (Decision 1b). The sandbox stays alive between test runs; prerequisites are pre-provisioned; `Output/Debug/` is mapped read-only.
6. ~~What is the cleanest artifact-return path from sandbox to host: mapped output folder, explicit export step, or both?~~ **Resolved:** mapped writable results folder with file-based signaling (Decision 8). The sandbox writes TRX and screenshots to a write-mapped results directory; the host polls a status file for progress and completion.