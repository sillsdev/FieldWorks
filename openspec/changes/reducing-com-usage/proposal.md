## Why

FieldWorks has a large, legitimate COM core around Views/FwKernel, RootBox, Graphite-backed rendering, and Windows TSF input. That core is not feasible to remove now. However, the audit in `COM_USAGE.md` and the near-term plan in `COM_FIXES_NOW.md` identify several smaller COM surfaces that are likely removable or containable without touching the native editor/rendering bedrock.

The immediate problem is that optional COM-visible managed classes, debug-only COM activation, dormant OLE clipboard cleanup, direct `SilEncConverters40` use, and class-specific reg-free manifest entries create ongoing activation, build, and maintenance complexity. Some of these decisions are 20 years old and no longer match current Windows/x64, .NET Framework 4.8 development practice.

## What Changes

- Clarify policy decisions before implementation: external CLSID compatibility, Windows-only support boundaries, DebugProcs replacement versus isolation, and first-slice scope for Encoding Converters.
- Remove low-risk optional COM surfaces where in-repo callers already use direct managed construction, beginning with `ManagedLgIcuCollator`.
- Treat manifest/build cleanup as part of each COM-surface removal, including `Build/RegFree.targets`, `Build/mkall.targets`, and `Src/Common/FieldWorks/BuildInclude.targets`.
- Remove or isolate debug-only and dormant COM paths only after characterization tests lock current behavior.
- Introduce a FieldWorks-owned adapter seam around `SilEncConverters40`; this does not remove the external COM dependency immediately, but prevents further spread.
- Preserve required native COM boundaries for Views/FwKernel, RootBox, Graphite, TSF, and core data-access/rendering contracts.

## Non-goals

- Removing Graphite.
- Replacing RootBox, Views/FwKernel, or the native rendering/editing ABI.
- Replacing Windows TSF text input or MSAA accessibility now.
- Removing the full `IPicture`, `IStream`, or `UnknownProp` ABI in this change.
- Adding new NuGet packages for image processing, mocking, or COM interop tooling.
- Introducing global COM registration or registry workarounds.

## Capabilities

### New Capabilities

- `architecture/testing/com-reduction`: Validation requirements for COM-reduction changes, including characterization tests, manifest diffs, and smoke checks.

### Modified Capabilities

- `architecture/interop/com-contracts`: Add requirements for class-specific COM-surface removals, manifest cleanup, and required-boundary preservation.
- `integration/external/encoding`: Add a FieldWorks-owned adapter requirement for future Encoding Converter call sites.
- `architecture/build-deploy/build-phases`: Clarify that optional managed COM removals must remove related reg-free build inputs in the same work slice.
- `architecture/testing/test-strategy`: Add COM-boundary validation expectations for managed/native cleanup work.

## Impact

- **Affected managed code:** `Src/ManagedLgIcuCollator/`, `Src/Common/FwUtils/DebugProcs.cs`, Encoding Converter call sites under `Src/FwCoreDlgs/`, `Src/ParatextImport/`, `Src/LexText/`, and optional non-Windows shims if approved.
- **Affected native code:** targeted cleanup in `Src/Generic/ModuleEntry.*`; optional guarded cleanup in `Src/views/` only for non-Windows shim removal.
- **Affected build:** `Build/RegFree.targets`, `Build/mkall.targets`, `Src/Common/FieldWorks/BuildInclude.targets`, and focused `RegFreeCreator` tests.
- **Dependencies:** no new runtime or test NuGet packages; continue using existing Moq/NUnit/System.Drawing/encoding-converters-core.
- **Risk:** low-to-medium for `ManagedLgIcuCollator` until external CLSID compatibility is ruled out; medium for DebugProcs and clipboard cleanup until required smoke validation is complete; optional/risky for Windows-policy shims, Encoding Converter adapter expansion, picture work, `VwDrawRootBuffered`, and `UnknownProp` narrowing.
