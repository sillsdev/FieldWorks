## 1. Architecture Documentation

- [ ] 1.1 Create `Src/Common/Controls/DetailControls/AGENTS.md` documenting the three-hierarchy composition pattern (Slices, Launchers, Views), key lifecycles, and refresh protocol. Reference `detail-controls-class-diagram.mmd`.
- [ ] 1.2 Copy `detail-controls-class-diagram.mmd` to `Src/Common/Controls/DetailControls/` alongside AGENTS.md for permanent maintenance.
- [ ] 1.3 Update `openspec/specs/architecture/ui-framework/winforms-patterns.md` to cross-reference the new DetailControls architecture documentation.
- [ ] 1.4 Verify the parent `Src/Common/Controls/AGENTS.md` link to `DetailControls/AGENTS.md` resolves correctly.

## 2. Trivial Unit Tests (Pure Logic, No WinForms)

- [ ] 2.1 Add unit tests for `MorphTypeAtomicLauncher.IsStemType()` — test all `MoMorphTypeTags.kguidMorphType*` GUIDs. File: `DetailControlsTests/MorphTypeAtomicLauncherTests.cs`. [Managed C#, ~30 min]
- [ ] 2.2 Add unit tests for `MorphTypeAtomicLauncher.CheckForAffixDataLoss()` using `MemoryOnlyBackendProvider` with real LCM morph objects. File: `DetailControlsTests/MorphTypeAtomicLauncherTests.cs`. [Managed C#, ~45 min]
- [ ] 2.3 Add unit tests for `MorphTypeAtomicLauncher.CheckForStemDataLoss()` using `MemoryOnlyBackendProvider`. File: `DetailControlsTests/MorphTypeAtomicLauncherTests.cs`. [Managed C#, ~45 min]
- [ ] 2.4 Run `.\test.ps1 -TestProject DetailControlsTests` and verify all new and existing tests pass.

## 3. Refresh Protocol Test Expansion

- [ ] 3.1 Add integration test: multiple `PropChanged` during `DoNotRefresh` results in single refresh on release. File: `DetailControlsTests/MorphTypeAtomicLauncherTests.cs` or new `DataTreeRefreshTests.cs`. [Managed C#, ~1 hr]
- [ ] 3.2 Add integration test: `PropChanged` with `m_postponePropChanged = true` defers correctly. [Managed C#, ~1 hr]
- [ ] 3.3 Run full `DetailControlsTests` suite to confirm no regressions.

## 4. Seam Interface Introduction

- [ ] 4.1 Define `IDataTreeServices` interface in `Src/Common/Controls/DetailControls/` exposing `LcmCache`, `Mediator`, `PropertyTable` accessors. [Managed C#, ~30 min]
- [ ] 4.2 Implement `IDataTreeServices` on `DataTree` (additive, no behavior change). [Managed C#, ~30 min]
- [ ] 4.3 Build with `.\build.ps1` and verify zero compilation errors.
- [ ] 4.4 Add the interface to `InternalsVisibleTo` if needed for test access.

## 5. Humble Object Extraction (MorphTypeSwapLogic)

- [ ] 5.1 Extract `MorphTypeSwapLogic` POCO class containing `IsStemType`, `CheckForAffixDataLoss`, `CheckForStemDataLoss`, `ChangeAffixToStem`, `ChangeStemToAffix` logic from `MorphTypeAtomicLauncher`. [Managed C#, ~1.5 hr]
- [ ] 5.2 Update `MorphTypeAtomicLauncher` to delegate to `MorphTypeSwapLogic`. [Managed C#, ~30 min]
- [ ] 5.3 Add direct unit tests for `MorphTypeSwapLogic` — no WinForms dependency. [Managed C#, ~1 hr]
- [ ] 5.4 Run `.\test.ps1 -TestProject DetailControlsTests` to verify all existing tests still pass.
- [ ] 5.5 Build with `.\build.ps1` to verify full solution compiles.

## 6. Validation & Cleanup

- [ ] 6.1 Run `.\Build\Agent\check-and-fix-whitespace.ps1` to ensure clean whitespace.
- [ ] 6.2 Run `.\test.ps1` for affected test projects to confirm no regressions.
- [ ] 6.3 Update `openspec/specs/` with any adjusted requirements discovered during implementation.
