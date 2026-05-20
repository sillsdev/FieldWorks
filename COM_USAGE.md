# COM Usage in FieldWorks

This document is a repo-specific audit of COM usage in FieldWorks as of 2026-05-19.

It groups COM usage into ten families and answers four practical questions for each one:

1. What is COM doing here?
2. Is it still justified?
3. Can we remove it easily?
4. Is there a realistic cleaner C#-first path?

Graphite is treated as blocked by constraint and is not proposed for removal here.

## Executive Summary

FieldWorks still has one large irreducible COM core: the native Views/FwKernel ABI that drives layout, editing, selection, and much of the rendering pipeline. That is not a surface-level dependency. Removing it would amount to replacing the editor and layout engine.

The good news is that not all COM usage is equally entrenched. Several surfaces are already managed implementations packaged behind old COM contracts, some UI-edge OLE code is dormant or replaceable, and a meaningful part of the build/deployment complexity exists only to preserve optional COM-visible entry points.

The highest-value near-term work is not "remove COM everywhere." It is:

- keep COM at the true native boundary,
- stop expanding COM-shaped APIs in new C# code,
- remove managed COM shims that no longer need activation,
- isolate or replace optional Windows/OLE helpers,
- and prune build/reg-free plumbing as those surfaces shrink.

## What Counted and What Did Not

Counted as COM usage:

- internal FieldWorks COM interfaces and coclasses in the Views/FwKernel ecosystem,
- Windows COM and OLE interfaces such as TSF, `IPicture`, `IAccessible`, `IOleServiceProvider`, and `IStream`,
- managed classes exported with `ComVisible(true)` or activated by CLSID/ProgID,
- registration-free COM manifest generation and native factory/registration support.

Explicitly not counted as meaningful COM usage:

- plain Win32 P/Invoke and generic `Marshal.*` calls that are not crossing a COM boundary,
- WinForms file dialogs in `Src/Common/Controls/FwControls/FileDialog/Windows`, which are managed wrappers around `OpenFileDialog`, `SaveFileDialog`, and `FolderBrowserDialog`,
- metadata fields named `ClsId` in caches that refer to FieldWorks class IDs rather than COM activation.

## Group Summary

| # | Group | What COM is doing | Example files | Importance | Removal path | Better solution / C# path |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Core Views/RootBox ABI | Native layout/editing engine contract between C++ and managed hosts | `Src/Common/ViewsInterfaces/Views.cs`, `Src/Common/SimpleRootSite/SimpleRootSite.cs`, `Src/views/VwRootBox.cpp` | Dominant | Blocked | No realistic C#-only path without replacing the editor/layout engine |
| 2 | Rendering and shaping engines | Graphite, Uniscribe, line breaking, text source, graphics objects | `Src/Common/SimpleRootSite/RenderEngineFactory.cs`, `Src/views/lib/GraphiteEngine.cpp`, `Src/views/lib/UniscribeEngine.cpp`, `Src/views/lib/LgLineBreaker.cpp` | Dominant | Graphite blocked; rest long-term | Managed selection/policy can improve, but engine layer is still native |
| 3 | Data access, cache, undo, and TsString-style object transport | Views-facing data ABI, cache notifications, `IUnknown` property transport, undo/sync | `Src/CacheLight/RealDataCache.cs`, `Src/views/lib/VwCacheDa.cpp`, `Src/views/lib/VwUndo.cpp`, `Src/Common/RootSite/CollectorEnv.cs` | Dominant | Incremental at the boundary; core ABI long-term | Push more C# code toward managed adapters, typed wrappers, and `ISilDataAccessManaged` |
| 4 | Managed implementations still exposed via COM | Managed code kept COM-visible for native compatibility or old activation patterns | `Src/ManagedLgIcuCollator/LgIcuCollator.cs`, `Src/ManagedVwWindow/ManagedVwWindow.cs`, `Src/ManagedVwDrawRootBuffered/VwDrawRootBuffered.cs`, `Src/Common/FwUtils/ManagedPictureFactory.cs`, `Src/Common/SimpleRootSite/ViewInputManager.cs`, `Src/LexText/LexTextDll/LexTextApp.cs` | Medium | Easy to incremental, depending on class | Several can move to direct construction or be removed entirely in a Windows-first repo |
| 5 | Windows text input and accessibility | TSF text-store integration, MSAA/service-provider bridge | `Src/views/VwTextStore.cpp`, `Src/views/VwAccessRoot.cpp`, `Src/Common/SimpleRootSite/SimpleRootSite.cs` | Medium, behavior-critical | TSF blocked/long-term; accessibility long-term | Keep as Windows-specific interop for now; reduce dead surface first |
| 6 | Clipboard, OLE picture, and media wrappers | Copy/paste glue, `IPicture`, OLE loading, picture lifetime management | `Src/Common/SimpleRootSite/EditingHelper.cs`, `Src/Common/FwUtils/PictureHolder.cs`, `Src/Common/ViewsInterfaces/IPicture.cs`, `Src/views/lib/VwGraphics.cpp` | Medium | Easy to incremental | Managed clipboard is already dominant; picture creation can move toward a single managed path |
| 7 | External legacy COM integrations | SIL Encoding Converters and related registry/ProgID lookups | `Src/ParatextImport/SCTextEnum.cs`, `Src/FwCoreDlgs/FwWritingSystemSetupModel.cs`, `Src/FwCoreDlgs/ConverterTester.cs`, `Src/ProjectUnpacker/RegistryData.cs` | Medium | Long-term | Wrap in a FieldWorks-owned adapter first; replacement comes later |
| 8 | Native COM stream and persistence helpers | `IStream` used as an internal persistence and transport abstraction | `Src/Generic/FileStrm.h`, `Src/Generic/ResourceStrm.h`, `Src/Generic/UtilPersist.h`, `Src/views/lib/TsString.cpp` | Medium | Incremental | Introduce non-COM byte-stream helpers for new/native code, then migrate gradually |
| 9 | Registration-free COM build and deployment plumbing | Manifest generation, CLSID/ProgID discovery, activation context setup | `Build/Src/FwBuildTasks/RegFreeCreator.cs`, `Build/RegFree.targets`, `Src/Common/FwUtils/ActivationContextHelper.cs` | Medium | Incremental | Shrinks as optional COM-visible surfaces are removed |
| 10 | Native factory registration and debug-only COM | Generic class factory support, leftover ProgID registry logic, debug sink activation | `Src/Generic/GenericFactory.cpp`, `Src/Generic/ComSmartPtr.h`, `Src/Common/FwUtils/DebugProcs.cs` | Minor to medium | Easy to long-term depending on piece | Debug COM is easy to replace; factory/ProgID cleanup is incremental |

## Group Details

### 1. Core Views/RootBox ABI

What it is:

- The central native/managed boundary for display, layout, editing, selection, and view construction.
- Managed code hosts a native root box and implements callback interfaces that the native engine expects.

Representative files:

- `Src/Common/ViewsInterfaces/Views.cs`
- `Src/Common/SimpleRootSite/SimpleRootSite.cs`
- `Src/Common/RootSite/RootSite.cs`
- `Src/views/VwRootBox.cpp`
- `Src/views/VwSelection.cpp`

Why it exists:

- The native Views engine is the editor and layout engine. COM is the ABI that lets C++ Views objects, managed root sites, and data access layers talk to each other.

Assessment:

- This is architectural bedrock.
- It is not a convenience wrapper that can be peeled away locally.
- A C#-only path would mean replacing RootBox and the surrounding native layout/editing stack.

Removal path:

- `blocked`

Better path:

- Keep COM isolated here and stop letting C# application code depend directly on more of the raw COM surface than necessary.

### 2. Rendering and Shaping Engines

What it is:

- COM-visible render engines and related text services: Graphite, Uniscribe, line breaker, text source, graphics, print, and related rendering objects.

Representative files:

- `Src/Common/SimpleRootSite/RenderEngineFactory.cs`
- `Src/views/lib/GraphiteEngine.cpp`
- `Src/views/lib/UniscribeEngine.cpp`
- `Src/views/lib/LgLineBreaker.cpp`
- `Src/views/VwTxtSrc.cpp`

Why it exists:

- Views expects render engines and text services through the historical COM ABI.
- The selection and caching policy is partly managed, but the engines themselves are still native.

Assessment:

- Graphite is explicitly blocked.
- Even apart from Graphite, the engine layer is still tightly coupled to the native segment and layout pipeline.

Removal path:

- Graphite: `blocked`
- Uniscribe/line-break/text-source layer: `long-term`

Better path:

- Keep render-engine selection in managed code.
- Add a non-COM provider abstraction on the C# side so application logic depends on a service/factory, not on COM activation details.

### 3. Data Access, Cache, Undo, and TsString-Style Object Transport

What it is:

- Views-facing data access and cache contracts such as `ISilDataAccess` and `IVwCacheDa`.
- Undo/synchronizer plumbing.
- `IUnknown` property transport such as `CacheUnknown`, `SetUnknown`, and `get_UnknownProp`.

Representative files:

- `Src/CacheLight/RealDataCache.cs`
- `Src/views/lib/VwCacheDa.cpp`
- `Src/views/lib/VwBaseDataAccess.cpp`
- `Src/views/lib/VwUndo.cpp`
- `Src/Common/RootSite/CollectorEnv.cs`
- `Src/Common/SimpleRootSite/EditingHelper.cs`

Why it exists:

- Views only understands its data and notification ABI through these interfaces.
- Some properties, especially paragraph style props, still move through an `IUnknown`-shaped channel.

Assessment:

- The ABI is old, but managed implementations already exist and some managed code already unwraps or prefers managed SDA paths.
- In practice, `UnknownProp` usage is narrower than the interface suggests; common usage is `ITsTextProps`, not arbitrary object transport.

Removal path:

- Core ABI: `long-term`
- Managed-side narrowing and wrapper cleanup: `incremental`

Better path:

- Use `ISilDataAccessManaged` or equivalent managed adapters by default in new C# code.
- Add typed managed APIs for the common `UnknownProp` cases so managed code stops relying on the generic COM-shaped property channel.

### 4. Managed Implementations Still Exposed via COM

What it is:

- Managed classes that implement old COM contracts or remain `ComVisible(true)` for historical/native compatibility.

Representative files:

- `Src/ManagedLgIcuCollator/LgIcuCollator.cs`
- `Src/ManagedVwWindow/ManagedVwWindow.cs`
- `Src/ManagedVwDrawRootBuffered/VwDrawRootBuffered.cs`
- `Src/Common/FwUtils/ManagedPictureFactory.cs`
- `Src/Common/SimpleRootSite/ViewInputManager.cs`
- `Src/LexText/LexTextDll/LexTextApp.cs`

Assessment:

- Not all of these are equally live.
- `ManagedLgIcuCollator` already has direct managed callers and appears to no longer need repo-local COM activation.
- `ViewInputManager`, `ManagedVwWindow`, and `ManagedPictureFactory` are strongly tied to non-Windows or legacy paths and look removable in a Windows-first repo after a targeted audit.
- `ManagedVwDrawRootBuffered` is a real managed implementation, but the active Windows path still prefers the native class.
- `LexTextApp` looks low-risk internally but should be audited for any out-of-repo automation assumptions before removing COM visibility.

Removal path:

- `ManagedLgIcuCollator`: `easy`
- `ViewInputManager`, `ManagedVwWindow`, `ManagedPictureFactory`: `easy` to `incremental`
- `ManagedVwDrawRootBuffered`, `LexTextApp`: `incremental`

Better path:

- Replace COM activation with direct construction or factory/DI where both sides are managed.
- Remove `ComVisible` and manifest entries as each class stops being needed by native callers.

### 5. Windows Text Input and Accessibility

What it is:

- TSF integration through `VwTextStore`.
- MSAA/service-provider exposure through `IAccessible` and `IOleServiceProvider`.

Representative files:

- `Src/views/VwTextStore.cpp`
- `Src/views/VwAccessRoot.cpp`
- `Src/views/VwAccessRoot.h`
- `Src/Common/SimpleRootSite/SimpleRootSite.cs`
- `Src/Common/SimpleRootSite/AccessibilityWrapper.cs`

Assessment:

- This is active Windows behavior, not dead legacy decoration.
- The text surface is a custom editor, not a stock WinForms text box, so TSF cannot be dropped in favor of ordinary managed input APIs without rebuilding part of the input layer.
- The accessibility path is also tied to the native box model and service-provider bridge.
- Some TSF surface is already effectively dead or stubbed and can be documented or trimmed separately.

Removal path:

- TSF core: `blocked` to `long-term`
- Accessibility/service-provider redesign: `long-term`

Better path:

- Treat this as Windows-specific interop that can be narrowed, not as the first target for COM removal.
- If any cleanup happens here, start by shrinking dead surface, not by rewriting the text-input bridge.

### 6. Clipboard, OLE Picture, and Media Wrappers

What it is:

- Managed clipboard packaging and recovery.
- `IPicture` usage, OLE picture loading, and picture lifetime management across managed/native boundaries.

Representative files:

- `Src/Common/SimpleRootSite/EditingHelper.cs`
- `Src/Common/SimpleRootSite/TsStringWrapper.cs`
- `Src/Common/FwUtils/PictureHolder.cs`
- `Src/Common/FwUtils/OLEConvert.cs`
- `Src/Common/ViewsInterfaces/IPicture.cs`
- `Src/views/lib/VwGraphics.cpp`
- `Src/views/lib/VwBaseVc.cpp`

Assessment:

- Clipboard behavior is already mostly managed.
- Remaining OLE clipboard cleanup appears low-value and possibly dormant.
- Picture/media COM is still real, but it is more of a chosen boundary shape than a hard OS requirement.
- There is already a managed picture implementation pattern on the non-Windows path.

Removal path:

- Clipboard OLE cleanup: `easy`
- Picture creation and loading cleanup: `incremental`
- Full `IPicture` ABI removal from Views: `long-term`

Better path:

- Make the managed clipboard path authoritative.
- Centralize picture creation so Windows does not mix AxHost conversion, `OleLoadPicture`, and multiple object lifetime conventions.
- Use the managed picture path as the template for Windows-side simplification where possible.

### 7. External Legacy COM Integrations

What it is:

- External COM libraries still used from C#, especially `SilEncConverters40`.
- Some registry/ProgID lookup code around these legacy integrations.

Representative files:

- `Src/ParatextImport/SCTextEnum.cs`
- `Src/FwCoreDlgs/FwWritingSystemSetupModel.cs`
- `Src/FwCoreDlgs/ConverterTester.cs`
- `Src/LexText/Interlinear/LinguaLinksImportDlg.cs`
- `Src/ProjectUnpacker/RegistryData.cs`

Assessment:

- This still looks like a real dependency, not dead code.
- It is not a good first candidate for ripping out directly because the product still constructs and ships around it.

Removal path:

- `long-term`

Better path:

- First hide it behind a FieldWorks-owned adapter/service so the rest of the codebase stops directly constructing `EncConverters`.
- Once isolated, replacement can happen per workflow instead of as a repo-wide flag day.

### 8. Native COM Stream and Persistence Helpers

What it is:

- Widespread native use of `IStream` as a persistence and transport abstraction.
- Helpers for file streams, resource streams, string streams, XML formatting, and object serialization.

Representative files:

- `Src/Generic/FileStrm.h`
- `Src/Generic/FileStrm.cpp`
- `Src/Generic/ResourceStrm.h`
- `Src/Generic/UtilPersist.h`
- `Src/Generic/Util.cpp`
- `Src/views/lib/TsString.cpp`

Why it matters:

- This is the part closest to "COM for data storage" in the current tree.
- The repo does not seem to rely heavily on structured storage (`IStorage`) anymore, but it still leans on `IStream` widely.

Assessment:

- This is not the same thing as the core Views ABI, but it is deeply embedded native plumbing.
- It is replaceable in principle, but only gradually.

Removal path:

- `incremental`

Better path:

- Add non-COM byte-stream/persistence helpers for new code.
- Migrate internal utilities and native persistence helpers gradually instead of attempting a broad replacement in one pass.

### 9. Registration-Free COM Build and Deployment Plumbing

What it is:

- Manifest generation, CLSID/ProgID discovery, manifest attachment, and activation-context setup.

Representative files:

- `Build/Src/FwBuildTasks/RegFreeCreator.cs`
- `Build/RegFree.targets`
- `Build/mkall.targets`
- `Src/Common/FwUtils/ActivationContextHelper.cs`

Assessment:

- This exists because the runtime architecture still needs COM, especially the native Views/FwKernel substrate.
- But the build complexity is inflated by optional managed COM-visible surfaces, excluded CLSID bookkeeping, and legacy ProgID support.

Removal path:

- Core reg-free mechanism: `long-term`
- Complexity reduction around optional surfaces: `incremental`

Better path:

- Keep reg-free COM for the true native substrate.
- Prune manifest inputs as optional managed COM surfaces are removed.
- Consolidate excluded CLSID coordination and simplify `RegFreeCreator` over time.

### 10. Native Factory Registration and Debug-Only COM

What it is:

- Native factory/registration support and legacy ProgID handling.
- Debug-only COM activation for unmanaged debug reporting.

Representative files:

- `Src/Generic/GenericFactory.cpp`
- `Src/Generic/GenericFactory.h`
- `Src/Generic/ComSmartPtr.h`
- `Src/Common/FwUtils/DebugProcs.cs`

Assessment:

- `DebugProcs` is easy to isolate or replace because it is Windows-only and debug-only.
- Generic factory/ProgID logic is harder because some of it is still tied to native activation and legacy assumptions, but a lot of ProgID overhead looks like cleanup debt rather than core runtime need.

Removal path:

- Debug COM sink: `easy`
- ProgID and factory cleanup: `incremental`

Better path:

- Replace the debug sink with a managed-only debug/reporting abstraction when convenient.
- Audit real ProgID usage, then remove unused ProgIDs from manifests and factory registration logic.

## Best Near-Term Opportunities

These are the changes most likely to reduce COM without destabilizing the product.

1. Remove `ComVisible` from `ManagedLgIcuCollator` and prune its manifest/build entries once an out-of-repo dependency check is done.
2. In a Windows-first cleanup wave, audit and likely remove `ViewInputManager`, `ManagedVwWindow`, and `ManagedPictureFactory` if they are only retained for non-Windows or dead compatibility paths.
3. Keep new C# code on managed data-access abstractions and treat raw `ISilDataAccess` as a boundary-only contract.
4. Introduce typed managed wrappers for the common `UnknownProp` cases, especially paragraph style props, so managed code stops depending on a generic `IUnknown` slot.
5. Make the managed clipboard path authoritative and quarantine or remove dormant OLE clipboard shutdown code after one more targeted validation pass.
6. Centralize Windows picture creation/loading behind one managed-facing factory and reduce direct `IPicture`/`OleLoadPicture` usage spread.
7. Wrap `SilEncConverters40` behind a FieldWorks-owned adapter so future replacement work is localized.
8. Audit optional managed COM-visible classes and remove manifest entries as each one becomes unnecessary.
9. Consolidate excluded CLSID management and simplify `RegFreeCreator`; that pays down build complexity without changing runtime architecture.
10. Replace or isolate `DebugProcs` COM activation because it is easy debt with minimal product risk.

## Things That Are Not Good First Targets

- Rewriting RootBox in C#.
- Replacing the native layout/editing pipeline while Graphite is still required.
- Replacing TSF with ordinary WinForms text input APIs.
- Rebuilding the accessibility tree before the current MSAA/service-provider contract is explicitly tested.

## Short Answer to the Original Question

Can COM be removed easily overall?

- No. The core Views/FwKernel architecture still depends on it.

Can meaningful parts of it be reduced?

- Yes. Managed COM-visible shims, optional picture/clipboard OLE helpers, debug-only COM, legacy ProgID/factory baggage, and direct external COM call sites are all practical reduction targets.

Is there a clean C#-only path?

- For the whole app, no.
- For several islands around managed implementations and application-level adapters, yes.
- The pragmatic strategy is to make the C# side cleaner first, then let the required COM boundary shrink toward the native rendering/data core instead of trying to erase it wholesale.
