# FieldWorks Copilot Code Review Instructions

## Purpose

Review FieldWorks pull requests for the project-specific risks most likely to
cause regressions in this Windows/x64, .NET Framework 4.8, native/managed
desktop application.

## Highest-priority checks

- Verify changes preserve the native-before-managed build ordering enforced by
  `build.ps1`, `FieldWorks.proj`, and related targets.
- Flag changes that introduce global COM registration or registry hacks;
  FieldWorks uses registration-free COM.
- Check all native, COM, file-system, installer, and process-boundary inputs for
  validation, encoding assumptions, buffer lengths, and ownership rules.
- Verify user-visible UI strings are placed in `.resx` resources and remain
  compatible with Crowdin localization.
- For C# code, flag language features newer than the repo default C# 8.0,
  nullable-reference-type assumptions when nullable is disabled, and APIs that
  are not compatible with .NET Framework 4.8.
- For legacy `.csproj` changes, verify new source files are explicitly included
  and test sources are explicitly excluded where production and test trees mix.
- Require meaningful test or validation evidence for bug fixes, regressions,
  parser changes, interlinear display changes, installer changes, and build
  workflow changes.

## Review focus from recent history

- Recent churn is concentrated in `Src/Utilities`, parser utilities, Allomorph
  Generator, Interlinear display, build scripts, and installer workflows.
- Recent bugs include click-handler crashes, stale UI state, layout/display
  regressions, media-line rework, localization gaps, installer/build fixes, and
  dependency-version alignment.
- When those areas change, review null checks, collection bounds, selection
  state, refresh/invalidation behavior, layout anchoring, localization, and
  regression-test coverage before style-only concerns.

## Review style

- Prefer concise, actionable comments that explain the FieldWorks-specific risk.
- Do not request unrelated refactors.
- Do not rely on external links; cite the relevant rule or file path directly.
