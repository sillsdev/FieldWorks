---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexText

## Purpose
Lexicon/Dictionary application and related components. This is one of the major application areas of FieldWorks, providing comprehensive lexicon editing, dictionary configuration, interlinear text analysis, morphology, and discourse features.

## Key Components
### Key Classes
- **FlexPathwayPlugin**
- **DeExportDialog**
- **MyFolders**
- **MyFoldersTest**
- **FlexPathwayPluginTest**
- **InterlinearMapping**
- **Sfm2FlexTextWordsFrag**
- **Sfm2FlexTextMappingBase**
- **Sfm2FlexTextBase**
- **PhonologicalFeatureChooserDlg**

### Key Interfaces
- **IFwExtension**
- **ICmLiftObject**
- **IPatternControl**
- **ILexReferenceSlice**
- **IParser**
- **IHCLoadErrorLogger**
- **IXAmpleWrapper**
- **IInterlinRibbon**

## Technology Stack
- C# .NET WinForms/WPF
- Complex linguistic data processing
- Dictionary and lexicon management
- Interlinear glossing and text analysis

## Dependencies
- Depends on: Cellar (data model), Common (UI infrastructure), FdoUi (data object UI), XCore (framework), xWorks (shared app infrastructure)
- Used by: Linguists and language workers for lexicon and text work

## Build Information
- Multiple C# projects comprising the lexicon application
- Build entire suite via solution or individual projects
- Build with MSBuild or Visual Studio

## Entry Points
- **LexTextExe** - Main lexicon application executable
- **LexTextDll** - Core functionality exposed to other components

## Related Folders
- **xWorks/** - Shared application infrastructure for LexText
- **XCore/** - Framework components used by LexText
- **FdoUi/** - Data object UI used in lexicon editing
- **Cellar/** - Data model for lexicon data
- **FwParatextLexiconPlugin/** - Exposes LexText data to Paratext
- **ParatextImport/** - Imports Paratext data into LexText

## Code Evidence
*Analysis based on scanning 430 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: FlexDePluginTests, FlexPathwayPluginTests, LexEdDllTests, LexTextControlsTests, LexTextDllTests
- **Project references**: ..\..\Common\Controls\DetailControls\DetailControls, ..\..\Common\Controls\XMLViews\XMLViews
