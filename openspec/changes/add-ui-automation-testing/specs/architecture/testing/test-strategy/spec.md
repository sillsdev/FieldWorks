## ADDED Requirements

### Requirement: Desktop UI automation runs in a dedicated validation lane

FieldWorks SHALL keep desktop UI automation separate from the default managed CI lane while still supporting repeatable local and GitHub Actions execution.

**Affected Paths**
- `.github/workflows/CI.yml`
- `.github/workflows/ui-tests.yml`
- `test.ps1`
- `testui.ps1`
- `Src/Common/FieldWorks/FieldWorksUiAutomationTests/**`

#### Scenario: Default managed CI stays fast and non-desktop

- **WHEN** the default managed test workflow runs in `.github/workflows/CI.yml`
- **THEN** it SHALL continue excluding desktop-bound UI automation from its normal test filter
- **AND** FieldWorks UI automation tests SHALL be categorized so they do not accidentally enter that default lane

#### Scenario: Dedicated workflow runs the UI automation assembly with repo scripts

- **WHEN** the dedicated UI automation workflow runs on `windows-latest`
- **THEN** it SHALL build with `./build.ps1`
- **AND** it SHALL execute the UI automation assembly through `./testui.ps1`
- **AND** it SHALL publish TRX results and screenshot artifacts from the run

#### Scenario: Local execution matches CI semantics

- **WHEN** a developer or agent invokes the UI automation project locally
- **THEN** the documented command path SHALL use the same repository scripts and test assembly layout as CI
- **AND** the test suite SHALL run non-parallel so desktop state is not shared across concurrent fixtures

#### Scenario: Warm Sandbox is the default local desktop provider

- **WHEN** local UI automation is invoked without an execution-mode override
- **THEN** `testui.ps1` SHALL use a warm Windows Sandbox as the default desktop provider
- **AND** the sandbox SHALL be pre-provisioned with test prerequisites and stay alive between test invocations within a session
- **AND** the host's `Output/Debug/` folder SHALL be mapped read-only into the sandbox
- **AND** results SHALL be exchanged through a mapped writable folder with file-based signaling
- **AND** only the main repo checkout SHALL be supported for sandbox mapping (not worktrees)
- **AND** CI execution SHALL remain capable of running the same suite without Sandbox when the Windows runner already supplies the required desktop session

#### Scenario: Same-session concurrency is not treated as supported isolation

- **WHEN** UI automation runs are launched from multiple worktrees on the same interactive desktop session
- **THEN** the documented strategy SHALL treat that configuration as unsupported for reliable parallel execution
- **AND** supported parallelism SHALL require separate desktop/session isolation boundaries such as distinct VMs or equivalent isolated desktops