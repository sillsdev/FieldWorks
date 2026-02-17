# Quickstart — PlatformTarget Redundancy Cleanup

1. **Audit current PlatformTarget usage**
   ```powershell
   python convergence.py platform-target audit --output specs/006-convergence-platform-target/platform_target_audit.csv
   ```
2. **Review audit output**
   - Confirm only FwBuildTasks should stay `AnyCPU`.
   - Mark all other explicit `x64` entries as `Remove` in `specs/006-convergence-platform-target/platform_target_decisions.csv`.
3. **Apply conversions**
   ```powershell
   python convergence.py platform-target convert --decisions specs/006-convergence-platform-target/platform_target_decisions.csv
   ```
4. **Validate + build**
   ```powershell
   python convergence.py platform-target validate
   msbuild FieldWorks.proj /m /p:Configuration=Debug
   ```
5. **Finalize**
   - Add (or verify) the XML comment beside the `FwBuildTasks` `AnyCPU` declaration explaining the exception.
   - Commit updated `.csproj` files plus CSV artifacts and research documentation.
