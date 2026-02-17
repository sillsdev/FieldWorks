# Research Summary — PlatformTarget Redundancy Cleanup

## Decision 1: Convergence tooling drives detection + enforcement
- **Rationale**: `convergence.py platform-target audit|convert|validate` already parses every SDK-style project and emits CSV artifacts we can diff. Reusing it avoids bespoke scripts, keeps reporting consistent across convergence specs, and feeds directly into the decision CSV required by the framework.
- **Alternatives considered**:
  - *Ad-hoc grep/PowerShell*: fast to prototype but brittle with conditional `PropertyGroup` blocks.
  - *MSBuild Structured Log analysis*: powerful yet slower to iterate and not part of existing convergence playbook.

## Decision 2: Preserve explicit AnyCPU only for documented build tools
- **Rationale**: FwBuildTasks runs inside MSBuild as tooling and must stay AnyCPU so it can load in either 32-bit or 64-bit hosts. Leaving its `<PlatformTarget>` declaration in place—and adding comments if missing—keeps intent obvious while the rest of the repo inherits x64. No other build/test artifact currently needs this override; future exceptions must justify themselves with the same level of documentation.
- **Alternatives considered**:
  - *Force x64 everywhere*: risks breaking MSBuild task hosting where CLR loads AnyCPU assemblies into 32-bit MSBuild instances.
  - *Detect AnyCPU heuristically (e.g., check OutputType)*: would still require manual curation and could delete legitimate overrides.

## Decision 3: Limit clean-up to `<PlatformTarget>`; leave `<Platform>` entries intact
- **Rationale**: Directory.Build.props already standardizes `<Platforms>` and `<PlatformTarget>` values; `<Platform>` settings inside solution configurations are handled elsewhere. Touching `<Platform>` risks diverging from `.sln` expectations without delivering extra benefit for this spec.
- **Alternatives considered**:
  - *Strip both `<Platform>` and `<PlatformTarget>`*: broader scope than required and forces coordination with solution files.
  - *Strip `<Platform>` only*: does not reduce redundancy because compilers still honor `<PlatformTarget>` values.

## Decision 4: Validation via targeted msbuild run instead of full rebuild
- **Rationale**: Running `python convergence.py platform-target validate` plus a single `msbuild FieldWorks.proj /m /p:Configuration=Debug` confirms both the script's static checks and the actual build succeed without requiring every configuration platform combination.
- **Alternatives considered**:
  - *Full matrix build (Debug/Release x AnyCPU/x64)*: unnecessary given FieldWorks is x64-only post-migration.
  - *Skipping msbuild*: would miss regressions where removing `<PlatformTarget>` changes how legacy projects compile.

## Exception Details

- **FwBuildTasks.csproj**:
  - **Location**: `Build/Src/FwBuildTasks/FwBuildTasks.csproj` (approx line 10)
  - **Setting**: `<PlatformTarget>AnyCPU</PlatformTarget>`
  - **Justification**: Must be AnyCPU to run inside both 32-bit and 64-bit MSBuild processes.
