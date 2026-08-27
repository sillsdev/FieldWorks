# Avalonia Uses `.fwlayout` Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the legacy XML layout inventory the single persisted source of project layout customization for both WinForms and Avalonia, then retire the unreleased Avalonia-only `.viewoverride.json` subsystem.

**Architecture:** Keep `Inventory` as the authority that loads shipped layouts, merges project `ConfigurationSettings/*.fwlayout` overrides, and persists mutations. Add an xWorks adapter that copies the effective `XmlNode` into an immutable `ViewDefinitionSourceSnapshot`; keep XCore out of FwAvalonia. Avalonia menu commands will execute the existing legacy `Slice` handlers through the already-approved hidden-DataTree command adapter, then recompose from `Inventory`. The compiler's content fingerprint, not a process-wide `(class, layout)` identity cache, will isolate projects and notice changed XML.

**Tech Stack:** C# 8 / .NET Framework 4.8, XCore `Inventory`, LINQ to XML, NUnit, PowerShell repository build/test scripts.

---

## Decision record

### One persistent store

`ConfigurationSettings/*.fwlayout` is the only supported project layout customization format after this work.

- WinForms and Avalonia read the same effective layout nodes from the project-keyed `Inventory`.
- Existing WinForms commands remain the only writers for field visibility, field order, and visible writing systems.
- Avalonia does not translate XML into a second persisted representation.
- Avalonia does not cache project A's effective layout for project B.

### Boundary

`FwAvalonia` remains independent of XCore, project folders, and mutable XML inventories. xWorks owns the adapter because it already references both XCore and FwAvalonia:

```text
Configuration/Parts/*.fwlayout
              +
project/ConfigurationSettings/*.fwlayout
              |
              v
     XCore Inventory (effective XML)
              |
              v
 xWorks immutable snapshot adapter
              |
              v
 FwAvalonia ViewDefinitionCompiler
              |
              v
      Avalonia DetailComposer
```

The adapter must clone XML to text before compilation. Compiler code must never retain a live `XmlNode` owned by `Inventory`.

### Cache rule

Remove `DetailComposer.CompilerSources.CompiledModels`, whose key omits project identity and XML content. Continue using `ViewDefinitionCompiler`, whose key includes a SHA-256 fingerprint of layout XML, parts XML, class, type, and base-class map. When a legacy command calls `Inventory.PersistOverrideElement`, the next snapshot has different XML and therefore a different compiler key without explicit invalidation.

**Implementation correction:** The Task 2 instruction to preserve `SnapshotCompileCount` was
not implemented. Arbitrary source resolvers may return reused snapshot instances, so an
incrementing static count would not describe compiler work or source freshness reliably. The
observable replacement contract is
`CompileForObject_InventoryContentFingerprintReusesAndRefreshesCompiledModel`: identical
snapshot content returns the same compiled model instance, while changed content returns a
different model with the changed behavior.

### Parts rule

This change makes layout customization converge; it does not redesign part loading. Keep the existing immutable merged `*Parts.xml` snapshot in `DetailComposer`. Project customization currently persists effective `<layout>` elements, and `LayoutCache.InitializePartInventories` does not load project-level part overrides. A separate parts-inventory unification would be unrelated scope.

### Existing JSON files

Do not migrate or delete `.viewoverride.json` files.

- The subsystem entered `main` in #964 and is not contained by a released FieldWorks tag.
- Automatic JSON-to-XML conversion would preserve a second compatibility contract while this change is explicitly retiring it.
- Deleting files from user project folders would be destructive.
- After this change, old files are inert. Developers using unreleased builds may delete them manually.

### Pull request order

Land this storage-convergence PR before the open customization PRs:

1. This PR: shared `.fwlayout` reads/writes and JSON retirement.
2. Rebase [#1097](https://github.com/sillsdev/FieldWorks/pull/1097); keep the writing-system behavior, but replace any JSON store/editor work with the shared legacy command path.
3. Rebase draft [#1108](https://github.com/sillsdev/FieldWorks/pull/1108); remove stacked assumptions and verify its writing-system selection lands in `.fwlayout` only.

Do not merge #1097 or #1108 first and then add migration code for their JSON output.

## Retirement inventory

Delete these production files in full:

- `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionOverrideApplier.cs`
- `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionOverrideDiffer.cs`
- `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionOverrideEditor.cs`
- `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionOverrideFileMigrator.cs`
- `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionOverrideJsonSerializer.cs`
- `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionOverrideMigrator.cs`
- `Src/Common/FwAvalonia/ViewDefinition/ViewDefinitionOverrideStore.cs`
- `Src/xWorks/Avalonia/DetailOverrideMigration.cs`

Delete these tests because they test the retired format or migration path:

- `Src/Common/FwAvalonia/FwAvaloniaTests/DetailOverrideRenderingTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideApplierTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideDifferTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideEdgeCaseTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideEditorTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideFileMigratorTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideJsonSerializerTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideMigratorTests.cs`
- `Src/Common/FwAvalonia/FwAvaloniaTests/ViewDefinitionOverrideStoreTests.cs`
- `Src/xWorks/xWorksTests/Avalonia/Composer/DetailOverrideMigrationTests.cs`

Replace, rather than simply delete, `Src/xWorks/xWorksTests/Avalonia/Composer/DetailComposerOverrideTests.cs`. Its useful assertions become `.fwlayout`/`Inventory` integration coverage.

Edit these production files to remove references to the retired layer:

- `Src/Common/FwAvalonia/Detail/DetailModel.cs`
- `Src/Common/FwAvalonia/FwAvalonia.csproj`
- `Src/xWorks/Avalonia/Composer/DetailComposer.cs`
- `Src/xWorks/Avalonia/Hosting/RecordEditView.Avalonia.cs`
- `Src/xWorks/Avalonia/Hosting/XCoreMenuBridge.cs` only if the post-command callback is implemented there

Keep these general view-definition components:

- `ViewDefinitionModel`, `XmlLayoutImporter`, and `DictionaryPartResolver`
- `ViewDefinitionSourceSnapshot`, `ViewDefinitionCompiler`, and its content cache
- `LayoutSourceLoader` for shipped layouts/parts and framework-neutral tests
- `ViewDefinitionJsonSerializer`; it serializes compiled definitions, not project override files
- `Newtonsoft.Json` references still used elsewhere; do not remove the package merely because the override serializer is gone

## Implementation tasks

### Task 1: Characterize `Inventory` as the source of effective layout XML

**Files:**

- Create: `Src/xWorks/xWorksTests/Avalonia/Composer/InventoryViewDefinitionSourceTests.cs`
- Create: `Src/xWorks/Avalonia/Composer/InventoryViewDefinitionSource.cs`

- [ ] Write a failing test that builds test `layouts` and `parts` inventories, asks the new source for `LexEntry/detail/Normal`, and verifies the returned snapshot contains the shipped layout.

- [ ] Write a failing test that calls `PersistOverrideElement` with a changed full `<layout>` and verifies a second snapshot contains the changed XML while the first snapshot remains unchanged.

- [ ] Write a failing test for choice layouts: exact `choiceGuid` wins, then the no-`choiceGuid` layout is the fallback. Use the same four-key lookup as `DataTree.GetTemplateForObjLayout`:

```csharp
inventory.GetElement("layout", new[] { className, "detail", layoutName, choiceGuid });
inventory.GetElement("layout", new[] { className, "detail", layoutName, null });
```

- [ ] Write a failing test that a missing derived-class layout walks to its base class and records the same base-class map the compiler uses for part resolution.

- [ ] Run the red tests:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~InventoryViewDefinitionSourceTests"
```

Expected: fail because `InventoryViewDefinitionSource` does not exist.

- [ ] Implement `InventoryViewDefinitionSource` in xWorks. Constructor inputs are the project layout `Inventory`, immutable merged parts XML, and metadata cache. Its public operation returns a `ViewDefinitionSourceSnapshot` or `null` for a missing layout. Clone the selected `XmlNode` with `OuterXml`; never return or retain the node.

- [ ] Run the same filtered test command.

Expected: all `InventoryViewDefinitionSourceTests` pass.

- [ ] Commit:

```text
test: characterize Inventory view snapshots
```

### Task 2: Make `DetailComposer` compile effective project layouts

**Files:**

- Modify: `Src/xWorks/Avalonia/Composer/DetailComposer.cs`
- Modify: `Src/xWorks/xWorksTests/Avalonia/Composer/DetailComposerOverrideTests.cs`

- [ ] Replace the old patch-resolver tests with failing integration tests named for the behavior, not the retired mechanism:

  - `Compose_InventoryVisibilityOverrideMatchesLegacyShowHiddenBehavior`
  - `Compose_InventoryReorderOverrideChangesSiblingOrder`
  - `Compose_SecondInventoryDoesNotSeeFirstProjectsOverride`
  - `Compose_PersistedChangeIsVisibleOnNextCompose`

- [ ] In each test, persist a full layout through `Inventory.PersistOverrideElement`; do not instantiate JSON types or call the compiler's internal cache directly.

- [ ] Run the red tests:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~DetailComposerOverrideTests"
```

Expected: at least the visibility/reorder tests fail because composer still reads shipped layouts unless given a JSON patch resolver.

- [ ] Replace `ViewDefinitionOverrideResolver` with a neutral snapshot/source resolver owned by xWorks. Thread it through both `Compose` overloads, `ComposeState`, and nested-object `CompileForObject` calls so descended `LexSense`, `MoForm`, and other layouts use the same project inventory.

- [ ] Remove `CompilerSources.CompiledModels`. Preserve the immutable shipped `LayoutIndex` only as the fallback for callers/tests that do not supply a project source.

- [ ] In `CompileForClass`, use the project source first. If no project inventory is available, preserve current shipped-layout fallback and logging. Pass every snapshot to `ViewDefinitionCompiler.Compile`, allowing its content fingerprint cache to deduplicate identical XML.

- [ ] Preserve `SnapshotCompileCount` semantics by incrementing only when a new source snapshot is constructed, and update memoization tests to assert content reuse rather than the removed identity dictionary.

- [ ] Run:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~DetailComposerOverrideTests|FullyQualifiedName~DetailComposer"
```

Expected: all selected composer tests pass; two inventories with the same class/layout key remain isolated.

- [ ] Commit:

```text
feat: compose Avalonia details from Inventory
```

### Task 3: Wire the product host to the project inventory

**Files:**

- Modify: `Src/xWorks/Avalonia/Hosting/RecordEditView.Avalonia.cs`
- Modify: `Src/xWorks/xWorksTests/Avalonia/Hosting/RecordEditViewSwitchTests.cs`
- Modify: `Src/xWorks/xWorksTests/Avalonia/Hosting/DetailCommandAdapterHardeningTests.cs`

- [ ] Add a failing host test proving Avalonia composition uses `Inventory.GetInventory("layouts", Cache.ProjectId.Name)` after `LayoutCache.InitializePartInventories` loads project overrides.

- [ ] Add a failing switch test: show one record in Avalonia, persist a `.fwlayout` change through the inventory, switch or refresh, and assert the recomposed model reflects it.

- [ ] Remove `m_viewOverrideStore`, `ViewOverrideStore`, and `ResolveViewOverride` from `RecordEditView`.

- [ ] Lazily construct one `InventoryViewDefinitionSource` per project-keyed host. Supply it to both LexEntry and non-LexEntry `DetailComposer.Compose` calls.

- [ ] Fail visibly in the log and use the existing first-slice/unsupported fallback if inventories are unavailable; do not silently read `.viewoverride.json` or bypass the repository's normal inventory initialization.

- [ ] Run:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~RecordEditViewSwitchTests|FullyQualifiedName~DetailCommandAdapterHardeningTests"
```

Expected: selected host tests pass.

- [ ] Commit:

```text
feat: wire Avalonia host to project layouts
```

### Task 4: Route visibility and move commands through legacy writers

**Files:**

- Modify: `Src/xWorks/Avalonia/Hosting/RecordEditView.Avalonia.cs`
- Modify: `Src/xWorks/Avalonia/Hosting/XCoreMenuBridge.cs` if needed for a post-execute callback
- Modify: `Src/xWorks/xWorksTests/Avalonia/Hosting/DetailObjectCommandExecutionTests.cs`
- Modify: `Src/xWorks/xWorksTests/Avalonia/Hosting/DetailCommandAdapterHardeningTests.cs`

- [ ] Add red tests for all five persistent layout commands:

  - `CmdAlwaysVisible`
  - `CmdIfData`
  - `CmdNormallyHidden`
  - `CmdDataTree-MoveFieldUp`
  - `CmdDataTree-MoveFieldDown`

  Each test must execute the native Avalonia menu item, assert the legacy command handler ran, assert a `.fwlayout` element was persisted through `Inventory`, and assert the Avalonia detail model recomposed from that XML.

- [ ] Add a red test that a failed or ambiguous command target clears `CurrentSlice` and writes no layout. Persistent commands must fail closed rather than mutate the first row sharing an object.

- [ ] Strengthen command targeting for persistent layout commands. Match the Avalonia field to the hidden legacy slice using object HVO, field name, layout context, and template occurrence/path. Keep the existing broader object fallback for non-persistent legacy commands, but require an exact unique target before enabling a visibility or move command.

- [ ] Replace `BuildOverrideCommandInterceptor`, `VisibilityItem`, `MoveItem`, `ApplyFieldVisibility`, `ApplyMoveField`, and `MutateOverrideAndRefresh` with a thin command wrapper:

```csharp
choice.OnClick(null, EventArgs.Empty); // existing Slice handler writes Inventory/.fwlayout
RefreshAvaloniaDetail();              // new snapshot sees changed effective XML
```

  Use the normal xCore display properties for label, checked state, and enablement. Do not recalculate these from a second model editor.

- [ ] For the WinForms fallback menu, refresh Avalonia after `ShowContextMenu` returns. A cancel may cause a harmless recompose; command execution must never leave Avalonia stale.

- [ ] Run:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~DetailObjectCommandExecutionTests|FullyQualifiedName~DetailCommandAdapterHardeningTests|FullyQualifiedName~DetailContextMenuCompositionTests"
```

Expected: all selected menu/adapter tests pass and no `.viewoverride.json` file is created.

- [ ] Commit:

```text
feat: share legacy layout command writers
```

### Task 5: Retire the JSON override subsystem

**Files:**

- Delete all production and test files listed in **Retirement inventory**.
- Modify: `Src/Common/FwAvalonia/Detail/DetailModel.cs`
- Modify: `Src/Common/FwAvalonia/FwAvalonia.csproj`
- Modify: `Src/xWorks/Avalonia/Composer/DetailComposer.cs`
- Modify: `Src/xWorks/Avalonia/Hosting/RecordEditView.Avalonia.cs`

- [ ] Delete the JSON patch store, serializer, model/operations, applier, differ, editor, and migration files.

- [ ] Delete their dedicated tests and the xWorks migration adapter/tests.

- [ ] Remove stale XML documentation and comments that claim `DetailField.ClassName`/`LayoutName` key a JSON override store. Retain these properties only if command targeting still needs their layout context; otherwise remove them and their stamping tests.

- [ ] Remove the `Newtonsoft.Json` package from `FwAvalonia.csproj` only if this command proves the production project no longer uses it:

```powershell
rg -n "Newtonsoft\.Json" Src/Common/FwAvalonia -g "*.cs"
```

Expected: if `ViewDefinitionJsonSerializer` still uses Newtonsoft, keep the package.

- [ ] Prove no production or test reference remains:

```powershell
rg -n "ViewDefinitionOverride|ViewOverrideOperation|viewoverride\.json|DetailOverrideMigration" Src
```

Expected: no matches.

- [ ] Prove the project contains no duplicate customization writer:

```powershell
rg -n "PersistOverrideElement|\.fwlayout|ConfigurationSettings" `
  Src/xWorks/Avalonia Src/Common/FwAvalonia -g "*.cs"
```

Expected: Avalonia host references point to `Inventory`/`.fwlayout`; no second extension or serializer appears.

- [ ] Run:

```powershell
.\build.ps1 -CommentHygiene -BuildTests
```

Expected: build succeeds with deleted SDK-globbed files absent.

- [ ] Commit:

```text
refactor: retire Avalonia JSON layout overrides
```

### Task 6: Verify persistence parity end to end

**Files:**

- Create: `Src/xWorks/xWorksTests/Avalonia/Hosting/LayoutPersistenceParityTests.cs`
- Modify only production files exposed by a failing parity test.

- [ ] Add an end-to-end test that begins with no project override, executes Avalonia `Normally hidden`, reloads inventories from disk, and verifies both Avalonia composition and legacy `DataTree` see `visibility="never"`.

- [ ] Add the inverse test: execute legacy `Always visible`, reconstruct Avalonia, and verify it reads the same layout without translation.

- [ ] Add reorder parity in both directions: Avalonia move affects WinForms order after reload; WinForms move affects Avalonia order after recompose.

- [ ] Add a backup/synchronization contract test or static assertion that the only new project artifact is `*.fwlayout`. No `.viewoverride.json` should be present or required.

- [ ] Run:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/xWorks/xWorksTests" `
  -TestFilter "FullyQualifiedName~LayoutPersistenceParityTests"
```

Expected: all two-framework round-trip tests pass.

- [ ] Run the focused FwAvalonia suite after deleting override tests:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/Common/FwAvalonia/FwAvaloniaTests"
```

Expected: all FwAvalonia tests pass.

- [ ] Run the focused xWorks suite:

```powershell
.\test.ps1 -CommentHygiene `
  -TestProject "Src/xWorks/xWorksTests"
```

Expected: all xWorks tests pass, excluding tests already marked `Explicit` by the repository.

- [ ] Commit:

```text
test: prove shared layout persistence parity
```

### Task 7: Full validation and PR preparation

**Files:**

- Modify: this plan only if implementation discoveries require a recorded correction.
- Modify: PR description outside the repository.

- [ ] Run the required full build:

```powershell
.\build.ps1 -CommentHygiene
```

Expected: exit code 0.

- [ ] Run the required full test suite:

```powershell
.\test.ps1 -CommentHygiene
```

Expected: exit code 0 and no failed tests.

- [ ] Verify the retirement and single-store invariants again:

```powershell
rg -n "ViewDefinitionOverride|ViewOverrideOperation|viewoverride\.json|DetailOverrideMigration" Src
git diff --check origin/main...HEAD
gitlint --ignore body-is-missing --commits origin/main..HEAD
```

Expected: `rg` finds nothing; diff and gitlint exit 0.

- [ ] Manually exercise one lexical-entry field in both modes against the same project:

  1. In Avalonia, change visibility and move the field.
  2. Switch to Legacy and confirm both changes.
  3. Close/reopen FieldWorks and confirm both changes.
  4. Change the field back in Legacy.
  5. Switch to Avalonia and confirm the reversal.
  6. Inspect `ConfigurationSettings` and confirm only the relevant `.fwlayout` changed.

- [ ] Update the PR description with:

  - one-store architecture and boundary
  - exact retired files
  - no-migration rationale for unreleased JSON files
  - automated and manual evidence
  - explicit sequencing instructions for #1097 and #1108

- [ ] Push without force and open the implementation PR against `main`.

## Landing criteria

The implementation PR is ready to land only when all are true:

- Both UI frameworks render project overrides from the same effective `Inventory` XML.
- Avalonia visibility and move commands use legacy persistence handlers.
- A change made in either framework appears in the other after refresh/reload.
- No production reference to `.viewoverride.json` remains.
- No automatic JSON migration or destructive JSON cleanup ships.
- Compiler caches are content- and project-safe.
- Focused parity tests, full build, full test suite, comment hygiene, diff check, and gitlint pass.
- #1097 and #1108 are rebased to use the shared store before either lands.

## Explicit non-goals

- Redesigning the `.fwlayout` format or `Inventory` unification rules.
- Moving XCore dependencies into FwAvalonia.
- Adding a third persistence abstraction for hypothetical future Avalonia-only features.
- Migrating unreleased `.viewoverride.json` data.
- Expanding project-level `*Parts.xml` customization.
- Changing backup or Send/Receive filters; using the existing `.fwlayout` artifact removes the need.
