## Why

The DetailControls subsystem (`Src/Common/Controls/DetailControls/`) is the property-editing backbone of FLEx — every field the user edits lives inside a DataTree of Slices, Launchers, and Views. Yet it has only ~31 tests across ~65 production files, with 11 key classes at zero coverage. The recent LT-22414 bug (Morph Type field disappearing after type change) was caused by a missing `RefreshListNeeded = true` in a single method — a defect that trivial tests would have caught. The subsystem also lacks a `DetailControls/AGENTS.md` (the parent AGENTS.md has a dangling link to it), so agents working in this area have no architectural guidance.

## What Changes

- **Create `DetailControls/AGENTS.md`** documenting the three-hierarchy composition pattern (Slices, Launchers, Views), lifecycles, refresh protocol, and testing strategy.
- **Add unit tests for pure-logic methods** — `IsStemType()`, `CheckForAffixDataLoss`, `CheckForStemDataLoss` — that require zero WinForms infrastructure.
- **Add integration tests for the refresh protocol** — expand `DataTree.PropChanged`/`RefreshList` coverage beyond the single LT-22414 scenario.
- **Introduce seam interfaces** (`IDataTreeServices`) to break tight coupling between Slices/Launchers and concrete DataTree/Cache/Mediator, enabling test doubles.
- **Extract launcher logic** into POCO classes (e.g., `MorphTypeSwapLogic`) following the Humble Object pattern, making business-critical morph-type-swap logic independently testable.
- **Document the architecture** with a Mermaid class diagram showing all inheritance hierarchies and composition relationships.

## Non-goals

- Rewriting or replacing the WinForms UI framework.
- Changing any user-visible behavior or UI layout.
- Adding Avalonia or cross-platform support.
- Modifying the XML layout/parts configuration schema.
- Native C++ changes — this is purely managed (C#) code.

## Capabilities

### New Capabilities

- `detail-controls-architecture`: Architectural documentation of the DetailControls subsystem — class hierarchies, composition patterns, lifecycles, refresh protocol, and the Mermaid class diagram.
- `detail-controls-testability`: Seam interfaces, Humble Object extractions, and test infrastructure improvements enabling unit testing of launcher and slice logic without WinForms dependencies.

### Modified Capabilities

- `architecture/ui-framework/winforms-patterns`: Add cross-reference to the new DetailControls architecture spec, since DetailControls is the primary consumer of the WinForms patterns documented there.

## Impact

- **Affected code:** `Src/Common/Controls/DetailControls/` — launchers, slices, views, DataTree
- **Test project:** `Src/Common/Controls/DetailControls/DetailControlsTests/`
- **Documentation:** New `DetailControls/AGENTS.md`, new architecture spec, updated `winforms-patterns.md`
- **Dependencies:** No new NuGet packages or external dependencies
- **Risk:** Low — seam interfaces are additive; Humble Object extractions preserve existing behavior behind delegation
