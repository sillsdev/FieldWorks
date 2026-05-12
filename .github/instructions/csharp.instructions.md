---
description: 'Guidelines for building C# applications'
applyTo: '**/*.cs'
---

# C# Development

## Scope
- FieldWorks managed code is a Windows desktop codebase built on .NET Framework 4.8 and native dependencies.
- Treat this file as guidance for existing FieldWorks C# projects, not for ASP.NET or service-oriented .NET applications.
- On Linux and macOS, limit work to editing, search, docs, and specs. Do not claim to have built or run managed binaries there.

## Language Version And Features
- Use the repo default C# language version unless a project explicitly overrides it. Today that default is C# 8.0 via `Directory.Build.props`.
- Do not introduce C# features that require a newer language version without first updating repo-wide build policy.
- Nullable reference types are disabled by default. Only use nullable annotations and nullable-flow assumptions in projects that opt in explicitly.
- Prefer syntax that is already common in the touched area. Consistency with nearby code is more important than using the newest available syntax.

## General Instructions
- Make only high-confidence suggestions when reviewing code changes.
- Fix the root cause when practical, but keep edits narrow and compatible with existing behavior.
- Handle edge cases and exception paths explicitly. Do not swallow exceptions without a documented reason.
- Treat native interop, COM, registry-free COM, file formats, and serialization boundaries as high-risk areas that need extra care.

## Naming Conventions
- Follow PascalCase for types, methods, properties, events, and public members.
- Use camelCase for locals and private fields.
- Prefix interfaces with `I`.
- Keep namespaces aligned with the existing project root namespace instead of inventing new top-level naming schemes.

## Formatting And Style
- Apply the formatting rules defined in `.editorconfig` and match surrounding code.
- Prefer block-scoped namespaces. Do not default to file-scoped namespaces in this repo.
- Keep using directives simple and consistent with the file you are editing.
- Use `nameof` instead of string literals when referencing member names.
- Use pattern matching where it improves clarity and is supported by the project language version. Do not force newer syntax into older-looking code.

## Documentation And Comments
- Public APIs should have XML documentation comments.
- Add code comments only when intent, invariants, or interop constraints are not obvious from the code itself.
- Do not add boilerplate comments to every method.

## Project Conventions
- Most managed projects here are SDK-style `.csproj` files targeting `net48`.
- Keep `GenerateAssemblyInfo` disabled where the project relies on linked `CommonAssemblyInfo.cs`.
- Preserve project-specific build settings such as warnings-as-errors, x64 assumptions, WinExe/WindowsDesktop settings, and registration-free COM behavior.
- When adding new files, update the project file only if the specific project format requires it.

## Desktop, UI, And Localization
- FieldWorks is a desktop application. Favor guidance relevant to WinForms, WPF, dialogs, view models, threading, and long-running UI work.
- UI-affecting code must respect the UI thread. Avoid blocking calls that can freeze the application.
- Keep user-visible strings in `.resx` resources and follow existing localization patterns. Do not hardcode new UI strings.
- Preserve designer compatibility for WinForms and avoid edits that break generated code patterns.

## Nullability And Defensive Code
- Because nullable reference types are usually disabled, write explicit null checks at public entry points and interop boundaries when required by the surrounding code.
- Prefer `is null` and `is not null` checks when adding new null checks.
- Do not pretend the compiler will enforce null-state safety unless the project has opted into nullable analysis.

## Testing
- For behavior changes and bug fixes, add or update tests when practical.
- Follow nearby test naming and structure conventions. Do not add `Arrange`, `Act`, or `Assert` comments unless the existing file already uses them.
- Prefer fast, deterministic NUnit tests for managed code.
- Use `./test.ps1` on Windows to run tests, and `./build.ps1` when you need a supporting build first. Do not recommend ad-hoc `dotnet test` or `msbuild` commands as the normal path for this repo.

## Build And Validation
- Use `./build.ps1` for builds and `./test.ps1` for tests in normal repo workflows.
- Avoid changing build, packaging, COM, or registry behavior without checking the existing build instructions and affected tests.
- Treat compiler warnings as actionable unless the repo already documents a specific exception.

## What Not To Assume
- Do not assume ASP.NET Core, Minimal APIs, Entity Framework Core, Swagger/OpenAPI, cloud deployment, container publishing, or JWT authentication are relevant unless the user is explicitly working in a repo area that adds those technologies.
