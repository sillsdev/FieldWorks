# Quickstart: GenerateAssemblyInfo Template Reintegration

## Prerequisites
- Windows developer environment with FieldWorks repo checked out in `fw-agent-1` worktree.
- Visual Studio 2022 build tools + WiX 3.14.1 per `.github/instructions/build.instructions.md`.
- Python 3.11 available in the repo environment (`py -3.11`).
- Ensure `Src/CommonAssemblyInfo.cs` is regenerated via `Build/SetupInclude.targets` before auditing.

### Automation entry points

All commands live under `scripts/GenerateAssemblyInfo/` and follow conventional CLI usage:

| Script                               | Purpose                                                                                           | Key Outputs                                                                             |
| ------------------------------------ | ------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| `audit_generate_assembly_info.py`    | Scans `Src/**/*.csproj`, classifies projects, and emits CSV/JSON summaries.                       | `Output/GenerateAssemblyInfo/generate_assembly_info_audit.csv` + optional decisions map |
| `convert_generate_assembly_info.py`  | Applies template links, flips `GenerateAssemblyInfo`, restores deleted files using a restore map. | Updated `.csproj` files + `Output/GenerateAssemblyInfo/decisions.csv`                   |
| `validate_generate_assembly_info.py` | Ensures repository-wide compliance, optionally running MSBuild + reflection harness.              | `Output/GenerateAssemblyInfo/validation_report.txt` + log files                         |

Each script accepts common flags defined in `scripts/GenerateAssemblyInfo/cli_args.py` (branch selection, output directories, restore-map path). The commands below show the baseline invocation pattern.

## 1. Run the audit
```powershell
py -3.11 scripts/GenerateAssemblyInfo/audit_generate_assembly_info.py `
    --output Output/GenerateAssemblyInfo `
    --json
```
- Produces `Output/GenerateAssemblyInfo/generate_assembly_info_audit.csv` plus a JSON mirror when `--json` is supplied.
- Review any `ManualReview` rows and annotate `decisions.csv` accordingly.

## 2. Apply conversions/restorations
```powershell
py -3.11 scripts/GenerateAssemblyInfo/convert_generate_assembly_info.py `
    --decisions Output/GenerateAssemblyInfo/decisions.csv `
    --restore-map Output/GenerateAssemblyInfo/restore.json
```
- Inserts the `<Compile Include="..\\..\\CommonAssemblyInfo.cs" Link="Properties\\CommonAssemblyInfo.cs" />` entry where missing.
- Forces `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` with an XML comment referencing the shared template.
- Restores any deleted `AssemblyInfo*.cs` directly from the supplied git shas.

## 3. Validate repository state
```powershell
py -3.11 scripts/GenerateAssemblyInfo/validate_generate_assembly_info.py `
    --report Output/GenerateAssemblyInfo/validation_report.txt
```
- Confirms every managed project links the template, contains at most one compile entry for `CommonAssemblyInfo.cs`, and has `GenerateAssemblyInfo=false`.
- Optionally run `msbuild FieldWorks.sln /m /p:Configuration=Debug` followed by Release to enforce zero CS0579 warnings.

## 4. Finalize and prepare review
1. Add the generated CSV/JSON reports to the PR under `Output/GenerateAssemblyInfo/` as artifacts.
2. Update relevant `AGENTS.md` files if project documentation changes.
3. Capture before/after counts (template-only vs template+custom) in the spec and reference the validation report in the PR description.

