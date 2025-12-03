---
applyTo: "**/*.{cpp,h,hpp,cc,ixx,def}"
name: "native.instructions"
description: "FieldWorks native (C++/C++-CLI) development guidelines"
---

# Native development guidelines for C++ and C++/CLI

## Purpose & Scope
This file outlines conventions and patterns for native C++/C++-CLI code used in FieldWorks, including interop rules and build-specific requirements.

## Context loading
- Review `Src/<Folder>/COPILOT.md` for managed/native boundaries and interop contracts.
- Include/Lib paths are injected by build props/targets; avoid ad-hoc project configs.

## Deterministic requirements
- Memory & RAII: Prefer smart pointers and RAII for resource management.
- Interop boundaries: Define clear marshaling rules (strings/arrays/structs). Avoid throwing exceptions across managed/native boundaries; translate appropriately.
- SAL annotations and warnings: Use SAL where feasible; keep warning level strict; fix warnings, don’t suppress casually.
- Encoding: Be explicit about UTF-8/UTF-16 conversions; do not rely on locale defaults.
- Threading: Document thread-affinity for UI and shared objects.

## Structured output
- Header hygiene: Minimize transitive includes; prefer forward declarations where reasonable.
- ABI stability: Avoid breaking binary interfaces used by C# or other native modules without coordinated changes.
- Tests: Favor deterministic unit tests; isolate filesystem/registry usage.

## Key Rules
- Enforce RAII and prefer checked operations for buffer management.
- Keep interop marshaling rules well documented and ensure managed tests exist for early validation.

## Native C++ Tests

### Test Framework
Native C++ tests use the **Unit++** framework (legacy). Test projects:
- `Src/Generic/Test/` → `testGenericLib.exe`
- `Src/views/Test/` → `TestViews.exe`

### Building Tests
Build from VS Developer Command Prompt:
```cmd
cd Src\Generic\Test
nmake /nologo BUILD_CONFIG=Debug BUILD_TYPE=d BUILD_ROOT=%CD%\..\..\..\  BUILD_ARCH=x64 /f testGenericLib.mak
```

### vcxproj Project Structure
The vcxproj files are "Makefile" projects (`ConfigurationType=Makefile`) that:
- Wrap nmake invocations via `NMakeBuildCommandLine`
- Require environment variables: `BUILD_ROOT`, `BUILD_CONFIG`, `BUILD_TYPE`, `BUILD_ARCH`
- Cannot be built via plain `msbuild` without VS toolchain in PATH

### Test Dependencies
- Main native libraries (Generic.lib, DebugProcs.dll) must be built first
- ICU 70 DLLs required at runtime
- `Bin/CollectUnit++Tests.cmd` generates test registration code

### Future Direction
Migration to GoogleTest is planned for better IDE integration. See `specs/007-test-modernization-vstest/native-migration-plan.md`.

## Examples
```cpp
// Prefer RAII
std::unique_ptr<MyBuffer> buf = std::make_unique<MyBuffer>(size);
```

## References
- Build: Use top-level solution/scripts to ensure props/targets are loaded.
- Interop: Coordinate with corresponding managed components in `Src/`.
