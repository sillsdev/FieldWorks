---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ScriptureUtils

## Purpose
Scripture-specific utilities for working with biblical texts. Provides support for Paratext integration, scripture references, and biblical text handling.

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
