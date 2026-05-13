---
applyTo: "Src/Common/ViewsInterfaces/**,Src/views/**,Src/Generic/**,Src/Kernel/**,**/*.{cpp,h,hpp,ixx,def}"
name: "fieldworks-interop-review"
description: "Copilot code review checks for native, C++/CLI, COM, and managed/native boundary changes"
---

# Interop and Native Boundary Review Checks

## Purpose

Use these checks for native code, C++/CLI-adjacent code, COM interfaces,
ViewsInterfaces, Generic, Kernel, and managed/native boundary changes.

## Boundary safety

- Validate untrusted input before it crosses native, COM, file-system, or process
  boundaries.
- Check string encoding conversions, buffer lengths, array counts, pointer
  ownership, lifetime, and nullability on every changed boundary.
- Avoid throwing exceptions across managed/native boundaries; translate failures
  into the existing error-reporting pattern.
- Verify SAL annotations, checked APIs, RAII, and deterministic cleanup are used
  where applicable.

## COM and ABI stability

- Flag COM GUID, interface layout, vtable order, manifest, or registration-free
  COM changes for explicit compatibility review.
- Do not introduce global COM registration, registry hacks, or assumptions that
  require elevated machine-wide state.
- Managed wrapper changes should stay consistent with the native contract and
  include tests or validation that exercise the boundary.

## Build and diagnostics

- Native changes must not hide compiler or MSBuild warnings; fix warnings rather
  than suppressing them casually.
- Boundary failures should produce actionable diagnostics without leaking
  sensitive data or overwhelming normal logs.
