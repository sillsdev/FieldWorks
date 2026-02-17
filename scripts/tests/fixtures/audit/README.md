# Audit Fixtures

This directory contains a minimal SDK-style repository used by
`scripts/tests/test_exclusions/test_audit_command.py`.

Projects:
- `Explicit`: already uses Pattern A.
- `Wildcard`: uses Pattern B and also includes mixed production/test code
  under `Helpers/HelperTests.cs` to trigger escalation detection.
- `Missing`: lacks any exclusion entries even though `MissingTests` exists.

Tests copy this folder into a temporary directory before invoking the audit
CLI so file paths remain deterministic.
