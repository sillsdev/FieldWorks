---
last-reviewed: 2025-10-31
last-reviewed-tree: a486b8c52d33bd2fde61d14b6fc651d9d308fe0acbb590c43e673a7b6ee64039
status: draft
---

# FwParatextLexiconPlugin COPILOT summary

## Purpose
Integration plugin enabling Paratext to access FieldWorks lexicon data. Implements Paratext.LexicalContracts interfaces (LexiconPlugin, LexiconPluginV2) allowing Paratext users to query and utilize FLEx lexicons during translation work. FwLexiconPlugin main class provides bidirectional access between Paratext and FieldWorks lexical data. FdoLexicon exposes lexicon as Lexicon/LexiconV2 interface. Supporting classes handle project selection (ChooseFdoProjectForm), data structures (FdoLexEntryLexeme, FdoWordAnalysis), and UI integration. Enables translators to leverage rich FLEx lexical resources within Paratext workflow.

## Architecture
C# class library (.NET Framework 4.6.2) implementing Paratext plugin contracts. FwLexiconPlugin (attributed with [LexiconPlugin]) is main plugin class maintaining lexicon cache (FdoLexiconCollection) and LCM cache (LcmCacheCollection). COM activation context management for FDO interop. ILRepack merges dependencies into single plugin DLL. Test project FwParatextLexiconPluginTests validates functionality. 4026 lines total.

## Key Components
- **FwLexiconPlugin** class (FwLexiconPlugin.cs): Main plugin entry point
  - Implements LexiconPlugin, LexiconPluginV2 (Paratext contracts)
  - [LexiconPlugin(ID = "FieldWorks", DisplayName = "FieldWorks Language Explorer")]
  - Caching: FdoLexiconCollection (5 lexicons), LcmCacheCollection (5 caches)
  - Thread-safe: m_syncRoot for synchronization
  - COM activation context: Ensures proper COM loading for FDO calls
  - ValidateLexicalProject(): Check if project/language valid
  - GetLexicon(): Retrieve lexicon for Paratext access
- **FwLexiconPluginV2** class (FwLexiconPluginV2.cs): V2 interface wrapper
- **FdoLexicon** (FdoLexicon.cs): Exposes FLEx lexicon as Paratext Lexicon/LexiconV2
  - Wraps LcmCache providing access to lexical entries
  - Implements Paratext lexicon interfaces
  - Raises events: LexemeAdded, SenseAdded, GlossAdded for Paratext notifications
- **FdoLexEntryLexeme** (FdoLexEntryLexeme.cs): Lexical entry representation
  - Lexeme interface implementation for Paratext
  - Provides sense, gloss, and analysis access
- **FdoWordAnalysis, FdoWordAnalysisV2** (FdoWordAnalysis.cs, FdoWordAnalysisV2.cs): Word analysis data
- **FdoWordformLexeme** (FdoWordformLexeme.cs): Wordform lexeme representation
- **FdoLexicalRelation** (FdoLexicalRelation.cs): Lexical relationship data
- **FdoSemanticDomain** (FdoSemanticDomain.cs): Semantic domain information
- **FdoLanguageText** (FdoLanguageText.cs): Language text representation
- **ChooseFdoProjectForm** (ChooseFdoProjectForm.cs/.Designer.cs/.resx): Project selection dialog
  - UI for Paratext users to select FLEx project
- **FilesToRestoreAreOlder** (FilesToRestoreAreOlder.cs/.Designer.cs/.resx): Restore warning dialog
- **ProjectExistsForm** (ProjectExistsForm.cs/.Designer.cs/.resx): Project exists dialog
- **LexemeKey** (LexemeKey.cs): Lexeme key for caching/lookup
- **ParatextLexiconPluginDirectoryFinder** (ParatextLexiconPluginDirectoryFinder.cs): Directory location
- **ParatextLexiconPluginLcmUI** (ParatextLexiconPluginLcmUI.cs): LCM UI integration
- **ParatextLexiconPluginProjectId** (ParatextLexiconPluginProjectId.cs): Project identifier
- **ParatextLexiconPluginRegistryHelper** (ParatextLexiconPluginRegistryHelper.cs): Registry access
- **ParatextLexiconPluginThreadedProgress** (ParatextLexiconPluginThreadedProgress.cs): Progress reporting
- **Event args**: FdoLexemeAddedEventArgs, FdoLexiconGlossAddedEventArgs, FdoLexiconSenseAddedEventArgs

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library (plugin DLL)
- **Paratext.LexicalContracts**: Paratext plugin interfaces
- **SIL.LCModel**: FieldWorks data model access
- **COM activation context**: For FDO COM object loading
- **ILRepack**: Merges dependencies into single plugin DLL
- Windows Forms for UI dialogs

## Dependencies

### Upstream (consumes)
- **Paratext.LexicalContracts**: Plugin interfaces (LexiconPlugin, LexiconPluginV2, Lexicon, etc.)
- **SIL.LCModel**: FieldWorks data model (LcmCache, ILexEntry)
- **Common/FwUtils**: Utilities (FwRegistryHelper, FwUtils.InitializeIcu)
- **SIL.WritingSystems**: Writing system support
- **Windows Forms**: Dialog UI

### Downstream (consumed by)
- **Paratext**: Loads plugin to access FLEx lexicons
- Translators using Paratext with FLEx lexical resources

## Interop & Contracts
- **LexiconPlugin interface**: Paratext contract for lexicon plugins
- **LexiconPluginV2 interface**: V2 Paratext contract
- **[LexiconPlugin] attribute**: Paratext plugin discovery
- **COM activation context**: Critical for FDO COM object loading
  - All public methods must activate context before FDO calls
  - Avoid deferred execution (LINQ, yield) crossing context boundaries
- **Events**: LexemeAdded, SenseAdded, GlossAdded for Paratext notifications

## Threading & Performance
- **Thread-safe**: m_syncRoot lock for cache access
- **Caching**: CacheSize=5 for lexicons and LCM caches
- **Performance**: Cache hits avoid repeated FLEx project loading
- **COM threading**: Activation context management

## Config & Feature Flags
- **CacheSize**: 5 lexicons/caches maintained
- Registry settings via ParatextLexiconPluginRegistryHelper
- Directory locations via ParatextLexiconPluginDirectoryFinder

## Build Information
- **Project file**: FwParatextLexiconPlugin.csproj (net462, OutputType=Library)
- **Test project**: FwParatextLexiconPluginTests/
- **ILRepack**: ILRepack.targets merges dependencies into single DLL
- **Output**: FwParatextLexiconPlugin.dll (deployed to Paratext plugins)
- **Build**: Via top-level FW.sln
- **Run tests**: `dotnet test FwParatextLexiconPluginTests/`

## Interfaces and Data Models

- **FwLexiconPlugin** (FwLexiconPlugin.cs)
  - Purpose: Main Paratext plugin entry point
  - Inputs: ValidateLexicalProject(projectId, langId), GetLexicon(scrTextName, projectId, langId)
  - Outputs: LexicalProjectValidationResult, Lexicon/LexiconV2
  - Notes: Thread-safe; COM activation context required for FDO; caches 5 lexicons

- **LexiconPlugin, LexiconPluginV2 interfaces** (Paratext.LexicalContracts)
  - Purpose: Paratext contracts for lexicon access
  - Inputs: Project/language identifiers
  - Outputs: Lexicon objects
  - Notes: Implemented by FwLexiconPlugin

- **FdoLexicon** (FdoLexicon.cs)
  - Purpose: Exposes FLEx lexicon to Paratext as Lexicon/LexiconV2
  - Inputs: LcmCache
  - Outputs: Lexical entries, senses, glosses
  - Notes: Raises events when lexicon changes

- **FdoLexEntryLexeme** (FdoLexEntryLexeme.cs)
  - Purpose: Represents lexical entry for Paratext
  - Inputs: ILexEntry from FLEx
  - Outputs: Lexeme data (senses, glosses, analyses)
  - Notes: Implements Paratext Lexeme interface

- **ChooseFdoProjectForm** (ChooseFdoProjectForm.cs)
  - Purpose: UI for selecting FLEx project in Paratext
  - Inputs: Available FLEx projects
  - Outputs: Selected project ID
  - Notes: Dialog shown to Paratext users

## Entry Points
- **Paratext loads plugin**: FwLexiconPlugin discovered via [LexiconPlugin] attribute
- Translators access via Paratext UI (Tools > Lexicons or similar)

## Test Index
- **Test project**: FwParatextLexiconPluginTests/
- **Run tests**: `dotnet test FwParatextLexiconPluginTests/`
- **Coverage**: Plugin initialization, lexicon access, caching

## Usage Hints
- **Installation**: Deploy FwParatextLexiconPlugin.dll to Paratext plugins folder
- **Paratext workflow**: Translator opens Paratext project, accesses FLEx lexicon via plugin
- **COM context**: All FDO operations must occur within activated activation context
- **Caching**: Plugin caches up to 5 lexicons; manage cache appropriately
- **Events**: Paratext receives notifications when FLEx lexicon changes
- **ILRepack**: Dependencies merged into single DLL for easy deployment

## Related Folders
- **Paratext8Plugin/**: Newer Paratext 8-specific integration
- **ParatextImport/**: Import Paratext data into FLEx (reverse direction)
- **Common/ScriptureUtils**: Paratext utilities

## References
- **Project files**: FwParatextLexiconPlugin.csproj (net462), FwParatextLexiconPluginTests/, ILRepack.targets
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: FwLexiconPlugin.cs, FwLexiconPluginV2.cs, FdoLexicon.cs, FdoLexEntryLexeme.cs, FdoWordAnalysis.cs, and others
- **Total lines of code**: 4026
- **Output**: FwParatextLexiconPlugin.dll (plugin for Paratext)
- **Namespace**: SIL.FieldWorks.ParatextLexiconPlugin
- **Icon**: question.ico