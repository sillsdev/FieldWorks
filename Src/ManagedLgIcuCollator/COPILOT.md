# ManagedLgIcuCollator

## Purpose
Managed wrapper for ICU collation services. Provides .NET-friendly access to ICU (International Components for Unicode) collation and sorting functionality for proper linguistic text ordering.

## Key Components
- **ManagedLgIcuCollator.csproj** - Managed collation wrapper library
- **ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.csproj** - Collation tests

## Technology Stack
- C# .NET with P/Invoke or C++/CLI
- ICU library integration
- Unicode collation algorithms

## Dependencies
- Depends on: ICU libraries (in Lib/), Common utilities
- Used by: Components needing proper linguistic sorting (LexText, xWorks)

## Build Information
- C# class library with native interop
- Includes comprehensive test suite
- Build with MSBuild or Visual Studio

## Testing
- Run tests: `dotnet test ManagedLgIcuCollator/ManagedLgIcuCollatorTests/ManagedLgIcuCollatorTests.csproj`
- Tests cover various collation scenarios and locales

## Entry Points
- Provides collation services for linguistic text sorting
- Used by UI components displaying sorted linguistic data

## Related Folders
- **Lib/** - Contains ICU native libraries
- **Kernel/** - May provide low-level string utilities
- **LexText/** - Uses collation for lexicon entry sorting
- **xWorks/** - Uses collation in various data displays
