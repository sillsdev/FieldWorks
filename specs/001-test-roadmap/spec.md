# Feature Specification: Short-term Test Modernization Roadmap

**Feature Branch**: `001-test-roadmap`
**Created**: November 18, 2025
**Status**: Draft
**Input**: User description: "implement short term roadmap from TEST_MIGRATION_PATHS.md"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - VSTest Pilot Toggle (Priority: P1)

Build engineers need to switch a representative managed test project (initially `FwControlsTests`) from the legacy `<NUnit3>` console task to a guarded `vstest.console` path so they can validate TRX output, timing, and parity without disrupting the rest of traversal.

**Why this priority**: Unlocks measured evidence that the modern runner can coexist with existing MSBuild orchestration, which is the prerequisite for broader migration.

**Independent Test**: Trigger `msbuild FieldWorks.proj /t:FwControlsTests /p:UseVSTestPilot=true /p:action=test` and verify the run completes, publishes TRX, and honors existing timeout/category knobs while other projects remain untouched.

**Acceptance Scenarios**:

1. **Given** traversal builds with `/p:UseVSTestPilot=true`, **When** `FwControlsTests` target runs, **Then** execution happens via `vstest.console` and produces a TRX artifact alongside legacy XML output.
2. **Given** the pilot flag is disabled, **When** the same target runs, **Then** the system falls back to the existing NUnit console path with no regressions.

---

### User Story 2 - Native UnitTests Target (Priority: P2)

Native component owners need an MSBuild target that compiles and runs the existing unit++-based binaries, failing the build on non-zero exit and collecting stdout/stderr so native regressions no longer slide past CI.

**Why this priority**: Provides immediate visibility into legacy native tests without waiting for a framework rewrite, satisfying the "Path 1" bootstrap in the roadmap.

**Independent Test**: Call `msbuild Build/Src/NativeBuild/NativeBuild.csproj /t:UnitTests` (or traversal wrapper) and confirm the unit++ executables build, execute, and emit log files that are uploaded even when failures occur.

**Acceptance Scenarios**:

1. **Given** native sources compile, **When** the `UnitTests` target runs, **Then** each registered unit++ executable is invoked and its pass/fail status rolls up into MSBuild success criteria.
2. **Given** a native test fails, **When** the target finishes, **Then** the build fails and logs capture the failure context for download in CI.

---

### User Story 3 - CI Observability (Priority: P3)

Release managers want consolidated evidence (TRX for managed pilot, log artifacts for native run) surfaced in CI dashboards so they can compare durations and failure signatures without spelunking raw build logs.

**Why this priority**: Ensures the pilot work delivers actionable telemetry for go/no-go decisions on broader modernization.

**Independent Test**: Run the CI lane (or an equivalent local script) that exercises both pilot capabilities and verify the resulting artifacts appear under predictable paths, linked from the summary UI.

**Acceptance Scenarios**:

1. **Given** a CI run with the pilot enabled, **When** the pipeline completes, **Then** TRX + legacy XML for `FwControlsTests` and log bundles for native unit++ executions are attached as named artifacts.
2. **Given** stakeholders inspect the collected metrics, **When** they compare run durations vs. baseline, **Then** the data is present without manual log scraping.

---

### Edge Cases

- Pilot flag misconfigured or vstest binaries missing on agent: the build must detect the missing dependency early, emit a helpful error, and fall back to NUnit when instructed.
- Native unit++ executables produce no output or hang: the orchestration must apply per-binary timeouts and kill/mark the run as failed while still uploading partial logs.
- TRX/log artifact publishing collides with existing artifact names: naming must be deterministic and scoped to avoid overwriting other reports.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Provide a guarded property (e.g., `UseVSTestPilot`) that, when true, routes `FwControlsTests` through `vstest.console` while preserving existing MSBuild target dependencies.
- **FR-002**: Ensure the pilot path emits both TRX and existing NUnit XML outputs, storing them under predictable `Output/<Configuration>/TestResults/` folders for CI pickup.
- **FR-003**: When the pilot property is false or vstest prerequisites are unavailable, default to the legacy `<NUnit3>` task without requiring engineers to edit project files.
- **FR-004**: Add an MSBuild target (`UnitTests`) that builds every registered unit++ executable, runs them with configurable timeouts, and fails the build on any non-zero exit code.
- **FR-005**: Capture stdout/stderr for each native executable, bundle them as artifacts, and include summary metadata (pass/fail counts, durations) in the MSBuild log and CI summary.
- **FR-006**: Document the pilot usage and fallback instructions in `TEST_MIGRATION_PATHS.md` (or linked doc) so developers know how to opt in/out locally and in CI.

### Key Entities *(include if feature involves data)*

- **VSTest Pilot Toggle**: Repository-wide property or environment switch controlling whether `FwControlsTests` (and future projects) invoke `vstest.console`. Attributes include default value (false), dependency on vstest binaries, and artifact naming conventions.
- **Native Unit Test Artifact**: Bundled log/output package per unit++ executable run, containing stdout/stderr, exit status metadata, and timestamps for observability pipelines.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Pilot runs produce TRX files and legacy XML every time `UseVSTestPilot=true`, with zero missing-artifact incidents across at least 5 CI runs.
- **SC-002**: Runtime difference between vstest pilot and legacy runner for `FwControlsTests` is measured and reported within 5% accuracy, enabling comparison in roadmap reviews.
- **SC-003**: Native `UnitTests` target executes all configured unit++ binaries and surfaces pass/fail counts in CI summaries with 100% failure propagation across trial runs.
- **SC-004**: Documentation updates enable at least 3 developers to run both pilots locally following the instructions without external support (validated via dry run or survey).

## Constitution Alignment Notes

- Data integrity: Native runner must leave existing binaries untouched; no schema/data migrations required.
- Internationalization: Managed pilot must ensure vstest output still records Graphite/complex-script cases exercised by `FwControlsTests` (already part of suite) with no loss of detail.
- Licensing: No new third-party dependencies introduced; vstest ships with VS Build Tools and unit++ already in-tree.