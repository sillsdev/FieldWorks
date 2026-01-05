# Registry Dependency Elimination Plan

**Priority**: ⚠️ **HIGH**
**Goal**: Make the Registration-Free COM build process hermetic and deterministic.
**Current State**: `RegFreeCreator.cs` relies on `HKEY_CLASSES_ROOT` to find DLL paths, threading models, and proxy stubs.
**Problem**: Builds fail or produce incorrect manifests if COM components are not registered on the build machine. This causes "registry errors" when running the application if the manifest doesn't match the actual file layout.

## Taking the trace

Here are the commands to capture and parse Side-by-Side (SxS) traces.
Note: sxstrace requires an Administrator terminal.

* go to the folder you want to save the trace in
* Start the trace (Run this in an Admin terminal):
  * `sxstrace Trace -logfile:sxstrace.etl`
* Run the application (In your normal terminal):
  * `.\Output\Debug\FieldWorks.exe`
  * (Wait for the error dialog to appear, then close it)
* Stop the trace:
  * Go back to the Admin terminal and press Enter to stop tracing.
* Parse the log (Converts the binary .etl to readable text):
  * `sxstrace Parse -logfile:sxstrace.etl -outfile:sxstrace.txt`

The resulting sxstrace.txt will contain the detailed error report explaining why the application failed to start.The resulting sxstrace.txt will contain the detailed error report explaining why the application failed to start.

---

## Rationale

The current implementation violates the core principle of Registration-Free COM: **independence from the registry**.

1.  **Non-Deterministic Builds**: The content of the generated manifest depends on the state of the build machine's registry. Two machines could produce different manifests.
2.  **Circular Dependency**: The build requires the DLLs to be registered to generate the manifest that allows them to run *without* registration.
3.  **CI/CD Failure**: Clean build agents (like GitHub Actions) cannot generate valid manifests because they don't have the components registered.

## Plan

We will refactor `Build/Src/FwBuildTasks/RegFreeCreator.cs` to derive all necessary information from the input files (DLLs and TypeLibs) themselves, rather than querying the registry.

### Strategy

1.  **Implicit Server Location**: When processing a TypeLib embedded in `MyComponent.dll`, assume `MyComponent.dll` is the `InprocServer32` for all CoClasses defined within it.
2.  **Default Threading Models**: Default to `Apartment` threading for native COM components (standard for FieldWorks) if not explicitly specified in metadata.
3.  **Standard Marshaling**: Use standard OLE automation marshaling for interfaces unless specific proxy/stub DLLs are provided as inputs.

---

## Implementation Checklist

### Phase 1: Refactor RegFreeCreator.cs

- [x] **Task 1.1**: Modify `ProcessTypeInfo` to stop reading `InprocServer32`.
    - *Current*: Looks up CLSID in registry to find the DLL path.
    - *New*: Use the `fileName` passed to `ProcessTypeLibrary` as the server path.
    - *Rationale*: The TypeLib is inside the DLL; the DLL is the server.

- [x] **Task 1.2**: Modify `ProcessTypeInfo` to handle ThreadingModel without registry.
    - *Current*: Reads `ThreadingModel` from registry.
    - *New*: Default to `"Apartment"`.
    - *Rationale*: FieldWorks native components (Views, FwKernel) are Apartment threaded.

- [x] **Task 1.3**: Deprecate/Remove `ProcessClasses`.
    - *Current*: Iterates all found CoClasses and updates them from HKCR.
    - *New*: Remove this step. All info should be gathered during `ProcessTypeLibrary`.

- [x] **Task 1.4**: Refactor `ProcessInterfaces` to remove registry dependency.
    - *Current*: Looks up `Interface\{IID}\ProxyStubClsid32` in registry.
    - *New*: If the interface is in the TypeLib, assume standard marshaling (OLE Automation) or use the TypeLib marshaler.
    - *Note*: If specific proxy DLLs are needed, they should be handled via explicit `<file>` entries or fragments, not registry lookups.

### Phase 2: Validation

- [x] **Task 2.1**: Clean Build Verification.
    - Run `msbuild FieldWorks.proj /t:regFreeCpp` on a machine *without* FieldWorks registered (or after unregistering `FwKernel.dll` and `Views.dll`).
    - Verify `FwKernel.X.manifest` and `Views.X.manifest` are generated.

- [x] **Task 2.2**: Manifest Content Inspection.
    - Verify `FwKernel.X.manifest` contains `<comClass>` entries pointing to `FwKernel.dll`.
    - Verify `Views.X.manifest` contains `<comClass>` entries pointing to `Views.dll`.
    - Ensure no absolute paths from the build machine are embedded.

- [x] **Task 2.3**: Runtime Verification.
    - Run `FieldWorks.exe` from `Output/Debug`.
    - Confirm it launches without "Class not registered" errors.
    - *Status*: Verified. Application launches successfully.

### Phase 3: Manifest Cleanup & Error Resolution (SxS Fixes)

**Goal**: Resolve `Activation Context generation failed` errors observed in `SxS.txt` and simplify the manifest generation logic to be more deterministic.

#### SxS Error Diagnosis (Suspected Causes)
1.  **`FwUtils.dll.MANIFEST` Validity**: The trace explicitly fails after parsing this file. It is likely generated with `processorArchitecture="msil"` or `type="win32"`, which conflicts with the x64 `FieldWorks.exe` process or other manifests in the context.
2.  **Conflicting Identities**: If `FwUtils.dll` is referenced as a dependency in `FieldWorks.exe.manifest` but the side-by-side manifest (`FwUtils.dll.MANIFEST`) declares a slightly different identity (e.g., different version or token), activation will fail.
3.  **Empty Manifest**: The wildcard generation might be creating a valid-looking but semantically empty or malformed manifest for `FwUtils.dll` if it doesn't export COM types as expected, causing the loader to reject it.
4.  **Filename Mismatch**: Windows SxS requires the manifest filename to match the `assemblyIdentity` name. `RegFree.targets` was generating `FwUtils.dll.manifest` but the identity was `FwUtils`.

#### Primary Fix Strategy
**Implement Task 3.1**: Removing the wildcard for managed assemblies will stop `FwUtils.dll.MANIFEST` from being generated. This forces the loader to use standard .NET probing, which is the correct behavior for this assembly and eliminates the conflict source.

- [x] **Task 3.1**: Disable Manifest Generation for Standard Managed Assemblies.
    - *Observation*: `SxS.txt` shows a failure parsing `FwUtils.dll.MANIFEST`. This file is generated because `RegFree.targets` includes `$(OutDir)*.dll` in `ManagedComAssemblies`.
    - *Problem*: Standard managed assemblies (like `FwUtils.dll`) do not need side-by-side manifests for simple .NET dependencies. The generated manifest likely contains conflicting `processorArchitecture` ("msil" vs "amd64") or invalid syntax for the x64 loader.
    - *Fix*: Modify `RegFree.targets` to remove the wildcard inclusion of managed assemblies. Only include managed assemblies if they are explicitly identified as COM servers needed by native code.
    - *Expected Result*: `FwUtils.dll.MANIFEST` will no longer be created. The loader will skip the manifest probe and load the DLL normally.

- [x] **Task 3.2**: Enforce Explicit Native DLL Lists.
    - *Problem*: `RegFree.targets` currently includes `$(OutDir)*.dll` for `NativeComDlls`. This is "spray and pray" and picks up non-COM DLLs, potentially creating empty or invalid manifests.
    - *Fix*: Update `RegFree.targets` to use an explicit list of known Native COM providers:
        - `Views.dll`
        - `FwKernel.dll`
        - `GraphiteEngine.dll`
        - `UniscribeEngine.dll`
    - *Benefit*: Reduces build noise and ensures we only generate manifests for actual COM servers.

- [x] **Task 3.3**: Fix Managed COM Assembly Manifests.
    - *Action*: Explicitly add `FwUtils.dll`, `SimpleRootSite.dll`, `ManagedVwDrawRootBuffered.dll`, `ManagedLgIcuCollator.dll`, `ManagedVwWindow.dll` to `ManagedComAssemblies` in `RegFree.targets`.
    - *Action*: Ensure `Platform="$(Platform)"` is used to generate `amd64` manifests for x64 builds.
    - *Action*: Ensure manifest filenames match Assembly Identity (e.g., `FwUtils.manifest` instead of `FwUtils.dll.manifest`).
    - *Status*: Completed. Manifests regenerated with `type="win64"`, `processorArchitecture="amd64"`, and correct filenames.

- [x] **Task 3.4**: Fix "clrClass not supported" Error.
    - *Problem*: `sxstrace` reported `The element clrClass appears as a child of element ... file which is not supported by this version of Windows`.
    - *Cause*: The `xsi:schemaLocation` attribute in the generated manifests triggered strict XML validation against a schema that doesn't support `clrClass` in the `asm.v1` namespace, or the schema file was missing.
    - *Fix*: Removed `xsi:schemaLocation` and `xmlns:xsi` from `RegFreeCreator.cs`.
    - *Status*: Completed. Manifests regenerated without schema location.

- [x] **Task 3.5**: Fix `clrClass` Nesting in Component Manifests.
    - *Problem*: `sxstrace` reports "The element clrClass appears as a child of element ... file".
    - *Cause*: `RegFreeCreator.cs` currently generates `<clrClass>` elements as children of the `<file>` element.
    - *Correction*: According to MSDN, `<clrClass>` must be a direct child of the `<assembly>` element in the component manifest.
    - *Action*: Modify `RegFreeCreator.cs` to move `clrClass` nodes up to the `assembly` level.
    - *Status*: Completed. `RegFreeCreator.cs` was updated to place `clrClass` elements correctly.

- [x] **Task 3.6**: Standardize EXE Integration.
    - *Action*: Audit other EXEs (`LCMBrowser.exe`, `UnicodeCharEditor.exe`) and ensure they import `RegFree.targets` with the same explicit configuration.
    - *Status*: Completed. `RegFree.targets` was updated to be generic and reusable.

### Phase 4: Runtime Stability Fixes

- [x] **Task 4.1**: Fix `InvalidCastException` in `SimpleRootSite`.
    - *Problem*: `SimpleRootSite` crashed at startup with `Unable to cast object of type 'SIL.FieldWorks.Views.VwDrawRootBuffered' to type 'SIL.FieldWorks.Common.ViewsInterfaces._VwDrawRootBufferedClass'`.
    - *Cause*: In a RegFree COM environment, when both client and server are managed code in the same process, the runtime bypasses the COM wrapper and returns the raw managed object. The code was expecting the COM wrapper class.
    - *Fix*: Modified `SimpleRootSite.cs` to instantiate `SIL.FieldWorks.Views.VwDrawRootBuffered` directly using `new`, bypassing the COM layer.
    - *Status*: Completed. Application launches successfully.

## Follow-up Tasks (Post-SxS Fix)

- [ ] **Run full test suite:** Ensure that the changes to manifest naming do not affect other parts of the system.
- [ ] **Check other projects:** Verify if any other projects use `RegFree.targets` and if they are also working correctly.
