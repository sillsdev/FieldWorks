---
last-reviewed: 2025-10-31
last-reviewed-tree: 6a49e787206a05cc0f1f25e52b960410d3922c982f322c92e892575d6216836d
status: reviewed
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
  - paratext-provider
  - scripture-text-wrappers
  - parser-state
  - alert-system
  - technology-stack
  - dependencies
  - interop--contracts
  - referenced-by
  - threading--performance
  - config--feature-flags
  - build-information
  - interfaces-and-data-models
  - entry-points
  - test-index
  - usage-hints
  - related-folders
  - references

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Paratext8Plugin

## Purpose
Paratext 8 integration adapter implementing IScriptureProvider interface for FLEx↔Paratext data interchange. Wraps Paratext.Data API (Paratext SDK v8) to provide FieldWorks-compatible scripture project access, verse reference handling (PT8VerseRefWrapper), text data wrappers (PT8ScrTextWrapper), and USFM parser state (PT8ParserStateWrapper). Enables Send/Receive synchronization between FLEx back translations and Paratext translation projects, supporting collaborative translation workflows where linguistic analysis (FLEx) informs translation (Paratext8) and vice versa.

### Referenced By

- [Paratext Integration](../../openspec/specs/integration/external/paratext.md#behavior) — Paratext provider plugin

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

### Referenced By

- [External APIs](../../openspec/specs/architecture/interop/external-apis.md#external-integration-patterns) — Paratext provider integration

## Threading & Performance
Synchronous operations. Relies on Paratext SDK threading model. MEF loads plugin once per AppDomain.

## Config & Feature Flags
MEF versioning: ExportMetadata("Version", "8") for PT8 only. SettingsDirectory property. Project filtering: NonEditableTexts, ScrTextNames. Alert routing via ParatextAlert().

## Build Information
C# library (net48). Build via `msbuild Paratext8Plugin.csproj`. Output: Paratext8Plugin.dll. Loaded via MEF.

## Interfaces and Data Models
IScriptureProvider implementation via MEF [Export]. Wrappers: PT8ScrTextWrapper (IScrText), PT8VerseRefWrapper (IVerseRef), PT8ParserStateWrapper (parser state).

## Entry Points
MEF discovery via [Export(typeof(IScriptureProvider))] with version "8" metadata. Initialize() called by ScriptureUtils. Used by FLEx Send/Receive and ParatextImport.

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
- **Common/ScriptureUtils/**: IScriptureProvider interface
- **FwParatextLexiconPlugin/**: Lexicon integration
- **ParatextImport/**: Import pipeline

## References
7 C# files (~546 lines). Key: Paratext8Provider.cs, PT8ScrTextWrapper.cs, PT8VerseRefWrapper.cs. See `.cache/copilot/diff-plan.json` for file listings.
