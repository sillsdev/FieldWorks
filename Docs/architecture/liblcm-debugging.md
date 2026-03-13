# Debugging liblcm from FieldWorks

This guide documents the practical debugging workflow for `liblcm` when it is used by FieldWorks.

FieldWorks is a Windows/x64, .NET Framework 4.8 desktop application with both managed and native code. That matters because the debugger choice is not interchangeable:

- Visual Studio 2022 is the supported debugger for the full FieldWorks plus `liblcm` scenario.
- VS Code can help for editing, navigation, and some limited managed-only sessions, but it is not the primary debugger for mixed managed/native work.

## Choose the right path

Use this decision tree first:

1. Need to step between C# and native C++ or debug a process that loads native DLLs:
   Use Visual Studio 2022.
2. Need trustworthy source-level debugging into your local `liblcm` changes:
	Use `FieldWorks.LocalLcm.sln` in Visual Studio or the local-LCM launcher/task pair in VS Code.
3. Need to inspect package behavior without rebuilding `liblcm` locally:
   Use Visual Studio 2022 with symbols and Source Link when available.
4. Need a quick managed-only session from VS Code:
   Use the legacy C# extension path with the existing `clr` launch configuration, but treat it as best effort.

## How FieldWorks consumes liblcm

By default, FieldWorks consumes `liblcm` through NuGet packages. The version is pinned in `Build/SilVersions.props` and flowed into `Directory.Packages.props`.

For local debugging, the preferred workflow is now local source mode with a nested checkout at `Localizations/LCM`. In that mode, FieldWorks switches from the `SIL.LCModel*` packages to local project references and local `liblcm` artifacts.

## Recommended workflow: Visual Studio 2022

This is the default workflow for real debugging.

### Package-based debugging

Use this when you want to investigate the currently pinned package version without changing the dependency source.

1. Build FieldWorks with `./build.ps1`.
2. Open FieldWorks in Visual Studio 2022.
3. Start the FieldWorks host process under the debugger, or attach to the running process.
4. In Visual Studio debugger options:
   Enable Source Link support.
5. In Visual Studio debugger options:
   Disable Just My Code for sessions where you need to step into package code.
6. In Visual Studio symbol settings:
   Enable the NuGet.org symbol server if the package publishes symbols there.
7. Use the Modules window to verify:
   the exact `SIL.LCModel*.dll` path loaded,
   whether symbols were found,
   and whether the PDB matches the loaded binary.

Use this path when the issue reproduces against the pinned package and you do not need to modify `liblcm` itself.

### Local-source debugging with nested checkout

Use this when you are actively changing `liblcm` or you need exact source and symbol fidelity.

Prerequisites:

- `liblcm` is cloned at `Localizations/LCM`.
- Build output is Debug/x64.

Steps:

1. Open `FieldWorks.LocalLcm.sln` in Visual Studio 2022.
2. Build the solution. If the local liblcm build tasks have not been generated yet, the first build bootstraps them into `Localizations/LCM/artifacts/<Configuration>/net462`.
3. Start debugging FieldWorks.
4. If breakpoints do not bind, check the Modules window before changing any debugger settings.

Why this works:

- FieldWorks compiles against local `liblcm` projects instead of the published packages.
- Shared build-time artifacts come from the local `liblcm` checkout.
- Visual Studio can resolve the matching local PDBs without a post-build copy step.

Common reset step:

- Switch back to `FieldWorks.sln` in Visual Studio or use the package-backed launcher/task in VS Code.

## Limited workflow: VS Code

VS Code is useful here, but with narrower expectations.

Use VS Code only for these cases:

- editing and navigation,
- quick managed-only debugging of the FieldWorks host,
- validating that breakpoints bind against local managed `liblcm` symbols,
- tracing and log-driven investigation.

Do not treat VS Code as the primary workflow for:

- mixed managed/native debugging,
- stepping across native boundaries,
- debugger-driven investigation of registration-free COM issues,
- anything that depends on Visual Studio's Modules and mixed-mode support.

### VS Code prerequisites

1. Keep `dotnet.preferCSharpExtension` enabled in workspace settings.
2. Use the Microsoft C# extension path, not C# Dev Kit, for this repo.
3. Use the `clr` launch type for the .NET Framework host executable.
4. Stay x64 only.
5. Ensure matching PDBs are present next to the loaded `SIL.LCModel*.dll` files.
6. Use the VS Code launchers in this repo, which prebuild managed projects with portable PDBs and refresh the runtime LCM copies in `Output/Debug`.

### VS Code launch workflow

1. Build FieldWorks with `./build.ps1`.
2. Choose `FieldWorks (.NET Framework, Package)` when you want the pinned package path.
3. Choose `FieldWorks (.NET Framework, Local LCM)` when you want the nested `Localizations/LCM` path.
4. Keep `justMyCode` disabled when stepping into `liblcm`.
5. The VS Code launchers first run `Prepare Debug (*)`, which checks the last successful debug-build stamp and skips the build when no relevant saved files changed.
6. Do not switch the VS Code debug path to Windows PDBs. The debugger used here requires portable PDBs.
7. If symbols still do not bind, inspect the loaded binaries in `Output/Debug` before changing code.

Practical limit:

- This path is best effort only. If the session turns into mixed managed/native debugging, move to Visual Studio.

## NuGet package versus local source

### When to stay on the package

Stay on the package when:

- you are reproducing a bug in the version currently pinned by FieldWorks,
- Source Link and symbols are already good enough,
- you only need to understand behavior, not change `liblcm`.

### When to switch to local source mode

Switch to local source mode when:

- you are modifying `liblcm`,
- you want a checked-out `liblcm` solution in the same Visual Studio session,
- you need build-time artifacts to come from the local checkout,
- you want the package-backed mode to remain untouched when local mode is off.

## Build entrypoints

- `./build.ps1 -LcmMode Package` forces the package-backed path.
- `./build.ps1 -LcmMode Local` forces the nested `Localizations/LCM` path and bootstraps the local build tasks when needed.
- `./build.ps1 -LcmMode Auto` stays package-backed by default, but prints whether local LCM inputs are available.
- `FieldWorks.sln` stays package-backed in Visual Studio.
- `FieldWorks.LocalLcm.sln` enables local source mode in Visual Studio.

## Failure modes to check first

If debugging does not behave as expected, check these before changing tool settings:

1. Wrong binary loaded:
   verify the loaded `SIL.LCModel*.dll` path.
2. PDB mismatch:
   verify that the PDB came from the same build as the DLL.
3. Package cache confusion:
   stale copies under `%USERPROFILE%\.nuget\packages` can mislead assumptions.
4. Architecture mismatch:
   keep the workflow x64 end to end.

## Minimal repo changes that improve this workflow

The current repo is close, but a few small changes make the workflow more obvious and less fragile.

### Recommended now

1. Keep a dedicated `liblcm` debugging guide in the repo.
2. Keep a VS Code launch configuration named for `.NET Framework` instead of a generic `FieldWorks` label.
3. Keep workspace settings biased toward the legacy C# extension for this repo.

### Recommended later if the team wants a smoother inner loop

1. Add a script that reports which `SIL.LCModel*.dll` files are currently loaded into `Output/Debug` and where they came from.
2. Add a short troubleshooting checklist for symbol binding and package-cache mismatches.
3. If package-debugging becomes a common workflow, standardize symbol publishing and Source Link expectations for the SIL packages.

## References

- Visual Studio mixed-mode debugging:
  https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-in-mixed-mode?view=vs-2022
- Visual Studio symbols and source files:
  https://learn.microsoft.com/en-us/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=vs-2022
- Visual Studio decompilation:
  https://learn.microsoft.com/en-us/visualstudio/debugger/decompilation?view=vs-2022
- Source Link guidance:
  https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/sourcelink
- NuGet symbol packages:
  https://learn.microsoft.com/en-us/nuget/create-packages/symbol-packages-snupkg
- VS Code C# Dev Kit FAQ:
  https://code.visualstudio.com/docs/csharp/cs-dev-kit-faq
- VS Code C# debugging:
  https://code.visualstudio.com/docs/csharp/debugging
- VS Code C# extension desktop .NET Framework note:
  https://github.com/dotnet/vscode-csharp/blob/main/docs/debugger/Desktop-.NET-Framework.md
- Damir Arh on debugging libraries from NuGet:
  https://www.damirscorner.com/blog/posts/20250411-DebuggingLibrariesFromNuGet.html