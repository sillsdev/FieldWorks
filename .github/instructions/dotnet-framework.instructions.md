---
description: 'Guidance for working with .NET Framework projects. Includes project structure, C# language version, NuGet management, and best practices.'
applyTo: '**/*.csproj, **/*.cs'
---

# .NET Framework Development

## Build and Compilation Requirements
- Use `./build.ps1` for normal builds and `./test.ps1` for normal test runs.
- Do not use `dotnet build` for the main repo workflow.
- Avoid direct `msbuild` or project-only builds unless you are explicitly debugging build infrastructure.

## Project File Management

### SDK-style net48 projects are common
Most managed projects in this repo use SDK-style `.csproj` files while still targeting `.NET Framework` `net48`.

- **Implicit file inclusion is common**: SDK-style projects usually include new `.cs` files automatically.
- **Check the touched project before editing**: Some projects still carry explicit includes, linked files, designer items, generated code, or custom metadata that must be preserved.
- **Assembly metadata is managed explicitly**: Keep `GenerateAssemblyInfo` disabled where the project links `CommonAssemblyInfo.cs`.
- **Preserve project-specific settings**: Keep existing settings for x64, WinExe, WindowsDesktop, COM, resources, warnings-as-errors, and custom MSBuild targets.

- **Do not normalize projects unnecessarily**: Avoid converting project structure, import style, or target declarations unless that is the task.

### Legacy project caveat
Some non-SDK-style or tool-specific projects may still exist. When you encounter one:
- Keep explicit `<Compile Include=... />` items and other legacy structure intact.
- Update the project file only as much as the change requires.
- Do not assume you can migrate it to SDK-style as part of routine code edits.

## NuGet Package Management
- Installing and updating NuGet packages in .NET Framework projects is a complex task requiring coordinated changes to multiple files. Therefore, **do not attempt to install or update NuGet packages** in this project.
- Instead, if changes to NuGet references are required, ask the user to install or update NuGet packages using the Visual Studio NuGet Package Manager or Visual Studio package manager console.
- When recommending NuGet packages, ensure they are compatible with .NET Framework or .NET Standard 2.0 (not only .NET Core or .NET 5+).

## C# Language Version
- The repo-wide default language version for C# projects is 8.0.
- Do not introduce features that require a newer language version unless the specific project or repo policy is updated first.
- Prefer syntax already used in the touched area so edits remain consistent with surrounding code.

### C# 8.0 features available by default
- Switch expressions and pattern matching enhancements.
- Using declarations where disposal scope remains clear.
- Null-coalescing assignment and range/index operators when they improve clarity.

### Features not safe to assume repo-wide
- Nullable reference types are not enabled repo-wide. Do not introduce `string?`, `#nullable enable`, or nullable-flow assumptions unless the project explicitly opts in.
- File-scoped namespaces are not available under the repo default language version and should not be introduced.

### C# 9.0+ Features (NOT SUPPORTED):
  - Records (`public record Person(string Name)`)
  - Init-only properties (`{ get; init; }`)
  - Top-level programs (program without Main method)
  - Pattern matching enhancements
  - Target-typed new expressions (`List<string> list = new()`)

### C# 10+ Features (NOT SUPPORTED):
  - Global using statements
  - File-scoped namespaces
  - Record structs
  - Required members

### Use instead when staying within repo defaults
  - Block-scoped namespaces
  - Explicit null checks unless the project has enabled nullable analysis
  - Established project patterns over newer syntax for its own sake

## Environment Considerations (Windows environment)
- FieldWorks builds and managed test runs are Windows-only.
- On non-Windows hosts, limit work to editing, code search, docs, and specs.
- When suggesting build or test commands, prefer the repo PowerShell scripts and Windows-oriented workflows.

## Common .NET Framework Pitfalls and Best Practices

### Async/Await Patterns
- **ConfigureAwait(false)**: Use it deliberately in library or background code when you do not need to resume on the UI thread:
  ```csharp
  var result = await SomeAsyncMethod().ConfigureAwait(false);
  ```
- **Avoid sync-over-async**: Don't use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` unless you have a clear, reviewed reason. These patterns can deadlock desktop UI code.
- **Respect the UI thread**: Do not move UI-bound code onto background threads or block the UI while waiting on long-running work.

### DateTime Handling
- **Use DateTimeOffset for timestamps**: Prefer `DateTimeOffset` over `DateTime` for absolute time points
- **Specify DateTimeKind**: When using `DateTime`, always specify `DateTimeKind.Utc` or `DateTimeKind.Local`
- **Culture-aware formatting**: Use `CultureInfo.InvariantCulture` for serialization/parsing

### String Operations
- **StringBuilder for concatenation**: Use `StringBuilder` for multiple string concatenations
- **StringComparison**: Always specify `StringComparison` for string operations:
  ```csharp
  string.Equals(other, StringComparison.OrdinalIgnoreCase)
  ```

### Memory Management
- **Dispose pattern**: Implement `IDisposable` properly for unmanaged resources
- **Using statements**: Always wrap `IDisposable` objects in using statements
- **Avoid large object heap**: Keep objects under 85KB to avoid LOH allocation

### Configuration
- **Use ConfigurationManager**: Access app settings through `ConfigurationManager.AppSettings`
- **Connection strings**: Store in `<connectionStrings>` section, not `<appSettings>`
- **Transformations**: Use web.config/app.config transformations for environment-specific settings

### Exception Handling
- **Specific exceptions**: Catch specific exception types, not generic `Exception`
- **Don't swallow exceptions**: Always log or re-throw exceptions appropriately
- **Use using for disposable resources**: Ensures proper cleanup even when exceptions occur

### Resources and localization
- Put user-visible strings into `.resx` resources and follow existing localization patterns.
- Avoid hardcoding new UI strings in C# code.

### Interop and COM
- Treat P/Invoke, COM, registration-free COM, and serialization boundaries as high risk.
- Preserve existing manifest, marshaling, and build settings unless the task explicitly requires changing them.

### Performance Considerations
- **Avoid boxing**: Be aware of boxing/unboxing with value types and generics
- **String interning**: Use `string.Intern()` judiciously for frequently used strings
- **Lazy initialization**: Use `Lazy<T>` for expensive object creation
- **Avoid reflection in hot paths**: Cache `MethodInfo`, `PropertyInfo` objects when possible
