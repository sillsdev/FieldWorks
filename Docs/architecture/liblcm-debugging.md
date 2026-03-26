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
	Use the local package workflow driven by `./build.ps1 -LocalLcm`.
3. Need to inspect package behavior without rebuilding `liblcm` locally:
   Use Visual Studio 2022 with symbols and Source Link when available.
4. Need a quick managed-only session from VS Code:
   Use the legacy C# extension path with the existing `clr` launch configuration, but treat it as best effort.

## How FieldWorks consumes liblcm

By default, FieldWorks consumes `liblcm` through NuGet packages. The version is pinned in `Build/SilVersions.props` and flowed into `Directory.Packages.props`.

For local debugging, the supported workflow keeps FieldWorks package-backed. `build.ps1 -LocalLcm` packs your checked-out `liblcm` repo into `Output/LocalNuGetFeed`, updates `Build/SilVersions.Local.props`, and rebuilds FieldWorks against those local packages.

## Recommended workflow: Visual Studio 2022

This is the default workflow for real debugging.

Visual Studio and VS Code intentionally share build artifacts, but they do not share one debugger configuration model. In practice that means:

- use the same `build.ps1` entrypoints from either editor
- expect Visual Studio to own mixed managed/native and test-debugging scenarios
- expect VS Code to own the lightweight managed-only path through `.vscode/launch.json`

### Package-based debugging

Use this when you want to investigate the currently pinned package version without changing the dependency source.

1. Build FieldWorks with `./build.ps1`.
2. Open FieldWorks in Visual Studio 2022.
3. Start the FieldWorks host process under the debugger, or attach to the running process.
4. In Visual Studio debugger options, enable Source Link support.
5. In Visual Studio debugger options, disable Just My Code for sessions where you need to step into package code.
6. In Visual Studio symbol settings, leave symbol loading on the recommended automatic mode, then enable the NuGet.org symbol server if the package publishes symbols there.
7. Use the Modules window to verify:
   the exact `SIL.LCModel*.dll` path loaded,
   whether symbols were found,
   and whether the PDB matches the loaded binary.

Visual Studio checklist:

- Debugger type: managed for package-only inspection, mixed-mode when native boundaries matter.
- Symbols: automatic loading, NuGet.org symbol server on when useful, local symbol paths only when needed.
- Source: Source Link enabled.
- Stepping: Just My Code off for dependency stepping.

Use this path when the issue reproduces against the pinned package and you do not need to modify `liblcm` itself.

### Local package debugging with a local `liblcm` checkout

Use this when you are actively changing `liblcm` and want FieldWorks to behave like a package consumer while still stepping into your local source.

Prerequisites:

- `FW_LOCAL_LCM` points to your `liblcm` checkout.
- Build output is Debug/x64.
- The built-in VS Code `Local Packages` launchers are the full local-stack shortcut and currently assume `FW_LOCAL_PALASO`, `FW_LOCAL_LCM`, and `FW_LOCAL_CHORUS` are all set. If you only want a local `liblcm` checkout, run `./build.ps1 -LocalLcm` yourself and prefer Visual Studio for the debug session.

Steps:

1. Run `./build.ps1 -LocalLcm`.
2. Open `FieldWorks.sln` in Visual Studio 2022, or use the `FieldWorks (.NET Framework, Local Packages)` launcher in VS Code when all three local dependency repos are configured.
3. Start debugging FieldWorks.
4. If breakpoints do not bind, check the Modules window before changing any debugger settings.

Visual Studio mixed-mode note:

- If the investigation crosses into native FieldWorks code, enable native code debugging for the startup project and stay in Visual Studio for the session.

Why this works:

- FieldWorks still restores `SIL.LCModel*` packages, but they were packed from your local checkout immediately before the build.
- The local package build uses embedded PDBs, so the package already contains the matching debug information.
- Visual Studio can resolve the matching source paths from the local `liblcm` checkout without a post-build copy step.

Common reset step:

- Run `./build.ps1` without local dependency switches to remove `Build/SilVersions.Local.props` and go back to the pinned package versions.

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
5. Use the VS Code launchers in this repo, which prebuild managed projects with portable PDBs and keep package mode and local-package mode explicit.
6. Ensure the local dependency build completed successfully so the feed under `Output/LocalNuGetFeed` contains the expected `SIL.LCModel*` packages.
7. Prefer the diagnostics launchers first when symbols do not bind; they log module loads and make symbol resolution problems easier to see.

VS Code checklist:

- Launch type: `clr`
- Platform: x64 only
- Symbols: portable PDBs, `justMyCode: false`, symbols searched next to the built outputs under `Output/Debug`, with NuGet.org available when package symbols exist
- Editor integration: classic C# extension preferred, not C# Dev Kit

### VS Code launch workflow

1. Build FieldWorks with `./build.ps1`.
2. Choose `FieldWorks (.NET Framework, Package)` when you want the pinned package path.
3. Choose `FieldWorks (.NET Framework, Local Packages)` only when `FW_LOCAL_PALASO`, `FW_LOCAL_LCM`, and `FW_LOCAL_CHORUS` are all configured, because that launcher runs the repo's full local-package shortcut. If you only need local `liblcm`, run `./build.ps1 -LocalLcm` manually and use Visual Studio.
4. Use the `Diagnostics` variants when you need module-load evidence or symbol troubleshooting.
5. Keep `justMyCode` disabled when stepping into `liblcm`.
6. The VS Code launchers first run `Prepare Debug (*)`, which checks the last successful debug-build stamp and skips the build when no relevant workspace files changed. For local-package mode it also checks whether the selected local dependency repos changed since the last successful debug build.
7. If another tool rewrites the FieldWorks launch binary or its matching PDB later, such as a Visual Studio rebuild in the same worktree, the prelaunch helper treats those newer launch outputs as unstamped and rebuilds before launch.
8. Do not switch the VS Code debug path to Windows PDBs. The debugger used here requires portable PDBs.
9. If symbols still do not bind, inspect the loaded binaries and symbol paths before changing code.

What VS Code does not try to share with Visual Studio:

- startup project selection
- mixed-mode/native debug flags
- test debugging integration
- symbol cache and debugger option state

Those remain editor-specific by design.

Important boundary:

- Local package mode is for diagnosis, development, and local verification.
- Package-backed builds remain the final merge and CI validation path.

Practical limit:

- This path is best effort only. If the session turns into mixed managed/native debugging, move to Visual Studio.
- Do not use the old `dotnet test`-style VS Code launcher pattern for this repo's .NET Framework tests. Use `test.ps1` for normal test runs and Visual Studio when you need interactive test debugging.

## NuGet package versus local packages

### When to stay on the package

Stay on the package when:

- you are reproducing a bug in the version currently pinned by FieldWorks,
- Source Link and symbols are already good enough,
- you only need to understand behavior, not change `liblcm`.

### When to switch to local package mode

Switch to local package mode when:

- you are modifying `liblcm`,
- you want FieldWorks to restore a package built from your local checkout,
- you need matching symbols without a manual copy step,
- you want the default pinned-package workflow to remain untouched when local mode is off.

## Build entrypoints

- `./build.ps1` uses the pinned package versions from `Build/SilVersions.props`.
- `./build.ps1 -LocalLcm` packs your local `liblcm` checkout and rebuilds FieldWorks against that package.
- `./build.ps1 -LocalPalaso -LocalLcm -LocalChorus` packs all three local dependency repos in the supported build order.
- `./test.ps1` accepts the same local dependency switches and forwards them to `build.ps1`.
- `FieldWorks.sln` remains the Visual Studio solution for both pinned-package and local-package workflows.

## Failure modes to check first

If debugging does not behave as expected, check these before changing tool settings:

1. Wrong binary loaded:
   verify the loaded `SIL.LCModel*.dll` path.
2. PDB mismatch:
   verify that the PDB came from the same build as the DLL.
3. Package cache confusion:
   stale copies under `%USERPROFILE%\.nuget\packages` or `packages\` can mislead assumptions.
4. Architecture mismatch:
   keep the workflow x64 end to end.

## Minimal repo changes that improve this workflow

This repo now uses a package-only inner loop, but a few small additions could still make the workflow more obvious and less fragile.

### Recommended now

1. Keep a dedicated `liblcm` debugging guide in the repo.
2. Keep a VS Code launch configuration named for `.NET Framework` instead of a generic `FieldWorks` label.
3. Keep workspace settings biased toward the legacy C# extension for this repo.

### Recommended later if the team wants a smoother inner loop

1. Add a script that reports which `SIL.LCModel*.dll` files are currently loaded and where they came from.
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