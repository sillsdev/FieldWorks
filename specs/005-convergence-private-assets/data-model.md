# Data Model – PrivateAssets Standardization

## Entities

### TestProject
- **Fields**:
  - `Name` (string): `<MSBuildProjectName>` extracted from the `.csproj` file.
  - `CsprojPath` (absolute path): used by the Convergence scripts.
  - `IsEligible` (bool): true when the project references a `SIL.LCModel.*.Tests` package.
  - `TouchedOn` (datetime): timestamp when conversion updated the file.
- **Relationships**: One `TestProject` has many `PackageReference` records.

### PackageReference
- **Fields**:
  - `Include` (string): NuGet package ID.
  - `Version` (string): SemVer range already used in the project.
  - `PrivateAssets` (enum): `None`, `All`, or `Inherited` (missing attribute).
  - `ParentProject` (foreign key): back-reference to `TestProject`.
- **Relationships**: Many `PackageReference` rows belong to one `TestProject`.

### AuditFinding
- **Fields**:
  - `ProjectName`
  - `PackagesMissingPrivateAssets` (CSV string)
  - `Action` (enum): `AddPrivateAssets` or `Ignore`.
  - `Severity` (enum): `Critical` when leakage risk is observed, `Info` otherwise.
- **Relationships**: Links to a single `TestProject`.

### ConversionDecision
- **Fields**:
  - `ProjectName`
  - `PackageInclude`
  - `Decision` (bool): whether to apply the fix when running `convert`.
  - `Reason` (string): e.g., `LCM test utility`.
- **Relationships**: Derived from an `AuditFinding` for a `PackageReference`.

### ValidationResult
- **Fields**:
  - `BuildStatus` (enum): `Succeeded`, `Failed`, `WarningsAsErrors`.
  - `NuGetWarnings` (list): specifically NU1102 entries.
  - `Timestamp`
  - `Artifacts` (list of paths): log files or CSV exports.
- **Relationships**: References the run that produced the result, indirectly tied to the set of `TestProject` instances involved.

## State Transitions

1. **Audit**: `TestProject` + `PackageReference` → zero or more `AuditFinding` rows when eligible packages lack `PrivateAssets`.
2. **Decision**: `AuditFinding` rows become `ConversionDecision` entries (default allow) that drive the converter.
3. **Conversion**: Selected `ConversionDecision` entries mutate `PackageReference.PrivateAssets` from `None` to `All` and stamp `TestProject.TouchedOn`.
4. **Validation**: Produces a `ValidationResult`. Success requires zero NU1102 warnings and a non-failing MSBuild traversal. Failure loops back to the Audit stage after investigating diffs.
