## 1. Clarify Alternatives Before Implementation

- [x] 1.1 Assess repo-local evidence for `ManagedLgIcuCollator` CLSID `{e771361c-ff54-4120-9525-98a0b7a9accf}`; treat it as repo-internal unless out-of-repo compatibility sign-off finds an exception. [Managed/build, decision]
- [x] 1.2 Decide whether FieldWorks is source-level Windows-only for `ViewInputManager` and `ManagedVwWindow`, or whether dormant non-Windows paths must remain buildable. [Native/managed, decision]
- [x] 1.3 Choose the DebugProcs path: isolate COM behind a debug-only shim, or attempt a non-COM replacement using existing native exports. [Managed/native, decision]
- [x] 1.4 Select the first `SilEncConverters40` adapter slice: Paratext import, writing-system setup, converter tester, bulk edit, or interlinear import. [Managed, decision]
- [x] 1.5 Confirm that picture creation, `VwDrawRootBuffered`, and `UnknownProp` narrowing are optional follow-ups rather than required implementation work for this change. [Architecture, decision]

## 2. Baseline Inventory and Characterization

- [x] 2.1 Snapshot current optional COM class references in `Build/RegFree.targets`, `Build/mkall.targets`, `Src/Common/FieldWorks/BuildInclude.targets`, and generated manifests. [Build, <=2h]
- [x] 2.2 Add or identify a `RegFreeCreatorTests` case proving `[ComVisible(false)]` classes do not emit `clrClass` manifest entries. [Managed build task, <=2h]
- [x] 2.3 Add or identify a `RegFreeCreatorTests` case proving targeted removed CLSIDs are absent from generated manifest output. [Managed build task, <=2h]
- [x] 2.4 Confirm `ModuleEntry::SetClipboard` has no live callers outside its declaration/definition before clipboard cleanup. [Native audit, <=1h]
- [x] 2.5 Confirm direct `new EncConverters()` and `Type.GetTypeFromCLSID` call sites before adapter/debug cleanup. [Managed audit, <=1h]
- [x] 2.6 Document `origin/main` git provenance, PR, and Jira context for `ManagedLgIcuCollator` COM exposure and dormant OLE clipboard cleanup. [History audit]

## 3. Required: De-COM `ManagedLgIcuCollator`

- [x] 3.1 Remove `ComVisible(true)` and COM-only attributes from `Src/ManagedLgIcuCollator/LgIcuCollator.cs` if task 1.1 confirms no external CLSID contract. [Managed C#, <=1h]
- [x] 3.2 Remove `ManagedLgIcuCollator.dll` from managed COM manifest inputs in `Build/RegFree.targets` and `Src/Common/FieldWorks/BuildInclude.targets`. [Build, <=1h]
- [x] 3.3 Remove CLSID `{e771361c-ff54-4120-9525-98a0b7a9accf}` from `Build/mkall.targets` `ExcludedClsids`. [Build, <=30m]
- [x] 3.4 Verify direct managed callers in `Src/Common/Filters/RecordSorter.cs` and `Src/LexText/Lexicon/SortReversalSubEntries.cs` still compile and need no behavior change. [Managed C#, <=1h]
- [x] 3.5 Run `.\test.ps1 -TestProject ManagedLgIcuCollatorTests`. [Validation]
- [x] 3.6 Run caller/build validation: `.\test.ps1 -TestProject FiltersTests`, `.\test.ps1 -TestProject LexEdDllTests`, and `.\test.ps1 -TestProject FwBuildTasksTests -TestFilter "FullyQualifiedName~RegFreeCreator"`. [Validation]
- [ ] 3.7 Before merge, confirm there is no known out-of-repo automation or extension that depends on the `ManagedLgIcuCollator` CLSID. [Compatibility sign-off]

## 4. Required Companion: Reg-Free Manifest Cleanup Discipline

- [x] 4.1 For each removed optional COM class, update all manifest/build inputs in the same PR: `Build/RegFree.targets`, `Build/mkall.targets`, and `Src/Common/FieldWorks/BuildInclude.targets` where applicable. [Build, ongoing]
- [x] 4.2 Add a test assertion that the removed `ManagedLgIcuCollator` CLSID no longer appears in generated `clrClass` entries. [Build/test, <=2h]
- [x] 4.3 Clean generated-manifest scan after removing stale ignored manifest outputs; removed `ManagedLgIcuCollator` CLSID now appears only in docs/tests, not generated manifests. [Validation]
- [x] 4.4 Run `.\build.ps1` or the narrow approved build target needed to regenerate and validate reg-free manifests. [Validation]

## 5. Required If Characterization Confirms: Remove Dormant OLE Clipboard Cleanup

- [x] 5.1 Remove `ModuleEntry::SetClipboard` ownership state and shutdown `OleIsCurrentClipboard` / `OleFlushClipboard` cleanup from `Src/Generic/ModuleEntry.cpp` and `Src/Generic/ModuleEntry.h`. [Native C++, <=2h]
- [x] 5.2 Remove unused managed declarations in `Src/Common/FwUtils/Win32Wrappers.cs` only if no references remain. [Managed C#, <=1h]
- [x] 5.3 Keep TSF `IDataObject` methods and managed clipboard behavior unchanged. [Review gate]
- [x] 5.4 Run `.\test.ps1 -TestProject SimpleRootSiteTests -TestFilter "FullyQualifiedName~EditingHelperTests"` and `.\test.ps1 -SkipManaged -TestProject TestGeneric`. [Validation]
- [ ] 5.5 Manual smoke pending: copy styled multilingual FieldWorks text within FieldWorks, paste to Notepad/Word, exit FieldWorks, then verify clipboard data remains pasteable. [Manual validation]

## 6. Required First Slice: Isolate DebugProcs COM Activation

- [x] 6.1 Add tests around `DebugProcs` construction/disposal failure tolerance and idempotent disposal without requiring real COM activation. [Managed test, <=2h]
- [x] 6.2 Introduce a tiny debug-report transport abstraction in `Src/Common/FwUtils/DebugProcs.cs`. [Managed C#, <=2h]
- [x] 6.3 Move current CLSID activation behind that abstraction as the safe default implementation. [Managed C#, <=1h]
- [x] 6.4 Not selected for this change: non-COM debug transport replacement is deferred; COM remains isolated behind the debug-only transport. [Optional, managed/native]
- [x] 6.5 Run `.\test.ps1 -TestProject FwUtilsTests -TestFilter "FullyQualifiedName~DebugProcs"`; native debug code was not changed, so `TestViews` is not required for this slice. [Validation]

## 7. Required Encoding Converter Provider Crossover

- [x] 7.1 Define a FieldWorks-owned `IEncodingConvertersProvider` seam in `Src/Common/FwUtils/`. [Managed C#, <=2h]
- [x] 7.2 Implement the production provider using existing `SilEncConverters40` / `encoding-converters-core`; do not add or replace runtime packages. [Managed C#, <=2h]
- [x] 7.3 Replace product-level direct `new EncConverters()` construction with `EncodingConvertersProvider` across Paratext import, FwCore dialogs, XMLViews bulk edit, LexText import, Interlinear import, Data Notebook import, and Sfm2Xml. [Managed C#]
- [x] 7.4 Preserve legacy configuration workflows by exposing the existing repository only through the provider where Add/Remove/AutoConfigure APIs are still required. [Managed C#]
- [x] 7.5 Add/update mocked provider tests using existing Moq/NUnit patterns. [Managed test]
- [x] 7.6 Run focused validation: `.\test.ps1 -TestProject ParatextImportTests`, `.\test.ps1 -TestProject FwCoreDlgsTests`, and `.\test.ps1 -TestProject Sfm2XmlTests`. [Validation]

## 8. Deferred Optional / Risky: Windows-First Shim Removal

- [ ] 8.1 Optional: If task 1.2 confirms source-level Windows-only support, remove `ViewInputManager` COM activation and source/build/manifest entries. [Optional, native/managed/build]
- [ ] 8.2 Optional: If task 1.2 confirms source-level Windows-only support, remove `ManagedVwWindow` source/build/manifest entries. [Optional, native/managed/build]
- [ ] 8.3 Optional validation: run `.\test.ps1 -SkipManaged -TestProject TestViews`; while the project still exists, run `.\test.ps1 -TestProject ManagedVwWindowTests`. [Optional validation]
- [ ] 8.4 Manual smoke: edit text in a RootSite field, move selection, use PageUp/PageDown, and test IME/composition if available to verify Windows remains on `VwTextStore`. [Manual validation]

## 9. Deferred Optional / Risky Follow-Ups to Keep Out of First PRs

- [ ] 9.1 Optional: Centralize picture creation/lifetime rules without removing the `IPicture` ABI. [Optional, rendering-adjacent]
- [ ] 9.2 Optional: Evaluate `ManagedVwDrawRootBuffered` COM visibility only after render parity validation; do not flip rendering defaults in this change. [Optional, rendering]
- [ ] 9.3 Optional: Add typed managed wrappers for common `UnknownProp` uses, but do not alter native cache/undo semantics. [Optional, data boundary]
- [ ] 9.4 Optional: Audit `LexTextApp` COM visibility for out-of-repo automation before any removal. [Optional, external compatibility]

## 10. Final Verification

- [x] 10.1 Run the narrow tests listed in completed task groups.
- [x] 10.2 Run `.\build.ps1` when build/manifest inputs change.
- [x] 10.3 Run `.\build.ps1 -BuildTests` or `.\test.ps1` for broader validation before merge if multiple slices land together.
- [x] 10.4 Run `.\Build\Agent\check-and-fix-whitespace.ps1` through the VS Code `CI: Whitespace check` task before committing.
