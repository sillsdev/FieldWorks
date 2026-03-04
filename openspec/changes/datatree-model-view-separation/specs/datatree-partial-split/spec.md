## ADDED Requirements

### Requirement: Partial-class file decomposition of DataTree

DataTree.cs SHALL be split into partial-class files organized by responsibility. Each file SHALL contain one logical concern. The runtime behavior SHALL remain identical — no method signatures, visibility, or logic changes.

#### Scenario: File split preserves compilation
- **WHEN** DataTree.cs is split into partial-class files
- **THEN** the project compiles with zero errors and zero new warnings

#### Scenario: File split preserves test results
- **WHEN** all existing DataTreeTests pass before the split
- **THEN** all existing DataTreeTests pass after the split with no modifications

### Requirement: Partial-class file organization

The following files SHALL be created, each containing the indicated `#region` or logical grouping:

| File | Content |
|------|---------|
| `DataTree.cs` | Data members, constructor, `Initialize`, `Dispose`, `CheckDisposed`, core properties (`Root`, `Cache`, `StyleSheet`, `Mediator`, etc.) |
| `DataTree.SliceManagement.cs` | `InsertSlice`, `RemoveSlice`, `RawSetSlice`, `InstallSlice`, `ForceSliceIndex`, `ResetTabIndices`, `InsertSliceRange`, tooltip management |
| `DataTree.LayoutParsing.cs` | `CreateSlicesFor`, `ApplyLayout`, `ProcessPartRefNode`, `ProcessPartChildren`, `ProcessSubpartNode`, `AddSimpleNode`, `AddAtomicNode`, `AddSeqNode`, `MakeGhostSlice`, `EnsureCustomFields`, `GetMatchingSlice`, `GetFlidFromNode`, label/weight resolution methods |
| `DataTree.WinFormsLayout.cs` | `OnLayout`, `HandleLayout1`, `MakeSliceRealAt`, `MakeSliceVisible`, `OnPaint`, `HandlePaintLinesBetweenSlices`, `OnSizeChanged`, `IndexOfSliceAtY`, `HeightOfSliceOrNullAt`, `FieldAt`, `FieldOrDummyAt`, `AboutToCreateField` |
| `DataTree.Navigation.cs` | `CurrentSlice` property, `DescendantForSlice`, `GotoFirstSlice`, `GotoNextSlice`, `GotoNextSliceAfterIndex`, `GotoPreviousSliceBeforeIndex`, `LastSlice`, `FocusFirstPossibleSlice`, `SelectFirstPossibleSlice`, `ScrollCurrentAndIfPossibleSectionIntoView`, `SetDefaultCurrentSlice`, `ActiveControl` |
| `DataTree.Messaging.cs` | All `IxCoreColleague` implementation (`Init`, `GetMessageTargets`, `ShouldNotCall`, `Priority`) and all `On*` message handlers |
| `DataTree.Persistence.cs` | `PrepareToGoAway`, `PersistPreferences`, `RestorePreferences`, `SetCurrentSlicePropertyNames`, `ShowObject` entry point, `RefreshList`, `CreateSlices`, show-hidden fields logic |

#### Scenario: Each file contains exactly one concern
- **WHEN** a developer opens `DataTree.LayoutParsing.cs`
- **THEN** they see only XML layout interpretation methods, not WinForms layout or messaging code

### Requirement: Partial-class decomposition of Slice

Slice.cs SHALL be similarly split into partial-class files. The exact file list SHALL be determined during implementation but MUST separate at minimum: core properties, installation/lifecycle, tree-node rendering, and child generation.

#### Scenario: Slice file split preserves compilation
- **WHEN** Slice.cs is split into partial-class files
- **THEN** the project compiles with zero errors and all SliceTests pass unchanged
