# Build/Agent Scripts

PowerShell scripts for build, test, and CI orchestration.

## Placement policy

- Put new build/test/CI orchestration scripts in `Build/Agent/`.
- Keep `scripts/Agent/` for existing helper wrappers and installer/ops-oriented scripts.

## CI helper scripts

| Script | Purpose |
|--------|---------|
| `Run-AllRenders.ps1` | Runs the DetailControls and RootSite render suites sequentially from one command without adding a meta test project. |
| `Summarize-NativeTestResults.ps1` | Parses native Unit++ logs and appends a pass/fail summary table to GitHub step summary. |

## GitHub Actions usage

These scripts are used by `.github/workflows/CI.yml` to keep workflow YAML concise and keep CI behavior testable/reusable in script files.
