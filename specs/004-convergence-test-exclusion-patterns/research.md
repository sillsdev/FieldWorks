# Research – Convergence 004: Test Exclusion Pattern Standardization

All clarifications identified in the spec have been resolved. The items below capture the final decisions, rationale, and alternatives considered.

## Pattern Selection
- **Decision**: Standardize every SDK-style project on explicit `<ProjectName>Tests/**` exclusions (Pattern A) with additive entries for nested component folders.
- **Rationale**: Pattern A is already used by 56% of projects, is self-documenting, and aligns with FieldWorks' "explicit over implicit" guidance. It avoids the accidental exclusions caused by `*Tests/**` while still being easy to audit.
- **Alternatives considered**: Pattern B (`*Tests/**`) offered brevity but introduced hidden exclusions and missed folders not ending in `Tests`. Pattern C (fully explicit paths) is already subsumed by the Pattern A approach plus nested entries but would remain too verbose without added benefit.

## Automation Workflow
- **Decision**: Build three Python 3.11 utilities—`audit_test_exclusions.py`, `convert_test_exclusions.py`, and `validate_test_exclusions.py`—to scan, normalize, and continuously verify `.csproj` exclusions.
- **Rationale**: Scripts enable deterministic, repeatable conversions across ~80 projects and can surface policy violations (missing exclusions, mixed content) before PRs are pushed. They also plug directly into Build/Agent tooling for CI/pre-commit use.
- **Alternatives considered**: Manual editing or ad-hoc PowerShell loops would be error-prone and slow, while modifying MSBuild imports globally would violate the clarified per-project policy and fail to cover nested folder edge cases.

## Mixed Test Code Policy
- **Decision**: Treat any project containing both production and test code as a policy violation—stop automation for that project and escalate to the owning team for structural cleanup.
- **Rationale**: This upholds the clarified requirement that test utilities live in dedicated projects, prevents scripts from hiding architectural issues with broader exclusions, and keeps accountability with component owners.
- **Alternatives considered**: Broad wildcard exclusions (Option A in `CLARIFICATIONS-NEEDED.md`) or per-file carve-outs (Option B) risk masking bad layouts; large refactors (Option C) are out of scope for this convergence but will be flagged separately.

## Validation Coverage
- **Decision**: Enforce the new pattern through layered validation—local MSBuild traversal runs after each conversion batch, automated script validation, a pre-commit hook, and a CI job that fails on pattern drift or missing exclusions.
- **Rationale**: Layered checks guarantee CS0436 regressions are caught early, even if a developer bypasses a single safeguard. Validation also confirms that nested folders and newly created tests stay excluded.
- **Alternatives considered**: Relying solely on MSBuild errors would force developers to hit CS0436 failures reactively, while CI-only enforcement would slow feedback loops and allow accidental pushes to sit in review queues longer.
