# Data Model — PlatformTarget Redundancy Cleanup

## Entities

### 1. `SdkProject`
- **Fields**
  - `Name` (string) — Friendly identifier derived from `.csproj` filename.
  - `RelativePath` (string) — Path from repo root (e.g., `Src/Common/FwUtils/FwUtils.csproj`).
  - `OutputType` (enum: `Library`, `Exe`, `WinExe`, `Unknown`).
  - `PlatformTarget` (enum: `x64`, `AnyCPU`, `x86`, `Unset`).
  - `PlatformsPropertyPresent` (bool) — Indicates `<Platforms>` is explicitly defined locally.
  - `HasConditionalPropertyGroups` (bool) — True when multiple conditional `<PropertyGroup>` nodes may influence inheritance.
- **Relationships**
  - Owns zero or one `PlatformTargetSetting` entries that originate directly in the project file.
  - Linked to zero or one `ExceptionRule` if the project must keep an explicit setting.
- **Lifecycle / State**
  - `Audited` → `PendingConversion` → (`Converted` | `ExceptionRecorded`).
  - Transition to `Converted` requires removal of redundant `<PlatformTarget>x64</PlatformTarget>` nodes and git diff verification.

### 2. `PlatformTargetSetting`
- **Fields**
  - `Value` (enum as above).
  - `Condition` (string) — The MSBuild condition guarding the property group (empty for unconditional groups).
  - `SourceFile` (string) — Typically `.csproj` path; noted to assist validation and code review.
  - `LineRange` (tuple<int,int>) — Line numbers for precise edits.
- **Validation Rules**
  - Must exist only when the project diverges from repo defaults or documents an exception.
  - When `Value == x64` and no `Condition`, the property is redundant and flagged for removal.

### 3. `ExceptionRule`
- **Fields**
  - `ProjectName` (string) — Reference back to `SdkProject`.
  - `Reason` (enum: `BuildTool`, `ThirdPartyRequirement`, `HybridTargeting`).
  - `DocumentationNote` (string) — Human-readable explanation inserted as an XML comment near the property.
- **Validation Rules**
  - Every exception must appear in the CSV `platform_target_decisions.csv` with `Action=Keep`.
  - Revalidation ensures the rationale is still accurate before each release cycle.

## Relationships Overview

```text
SdkProject 1 --- 0..1 PlatformTargetSetting
     |\
     | \__ 0..1 ExceptionRule
     |
     +--> participates in convergence.csv outputs (audit/convert/validate)
```

## Derived Data & Reports
- `platform_target_audit.csv` aggregates `SdkProject` rows with their discovered `PlatformTargetSetting`.
- `platform_target_decisions.csv` enriches each row with the intended action (`Remove` vs `Keep`) and optional `ExceptionRule` metadata.
- Validation reports confirm zero remaining redundant `<PlatformTarget>` entries by diffing the audit output against the decisions file.
