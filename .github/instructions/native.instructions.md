---
applyTo: "**/*.{cpp,h,hpp,cc,ixx,def}"
description: "FieldWorks native (C++/C++-CLI) development guidelines"
---
# Native development guidelines for C++ and C++/CLI

## Context loading
- Review `Src/<Folder>/COPILOT.md` for managed/native boundaries and interop contracts.
- Include/Lib paths are injected by build props/targets; avoid ad-hoc project configs.

## Deterministic requirements
- Memory & RAII: Prefer smart pointers and RAII for resource management.
- Interop boundaries: Define clear marshaling rules (strings/arrays/structs). Avoid throwing exceptions across managed/native boundaries; translate appropriately.
- SAL annotations and warnings: Use SAL where feasible; keep warning level strict; fix warnings, don’t suppress casually.
- Encoding: Be explicit about UTF-8/UTF-16 conversions; do not rely on locale defaults.
- Threading: Document ownership and thread-affinity for UI and shared objects.

## Structured output
- Header hygiene: Minimize transitive includes; prefer forward declarations where reasonable.
- ABI stability: Avoid breaking binary interfaces used by C# or other native modules without coordinated changes.
- Tests: Favor deterministic unit tests; isolate filesystem/registry usage.

## References
- Build: Use top-level solution/scripts to ensure props/targets are loaded.
- Interop: Coordinate with corresponding managed components in `Src/`.
