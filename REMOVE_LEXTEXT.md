# Removing LexTextExe (Flex.exe) Launcher

## Context Recap
- `Src\LexText\LexTextExe\LexText.cs` is a 32-line stub whose `Main` simply calls `FieldWorks.StartFwApp(rgArgs)` and exits. It exists only to produce `Flex.exe` with LexText-branded icons.
- The real application entry point (`[STAThread] static int Main`) plus all field lifecycle logic already live in `Src\Common\FieldWorks\FieldWorks.cs` (output `FieldWorks.exe`).
- `LexTextExe.csproj` references `FieldWorks.csproj` (and all shared libs) and copies `Flex.exe.manifest`. `FieldWorks.csproj` already generates its own manifest via `BuildInclude.targets`.
- COPILOT summaries confirm: LexTextExe is a "Minimal entry point" while `FieldWorks` holds startup dialogs, analytics, localization, cache creation, etc. xWorks (`Src\xWorks`) hosts the actual FLEx shell via `LexTextApp`.
- Maintaining two executables complicates reg-free COM coverage, packaging, and produces redundant shortcuts in the installer.

## Desired End State
1. Ship a single supported executable named `FieldWorks.exe` that users double-click and scripts invoke.
2. Preserve existing functionality, splash sequence, icons, and command-line behaviors.
3. Remove the obsolete `LexTextExe` project, binaries, and installer assets once back-compat contingencies are addressed.

## Guiding Constraints & References
- Follow `.github/instructions/managed.instructions.md` for C# changes, `.github/instructions/build.instructions.md` for project edits, and `.github/instructions/installer.instructions.md` for WiX updates.
- Keep reg-free COM manifest generation via `BuildInclude.targets` in `FieldWorks` and extend it as needed; do **not** regress the spec/003 work.
- Respect documentation workflows (`.github/update-copilot-summaries.md`) when touching COPILOT summaries later.

## Workstream 1 – Entry Point & Branding Consolidation
1. **Embed FLEx branding in FieldWorks.exe**
   - Move `LT.ico`, `LT.png`, `LT64.png`, `LT128.png` assets under `Src\Common\FieldWorks` (or shared resources) and update `FieldWorks.csproj` to set `<ApplicationIcon>` and resource items.
   - Verify icons currently referenced in `FieldWorks` (Book+Cube) are either retained for other tools or removed if unused.
2. **Confirm `FieldWorks.Main` handles all CLI scenarios**
   - Review CLI parsing in `FieldWorks.cs` (`FwAppArgs`, `StartFwApp`) to ensure direct invocation behaves as today.
   - Document supported arguments in README/spec after consolidation.
3. **Optional transitional shim**
   - For one release, consider keeping a tiny `Flex.exe` (new project or existing stub) that immediately displays "Use FieldWorks.exe" and forwards args, to avoid breaking automation. Plan removal date.

## Workstream 2 – Build & Solution Cleanup
1. **Remove `LexTextExe` project**
   - Delete `Src\LexText\LexTextExe` (csproj, icons, manifest) after replacement steps are done.
   - Update `FieldWorks.sln`, `FieldWorks.proj`, `dirs.proj` (if referenced) to drop the project.
2. **Fix MSBuild references**
   - Any projects referencing `Flex.exe` outputs need to be updated to consume `FieldWorks.exe` or the shared libs directly.
   - Run `git grep -n "LexTextExe"` / `"Flex.exe"` to locate build scripts, packaging, or docs that require edits.
3. **Adjust build artifacts**
   - Ensure `Output/Debug` no longer expects `Flex.exe`. Update scripts that copy both EXEs to staging folders.

### Reference Sweep Status
- Latest `git grep -n "LexTextExe"` / `"Flex.exe"` runs show no build or packaging files still depend on the legacy stub.
- Technical narratives (`ReadMe.md`, `Docs/64bit-regfree-migration.md`, `SDK-MIGRATION.md`, LexText/FieldWorks COPILOT files) retain brief historical mentions and were left intact for context.
- Specs `specs/001-64bit-regfree-com` and `specs/003-convergence-regfree-com-coverage` cite LexTextExe solely to document reg-free rollout sequencing.
- `FLExInstaller/CustomComponents.wxi` intentionally keeps a `<RemoveFile Name="Flex.exe"/>` directive to delete stale binaries when upgrading from previous releases.

## Workstream 3 – Installer & Packaging
1. **WiX payload updates** (`FLExInstaller/**` per installer instructions)
   - Remove `Flex.exe` components/files/fragments.
   - Update shortcuts (Start Menu, desktop) to point at `FieldWorks.exe` while keeping display text "FieldWorks Language Explorer".
   - Ensure `FieldWorks.exe.manifest` is included for reg-free COM.
2. **Upgrade/repair scenarios**
   - If the installer previously removed/updated `Flex.exe`, ensure uninstall of older versions deletes `Flex.exe` to prevent stale binaries.
3. **Bootstrapper scripts**
   - Review `DistFiles`, `Build/Installer.targets`, `Build/RegFree.targets` for hard-coded `Flex.exe` references.

## Workstream 4 – Documentation & Tooling
1. Update:
   - `README.md`, `Docs/64bit-regfree-migration.md`, `SDK-MIGRATION.md` to mention single executable.
   - Any internal troubleshooting guides referencing `Flex.exe`.
2. COPILOT summaries:
   - `Src/LexText/LexTextExe/COPILOT.md` removed with project.
   - Update `FieldWorks/COPILOT.md`, `LexText/COPILOT.md`, `xWorks/COPILOT.md` to describe the unified entry point.
3. Specs/Plans:
   - Integrate outcome into spec `specs/003-convergence-regfree-com-coverage/spec.md` once implemented.

## Workstream 5 – Testing & Validation
1. **Automated Validation**
   - Build `FieldWorks.sln` in the fw-agent container; ensure no missing project references.
   - Run targeted smoke tests (e.g., `dotnet test Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.csproj`).
2. **Manual Verification**
   - Launch `FieldWorks.exe` directly with/without project args on a clean VM to ensure no registry reliance.
   - Confirm splash/update dialogs still show once per process.
   - Validate `FieldWorks.exe` still registers analytics, handles DPI awareness, and opens projects.
3. **Installer QA**
   - Build installer, install on clean Windows, verify shortcuts launch the new exe.
   - Upgrade from prior version: confirm old `Flex.exe` removed and new FieldWorks works.

## Workstream 6 – Risk & Mitigation
| Risk                                           | Mitigation                                                                                                                                |
| ---------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Automation or scripts reference `Flex.exe`     | Communicate change in release notes, optionally ship deprecation shim for one release, provide alias batch file if needed.                |
| Missing icon resources create generic exe icon | Confirm `<ApplicationIcon>` and resources compile; check resulting EXE properties.                                                        |
| WiX removal leaves orphaned files              | Add removal custom actions or ensure Component GUID reuse removes old files.                                                              |
| Reg-free manifest path breaks                  | Ensure `FieldWorks` still sets `<EnableRegFreeCom>true</EnableRegFreeCom>` and includes manifest copy steps; update spec 003 accordingly. |

## Execution Sequence (Recommended)
1. Add FLEx icon resources to `FieldWorks.csproj`, verify `FieldWorks.exe` branding.
2. Update installer shortcuts to target `FieldWorks.exe`; validate icons.
3. Remove `LexTextExe` from solution/build; update references.
4. Clean up documentation/specs.
5. Decide on shim (if any), communicate change, merge.

## Implementation Checklist
- [x] Copy FLEx icon assets into `Src/Common/FieldWorks`, reference them via `<ApplicationIcon>` and verify the compiled `FieldWorks.exe` metadata uses the new icon.
- [ ] Re-run reg-free COM generation for `FieldWorks.exe` to confirm `FieldWorks.exe.manifest` still emits under `Output/<Config>`.
- [x] Delete `Src/LexText/LexTextExe` (project, icons, manifest) and remove the project entry from `FieldWorks.sln`, `FieldWorks.proj`, and any traversal props/targets.
- [x] Search the repo for `LexTextExe` or `Flex.exe` (scripts, WiX fragments, docs) and update each reference to `FieldWorks.exe` (see "Reference Sweep Status").
- [x] Update WiX authoring so installer components, shortcuts, and upgrades install only `FieldWorks.exe` + manifest, and ensure uninstall removes any legacy `Flex.exe`.
- [x] Don't use a temporary shim `Flex.exe`.
- [x] Update `README.md`, `SDK-MIGRATION.md`, `Docs/64bit-regfree-migration.md`, and relevant COPILOT/spec files (using update-copilot-summaries.md) to reflect the single executable.
- [ ] Build `FieldWorks.sln` (fw-agent container path) and run `dotnet test Src/Common/FieldWorks/FieldWorksTests/FieldWorksTests.csproj` to catch regressions.
- [ ] Install the rebuilt MSI on a clean VM, verify shortcuts launch `FieldWorks.exe`, and confirm an upgrade from the previous version removes the old `Flex.exe` binary.
- [ ] Document rollout status (including telemetry/announcement plan) before merging to ensure downstream teams know the executable name change.

## Follow-Up Questions
- Do we need to keep a temporary `Flex.exe` shim for external automation? If yes, how long?
- Are there other FieldWorks-branded exes (e.g., `LexTextAppLoader`) hidden in installers that also need collapsing?
- Should telemetry distinguish between launches via former stub vs direct `FieldWorks.exe` for rollout metrics? (Possible by logging environment flag for one release.)
