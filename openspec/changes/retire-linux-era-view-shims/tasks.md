## 1. Baseline and Compatibility Checks

- [ ] 1.1 Confirm no known out-of-repo automation, extension, installer probe, or downstream consumer depends on CLSID `{830BAF1F-6F84-46EF-B63E-3C1BFDF9E83E}` for `ViewInputManager`. [Compatibility, <=1h]
- [ ] 1.2 Confirm no known out-of-repo automation, extension, installer probe, or downstream consumer depends on CLSID `{3fb0fcd2-ac55-42a8-b580-73b89a2b6215}` for `ManagedVwWindow`. [Compatibility, <=1h]
- [ ] 1.3 Run pre-removal baseline `.	est.ps1 -TestProject ManagedVwWindowTests` while the project still exists. [Managed test, <=1h]
- [ ] 1.4 Run pre-removal baseline `.	est.ps1 -SkipManaged -TestProject TestViews`. [Native test, <=2h]

## 2. Native Views Cleanup

- [ ] 2.1 Remove `CLSID_ViewInputManager` and the non-Windows managed `CreateInstance` branch from `Src/views/VwRootBox.cpp`; keep the Windows `VwTextStore` path intact. [Native C++, <=2h]
- [ ] 2.2 Remove `CLSID_VwWindow` activation branches from `Src/views/VwSelection.cpp`; keep the Windows `GetClientRect` path intact. [Native C++, <=2h]
- [ ] 2.3 Audit native references to `IViewInputMgr`, `VwRootBox::InputManager()`, and `VwTextStore` after cleanup; do not remove active Windows input contracts. [Native C++, <=1h]

## 3. Managed Shim Removal

- [ ] 3.1 Remove `Src/Common/SimpleRootSite/ViewInputManager.cs` from source/project inputs without removing unrelated `SimpleRootSite.dll` COM surfaces. [Managed C#, <=2h]
- [ ] 3.2 Remove `Src/ManagedVwWindow/ManagedVwWindow.cs`, `Src/ManagedVwWindow/ManagedVwWindow.csproj`, and `Src/ManagedVwWindow/ManagedVwWindowTests/**`. [Managed C#, <=2h]
- [ ] 3.3 Remove `ManagedVwWindow` and `ManagedVwWindowTests` from `FieldWorks.sln` and any traversal/build inputs that explicitly reference those projects. [Build, <=1h]

## 4. IDL and Generated Interop Cleanup

- [ ] 4.1 Remove stale `VwWindow` coclass exposure from `Src/views/Views.idh`; remove `IVwWindow` too only if the native branch removal leaves no valid consumer. [Native/IDL, <=2h]
- [ ] 4.2 Update `Src/views/Views_GUIDs.cpp` and generated `Src/Common/ViewsInterfaces/Views.cs` consistently with the `Views.idh` decision. [Native/managed generated, <=2h]
- [ ] 4.3 Clear or regenerate stale `Output/<Config>/Common/ViewsTlb.*`, `ViewsPs.*`, `Raw/ViewsTlb*.*`, and `Raw/ViewsPs*.*` artifacts before build validation if `Views.idh` changes. [Build hygiene, <=1h]

## 5. Reg-Free Manifest and Build Cleanup

- [ ] 5.1 Remove the `ManagedVwWindow` managed COM assembly input from `Build/RegFree.targets` and `Src/Common/FieldWorks/BuildInclude.targets`. [Build, <=1h]
- [ ] 5.2 Remove retired shim CLSIDs from `Build/mkall.targets` `ExcludedClsids`: `{830BAF1F-6F84-46EF-B63E-3C1BFDF9E83E}` and `{3fb0fcd2-ac55-42a8-b580-73b89a2b6215}`. [Build, <=1h]
- [ ] 5.3 Update `Build/Src/FwBuildTasks/FwBuildTasksTests/RegFreeCreatorTests.cs` so tests no longer rely on the retired `ManagedVwWindow` CLSID as sample data. [Managed test, <=1h]
- [ ] 5.4 Inspect generated manifests after build to confirm the two retired CLSIDs are absent and required native Views/FwKernel COM entries remain present. [Validation, <=1h]

## 6. Documentation Updates

- [ ] 6.1 Update architecture docs/specs that currently describe `ManagedVwWindow` as a live bridge, including `openspec/specs/architecture/ui-framework/views-rendering.md` when this change is archived. [Docs, <=1h]
- [ ] 6.2 Update COM inventory notes to record that `ViewInputManager` and `ManagedVwWindow` were retired as Linux-era shims. [Docs, <=1h]
- [ ] 6.3 Update any affected folder `AGENTS.md` or source comments that still describe Linux/Mono paths as supported runtime behavior. [Docs, <=1h]

## 7. Automated Validation

- [ ] 7.1 Run `.uild.ps1` to validate native-before-managed ordering and regenerate reg-free manifests. [Build validation]
- [ ] 7.2 Run `.	est.ps1 -SkipManaged -TestProject TestViews`. [Native validation]
- [ ] 7.3 Run `.	est.ps1 -TestProject FwBuildTasksTests -TestFilter "FullyQualifiedName~RegFreeCreator"`. [Managed validation]
- [ ] 7.4 Run targeted RootSite/Views managed tests, including `.	est.ps1 -TestProject SimpleRootSiteTests` if available in the current test routing. [Managed validation]
- [ ] 7.5 Run `CI: Whitespace check`; if it rewrites files, review the diff and rerun until clean. [CI hygiene]

## 8. Manual Smoke Validation

- [ ] 8.1 Edit text in a RootSite field in a rebuilt FieldWorks app; verify normal typing and keyboard switching still work. [Manual]
- [ ] 8.2 Test IME/composition in a RootSite field if an IME is available; verify pre-edit and commit behavior still work. [Manual]
- [ ] 8.3 Exercise mouse selection, keyboard selection, Shift+arrow selection, PageUp, and PageDown in a RootSite field; verify selection and visible-page movement remain correct. [Manual]
- [ ] 8.4 Launch from rebuilt output and watch for side-by-side activation or missing manifest errors. [Manual]
