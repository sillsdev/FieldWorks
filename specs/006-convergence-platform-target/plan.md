# Implementation Plan: PlatformTarget Redundancy Cleanup

**Branch**: `specs/006-convergence-platform-target` | **Date**: 2025-11-14 | **Spec**: [/specs/006-convergence-platform-target/spec.md](../../006-convergence-platform-target/spec.md)
**Input**: Feature specification from `/specs/006-convergence-platform-target/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Remove redundant `<PlatformTarget>x64</PlatformTarget>` declarations from 110 SDK-style projects so that the global defaults defined in `Directory.Build.props` remain the single source of truth. Retain the single intentional AnyCPU declaration (FwBuildTasks) with an XML comment explaining that it is tooling, and document any future exceptions. Implementation relies on the convergence Python tooling (`convergence.py platform-target *`) to audit, apply, and validate changes while ensuring x64-only enforcement stays intact, followed by targeted builds of FwBuildTasks and COPILOT.md updates wherever project files change.

## Technical Context

<!--
  ACTION REQUIRED: Replace the content in this section with the technical details
  for the project. The structure here is presented in advisory capacity to guide
  the iteration process.
-->

**Language/Version**: Python 3.11 tooling plus C#/.NET SDK-style project files (MSBuild 17.x)
**Primary Dependencies**: `convergence.py` framework, MSBuild/Directory.Build.props inheritance, git for change tracking
**Storage**: N/A (edits are to project files tracked in git)
**Testing**: `python convergence.py platform-target validate`, `msbuild FieldWorks.proj /m /p:Configuration=Debug`
**Target Platform**: Windows x64 developer environments and CI runners
**Project Type**: Multi-project desktop/CLI suite (FieldWorks mono-repo)
**Performance Goals**: No regressions to build time; maintain single-pass traversal build
**Constraints**: Preserve x64-only enforcement, document AnyCPU exceptions with XML comments (specifically `<!-- Must be AnyCPU... -->`) explaining they are build/test tools that never ship in end-user executables, avoid touching unrelated MSBuild properties, and keep every touched `Src/**` folder’s COPILOT.md accurate
**Scale/Scope**: 119 SDK-style projects (110 redundant x64 declarations, 1 justified AnyCPU exception, remainder already inheriting defaults)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Data integrity**: Not applicable—no schemas or persisted assets change. ✅
- **Test evidence**: Plan mandates rerunning `convergence.py platform-target validate` plus a targeted MSBuild to prove all projects still build; no new runtime features introduced. ✅
- **I18n/script correctness**: No text rendering paths touched. ✅
- **Licensing**: No new dependencies introduced; editing existing csproj metadata only. ✅
- **Stability/performance**: Risk is limited to build failures; mitigated via validation phase and git bisect-friendly commits. ✅

*Post-Phase 1 Re-check (2025-11-14): No new risks introduced; gates remain satisfied.*

## Project Structure

### Documentation (this feature)

```text
specs/006-convergence-platform-target/
├── spec.md              # Feature specification (Path A rationale)
├── plan.md              # This file (/speckit.plan output)
├── research.md          # Decision log + clarification history
├── data-model.md        # Entity/state tracking for audits
├── quickstart.md        # Operator command cheat sheet
├── contracts/
│   └── platform-target.yaml  # Structured CLI contract
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
Repository root
├── convergence.py          # CLI driver for convergence specs (already exposes platform-target commands)
├── Build/
│   ├── Agent/
│   ├── Src/
│   │   └── NativeBuild/
│   └── Directory.Build.props  # Centralized PlatformTarget=x64 settings
└── Src/
  ├── Common/**.csproj
  ├── LexText/**.csproj
  ├── Utilities/**.csproj
  └── ...                     # 119 SDK-style managed projects touched by the audit
```

**Structure Decision**: Operate directly on existing `Src/**.csproj` projects using the convergence Python tooling in `Build/`. No new source directories are created; documentation artifacts remain within `specs/006-convergence-platform-target/`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --------- | ---------- | ------------------------------------ |
| _None_    |            |                                      |
