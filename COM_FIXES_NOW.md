# COM Fixes Now

This document narrows the broader audit in `COM_USAGE.md` down to the best near-term COM reduction work that is realistically feasible now.

"Feasible now" means all of the following are true:

- the work does not require rewriting RootBox, Views, FwKernel, Graphite, or the Windows TSF bridge,
- the scope can be bounded to one or a small number of PRs,
- there is a clear validation path using existing build/tests or focused smoke checks,
- and the likely outcome is a real reduction in COM surface area, activation complexity, or COM-shaped application code.

This document is intentionally not a list of all COM cleanups. It is a prioritized shortlist.

## Not In Scope for "Now"

These are real COM dependencies, but they are not good immediate targets:

- replacing the Views/FwKernel ABI,
- removing Graphite,
- replacing the Windows TSF text-store implementation with ordinary WinForms input,
- replacing the full `IPicture` or `IStream` ABI at the native boundary,
- rewriting GenericFactory or the entire reg-free COM story in one pass.

## Recommended Order

| Rank | Fix | Why it belongs in the now list | Expected scope |
| --- | --- | --- | --- |
| 1 | De-COM `ManagedLgIcuCollator` | Very high confidence, direct managed callers already exist, low user risk | Small |
| 2 | Remove or isolate `DebugReport` COM usage in managed code | Debug-only, Windows-only, isolated call path | Small to medium |
| 3 | Retire `ViewInputManager` and `ManagedVwWindow` if Windows-first is official | Looks like non-Windows compatibility scaffolding, not active Windows behavior | Medium, policy-gated |
| 4 | Remove dormant OLE clipboard ownership cleanup | The real clipboard path is already managed; the native OLE cleanup looks like legacy carryover | Small |
| 5 | Introduce a FieldWorks-owned adapter around `SilEncConverters40` | Does not remove COM immediately, but creates the seam needed to do so later | Medium |
| 6 | Prune reg-free build plumbing as each optional COM surface disappears | Necessary follow-through that turns individual removals into real simplification | Small to medium |

## 1. De-COM `ManagedLgIcuCollator`

### Why this is the top fix

This is the cleanest confirmed win in the current tree.

- The class is still marked `ComVisible(true)` in `Src/ManagedLgIcuCollator/LgIcuCollator.cs`.
- In repo-local code, the important callers already instantiate it directly in managed code:
  - `Src/Common/Filters/RecordSorter.cs`
  - `Src/LexText/Lexicon/SortReversalSubEntries.cs`
- The class already has focused managed tests in `Src/ManagedLgIcuCollator/ManagedLgIcuCollatorTests`.
- The build still treats it as a COM assembly in `Build/RegFree.targets` and as an excluded CLSID in `Build/mkall.targets`, so removing the COM surface pays off twice: less runtime surface and less build plumbing.

This is exactly the kind of change we want: a managed implementation that no longer appears to need COM at all inside this repo.

### What success looks like

- `ManagedLgIcuCollator` is no longer COM-visible.
- No reg-free manifest or excluded-CLSID plumbing remains for it.
- Callers continue to construct it directly as a normal managed class.

### Concrete scope

Primary files to inspect or change:

- `Src/ManagedLgIcuCollator/LgIcuCollator.cs`
- `Src/Common/Filters/RecordSorter.cs`
- `Src/LexText/Lexicon/SortReversalSubEntries.cs`
- `Build/RegFree.targets`
- `Build/mkall.targets`

Potential follow-up audit only:

- installer/build inclusion files if this assembly is still packaged solely because of COM activation expectations.

### Detailed plan

1. Confirm there is no repo-local CLSID/ProgID activation path for `ManagedLgIcuCollator`.
2. Remove `ComVisible(true)` from `ManagedLgIcuCollator`.
3. Leave the managed type public and directly constructible.
4. Remove `ManagedLgIcuCollator.dll` from the managed COM manifest list in `Build/RegFree.targets`.
5. Remove the collator CLSID from `Build/mkall.targets` `ExcludedClsids`.
6. Rebuild/regenerate manifests and verify no stray references remain.
7. Run focused managed tests and a sorting smoke test through the existing callers.

### Validation plan

- Run the managed collator unit tests.
- Run the affected managed tests that exercise sorting callers.
- Run a narrow build or manifest-generation check to ensure the removed CLSID is not still emitted.

### Risks

- The main risk is out-of-repo automation or legacy tooling depending on the CLSID.
- That risk is not visible in this repository, so the change should be staged with a clear release note or compatibility note if external tools exist.

### Alternate paths

Preferred alternate if compatibility concerns remain:

- stop shipping it in reg-free manifests first,
- keep the class temporarily public and decorated only as long as external compatibility is still being audited,
- then remove COM visibility in a second step.

Conservative alternate:

- keep the class COM-visible, but formalize a rule that all in-repo callers must use direct construction only.

## 2. Remove or Isolate `DebugReport` COM Usage in Managed Code

### Why this belongs in the now list

`Src/Common/FwUtils/DebugProcs.cs` is the clearest managed COM activation path still in product code that is both:

- Windows-only,
- and debug-only.

The current code explicitly creates a COM object by CLSID through `Type.GetTypeFromCLSID` and `Activator.CreateInstance`. That means it is still paying COM activation cost and manifest/build coordination for a non-production feature.

This is good cleanup terrain because it is isolated and low consequence compared with runtime editing or rendering.

### What success looks like

One of the following becomes true:

- managed debug reporting no longer relies on COM activation at all, or
- COM remains only as an explicitly accepted fallback inside one tiny shim, not as a first-class dependency path.

### Concrete scope

Primary files:

- `Src/Common/FwUtils/DebugProcs.cs`
- `Src/DebugProcs/DebugProcs.cpp`
- `Src/DebugProcs/DebugProcs.h`
- generated debug-report interfaces in `Src/Common/ViewsInterfaces/Views.cs`

Potential build follow-through:

- any manifest/build plumbing that exists only to surface `DebugReport` to managed code.

### Detailed plan

1. Introduce a tiny managed abstraction for debug-report transport inside `DebugProcs.cs`.
2. Implement a no-op/default path for non-Windows or when native debug support is unavailable.
3. Evaluate whether the existing native debug DLL exports are enough to support a non-COM bridge.
4. If they are, switch `DebugProcs.cs` to the non-COM bridge.
5. If they are not, keep COM only behind the abstraction and treat that as intentionally isolated debug-only debt.
6. Remove any no-longer-needed manifest/build references if COM activation is dropped.

### Validation plan

- Debug build only.
- Smoke test that native assertions and debug output still surface during development.
- Confirm release builds are unchanged.

### Risks

- The native debug library may assume a COM sink shape more deeply than the managed code suggests.
- If so, a full replacement may not be worth it now.

### Alternate paths

Best alternate if full removal is not worth it:

- do not remove COM here yet,
- but confine it to one implementation class and explicitly document it as accepted debug-only interop.

That still improves the architecture because COM stops leaking into general managed code.

## 3. Retire `ViewInputManager` and `ManagedVwWindow` if Windows-First Is Official

### Why this belongs in the now list

These two classes look like compatibility shims for a non-Windows path rather than active Windows behavior:

- `Src/Common/SimpleRootSite/ViewInputManager.cs` explicitly says Windows uses unmanaged `VwTextStore`.
- `Src/views/VwRootBox.cpp` creates `VwTextStore` on Windows and `ViewInputManager` through CLSID on the alternate path.
- `Src/ManagedVwWindow/ManagedVwWindow.cs` is the managed window wrapper, and the corresponding native references point at non-Windows logic rather than the active Windows path.

If FieldWorks is now Windows/x64-only as an operational decision, these are likely removable.

### What success looks like

- `ViewInputManager` and `ManagedVwWindow` are no longer shipped as COM surfaces.
- The active Windows text-input path remains `VwTextStore`.
- The build no longer carries CLSIDs/manifests for these compatibility types.

### Concrete scope

Primary files:

- `Src/Common/SimpleRootSite/ViewInputManager.cs`
- `Src/ManagedVwWindow/ManagedVwWindow.cs`
- `Src/views/VwRootBox.cpp`
- `Src/views/VwSelection.cpp`
- `Build/RegFree.targets`
- `Build/mkall.targets`

### Detailed plan

1. Make the product/platform decision explicit: are these paths still supported or not?
2. If the answer is Windows-only, remove the alternate creation path in native code and keep the Windows `VwTextStore` path untouched.
3. Remove `ComVisible(true)` and any assembly-level reg-free packaging for these managed classes.
4. Remove related CLSIDs from `ExcludedClsids` and managed COM assembly lists.
5. Delete the managed classes if they are truly dead, or keep them source-only behind a temporary compatibility guard if needed.

### Validation plan

- Run focused RootSite or Views smoke tests on Windows.
- Confirm focus, selection, IME, and composition behavior are unchanged on the Windows path.
- Confirm manifests no longer include the removed CLSIDs.

### Risks

- This is not purely technical. It depends on product support policy.
- If the repo still wants to preserve dormant non-Windows paths for future revival, deleting the code may be politically or strategically premature.

### Alternate paths

If Windows-only is not yet a formal decision:

- stop shipping these classes in production manifests first,
- keep the source but isolate it behind a build property or compatibility symbol,
- and treat complete removal as a second-stage cleanup.

## 4. Remove Dormant OLE Clipboard Ownership Cleanup

### Why this belongs in the now list

The live clipboard behavior in FieldWorks is already managed:

- `Src/Common/SimpleRootSite/EditingHelper.cs` creates and consumes managed clipboard payloads,
- `Src/Common/FwUtils/ClipboardUtils.cs` is the real managed wrapper,
- `Src/Common/SimpleRootSite/TsStringWrapper.cs` handles the serialized payload.

By contrast, `Src/Generic/ModuleEntry.cpp` still tracks an `IDataObject` and calls `OleIsCurrentClipboard` / `OleFlushClipboard` during shutdown. That looks like legacy ownership cleanup rather than the path the application actually depends on.

This is attractive because it is small, testable, and outside the core rendering/data ABI.

### What success looks like

- Native module shutdown no longer carries clipboard ownership bookkeeping that the application no longer needs.
- Unused managed P/Invoke declarations for those APIs are removed if they are truly dead.

### Concrete scope

Primary files:

- `Src/Generic/ModuleEntry.cpp`
- `Src/Generic/ModuleEntry.h`
- `Src/Common/FwUtils/Win32Wrappers.cs`

Related verification only:

- clipboard tests around `EditingHelper` and `SimpleRootSite`.

### Detailed plan

1. Confirm `ModuleEntry::SetClipboard` is no longer used by live product code in any meaningful clipboard flow.
2. Remove the stored `IDataObject` ownership tracking from `ModuleEntry`.
3. Remove shutdown calls to `OleIsCurrentClipboard` and `OleFlushClipboard`.
4. Delete unused managed declarations in `Win32Wrappers.cs` if they are not referenced.
5. Keep the managed clipboard path unchanged.

### Validation plan

- Run existing clipboard-related managed tests.
- Manual smoke test: copy/paste FieldWorks text inside the app and into/out of other applications.
- Verify shutdown is unaffected.

### Risks

- The main risk is an old native path still using `ModuleEntry::SetClipboard` indirectly.
- That should be resolved with one more targeted usage audit before code deletion.

### Alternate paths

Conservative alternate:

- keep the native cleanup in place,
- but mark it deprecated and add a guardrail that no new code may depend on `ModuleEntry::SetClipboard`.

## 5. Introduce a FieldWorks-Owned Adapter Around `SilEncConverters40`

### Why this belongs in the now list

This does not remove COM immediately, but it is the most realistic first move against one of the largest remaining external COM islands.

Current product code still constructs or consumes `EncConverters` directly in places such as:

- `Src/ParatextImport/SCTextEnum.cs`
- `Src/FwCoreDlgs/FwWritingSystemSetupModel.cs`
- `Src/FwCoreDlgs/ConverterTester.cs`

Right now there is no clear seam between FieldWorks logic and the external COM library. Until that seam exists, replacing or shrinking the dependency later will remain expensive.

### What success looks like

- Product code depends on a FieldWorks-owned interface, not directly on `EncConverters`.
- Direct `new EncConverters()` creation is centralized.
- Existing tests can mock the abstraction cleanly.

### Concrete scope

Primary files:

- `Src/ParatextImport/SCTextEnum.cs`
- `Src/FwCoreDlgs/FwWritingSystemSetupModel.cs`
- `Src/FwCoreDlgs/ConverterTester.cs`
- other direct product call sites found during implementation

New code likely needed:

- a small FieldWorks abstraction interface and one production adapter implementation.

### Detailed plan

1. Define the minimum interface FieldWorks actually needs from encoding-converter lookup and execution.
2. Implement a production adapter that wraps `SilEncConverters40`.
3. Replace direct `new EncConverters()` and direct product-level indexing/casting with the new abstraction.
4. Reuse or extend existing mocks in tests where possible.
5. Once all direct product call sites are behind the adapter, reassess whether some workflows can use a managed replacement or a simpler built-in path.

### Validation plan

- Run Paratext import tests and any dialog-model tests that already mock converter behavior.
- Manual smoke test for converter selection/configuration flows.

### Risks

- This touches user-facing import/configuration workflows.
- The work is still bounded, but it should be split by feature area rather than done in one large sweep.

### Alternate paths

Narrower alternate if time is tight:

- start with Paratext import only,
- or centralize `EncConverters` creation in one factory first without introducing the full abstraction everywhere.

That still buys a useful seam.

## 6. Prune Reg-Free Build Plumbing as Each Optional COM Surface Disappears

### Why this belongs in the now list

This is the mandatory companion to every successful COM surface reduction.

Right now the build carries explicit COM plumbing for optional managed surfaces:

- managed COM assembly entries in `Build/RegFree.targets`,
- excluded CLSIDs in `Build/mkall.targets`,
- CLSID filtering logic in `Build/Src/FwBuildTasks/RegFreeCreator.cs` and `Build/Src/FwBuildTasks/RegFree.cs`.

If we remove a COM surface but leave this plumbing behind, the code gets cleaner but the build does not.

### What success looks like

- every optional COM surface removal also deletes its manifest/build baggage,
- the `ExcludedClsids` list only contains genuinely required entries,
- and manifest generation becomes easier to reason about.

### Concrete scope

Primary files:

- `Build/RegFree.targets`
- `Build/mkall.targets`
- `Build/Src/FwBuildTasks/RegFreeCreator.cs`
- `Build/Src/FwBuildTasks/RegFree.cs`

### Detailed plan

1. Treat manifest/build cleanup as part of every COM-removal PR, not a later chore.
2. For each removed managed COM class, delete:
   - the managed COM assembly entry,
   - the related excluded CLSID,
   - and any tests or helper data that exist only for that surface.
3. Add a simple validation rule or test that fails if a removed CLSID still appears in manifest inputs.
4. Once a few removals land, consider moving `ExcludedClsids` to a single data source instead of hardcoding them in multiple places.

### Validation plan

- Run a focused build/reg-free generation check after each change.
- Diff generated manifests and verify the removed classes are gone while required native COM remains.

### Risks

- The main risk is deleting a CLSID from the wrong list and breaking native/managed manifest coordination.
- That is why this work should be coupled tightly to specific removals and validated immediately.

### Alternate paths

Conservative alternate:

- if build refactoring is too much for a feature PR, at least remove the class-specific manifest entries and CLSIDs immediately,
- then leave broader `RegFreeCreator` cleanup for a dedicated follow-up.

## Strong Alternates If Priorities Shift

These did not make the top six, but they are strong next candidates.

### A. Centralize Picture Creation and Stop Growing `IPicture` Debt

Why it almost made the list:

- there is already a managed picture path in the repo,
- Windows code still mixes AxHost conversion, `IPicture`, and `OleLoadPicture` usage,
- and that mix makes picture COM harder to retire later.

Why it did not rank higher:

- it is more user-visible and rendering-adjacent than the top items,
- and it is best handled after the easiest wins above.

### B. Narrow Managed Use of `UnknownProp`

Why it almost made the list:

- this is one of the best ways to reduce COM-shaped API usage inside managed code without rewriting Views.

Why it did not rank higher:

- it is broader and less self-contained than the top six,
- and it is better done after the easiest surface removals so the team has a cleaner baseline.

### C. Remove or Disable `setKeysInHKCU` Developer Registry Writes

Why it almost made the list:

- `Build/mkall.targets` already comments that `setRegistryValues` / `setKeysInHKCU` may be unused in the modernized build flow.

Why it did not rank higher:

- it is COM-adjacent build debt, not directly COM-surface reduction,
- and it should follow confirmation that no developer workflows still depend on it.

## Practical Sequencing

Recommended implementation order:

1. `ManagedLgIcuCollator`
2. `DebugReport` cleanup or isolation
3. Clipboard OLE cleanup
4. Windows-first shim retirement if policy allows it
5. `SilEncConverters40` adapter work
6. Build/reg-free cleanup as a required companion to each of the above

## Final Recommendation

If only one fix is done now, do `ManagedLgIcuCollator`.

If two or three are done, add `DebugReport` cleanup and the clipboard OLE cleanup.

If the team wants a slightly larger but still disciplined COM-reduction initiative, pair those with:

- the Windows-first removal of `ViewInputManager` and `ManagedVwWindow`, and
- the `SilEncConverters40` adapter seam.

That combination reduces real COM surface area, lowers manifest/build complexity, and improves the C# side of the codebase without touching Graphite, RootBox, or other architectural bedrock.
