# VS Code Stability Profile (FieldWorks)

This repository is a mixed native + managed solution (C++ + .NET Framework `net48`) with traversal orchestration in `FieldWorks.proj`.
See [Contributing](CONTRIBUTING.md) for the recommended Visual Studio.

## Recommended Extensions and Tools

- **ReSharper for VS Code** (`jetbrains.resharper-code`) for C# editing, navigation, refactoring, and test explorer
- **Discouraged:** C# Dev Kit (`ms-dotnettools.csdevkit`) and C# (`ms-dotnettools.csharp`)
  (these do not support debugging or test discovery for projects that use .NET Framework, as this workspace still does).
- **C/C++** (`ms-vscode.cpptools`) for C/C++ IntelliSense and debugging.
- **PowerShell** (`ms-vscode.powershell`) for build and test scripts:
  - `./build.ps1`
  - `./test.ps1`

## Workspace settings rationale

- `dotnet.preferCSharpExtension=true` avoids activating C# Dev Kit as the default C# experience.
- `dotnet.automaticallyBuildProjects=false` avoids background build churn/conflicts in large mixed-language solutions.
- ReSharper build settings in `.vscode/settings.json` are pinned for stability:
  - `resharper.build.useResharperBuild=false` (delegates full build to MSBuild.exe)
  - `resharper.build.restorePackagesOnBuild=true`
  - `resharper.build.smartNugetRestore=true`
  - `resharper.build.customMsbuildPath` points to `...\\MSBuild\\Current\\Bin\\amd64\\MSBuild.exe`

## Test/build authority

- **Authoritative:** `./build.ps1` and `./test.ps1`
- **VS Code Test Explorer (ReSharper):** use for managed test discovery/execution.
- **Managed test build path:** use MSBuild-backed repo scripts (`./build.ps1 -BuildTests`, `./test.ps1`) for reliable results.
- **Visual Studio Test Explorer:** fallback for edge-case .NET Framework discovery/debugging issues.

## When to switch to Visual Studio

Use Visual Studio instead of VS Code when you need:

- WinForms designer editing (forms/resources designer workflows).
- Mixed-mode debugging across managed + native boundaries (interop/COM-heavy scenarios).
- Advanced native C++ project configuration/debugging not handled well by the VS Code launch profile.
- Complex legacy .NET Framework project-system behavior where VS Code project load or debugging becomes unreliable.

## Native toolchain note

If `cl` is not found in a terminal, initialize the Visual Studio developer environment (or run through the repo scripts, which do this for you).
