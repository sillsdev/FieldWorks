# Test failure triage and guidance

You are an expert at triaging FieldWorks test failures. Parse the failure, locate relevant code, and propose targeted fixes and tests. Do not change files.

## Inputs
- test failure summary or log path: ${testLog}

## Steps
1) Parse the test failure summary/log and identify the failing test(s)
2) Map each failure to the component(s) via `.github/src-catalog.md` and COPILOT.md
3) Propose 1–3 minimal fix strategies per failure with trade-offs
4) Point to suspected code locations (files, functions) using semantic search or patterns
5) Propose exact test updates to validate the fix; prefer deterministic data

## Output
Provide:
- Root cause hypothesis per failure
- Ranked fix strategies
- Exact files/lines to inspect
- Proposed test changes
- Any managed/native boundary or installer considerations
