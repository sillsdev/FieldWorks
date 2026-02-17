# VS Code Stability Profile (FieldWorks)

This repository is a mixed native + managed solution (C++ + .NET Framework `net48`) with traversal orchestration in `FieldWorks.proj`.

## Supported inner-loop in VS Code

- Use ReSharper for VS Code (`jetbrains.resharper-code`) as the default C# experience in VS Code.
- C# Dev Kit (`ms-dotnettools.csdevkit`) is discouraged in this workspace.
- Use `ms-vscode.cpptools` for C/C++ editing and IntelliSense.
- Build and test through repo scripts/tasks:
  - `./build.ps1`
  - `./test.ps1`

## Workspace settings rationale

- `dotnet.preferCSharpExtension=true` avoids activating C# Dev Kit as the default C# experience.
- `dotnet.automaticallyBuildProjects=false` avoids background build churn/conflicts in large mixed-language solutions.

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
