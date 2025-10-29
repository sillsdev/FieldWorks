# Bugfix workflow (triage → RCA → fix)

You are an expert FieldWorks engineer. Triage and fix a defect with a validation gate before code changes.

## Inputs
- failure description or issue link: ${issue}
- logs or stack trace (optional): ${logs}

## Triage
1) Summarize the failure and affected components
2) Reproduce locally if possible; capture steps or failing test
3) Identify recent changes that could be related

## Root cause analysis (RCA)
- Hypothesize likely causes (3 candidates) and quick tests to confirm/deny
- Note any managed/native or installer boundary implications

## Validation gate (STOP)
Do not change files yet. Present:
- Root cause hypothesis and evidence
- Proposed fix (minimal diff) and test changes
- Risk assessment and fallback plan

Wait for approval before proceeding.

## Implementation
- Apply the minimal fix aligned with repository conventions
- Ensure localization, threading, and interop rules are respected

## Tests
- Add/adjust tests to reproduce the original failure and verify the fix
- Prefer deterministic tests; update `TestLangProj/` data only if necessary

## Handoff checklist
- [ ] Build and local tests pass
- [ ] Commit messages conform to gitlint rules
- [ ] COPILOT.md updated if behavior/contract changed
