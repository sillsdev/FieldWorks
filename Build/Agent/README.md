# Build/Agent Scripts

PowerShell scripts for build, test, and CI orchestration.

## Placement policy

- Put new build/test/CI orchestration scripts in `Build/Agent/`.
- Keep `scripts/Agent/` for existing helper wrappers and installer/ops-oriented scripts.

## CI helper scripts

| Script | Purpose |
|--------|---------|
| `Run-ManagedCi.ps1` | Orchestrates managed CI path: build (`build.ps1 -BuildTests`) then filtered managed tests (`test.ps1 -NoBuild`). |
| `Run-NativeCi.ps1` | Orchestrates native CI path: build (`build.ps1 -BuildTests`), build native test exes, then run native tests (`test.ps1 -Native -NoBuild`). |
| `Build-NativeTestExecutables.ps1` | Builds native test executables (`TestGeneric`, `TestViews`) via `Build/scripts/Invoke-CppTest.ps1`. |
| `Summarize-NativeTestResults.ps1` | Parses native Unit++ logs and appends a pass/fail summary table to GitHub step summary. |

## GitHub Actions usage

These scripts are used by `.github/workflows/CI.yml` to keep workflow YAML concise and keep CI behavior testable/reusable in script files.
