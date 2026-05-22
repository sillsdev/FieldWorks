## Why

FieldWorks is now documented as Windows-only, but the Views stack still carries two managed COM shims from the Linux/Mono era: `ViewInputManager` and `ManagedVwWindow`. They are not active on the Windows x64 runtime path, yet they still affect source, generated interop, reg-free manifest inputs, build exclusions, and test topology.

This change captures the dedicated cleanup needed to retire those shims deliberately, with the Windows `VwTextStore` and Win32 window-size paths validated after removal.

## What Changes

- Remove the managed COM activation surface for `ViewInputManager`, the old non-Windows input-method bridge.
- Remove the managed COM activation surface and project/test topology for `ManagedVwWindow`, the old cross-platform HWND wrapper.
- Remove native non-Windows activation branches that create `CLSID_ViewInputManager` or `CLSID_VwWindow`.
- Clean reg-free COM manifest inputs and CLSID exclusion lists for the retired managed shims.
- Update Views IDL/generated artifacts only as far as needed to remove stale `VwWindow` coclass exposure while preserving required Windows input contracts.
- Validate that Windows remains on native `VwTextStore` for TSF/input and Win32 `GetClientRect` for page/selection geometry.

## Non-goals

- Do not remove or rewrite the native Windows `VwTextStore` implementation.
- Do not remove `IViewInputMgr` unless a focused native audit proves it is no longer needed; current evidence says it is still active through `VwTextStore`.
- Do not change RootSite rendering, selection semantics, TSF behavior, or IME composition behavior.
- Do not remove unrelated managed COM surfaces such as `ManagedPictureFactory` or `ManagedVwDrawRootBuffered`.
- Do not add global COM registration or registry-based activation.

## Capabilities

### New Capabilities

- None.

### Modified Capabilities

- `architecture/interop/com-contracts`: optional Linux-era managed COM shims may be retired only with source, build, manifest, and compatibility cleanup in the same slice.
- `architecture/ui-framework/views-rendering`: Windows Views rendering/input requirements are clarified to use native `VwTextStore` and Win32 window geometry, not the managed shim implementations.
- `architecture/testing/test-strategy`: shim retirement requires native Views, reg-free manifest, and manual RootSite/IME smoke validation.

## Impact

- Native C++ Views code: `Src/views/VwRootBox.cpp`, `Src/views/VwSelection.cpp`, `Src/views/Views.idh`, generated Views interop artifacts as needed.
- Managed C# shim code: `Src/Common/SimpleRootSite/ViewInputManager.cs`, `Src/ManagedVwWindow/**`.
- Build and reg-free COM files: `Build/RegFree.targets`, `Build/mkall.targets`, `Src/Common/FieldWorks/BuildInclude.targets`, `FieldWorks.sln`, generated manifests, and affected reg-free tests.
- Validation: native `TestViews`, managed build-task tests, targeted RootSite tests, and manual RootSite keyboard/selection/IME smoke checks.
