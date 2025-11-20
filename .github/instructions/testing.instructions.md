---
applyTo: "**/*.{cs,cpp,h}"
name: "testing.instructions"
description: "FieldWorks testing guidelines (unit/integration)"
---
# Testing guidelines

## Purpose & Scope
Guidance for writing deterministic unit and integration tests for FieldWorks and CI expectations.

## Context loading
- Locate tests near their components (e.g., `Src/<Component>.Tests`). Some integration scenarios use `TestLangProj/` data.
- **Current test infrastructure**: MSBuild-based with NUnit Console Runner via `Build/FieldWorks.targets`
- **Future direction**: Migrating to `dotnet test` (VSTest platform) for modern CI/CD integration
- Test projects are marked with `<IsTestProject>true</IsTestProject>` and automatically receive modern test packages via `Directory.Build.props`

## Deterministic requirements
- Keep tests hermetic: avoid external state; use test data under version control.
- Name tests for intent; include happy path and 1–2 edge cases.
- Timeouts: Use sensible limits; see `Build/TestTimeoutValues.xml` for reference values.

## Structured output
- Provide clear Arrange/Act/Assert; minimal fixture setup.
- Prefer stable IDs and data to avoid flakiness.

## Key Rules
- Keep tests deterministic and fast where possible; add integration tests only for end-to-end scenarios.
- Name tests clearly for reviewer understanding.
- Tests use NUnit 3.x framework with `[Test]` and `[TestFixture]` attributes.

## Running Tests

### Current Method: MSBuild with `action=test` (Recommended for Now)

The established approach uses custom MSBuild tasks that invoke NUnit Console Runner:

```powershell
# Run all tests during build
.\build.ps1 -Configuration Debug
# Or explicitly:
msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /p:action=test

# Run tests for a specific component
msbuild Build\FieldWorks.targets /t:CacheLightTests /p:Configuration=Debug /p:Platform=x64 /p:action=test
```

**How it works**:
- Tests are executed via NUnit Console Runner: `packages/NUnit.ConsoleRunner.3.12.0/tools/nunit3-console.exe`
- Test results: `Output/Debug/<ProjectName>.dll-nunit-output.xml`
- Each test target in `Build/FieldWorks.targets` has an `NUnit3` task that runs when `action=test`

### Future Method: `dotnet test` (Under Development)

A modernized `dotnet test` workflow is planned:

```powershell
# Target workflow (not fully functional yet):
dotnet test FieldWorks.sln --configuration Debug

# Or specific projects:
dotnet test Src/InstallValidator/InstallValidatorTests/InstallValidatorTests.csproj
```

**Current Status**:
- Test projects now reference `Microsoft.NET.Test.Sdk` and `NUnit3TestAdapter` via `Directory.Build.props`
- Adapter discovery with .NET Framework 4.8 projects needs additional work
- This approach will replace MSBuild-based test execution once fully validated

### In Docker Container

```powershell
# Current working approach (MSBuild)
docker run --rm -v "${PWD}:C:\src" -w C:\src fw-build:ltsc2022 `
  powershell -NoLogo -Command "msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /p:action=test"

# Future approach (dotnet test - when adapter discovery is fixed)
docker run --rm -v "${PWD}:C:\src" -w C:\src fw-build:ltsc2022 `
  powershell -NoLogo -Command "C:\dotnet\dotnet.exe test FieldWorks.sln --configuration Debug"
```

## References
- **Build Infrastructure**: `Build/FieldWorks.targets` for MSBuild test tasks
- **Test Data**: `TestLangProj/` for integration test data
- **Build Instructions**: `.github/instructions/build.instructions.md` for build and test execution
