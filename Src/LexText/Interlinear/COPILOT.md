# LexText/Interlinear

## Purpose
Interlinear text analysis and glossing functionality. Provides tools for creating and managing interlinear texts with morpheme-by-morpheme analysis and glossing.

## Key Components
- **ITextDll.csproj** - Interlinear text library
- **BIRDInterlinearImporter.cs** - BIRD format import
- **ChooseAnalysisHandler.cs** - Analysis selection
- **ChooseTextWritingSystemDlg** - Writing system selection dialog
- **ClosedFeatureNode.cs** - Feature analysis nodes
- Interlinear text editing and display components
- Morpheme analysis tools

## Technology Stack
- C# .NET WinForms
- Complex text layout and rendering
- Linguistic analysis algorithms

## Dependencies
- Depends on: Cellar (data model), Common (UI and views), LexText/ParserCore
- Used by: LexText application for text analysis

## Build Information
- C# class library project
- Build via: `dotnet build ITextDll.csproj` (Note: project named ITextDll)
- Core component of text analysis features

## Entry Points
- Interlinear text editor
- Morpheme analysis and glossing
- Text import from various formats

## Related Folders
- **LexText/Morphology/** - Morphological parsing for interlinear
- **LexText/ParserCore/** - Parsing engine for analysis
- **LexText/Discourse/** - Discourse analysis on interlinear texts
- **Common/SimpleRootSite/** - View hosting for interlinear display
- **views/** - Native rendering for complex interlinear layout
