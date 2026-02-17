# Unresolved Reference Remediation Plan

This plan standardizes how we eliminate MSBuild unresolved-reference warnings (MSB3245/MSB3246) and bad-image issues across FieldWorks. It focuses on predictable inputs, modern dependency patterns, and repeatable validation.

## Scope and goals
- Cover native/managed artifacts required during restore, ResolveAssemblyReferences, and test execution.
- Replace ad-hoc HintPaths to shared output folders with deterministic sources (project/package).
- Ensure transform assemblies (ApplicationTransforms/PresentationTransforms) and native prerequisites are produced even for single-project builds.
- Keep solutions aligned with current MSBuild guidance: prefer ProjectReference within the repo, PackageReference for third-party bits, and avoid opaque binary drops.

## Current issues (observed)
- `ApplicationTransforms.dll` / `PresentationTransforms.dll`: missing when building isolated projects → MSB3246.
- `Utilities` assembly reference in `ScriptureUtils.csproj` → MSB3245 (assembly not present).
- Occasional native prereq misses when skipping the native phase (e.g., ViewsInterfaces EnsureNativeArtifacts).

## Resolution strategy
1. **Source of truth for assemblies**
   - In-repo outputs → use `<ProjectReference>`; never point at `$(dir-outputBase)` HintPaths.
   - Third-party → use `<PackageReference>` with explicit versions; set `IncludeAssets`/`PrivateAssets` as needed instead of dropping binaries.

2. **Transforms (Application/Presentation)**
   - Build via `BuildWindowsXslAssemblies` (or copy fallback) before managed reference resolution.
   - Enforce via `Directory.Build.targets` (target `EnsureTransformsForManagedBuilds`), so single-project builds and tests get the artifacts.
   - Runtime fallback already loads unpacked XSL files if precompiled assemblies are absent (XmlUtils).

3. **Native prerequisites**
   - Default path: run the traversal (`FieldWorks.proj`) which builds `NativeBuild` first.
   - For targeted builds, rely on `ViewsInterfaces` `EnsureNativeArtifacts` guard; if native artifacts are missing, build `Build/Src/NativeBuild/NativeBuild.csproj` (Debug/x64).
   - Avoid duplicating native binaries in managed output; keep them produced by the native phase only.

4. **Project-by-project remediation steps**
   - Identify warning source (project + assembly) from `warnings.{json,csv}`.
   - Map the producer:
     - If in-repo project exists → switch to `<ProjectReference>`.
     - If NuGet provides it → add `<PackageReference>` with pinned version.
     - If neither exists → create a minimal shim project in-repo rather than committing binaries.
   - Remove stale `<Reference Include="...">` + HintPath entries once the new source is in place.

5. **Utilities assembly (ScriptureUtils)**
   - Confirm whether `Utilities.Enum<T>` comes from a NuGet package (preferred) or an in-repo project.
   - If from Paratext packages: add the correct `PackageReference` that provides the `Utilities` assembly and drop the raw `<Reference Include="Utilities" />`.
   - If functionality is trivial: replace usage with an equivalent in existing packages (e.g., `System.Enum` helpers) and remove the reference.

6. **Validation**
   - Run `./build.ps1 -Configuration Debug` (container-aware) to exercise traversal ordering.
   - For focused checks, `msbuild <project>.csproj /t:Restore,ResolveAssemblyReferences /p:Configuration=Debug /p:Platform=x64` to confirm RAR is clean.
   - Re-run `test.ps1` or relevant test projects after dependency fixes.
   - Keep `warnings.json`/`warnings.csv` clean; any remaining MSB3245/3246 should block merge until mapped to a plan item.

## References (industry guidance)
- Microsoft docs: prefer `<ProjectReference>` for intra-solution dependencies; use `<PackageReference>` for external dependencies; avoid direct DLL HintPaths (maintainability + binding redirect stability).
- .NET SDK/MSBuild guidance: keep build artifacts in `bin/obj` or centralized output, not as inputs; use deterministic restore + build ordering to satisfy `ResolveAssemblyReferences`.

## Action items
- [ ] ScriptureUtils: replace `Utilities` reference with the proper package or inline helper; drop the bare assembly reference.
- [ ] Confirm no managed project uses output HintPaths for transforms; rely on the orchestrated build + fallback.
- [ ] Document any unavoidable binary-only deps and track them for future replacement with packages.