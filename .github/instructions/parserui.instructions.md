---
applyTo: "Src/LexText/ParserUI/**"
name: "parserui.instructions"
description: "Auto-generated concise instructions from COPILOT.md for ParserUI"
---

# ParserUI (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **TryAWordDlg**: Main parser testing dialog. Allows entering a wordform, invoking parser via ParserListener→ParserConnection→ParserScheduler, displaying analyses in TryAWordSandbox, and showing trace in HTML viewer (Gecko WebBrowser). Supports "Trace parse" checkbox and "Select morphs to trace" for granular HC trace control. Persists state via PersistenceProvider. Implements IMediatorProvider, IPropertyTableProvider for XCore integration.
- Inputs: LcmCache, Mediator, PropertyTable, word string, ParserListener
- UI Controls: FwTextBox (wordform input), TryAWordSandbox (analysis display), HtmlControl (Gecko trace viewer), CheckBox (trace options), Timer (async status updates)
- Methods: SetDlgInfo(), TryItHandler(), OnParse(), DisplayTrace()
- **TryAWordSandbox**: Sandbox control for displaying parse results within TryAWordDlg. Extends InterlinLineChoices for analysis display. Uses TryAWordRootSite for Views rendering.
- **TryAWordRootSite**: Root site for Views-based analysis display in sandbox. Extends SimpleRootSite.

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: c17511bd9bdcdbda3ea395252447efac41d9c4b5ef7ad360afbd374ff008585b
status: reviewed
---

# ParserUI

## Purpose
Parser configuration and testing UI components. Provides TryAWordDlg for interactive single-word parsing with trace visualization, ParserReportsDialog for viewing parse batch results and statistics, ImportWordSetDlg for bulk wordlist import, ParserParametersDlg for parser configuration, and XAmpleWordGrammarDebugger for grammar file debugging. Enables linguists to refine and validate morphological descriptions by testing parser behavior, viewing parse traces (HC XML or XAmple SGML), managing parser settings, and debugging morphological analyses.

## Architecture
C# library (net48) with 28 source files (~5.9K lines). Mix of WinForms (TryAWordDlg, ImportWordSetDlg, ParserParametersDlg) and WPF/XAML (ParserReportsDialog, ParserReportDialog) with MVVM view models. Integrates Gecko WebBrowser control for HTML trace display via GeneratedHtmlViewer.

## Key Components

### Try A Word Dialog
- **TryAWordDlg**: Main parser testing dialog. Allows entering a wordform, invoking parser via ParserListener→ParserConnection→ParserScheduler, displaying analyses in TryAWordSandbox, and showing trace in HTML viewer (Gecko WebBrowser). Supports "Trace parse" checkbox and "Select morphs to trace" for granular HC trace control. Persists state via PersistenceProvider. Implements IMediatorProvider, IPropertyTableProvider for XCore integration.
  - Inputs: LcmCache, Mediator, PropertyTable, word string, ParserListener
  - UI Controls: FwTextBox (wordform input), TryAWordSandbox (analysis display), HtmlControl (Gecko trace viewer), CheckBox (trace options), Timer (async status updates)
  - Methods: SetDlgInfo(), TryItHandler(), OnParse(), DisplayTrace()
- **TryAWordSandbox**: Sandbox control for displaying parse results within TryAWordDlg. Extends InterlinLineChoices for analysis display. Uses TryAWordRootSite for Views rendering.
- *
