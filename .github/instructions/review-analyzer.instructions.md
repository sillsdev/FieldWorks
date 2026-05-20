---
name: "FieldWorks Review Analyzer"
description: "Use when performing PR or branch review analysis for FieldWorks. Defines required checks for contracts, managed UI, native interop, build/test, installer, localization, and validation evidence."
---

# FieldWorks Review Analyzer

Use these instructions whenever Copilot analyzes a FieldWorks branch, PR, or diff for review. This is review policy, not an interactive workflow; the interactive author interview lives in the `challenge-pr-author` skill.

FieldWorks is a Windows/x64 desktop application with .NET Framework 4.8 managed code, native C++, C++/CLI-adjacent boundaries, registration-free COM, WiX installer flows, `.resx` localization, and repo scripts for build/test validation.

## Accuracy Requirement

Before reporting any finding, verify it against actual code. Read the relevant file and, when necessary, compare against the before-state with `git show <merge-base>:<file>`. Do not flag issues based only on file names, assumptions, or grep hits.

A small number of accurate findings is better than a long list with errors. If you cannot confirm a concern from the actual code, omit it or explicitly note that it could not be verified.

## Context to Apply

Use the repository guidance that applies to changed files, especially:

- `AGENTS.md`
- `.github/copilot-instructions.md`
- `.github/instructions/build.instructions.md`
- `.github/instructions/testing.instructions.md`
- `.github/instructions/managed.instructions.md`
- `.github/instructions/native.instructions.md`
- `.github/instructions/fieldworks-build-installer-review.instructions.md`
- `.github/instructions/fieldworks-interop-review.instructions.md`
- `.github/instructions/fieldworks-parser-utilities-review.instructions.md`
- `.github/instructions/fieldworks-ui-review.instructions.md`

Load only the files needed for the changed areas. For structural or hidden-dependency risk, such as inheritance, interfaces, factories, DI/composition, native/managed boundaries, COM, parser pipelines, installer chains, or build graph changes, use symbol/reference/navigation tools if available. If not available, inspect definitions, callers, project references, and relevant grep results before concluding there is no dependency.

## Severity Rubric

- **Critical**: Blocks merge. Examples: correctness bug, security issue, native/managed boundary safety issue, breaking public contract or ABI change without compatibility plan, global COM registration/registry hack, build ordering break, CI silently skipping tests, installer behavior that can corrupt install/upgrade/uninstall.
- **Important**: Should fix before merge. Examples: missing meaningful tests for risky behavior, .NET Framework/C# 7.3 incompatibility, localization gap, legacy project file omission, likely UI crash path, installer/build validation gap, significant reuse/architecture concern.
- **Minor**: Nice to have. Examples: naming, small simplification, low-risk consistency issue, dependency bump note, documentation polish.

When a verified finding's severity is unclear, prefer the higher severity and explain the risk.

## FieldWorks Hot Spots

Be especially careful when changes touch `Src/Utilities`, parser utilities, Allomorph Generator, interlinear display, installer/build workflows, dependency versions, WinForms click handlers, stale UI state, layout/display refresh, media-line behavior, or localization resources. Recent regressions in these areas often involve null checks, collection bounds, selection state, refresh/invalidation, layout anchoring, resource keys, and missing regression-test evidence.

## Required Analysis Passes

Run all four passes for PR/branch review. They may be done by separate read-only subagents or by Copilot directly, but every pass must be represented in the final analysis.

### Pass 1: Contracts, Compatibility, and Correctness

Analyze compatibility-relevant surfaces:

- Public or protected C# types and members in changed assemblies, especially under `Src/` and shared libraries.
- Native headers, exported functions, `.def` files, COM interfaces, MIDL-generated interfaces, GUIDs, interface layout, vtable order, manifests, and registration-free COM configuration.
- Serialized data, project data, migration formats, settings/config schemas, XML formats, resource keys whose meaning changed, and file formats consumed across versions.
- PowerShell/MSBuild/script parameters and outputs that developers, CI, or installer workflows depend on.
- Installer product/upgrade codes, component IDs, feature layout, bundle/MSI command-line behavior, registry/file footprint, and upgrade/uninstall contracts.

Steps:

1. Get changed files with `git diff --name-only <merge-base>`.
2. For candidate contract files, compare before/after using `git show <merge-base>:<file>` and current content.
3. Inspect callers/consumers when behavior or signatures change.
4. For legacy `.csproj` changes or new C# files, verify source files are explicitly included where required and test sources are not accidentally included in production projects.
5. For COM/native interfaces, verify GUIDs, layout, registration-free COM assumptions, and managed wrappers remain compatible or are explicitly coordinated.

Flag Critical when confirmed:

- Public C# API removal, parameter removal/reordering, type narrowing, behavior change for valid inputs, or undocumented default change that can break existing callers.
- COM interface, GUID, vtable, marshaling, or manifest change without a compatibility plan and validation.
- Native exported surface or ABI change that can break managed/native consumers.
- Serialized format or migration behavior change that risks existing projects.
- Build/test/installer script contract change that breaks documented repo workflows.

Also read changed files and assess whether the implementation accomplishes the branch purpose. Flag incomplete implementations, stubs, new TODOs, dead code, null/bounds bugs, stale state, missed refresh/invalidation, behavior mismatches, security risks, and data-loss risks.

### Pass 2: Managed UI, C#, and Localization

Review changed managed files (`*.cs`, `*.csproj`, `*.resx`, `*.config`, `*.xaml`) and related UI resources.

Check:

- FieldWorks targets .NET Framework 4.8 and C# 7.3. Flag C# 8+ syntax/APIs such as nullable reference types, switch expressions, `using var`, ranges, records, target-typed `new`, file-scoped namespaces, or `required` members.
- Legacy `.csproj` files require explicit source includes. New `.cs` files must be included, and tests must not leak into production projects.
- NuGet/reference changes are coordinated with `Directory.Packages.props`, binding redirects, app configs, and .NET Framework compatibility.
- Event handlers and callbacks guard `sender`, selected items, selected indices, model objects, cast results, and collection bounds.
- Deleted/replaced model objects cannot remain in chooser lists, cached selections, mediator state, or reference collections.
- UI updates from background work marshal back to the UI thread.
- Interlinear, preview pane, dictionary, media-line, and tree/list display changes preserve refresh/invalidation, persistence, and selection behavior.
- Designer/layout changes preserve `TableLayoutPanel`, `FlowLayoutPanel`, anchoring, docking, minimum size, tab order, resizable dialogs, disposal, component initialization, and resource manager usage.
- User-visible UI strings belong in `.resx` resources, not hardcoded in C#.
- Resource keys are stable and descriptive. If a string's meaning changed, flag that a new key may be needed for Crowdin/localization safety.
- `.resx`, designer-generated resource accessors, and consuming code stay synchronized.

### Pass 3: Native, COM, and Boundary Safety

Review native and boundary files, especially `*.cpp`, `*.h`, `*.hpp`, `*.ixx`, `*.def`, `Src/Common/ViewsInterfaces/**`, `Src/views/**`, `Src/Generic/**`, `Src/Kernel/**`, and managed wrappers around native code.

Check:

- Untrusted input is validated before native, COM, file-system, installer, or process boundaries.
- String encoding conversions are explicit and do not rely on locale defaults.
- Buffer lengths, array counts, pointer ownership, lifetime, nullability, and out parameters are correct.
- RAII or deterministic cleanup is used for native resources.
- SAL annotations and checked APIs are used where existing patterns call for them.
- Exceptions do not cross managed/native boundaries; failures are translated through existing error-reporting patterns.
- COM GUID, interface layout, vtable order, manifest, or registration-free COM changes are explicit and compatible.
- No global COM registration or registry hacks are introduced.
- Managed wrapper changes remain consistent with native contracts.
- Native changes do not hide compiler/MSBuild warnings.
- Boundary failures produce actionable diagnostics without excessive normal logging.
- Native tests or managed boundary tests cover risky changes.

### Pass 4: Build, Tests, CI, Dependencies, and Installer

Review build scripts, project files, tests, CI workflows, package/dependency files, installer files, and validation evidence.

Check:

- Normal build/test validation uses `./build.ps1` and `./test.ps1`; avoid ad-hoc `msbuild`, `dotnet build`, `vstest.console`, or `nmake` unless debugging build infrastructure.
- Native C++ still builds before managed projects through `FieldWorks.proj` and `build.ps1` ordering.
- Build/test process cleanup remains scoped to the current worktree and does not terminate other worktrees' processes.
- CI changes do not skip tests or quality gates silently.
- Hardcoded absolute paths, machine-specific assumptions, hidden downloads, or untracked prerequisites are not introduced.
- Bug fixes, parser/morphology/interlinear/media-line/display changes, interop changes, installer changes, build workflow changes, and dependency alignment changes have meaningful validation evidence.
- Version changes in `Directory.Packages.props`, `Build/SilVersions.props`, app configs, binding redirects, local-library scripts, and installer/runtime packaging agree.
- New dependencies are compatible with .NET Framework 4.8 and Windows x64.
- WiX and installer changes preserve WiX 3 and WiX 6 flows unless explicitly scoped.
- Bundle/MSI install, upgrade, uninstall, logs, and expected footprint have validation evidence when behavior changes.
- PowerShell scripts propagate exit codes, quote paths with spaces, avoid unsafe pipelines where repo wrappers exist, and avoid name-based process termination.

Do not mark manual smoke tests complete unless they were directly performed or the author explicitly confirms them.

## Analysis Output Format

Use this format for each pass or for the combined analyzer result:

```markdown
## Contract/API Changes Summary

[Factual list of additions, removals, and modifications to public or compatibility-relevant surfaces. Write "None." if no such surfaces changed.]

## Findings

### Critical (must address)

- [ ] `path/to/file.ext`: Specific verified finding with explanation and suggested fix if applicable.

### Important (should address)

- [ ] `path/to/file.ext`: Specific verified finding with explanation.

### Minor (consider)

- [ ] `path/to/file.ext`: Specific verified finding.

### Positive Observations

- Things done well: good tests, safe boundary handling, localized strings, clear diagnostics, minimal scope, etc.

### Required Validation

- [ ] `./build.ps1` - needed because [reason]
- [ ] `./test.ps1 -TestProject "..."` - needed because [reason]
- [n/a] No additional validation beyond normal review identified.
```

If a section has no entries, write `None.` instead of leaving it blank.
