# Registration-Free COM Best Practices

## Reference
[Configure .NET Framework-Based COM Components for Registration-Free Activation](https://learn.microsoft.com/en-us/dotnet/framework/interop/configure-net-framework-based-com-components-for-reg?redirectedfrom=MSDN)

## Core Concepts

### 1. Separation of Concerns
Registration-free COM for .NET components requires two distinct types of manifests:
*   **Application Manifest**: Embedded in the executable (e.g., FieldWorks.exe.manifest). It simply declares a dependency on the managed component.
*   **Component Manifest**: Embedded in the managed assembly (e.g., FwUtils.manifest). It describes the COM classes exported by that assembly.

### 2. Application Manifest Structure
The application manifest should **not** describe the classes. It only references the component assembly.

`xml
<dependency>
    <dependentAssembly>
        <assemblyIdentity name="FwUtils" version="9.3.4.45981" processorArchitecture="amd64" type="win64" />
    </dependentAssembly>
</dependency>
`

### 3. Component Manifest Structure
The component manifest is where the clrClass elements live. Crucially, **clrClass must be a direct child of the ssembly element**, not nested inside a ile element.

**Correct Structure:**
`xml
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <assemblyIdentity name="FwUtils" version="9.3.4.45981" ... />

    <!-- clrClass is a child of assembly -->
    <clrClass
        clsid="{...}"
        progid="SIL.FieldWorks.Common.FwUtils.ManagedPictureFactory"
        threadingModel="Both"
        name="SIL.FieldWorks.Common.FwUtils.ManagedPictureFactory"
        runtimeVersion="v4.0.30319">
    </clrClass>

    <file name="FwUtils.dll">
        <!-- No clrClass here -->
    </file>
</assembly>
`

**Incorrect Structure (Causes SxS Error):**
`xml
<assembly ...>
    <file name="FwUtils.dll">
        <!-- Error: clrClass cannot be a child of file -->
        <clrClass ... />
    </file>
</assembly>
`

### 4. Embedding
The component manifest must be embedded as a resource (RT_MANIFEST, ID 1) within the managed assembly itself. This allows the CLR to find the definition when the application loads the assembly via the dependency.
