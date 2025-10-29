# Common/ScriptureUtils

## Purpose
Scripture-specific utilities for working with biblical texts. Provides support for Paratext integration, scripture references, and biblical text handling.

## Key Components
- **ScriptureUtils.csproj** - Scripture utilities library
- **PT7ScrTextWrapper.cs** - Paratext 7 text wrapper
- **Paratext7Provider.cs** - Paratext 7 data provider
- **ParatextHelper.cs** - Paratext integration helpers
- **ScriptureProvider.cs** - Scripture data provider abstraction
- **ScrReferencePositionComparer.cs** - Reference sorting
- **ScriptureReferenceComparer.cs** - Reference comparison
- **ScriptureUtilsTests/** - Test suite

## Technology Stack
- C# .NET
- Paratext integration APIs
- Biblical reference handling

## Dependencies
- Depends on: Paratext SDK, Common/FwUtils
- Used by: Scripture-related features in xWorks and specialized modules

## Build Information
- C# class library project
- Build via: `dotnet build ScriptureUtils.csproj`
- Includes test suite

## Testing
- Run tests: `dotnet test ScriptureUtils/ScriptureUtilsTests/`
- Tests cover reference handling and Paratext integration

## Entry Points
- Paratext data providers
- Scripture reference comparison and sorting
- Biblical text handling utilities

## Related Folders
- **ParatextImport/** - Uses ScriptureUtils for data import
- **Paratext8Plugin/** - Modern Paratext integration
- **FwParatextLexiconPlugin/** - Lexicon integration with Paratext
