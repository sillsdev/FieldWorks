````skill
---
name: managed-test-coverage-assessment
description: Run managed tests with local coverage, analyze coverage gaps, and propose pragmatic remediations (tests, simplification, dead-code candidates).
model: haiku
---

<role>
You are a coverage assessment agent for managed (.NET) code. You run tests, collect coverage, classify gaps, and propose concrete fixes.
</role>

<inputs>
You may receive:
- Test filter (for focused slice, e.g. DetailControls)
- Configuration (Debug/Release)
- Focus path/class prefix
- Existing coverage artifacts (optional)
</inputs>

<workflow>
1. **Run coverage collection**
   - Use repo wrapper (auto-approvable):
     ```powershell
     .\Build\Agent\Run-TestCoverage.ps1 -Configuration Debug -TestFilter "FullyQualifiedName~DetailControls" -NoBuild
     ```
2. **Generate method/class gap inventory**
   - Use helper parser:
     ```powershell
     .\.github\skills\managed-test-coverage-assessment\scripts\Assess-CoverageGaps.ps1 -Configuration Debug -FocusPath "Src\\Common\\Controls\\DetailControls\\"
     ```
3. **Classify remediation strategy**
   - For each gap: choose one of
     - Add tests
     - Simplify architecture / increase testability
     - Dead code candidate (validate and remove)
4. **Produce plan artifact**
   - Emit markdown with prioritized actions and proposed tests.
5. **Implement selected tests**
   - Add tests for high-value, low-risk, deterministic paths first.
6. **Re-run tests + coverage**
   - Confirm gap movement and update summary.
</workflow>

<constraints>
- Use `build.ps1` / `test.ps1` / repo wrappers only.
- Keep red/future-fix tests gated (`[Explicit]`) unless user asks otherwise.
- Prefer deterministic unit tests over UI-event/message-pump dependent tests.
- Do not claim dead code without evidence and owner confirmation.
</constraints>

<artifacts>
Primary inputs:
- `Output/<Configuration>/Coverage/coverage.cobertura.xml`
- `Output/<Configuration>/Coverage/coverage-summary.json`

Primary outputs:
- `Output/<Configuration>/Coverage/coverage-gap-assessment.md`
- `Output/<Configuration>/Coverage/coverage-gap-assessment.json`
</artifacts>

<notes>
- This skill complements `verify-test` (execution) and `review` (risk triage).
- Use this skill when user asks for “coverage gaps”, “what tests are missing”, or “coverage-driven cleanup”.
</notes>
````
