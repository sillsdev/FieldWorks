---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# ParserUI

## Purpose
Parser user interface components. Provides UI for configuring, testing, and tracing the morphological parser, including parser trace visualization and word import dialogs.

## Key Components
### Key Classes
- **TryAWordDlg**
- **ParserReportsDialog**
- **TryAWordSandbox**
- **WordImporter**
- **ParserParametersDlg**
- **FileTimeToDateTimeConverter**
- **ImportWordSetDlg**
- **XAmpleWordGrammarDebugger**
- **ImportWordSetListener**
- **ParserParametersListener**

### Key Interfaces
- **IParserTrace**

## Technology Stack
- C# .NET WinForms
- Parser configuration UI
- Trace visualization

## Dependencies
- Depends on: LexText/ParserCore (engine), Common (UI infrastructure)
- Used by: LexText/LexTextDll (morphology tools)

## Build Information
- C# class library project
- Build via: `dotnet build ParserUI.csproj`
- Parser UI and configuration

## Entry Points
- Parser configuration dialogs
- Trace viewer for parser analysis
- Word set import interface

## Related Folders
- **LexText/ParserCore/** - Parser engine configured by this UI
- **LexText/Morphology/** - Morphology editor with parser integration
- **LexText/Interlinear/** - Uses parser for text analysis

## Code Evidence
*Analysis based on scanning 26 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: SIL.FieldWorks.LexText.Controls
- **Project references**: ..\..\Common\Controls\DetailControls\DetailControls, ..\..\Common\Controls\XMLViews\XMLViews
