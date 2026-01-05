---
applyTo: "**/*"
name: "security.instructions"
description: "Security expectations and OWASP-derived rules for code changes in FieldWorks"
---

# Security & Secure-by-default

## Purpose & Scope
- Provide concise, actionable security checks that reviewers and agents should enforce.

## Rules
- Sanitize all untrusted input before passing to native boundaries or COM APIs.
- Avoid hardcoded secrets or credentials; enforce use of env vars or secrets stores.
- Review changes that touch interop layers (C# `<->` C++/CLI) for buffer handling / marshaling issues.

## Automated Checks
- Run static analyzers for C# & C++ where possible; surface results in CI.

## Examples & Quick Checks
- For C++ code manipulating buffers, prefer usage of checked APIs and unit tests that validate boundaries.
