---
last-reviewed: 2025-10-31
last-reviewed-tree: 40947eb2517b52a47348601f466166915ba1c66369b07378d44191e713efc61a
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - architecture
  - key-components
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

# FwParatextLexiconPlugin COPILOT summary

## Purpose
Integration plugin enabling Paratext to access FieldWorks lexicon data. Implements Paratext.LexicalContracts interfaces (LexiconPlugin, LexiconPluginV2) allowing Paratext users to query and utilize FLEx lexicons during translation work. FwLexiconPlugin main class provides bidirectional access between Paratext and FieldWorks lexical data. FdoLexicon exposes lexicon as Lexicon/LexiconV2 interface. Supporting classes handle project selection (ChooseFdoProjectForm), data structures (FdoLexEntryLexeme, FdoWordAnalysis), and UI integration. Enables translators to leverage rich FLEx lexical resources within Paratext workflow.

### Referenced By

- [Paratext Integration](../../openspec/specs/integration/external/paratext.md#behavior) — Lexicon integration

## Architecture
C# class library (.NET Framework 4.8.x) implementing Paratext plugin contracts. FwLexiconPlugin (attributed with [LexiconPlugin]) is main plugin class maintaining lexicon cache (FdoLexiconCollection) and LCM cache (LcmCacheCollection). COM activation context management for FDO interop. ILRepack merges dependencies into single plugin DLL. Test project FwParatextLexiconPluginTests validates functionality. 4026 lines total.

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
C# .NET Framework 4.8.x, Paratext.LexicalContracts, COM activation context, ILRepack.

## Dependencies
- Upstream: Paratext.LexicalContracts, SIL.LCModel, Common/FwUtils
- Downstream: Paratext loads plugin, translators access FLEx lexicons

## Interop & Contracts
[LexiconPlugin] attribute for discovery, LexiconPlugin/V2 interfaces, COM activation context required for FDO, Events (LexemeAdded, SenseAdded, GlossAdded).

### Referenced By

- [External APIs](../../openspec/specs/architecture/interop/external-apis.md#external-integration-patterns) — Lexicon plugin integration

## Threading & Performance
Thread-safe (m_syncRoot), CacheSize=5 for lexicons/caches, cache hits avoid project reloading.

## Config & Feature Flags
CacheSize=5, registry settings via ParatextLexiconPluginRegistryHelper.

## Build Information
FwParatextLexiconPlugin.csproj (net48) → merged DLL via ILRepack.

## Interfaces and Data Models
FwLexiconPlugin (plugin entry), LexiconPlugin/V2, FdoLexicon, FdoLexEntryLexeme, ChooseFdoProjectForm.

## Entry Points
FwLexiconPlugin discovered via [LexiconPlugin] attribute, accessed via Paratext UI.

## Test Index
FwParatextLexiconPluginTests validates plugin, lexicon access, caching.

## Usage Hints
Deploy DLL to Paratext plugins folder, COM context required for FDO operations, caches 5 lexicons.

## Related Folders
Paratext8Plugin, ParatextImport, Common/ScriptureUtils.

## References
See `.cache/copilot/diff-plan.json` for file details.
