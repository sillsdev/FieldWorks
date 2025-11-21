# Contract: MSBuild RegFree Target (integration)

Purpose: Provide a shared build target that generates registration-free COM manifests for EXE projects.

Inputs (MSBuild properties/items):
- EnableRegFreeCom (bool, default true)
- RegFreePlatform (string, e.g., win64)
- NativeComDlls (ItemGroup): DLLs to scan in $(TargetDir)

Behavior:
- AfterTargets="Build"; when OutputType == WinExe and EnableRegFreeCom == true, invoke RegFree task to update $(TargetPath).manifest.
- Ensures a minimal manifest exists; RegFree augments with COM entries.

Outputs:
- $(TargetPath).manifest updated/created with COM activation entries

Non-goals:
- No registry writes or regsvr32 calls during build

Verification:
- Build logs show RegFree invocation;
- Resulting manifest contains <file>/<comClass>/<typelib> entries for expected servers.
