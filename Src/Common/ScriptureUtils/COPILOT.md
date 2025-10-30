---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ScriptureUtils

## Purpose
Scripture-specific utilities and Paratext integration support.
Provides specialized handling for biblical text references, Paratext project integration,
scripture navigation, and biblical text structure. Enables FieldWorks to work effectively
with Bible translation projects and interoperate with Paratext software.

## Key Components
### Key Classes
- **ParatextHelper**
- **Manager**
- **ScrReferencePositionComparer**
- **ReferencePositionType**
- **ScriptureReferenceComparer**
- **ScriptureProvider**
- **ScrReferencePositionComparerTests**
- **ScriptureReferenceComparerTests**
- **MockParatextHelper**
- **ParatextHelperUnitTests**

### Key Interfaces
- **IParatextHelper**
- **IScrText**
- **ILexicalProject**
- **ITranslationInfo**
- **IScriptureProviderBookSet**
- **IScriptureProviderParser**
- **IUsfmToken**
- **IVerseRef**

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

## Entry Points
- Paratext data providers
- Scripture reference comparison and sorting
- Biblical text handling utilities

## Related Folders
- **ParatextImport/** - Uses ScriptureUtils for data import
- **Paratext8Plugin/** - Modern Paratext integration
- **FwParatextLexiconPlugin/** - Lexicon integration with Paratext

## Code Evidence
*Analysis based on scanning 10 source files*

- **Classes found**: 11 public classes
- **Interfaces found**: 14 public interfaces
- **Namespaces**: SIL.FieldWorks.Common.ScriptureUtils

## Interfaces and Data Models

- **ILexicalProject** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **IParatextHelper** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **IScrText** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **IScrVerse** (interface)
  - Path: `ScriptureProvider.cs`
  - Public interface definition

- **IScriptureProvider** (interface)
  - Path: `ScriptureProvider.cs`
  - Public interface definition

- **IScriptureProviderBookSet** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **IScriptureProviderMetadata** (interface)
  - Path: `ScriptureProvider.cs`
  - Public interface definition

- **IScriptureProviderParser** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **IScriptureProviderParserState** (interface)
  - Path: `ScriptureProvider.cs`
  - Public interface definition

- **IScriptureProviderStyleSheet** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **ITag** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **ITranslationInfo** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **IUsfmToken** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **IVerseRef** (interface)
  - Path: `ParatextHelper.cs`
  - Public interface definition

- **Manager** (class)
  - Path: `ParatextHelper.cs`
  - Public class implementation

- **MockParatextHelper** (class)
  - Path: `ScriptureUtilsTests/ParatextHelperTests.cs`
  - Public class implementation

- **ParatextHelper** (class)
  - Path: `ParatextHelper.cs`
  - Public class implementation

- **ReferencePositionType** (class)
  - Path: `ScrReferencePositionComparer.cs`
  - Public class implementation

- **ScrReferencePositionComparer** (class)
  - Path: `ScrReferencePositionComparer.cs`
  - Public class implementation

- **ScriptureProvider** (class)
  - Path: `ScriptureProvider.cs`
  - Public class implementation

- **ScriptureReferenceComparer** (class)
  - Path: `ScriptureReferenceComparer.cs`
  - Public class implementation

- **ProjectType** (enum)
  - Path: `ParatextHelper.cs`

- **ScrStyleType** (enum)
  - Path: `ParatextHelper.cs`

- **TokenType** (enum)
  - Path: `ParatextHelper.cs`

## References

- **Project files**: ScriptureUtils.csproj, ScriptureUtilsTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, PT7ScrTextWrapper.cs, Paratext7Provider.cs, ParatextHelper.cs, ParatextHelperTests.cs, ScrReferencePositionComparer.cs, ScrReferencePositionComparerTests.cs, ScriptureProvider.cs, ScriptureReferenceComparer.cs, ScriptureReferenceComparerTests.cs
- **Source file count**: 10 files
- **Data file count**: 0 files
