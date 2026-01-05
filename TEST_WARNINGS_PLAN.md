# Test Warnings Eradication Plan
**Branch:** `fix_csharp_tests`
**Last Updated:** 2025-12-08
**Goal:** Eliminate all compiler warnings in test projects (no suppressions), so tests build cleanly with `TreatWarningsAsErrors=true`.

## Scope
- All managed test projects (Debug/x64), starting with assemblies that currently fail under warnings-as-errors.
- Includes duplicated `AssemblyInfoForTests.cs` items and unused field/variable warnings.
- Excludes production assemblies unless needed to unblock test fixes.

## Constraints
- Do not suppress with pragmas or analyzer suppressions; prefer code changes (remove, refactor, or make assertions meaningful).
- Preserve test intent; when removing unused members, ensure no coverage loss.
- Continue to use repo-standard entry points (`./build.ps1`, `./test.ps1`) for validation.

## Baseline & Tracking
1. **Inventory warnings without failing the build (all managed tests)**
   - Run from the repo root: `msbuild FieldWorks.proj /p:Configuration=Debug /p:Platform=x64 /p:TreatWarningsAsErrors=false /m /t:allCsharp "/flp:logfile=warnings.log;warningsonly"`.
   - The warnings-only log is written to the repo root as `warnings.log`. Per-project reruns can emit `warnings-<project>.log` using the same `/flp` pattern.
2. **Bucket by project and warning code**
   - Parse `warnings.log` to a CSV (project, code, file, line, message).
   - Rank by count and by project to target largest clusters first.
3. **Definition of Done for this plan**
   - `warnings.log` empty for test projects when running with default `TreatWarningsAsErrors` (i.e., a clean build).

## Fix Strategy by Warning Pattern
- **CS2002 (duplicate source include)**: In the test `.csproj`, keep a single include for `Src\AssemblyInfoForTests.cs`.
   1) Remove extra `<Compile Include="..\..\..\..\AssemblyInfoForTests.cs" />` entries; or
   2) Add `<Compile Remove="..\..\..\..\AssemblyInfoForTests.cs" />` if the item is brought in transitively.
- **CS0169/CS0649 (unused/unassigned fields)**: Delete unused fields or initialize and assert on them. If kept for side-effects, set explicitly and assert the expected behavior.
- **CS0219/CS0168 (assigned/dead locals)**: Remove the dead local or replace with `_ = <expr>;` only when the expression’s side-effects matter. Prefer asserting on the value instead of discarding it.
- **CS0414 (assigned but never used fields)**: Remove the assignment or assert on the field to prove intent.
- **Missing references (e.g., MSB3245)**: Add the needed `PackageReference`/`ProjectReference` for `AssemblyInfoForTests.cs` dependencies (`FwUtils`, `FwUtilsTests`, `SIL.LCModel.Core`, `SIL.LCModel.Utils`, `SIL.TestUtilities`), or set `<UseUiIndependentTestAssemblyInfo>true</UseUiIndependentTestAssemblyInfo>` to swap in the lighter assembly info.
- **Other warning codes**: Address case-by-case with minimal, intent-preserving edits (rename, initialize, or restructure tests as needed).

## Execution Order (initial target list from last build)
1. `WidgetsTests` — unused stylesheet fields (`FwTextBoxTests`, `FwListBoxTests`).
2. `FwControlsTests` — unused `flags` local (`ObtainProjectMethodTests`).
3. `FwCoreDlgsTests` — unused/never-assigned fields (`FwNewLangProjectModelTests`, `TestWSContainer`, `CharContextCtrlTests`).
4. `FieldWorksTests` — unused `m_defaultBepType` (`ProjectIDTests`).
5. `xCoreInterfacesTests` — multiple dead locals in `PropertyTableTests`.
6. `SimpleRootSiteTests` — uninitialized `m_rootb` (`ScrollTestsBase`).
7. `ParserCoreTests` — unused `m_fResultMatchesExpected` (`M3ToXAmpleTransformerTests`).
8. `xWorksTests` — numerous dead locals and unused fields (e.g., `DictionaryConfigManagerTests`, `MacroListenerTests`, `CssGeneratorTests`, `ConfiguredXHTMLGeneratorTests`, `DictionaryDetailsControllerTests`).
9. `LexTextControlsTests` — unused locals in `Lift*` tests; unassigned flags.
10. `Paratext8PluginTests` — unused `_potentialScriptureProvider` field.
11. `DiscourseTests` — unused/unassigned fields (`ConstituentChartDatabaseTests`, `ConstChartRowDecoratorTests`).
12. Any remaining warnings discovered in the baseline log.

## Workflow per Project
1. Open the warning cluster for the project; fix all warnings in a tight PR-sized batch.
2. Run targeted tests for the project (`./test.ps1 -TestProject "<path>" -NoBuild` when possible) to validate.
3. Update `warnings.log` to confirm the cluster is cleared before moving to the next.

## Validation
- After all clusters are addressed, run `./build.ps1` to confirm zero warnings in tests with warnings-as-errors enabled.
- Follow with `./test.ps1 -NoBuild` to ensure runtime behavior is unchanged.

## Reporting
- Track progress by updating this plan and `warnings.log` checkpoints after each project group is cleaned.
- Add brief notes to `CSHARP_TEST_FAILURES.md` if any warning fix uncovers real defects.
