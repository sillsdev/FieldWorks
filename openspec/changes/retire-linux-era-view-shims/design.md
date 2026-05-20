## Context

FieldWorks is Windows-only, but the Views stack still contains two managed COM-visible shims from the Linux/Mono era:

- `ViewInputManager`, a `SimpleRootSite.dll` class created by the old non-Windows `VwRootBox` branch to satisfy `IViewInputMgr`.
- `ManagedVwWindow`, a `ManagedVwWindow.dll` class created by old non-Windows selection/page-sizing branches to query a WinForms control client rectangle.

Current Windows behavior does not use either managed class. Windows `VwRootBox::Init()` creates native `VwTextStore` and queries `IViewInputMgr`; Windows selection/page sizing calls Win32 `GetClientRect`. The managed shims remain as source, project/test topology, generated interop declarations, reg-free manifest inputs, and CLSID exclusions.

See `research.md` in this change for the evidence map, diagrams, and external documentation notes.

## Goals / Non-Goals

**Goals:**

- Retire `ViewInputManager` and `ManagedVwWindow` as COM activation surfaces for Windows-first FieldWorks.
- Remove dead non-Windows native activation branches that create `CLSID_ViewInputManager` and `CLSID_VwWindow`.
- Clean related build, solution, reg-free manifest, and generated interop artifacts in the same slice.
- Preserve and validate the active Windows `VwTextStore` input path.
- Preserve and validate Win32 `GetClientRect` selection/page geometry behavior.
- Leave a clear audit trail for why these are Linux-era compatibility shims.

**Non-Goals:**

- Do not remove native `VwTextStore` or rewrite Windows TSF behavior.
- Do not remove `IViewInputMgr` unless a separate native audit proves all active callers have a replacement. Current evidence says it is required.
- Do not remove `SimpleRootSite.dll` from managed COM manifest inputs wholesale unless every other COM-visible class in that assembly is audited.
- Do not remove unrelated optional COM surfaces such as `ManagedPictureFactory`, `ManagedVwDrawRootBuffered`, or `UnknownProp` paths.
- Do not restore, support, or test Linux builds.

## Decisions

### Remove the two shim implementations together

`ViewInputManager` and `ManagedVwWindow` are part of the same old cross-platform story. Removing only one would leave the repo half-clean and preserve unnecessary build/manifest exclusions. The implementation should retire both in one focused change.

Alternative considered: keep them deferred indefinitely. That avoids churn but keeps misleading COM-visible managed classes and stale reg-free manifest plumbing around a Windows-only product.

### Preserve `IViewInputMgr` while removing managed `ViewInputManager`

The managed `ViewInputManager` class is dormant on Windows, but the `IViewInputMgr` contract is still the native abstraction that `VwTextStore` implements and native Views callers use. The removal should target the managed class and old non-Windows `CreateInstance` branch, not the active interface.

Alternative considered: remove `IViewInputMgr` entirely. That would become a TSF/native Views refactor and is outside this cleanup.

### Remove the `VwWindow` coclass and generated managed exposure when removing `ManagedVwWindow`

`ManagedVwWindow` exists only to implement `IVwWindow` through COM activation. Once the implementation and non-Windows activation branches are gone, keeping the `VwWindow` coclass in `Views.idh` and generated `Views.cs` would preserve a dead activation contract. The conservative path is to remove the coclass and generated class exposure. The `IVwWindow` interface can be removed too if no internal consumers remain after the native branch removal.

Alternative considered: delete only the C# project. That would leave stale IDL/generated activation metadata and make future audits harder.

### Keep manifest cleanup in the same implementation slice

These shims are still represented in `Build/mkall.targets`, `Build/RegFree.targets`, and `Src/Common/FieldWorks/BuildInclude.targets`. Removing source without cleaning manifest inputs would leave confusing or broken reg-free COM output. The implementation must update build inputs and prove the removed CLSIDs disappear from generated manifests while required COM entries remain.

Alternative considered: stage source deletion and manifest cleanup separately. That increases the chance of transient broken manifests and is unnecessary for this isolated cleanup.

## Risks / Trade-offs

- Hidden external CLSID consumer -> Confirm no known out-of-repo automation or extensions depend on either CLSID before merge. If uncertain, record compatibility sign-off in the PR.
- Stale generated Views artifacts -> Clean/regenerate `Views.cs`, `ViewsTlb.*`, `ViewsPs.*`, and related manifests after `Views.idh` edits. Be alert to stale `Output` and `Obj` artifacts.
- Accidental removal of active input behavior -> Keep `IViewInputMgr` and `VwTextStore` intact; run native Views tests and manual IME/composition smoke checks.
- Reg-free manifest regression -> Run reg-free build-task tests and inspect generated manifests for absence of only the targeted CLSIDs.
- Review complexity -> Keep this as a dedicated cleanup PR, separate from Encoding Converters or other managed COM work.

## Migration Plan

1. Baseline current behavior with `ManagedVwWindowTests` and `TestViews` before deletion.
2. Remove dead non-Windows native activation branches in `VwRootBox.cpp` and `VwSelection.cpp`, leaving Windows code paths unchanged.
3. Remove `ViewInputManager` as a COM-visible class and delete `ManagedVwWindow` project/test topology.
4. Update `Views.idh`, generated interop, and GUID declarations to remove stale `VwWindow` coclass/interface exposure as appropriate.
5. Clean reg-free manifest inputs and excluded CLSIDs.
6. Run build/test validation and manual RootSite input/selection smoke checks.
7. Update architecture docs/specs when archiving so main specs no longer describe `ManagedVwWindow` as a live bridge.

Rollback is normal source rollback: restore the shim projects, native `#else` activation branches, and manifest inputs if validation reveals a hidden dependency.

## Open Questions

- Is there any known out-of-repo automation, extension, installer probe, or downstream consumer that activates either managed CLSID directly?
- Should `IVwWindow` be removed entirely from `Views.idh`, or should only the `VwWindow` coclass be removed first to minimize generated interface churn?
- Which generated artifacts are committed or expected to be updated in this repository for `Views.idh` changes on this branch?
