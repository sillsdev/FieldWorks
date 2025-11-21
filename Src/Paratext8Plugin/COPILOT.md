---
last-reviewed: 2025-10-31
last-reviewed-tree: 6a49e787206a05cc0f1f25e52b960410d3922c982f322c92e892575d6216836d
status: reviewed
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/Paratext8Plugin. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# Paratext8Plugin

## Purpose
Paratext 8 integration adapter implementing IScriptureProvider interface for FLEx↔Paratext data interchange. Wraps Paratext.Data API (Paratext SDK v8) to provide FieldWorks-compatible scripture project access, verse reference handling (PT8VerseRefWrapper), text data wrappers (PT8ScrTextWrapper), and USFM parser state (PT8ParserStateWrapper). Enables Send/Receive synchronization between FLEx back translations and Paratext translation projects, supporting collaborative translation workflows where linguistic analysis (FLEx) informs translation (Paratext8) and vice versa.

## Architecture
C# library (net48) with 7 source files (~546 lines). Implements MEF-based plugin pattern via [Export(typeof(IScriptureProvider))] attribute with [ExportMetadata("Version", "8")] for Paratext 8 API versioning. Wraps Paratext.Data types (ScrText, VerseRef, ScrParserState) with FLEx-compatible interfaces.

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
- **Language**: C#
- **Target framework**: .NET Framework 4.8.x (net48)
- **Plugin pattern**: MEF (Managed Extensibility Framework)
  - Export attributes: [Export(typeof(IScriptureProvider))], [ExportMetadata("Version", "8")]
- **Key libraries**:
  - Paratext.Data (Paratext SDK v8 - ScrText, ScrTextCollection, VerseRef, ScrParserState)
  - PtxUtils (Paratext utilities)
  - SIL.Scripture (IVerseRef, scripture data model)
  - Common/ScriptureUtils (IScriptureProvider, IScrText, IUsfmToken interfaces)
  - System.ComponentModel.Composition (MEF framework)
- **External SDK**: Paratext SDK v8 (NuGet or local assembly)
- **Config**: App.config for assembly binding redirects

## Dependencies
- **External**: Paratext.Data (Paratext SDK v8 - ScrText, ScrTextCollection, VerseRef, ScrParserState, ParatextData.Initialize()), PtxUtils (Paratext utilities), SIL.Scripture (IVerseRef, scripture data model), Common/ScriptureUtils (ScriptureProvider.IScriptureProvider interface, IScrText, IUsfmToken), System.ComponentModel.Composition (MEF [Export]/[ExportMetadata])
- **Internal (upstream)**: Common/ScriptureUtils (IScriptureProvider interface definition)
- **Consumed by**: Common/ScriptureUtils (dynamically loads Paratext8Plugin via MEF when Paratext 8 detected), FLEx Send/Receive features (back translation sync), ParatextImport (may use provider for import operations)

## Interop & Contracts
- **MEF plugin contract**: IScriptureProvider from Common/ScriptureUtils
  - Export: [Export(typeof(IScriptureProvider))]
  - Metadata: [ExportMetadata("Version", "8")] for Paratext 8 API versioning
  - Discovery: Dynamically loaded by ScriptureUtils when Paratext 8 detected
- **Paratext SDK interop**: Wraps Paratext.Data types
  - ScrText → PT8ScrTextWrapper (IScrText)
  - VerseRef → PT8VerseRefWrapper (IVerseRef)
  - ScrParserState → PT8ParserStateWrapper (IScriptureProviderParserState)
- **Alert bridging**: ParatextAlert implements Paratext alert interface
  - Purpose: Route Paratext alerts to FLEx UI context
  - Integration: Alert.Implementation = new ParatextAlert()
- **Data contracts**:
  - IScrText: Scripture project data access (text, references, properties)
  - IVerseRef: Book/chapter/verse navigation
  - IUsfmToken: USFM markup parsing tokens
  - IScriptureProviderParserState: USFM parser context
- **Versioning**: ExportMetadata("Version", "8") distinguishes from other Paratext plugin versions
- **Installation check**: IsInstalled property checks ParatextInfo.IsParatextInstalled

## Threading & Performance
- **UI thread affinity**: Paratext SDK operations likely require UI thread (WinForms-based)
- **Synchronous operations**: All provider methods synchronous
  - Project enumeration: ScrTextCollection queries (fast)
  - Text access: On-demand loading via Paratext SDK
- **Performance characteristics**:
  - Initialize(): One-time SDK setup (fast)
  - RefreshScrTexts(): Re-enumerates projects (fast unless many projects)
  - ScrTexts(): Returns cached or wrapped ScrText objects (fast)
  - Text data access: Depends on Paratext SDK caching and file I/O
- **No manual threading**: Relies on Paratext SDK threading model
- **Caching**: PT8ParserStateWrapper caches wrapped token lists (wrappedTokenList) for identity checks
- **MEF loading**: Plugin loaded once per AppDomain when Paratext 8 detected

## Config & Feature Flags
- **MEF versioning**: ExportMetadata("Version", "8") ensures plugin loaded only for Paratext 8
  - ScriptureUtils checks installed Paratext version and selects matching plugin
- **Paratext settings directory**: SettingsDirectory property exposes ScrTextCollection.SettingsDirectory
  - Default: %LOCALAPPDATA%\SIL\Paratext8Projects (or equivalent)
- **Project filtering**:
  - NonEditableTexts: Resource projects, inaccessible projects (read-only)
  - ScrTextNames: All accessible projects for user
- **Alert configuration**: Alert.Implementation = ParatextAlert() routes Paratext alerts
- **Installation detection**: IsInstalled property checks ParatextInfo.IsParatextInstalled
  - Returns false if Paratext 8 not installed (graceful degradation)
- **App.config**: Assembly binding redirects for Paratext SDK dependencies
- **No feature flags**: Behavior entirely determined by Paratext SDK and project properties

## Build Information
- Project type: C# class library (net48)
- Build: `msbuild Paratext8Plugin.csproj` or `dotnet build` (from FieldWorks.sln)
- Output: Paratext8Plugin.dll
- Dependencies: Paratext.Data (Paratext SDK NuGet or local assembly), PtxUtils, SIL.Scripture, Common/ScriptureUtils, System.ComponentModel.Composition (MEF)
- Deployment: Loaded dynamically via MEF when Paratext 8 installed and version match detected
- Config: App.config for assembly binding redirects

## Interfaces and Data Models

### Interfaces Implemented
- **IScriptureProvider** (path: Src/Common/ScriptureUtils/)
  - Purpose: Abstract scripture data access for FLEx↔Paratext integration
  - Methods: Initialize(), RefreshScrTexts(), ScrTexts(), Get(), MakeScrText(), MakeVerseRef(), GetParserState()
  - Properties: SettingsDirectory, NonEditableTexts, ScrTextNames, MaximumSupportedVersion, IsInstalled
  - Notes: MEF [Export] for dynamic plugin loading

- **IScrText** (implemented by PT8ScrTextWrapper)
  - Purpose: Scripture project data access
  - Methods: Text access, reference navigation, project properties
  - Notes: Wraps Paratext.Data.ScrText

- **IVerseRef** (implemented by PT8VerseRefWrapper)
  - Purpose: Book/chapter/verse reference handling
  - Methods: Navigation, comparison, formatting
  - Notes: Wraps Paratext.Data.VerseRef

- **IScriptureProviderParserState** (implemented by PT8ParserStateWrapper)
  - Purpose: USFM parsing context maintenance
  - Properties: Token list, parser state
  - Notes: Wraps Paratext.Data.ScrParserState

### Data Models (Wrappers)
- **PT8ScrTextWrapper** (path: Src/Paratext8Plugin/PTScrTextWrapper.cs)
  - Purpose: Adapt Paratext ScrText to FLEx IScrText interface
  - Shape: Wraps ScrText, exposes project name, text data, references
  - Consumers: FLEx Send/Receive, ParatextImport

- **PT8VerseRefWrapper** (path: Src/Paratext8Plugin/PT8VerseRefWrapper.cs)
  - Purpose: Adapt Paratext VerseRef to FLEx IVerseRef interface
  - Shape: Book (int), Chapter (int), Verse (int), navigation methods
  - Consumers: Scripture reference navigation in FLEx

- **PT8ParserStateWrapper** (path: Src/Paratext8Plugin/)
  - Purpose: Maintain USFM parsing state for scripture processing
  - Shape: ScrParserState wrapper, cached token list
  - Consumers: USFM import/export operations

## Entry Points
- **MEF discovery**: ScriptureUtils dynamically loads plugin via MEF
  - Export: [Export(typeof(IScriptureProvider))]
  - Metadata filter: [ExportMetadata("Version", "8")] matches Paratext 8 installation
  - Loading: CompositionContainer.GetExportedValue<IScriptureProvider>() when Paratext 8 detected
- **Initialization**: Paratext8Provider.Initialize() called by ScriptureUtils
  - Setup: ParatextData.Initialize(), Alert.Implementation = ParatextAlert()
  - One-time per AppDomain
- **Usage pattern** (ScriptureUtils):
  1. Detect Paratext 8 installation
  2. Load Paratext8Plugin via MEF
  3. Call Initialize()
  4. Access projects via ScrTexts() or Get(projectName)
  5. Wrap verse references via MakeVerseRef()
  6. Parse USFM via GetParserState()
- **Common consumers**:
  - FLEx Send/Receive: Synchronize back translations with Paratext projects
  - ParatextImport: Import Paratext books into FLEx scripture data
  - Scripture reference navigation: Verse lookup in Paratext projects

## Test Index
- **Test project**: ParaText8PluginTests/Paratext8PluginTests.csproj
- **Test file**: ParatextDataIntegrationTests.cs
  - Includes MockScriptureProvider for test isolation
- **Test coverage**:
  - Provider initialization: Initialize() setup, alert bridging
  - Installation detection: IsInstalled property checks
  - Project enumeration: ScrTexts(), ScrTextNames, NonEditableTexts
  - Text wrapping: PT8ScrTextWrapper creation and access
  - Verse reference: MakeVerseRef() creates PT8VerseRefWrapper
  - Parser state: GetParserState() returns PT8ParserStateWrapper
  - MEF metadata: ExportMetadata("Version", "8") attribute
- **Test approach**: Integration tests requiring Paratext 8 SDK (or mocks for CI)
- **Test runners**:
  - Visual Studio Test Explorer
  - Via FieldWorks.sln top-level build
- **Prerequisites for tests**: Paratext 8 SDK assemblies (Paratext.Data.dll, PtxUtils.dll)

## Usage Hints
- **Prerequisites**: Paratext 8 must be installed
  - Plugin only loads if IsInstalled returns true
  - Paratext SDK assemblies (Paratext.Data.dll) must be available
- **FLEx Send/Receive setup**:
  1. Install Paratext 8
  2. Create or open Paratext project
  3. In FLEx, configure Send/Receive to link with Paratext project
  4. FLEx uses Paratext8Plugin to access project data
- **Project access pattern**:
  ```csharp
  var provider = GetParatextProvider(); // via MEF
  provider.Initialize();
  var projects = provider.ScrTexts();
  var targetProject = provider.Get("ProjectName");
  var verseRef = provider.MakeVerseRef(1, 1, 1); // Genesis 1:1
  ```
- **Versioning**: ExportMetadata("Version", "8") ensures correct plugin for Paratext 8
  - Paratext 9 would use separate Paratext9Plugin with Version="9"
- **Alert handling**: ParatextAlert routes Paratext messages to FLEx UI
  - User sees FLEx-style dialogs instead of Paratext-native alerts
- **Debugging tips**:
  - Verify Paratext 8 installation: Check ParatextInfo.IsParatextInstalled
  - MEF loading issues: Check composition errors in ScriptureUtils
  - Project access: Verify user has permission to access Paratext projects
- **Common pitfalls**:
  - Paratext not installed: Plugin won't load, fallback to non-Paratext mode
  - Version mismatch: Wrong plugin version loaded if metadata incorrect
  - Project permissions: Some Paratext projects read-only (NonEditableTexts)
- **Extension point**: Implement IScriptureProvider for other Paratext versions or scripture sources
- **Related plugins**: FwParatextLexiconPlugin handles lexicon integration separately

## Related Folders
- **Common/ScriptureUtils/**: Defines IScriptureProvider interface, loads Paratext8Plugin via MEF
- **FwParatextLexiconPlugin/**: Separate plugin for Paratext lexicon integration (FLEx→Paratext lexicon export)
- **ParatextImport/**: Imports Paratext projects/books into FLEx (may use Paratext8Provider)
- **LexText/**: Scripture text data in FLEx that synchronizes with Paratext

## References

- **Project files**: Paratext8Plugin.csproj, Paratext8PluginTests.csproj
- **Target frameworks**: net48
- **Key C# files**: AssemblyInfo.cs, PT8VerseRefWrapper.cs, PTScrTextWrapper.cs, Paratext8Provider.cs, ParatextAlert.cs, ParatextDataIntegrationTests.cs, Pt8VerseWrapper.cs
- **Source file count**: 7 files
- **Data file count**: 1 files

## Auto-Generated Project and File References
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