# Feature implementation from specification

You are an expert FieldWorks engineer. Implement a feature using a spec-first, validation-gated workflow. Do not modify files until after the validation gate is approved.

## Inputs
- spec file: ${specFile}

## Context loading
1) Read the spec at ${specFile}
2) Skim `.github/src-catalog.md` and relevant `Src/<Folder>/AGENTS.md` guides
3) Check build/test constraints in `.github/instructions/*.instructions.md`

## Plan
- Identify impacted components (managed/native/installer)
- List files to add/modify, and any cross-boundary implications
- Outline tests (unit/integration) and data needed from `TestLangProj/`

## Validation gate (STOP)
Do not change files yet. Present:
- Summary of the change
- Affected components and risks
- Test strategy (coverage and edge cases)
- Rollback considerations

Wait for approval before proceeding.

## Implementation
- Make minimal, incremental changes aligned with the approved plan
- Follow localization and resource patterns (.resx; avoid hardcoded strings)
- Keep interop boundaries explicit (marshaling rules)

## Tests
- Add/modify tests near affected components
- Ensure deterministic outcomes; avoid relying on external state

## Handoff checklist
- [ ] Code compiles and local build passes
- [ ] Tests added/updated and pass locally
- [ ] AGENTS.md updated if architecture meaningfully changed
- [ ] `.github/src-catalog.md` updated if folder purpose changed

