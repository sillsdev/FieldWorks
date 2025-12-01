# Data Model – Convergence 004

This feature manipulates metadata about SDK-style projects, their test folders, and exclusion policies. The following conceptual entities align the automation scripts, OpenAPI contracts, and eventual tasks.

## Entities

### Project
- **Fields**: `name`, `relativePath`, `patternType` (Enum: A|B|C|None), `hasMixedCode` (bool), `status` (Enum: Pending|Converted|Flagged), `lastValidated` (DateTime)
- **Relationships**: One-to-many with `TestFolder`; one-to-many with `ExclusionRule`; one-to-many with `ValidationIssue` (historic log)
- **Validation Rules**:
  - `patternType` MUST be `A` before marking `status=Converted`.
  - `hasMixedCode=true` forces `status=Flagged` until structural cleanup occurs.
  - `lastValidated` must be updated whenever validation passes on the current commit.

### TestFolder
- **Fields**: `projectName`, `relativePath`, `depth`, `containsSource` (bool), `excluded` (bool)
- **Relationships**: Belongs to exactly one `Project`; linked to zero or one `ExclusionRule` covering it.
- **Validation Rules**:
  - `excluded=true` for every folder ending in `Tests`.
  - `containsSource=false` is enforced for production projects; if true, automation raises a mixed-code flag.

### ExclusionRule
- **Fields**: `projectName`, `pattern` (string), `scope` (Enum: Compile|None|Both), `source` (Enum: Explicit|Generated), `coversNested` (bool)
- **Relationships**: Targets one or more `TestFolder` paths.
- **Validation Rules**:
  - `pattern` must be explicit (no leading wildcard) unless `source=Generated` and approved.
  - `scope` defaults to `Both` (Compile and None) for SDK-style projects.

### ValidationIssue
- **Fields**: `id`, `projectName`, `issueType` (MissingExclusion|MixedCode|WildcardDetected|ScriptError), `severity` (Warning|Error), `details`, `detectedOn` (DateTime), `resolved` (bool)
- **Relationships**: Linked to the `Project` and optionally to specific `TestFolder` entries.
- **Validation Rules**:
  - New issues default to `resolved=false`; conversions must close issues before marking a batch complete.
  - Severity escalates to `Error` when test code could ship (MissingExclusion/MixedCode).

### ConversionJob
- **Fields**: `jobId`, `initiatedBy`, `projectList` (array of `Project` names), `scriptVersion`, `startTime`, `endTime`, `result` (Success|Partial|Failed)
- **Relationships**: References multiple `Project` records and aggregates their `ValidationIssue` outputs.
- **Validation Rules**:
  - `result=Success` only when all projects reach `status=Converted` and validation passes.
  - Persist `scriptVersion` for auditability; mismatches trigger re-validation.

## State Transitions

1. **Project Workflow**: `Pending` → `Converted` (after exclusions updated and validation clean). If automation detects mixed code, transition to `Flagged` until manual cleanup occurs. Once resolved, the project re-enters `Pending` for another conversion attempt.
2. **ValidationIssue Lifecycle**: `resolved=false` upon detection, transitions to `resolved=true` only after scripts confirm remediation. Closed issues remain attached for historical auditing.
3. **ConversionJob Lifecycle**: Starts in-progress upon script kickoff, moves to `Success` or `Partial/Failed` based on downstream validation. Partial jobs require follow-up ConversionJobs referencing remaining `Pending` projects.

## Derived Views

- **Compliance Dashboard**: Aggregates `Project.status`, highlighting `Flagged` entries and the count of remaining `Pending` conversions.
- **Mixed Code Watchlist**: Filters `TestFolder` where `containsSource=true` to feed manual investigation tasks.
- **CI Validation Report**: Summarizes latest `ConversionJob` and `ValidationIssue` details; exported via `contracts/test-exclusion-api.yaml` endpoints.
