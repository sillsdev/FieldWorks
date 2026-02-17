# Quickstart – PrivateAssets Convergence

## Prerequisites
1. Ensure the `fw-agent-3` container is running (required for agent worktrees).
2. From the repo root run `source ./environ` (Linux) or use the Developer PowerShell shortcut (Windows host) before attaching to the container.
3. Install Python deps if missing: `pip install -r BuildTools/FwBuildTasks/requirements.txt`.

## Core Workflow

1. **Audit**
   ```powershell
   cd C:\Users\johnm\Documents\repos\fw-worktrees\agent-3
   python convergence.py private-assets audit
   ```
   - Produces `private_assets_audit.csv` listing each `*Tests.csproj` with missing `PrivateAssets` on `SIL.LCModel.*.Tests` packages.
   - Spot-check the CSV to ensure only the intended LCM packages appear in `PackagesMissingPrivateAssets`.

2. **Approve decisions** (optional)
   - Copy `private_assets_audit.csv` to `private_assets_decisions.csv`.
   - Mark rows you want to skip by changing the `Action` column to `Ignore`.

3. **Convert**
   ```powershell
   python convergence.py private-assets convert --decisions private_assets_decisions.csv
   ```
   - Script rewrites each targeted `.csproj`, adding `PrivateAssets="All"` without disturbing other attributes.
   - Review `git status` to verify only expected files changed.

4. **Validate**
   ```powershell
   python convergence.py private-assets validate
   # Output: ✅ All projects pass validation!

   docker exec fw-agent-3 powershell -NoProfile -Command "msbuild FieldWorks.sln /m /p:Configuration=Debug" > Output/Debug/private-assets-build.log
   ```
   - Validation confirms every `SIL.LCModel.*.Tests` reference now specifies `PrivateAssets="All"`.
   - The MSBuild run should ideally succeed. If it fails with unrelated errors (e.g. CS0579), verify absence of NU1102 warnings:
     ```powershell
     Select-String "NU1102" Output/Debug/private-assets-build.log
     # Expect: no matches
     ```

5. **Rollback (if needed)**
   ```powershell
   git checkout -- src/.../YourTests.csproj
   ```
   - Re-run the audit to regenerate CSVs after any rollback.

## Deliverables Checklist
- [ ] Updated `.csproj` files limited to projects referencing `SIL.LCModel.*.Tests` packages.
- [ ] `private_assets_audit.csv` and `private_assets_decisions.csv` attached to the PR (optional but recommended).
- [ ] Validation logs showing clean MSBuild + absence of NU1102 warnings.
