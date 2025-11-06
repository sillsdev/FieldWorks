# FieldWorks64-bit only + Registration-free COM migration plan

Owner: Build/Infra
Status: Draft

Goals
- Drop32-bit (x86/Win32) builds; ship and run64-bit only.
- Eliminate COM registration at install/dev time. All COM activation should succeed via application manifests (registration-free COM) so the app is self-contained and runnable without regsvr32/admin.

Context (what we found)
- Native COM servers and interop:
 - COM classes are implemented in native DLLs and registered by generic `ModuleEntry`/`GenericFactory` plumbing (DllRegisterServer/DllInstall) under CLSID/ProgID (see `Src/Generic/*`).
 - Managed interop stubs are generated from IDL with `SIL.IdlImporter` in `ViewsInterfaces` (`BuildInclude.targets` runs `idlimport`). Managed code creates COM objects with `[ComImport]` coclasses, e.g. `_DebugReportClass`, `VwGraphicsWin32`, `VwCacheDa` (see `Src/Common/ViewsInterfaces/Views.cs`).
- Reg-free infrastructure exists:
 - `Build/Src/FwBuildTasks/RegFree.cs` + `RegFreeCreator.cs` generate application manifests from COM type libraries and a redirected temporary registry. They:
 - Temporarily call `DllInstall(user)` into a HKCU-redirected hive, inspect CLSIDs/Interfaces/Typelibs, generate `<file>`, `<comClass>`, `<typelib>`, and `<comInterfaceExternalProxyStub>` entries, and optionally unregister.
 - `RegisterForTestsTask.cs` registers DLLs for tests but is not required if we switch tests/exes to reg-free.
- Current builds still include dual-platform configs in many csproj (Debug/Release for x86 & x64) and native projects likely still have Win32 configurations.

Non-goals (for this phase)
- Changing the IDL/COM surface or marshaling.
- Installer modernization (WiX) beyond removing COM registration steps and including manifests.

Plan overview
A) Enforce64-bit everywhere (managed + native + solution/CI)
B) Produce and ship registration-free COM manifests for every EXE that activates COM (FieldWorks.exe, LexText.exe, tools/tests that create COM objects)
C) Remove registration steps from dev build/run and tests; keep `RegFree` manifest generation as the only COM-related build step.

Details

A) Move to64-bit only
1) Central MSBuild defaults
- Add `Directory.Build.props` at the solution root:
 - `<PlatformTarget>x64</PlatformTarget>` for all managed projects unless explicitly overridden.
 - `<Platforms>x64</Platforms>` for solution-wide consistency where applicable.
- For projects that currently set `PlatformTarget` conditionally per-configuration (`Debug|x86`, `Release|x86`), remove x86 property groups and keep `Debug|x64`/`Release|x64` only. Example (from `ViewsInterfaces.csproj`) shows both x86/x64 groups – keep x64, delete x86.
- Ensure AnyCPU isn’t used for processes that host COM (prefer explicit x64 to avoid WOW32 confusion).

2) Native (C++14) projects (vcxproj)
- Remove Win32 configurations and keep only `x64` for all native COM servers (Views, FwKernel, engines, etc.).
- Validate MIDL/proxy settings produce64-bit compatible outputs; keep `_MERGE_PROXYSTUB` where it is in use (we rely on reg-free to reference proxies if produced separately).

3) Solution + CI
- Update `FieldWorks.sln` to remove Win32 platforms and keep `x64` (Debug/Release). If other solutions exist, do the same.
- Update CI/build scripts to call: `msbuild FieldWorks.sln /m /p:Configuration=Debug /p:Platform=x64`.
- Remove any32-bit specific paths or tools (e.g., SysWOW64 regsvr32) from build/targets.

B) Registration‑free COM (no regsvr32)
1) Identify COM servers to include in manifests
- All native DLLs that export COM classes or proxies typelibs. Based on interop usage and project layout:
 - Views (e.g., `Views.dll`, classes: `VwRootBox`, `VwGraphicsWin32`, `VwCacheDa`, etc.)
 - Kernel (`FwKernel.dll` – typelibs and interfaces in `Src/Kernel/FwKernel.idh`)
 - Render engines (`UniscribeEngine.dll`, `GraphiteEngine.dll`)
 - Other COM servers referenced by generated interop in `Views.cs` (scan for `[ComImport]` coclasses’ implementing DLLs during implementation phase).
- We will build the initial list by enumerating native output dirs for DLLs and letting `RegFree` filter down to those with type libraries/COM registration entries. This is robust against drift.

2) Add a shared MSBuild target to generate manifests
- Create `Build/Targets/RegFree.targets` with a property switch `EnableRegFreeCom` (default true):
 - Define per-EXE ItemGroups listing native DLLs to process into the EXE’s manifest. Start broad (all native DLLs in the output directory) then narrow if needed.
 - Invoke the existing `RegFree` task after each EXE build to update `<TargetPath>.manifest`.
 - Example (sketch):
 ```xml
 <Project>
 <UsingTask TaskName="SIL.FieldWorks.Build.Tasks.RegFree" AssemblyFile="$(MSBuildThisFileDirectory)..\..\Src\FwBuildTasks\bin\$(Configuration)\SIL.FieldWorks.Build.Tasks.dll" />

 <PropertyGroup>
 <EnableRegFreeCom Condition=" '$(EnableRegFreeCom)' == '' ">true</EnableRegFreeCom>
 <RegFreePlatform>win64</RegFreePlatform>
 </PropertyGroup>

 <ItemGroup Condition=" '$(EnableRegFreeCom)' == 'true' ">
 <!-- Broad include: all native dlls next to the exe; adjust as needed -->
 <NativeComDlls Include="$(TargetDir)*.dll" />
 <!-- Optional fragments or dependent assemblies can be added here later -->
 </ItemGroup>

 <Target Name="GenerateRegistrationFreeManifest" AfterTargets="Build" Condition=" '$(EnableRegFreeCom)' == 'true' and '$(OutputType)' == 'WinExe' ">
 <!-- Ensure a basic manifest exists (create minimal if missing) -->
 <WriteLinesToFile File="$(TargetPath).manifest" Lines='&lt;assembly xmlns="urn:schemas-microsoft-com:asm.v1" manifestVersion="1.0" /&gt;' Overwrite="False" />

 <RegFree
 Executable="$(TargetPath)"
 Output="$(TargetPath).manifest"
 Platform="$(RegFreePlatform)"
 Dlls="@(NativeComDlls)"
 Unregister="true" />
 </Target>
 </Project>
 ```
- Import this target in each EXE csproj that activates COM (e.g., `Src/Common/FieldWorks/FieldWorks.csproj`, `Src/LexText/LexTextExe/LexTextExe.csproj`, tools/test exes). For tests producing executables, include the same target so they also run reg-free.

3) Packaging/runtime layout
- Keep native COM DLLs in the same directory as the EXE (or reference with `codebase` in the manifest). The `RegFree` task writes `<file name="...">` entries assuming same-dir layout.
- Ensure ICU and other native dependencies remain locatable (FieldWorks.exe already prepends `lib/x64` to PATH).

4) Remove registration steps
- Remove/disable any msbuild targets, scripts, or post-build steps that call regsvr32, `DllRegisterServer`, or use `RegisterForTests` in dev builds. Keep `RegisterForTestsTask` only where tests explicitly need install-time registration (should not be needed with reg-free manifests).
- In CI and dev docs, drop steps requiring elevation.

5) Verification
- Launch each EXE (FieldWorks.exe and major tools) on a clean dev VM with no COM registration and confirm no `REGDB_E_CLASSNOTREG` occurs. In DEBUG, `DebugProcs` sink creation can be wrapped in try/catch to degrade gracefully if needed.
- Optional: validate manifests contain entries for expected CLSIDs/IIDs by checking for known GUIDs (e.g., `IDebugReport`, `IVwRootBox`).

C) Update tests and utilities
- Test executables that create COM must import `RegFree.targets` to produce their own manifests. For library-only tests (no EXE), prefer running under a testhost that already has a manifest (or avoid COM activation there).
- Remove test-time registration logic; if any test harness relied on `RegisterForTestsTask`, switch it off and ensure `@(NativeComDlls)` includes the required DLLs for the test EXE.

Risks/mitigations
- Missing DLL list in manifests → COM activation fails:
 - Mitigation: Start with broad `$(TargetDir)*.dll` include. The task ignores non‑COM DLLs.
- Proxy/stub coverage:
 - `RegFreeCreator` already adds `<comInterfaceExternalProxyStub>`. Verify that proxystub content is produced (merged or separate) in x64 builds.
- Bitness mismatch:
 - Enforcing x64 everywhere avoids WOW32 confusion.
- Installer: If MSI previously depended on COM registration at install, remove those steps and ensure the EXE manifests are installed intact.

Work items checklist
1) Create `Directory.Build.props` to default to x64, and update all csproj/vcxproj to drop x86/Win32 configurations.
2) Create `Build/Targets/RegFree.targets` and wire `RegFree` task into EXE projects.
3) Add `@(NativeComDlls)` item patterns and validate the manifest generation output.
4) Remove any regsvr32/DllRegisterServer build steps.
5) Update CI to build x64 only; run smoke tests on a clean VM.
6) Update developer docs (build/run) to reflect no-registration workflow.

Appendix: key references in repo
- Reg-free tasks: `Build/Src/FwBuildTasks/RegFree.cs`, `RegFreeCreator.cs`, `RegHelper.cs`.
- Generic COM registration plumbing (for reference only): `Src/Generic/ModuleEntry.cpp`, `GenericFactory.cpp`.
- Managed interop generation: `Src/Common/ViewsInterfaces/BuildInclude.targets`, `ViewsInterfaces.csproj`.
- COM interop usage sites: `Src/Common/ViewsInterfaces/Views.cs`, `Src/Common/FwUtils/DebugProcs.cs`.

Validation path (first pass)
- Build all (x64): `msbuild FieldWorks.sln /m /p:Configuration=Debug /p:Platform=x64`.
- Confirm `FieldWorks.exe.manifest` is generated and contains `<file name="Views.dll">` with `comClass` entries and interfaces.
- From a machine with no FieldWorks registrations, launch `FieldWorks.exe` → expect no class-not-registered exceptions.
