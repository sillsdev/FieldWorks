---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# xWorks

## Purpose
Primary FieldWorks application shell and modules. Provides the main application infrastructure, dictionary configuration, data tree navigation, and shared functionality used across all FieldWorks work areas. This is the core application framework that hosts LexText and other modules.

## Key Components
### Key Classes
- **DTMenuHandler**
- **HeadwordNumbersDlg**
- **DictionaryPublicationDecorator**
- **DictionaryNodeOptions**
- **DictionaryNodeSenseOptions**
- **DictionaryNodeListOptions**
- **DictionaryNodeOption**
- **DictionaryNodeListAndParaOptions**
- **DictionaryNodeWritingSystemAndParaOptions**
- **DictionaryNodeWritingSystemOptions**

### Key Interfaces
- **IDictionaryListOptionsView**
- **IParaOption**
- **IDictionaryGroupingOptionsView**
- **ILcmStylesGenerator**
- **IFragmentWriter**
- **IFragment**
- **IHeadwordNumbersView**
- **IDictionarySenseOptionsView**

## Technology Stack
- C# .NET WinForms/WPF
- Application shell architecture
- Dictionary and data visualization
- XCore-based plugin framework

## Dependencies
- Depends on: XCore (framework), Cellar (data model), Common (UI), FdoUi (data UI), FwCoreDlgs (dialogs), views (rendering)
- Used by: End users as the main FieldWorks application

## Build Information
- C# application with extensive test suite
- Build with MSBuild or Visual Studio
- Primary executable for FieldWorks

## Entry Points
- Main application executable
- Application shell hosting various modules (LexText, etc.)
- Dictionary and data tree interfaces

## Related Folders
- **XCore/** - Application framework that xWorks is built on
- **LexText/** - Major module hosted by xWorks
- **Common/** - UI infrastructure used throughout xWorks
- **FdoUi/** - Data object UI components
- **FwCoreDlgs/** - Dialogs used in xWorks
- **views/** - Native rendering engine for data display
- **ManagedVwWindow/** - View window management
- **Cellar/** - Data model accessed by xWorks
- **FwResources/** - Resources used in xWorks UI

## Code Evidence
*Analysis based on scanning 159 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 15 public interfaces
- **Namespaces**: SIL.FieldWorks.XWorks, SIL.FieldWorks.XWorks.Archiving, SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators, SIL.FieldWorks.XWorks.DictionaryDetailsView, SIL.FieldWorks.XWorks.LexText
