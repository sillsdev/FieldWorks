# Data Model: GenerateAssemblyInfo Template Reintegration

## ManagedProject
| Field                       | Type                                                         | Description                                                                               |
| --------------------------- | ------------------------------------------------------------ | ----------------------------------------------------------------------------------------- |
| `id`                        | string                                                       | Unique path relative to repo root (e.g., `Src/Common/FieldWorks/FieldWorks.csproj`).      |
| `category`                  | enum {T, C, G}                                               | Inventory classification: Template-only, Template+Custom, Needs GenerateAssemblyInfo fix. |
| `templateImported`          | bool                                                         | Indicates whether the project already links `Src/CommonAssemblyInfo.cs`.                  |
| `hasCustomAssemblyInfo`     | bool                                                         | True when any `AssemblyInfo*.cs` exists on disk or must be restored.                      |
| `generateAssemblyInfoValue` | enum {true,false,missing}                                    | Current property value in the `.csproj`.                                                  |
| `remediationState`          | enum {AuditPending, NeedsRemediation, Remediated, Validated} | Workflow state (see transitions below).                                                   |
| `notes`                     | string                                                       | Free-form explanation for exceptions or reviewer guidance.                                |

**Relationships**:
- `ManagedProject` **has one** `AssemblyInfoFile` when `hasCustomAssemblyInfo=true`.
- `ManagedProject` **produces many** `ValidationFinding` records across audit/validate scripts.

**State transitions**:
- `AuditPending → NeedsRemediation`: audit script detects mismatch.
- `NeedsRemediation → Remediated`: conversion script inserts template link, flips `GenerateAssemblyInfo`, and restores missing files.
- `Remediated → Validated`: validation script passes and CI builds show zero CS0579 warnings.
- Any failure returns the project to `NeedsRemediation` for manual follow-up.

## AssemblyInfoFile
| Field               | Type     | Description                                                                   |
| ------------------- | -------- | ----------------------------------------------------------------------------- |
| `projectId`         | string   | Foreign key to `ManagedProject`.                                              |
| `path`              | string   | Location of the custom file (e.g., `Src/App/Properties/AssemblyInfo.App.cs`). |
| `restorationSha`    | string   | Git commit hash used for restoration (`git show <sha>`).                      |
| `customAttributes`  | string[] | Attributes beyond the template (e.g., `AssemblyTrademark`, `CLSCompliant`).   |
| `conditionalBlocks` | bool     | Indicates presence of `#if/#endif` requiring preservation.                    |

**Validation rules**:
- `restorationSha` required when `path` was missing at HEAD.
- `customAttributes` must include at least one entry; otherwise the file reverts to template-only and should be removed.

## TemplateLink
| Field         | Type   | Description                                                             |
| ------------- | ------ | ----------------------------------------------------------------------- |
| `projectId`   | string | Foreign key to `ManagedProject`.                                        |
| `linkInclude` | string | Relative path used in `<Compile Include="..." />`.                      |
| `linkAlias`   | string | Value of `<Link>` element (e.g., `Properties\CommonAssemblyInfo.cs`).   |
| `commentId`   | string | Anchor for the XML comment explaining why `GenerateAssemblyInfo=false`. |

**Constraints**:
- `linkInclude` must resolve to `Src/CommonAssemblyInfo.cs` from the project directory.
- Exactly one `TemplateLink` per project; duplicates cause CS0579.

## ValidationFinding
| Field         | Type                                                                                                   | Description                                   |
| ------------- | ------------------------------------------------------------------------------------------------------ | --------------------------------------------- |
| `id`          | string                                                                                                 | `projectId` + `findingCode` combination.      |
| `projectId`   | string                                                                                                 | Foreign key to `ManagedProject`.              |
| `findingCode` | enum {MissingTemplateImport, GenerateAssemblyInfoTrue, MissingAssemblyInfoFile, DuplicateCompileEntry} |
| `severity`    | enum {Error, Warning, Info}                                                                            | How urgently the issue blocks merge.          |
| `details`     | string                                                                                                 | Human-readable description used in CI output. |

**State transitions**:
- Created during audit.
- Cleared once remediation script or manual fix resolves the condition.
- Persisted summaries posted to CI artifacts for reviewer visibility.

## RemediationScriptRun
| Field             | Type                            | Description                                           |
| ----------------- | ------------------------------- | ----------------------------------------------------- |
| `script`          | enum {audit, convert, validate} | Which automation ran.                                 |
| `timestamp`       | datetime                        | Execution time (UTC) to order evidence.               |
| `inputArtifacts`  | string[]                        | Paths to CSV/JSON inputs consumed.                    |
| `outputArtifacts` | string[]                        | Paths to CSV/JSON reports produced.                   |
| `exitCode`        | int                             | Non-zero indicates failure requiring human attention. |

**Workflow**:
1. `audit` produces an inventory CSV (`generate_assembly_info_audit.csv`).
2. `convert` consumes the CSV plus `restore.json` to modify projects.
3. `validate` consumes the repo state and emits `validation_report.txt`; success is required before merging.
