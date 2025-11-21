---
last-reviewed: 2025-10-31
last-reviewed-tree: c399812b4465460b9d8163ce5e2d1dfee7116f679fa3ec0a64c6ceb477091ed8
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/LexText. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
<!-- copilot:auto-change-log end -->


# LexText COPILOT summary

## Purpose
Organizational parent folder containing lexicon and text analysis components of FieldWorks Language Explorer (FLEx). Houses 11 subfolders covering lexicon management, interlinear text analysis, discourse charting, morphological parsing, and Pathway publishing integration. No direct source files; see individual subfolder COPILOT.md files for detailed documentation.

## Architecture
Container folder organizing related lexicon/text functionality into cohesive modules.

## Key Components
This is an organizational parent folder. Key components are in the subfolders:
- **Discourse/**: Discourse chart analysis (Discourse.csproj)
- **FlexPathwayPlugin/**: Pathway publishing integration (FlexPathwayPlugin.csproj)
- **Interlinear/**: Interlinear text analysis (ITextDll.csproj)
- **LexTextControls/**: Shared UI controls (LexTextControls.csproj)
- **LexTextDll/**: Core business logic (LexTextDll.csproj)
- **FieldWorks/Common**: Provides the FieldWorks.exe host that now launches the LexText UI (LexTextExe stub removed in 2025)
- **Lexicon/**: Lexicon editor UI (LexEdDll.csproj)
- **Morphology/**: Morphological analysis (MorphologyEditorDll.csproj, MGA.csproj)
- **ParserCore/**: Parser engine (ParserCore.csproj, XAmpleCOMWrapper.vcxproj)
- **ParserUI/**: Parser UI (ParserUI.csproj)
- **images/**: Shared image resources

## Technology Stack
See individual subfolder COPILOT.md files.

## Dependencies

### Upstream (subfolders consume)
- **LCModel**: Data model
- **Common/**: Shared FW infrastructure
- **XCore**: Application framework
- **FdoUi**: Data object UI
- **FwCoreDlgs**: Common dialogs

### Downstream (consumed by)
- **xWorks**: Main FLEx application shell
- **FLEx users**: Lexicon and text analysis features

## Interop & Contracts
This folder is organizational only. Interop contracts exist in subfolders:
- **ParserCore/XAmpleCOMWrapper**: C++ COM interop for XAmple parser integration
- See individual subfolder COPILOT.md files for detailed interop contracts

## Threading & Performance
No direct threading code at this organizational level. Threading considerations are documented in individual subfolder COPILOT.md files, particularly:
- **Interlinear/**: UI controls requiring main thread affinity
- **ParserCore/**: Parser engine threading model
- **FieldWorks/Common**: Application-level threading concerns now live in the FieldWorks host

## Config & Feature Flags
Configuration is managed at the subfolder level. No centralized config at this organizational level. See individual subfolder COPILOT.md files for component-specific configurations.

## Build Information
No direct build at this level. Build via:
- Top-level FieldWorks.sln includes all LexText subprojects
- Individual subfolders have their own .csproj/.vcxproj files (see References section for complete list)

## Interfaces and Data Models
No interfaces or data models at this organizational level. Each subfolder defines its own interfaces and models:
- **Discourse/**: Chart data structures and UI contracts
- **Interlinear/**: Interlinear text models and glossing interfaces
- **Lexicon/**: Lexicon entry models and editor interfaces
- **ParserCore/**: Parser interfaces and morphological data models
- See individual subfolder COPILOT.md files for detailed interface documentation

## Entry Points
No direct entry points at this organizational level. Main entry points are:
- **FieldWorks.exe**: Hosts the LexText UI after the LexTextExe stub was removed
- **LexTextDll/**: Core library consumed by xWorks main application
- See individual subfolder COPILOT.md files for component-specific entry points

## Test Index
No tests at this organizational level. Tests are organized in subfolder test projects:
- Discourse/DiscourseTests/DiscourseTests.csproj
- FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.csproj
- Interlinear/ITextDllTests/ITextDllTests.csproj
- LexTextControls/LexTextControlsTests/LexTextControlsTests.csproj
- LexTextDll/LexTextDllTests/LexTextDllTests.csproj
- Lexicon/LexEdDllTests/LexEdDllTests.csproj
- Morphology/MorphologyEditorDllTests/MorphologyEditorDllTests.csproj
- Morphology/MGA/MGATests/MGATests.csproj
- ParserCore/ParserCoreTests/ParserCoreTests.csproj
- ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/XAmpleManagedWrapperTests.csproj
- ParserUI/ParserUITests/ParserUITests.csproj

Run tests via Visual Studio Test Explorer or FieldWorks.sln build.

## Usage Hints
This is an organizational folder. For usage guidance, see individual subfolder COPILOT.md files:
- **Lexicon/**: How to work with lexicon entries and management UI
- **Interlinear/**: Interlinear text analysis workflow
- **Discourse/**: Discourse chart creation and analysis
- **ParserCore/**: Parser configuration and integration
- **Morphology/**: Morphological analysis tools

## Related Folders
- **xWorks/**: Main application container
- **Common/**: Shared infrastructure
- **FdoUi**: Data object UI

## References
See individual subfolder COPILOT.md files:
- Discourse/COPILOT.md
- FlexPathwayPlugin/COPILOT.md
- Interlinear/COPILOT.md
- LexTextControls/COPILOT.md
- LexTextDll/COPILOT.md
- Common/FieldWorks/COPILOT.md (FieldWorks.exe host)
- Lexicon/COPILOT.md
- Morphology/COPILOT.md
- ParserCore/COPILOT.md
- ParserUI/COPILOT.md

## Auto-Generated Project References
- Project files:
  - Src/LexText/Discourse/Discourse.csproj
  - Src/LexText/Discourse/DiscourseTests/DiscourseTests.csproj
  - Src/LexText/FlexPathwayPlugin/FlexPathwayPlugin.csproj
  - Src/LexText/FlexPathwayPlugin/FlexPathwayPluginTests/FlexPathwayPluginTests.csproj
  - Src/LexText/Interlinear/ITextDll.csproj
  - Src/LexText/Interlinear/ITextDllTests/ITextDllTests.csproj
  - Src/LexText/LexTextControls/LexTextControls.csproj
  - Src/LexText/LexTextControls/LexTextControlsTests/LexTextControlsTests.csproj
  - Src/LexText/LexTextDll/LexTextDll.csproj
  - Src/LexText/LexTextDll/LexTextDllTests/LexTextDllTests.csproj
  - Src/LexText/Lexicon/LexEdDll.csproj
  - Src/LexText/Lexicon/LexEdDllTests/LexEdDllTests.csproj
  - Src/LexText/Morphology/MGA/MGA.csproj
  - Src/LexText/Morphology/MGA/MGATests/MGATests.csproj
  - Src/LexText/Morphology/MorphologyEditorDll.csproj
  - Src/LexText/Morphology/MorphologyEditorDllTests/MorphologyEditorDllTests.csproj
  - Src/LexText/ParserCore/ParserCore.csproj
  - Src/LexText/ParserCore/ParserCoreTests/ParserCoreTests.csproj
  - Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleCOMWrapper.vcxproj
  - Src/LexText/ParserCore/XAmpleManagedWrapper/BuildInclude.targets
  - Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapper.csproj
  - Src/LexText/ParserCore/XAmpleManagedWrapper/XAmpleManagedWrapperTests/XAmpleManagedWrapperTests.csproj
  - Src/LexText/ParserUI/ParserUI.csproj
  - Src/LexText/ParserUI/ParserUITests/ParserUITests.csproj
- Key C# files:
  - Src/LexText/Discourse/AdvancedMTDialog.Designer.cs
  - Src/LexText/Discourse/AdvancedMTDialog.cs
  - Src/LexText/Discourse/ChartLocation.cs
  - Src/LexText/Discourse/ConstChartBody.cs
  - Src/LexText/Discourse/ConstChartRowDecorator.cs
  - Src/LexText/Discourse/ConstChartVc.cs
  - Src/LexText/Discourse/ConstituentChart.Designer.cs
  - Src/LexText/Discourse/ConstituentChart.cs
  - Src/LexText/Discourse/ConstituentChartLogic.cs
  - Src/LexText/Discourse/DiscourseExportDialog.cs
  - Src/LexText/Discourse/DiscourseExporter.cs
  - Src/LexText/Discourse/DiscourseStrings.Designer.cs
  - Src/LexText/Discourse/DiscourseTests/AdvancedMTDialogLogicTests.cs
  - Src/LexText/Discourse/DiscourseTests/ConstChartRowDecoratorTests.cs
  - Src/LexText/Discourse/DiscourseTests/ConstituentChartDatabaseTests.cs
  - Src/LexText/Discourse/DiscourseTests/ConstituentChartTests.cs
  - Src/LexText/Discourse/DiscourseTests/DiscourseExportTests.cs
  - Src/LexText/Discourse/DiscourseTests/DiscourseTestHelper.cs
  - Src/LexText/Discourse/DiscourseTests/InMemoryDiscourseTestBase.cs
  - Src/LexText/Discourse/DiscourseTests/InMemoryLogicTest.cs
  - Src/LexText/Discourse/DiscourseTests/InMemoryMoveEditTests.cs
  - Src/LexText/Discourse/DiscourseTests/InMemoryMovedTextTests.cs
  - Src/LexText/Discourse/DiscourseTests/InterlinRibbonTests.cs
  - Src/LexText/Discourse/DiscourseTests/LogicTest.cs
  - Src/LexText/Discourse/DiscourseTests/MultilevelHeaderModelTests.cs
- Key C++ files:
  - Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleCOMWrapper.cpp
  - Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleWrapper.cpp
  - Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleWrapperCore.cpp
  - Src/LexText/ParserCore/XAmpleCOMWrapper/stdafx.cpp
- Key headers:
  - Src/LexText/ParserCore/XAmpleCOMWrapper/Resource.h
  - Src/LexText/ParserCore/XAmpleCOMWrapper/XAmpleWrapperCore.h
  - Src/LexText/ParserCore/XAmpleCOMWrapper/stdafx.h
  - Src/LexText/ParserCore/XAmpleCOMWrapper/xamplewrapper.h
- Data contracts/transforms:
  - Src/LexText/Discourse/AdvancedMTDialog.resx
  - Src/LexText/Discourse/ConstChartBody.resx
  - Src/LexText/Discourse/ConstituentChart.resx
  - Src/LexText/Discourse/DiscourseStrings.resx
  - Src/LexText/Discourse/SelectClausesDialog.resx
  - Src/LexText/Interlinear/ChooseTextWritingSystemDlg.resx
  - Src/LexText/Interlinear/ComplexConcControl.resx
  - Src/LexText/Interlinear/ComplexConcMorphDlg.resx
  - Src/LexText/Interlinear/ComplexConcTagDlg.resx
  - Src/LexText/Interlinear/ComplexConcWordDlg.resx
  - Src/LexText/Interlinear/ConcordanceControl.resx
  - Src/LexText/Interlinear/ConfigureInterlinDialog.resx
  - Src/LexText/Interlinear/CreateAllomorphTypeMismatchDlg.resx
  - Src/LexText/Interlinear/EditMorphBreaksDlg.resx
  - Src/LexText/Interlinear/FilterAllTextsDialog.resx
  - Src/LexText/Interlinear/FilterTextsDialog.resx
  - Src/LexText/Interlinear/FocusBoxController.resx
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTest.xml
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTestPunctuation.xml
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTestPunctuationWordAlignedXLingPap.xml
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-KalabaTestWordAlignedXLingPap.xml
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-OrizabaLesson2.xml
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-OrizabaLesson2WordAlignedXLingPap.xml
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/Phase1-SETepehuanCorn.xml
  - Src/LexText/Interlinear/ITextDllTests/ExportTestFiles/SETepehuanCornSingleListExample.xml
## Subfolders
- **Discourse/**: Discourse chart analysis tools (COPILOT.md)
- **FlexPathwayPlugin/**: Pathway publishing plugin integration (COPILOT.md)
- **Interlinear/**: Interlinear text analysis and glossing (COPILOT.md)
- **LexTextControls/**: Shared UI controls for lexicon/text features (COPILOT.md)
- **LexTextDll/**: Core lexicon/text business logic library (COPILOT.md)
- **FieldWorks/Common**: FieldWorks.exe host (COPILOT.md)
- **Lexicon/**: Lexicon editing and management UI (COPILOT.md)
- **Morphology/**: Morphological analysis tools (COPILOT.md)
- **ParserCore/**: Parser engine core logic (COPILOT.md)
- **ParserUI/**: Parser UI and configuration (COPILOT.md)
- **images/**: Shared image resources
