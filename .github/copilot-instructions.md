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
- For C# code, flag C# 8+ syntax and APIs that are not compatible with .NET
  Framework 4.8 / C# 7.3.
- For native/non-SDK `.csproj` changes, verify explicit imports, package
  wiring, and native-before-managed build ordering remain correct.
- Require meaningful test or validation evidence for bug fixes, regressions,
  parser changes, interlinear display changes, installer changes, and build
  workflow changes.

## Review style

- Prefer concise, actionable comments that explain the FieldWorks-specific risk.
- Do not request unrelated refactors.
- Do not rely on external links; cite the relevant rule or file path directly.
