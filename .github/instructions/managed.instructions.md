---
applyTo: "**/*.{cs,xaml,config,resx}"
name: "managed.instructions"
description: "FieldWorks managed (.NET/C#) development guidelines"
---

# Managed development guidelines for C# and .NET

## Purpose & Scope
This file describes conventions, deterministic requirements, and best practices for managed (.NET/C#) development in FieldWorks.

## Platform Support
- **Windows only**: FieldWorks targets .NET Framework 4.8 on Windows x64.
- **Mono is deprecated**: Mono/Linux support was removed. When you encounter `Platform.IsMono` checks, `TODO-Linux` comments, or other Mono-specific code, remove it when practical. Do not add new Mono-specific code paths.

## Context loading
- Review `.github/src-catalog.md` and `Src/<Folder>/AGENTS.md` for component responsibilities and entry points.
- Follow localization patterns (use .resx resources; avoid hardcoded UI strings). Crowdin sync is configured via `crowdin.json`.

## Deterministic requirements
- Threading: UI code must run on the UI thread; prefer async patterns for long-running work. Avoid deadlocks; do not block the UI.
- Exceptions: Fail fast for unrecoverable errors; log context. Avoid swallowing exceptions.
- Encoding: Favor UTF-16/UTF-8; be explicit at interop boundaries; avoid locale-dependent APIs.
- Tests: Co-locate unit/integration tests under `Src/<Component>.Tests` (NUnit patterns are common). Keep tests deterministic and portable.
- Resources: Place images/strings in resource files; avoid absolute paths; respect `.editorconfig`.

## Key Rules
- Use existing patterns for localization, unit tests, and avoid runtime-incompatible behaviors.
- Keep public APIs stable and documented with XML docs.
- **AssemblyInfo Policy**:
  - All managed projects must link `Src/CommonAssemblyInfo.cs` via `<Compile Include="..\..\CommonAssemblyInfo.cs" Link="Properties\CommonAssemblyInfo.cs" />`.
  - Set `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` to prevent SDK duplicate attribute errors.
  - Restore and maintain project-specific `AssemblyInfo*.cs` files if custom attributes are required.
  - Use `scripts/GenerateAssemblyInfo/validate_generate_assembly_info.py` to verify compliance.

## Test exclusion conversion playbook (Pattern A standard)
- Always prefer explicit `<ProjectName>Tests/**` exclusions. For nested test folders add matching explicit entries (for example `Component/ComponentTests/**`).
- Audit current state before making changes:
	```powershell
	python -m scripts.audit_test_exclusions
	```
	Inspect the resulting CSV/JSON plus the generated `Output/test-exclusions/mixed-code.json` and Markdown issue templates under `Output/test-exclusions/escalations/` before touching `.csproj` files.
- Convert projects in deterministic batches using dry-run mode first:
	```powershell
	python -m scripts.convert_test_exclusions --input Output/test-exclusions/report.json --batch-size 15 --dry-run
	```
	Remove `--dry-run` once you are satisfied with the diff. The converter rewrites only the targeted SDK-style projects and inserts the explicit `<Compile Remove="…" />` + `<None Remove="…" />` pairs.
- Typical conversion (Pattern B ➜ Pattern A):
	```xml
	<!-- Before -->
	<ItemGroup>
		<Compile Remove="*Tests/**" />
		<None Remove="*Tests/**" />
	</ItemGroup>

	<!-- After (Pattern A) -->
	<ItemGroup>
		<Compile Remove="FrameworkTests/**" />
		<None Remove="FrameworkTests/**" />
	</ItemGroup>
	```
- After each batch, rerun the audit command so `patternType` values and `ValidationIssue` records stay current, then update `Directory.Build.props` comments and any affected `Src/**/AGENTS.md` files to reflect the new pattern.

## Mixed-code escalation workflow
- Use `scripts/test_exclusions/escalation_writer.py` outputs (stored under `Output/test-exclusions/escalations/`) to open the pre-filled GitHub issue template for each project. Attach:
	- The audit/validator excerpts showing the mixed folders.
	- A short summary of the blocking files and the owning team/contact.
	- A proposed remediation plan (e.g., split helpers into a dedicated test project).
- Track the escalation link inside your working notes/PR description so reviewers can confirm every mixed-code violation has an owner before merging conversions.

## Test exclusion validation checklist
- Run the validator CLI locally for every PR touching exclusions:
	```powershell
	python -m scripts.validate_test_exclusions --fail-on-warning --json-report Output/test-exclusions/validator.json
	```
	This enforces “Pattern A only”, ensures all detected test folders are excluded, and fails on mixed-code records or CS0436 parsing hits.
- Use the Agent wrapper when running in CI or automation:
	```powershell
	pwsh Build/Agent/validate-test-exclusions.ps1 -FailOnWarning
	```
	The wrapper chains the Python validator, MSBuild invocation, and CS0436 log parsing so agent runs match local expectations.
- Guard against leaked test types before publishing artifacts:
	```powershell
	pwsh scripts/test_exclusions/assembly_guard.ps1 -Assemblies "Output/Debug/**/*.dll"
	```
	The guard loads each assembly and fails when any type name ends in `Test`/`Tests`. Include the log in release sign-off packages.
- Document the validation evidence (validator JSON, PowerShell transcript, assembly guard output) in the PR description alongside the rerun audit results.

## Examples
```csharp
// Minimal example of public API with XML docs
/// <summary>Converts foo to bar</summary>
public Bar Convert(Foo f) { ... }
```

## Structured output
- Public APIs include XML docs; keep namespaces consistent.
- Include minimal tests (happy path + one edge case) when modifying behavior.
- Follow existing project/solution structure; avoid creating new top-level patterns without consensus.

## References
- Build: `msbuild FieldWorks.sln /m /p:Configuration=Debug`
- Tests: Use Test Explorer or `dotnet test` for SDK-style; NUnit console for .NET Framework assemblies.
- Localization: See `DistFiles/CommonLocalizations/` and `crowdin.json`.

