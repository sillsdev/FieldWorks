# Quickstart – Test Exclusion Pattern Standardization

Follow these steps to audit existing projects, convert them to Pattern A, and keep the repo compliant.

## 1. Prerequisites
- Windows 10/11 x64 with Visual Studio 2022 tooling enabled (per `.github/instructions/build.instructions.md`).
- Python 3.11 available on PATH for running the audit/convert/validate scripts.
- FieldWorks repo cloned or agent worktree ready; run commands from the repo root (`c:/Users/johnm/Documents/repos/fw-worktrees/agent-4`).
- Ensure `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot` has been executed so other agents inherit this plan.

## 2. Audit Current Patterns
```powershell
python -m scripts.audit_test_exclusions
```
- Produces `Output/test-exclusions/report.json` + `report.csv` summarizing every `.csproj`, detected pattern, and missing exclusions.
- Automatically writes `Output/test-exclusions/mixed-code.json` plus Markdown issue templates under `Output/test-exclusions/escalations/` for every project that mixes production and test code.
- Open an issue for each template (one per project), link the Markdown body, and assign the owning team **before** running conversions.

## 3. Convert Projects to Pattern A
```powershell
python -m scripts.convert_test_exclusions --input Output/test-exclusions/report.json --batch-size 15 --dry-run
```
- **Dry Run**: Always start with `--dry-run` to review planned edits.
- **Execute**: Remove `--dry-run` to apply changes. The script automatically verifies builds for each converted project.
- **Fast Mode**: Use `--no-verify` to skip the per-project build check (faster, but risky; use only if you plan to run a full solution build immediately after).
- **Mixed Code**: If the script encounters a mixed-code project, it skips it.
- **Post-Conversion**: Rerun the audit command to update `report.json` with the new `patternType` values before starting the next batch.


## 4. Validate Before Committing
```powershell
python -m scripts.validate_test_exclusions --fail-on-warning
msbuild FieldWorks.proj /m /p:Configuration=Debug
powershell scripts/test_exclusions/assembly_guard.ps1 -Assemblies "Output/Debug/**/*.dll"
```
- First command enforces policy-level checks (no wildcards, no missing exclusions, no mixed code). Use `--json-report` when you need a machine-readable summary.
- Second command ensures MSBuild succeeds without CS0436 errors and that no test code leaks into binaries.
- Third command loads each produced assembly and fails if any type name matches `*Test*`; keep the output as part of the manual release sign-off package.

## 5. Keep Documentation in Sync
- After each batch, update `.github/instructions/managed.instructions.md`, Directory.Build.props comments, and any affected `Src/**/AGENTS.md` files so guidance matches the new exclusions.
- Re-run the COPILOT validation helpers (detect/propose/validate) once the documentation refresh is complete.

## 6. Rollout Tips
- Convert 10–15 projects per PR to keep review diffs manageable.
- Always rerun the audit script after merging to refresh the baseline report.
- Coordinate with teams owning flagged projects so structural fixes (e.g., splitting test utilities) keep pace with conversions.

