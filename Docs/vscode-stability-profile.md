# VS Code Stability Profile (FieldWorks)

This repository is a mixed native + managed solution (C++ + .NET Framework `net48`) with traversal orchestration in `FieldWorks.proj`.

## Supported inner-loop in VS Code

- Use `ms-dotnettools.csharp` for C# language services (IntelliSense, navigation, diagnostics).
- Do **not** use C# Dev Kit (`ms-dotnettools.csdevkit`) in this workspace.
- Use `ms-vscode.cpptools` for C/C++ editing and IntelliSense.
- Build and test through repo scripts/tasks:
  - `./build.ps1`
  - `./test.ps1`

## Workspace settings rationale

- `dotnet.preferCSharpExtension=true` ensures .NET Framework projects are handled by the C# extension path.
- `dotnet.automaticallyBuildProjects=false` avoids background build churn/conflicts in large mixed-language solutions.

## Test/build authority

- **Authoritative:** `./build.ps1` and `./test.ps1`
- **VS Code Test UI:** optional/lightweight only.
- **Visual Studio Test Explorer:** preferred for complex .NET Framework test discovery/debugging.

## Native toolchain note

If `cl` is not found in a terminal, initialize the Visual Studio developer environment (or run through the repo scripts, which do this for you).
