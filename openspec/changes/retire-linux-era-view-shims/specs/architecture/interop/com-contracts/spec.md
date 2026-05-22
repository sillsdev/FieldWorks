## ADDED Requirements

### Requirement: Linux-Era Managed Views Shims Are Optional COM Surfaces
FieldWorks SHALL treat `ViewInputManager` and `ManagedVwWindow` as optional Linux-era managed COM shims, not required Windows runtime COM contracts.

#### Scenario: ViewInputManager removal is implemented
- **WHEN** `Src/Common/SimpleRootSite/ViewInputManager.cs` is removed or made non-COM-visible
- **THEN** the non-Windows `CLSID_ViewInputManager` activation branch in `Src/views/VwRootBox.cpp` MUST be removed
- **AND** the active Windows `VwTextStore` implementation of `IViewInputMgr` MUST remain intact.

#### Scenario: ManagedVwWindow removal is implemented
- **WHEN** `Src/ManagedVwWindow/ManagedVwWindow.cs` and its project are removed
- **THEN** the non-Windows `CLSID_VwWindow` activation branches in `Src/views/VwSelection.cpp` MUST be removed
- **AND** stale `VwWindow` coclass exposure in `Src/views/Views.idh` and generated interop artifacts MUST be removed or explicitly justified.

### Requirement: Shim Retirement Includes Reg-Free COM Cleanup
Retiring either Linux-era managed Views shim SHALL include matching reg-free COM build and manifest cleanup in the same implementation slice.

#### Scenario: Managed shim CLSIDs are retired
- **WHEN** `{830BAF1F-6F84-46EF-B63E-3C1BFDF9E83E}` or `{3fb0fcd2-ac55-42a8-b580-73b89a2b6215}` is no longer exposed for COM activation
- **THEN** the corresponding entries in `Build/mkall.targets`, `Build/RegFree.targets`, and `Src/Common/FieldWorks/BuildInclude.targets` MUST be updated where applicable
- **AND** generated manifests MUST be checked to confirm the retired CLSIDs are absent while required native COM entries remain.

#### Scenario: SimpleRootSite manifest inputs are reviewed
- **WHEN** `ViewInputManager` is removed from `SimpleRootSite.dll`
- **THEN** `SimpleRootSite.dll` MUST remain in managed COM manifest inputs unless a separate audit proves no remaining COM-visible class in that assembly is required.
