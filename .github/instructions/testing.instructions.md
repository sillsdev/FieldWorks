---
applyTo: "**/*.{cs,cpp,h}"
description: "FieldWorks testing guidelines (unit/integration)"
---
# Testing guidelines

## Context loading
- Locate tests near their components (e.g., `Src/<Component>.Tests`). Some integration scenarios use `TestLangProj/` data.
- Determine test runner: SDK-style projects use `dotnet test`; .NET Framework often uses NUnit Console.

## Deterministic requirements
- Keep tests hermetic: avoid external state; use test data under version control.
- Name tests for intent; include happy path and 1–2 edge cases.
- Timeouts: Use sensible limits; see `Build/TestTimeoutValues.xml` for reference values.

## Structured output
- Provide clear Arrange/Act/Assert; minimal fixture setup.
- Prefer stable IDs and data to avoid flakiness.

## References
- Test data: `TestLangProj/`
