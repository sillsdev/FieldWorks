# FieldWorks COM Strategy

## Overview
FieldWorks (FLEx) relies heavily on COM interoperability between its managed C# code and native C++ components (`Views.dll`, `FwKernel.dll`, `Graphite`, etc.). Historically, this required global COM registration (`regsvr32`), which caused "DLL Hell" and made side-by-side installations impossible.

The current strategy is **Registration-Free COM (RegFree COM)** using Side-by-Side (SxS) Manifests. This allows the application to locate and load COM components without any registry entries.

## Mechanism: SxS Manifests
Instead of looking up CLSIDs in the Windows Registry (`HKCR\CLSID\...`), the Windows Loader checks the application's **Manifest**.

1.  **Application Manifest:** The executable (e.g., `FieldWorks.exe` or `vstest.console.exe` via a config/manifest) declares dependencies on specific assemblies.
2.  **Component Manifests:** Files like `Views.X.manifest` and `FwKernel.X.manifest` define the COM classes (CLSIDs) and the DLLs that implement them.
3.  **Activation Context:** When the application starts (or when a test activates a context), the OS binds these manifests together.

## Build Strategy: Debug vs. Release
To ensure consistency between Host and Container environments (and to work around a Windows Container bug with `mt.exe`), the build strategy is keyed off the **Configuration**.

| Feature | Debug Build (Host & Container) | Release Build (Host Only) |
| :--- | :--- | :--- |
| **Manifest Location** | **External** (`.manifest` file on disk) | **Embedded** (Resource #1) |
| **Tooling** | `RegFree.targets` generates the file but **skips** `mt.exe` embedding. | `mt.exe` embeds the manifest into the DLL/EXE. |
| **Runtime Behavior** | Windows Loader looks for `<Name>.manifest`. | Windows Loader prefers embedded manifests. |

**Implication for Testing:**
*   **Debug Builds:** Are now portable between Host and Container regarding COM activation. Both rely on external `.manifest` files.
*   **Release Builds:** Are blocked in Containers. On Host, they use embedded manifests (standard for deployment).

## Testing Strategy
Unit tests run in a different process (`vstest.console.exe` or `testhost.exe`) than the main application. This process does *not* automatically have the FieldWorks manifest.

### ActivationContextHelper
To solve this, FieldWorks uses `SIL.FieldWorks.Common.FwUtils.ActivationContextHelper`.
*   **Role:** Manually creates and activates a Windows Activation Context (`CreateActCtx`) for the duration of the test or fixture.
*   **Usage:** Tests that require COM must instantiate this helper, pointing it to the relevant manifest (usually `FieldWorks.Tests.manifest` or similar).

### Best Practices for Tests
1.  **Always Build and Run in the Same Environment:**
    *   If running in Docker: **Build in Docker**. This ensures external manifests are generated and used.
    *   If running on Host: **Build on Host**.
2.  **Do Not Register COM:**
    *   Avoid running `regsvr32`. It masks manifest issues and leads to "it works on my machine" failures.
3.  **Verify Manifest Generation:**
    *   Ensure `Views.X.manifest` and `FwKernel.X.manifest` exist in `Output/Debug`.
    *   Ensure `FieldWorks.Tests.manifest` (or the project-specific manifest) correctly references them.

## Troubleshooting
*   **System.BadImageFormatException (0x800700C1):** Usually means the process is 64-bit but tried to load a 32-bit DLL, OR the COM loader couldn't find the DLL specified in the manifest. In Docker, this often means the manifest is missing or the path is wrong.
*   **Class Not Registered (0x80040154):** The manifest is not active, or the CLSID is missing from the manifest. Check `ActivationContextHelper` usage.
