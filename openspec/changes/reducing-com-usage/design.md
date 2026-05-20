## Context

The current COM inventory splits into two categories that must not be confused:

- required native COM boundaries: Views/FwKernel, RootBox, Graphite/Uniscribe rendering, TSF input, MSAA accessibility, and Views-facing data access;
- optional or containable COM surfaces: managed classes still marked `ComVisible(true)`, debug-only COM activation, dormant OLE clipboard ownership cleanup, direct external Encoding Converter construction, and class-specific reg-free manifest entries.

The design follows the audit documents copied into this change directory: `COM_USAGE.md` and `COM_FIXES_NOW.md`. Subagent review added three important refinements:

- `Src/Common/FieldWorks/BuildInclude.targets` must be cleaned alongside `Build/RegFree.targets` and `Build/mkall.targets`.
- `SilEncConverters40` has no realistic managed .NET Framework 4.8 replacement package today; the right move is a FieldWorks-owned adapter, not a package swap.
- optional non-Windows shims require an explicit Windows-first support decision before deletion.

## Goals / Non-Goals

**Goals:**

- Reduce optional COM surface area without destabilizing required native COM boundaries.
- Make each cleanup independently reviewable, testable, and reversible.
- Clarify alternative paths before implementation when compatibility or product policy is uncertain.
- Prefer internal adapters and existing test tools over new dependencies.
- Keep reg-free manifest cleanup paired with each removed COM class.

**Non-Goals:**

- No RootBox, Views/FwKernel, Graphite, TSF core, or native rendering rewrite.
- No global COM registration.
- No broad search-and-delete of `COM`, `IUnknown`, `IDataObject`, `IStream`, or `ComVisible`.
- No new mocking, image processing, COM interop, or packaging NuGet dependencies for this change.

## Decisions

### 1. Branch by abstraction for uncertain dependencies

Encoding Converter work SHALL use a FieldWorks-owned provider around `SilEncConverters40`, not replace the package in this change. This follows Branch by Abstraction and Anti-Corruption Layer patterns: move callers behind a stable FieldWorks contract first, then evaluate future replacements per workflow.

References to guide implementation:

- Martin Fowler, Branch by Abstraction: https://martinfowler.com/bliki/BranchByAbstraction.html
- Martin Fowler, Strangler Fig Application: https://martinfowler.com/bliki/StranglerFigApplication.html
- Microsoft, Anti-corruption Layer pattern: https://learn.microsoft.com/azure/architecture/patterns/anti-corruption-layer

### 2. Keep required COM as an intentional boundary

Registration-free COM remains the correct deployment model for required native FieldWorks components. Cleanup SHALL remove optional class-specific entries only when the corresponding surface is actually removed.

Reference:

- Microsoft, Registration-Free COM Interop: https://learn.microsoft.com/dotnet/framework/interop/registration-free-com-interop

### 3. No package-first modernization

Subagent package research found no useful new NuGet dependency for the near-term work:

- keep `encoding-converters-core` for now and wrap it;
- keep existing `System.Drawing.Common` and current picture patterns for now;
- keep existing NUnit and Moq;
- do not add ImageMagick, SkiaSharp, FakeItEasy, NSubstitute, Costura/Fody, or new COM tooling.

### 4. Characterization tests before risky removals

Following legacy-code refactoring practice, behavior-sensitive work must first lock current behavior with focused tests or smoke checks. This especially applies to clipboard, DebugProcs, Encoding Converters, and manifest generation.

### 5. Optional work is policy-gated or behavior-sensitive

The following are marked optional until clarified:

- deleting `ViewInputManager` and `ManagedVwWindow` source paths, because this depends on Windows-only support policy;
- replacing rather than isolating DebugProcs COM activation;
- replacing the Encoding Converters runtime after the provider crossover;
- changing picture creation/lifetime or `VwDrawRootBuffered` defaults;
- narrowing `UnknownProp` usage beyond documentation and local managed wrappers.

### 6. Implemented first-slice decisions

DebugProcs COM activation is isolated, not replaced, in this change. `Src/Common/FwUtils/DebugProcs.cs` now routes CLSID activation through a debug-only transport seam so construction, failure tolerance, and disposal can be tested without activating real COM.

The Encoding Converter crossover now uses `Src/Common/FwUtils/EncodingConvertersProvider.cs` as the shared managed boundary. Product call sites no longer construct `SilEncConverters40.EncConverters` directly; the provider owns lazy construction and exposes lookup/enumeration helpers plus controlled access to the existing repository for legacy configuration flows.

This is containment, not replacement. FieldWorks still uses `encoding-converters-core` and still ships native/plugin payloads. User-facing converter management remains tied to EncConverters repository APIs such as Add, Remove, AutoConfigure, and converter type constants, so true runtime replacement remains a later per-workflow decision.

### 7. Historical provenance checks for removed COM surfaces

The first removals were checked against `origin/main` history so that the decision is based on why the COM existed, not only on current branch diffs.

| Surface | Historical evidence | Historical issue or PR relevance | Current relevance assessment | Decision |
| --- | --- | --- | --- | --- |
| `ManagedLgIcuCollator` COM visibility and manifest entries | The class-level `[ComVisible(true)]`, `[ClassInterface(ClassInterfaceType.None)]`, and CLSID `{e771361c-ff54-4120-9525-98a0b7a9accf}` trace to the truncated 2012 git import. `Build/mkall.targets` referenced `ManagedLgIcuCollator` in the 2012 MSBuild migration commit `d2cbca5776`. PR #678 / commit `5711bf6bed` later preserved the class in the new registration-free COM manifest inputs as part of broad .NET tooling and reg-free COM modernization. | No LT/Jira issue was found for the original collator COM exposure. PR #678 is relevant as deployment infrastructure, but its PR body describes general registration-free COM modernization and does not identify a class-specific requirement for this CLSID. | `origin/main` references to the collator CLSID are limited to the class attribute and build/manifest inputs. Repo-local callers construct `ManagedLgIcuCollator` directly in managed code. Native `Src/views` code uses `ILgCollatingEngine`, but the in-repo native path creates `LgUnicodeCollater::CreateCom`, not the managed collator CLSID. | Continue removal of the managed collator COM surface, with one caveat: any out-of-repo automation or extension using the CLSID would be broken and must be treated as an external compatibility exception if discovered. |
| Dormant OLE clipboard ownership cleanup in `ModuleEntry` and unused OLE P/Invokes | `IDataObjectPtr ModuleEntry::s_qdobjClipboard`, `ModuleEntry::SetClipboard`, and the shutdown `OleIsCurrentClipboard` / `OleFlushClipboard` cleanup trace to the truncated 2012 git import. The managed `Win32Wrappers` declarations also trace to the import, with a later 2018 touch in `f2ba34747f` for `LT-19322`. | `LT-19322` is closed and was a general Win32Wrapper 64-bit audit prompted by `LT-19315` (`Help > About` x64 crash). Jira text and follow-up history do not mention `OleFlushClipboard`, `OleIsCurrentClipboard`, or clipboard persistence. | `origin/main` has no live callers of `ModuleEntry::SetClipboard` outside its declaration/definitions, so `s_qdobjClipboard` is never populated by product code. Active clipboard behavior uses managed `ClipboardUtils` / `Clipboard.SetDataObject(data, copy, ...)`. | Continue removal of the dormant native ownership cleanup and unused P/Invokes, but keep the manual clipboard persistence smoke test as required validation before merge. |

## Migration Plan

1. Clarify compatibility and alternative paths.
2. Capture baseline usage and manifest state.
3. Implement the safest removal first: `ManagedLgIcuCollator` COM visibility and related manifest/build inputs.
4. Add or strengthen manifest-generation tests so optional COM removals cannot leave stale entries behind.
5. Remove dormant OLE clipboard ownership cleanup if characterization confirms it is unused.
6. Isolate or replace DebugProcs COM activation depending on the chosen alternative.
7. Add the shared Encoding Converter provider and migrate direct repository construction to it.
8. Consider optional/risky follow-ups only after explicit decisions and baseline validation.

## Risks / Trade-offs

| Risk | Mitigation |
| --- | --- |
| External tools depend on a removed CLSID | First task clarifies external compatibility contract; stage manifest removal separately if needed. |
| Manifest cleanup accidentally removes required native COM | Diff manifests and track targeted CLSIDs explicitly. |
| Debug diagnostics regress | Add constructor/dispose/failure-tolerance tests; keep isolation as fallback. |
| Encoding Converter provider becomes a runtime rewrite | Centralize construction and access only; keep `encoding-converters-core` and defer replacement decisions per workflow. |
| Non-Windows shim removal breaks dormant source support | Make Windows-first policy explicit before source deletion. |
| Clipboard cleanup touches active TSF or managed clipboard flows | Limit scope to `ModuleEntry::SetClipboard` ownership cleanup; leave TSF and managed clipboard intact. |

## Open Questions

1. Is `ManagedLgIcuCollator` CLSID compatibility required for any out-of-repo automation or extension?
2. Is FieldWorks now source-level Windows-only, or only product-runtime Windows-only?
3. Should picture creation centralization and `UnknownProp` narrowing remain follow-up proposals rather than optional tasks in this change?
