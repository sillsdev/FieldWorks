---
last-reviewed: 2025-10-30
last-verified-commit: 9611cf70e
status: draft
---

# ParserUI

## Purpose
User interface components for parser configuration and testing.
Provides UI for configuring the morphological parser, testing parser behavior, viewing parse traces,
managing parser settings, and debugging morphological analyses. Enables linguists to refine
and validate their morphological descriptions.

## Architecture
C# library with 28 source files. Contains 1 subprojects: ParserUI.

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

## Interop & Contracts
No explicit interop boundaries detected. Pure managed or native code.

## Threading & Performance
Threading model: UI thread marshaling.

## Config & Feature Flags
No explicit configuration or feature flags detected.

## Build Information
- C# class library project
- Build via: `dotnet build ParserUI.csproj`
- Parser UI and configuration

## Interfaces and Data Models

- **IParserTrace** (interface)
  - Path: `IParserTrace.cs`
  - Public interface definition

- **FileTimeToDateTimeConverter** (class)
  - Path: `FileTimeToDateTimeConverter.cs`
  - Public class implementation

- **HCTrace** (class)
  - Path: `HCTrace.cs`
  - Public class implementation

- **ImportWordSetDlg** (class)
  - Path: `ImportWordSetDlg.cs`
  - Public class implementation

- **ImportWordSetListener** (class)
  - Path: `ImportWordSetListener.cs`
  - Public class implementation

- **MillisecondsToTimeSpanConverter** (class)
  - Path: `MillisecondsToTimeSpanConverter.cs`
  - Public class implementation

- **ParserConnection** (class)
  - Path: `ParserConnection.cs`
  - Public class implementation

- **ParserListener** (class)
  - Path: `ParserListener.cs`
  - Public class implementation

- **ParserParametersBase** (class)
  - Path: `ParserParametersBase.cs`
  - Public class implementation

- **ParserParametersDlg** (class)
  - Path: `ParserParametersDlg.cs`
  - Public class implementation

- **ParserParametersListener** (class)
  - Path: `ImportWordSetListener.cs`
  - Public class implementation

- **ParserReportViewModel** (class)
  - Path: `ParserReportViewModel.cs`
  - Public class implementation

- **ParserReportsViewModel** (class)
  - Path: `ParserReportsViewModel.cs`
  - Public class implementation

- **ParserTraceUITransform** (class)
  - Path: `ParserTraceUITransform.cs`
  - Public class implementation

- **ParserUIStrings** (class)
  - Path: `ParserUIStrings.Designer.cs`
  - Public class implementation

- **TryAWordDlg** (class)
  - Path: `TryAWordDlg.cs`
  - Public class implementation

- **TryAWordSandbox** (class)
  - Path: `TryAWordSandbox.cs`
  - Public class implementation

- **WebPageInteractor** (class)
  - Path: `WebPageInteractor.cs`
  - Public class implementation

- **WordImporter** (class)
  - Path: `WordImporter.cs`
  - Public class implementation

- **XAmpleTrace** (class)
  - Path: `XAmpleTrace.cs`
  - Public class implementation

- **XAmpleWordGrammarDebugger** (class)
  - Path: `XAmpleWordGrammarDebugger.cs`
  - Public class implementation

- **ParserReportDialog** (xaml)
  - Path: `ParserReportDialog.xaml`
  - XAML UI definition

- **ParserReportsDialog** (xaml)
  - Path: `ParserReportsDialog.xaml`
  - XAML UI definition

- **TestUnificationViaXSLT** (xslt)
  - Path: `ParserUITests/TestUnificationViaXSLT.xsl`
  - XSLT transformation template

## Entry Points
- Parser configuration dialogs
- Trace viewer for parser analysis
- Word set import interface

## Test Index
Test projects: ParserUITests. 1 test files. Run via: `dotnet test` or Test Explorer in Visual Studio.

## Usage Hints
Library component. Reference in consuming projects. See Dependencies section for integration points.

## Related Folders
- **LexText/ParserCore/** - Parser engine configured by this UI
- **LexText/Morphology/** - Morphology editor with parser integration
- **LexText/Interlinear/** - Uses parser for text analysis

## References

- **Project files**: ParserUI.csproj, ParserUITests.csproj
- **Target frameworks**: net462
- **Key dependencies**: ..\..\Common\Controls\DetailControls\DetailControls, ..\..\Common\Controls\XMLViews\XMLViews
- **Key C# files**: AssemblyInfo.cs, FileTimeToDateTimeConverter.cs, IParserTrace.cs, ImportWordSetDlg.cs, ParserParametersDlg.cs, ParserReportsDialog.xaml.cs, ParserUIStrings.Designer.cs, TryAWordDlg.cs, TryAWordSandbox.cs, WordImporter.cs
- **XAML files**: ParserReportDialog.xaml, ParserReportsDialog.xaml
- **XSLT transforms**: RequiredOptionalPrefixSlotsWordGrammarDebugger.xsl, TLPSameSlotTwiceWordGrammarDebugger.xsl, TestUnificationViaXSLT.xsl
- **XML data/config**: nihimbiliguStep01BadInflectionClass.xml, nihinlikximuraNoCompoundRulesStep00.xml, nihinxolikximukestiraNoCompoundRulesStep00.xml, niyuwowwupeStemNameNotSetStep00.xml, niyuyiywupeStemNameFailStep02Result.xml
- **Source file count**: 28 files
- **Data file count**: 261 files

## References (auto-generated hints)
- Project files:
  - LexText/ParserUI/ParserUI.csproj
  - LexText/ParserUI/ParserUITests/ParserUITests.csproj
- Key C# files:
  - LexText/ParserUI/AssemblyInfo.cs
  - LexText/ParserUI/FileTimeToDateTimeConverter.cs
  - LexText/ParserUI/HCMaxCompoundRulesDlg.Designer.cs
  - LexText/ParserUI/HCMaxCompoundRulesDlg.cs
  - LexText/ParserUI/HCTrace.cs
  - LexText/ParserUI/IParserTrace.cs
  - LexText/ParserUI/ImportWordSetDlg.cs
  - LexText/ParserUI/ImportWordSetListener.cs
  - LexText/ParserUI/MillisecondsToTimeSpanConverter.cs
  - LexText/ParserUI/ParserConnection.cs
  - LexText/ParserUI/ParserListener.cs
  - LexText/ParserUI/ParserParametersBase.cs
  - LexText/ParserUI/ParserParametersDlg.cs
  - LexText/ParserUI/ParserReportDialog.xaml.cs
  - LexText/ParserUI/ParserReportViewModel.cs
  - LexText/ParserUI/ParserReportsDialog.xaml.cs
  - LexText/ParserUI/ParserReportsViewModel.cs
  - LexText/ParserUI/ParserTraceUITransform.cs
  - LexText/ParserUI/ParserUIStrings.Designer.cs
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingTests.cs
  - LexText/ParserUI/PositiveIntToRedBrushConverter.cs
  - LexText/ParserUI/TryAWordDlg.cs
  - LexText/ParserUI/TryAWordRootSite.cs
  - LexText/ParserUI/TryAWordSandbox.cs
  - LexText/ParserUI/WebPageInteractor.cs
- Data contracts/transforms:
  - LexText/ParserUI/HCMaxCompoundRulesDlg.resx
  - LexText/ParserUI/ImportWordSetDlg.resx
  - LexText/ParserUI/ParserParametersDlg.resx
  - LexText/ParserUI/ParserReportDialog.xaml
  - LexText/ParserUI/ParserReportsDialog.xaml
  - LexText/ParserUI/ParserUIStrings.resx
  - LexText/ParserUI/ParserUITests/TestUnificationViaXSLT.xsl
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/EmptyWord.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDump.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDumpAffixAlloFeats.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDumpNoCompoundRules.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTDumpStemNames.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/M3FXTRequiredOptionalPrefixSlots.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/RequiredOptionalPrefixSlotsWordGrammarDebugger.xsl
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/TLPSameSlotTwiceWordGrammarDebugger.xsl
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/TestFeatureStructureUnification.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/biliStep00BadInflection.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/biliStep01BadInflection.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/biliStep02BadInflection.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep00.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep01.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep02.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep03.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep04.xml
  - LexText/ParserUI/ParserUITests/WordGrammarDebuggingInputsAndResults/bilikesziStep05.xml
## Code Evidence
*Analysis based on scanning 26 source files*

- **Classes found**: 20 public classes
- **Interfaces found**: 1 public interfaces
- **Namespaces**: SIL.FieldWorks.LexText.Controls
- **Project references**: ..\..\Common\Controls\DetailControls\DetailControls, ..\..\Common\Controls\XMLViews\XMLViews
