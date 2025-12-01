# Registration-Free COM Findings

## Current Status (2025-11-20)

### Issue: Application Startup Failure
`FieldWorks.exe` fails to start with a Side-by-Side (SxS) configuration error.

### Error Details
**Event Log ID 33 (SideBySide):**
```
Activation context generation failed for "C:\Users\johnm\Documents\repos\FieldWorks\Output\Debug\FieldWorks.exe".
Error in manifest or policy file "C:\Users\johnm\Documents\repos\FieldWorks\Output\Debug\FwUtils.dll.MANIFEST" on line 5.
The element clrClass appears as a child of element urn:schemas-microsoft-com:asm.v1^file which is not supported by this version of Windows.
```

### Artifacts
**FwUtils.dll.MANIFEST (Generated):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns:asmv1="urn:schemas-microsoft-com:asm.v1" xmlns:asmv2="urn:schemas-microsoft-com:asm.v2" xmlns:dsig="http://www.w3.org/2000/09/xmldsig#" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:schemaLocation="urn:schemas-microsoft-com:asm.v1 assembly.adaptive.xsd" xmlns="urn:schemas-microsoft-com:asm.v1">
	<assemblyIdentity name="FwUtils.dll" version="9.3.4.45981" type="win64" processorArchitecture="amd64" />
	<file name="FwUtils.dll" asmv2:size="218112">
		<clrClass clsid="{17a2e876-2968-11e0-8046-0019dbf4566e}" threadingModel="Both" name="SIL.FieldWorks.Common.FwUtils.ManagedPictureFactory" runtimeVersion="v4.0.30319" />
	</file>
</assembly>
```

### Analysis
1.  **Manifest Structure**: The `clrClass` element is correctly placed as a child of `file`.
2.  **Namespace**: The `file` element is in the default namespace (`urn:schemas-microsoft-com:asm.v1`).
3.  **Error Interpretation**: The error "element clrClass ... is not supported" usually indicates a schema validation failure or that the context in which `clrClass` is used is invalid for the active activation context.
4.  **Hypothesis**:
    *   There might be a conflict if `FwUtils.dll` already has an embedded manifest that doesn't match, or if the external manifest is being prioritized but is considered invalid.
    *   The `runtimeVersion` might be problematic if it doesn't match the actual runtime exactly, though `v4.0.30319` is standard for .NET 4.x.
    *   **Crucial**: The `assemblyIdentity` name in `FwUtils.dll.MANIFEST` is `FwUtils.dll`. Usually, for a library, the name should be just `FwUtils` (without .dll), although including .dll is sometimes seen. However, the filename of the manifest is `FwUtils.dll.MANIFEST`.
    *   In `FieldWorks.exe.manifest`, the dependency is:
        ```xml
        <dependentAssembly asmv2:codebase="FwUtils.dll.manifest">
            <assemblyIdentity name="FwUtils.dll" version="9.3.4.45981" ... />
        </dependentAssembly>
        ```
    *   If the assembly name is `FwUtils.dll`, that matches.

### Actions Taken
1.  **Rebuild**: Ran `rebuild_fw_exe.ps1` to force regeneration of `FieldWorks.exe` and its manifest.
    *   Result: Build successful (with warnings), but runtime error persists.
2.  **Manual Inspection**: Verified manifest contents match the error location.

### Next Steps
1.  Investigate the `clrClass` validity in `asm.v1` namespace.
2.  Check if `FwUtils.dll` has an embedded manifest.
3.  Try removing `.dll` from the `assemblyIdentity` name in `FwUtils.dll.MANIFEST` (and updating `FieldWorks.exe.manifest` to match) to see if it's a naming issue.
4.  Validate `RegFreeCreator.cs` logic for generating these manifests.
