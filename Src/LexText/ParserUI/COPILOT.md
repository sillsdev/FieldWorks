---
owner: FIXME(set-owner)
last-reviewed: 2025-10-30
status: verified
---

# LexText/ParserUI

## Purpose
Parser user interface components. Provides UI for configuring, testing, and tracing the morphological parser, including parser trace visualization and word import dialogs.

## Key Components
- **ParserUI.csproj** - Parser UI library
- **HCTrace.cs** - Parser trace visualization
- **HCMaxCompoundRulesDlg** - Maximum compound rules configuration
- **ImportWordSetDlg** - Word set import dialog
- **FileTimeToDateTimeConverter.cs** - Utility converters
- **IParserTrace.cs** - Trace interface

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


## References
- **Project Files**: ParserUI.csproj
- **Key Dependencies**: ..\..\Common\Controls\DetailControls\DetailControls, ..\..\Common\Controls\XMLViews\XMLViews
- **Key C# Files**: FileTimeToDateTimeConverter.cs, HCMaxCompoundRulesDlg.cs, HCTrace.cs, IParserTrace.cs, ImportWordSetDlg.cs, ImportWordSetListener.cs, MillisecondsToTimeSpanConverter.cs, ParserConnection.cs
