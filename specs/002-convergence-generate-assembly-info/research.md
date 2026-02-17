# Research Findings: GenerateAssemblyInfo Template Reintegration

## Decision 1: Link `Src/CommonAssemblyInfo.cs` into every project
- **Decision**: Source-controlled `Src/CommonAssemblyInfo.cs` remains the authoritative artifact and is linked into each `.csproj` via `<Compile Include="..\\..\\CommonAssemblyInfo.cs" Link="Properties\\CommonAssemblyInfo.cs" />`.
- **Rationale**: Linking preserves a single regeneration path (`SetupInclude.targets`) and keeps existing tooling (localization tasks, MsBuild customizations) untouched while satisfying the clarifications that mandate template consumption everywhere.
- **Alternatives considered**:
  - *Import a props file*: Would require a new `CommonAssemblyInfoTemplate.props` with duplicated metadata, risking drift from the template pipeline.
  - *Directory.Build.props injection*: Hides per-project intent and complicates exception documentation; rejected to keep explicit linkage visible in each `.csproj`.

## Decision 2: Force `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` with inline comments
- **Decision**: Every project that links the shared template or owns a custom `AssemblyInfo*.cs` sets `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` adjacent to a short XML comment referencing the template.
- **Rationale**: Prevents CS0579 duplicate attribute warnings, documents why SDK generation is disabled, and keeps behavior stable across Debug/Release builds.
- **Alternatives considered**:
  - *Leave property omitted (default true)*: Causes the SDK to emit duplicate attributes for template consumers.
  - *Conditional property per project type*: Adds unnecessary branching logic and increases maintenance overhead without additional benefit.

## Decision 3: Restore deleted `AssemblyInfo*.cs` directly from git history
- **Decision**: Projects that previously had bespoke attributes must recover their `AssemblyInfo*.cs` via `git show <sha>:<path>` instead of rewriting by hand.
- **Rationale**: Guarantees fidelity with pre-migration metadata (including conditional compilation blocks) and shortens review because diffs show precise restorations.
- **Alternatives considered**:
  - *Hand-author new files*: Error-prone; risk of omitting lesser-known attributes and legal notices.
  - *Rely solely on the common template*: Would drop legitimately custom metadata (e.g., CLSCompliant overrides) and violate the clarified requirement.

## Decision 4: Automate compliance with three Python scripts
- **Decision**: Implement `audit_generate_assembly_info.py`, `convert_generate_assembly_info.py`, and `validate_generate_assembly_info.py` under `scripts/GenerateAssemblyInfo/`.
- **Rationale**: Automation keeps the inventory of 115 projects manageable, enforces consistent remediation steps, and produces repeatable validation artifacts for CI and reviewers.
- **Alternatives considered**:
  - *Manual spot fixes*: Too slow and risks missing projects.
  - *PowerShell-only pipeline*: Possible, but Python offers easier cross-platform parsing and aligns with other FieldWorks automation scripts.

## Ambiguous Project Checkpoint
- **Process**: After generating `Output/GenerateAssemblyInfo/generate_assembly_info_audit.csv`, filter rows whose `remediationState` remains `NeedsRemediation` but lack a deterministic rule (e.g., projects that intentionally removed `AssemblyInfo` files). Export those rows into a temporary CSV and capture owner decisions inside this section to keep an auditable trail.
- **Tools**: `history_diff.py` supplies `restore_map.json`; the audit CLI will mark `category=G` with `notes` like `analysis-error` or `manual-review`. Before running the conversion script, reviewers must sign off on each ambiguous row by adding a bullet below with rationale and the chosen action (link template, restore file, or document exception).
- **Blocking Rule**: The `/speckit.implement` workflow must not start conversion (User Story 2) until every ambiguous row is resolved and referenced here.
