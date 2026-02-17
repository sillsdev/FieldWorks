# Registration-Free COM Manifest Investigation

## 1. Problem Description

The FieldWorks application (`FieldWorks.exe`) fails to start with a Side-by-Side (SxS) configuration error (Event ID 72).

**Error Message:**
> "The element clrClass appears as a child of element urn:schemas-microsoft-com:asm.v1^file which is not supported by this version of Windows."

**Context:**
This occurs after migrating the build system to use MSBuild Traversal SDK and attempting to run the application. The error indicates a structural violation in the generated application manifest.

## 2. Microsoft Guidance & Core Documentation

According to Microsoft documentation on Registration-Free COM with .NET assemblies:

1.  **Structure**: A .NET assembly exposing COM types (via `[ComVisible(true)]`) must have its own manifest file (e.g., `MyAssembly.dll.manifest`).
2.  **Component Manifest**: This component manifest contains the `<clrClass>` elements mapping COM CLSIDs to .NET types.
3.  **Application Manifest**: The main application manifest (`FieldWorks.exe.manifest`) should **not** contain `<clrClass>` elements directly under `<file>` elements for the assemblies. Instead, it should reference the component manifests using `<dependency>` and `<dependentAssembly>` elements.

**Correct Structure (Component Manifest - `MyAssembly.manifest`):**
```xml
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <assemblyIdentity name="MyAssembly" version="1.0.0.0" type="win32" processorArchitecture="msil" />
    <file name="MyAssembly.dll">
        <clrClass clsid="{...}" progid="..." threadingModel="Both" name="MyNamespace.MyClass" runtimeVersion="v4.0.30319" />
    </file>
</assembly>
```

**Correct Structure (Application Manifest - `FieldWorks.exe.manifest`):**
```xml
<assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0">
    <assemblyIdentity name="FieldWorks.exe" version="..." type="win32" processorArchitecture="amd64" />
    <dependency>
        <dependentAssembly>
            <assemblyIdentity name="MyAssembly" version="1.0.0.0" type="win32" processorArchitecture="msil" />
        </dependentAssembly>
    </dependency>
</assembly>
```

**The Violation:**
The current build generates a "monolithic" manifest where `<clrClass>` elements are embedded directly inside the `<file>` element of the application manifest, which is invalid for `clrClass` elements in the application manifest context in modern Windows versions (or specifically when mixed with other manifest types). While `<comClass>` (for native COM) is allowed under `<file>` in the app manifest, `<clrClass>` requires a separate component manifest context.

## 3. Current Implementation Analysis

### `RegFree.targets`
The current MSBuild targets (`Build\RegFree.targets`) treat all DLLs similarly:
```xml
<Target Name="CreateManifest" Condition="'$(OS)'=='Windows_NT'">
    <RegFree
        Executable="$(Executable)"
        DependentAssemblies="@(DependentAssemblies)"
        Dlls="@(NativeComDlls)"
        ManagedAssemblies="@(ManagedComAssemblies)"
        AsIs="@(Fragments)"
        Platform="$(Platform)"
    />
</Target>
```
It passes a list of `ManagedAssemblies` to the `RegFree` task, expecting them to be merged into the main executable's manifest.

### `RegFreeCreator.cs`
The C# task (`Build\Src\FwBuildTasks\RegFreeCreator.cs`) implements `ProcessManagedAssembly`:
1.  It opens the managed assembly using `System.Reflection.Metadata`.
2.  It finds public, COM-visible classes.
3.  It calls `GetOrCreateFileNode` to create a `<file>` element in the **main** document.
4.  It appends `<clrClass>` elements as children of this `<file>` element.

```csharp
// Inside ProcessManagedAssembly
var file = GetOrCreateFileNode(parent, fileName);
// ...
AddOrReplaceClrClass(file, clsId, "Both", typeName, progId, runtimeVersion);
```

This logic produces the invalid XML structure:
```xml
<assembly ...>
  <file name="DotNetZip.dll">
    <clrClass ... /> <!-- INVALID HERE -->
  </file>
</assembly>
```

## 4. Gap Analysis

| Feature | Current Implementation | Required Implementation |
| :--- | :--- | :--- |
| **Managed COM Definition** | Embedded in App Manifest | Separate Component Manifests |
| **App Manifest Reference** | `<file><clrClass>...</file>` | `<dependency><dependentAssembly>...</dependentAssembly>` |
| **Build Process** | Single pass (App Manifest) | Multi-pass (Component Manifests -> App Manifest) |

The current implementation assumes a "flat" manifest style that works for native COM (`<comClass>`) but violates the requirements for managed COM (`<clrClass>`).

## 5. Proposed Solution

To resolve this, we must refactor the build process to generate separate manifests for managed assemblies that expose COM types.

### Step 1: Modify `RegFree.targets`
We need to split the manifest generation into two phases:
1.  **Component Manifest Generation**: Iterate over `ManagedComAssemblies` and generate a `.manifest` file for each one.
2.  **Application Manifest Generation**: Generate the app manifest, but instead of embedding the managed assemblies, treat them as `DependentAssemblies`.

**Draft Logic for `RegFree.targets`:**
```xml
<!-- Phase 1: Generate manifests for managed DLLs -->
<Target Name="CreateComponentManifests" Outputs="%(ManagedComAssemblies.Identity)">
    <RegFree
        Executable="%(ManagedComAssemblies.Identity)"
        Output="$(OutDir)%(ManagedComAssemblies.Filename)%(ManagedComAssemblies.Extension).manifest"
        ManagedAssemblies="%(ManagedComAssemblies.Identity)"
        Platform="$(Platform)"
    />
</Target>

<!-- Phase 2: Generate App manifest referencing the above -->
<Target Name="CreateManifest" DependsOnTargets="CreateComponentManifests">
    <ItemGroup>
        <!-- Add the generated manifests to DependentAssemblies -->
        <DependentAssemblies Include="@(ManagedComAssemblies->'$(OutDir)%(Filename)%(Extension).manifest')" />
    </ItemGroup>
    <RegFree
        Executable="$(Executable)"
        DependentAssemblies="@(DependentAssemblies)"
        Dlls="@(NativeComDlls)"
        ManagedAssemblies="" <!-- Clear this so they aren't embedded -->
        ...
    />
</Target>
```

### Step 2: Verify `RegFree` Task Support
We need to ensure the `RegFree` task can handle generating a manifest *for a DLL*.
-   The `Executable` property is used to set the `assemblyIdentity`.
-   If `Executable` points to a DLL, `RegFreeCreator.CreateExeInfo` needs to ensure it sets the `type` correctly (e.g., `win32` is usually fine, but the name should match the DLL).
-   The `ProcessManagedAssembly` logic works by adding to the passed `XmlDocument`. If we pass a fresh document for the DLL manifest, it should correctly generate the `<assembly><file><clrClass>...</file></assembly>` structure, which *is* valid for a component manifest.

### Step 3: Clean Up
-   Ensure `NativeComDlls` does not overlap with `ManagedComAssemblies`.
-   Ensure the generated manifests are deployed/available next to the executable.

## 6. Decision
We will modify `Build\RegFree.targets` to implement the multi-pass manifest generation strategy. This avoids complex C# code changes in `RegFreeCreator.cs` (which already knows how to generate the XML content) and leverages MSBuild to orchestrate the file separation.
