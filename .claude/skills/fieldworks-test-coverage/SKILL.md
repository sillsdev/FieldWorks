---
name: fieldworks-test-coverage
description: Check that new or changed FieldWorks code is actually exercised by tests, using test.ps1 -Coverage. Use whenever writing tests for a change, before claiming a bug fix or feature is covered, or when reviewing whether "tests pass" also means the new logic runs under test.
model: haiku
---

<role>
You confirm that new/changed behavior is exercised by tests, not merely that some test file exists nearby. "Tests pass" is not the same claim as "the new code ran during the test" — this skill closes that gap.
</role>

<inputs>
You will receive:
- The diff or file list for the change
- The test project(s) that build against the changed files
</inputs>

<workflow>
1. **Identify the decision surface**
   - For each changed method, find the new/changed branches, guards, and edge cases — not just "a test file touches this class."
   - Pure decisions (parsing, gating, mapping) are cheap to cover directly. Side-effecting code wrapped around registry/filesystem/global-singleton calls is often already excluded by design in this repo (e.g. `AvaloniaOptionsDialogLauncher.Apply`) — check whether the surrounding test file already documents that scope boundary before assuming it's a gap.
2. **Run coverage for the touched project(s)**
   - `.\test.ps1 -TestProject <project path or name> -Coverage [-NoBuild] [-TestFilter "..."]`
   - This wraps vstest.console.exe with the dotnet-coverage tool (dynamic, in-memory instrumentation; free on any VS edition — the older `/EnableCodeCoverage` flag in `Build/SetupInclude.targets` requires VS Enterprise and will silently fail to collect on Community/Build Tools installs). Nothing on disk is rewritten, so parallel testhosts are safe despite the shared Output/<Configuration> folder.
   - Reads the merged report from `Output/<Configuration>/TestResults/CoverageReport/Summary.txt` (and `index.html` for line-by-line detail) that the script prints automatically.
3. **Check the new lines specifically**
   - Open `index.html` for the changed file(s) and confirm the new/changed lines show as covered (green), not just that the file's overall percentage moved.
   - A changed file can show high coverage while the new branch itself is dead — always check the specific lines, not the file average.
4. **Close real gaps, document accepted ones**
   - If a new decision is uncovered and cheaply testable, write the test (see repo test file conventions — extract pure decisions into small static/testable methods when the surrounding code is otherwise hard to unit test, as with `AvaloniaDialogHost.ResolveEffectiveOwner`).
   - If a gap is accepted (e.g. it requires a live/manual/desktop-only round-trip), say so explicitly in the PR description or handoff — do not let it pass silently as "tests pass."
</workflow>

<constraints>
- Never report coverage as a percentage alone; tie the claim to the specific new/changed lines.
- Do not chase repo-wide coverage — scope to the current change's touched files.
- Prefer extracting a testable pure decision over mocking heavy framework/registry/filesystem dependencies to force coverage of an untestable method.
</constraints>

<notes>
- See `verify-test` for the broader build/test verification flow this slots into.
- See `execute-implement`'s self-check step, which references this skill for new logic.
- `test.ps1 -Coverage` restores the local dotnet tool manifest automatically (`dotnet-coverage` collects the raw `coverage.cobertura.xml`; ReportGenerator renders the summary/HTML). On PRs, Codecov reports the same data as a patch-coverage check.
</notes>
