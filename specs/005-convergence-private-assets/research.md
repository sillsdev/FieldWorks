# Research – PrivateAssets Convergence

### Decision 1: Scope of PrivateAssets enforcement
- **Decision**: Restrict `PrivateAssets="All"` enforcement to the three LCM mixed test-utility packages published as `SIL.LCModel.Core.Tests`, `SIL.LCModel.Tests`, and `SIL.LCModel.Utils.Tests`.
- **Rationale**: Those NuGet packages bundle reusable helpers plus their own tests; marking them private prevents the helper consumers from inheriting NUnit/Moq dependencies. Other packages (e.g., NUnit, Moq, Microsoft.NET.Test.Sdk) are already test-only and do not ship reusable utilities, so changing them would add churn without solving a leak.
- **Alternatives considered**:
  - Apply PrivateAssets to every PackageReference inside `*Tests.csproj` (overly aggressive, risks hiding legitimate shared dependencies).
  - Maintain a heuristic list (`*test*`, `*mock*`, etc.) and refresh it periodically (high maintenance, still at risk of false positives/negatives).

### Decision 2: Automation strategy
- **Decision**: Use the existing `convergence.py private-assets audit|convert|validate` workflow with custom auditor/converter classes rather than editing `.csproj` files manually.
- **Rationale**: The framework already parses MSBuild XML safely, keeps indentation, and produces CSV decision logs that can be reviewed or re-run deterministically.
- **Alternatives considered**:
  - Manual editing inside Visual Studio (error-prone across 46 projects and easy to miss new references).
  - Bespoke PowerShell or Roslyn scripts (would duplicate functionality that Convergence already offers).

### Decision 3: Validation gates
- **Decision**: Treat `python convergence.py private-assets validate` plus a full `msbuild FieldWorks.sln /m /p:Configuration=Debug` run as the authoritative validation pipeline; additionally scan build logs for NU1102 warnings.
- **Rationale**: The validate command ensures all targeted PackageReferences gained `PrivateAssets="All"`, while the MSBuild run confirms no regressions in restore/build due to newly private packages. NU1102 scans guarantee no missing packages after the change.
- **Alternatives considered**:
  - Rely solely on unit tests (would miss restore-time dependency leaks).
  - Skip the MSBuild traversal and only spot-check a few projects (would not match CI coverage and could miss platform-specific fallout).
