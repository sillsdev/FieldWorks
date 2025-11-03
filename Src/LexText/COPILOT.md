---
last-reviewed: 2025-10-31
last-reviewed-tree: c399812b4465460b9d8163ce5e2d1dfee7116f679fa3ec0a64c6ceb477091ed8
status: draft
---

# LexText COPILOT summary

## Purpose
Organizational parent folder containing lexicon and text analysis components of FieldWorks Language Explorer (FLEx). Houses 11 subfolders covering lexicon management, interlinear text analysis, discourse charting, morphological parsing, and Pathway publishing integration. No direct source files; see individual subfolder COPILOT.md files for detailed documentation.

## Architecture
Container folder organizing related lexicon/text functionality into cohesive modules.

## Key Components
TBD - populate from code. See auto-generated hints below.

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
TBD - populate from code. See auto-generated hints below.

## Threading & Performance
TBD - populate from code. See auto-generated hints below.

## Config & Feature Flags
TBD - populate from code. See auto-generated hints below.

## Build Information
TBD - populate from code. See auto-generated hints below.

## Interfaces and Data Models
TBD - populate from code. See auto-generated hints below.

## Entry Points
TBD - populate from code. See auto-generated hints below.

## Test Index
TBD - populate from code. See auto-generated hints below.

## Usage Hints
TBD - populate from code. See auto-generated hints below.

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
- LexTextExe/COPILOT.md
- Lexicon/COPILOT.md
- Morphology/COPILOT.md
- ParserCore/COPILOT.md
- ParserUI/COPILOT.md

## References (auto-generated hints)
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
  - Src/LexText/LexTextExe/LexTextExe.csproj
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
- **LexTextExe/**: FLEx application entry point (COPILOT.md)
- **Lexicon/**: Lexicon editing and management UI (COPILOT.md)
- **Morphology/**: Morphological analysis tools (COPILOT.md)
- **ParserCore/**: Parser engine core logic (COPILOT.md)
- **ParserUI/**: Parser UI and configuration (COPILOT.md)
- **images/**: Shared image resources
