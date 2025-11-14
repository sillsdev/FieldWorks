---
applyTo: "Src/Common/**"
name: "common.instructions"
description: "Key conventions and integration points for the Common project." 
---

# Common Library Guidelines

## Purpose & Scope
- Document public utility contracts, expected threading model, and test patterns for `Src/Common`.

## Conventions
- Keep public API surface minimal and stable; add unit tests for contract changes.
- Avoid adding heavy dependencies to Common; favor small, well-defined helper functions.

## Examples
```csharp
// Good: small, well-defined helper public API
public static string NormalizePath(string path) => Path.GetFullPath(path);
```
