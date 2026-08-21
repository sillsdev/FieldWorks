# Per-build Local Library Selection Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make local SIL dependency selection explicit for one FieldWorks build and automatically remove unselected local packages.

**Architecture:** A shared PowerShell module owns library metadata and provenance-aware cleanup. `build.ps1` orchestrates cleanup, packing, version overrides, and restore sources without modifying tracked version properties.

**Tech Stack:** Windows PowerShell 5.1-compatible PowerShell, MSBuild global properties, NuGet PackageReference metadata, fixture-based script tests.

---

### Task 1: Prove provenance-aware cleanup

**Files:**
- Create: `Build/LocalLibraries.Tests.ps1`
- Create: `Build/LocalLibraries.psm1`

- [ ] **Step 1: Write the failing fixture test**

Create local and HTTP-sourced `.nupkg.metadata` files for Machine, create
matching feed packages, invoke `Clear-FieldWorksLocalLibraries`, and assert
that the local cache and managed feed files disappear while the HTTP cache and
an unrelated package remain.

- [ ] **Step 2: Verify the test fails**

Run: `pwsh -File Build/LocalLibraries.Tests.ps1`

Expected: failure because `Build/LocalLibraries.psm1` or its exported cleanup
function does not exist.

- [ ] **Step 3: Implement the shared catalogue and cleanup**

Export `Get-FieldWorksLocalLibraryConfig` and
`Clear-FieldWorksLocalLibraries`. The catalogue contains the five existing
library keys, version properties, environment variables, package prefixes,
PDB directories, pack order, and Machine's project list. Cleanup treats only
filesystem metadata sources as local and scopes feed deletion to configured
package prefixes.

- [ ] **Step 4: Verify the test passes**

Run: `pwsh -File Build/LocalLibraries.Tests.ps1`

Expected: all fixture assertions pass with exit code 0.

### Task 2: Return packed versions without persistent version edits

**Files:**
- Modify: `Build/Manage-LocalLibraries.ps1`
- Modify: `Build/LocalLibraries.Tests.ps1`

- [ ] **Step 1: Add a failing script-contract test**

Assert that pack orchestration accepts `-VersionOutputPath`, sources its
catalogue from `LocalLibraries.psm1`, and routes cache invalidation through the
shared module.

- [ ] **Step 2: Verify the contract test fails**

Run: `pwsh -File Build/LocalLibraries.Tests.ps1`

Expected: failure naming the missing version-output contract.

- [ ] **Step 3: Implement version output mode**

Import the shared module, add `-VersionOutputPath`, collect each packed
library's `VersionProperty` and detected version, serialize that map as JSON,
and skip `SilVersions.props` edits in this mode. Preserve SetVersion mode and
direct pack behavior for existing callers.

- [ ] **Step 4: Verify the tests pass**

Run: `pwsh -File Build/LocalLibraries.Tests.ps1`

Expected: all tests pass.

### Task 3: Make local selection a build parameter

**Files:**
- Modify: `build.ps1`
- Modify: `nuget.config`
- Modify: `Build/LocalLibraries.Tests.ps1`

- [ ] **Step 1: Add failing build-contract tests**

Assert that `build.ps1` exposes a validated `LocalLibraries` array, cleans
local state before restore, invokes the manager for selected libraries, and
adds detected versions to both restore and MSBuild arguments. Assert that
`nuget.config` clears inherited package sources.

- [ ] **Step 2: Verify the contract tests fail**

Run: `pwsh -File Build/LocalLibraries.Tests.ps1`

Expected: failures for the missing parameter and source isolation.

- [ ] **Step 3: Implement orchestration**

Add `-LocalLibraries` with the five catalogue keys. Before restore, clean all
managed local artifacts. When selections exist, invoke the manager with the
corresponding switches and a temporary JSON output path, append
`/p:<VersionProperty>=<Version>` to restore and build arguments, and add the
local feed as an invocation-scoped restore source. Add `<clear />` before the
repository NuGet source.

- [ ] **Step 4: Verify tests and PowerShell compatibility**

Run: `pwsh -File Build/LocalLibraries.Tests.ps1`

Run: `Build/Agent/powershell-compat.ps1`

Expected: both commands exit 0.

### Task 4: Replace the persistent workflow documentation

**Files:**
- Modify: `Docs/architecture/local-library-debugging.md`
- Modify: `Docs/architecture/dependencies.md`
- Modify: `Build/Manage-LocalLibraries.ps1`

- [ ] **Step 1: Document the invocation contract**

Make `build.ps1 -LocalLibraries <names>` the public workflow, state that each
selected library is repacked on every invocation, explain automatic cleanup,
and include all five library names and environment variables.

- [ ] **Step 2: Run comment hygiene and whitespace checks**

Run: `Build/Agent/comment-hygiene.ps1 -FailOnViolation`

Run: `git diff --check`

Expected: both commands exit 0.

### Task 5: Validate and publish

**Files:**
- Modify or delete working design/plan Markdown according to PR triage.

- [ ] **Step 1: Run repository validation**

Run: `.\build.ps1 -CommentHygiene`

Run: `.\test.ps1 -CommentHygiene`

Expected: both commands exit 0 with no comment-hygiene violations.

- [ ] **Step 2: Review the branch against `origin/main`**

Inspect all four FieldWorks review passes, resolve findings, and record any
validation gaps in `.review/summary.md`.

- [ ] **Step 3: Commit with Jira linkage**

Use a subject no longer than 72 characters, wrap every body line below 80
characters, include `Refs LT-22728`, and run:

`gitlint --ignore body-is-missing --commits origin/main..HEAD`

- [ ] **Step 4: Push and open the PR**

Push `LT-22728-local-library-selection` and create a PR titled
`LT-22728: Make local library selection build-scoped`. The body leads with
the reviewer decision, identifies `build.ps1` as the entry point, reports
fresh validation, and ends with one `Next:` line.
