---
last-reviewed: 2025-10-31
last-reviewed-tree: 68897dd419050f2e9c0f59ed91a75f5770ebd5aef2a9185ea42583a6d9d208d9
status: reviewed
---

# Paratext8Plugin

## Purpose
Paratext 8 integration adapter implementing IScriptureProvider interface for FLEx↔Paratext data interchange. Wraps Paratext.Data API (Paratext SDK v8) to provide FieldWorks-compatible scripture project access, verse reference handling (PT8VerseRefWrapper), text data wrappers (PT8ScrTextWrapper), and USFM parser state (PT8ParserStateWrapper). Enables Send/Receive synchronization between FLEx back translations and Paratext translation projects, supporting collaborative translation workflows where linguistic analysis (FLEx) informs translation (Paratext8) and vice versa.

## Architecture
C# library (net462) with 7 source files (~546 lines). Implements MEF-based plugin pattern via [Export(typeof(IScriptureProvider))] attribute with [ExportMetadata("Version", "8")] for Paratext 8 API versioning. Wraps Paratext.Data types (ScrText, VerseRef, ScrParserState) with FLEx-compatible interfaces.

## Key Components

### Paratext Provider
- **Paratext8Provider**: Implements IScriptureProvider via MEF [Export]. Wraps Paratext.Data API: ScrTextCollection for project enumeration, ParatextData.Initialize() for SDK setup, Alert.Implementation = ParatextAlert() for alert bridging. Provides project filtering (NonEditableTexts, ScrTextNames), scripture text wrapping (ScrTexts() → PT8ScrTextWrapper), verse reference creation (MakeVerseRef() → PT8VerseRefWrapper), parser state (GetParserState() → PT8ParserStateWrapper).
  - Properties:
    - SettingsDirectory: Paratext settings folder (ScrTextCollection.SettingsDirectory)
    - NonEditableTexts: Resource/inaccessible projects
    - ScrTextNames: All accessible projects
    - MaximumSupportedVersion: Paratext version installed
    - IsInstalled: Checks ParatextInfo.IsParatextInstalled
  - Methods:
    - Initialize(): Sets up ParatextData SDK and alert system
    - RefreshScrTexts(): Refreshes project list
    - ScrTexts(): Returns wrapped scripture texts
    - Get(string project): Gets specific project wrapper
    - MakeScrText(string): Creates new ScrText wrapper
    - MakeVerseRef(bookNum, chapter, verse): Creates verse reference
    - GetParserState(ptProjectText, ptCurrBook): Creates parser state wrapper
  - MEF: [Export(typeof(IScriptureProvider))], [ExportMetadata("Version", "8")]

### Scripture Text Wrappers
- **PT8ScrTextWrapper**: Wraps Paratext.Data.ScrText to implement IScrText interface. Provides FLEx-compatible access to Paratext project properties, text data, verse references. (Implementation details in PTScrTextWrapper.cs)
- **PT8VerseRefWrapper**: Wraps Paratext.Data.VerseRef to implement IVerseRef interface. Provides book/chapter/verse navigation compatible with FLEx scripture reference system. (Implementation in PT8VerseRefWrapper.cs)
- **Pt8VerseWrapper**: Additional verse wrapping utilities. (Implementation in Pt8VerseWrapper.cs)

### Parser State
- **PT8ParserStateWrapper**: Implements IScriptureProviderParserState wrapping Paratext.Data.ScrParserState. Maintains USFM parsing context during scripture text processing. Caches wrapped token lists (wrappedTokenList) for identity comparison on UpdateState() calls.
  - Internal: ptParserState (ScrParserState), wrappedTokenList (List<IUsfmToken>)

### Alert System
- **ParatextAlert**: Implements Paratext alert interface bridging Paratext alert dialogs to FLEx UI context.

## Technology Stack
TBD - populate from code. See auto-generated hints below.

## Dependencies
- **External**: Paratext.Data (Paratext SDK v8 - ScrText, ScrTextCollection, VerseRef, ScrParserState, ParatextData.Initialize()), PtxUtils (Paratext utilities), SIL.Scripture (IVerseRef, scripture data model), Common/ScriptureUtils (ScriptureProvider.IScriptureProvider interface, IScrText, IUsfmToken), System.ComponentModel.Composition (MEF [Export]/[ExportMetadata])
- **Internal (upstream)**: Common/ScriptureUtils (IScriptureProvider interface definition)
- **Consumed by**: Common/ScriptureUtils (dynamically loads Paratext8Plugin via MEF when Paratext 8 detected), FLEx Send/Receive features (back translation sync), ParatextImport (may use provider for import operations)

## Interop & Contracts
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
- Project type: C# class library (net462)
- Build: `msbuild Paratext8Plugin.csproj` or `dotnet build` (from FW.sln)
- Output: Paratext8Plugin.dll
- Dependencies: Paratext.Data (Paratext SDK NuGet or local assembly), PtxUtils, SIL.Scripture, Common/ScriptureUtils, System.ComponentModel.Composition (MEF)
- Deployment: Loaded dynamically via MEF when Paratext 8 installed and version match detected
- Config: App.config for assembly binding redirects

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

## Related Folders
- **Common/ScriptureUtils/**: Defines IScriptureProvider interface, loads Paratext8Plugin via MEF
- **FwParatextLexiconPlugin/**: Separate plugin for Paratext lexicon integration (FLEx→Paratext lexicon export)
- **ParatextImport/**: Imports Paratext projects/books into FLEx (may use Paratext8Provider)
- **LexText/**: Scripture text data in FLEx that synchronizes with Paratext

## References

- **Project files**: Paratext8Plugin.csproj, Paratext8PluginTests.csproj
- **Target frameworks**: net462
- **Key C# files**: AssemblyInfo.cs, PT8VerseRefWrapper.cs, PTScrTextWrapper.cs, Paratext8Provider.cs, ParatextAlert.cs, ParatextDataIntegrationTests.cs, Pt8VerseWrapper.cs
- **Source file count**: 7 files
- **Data file count**: 1 files

## References (auto-generated hints)
- Project files:
  - Src/Paratext8Plugin/ParaText8PluginTests/Paratext8PluginTests.csproj
  - Src/Paratext8Plugin/Paratext8Plugin.csproj
- Key C# files:
  - Src/Paratext8Plugin/PT8VerseRefWrapper.cs
  - Src/Paratext8Plugin/PTScrTextWrapper.cs
  - Src/Paratext8Plugin/ParaText8PluginTests/ParatextDataIntegrationTests.cs
  - Src/Paratext8Plugin/Paratext8Provider.cs
  - Src/Paratext8Plugin/ParatextAlert.cs
  - Src/Paratext8Plugin/Properties/AssemblyInfo.cs
  - Src/Paratext8Plugin/Pt8VerseWrapper.cs
- Data contracts/transforms:
  - Src/Paratext8Plugin/ParaText8PluginTests/App.config
## Test Information
- Test project: ParaText8PluginTests
- Test file: ParatextDataIntegrationTests.cs (includes MockScriptureProvider for test isolation)
- Run: `dotnet test` or Test Explorer in Visual Studio
- Test coverage: Provider initialization, project enumeration, text wrapping, verse reference creation, parser state management, Paratext installation detection

## Code Evidence
*Analysis based on scanning 6 source files*

- **Classes found**: 5 public classes
- **Namespaces**: Paratext8Plugin
